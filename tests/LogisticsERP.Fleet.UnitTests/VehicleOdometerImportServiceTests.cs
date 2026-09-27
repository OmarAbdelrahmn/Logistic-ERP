using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleOdometerImportServiceTests
{
    [Fact]
    public async Task ValidationPreviewsChangesWithoutWriting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, cancellationToken);
        using var workbook = CreateWorkbook(["SERIAL-100", 12500]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "odometer.xlsx", true, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Equal(1, result.Value.ChangedVehicles);
        Assert.Equal(10_000, vehicle.CurrentOdometer);
        Assert.Equal(10_000, vehicle.TrackedDistanceKm);
        Assert.Null(vehicle.LastOdometerAtUtc);
    }

    [Fact]
    public async Task ImportUpdatesMatchedVehicleMileageFieldsDirectly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, cancellationToken);
        using var workbook = CreateWorkbook(["SERIAL-100", 12500]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "odometer.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Updated);
        Assert.Equal(12_500, vehicle.CurrentOdometer);
        Assert.Equal(12_500, vehicle.TrackedDistanceKm);
        Assert.NotNull(vehicle.LastOdometerAtUtc);
        Assert.Empty(dbContext.VehicleOdometerReadings);
    }

    [Fact]
    public async Task ImportCanCorrectAnOverstatedReading()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, cancellationToken);
        using var workbook = CreateWorkbook(["SERIAL-100", 9500]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "odometer.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(9_500, vehicle.CurrentOdometer);
        Assert.Equal(9_500, vehicle.TrackedDistanceKm);
    }

    [Fact]
    public async Task InvalidRowBlocksEveryVehicleUpdate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, cancellationToken);
        using var workbook = CreateWorkbook(["SERIAL-100", 12500], ["UNKNOWN", 9000]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "odometer.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "serialNumber");
        Assert.Equal(10_000, vehicle.CurrentOdometer);
    }

    [Fact]
    public async Task RejectsDuplicateSerialAndInvalidReading()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, cancellationToken);
        using var workbook = CreateWorkbook(["SERIAL-100", 12000], ["SERIAL 100", 13000], ["OTHER", -1]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "odometer.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.Equal(2, result.Value.Issues.Count);
        Assert.Equal(10_000, vehicle.CurrentOdometer);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleOdometerImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false))
            .Options,
        TimeProvider.System);

    private static VehicleOdometerImportService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, NullLogger<VehicleOdometerImportService>.Instance);

    private static async Task<Vehicle> SeedVehicleAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-100",
            NormalizedAssetNumber = "VEH100",
            SerialNumber = "SERIAL-100",
            NormalizedSerialNumber = "SERIAL100",
            CurrentOdometer = 10_000,
            TrackedDistanceKm = 10_000
        };
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    private static MemoryStream CreateWorkbook(params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("عدادات المركبات");
        sheet.Cell(1, 1).Value = "الرقم التسلسلي";
        sheet.Cell(1, 2).Value = "الكيلومترات الحالية";
        for (var row = 0; row < rows.Length; row++)
        for (var column = 0; column < rows[row].Length; column++)
            sheet.Cell(row + 2, column + 1).Value =
                XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
