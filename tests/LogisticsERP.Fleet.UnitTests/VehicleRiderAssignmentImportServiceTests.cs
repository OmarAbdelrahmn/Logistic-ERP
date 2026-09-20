using ClosedXML.Excel;
using System.Globalization;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRiderAssignmentImportServiceTests
{
    [Fact]
    public async Task ImportLinksVehicleToRiderByTammIqamaWithoutRealRiderOverride()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"VehicleRiderAssignmentImport_{Guid.NewGuid():N}",
                    options => options.EnableNullChecks(false)).Options,
            TimeProvider.System);
        var employee = new Employee
        {
            IqamaNo = "2525914756", FullNameAr = "السائق التجريبي",
            IsEmployee = false, Status = EmployeeStatus.Active
        };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-1", NormalizedAssetNumber = "VEH1",
            SerialNumber = "102604220", NormalizedSerialNumber = "102604220",
            CurrentOperationalStatus = VehicleOperationalStatus.Available,
            CurrentOdometer = 123
        };
        dbContext.AddRange(employee, rider, vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = new VehicleRiderAssignmentImportService(
            dbContext, NullLogger<VehicleRiderAssignmentImportService>.Instance);
        using var stream = Workbook("102604220", "2525914756", new DateTime(2026, 9, 18));

        var result = await service.ImportAsync(stream, "tamm.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        var assignment = await dbContext.RiderVehicleAssignments.SingleAsync(cancellationToken);
        Assert.Equal(rider.Id, assignment.RiderProfileId);
        Assert.Equal(vehicle.Id, assignment.VehicleId);
        Assert.True(assignment.IsRealRider);
        Assert.Equal(new DateOnly(2026, 9, 18), assignment.PermissionStartsOn);
        Assert.Empty(await dbContext.RealRiders.ToArrayAsync(cancellationToken));
        Assert.Equal(assignment.Id, vehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Assigned, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public void ParserAcceptsTheExactArabicHeadersAndSlashDate()
    {
        using var stream = Workbook("549694220", "2618245001", "9/18/2026");

        var parsed = VehicleRiderAssignmentSpreadsheetParser.Parse(stream);

        var row = Assert.Single(parsed.Rows);
        Assert.Equal("549694220", row.SerialNumber);
        Assert.Equal("2618245001", row.IqamaNo);
        Assert.Equal(new DateOnly(2026, 9, 18), row.PermissionStartsOn);
    }

    [Fact]
    public async Task ImportCreatesValidAssignmentsAndReportsInvalidRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"PartialVehicleRiderAssignmentImport_{Guid.NewGuid():N}",
                    options => options.EnableNullChecks(false)).Options,
            TimeProvider.System);
        var employee = new Employee
        {
            IqamaNo = "2525914756", FullNameAr = "السائق الصحيح",
            IsEmployee = false, Status = EmployeeStatus.Active
        };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-VALID", NormalizedAssetNumber = "VEHVALID",
            SerialNumber = "102604220", NormalizedSerialNumber = "102604220",
            CurrentOperationalStatus = VehicleOperationalStatus.Available
        };
        dbContext.AddRange(employee, rider, vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = new VehicleRiderAssignmentImportService(
            dbContext, NullLogger<VehicleRiderAssignmentImportService>.Instance);
        using var stream = Workbook(
            ["102604220", "2525914756", "9/18/2026"],
            ["UNKNOWN", "2618245001", "9/18/2026"]);

        var result = await service.ImportAsync(stream, "mixed-tamm.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanImport);
        Assert.True(result.Value.Imported);
        Assert.Equal(2, result.Value.TotalRows);
        Assert.Equal(1, result.Value.ValidRows);
        Assert.Equal(1, result.Value.CreatedAssignments);
        Assert.Single(result.Value.Issues, issue => issue.RowNumber == 3 && issue.Severity == "Error");
        Assert.Single(await dbContext.RiderVehicleAssignments.ToArrayAsync(cancellationToken));
    }

    private static MemoryStream Workbook(string serial, string iqama, object date)
        => Workbook([serial, iqama, date]);

    private static MemoryStream Workbook(params object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("التفويضات");
        sheet.Cell(1, 1).Value = "الرقم التسلسلي";
        sheet.Cell(1, 2).Value = "هوية المفوض في تم";
        sheet.Cell(1, 3).Value = "تاريخ بداية التفويض";
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
                sheet.Cell(row + 2, column + 1).Value =
                    XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
        }
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
