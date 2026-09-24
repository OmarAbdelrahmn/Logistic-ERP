using ClosedXML.Excel;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleRiderHistoryImportServiceTests
{
    [Fact]
    public async Task ImportCreatesCompletedHistoryWithoutChangingCurrentAssignment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = Context();
        var employee = new Employee { IqamaNo = "2627814839", FullNameAr = "سائق" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-1", NormalizedAssetNumber = "VEH1",
            SerialNumber = "856594220", NormalizedSerialNumber = "856594220",
            CurrentOperationalStatus = VehicleOperationalStatus.Assigned,
            CurrentOdometer = 999
        };
        var current = new RiderVehicleAssignment
        {
            RiderProfileId = rider.Id, VehicleId = vehicle.Id, OperationId = Guid.CreateVersion7(),
            StartedAtUtc = new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.FromHours(3)),
            AssignmentReason = "Current assignment", AssignedByUserId = Guid.CreateVersion7()
        };
        vehicle.CurrentAssignmentId = current.Id;
        db.AddRange(employee, rider, vehicle, current);
        await db.SaveChangesAsync(cancellationToken);
        using var workbook = Workbook(["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-09-10"]);
        var service = Service(db);

        var result = await service.ImportAsync(workbook, "history.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(1, result.Value!.CreatedAssignments);
        var history = await db.RiderVehicleAssignments.SingleAsync(x => x.Id != current.Id, cancellationToken);
        Assert.Equal(RiderVehicleAssignmentStatus.Completed, history.Status);
        Assert.Equal("101447007921467", history.PermissionReference);
        Assert.Equal(new DateTimeOffset(2026, 2, 14, 0, 0, 0, TimeSpan.FromHours(3)), history.StartedAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 11, 0, 0, 0, TimeSpan.FromHours(3)), history.EndedAtUtc);
        Assert.Equal(new DateOnly(2026, 9, 10), history.PermissionEndsOn);
        Assert.Equal(VehicleCondition.Unknown, history.StartVehicleCondition);
        Assert.Equal(0, history.StartOdometer);
        Assert.Equal(current.Id, vehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Assigned, vehicle.CurrentOperationalStatus);
        Assert.Equal(999, vehicle.CurrentOdometer);
        Assert.Null(current.EndedAtUtc);
        Assert.Empty(await db.VehicleOperationalStatusPeriods.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ValidationDoesNotSaveAndRepeatImportIsIdempotent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = Context();
        var employee = new Employee { IqamaNo = "2627814839", FullNameAr = "سائق" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-1", NormalizedAssetNumber = "VEH1",
            SerialNumber = "856594220", NormalizedSerialNumber = "856594220"
        };
        db.AddRange(employee, rider, vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = Service(db);

        using (var workbook = Workbook(["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-09-10"]))
        {
            var preview = await service.ImportAsync(workbook, "history.xlsx", true, cancellationToken);
            Assert.True(preview.IsSuccess, preview.Error.Description);
            Assert.True(preview.Value!.CanImport);
            Assert.Equal(0, preview.Value.CreatedAssignments);
            Assert.Empty(await db.RiderVehicleAssignments.ToArrayAsync(cancellationToken));
        }
        using (var workbook = Workbook(["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-09-10"]))
            Assert.True((await service.ImportAsync(workbook, "history.xlsx", false, cancellationToken)).IsSuccess);
        using (var workbook = Workbook(["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-09-10"]))
        {
            var repeat = await service.ImportAsync(workbook, "history.xlsx", false, cancellationToken);
            Assert.True(repeat.IsSuccess, repeat.Error.Description);
            Assert.Equal(0, repeat.Value!.CreatedAssignments);
            Assert.Equal(1, repeat.Value.AlreadyImported);
        }
        Assert.Single(await db.RiderVehicleAssignments.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public async Task ImportCreatesOneMinimalRiderProfileForEmployeeWithoutProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = Context();
        var employee = new Employee
        {
            IqamaNo = "2627814839", FullNameAr = "موظف بدون ملف سائق",
            IsEmployee = true, Status = EmployeeStatus.Active
        };
        var vehicle = new Vehicle
        {
            AssetNumber = "VEH-1", NormalizedAssetNumber = "VEH1",
            SerialNumber = "856594220", NormalizedSerialNumber = "856594220",
            CurrentOperationalStatus = VehicleOperationalStatus.Available
        };
        db.AddRange(employee, vehicle);
        await db.SaveChangesAsync(cancellationToken);
        var service = Service(db);

        using (var previewWorkbook = Workbook(
            ["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-03-10"],
            ["856594220", "101447007921468", "2627814839", "2026-03-11", "2026-04-10"]))
        {
            var preview = await service.ImportAsync(previewWorkbook, "history.xlsx", true, cancellationToken);
            Assert.True(preview.IsSuccess, preview.Error.Description);
            Assert.Equal(2, preview.Value!.ValidRows);
            Assert.Single(preview.Value.Rows.Select(row => row.RiderProfileId).Distinct());
            Assert.Empty(await db.RiderProfiles.ToArrayAsync(cancellationToken));
        }

        using var workbook = Workbook(
            ["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-03-10"],
            ["856594220", "101447007921468", "2627814839", "2026-03-11", "2026-04-10"]);
        var result = await service.ImportAsync(workbook, "history.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value!.CreatedAssignments);
        var profile = await db.RiderProfiles.SingleAsync(cancellationToken);
        Assert.Equal(employee.Id, profile.EmployeeId);
        Assert.NotNull(profile.OperationalNotes);
        var assignments = await db.RiderVehicleAssignments.ToArrayAsync(cancellationToken);
        Assert.Equal(2, assignments.Length);
        Assert.All(assignments, assignment =>
        {
            Assert.Equal(profile.Id, assignment.RiderProfileId);
            Assert.Equal(RiderVehicleAssignmentStatus.Completed, assignment.Status);
            Assert.NotNull(assignment.EndedAtUtc);
        });
        Assert.Null(vehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task ImportReportsUnknownRiderAndFutureEndWithoutWriting()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = Context();
        using var workbook = Workbook(
            ["856594220", "101447007921467", "2627814839", "2026-02-14", "2026-09-10"],
            ["856594220", "101447007921468", "2627814839", "2026-09-24", "2026-12-01"]);

        var result = await Service(db).ImportAsync(workbook, "history.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.Imported);
        Assert.Equal(2, result.Value.TotalRows);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "riderIqamaNo");
        Assert.Contains(result.Value.Issues, issue => issue.Field == "endsOn");
        Assert.Empty(await db.RiderVehicleAssignments.ToArrayAsync(cancellationToken));
    }

    [Fact]
    public void ParserAcceptsNumericSerialAndIgnoresBlankFormattedRows()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("ورقة1");
        string[] headers = ["الرقم التسلسلي", "رقم التفويض", "رقم هوية المفوض", "تاريخ بداية التفويض", "تاريخ نهاية التفويض / الإلغاء"];
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
        sheet.Cell(2, 1).Value = 856594220;
        sheet.Cell(2, 2).Value = "101447007921467";
        sheet.Cell(2, 3).Value = 2627814839;
        sheet.Cell(2, 4).Value = "2026-02-14";
        sheet.Cell(2, 5).Value = "2026-09-10";
        sheet.Cell(10, 1).Style.Fill.BackgroundColor = XLColor.White;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var parsed = VehicleRiderHistorySpreadsheetParser.Parse(stream);

        var row = Assert.Single(parsed.Rows);
        Assert.Equal(1, parsed.TotalRows);
        Assert.Equal("856594220", row.SerialNumber);
        Assert.Equal("2627814839", row.IqamaNo);
        Assert.Equal(new DateOnly(2026, 2, 14), row.StartsOn);
        Assert.Equal(new DateOnly(2026, 9, 10), row.EndsOn);
    }

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleRiderHistory_{Guid.NewGuid():N}", options => options.EnableNullChecks(false)).Options,
        TimeProvider.System);

    private static VehicleRiderHistoryImportService Service(ApplicationDbContext db) =>
        new(db, NullLogger<VehicleRiderHistoryImportService>.Instance);

    private static MemoryStream Workbook(params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("ورقة1");
        string[] headers = ["الرقم التسلسلي", "رقم التفويض", "رقم هوية المفوض", "تاريخ بداية التفويض", "تاريخ نهاية التفويض / الإلغاء"];
        for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
        for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < headers.Length; column++)
                sheet.Cell(row + 2, column + 1).Value = rows[row][column];
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
