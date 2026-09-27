using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleStatusImportServiceTests
{
    [Fact]
    public async Task ValidationReadsInventoryStatusesWithoutSaving()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "419679020", cancellationToken);
        using var workbook = CreateWorkbook([419679020, "تحت مسؤلية الحركة", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", true, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Equal(VehicleOperationalStatus.UnderMovementResponsibility,
            Assert.Single(result.Value.Rows).ImportedStatus);
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
        Assert.Single(dbContext.VehicleOperationalStatusPeriods);
    }

    [Fact]
    public async Task ImportUpdatesStatusesAndClosesPreviousPeriods()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var accidentVehicle = await SeedVehicleAsync(dbContext, "ACC-1", cancellationToken);
        var decommissionedVehicle = await SeedVehicleAsync(dbContext, "SCRAP-1", cancellationToken);
        using var workbook = CreateWorkbook(["ACC-1", "حادث", "كبوت ورفرف"], ["SCRAP-1", "تالف", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Updated);
        Assert.Equal(2, result.Value.ChangedVehicles);
        Assert.Equal(VehicleOperationalStatus.AccidentHold, accidentVehicle.CurrentOperationalStatus);
        Assert.Equal(VehicleOperationalStatus.Decommissioned, decommissionedVehicle.CurrentOperationalStatus);
        Assert.NotNull(decommissionedVehicle.DecommissionedAtUtc);
        Assert.Contains("كبوت ورفرف", dbContext.VehicleOperationalStatusPeriods
            .Single(period => period.VehicleId == accidentVehicle.Id && period.EffectiveToUtc == null).Reason);
        Assert.All(dbContext.VehicleOperationalStatusPeriods
            .Where(period => period.Status == VehicleOperationalStatus.Available),
            period => Assert.NotNull(period.EffectiveToUtc));
    }

    [Fact]
    public async Task InvalidStatusBlocksAllUpdates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "READY-1", cancellationToken);
        using var workbook = CreateWorkbook(["READY-1", "صيانه", null], ["UNKNOWN", "unexpected", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.False(result.Value.Updated);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "status");
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task ActiveAssignmentBlocksStatusChangeWithoutEndingRiderHistory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "ASSIGNED-1", cancellationToken);
        var assignment = new RiderVehicleAssignment { VehicleId = vehicle.Id, StartedAtUtc = DateTimeOffset.UtcNow };
        dbContext.RiderVehicleAssignments.Add(assignment);
        vehicle.CurrentAssignmentId = assignment.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = CreateWorkbook(["ASSIGNED-1", "تالف", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.Null(assignment.EndedAtUtc);
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task DuplicateNormalizedSerialBlocksAllUpdates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "SER-1", cancellationToken);
        using var workbook = CreateWorkbook(["SER-1", "صيانه", null], ["SER 1", "حادث", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "serialNumber");
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task BlockingIssuePreventsReadyStatus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "BROKEN-1", cancellationToken);
        vehicle.CurrentOperationalStatus = VehicleOperationalStatus.OutOfService;
        dbContext.VehicleIssues.Add(new VehicleIssue
        {
            VehicleId = vehicle.Id,
            IssueNumber = "ISS-1",
            BlocksOperation = true,
            Status = VehicleIssueStatus.Open,
            ReportedByUserId = TestCurrentUser.Actor
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        using var workbook = CreateWorkbook(["BROKEN-1", "ready", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.False(result.Value!.CanUpdate);
        Assert.Contains(result.Value.Issues, issue => issue.Field == "status");
        Assert.Equal(VehicleOperationalStatus.OutOfService, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task ReimportOfSameStatusDoesNotCreateAnotherHistoryPeriod()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "READY-1", cancellationToken);
        using var workbook = CreateWorkbook(["READY-1", "ready", null]);

        var result = await CreateService(dbContext).ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(0, result.Value!.ChangedVehicles);
        Assert.Equal(1, result.Value.UnchangedVehicles);
        Assert.Single(dbContext.VehicleOperationalStatusPeriods);
        Assert.Equal(VehicleOperationalStatus.Available, vehicle.CurrentOperationalStatus);
    }

    [Fact]
    public async Task AnonymousImportUsesSystemActorForStatusHistory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateContext();
        var vehicle = await SeedVehicleAsync(dbContext, "ANON-1", cancellationToken);
        using var workbook = CreateWorkbook(["ANON-1", "تحت مسؤليه الحركة", null]);

        var result = await CreateService(dbContext, new TestCurrentUser(null))
            .ImportAsync(workbook, "inventory.xlsx", false, cancellationToken);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.True(result.Value!.Updated);
        Assert.Equal(VehicleOperationalStatus.UnderMovementResponsibility,
            vehicle.CurrentOperationalStatus);
        Assert.Equal(Guid.Parse("019c18d5-62e1-7000-d000-000000000003"),
            dbContext.VehicleOperationalStatusPeriods.Single(period =>
                period.VehicleId == vehicle.Id && period.EffectiveToUtc == null).ChangedByUserId);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"VehicleStatusImport_{Guid.NewGuid():N}",
                options => options.EnableNullChecks(false))
            .Options,
        TimeProvider.System);

    private static VehicleStatusImportService CreateService(
        ApplicationDbContext dbContext, TestCurrentUser? user = null) =>
        new(dbContext, user ?? new TestCurrentUser(TestCurrentUser.Actor), TimeProvider.System,
            NullLogger<VehicleStatusImportService>.Instance);

    private static async Task<Vehicle> SeedVehicleAsync(
        ApplicationDbContext dbContext, string serial, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            AssetNumber = serial,
            NormalizedAssetNumber = FleetServiceSupport.NormalizeIdentifier(serial),
            SerialNumber = serial,
            NormalizedSerialNumber = FleetServiceSupport.NormalizeIdentifier(serial)
        };
        dbContext.Vehicles.Add(vehicle);
        dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod
        {
            VehicleId = vehicle.Id,
            Status = VehicleOperationalStatus.Available,
            EffectiveFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Reason = "Initial status.",
            SourceType = VehicleStatusSourceType.Vehicle,
            ChangedByUserId = TestCurrentUser.Actor
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    private static MemoryStream CreateWorkbook(params object?[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1).Value = "serial";
        sheet.Cell(1, 2).Value = "status";
        sheet.Cell(1, 3).Value = "اعطال السياره";
        for (var row = 0; row < rows.Length; row++)
        for (var column = 0; column < rows[row].Length; column++)
            sheet.Cell(row + 2, column + 1).Value =
                XLCellValue.FromObject(rows[row][column], CultureInfo.InvariantCulture);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public static readonly Guid Actor = Guid.Parse("4e8ea764-64e0-42a2-a89a-1eaf9d1a9912");
        public TestCurrentUser(Guid? userId) => UserId = userId;
        public Guid? UserId { get; }
        public Guid? SessionId => null;
        public long? AuthorizationVersion => null;
        public string? CorrelationId => null;
    }
}
