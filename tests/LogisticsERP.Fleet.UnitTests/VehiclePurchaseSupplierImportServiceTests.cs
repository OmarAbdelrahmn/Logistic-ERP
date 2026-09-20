using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehiclePurchaseSupplierImportServiceTests
{
    [Fact]
    public async Task ValidationMatchesVehicleAndSupplierWithoutWriting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var (vehicle, supplier) = await SeedAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook([vehicle.SerialNumber!, supplier.CommercialRegistrationNumber!]);

        var result = await service.ImportAsync(
            workbook,
            "purchase-suppliers.xlsx",
            validateOnly: true,
            cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Equal(1, result.Value.MatchedVehicles);
        Assert.Equal(1, result.Value.ChangedVehicles);
        Assert.Equal(supplier.Id, result.Value.Rows[0].SupplierId);
        Assert.Null(vehicle.PurchasedFromSupplierId);
    }

    [Fact]
    public async Task ImportSetsPurchaseSupplierOnOwnedVehicle()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var (vehicle, supplier) = await SeedAsync(dbContext, cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook([vehicle.SerialNumber!, supplier.CommercialRegistrationNumber!]);

        var result = await service.ImportAsync(
            workbook,
            "purchase-suppliers.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanUpdate);
        Assert.True(result.Value.Updated);
        Assert.Equal(1, result.Value.ChangedVehicles);
        Assert.Equal(supplier.Id, vehicle.PurchasedFromSupplierId);
        Assert.True(FleetBusinessRules.IsCoreIdentityReady(vehicle));
    }

    [Fact]
    public async Task UnknownSupplierBlocksEveryUpdate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var (firstVehicle, supplier) = await SeedAsync(dbContext, cancellationToken);
        var secondVehicle = CreateVehicle("SERIAL-200");
        dbContext.Vehicles.Add(secondVehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = CreateService(dbContext);
        using var workbook = CreateWorkbook(
            [firstVehicle.SerialNumber!, supplier.CommercialRegistrationNumber!],
            [secondVehicle.SerialNumber!, "9999999999"]);

        var result = await service.ImportAsync(
            workbook,
            "invalid-purchase-supplier.xlsx",
            validateOnly: false,
            cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Contains(result.Value.Issues, issue =>
            issue.Field == "supplierRegistryNumber" && issue.Severity == "Error");
        Assert.Null(firstVehicle.PurchasedFromSupplierId);
        Assert.Null(secondVehicle.PurchasedFromSupplierId);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehiclePurchaseSupplierImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false))
            .Options,
        TimeProvider.System);

    private static VehiclePurchaseSupplierImportService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, NullLogger<VehiclePurchaseSupplierImportService>.Instance);

    private static async Task<(Vehicle Vehicle, VehicleSupplier Supplier)> SeedAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var vehicle = CreateVehicle("SERIAL-100");
        var supplier = new VehicleSupplier
        {
            Code = "PURCHASE-SUPPLIER",
            NameAr = "مورد الشراء",
            NameEn = "Purchase Supplier",
            CommercialRegistrationNumber = "1010101010",
            Status = VehicleCatalogStatus.Active
        };
        dbContext.AddRange(vehicle, supplier);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (vehicle, supplier);
    }

    private static Vehicle CreateVehicle(string serialNumber) => new()
    {
        AssetNumber = $"VEH-{serialNumber}",
        NormalizedAssetNumber = $"VEH{serialNumber.Replace("-", string.Empty, StringComparison.Ordinal)}",
        SerialNumber = serialNumber,
        NormalizedSerialNumber = serialNumber.Replace("-", string.Empty, StringComparison.Ordinal),
        ChassisNumber = $"CHASSIS-{serialNumber}",
        NormalizedChassisNumber = $"CHASSIS{serialNumber.Replace("-", string.Empty, StringComparison.Ordinal)}",
        PlateNumberAr = "ا ب ج 1234",
        PlateNumberEn = "A B J 1234",
        SponsorId = Guid.CreateVersion7(),
        OperatingCityId = Guid.CreateVersion7(),
        RegistrationType = VehicleRegistrationType.Private,
        VehicleManufacturerId = Guid.CreateVersion7(),
        VehicleModelId = Guid.CreateVersion7(),
        VehicleType = VehicleType.Car,
        FuelType = VehicleFuelType.Petrol,
        TransmissionType = VehicleTransmissionType.Automatic,
        OwnershipType = VehicleOwnershipType.Owned,
        CurrentOperationalStatus = VehicleOperationalStatus.Available
    };

    private static MemoryStream CreateWorkbook(params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("موردي الشراء");
        sheet.Cell(1, 1).Value = "الرقم التسلسلي";
        sheet.Cell(1, 2).Value = "رقم السجل التجاري للمورد";
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 2, column + 1).Value =
                    XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
