using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class AccidentCreationVehicleStateTests
{
    [Theory]
    [InlineData(false, VehicleOperationalStatus.Assigned)]
    [InlineData(true, VehicleOperationalStatus.Assigned)]
    [InlineData(false, VehicleOperationalStatus.OutOfService)]
    [InlineData(true, VehicleOperationalStatus.OutOfService)]
    public async Task CreatingAccidentPreservesVehicleStatusAndAssignment(
        bool isDrivable, VehicleOperationalStatus status)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AccidentCreation_{Guid.NewGuid():N}", x => x.EnableNullChecks(false))
            .Options);
        var user = new TestUser();
        var now = DateTimeOffset.UtcNow;
        var employee = new Employee { FullNameAr = "Test rider" };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var vehicle = new Vehicle
        {
            AssetNumber = "ACC-VEHICLE", NormalizedAssetNumber = "ACCVEHICLE",
            CurrentOperationalStatus = status
        };
        var assignment = new RiderVehicleAssignment
        {
            VehicleId = vehicle.Id, RiderProfileId = rider.Id, StartedAtUtc = now.AddDays(-1)
        };
        vehicle.CurrentAssignmentId = assignment.Id;
        var period = new VehicleOperationalStatusPeriod
        {
            VehicleId = vehicle.Id, Status = status, EffectiveFromUtc = now.AddDays(-1),
            ChangedByUserId = user.UserId!.Value, SourceType = VehicleStatusSourceType.Administrative
        };
        db.AddRange(employee, rider, vehicle, assignment, period);
        await db.SaveChangesAsync(ct);
        var notifications = new TestNotifications();
        var service = new VehicleAccidentService(db,
            new FleetServiceSupport(user, new PermitAll(), TimeProvider.System),
            new UnusedStorage(), new UnusedPdf(), notifications);
        var request = new CreateVehicleAccidentRequest("ACC-TEST", vehicle.Id, rider.Id, now.AddMinutes(-1),
            null, null, null, "TRAFFIC-TEST", null, VehicleAccidentSeverity.Serious,
            isDrivable, false, null, null, null, null, null);

        var created = await service.CreateAsync(request, "create-test", ct);
        Assert.True(created.IsSuccess, created.Error.Description);
        db.ChangeTracker.Clear();
        var replay = await service.CreateAsync(request, "create-test", ct);

        Assert.True(replay.IsSuccess, replay.Error.Description);
        Assert.Equal(created.Value!.Summary.Id, replay.Value!.Summary.Id);
        var persistedVehicle = await db.Vehicles.SingleAsync(ct);
        Assert.Equal(status, persistedVehicle.CurrentOperationalStatus);
        Assert.Equal(assignment.Id, persistedVehicle.CurrentAssignmentId);
        var persistedAssignment = await db.RiderVehicleAssignments.SingleAsync(ct);
        Assert.Equal(RiderVehicleAssignmentStatus.Active, persistedAssignment.Status);
        Assert.Null(persistedAssignment.EndedAtUtc);
        Assert.Null(persistedAssignment.EndVehicleCondition);
        Assert.Null(persistedAssignment.CompletionReason);
        Assert.Null(persistedAssignment.EndedByUserId);
        var persistedPeriod = await db.VehicleOperationalStatusPeriods.SingleAsync(ct);
        Assert.Equal(period.Id, persistedPeriod.Id);
        Assert.Equal(status, persistedPeriod.Status);
        Assert.Null(persistedPeriod.EffectiveToUtc);
        Assert.Empty(await db.RiderVehicleAssignmentEvents.ToArrayAsync(ct));
        var accident = await db.VehicleAccidents.SingleAsync(ct);
        Assert.Equal(isDrivable, accident.IsDrivable);
        Assert.Equal(assignment.Id, accident.RiderVehicleAssignmentId);
        Assert.Equal(!isDrivable, (await db.VehicleIssues.SingleAsync(ct)).BlocksOperation);
        Assert.Single(await db.VehicleAccidentCases.ToArrayAsync(ct));
        Assert.Single(await db.VehicleAccidentEvents.ToArrayAsync(ct));
        Assert.Single(await db.FleetCommandReceipts.ToArrayAsync(ct));
        Assert.Equal(1, notifications.QueuedCount);
    }

    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.CreateVersion7();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => null;
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }

    private sealed class UnusedStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file,
            long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType,
            string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }

    private sealed class UnusedPdf : IAccidentPdfGenerator
    {
        public byte[] Generate(AccidentPdfSnapshot snapshot) => throw new NotSupportedException();
    }

    private sealed class TestNotifications : IAccidentNotificationService
    {
        public int QueuedCount { get; private set; }
        public Task QueueAsync(Guid accidentId, Guid vehicleId, string accidentNumber, string eventKey,
            string description, CancellationToken cancellationToken = default)
        {
            QueuedCount++;
            return Task.CompletedTask;
        }
        public Task RunDueNotificationsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
