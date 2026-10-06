using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelCardCityTests
{
    [Fact]
    public async Task CreateAndListReturnCityNamesAndFilterBeforeCountingAndPaging()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        await db.SaveChangesAsync(ct);
        var service = Service(db);
        var sponsorId = Assert.Single(db.Sponsors.Local).Id;
        var jeddah = await service.CreateCardAsync(new("PetroApp", "B W 201", null, null, sponsorId, OperatingCity.JeddahId), ct);
        var riyadh = await service.CreateCardAsync(new("PetroApp", "BW202", null, null, sponsorId, OperatingCity.RiyadhId), ct);

        Assert.True(jeddah.IsSuccess, jeddah.Error.Description);
        Assert.True(riyadh.IsSuccess, riyadh.Error.Description);
        Assert.Equal("جدة", jeddah.Value!.OperatingCityNameAr);
        Assert.Equal("BW201", jeddah.Value.CardNumber);
        Assert.Equal("Jeddah", jeddah.Value.OperatingCityNameEn);
        var page = await service.GetCardsAsync(null, null, null, 1, 1, OperatingCity.RiyadhId, ct);
        Assert.True(page.IsSuccess, page.Error.Description);
        Assert.Equal(1, page.Value!.TotalCount);
        Assert.Equal(riyadh.Value!.Id, Assert.Single(page.Value.Items).Id);
        var detail = await service.GetCardAsync(riyadh.Value.Id, ct);
        Assert.Equal("الرياض", detail.Value!.OperatingCityNameAr);
        Assert.Equal("Riyadh", detail.Value.OperatingCityNameEn);
    }

    [Fact]
    public async Task InvalidCitiesAndStaleVersionsDoNotChangeCardsOrAssignments()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var deletedCity = new OperatingCity { IsDeleted = true };
        db.OperatingCities.Add(deletedCity);
        await db.SaveChangesAsync(ct);
        var sponsorId = Assert.Single(db.Sponsors.Local).Id;
        var service = Service(db);
        foreach (var cityId in new[] { Guid.Empty, Guid.NewGuid(), deletedCity.Id })
        {
            var invalid = await service.CreateCardAsync(new("PetroApp", "BW201", null, null, sponsorId, cityId), ct);
            Assert.Equal(FuelErrors.OperatingCityNotFound.Code, invalid.Error.Code);
        }
        Assert.Empty(db.FuelCards.Local);
        var created = await service.CreateCardAsync(new("PetroApp", "BW201", null, "Keep note", sponsorId, OperatingCity.JeddahId), ct);
        Assert.True(created.IsSuccess, created.Error.Description);
        var card = created.Value!;
        var assignment = new FuelCardRiderAssignment
        {
            FuelCardId = card.Id, RiderProfileId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(),
            EffectiveFrom = new DateOnly(2026, 9, 1), AssignmentReason = "Keep assignment"
        };
        db.FuelCardRiderAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        var stale = await service.SetCityAsync(card.Id, new(OperatingCity.RiyadhId, Convert.ToBase64String([1])), ct);
        Assert.Equal(FuelErrors.ConcurrencyConflict.Code, stale.Error.Code);
        var invalidCity = await service.SetCityAsync(card.Id, new(deletedCity.Id, card.RowVersion), ct);
        Assert.Equal(FuelErrors.OperatingCityNotFound.Code, invalidCity.Error.Code);
        Assert.Equal(OperatingCity.JeddahId, (await db.FuelCards.SingleAsync(ct)).OperatingCityId);
        var forbidden = await Service(db, allow: false).SetCityAsync(card.Id, new(OperatingCity.RiyadhId, card.RowVersion), ct);
        Assert.Equal(FuelErrors.Forbidden.Code, forbidden.Error.Code);
        var updated = await service.SetCityAsync(card.Id, new(OperatingCity.RiyadhId, card.RowVersion), ct);
        Assert.True(updated.IsSuccess, updated.Error.Description);
        Assert.Equal(OperatingCity.RiyadhId, updated.Value!.OperatingCityId);
        Assert.Equal(sponsorId, updated.Value.SponsorId);
        Assert.Equal("Keep note", updated.Value.Notes);
        Assert.NotEqual(card.RowVersion, updated.Value.RowVersion);
        Assert.Null((await db.FuelCardRiderAssignments.SingleAsync(ct)).EffectiveTo);
        var replay = await service.SetCityAsync(card.Id, new(OperatingCity.JeddahId, card.RowVersion), ct);
        Assert.Equal(FuelErrors.ConcurrencyConflict.Code, replay.Error.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CardImportsUseCitiesWhileUsageImportSkipsUnknownCards(bool selectRiyadh)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var sponsorId = Assert.Single(db.Sponsors.Local).Id;
        db.FuelCards.Add(new FuelCard
        {
            SponsorId = sponsorId, OperatingCityId = OperatingCity.RiyadhId,
            Provider = FuelCardProvider.PetroApp, IdentifierType = FuelCardIdentifierType.InternalNumber,
            CardNumber = "BW200", NormalizedCardNumber = "BW200"
        });
        await db.SaveChangesAsync(ct);
        Guid? selectedCity = selectRiyadh ? OperatingCity.RiyadhId : null;
        var expectedCity = selectedCity ?? OperatingCity.JeddahId;
        var service = Service(db);
        using var bulk = Workbook(["number", "sponsor 70 number", "company name", "city name"],
            ["BW200", "7038745530", "بترو اب", "Jeddah"],
            ["BW201", "7038745530", "بترو اب", selectRiyadh ? "Riyadh" : "Jeddah"]);
        var bulkResult = await new FuelCardBulkImportService(db).ImportAsync(bulk, false, ct);
        Assert.True(bulkResult.IsSuccess, bulkResult.Error.Description);
        Assert.True(bulkResult.Value!.Imported);
        Assert.Equal(OperatingCity.RiyadhId, Assert.Single(bulkResult.Value.Rows, x => !x.WillCreateCard).OperatingCityId);
        Assert.Equal(expectedCity, Assert.Single(bulkResult.Value.Rows, x => x.WillCreateCard).OperatingCityId);
        using var numbers = Workbook(["number"], ["BW200"], ["BW202"]);
        var numberResult = await service.ImportCardNumbersAsync(numbers, sponsorId, false, selectedCity, ct);
        Assert.True(numberResult.IsSuccess, numberResult.Error.Description);
        using var detailed = Workbook(
            ["رقم الفاتورة", "المركبة", "الرقم الداخلي", "نوع الوقود", "التكلفة", "التكلفة قبل الضريبة", "عدد اللترات", "التاريخ"],
            ["INV-1", "ب ب و 835", "BW200", "91", "20", "17.39", "9.174", "2026-09-15 10:00:00"],
            ["INV-2", "ب ب و 836", "BW203", "91", "20", "17.39", "9.174", "2026-09-15 10:00:00"]);
        var reportResult = await service.ImportAsync(new PrivateFileUpload(detailed, "fuel.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", detailed.Length),
            new DateOnly(2026, 9, 1), ct);
        Assert.True(reportResult.IsSuccess, reportResult.Error.Description);
        var cards = await db.FuelCards.OrderBy(x => x.CardNumber).ToArrayAsync(ct);
        Assert.Equal(3, cards.Length);
        Assert.Equal(OperatingCity.RiyadhId, cards[0].OperatingCityId);
        Assert.All(cards.Skip(1), x => Assert.Equal(expectedCity, x.OperatingCityId));
        Assert.DoesNotContain(cards, card => card.CardNumber == "BW203");
        Assert.Equal(0, reportResult.Value!.CreatedCards);
        Assert.Equal(1, reportResult.Value.InvalidRows);
        Assert.Contains(reportResult.Value.Errors, error => error.CardNumber == "BW203" && error.Code == "card_not_found");
        Assert.Single(await db.FuelCardMonthlyUsages.ToArrayAsync(ct));
    }

    [Fact]
    public async Task CardNumberImportRejectsUnknownCityAndUsageImportRejectsInvalidFile()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        await db.SaveChangesAsync(ct);
        var sponsorId = Assert.Single(db.Sponsors.Local).Id;
        var unknownCity = Guid.NewGuid();
        var service = Service(db);
        using var bulk = Workbook(["number", "sponsor 70 number", "company name", "city name"],
            ["BW201", "7038745530", "بترو اب", "Unknown city"]);
        var bulkResult = await new FuelCardBulkImportService(db).ImportAsync(bulk, false, ct);
        Assert.True(bulkResult.IsSuccess, bulkResult.Error.Description);
        Assert.False(bulkResult.Value!.CanImport);
        Assert.Single(bulkResult.Value.Issues);
        Assert.Equal(2, bulkResult.Value.Issues[0].RowNumber);
        using var numbers = Workbook(["number"], ["BW202"]);
        var numberResult = await service.ImportCardNumbersAsync(numbers, sponsorId, false, unknownCity, ct);
        Assert.Equal(FuelErrors.OperatingCityNotFound.Code, numberResult.Error.Code);
        var reportResult = await service.ImportAsync(new PrivateFileUpload(numbers, "fuel.xlsx", "application/octet-stream", numbers.Length), null, ct);
        Assert.Equal(FuelErrors.InvalidFile.Code, reportResult.Error.Code);
        Assert.Empty(await db.FuelCards.ToArrayAsync(ct));
        Assert.Empty(await db.FuelCardImports.ToArrayAsync(ct));
    }

    [Fact]
    public void CityForeignKeyIsRequiredIndexedAndRestrictsDeletion()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FuelCityModel;Trusted_Connection=True").Options);
        var entity = db.Model.FindEntityType(typeof(FuelCard))!;
        var property = entity.FindProperty(nameof(FuelCard.OperatingCityId))!;
        Assert.False(property.IsNullable);
        var foreignKey = Assert.Single(entity.GetForeignKeys(), x => x.Properties.Contains(property));
        Assert.Equal(typeof(OperatingCity), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Contains(property));
    }

    private static ApplicationDbContext CreateContext()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FuelCity_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .AddInterceptors(new RowVersionInterceptor()).Options);
        db.AddRange(new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "Test sponsor" },
            new GlobalCity { Id = GlobalCity.JeddahId, Code = "JEDDAH", NameAr = "جدة", NameEn = "Jeddah" },
            new GlobalCity { Id = GlobalCity.RiyadhId, Code = "RIYADH", NameAr = "الرياض", NameEn = "Riyadh" },
            new OperatingCity { Id = OperatingCity.JeddahId, GlobalCityId = GlobalCity.JeddahId },
            new OperatingCity { Id = OperatingCity.RiyadhId, GlobalCityId = GlobalCity.RiyadhId });
        return db;
    }

    private static FuelCardService Service(ApplicationDbContext db, bool allow = true) =>
        new(db, new CurrentUser(), new PermissionChecker(allow), TimeProvider.System);

    private static MemoryStream Workbook(string[] headers, params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Cards");
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
        for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < rows[row].Length; column++) sheet.Cell(row + 2, column + 1).Value = rows[row][column];
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class PermissionChecker(bool allow) : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(allow);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class RowVersionInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntity>()
                         .Where(x => x.State is EntityState.Added or EntityState.Modified))
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            return ValueTask.FromResult(result);
        }
    }
}
