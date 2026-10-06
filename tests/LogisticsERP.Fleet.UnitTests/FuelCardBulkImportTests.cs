using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelCardBulkImportTests
{
    [Fact]
    public async Task ValidateThenImportCreatesCardsUnderEachSponsorAndProvider()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var first = new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "مؤسسة البوابة التجارية" };
        var second = new Sponsor { EmployerIdentityNumber = "7015658094", RegistryNameAr = "شركة البوابة المقبلة" };
        dbContext.Sponsors.AddRange(first, second);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = BulkService(dbContext);
        using var previewFile = BulkWorkbook(
            ["B W 203", "7038745530", "بترو اب", ""],
            ["BW203", "7015658094", "سياره كار", "الرياض"]);

        var preview = await service.ImportAsync(previewFile, true, cancellationToken: cancellationToken);

        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.True(preview.Value!.CanImport);
        Assert.False(preview.Value.Imported);
        Assert.Equal(2, preview.Value.NewCards);
        Assert.Equal(["PetroApp", "SayaraApp"], preview.Value.Rows.Select(row => row.Provider));
        Assert.Empty(await dbContext.FuelCards.ToArrayAsync(cancellationToken));

        using var importFile = BulkWorkbook(
            ["B W 203", "7038745530", "بترو اب", ""],
            ["BW203", "7015658094", "سياره كار", "الرياض"]);
        var import = await service.ImportAsync(importFile, false, cancellationToken: cancellationToken);

        Assert.True(import.IsSuccess, import.Error.Description);
        Assert.True(import.Value!.Imported);
        var cards = await dbContext.FuelCards.OrderBy(card => card.Provider).ToArrayAsync(cancellationToken);
        Assert.Equal(2, cards.Length);
        Assert.Equal(first.Id, cards[0].SponsorId);
        Assert.Equal(second.Id, cards[1].SponsorId);
        Assert.Equal("BW203", cards[0].CardNumber);
        Assert.Equal([OperatingCity.JeddahId, OperatingCity.RiyadhId], cards.Select(card => card.OperatingCityId));
        Assert.Equal([OperatingCity.JeddahId, OperatingCity.RiyadhId], preview.Value.Rows.Select(row => row.OperatingCityId));
    }

    [Fact]
    public async Task ImportRejectsUnknownSponsorAndFuelCompanyWithoutSavingAnyRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.Sponsors.Add(new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "مؤسسة البوابة التجارية" });
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = BulkWorkbook(
            ["BW201", "7038745530", "بترو اب", "جدة"],
            ["BW202", "7099999999", "بترو اب", "جدة"],
            ["BW203", "7038745530", "غير معروف", "جدة"]);

        var result = await BulkService(dbContext).ImportAsync(workbook, false, cancellationToken: cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Equal([3, 4], result.Value.Issues.Select(issue => issue.RowNumber));
        Assert.Empty(await dbContext.FuelCards.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ExistingCardWithDifferentSponsorBlocksTheBatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var first = new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "مؤسسة البوابة التجارية" };
        var second = new Sponsor { EmployerIdentityNumber = "7015658094", RegistryNameAr = "شركة البوابة المقبلة" };
        dbContext.Sponsors.AddRange(first, second);
        dbContext.FuelCards.Add(new FuelCard
        {
            SponsorId = first.Id, Provider = FuelCardProvider.PetroApp,
            IdentifierType = FuelCardIdentifierType.InternalNumber,
            CardNumber = "BW203", NormalizedCardNumber = "BW203"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = BulkWorkbook(["BW203", "7015658094", "بترو اب", "جدة"]);

        var result = await BulkService(dbContext).ImportAsync(workbook, false, cancellationToken: cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanImport);
        Assert.Single(result.Value.Issues);
        Assert.Equal(first.Id, (await dbContext.FuelCards.SingleAsync(cancellationToken)).SponsorId);
    }

    [Fact]
    public async Task ExistingCardNumberEndpointStillAcceptsOneColumnAndSponsorId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var sponsor = new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "مؤسسة البوابة التجارية" };
        dbContext.Sponsors.Add(sponsor);
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Cards");
        sheet.Cell(1, 1).Value = "number";
        sheet.Cell(2, 1).Value = "B W 203";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await new FuelCardService(dbContext, new TestCurrentUser(), new AllowPermissionChecker(), TimeProvider.System)
            .ImportCardNumbersAsync(stream, sponsor.Id, false, cancellationToken: cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        var card = await dbContext.FuelCards.SingleAsync(cancellationToken);
        Assert.Equal(sponsor.Id, card.SponsorId);
        Assert.Equal("BW203", card.CardNumber);
    }

    [Fact]
    public async Task LegacySheetWithoutCityColumnDefaultsToJeddah()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        dbContext.Sponsors.Add(new Sponsor { EmployerIdentityNumber = "7038745530", RegistryNameAr = "Test sponsor" });
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Cards");
        sheet.Cell(1, 1).Value = "number";
        sheet.Cell(1, 2).Value = "sponsor 70 number";
        sheet.Cell(1, 3).Value = "company name";
        sheet.Cell(2, 1).Value = "BW204";
        sheet.Cell(2, 2).Value = "7038745530";
        sheet.Cell(2, 3).Value = "بترو اب";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await BulkService(dbContext).ImportAsync(stream, false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        Assert.Equal(OperatingCity.JeddahId, (await dbContext.FuelCards.SingleAsync(cancellationToken)).OperatingCityId);
    }

    private static ApplicationDbContext CreateContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FuelCardBulkImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false)).Options);
        context.GlobalCities.AddRange(
            new GlobalCity { Id = GlobalCity.JeddahId, NameAr = "جدة", NameEn = "Jeddah" },
            new GlobalCity { Id = GlobalCity.RiyadhId, NameAr = "الرياض", NameEn = "Riyadh" });
        context.OperatingCities.AddRange(
            new OperatingCity { Id = OperatingCity.JeddahId, GlobalCityId = GlobalCity.JeddahId },
            new OperatingCity { Id = OperatingCity.RiyadhId, GlobalCityId = GlobalCity.RiyadhId });
        return context;
    }

    private static FuelCardBulkImportService BulkService(ApplicationDbContext dbContext) =>
        new(dbContext);

    private static MemoryStream BulkWorkbook(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Cards");
        sheet.Cell(1, 1).Value = "number";
        sheet.Cell(1, 2).Value = "sponsor 70 number";
        sheet.Cell(1, 3).Value = "company name";
        sheet.Cell(1, 4).Value = "city name";
        for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < rows[row].Length; column++)
                sheet.Cell(row + 2, column + 1).Value = rows[row][column];
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class AllowPermissionChecker : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
