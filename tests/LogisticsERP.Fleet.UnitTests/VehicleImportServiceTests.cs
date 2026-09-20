using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleImportServiceTests
{
    [Theory]
    [InlineData("ه أ\u00A0 2993", "ه ا 2993", "H A 2993")]
    [InlineData("أ ط ب 2255", "ا ط ب 2255", "A T B 2255")]
    [InlineData("ق ب\u00A0 6488", "ق ب 6488", "G B 6488")]
    [InlineData("ح أ\u00A0 8332", "ح ا 8332", "J A 8332")]
    public void ArabicSaudiPlateIsNormalizedAndConvertedToEnglish(
        string input,
        string expectedArabic,
        string expectedEnglish)
    {
        var parsed = VehicleImportProcessor.TryParsePlate(input, out var plate);

        Assert.True(parsed);
        Assert.NotNull(plate);
        Assert.Equal(expectedArabic, plate.NumberAr);
        Assert.Equal(expectedEnglish, plate.NumberEn);
    }

    [Fact]
    public async Task ValidationResolvesOwnersAndCatalogsWithoutWriting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var beforeVehicles = await dbContext.Vehicles.CountAsync(cancellationToken);
        var beforeManufacturers = await dbContext.VehicleManufacturers.CountAsync(cancellationToken);
        var service = new VehicleImportValidationService(CreateProcessor(dbContext));
        using var workbook = CreateWorkbook(
            ["ه أ\u00A0 2415", "دراجة آلية", "بجي", "دراجة نارية", 2025, "411242220", "MD2A21BX1SWG48250", "احمر", "جدة", "7015658094", "7015658094"],
            ["أ ط ب 2255", "خاص", "شانجان", "السفن", 2023, "48426910", "LS5A2ASE6PD918591", "فضي", "الرياض", "7038745530", "7034861059"]);

        var result = await service.ValidateAsync(workbook, "vehicles.xlsx", cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.ValidateOnly);
        Assert.True(result.Value.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Equal(2, result.Value.CreatedVehicles);
        Assert.Equal(2, result.Value.CreatedManufacturers);
        Assert.Equal(2, result.Value.CreatedModels);
        Assert.Equal("H A 2415", result.Value.Rows[0].PlateNumberEn);
        Assert.Equal("Sponsor", result.Value.Rows[0].OwnerType);
        Assert.Equal(VehicleOwnershipType.Owned.ToString(), result.Value.Rows[0].OwnershipType);
        Assert.Equal(VehicleRegistrationType.Private.ToString(), result.Value.Rows[1].RegistrationType);
        Assert.Equal(VehicleOwnershipType.Leased.ToString(), result.Value.Rows[1].OwnershipType);
        Assert.Contains(result.Value.Issues, issue =>
            issue.Severity == "Warning" && issue.Field == "purchasedFromSupplierId");
        Assert.Equal(beforeVehicles, await dbContext.Vehicles.CountAsync(cancellationToken));
        Assert.Equal(beforeManufacturers, await dbContext.VehicleManufacturers.CountAsync(cancellationToken));
        Assert.DoesNotContain(dbContext.ChangeTracker.Entries(), entry => entry.State != EntityState.Unchanged);
    }

    [Fact]
    public async Task ImportCreatesVehiclesAndUsesSupplierOwnerWithoutPurchaseSupplier()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var ownerSupplier = new VehicleSupplier
        {
            Code = "OWNER-IMPORT",
            NameAr = "مورد مالك",
            NameEn = "Owner Supplier",
            CommercialRegistrationNumber = "1010101010",
            Status = VehicleCatalogStatus.Active
        };
        dbContext.VehicleSuppliers.Add(ownerSupplier);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = new VehicleImportService(CreateProcessor(dbContext, new AnonymousCurrentUser()));
        using var workbook = CreateWorkbook(
            ["ق ب\u00A0 6956", "دراجة آلية", "بوكسر", "BOXER", 2025, "554717220", "MD2A21BX2SWH41214", "اسود", "جدة", "1010101010", "7015658094"]);

        var result = await service.ImportAsync(workbook, "vehicle-supplier-owner.xlsx", cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanImport);
        Assert.True(result.Value.Imported);
        Assert.Equal(1, result.Value.CreatedVehicles);
        var vehicle = await dbContext.Vehicles.SingleAsync(cancellationToken);
        Assert.Equal("ق ب 6956", vehicle.PlateNumberAr);
        Assert.Equal("G B 6956", vehicle.PlateNumberEn);
        Assert.Equal(VehicleRegistrationType.Motorcycle, vehicle.RegistrationType);
        Assert.Equal(VehicleType.Motorcycle, vehicle.VehicleType);
        Assert.Equal(ownerSupplier.Id, vehicle.RegisteredOwnerSupplierId);
        Assert.Null(vehicle.RegisteredOwnerSponsorId);
        Assert.Null(vehicle.PurchasedFromSupplierId);
        Assert.Equal(Sponsor.AlBawabaNextCompanyId, vehicle.SponsorId);
        Assert.Equal(OperatingCity.JeddahId, vehicle.OperatingCityId);
        Assert.Equal(VehicleOwnershipType.Leased, vehicle.OwnershipType);
        Assert.Single(await dbContext.VehicleOperationalStatusPeriods.ToArrayAsync(cancellationToken));
        Assert.NotEqual(Guid.Empty, (await dbContext.VehicleOperationalStatusPeriods.SingleAsync(cancellationToken)).ChangedByUserId);
        Assert.Single(await dbContext.VehicleOdometerReadings.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportWithAnyInvalidOwnerDoesNotWriteAnyRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var service = new VehicleImportService(CreateProcessor(dbContext));
        using var workbook = CreateWorkbook(
            ["ه أ 3038", "دراجة آلية", "بجي", "دراجة نارية", 2025, "821973220", "MD2A21BX3SWG48444", "احمر", "جدة", "7015658094", "7015658094"],
            ["ق ب 6463", "دراجة آلية", "بوكسر", "BOXER", 2025, "271727220", "MD2A21BX4SWH41196", "اسود", "جدة", "9999999999", "7015658094"]);

        var result = await service.ImportAsync(workbook, "invalid-owner.xlsx", cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanImport);
        Assert.False(result.Value.Imported);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "ownerNumber" && issue.Severity == "Error");
        Assert.Empty(await dbContext.Vehicles.ToArrayAsync(cancellationToken));
        Assert.DoesNotContain(await dbContext.VehicleManufacturers.ToArrayAsync(cancellationToken),
            item => item.NameAr is "بجي" or "بوكسر");
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleImport_{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
            .ConfigureWarnings(options => options.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options,
        TimeProvider.System);

    private static VehicleImportProcessor CreateProcessor(
        ApplicationDbContext dbContext,
        ICurrentUser? currentUser = null) => new(
        dbContext,
        new FleetServiceSupport(currentUser ?? new TestCurrentUser(), new PermitAll(), TimeProvider.System),
        NullLogger<VehicleImportProcessor>.Instance);

    private static MemoryStream CreateWorkbook(params object?[][] rows)
    {
        string[] headers =
        [
            "رقم   اللوحة", "نوع تسجيل الاستماره", "الماركة", "الطراز", "سنة الصنع",
            "الرقم التسلسلي", "رقم الهيكل", "اللون الأساسي", "المدينة",
            "رقم هوية المالك", "رقم السجل او المستخدم"
        ];
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("المركبات");
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value = XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.Parse("019d0000-0000-7000-8000-000000000001");
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "vehicle-import-test";
    }

    private sealed class AnonymousCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => "anonymous-vehicle-import-test";
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(
            Guid userId,
            long authorizationVersion,
            string permissionKey,
            PermissionScope? scope = null,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public void InvalidateUser(Guid userId, long authorizationVersion)
        {
        }
    }
}
