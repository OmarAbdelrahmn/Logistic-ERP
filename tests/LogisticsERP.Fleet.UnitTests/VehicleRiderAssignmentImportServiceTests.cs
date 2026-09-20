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

    [Fact]
    public async Task ImportEndsExistingRiderAssignmentAtNewAssignmentStart()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext("ReplaceAssignment");
        var employee = new Employee
        {
            IqamaNo = "2525914756", FullNameAr = "سائق لديه مركبة",
            IsEmployee = false, Status = EmployeeStatus.Active
        };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var oldVehicle = Vehicle("OLD-100", "OLD");
        oldVehicle.CurrentOperationalStatus = VehicleOperationalStatus.Assigned;
        var newVehicle = Vehicle("NEW-200", "NEW");
        var oldAssignment = new RiderVehicleAssignment
        {
            RiderProfileId = rider.Id,
            VehicleId = oldVehicle.Id,
            OperationId = Guid.CreateVersion7(),
            StartedAtUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(3)),
            StartOdometer = 10,
            StartVehicleCondition = VehicleCondition.Good,
            AssignmentReason = "Old assignment",
            AssignedByUserId = Guid.CreateVersion7()
        };
        oldVehicle.CurrentAssignmentId = oldAssignment.Id;
        dbContext.AddRange(employee, rider, oldVehicle, newVehicle, oldAssignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = Service(dbContext);
        using var stream = Workbook("NEW-200", "2525914756", "9/18/2026");

        var result = await service.ImportAsync(stream, "replacement.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        Assert.Equal(new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.FromHours(3)), oldAssignment.EndedAtUtc);
        Assert.Equal(RiderVehicleAssignmentStatus.Completed, oldAssignment.Status);
        Assert.Null(oldVehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Available, oldVehicle.CurrentOperationalStatus);
        var replacement = await dbContext.RiderVehicleAssignments.SingleAsync(
            assignment => assignment.Id != oldAssignment.Id, cancellationToken);
        Assert.Equal(oldAssignment.Id, replacement.PreviousAssignmentId);
        Assert.Equal(rider.Id, replacement.RiderProfileId);
        Assert.Equal(newVehicle.Id, replacement.VehicleId);
    }

    [Fact]
    public async Task ImportCreatesMinimalRiderProfileForActiveEmployee()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext("VirtualRiderProfile");
        var employee = new Employee
        {
            IqamaNo = "2525914756", FullNameAr = "موظف بدون ملف سائق",
            IsEmployee = true, Status = EmployeeStatus.Active
        };
        var vehicle = Vehicle("EMP-300", "EMP");
        dbContext.AddRange(employee, vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        var service = Service(dbContext);
        using var stream = Workbook("EMP-300", "2525914756", "9/18/2026");

        var result = await service.ImportAsync(stream, "employee-assignment.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Imported);
        var profile = await dbContext.RiderProfiles.SingleAsync(cancellationToken);
        Assert.Equal(employee.Id, profile.EmployeeId);
        Assert.NotNull(profile.OperationalNotes);
        var assignment = await dbContext.RiderVehicleAssignments.SingleAsync(cancellationToken);
        Assert.Equal(profile.Id, assignment.RiderProfileId);
        Assert.True(assignment.IsRealRider);
        Assert.Empty(await dbContext.RealRiders.ToArrayAsync(cancellationToken));
    }

    private static ApplicationDbContext CreateContext(string name) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"{name}_{Guid.NewGuid():N}", options => options.EnableNullChecks(false)).Options,
        TimeProvider.System);

    private static VehicleRiderAssignmentImportService Service(ApplicationDbContext dbContext) =>
        new(dbContext, NullLogger<VehicleRiderAssignmentImportService>.Instance);

    private static Vehicle Vehicle(string serial, string asset) => new()
    {
        AssetNumber = asset,
        NormalizedAssetNumber = asset,
        SerialNumber = serial,
        NormalizedSerialNumber = serial.Replace("-", string.Empty, StringComparison.Ordinal),
        CurrentOperationalStatus = VehicleOperationalStatus.Available,
        CurrentOdometer = 100
    };

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
