using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fleet;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehicleReturnOdometerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5_000)]
    [InlineData(12_000)]
    [InlineData(15_000)]
    [InlineData(17_000)]
    public async Task ReturnRecordsReadingWithoutComparingItToHandoverCurrentOrHistory(long reading)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(ct);
        var result = await fixture.Service.ReturnAsync(fixture.Request(reading), [], "return-test", ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(reading, result.Value!.EndOdometer);
        Assert.Equal(RiderVehicleAssignmentStatus.Completed, fixture.Assignment.Status);
        Assert.Null(fixture.Vehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Available, fixture.Vehicle.CurrentOperationalStatus);
        Assert.Equal(Math.Max(15_000, reading), fixture.Vehicle.CurrentOdometer);
        Assert.Equal(Math.Max(15_000.25m, reading), fixture.Vehicle.TrackedDistanceKm);
        var savedReading = await fixture.Db.VehicleOdometerReadings.SingleAsync(
            x => x.SourceEntityId == fixture.Assignment.Id && x.SourceType == VehicleOdometerSourceType.AssignmentReturn, ct);
        Assert.Equal(reading, savedReading.Reading);
        Assert.True(await fixture.Db.RiderVehicleAssignmentEvents.AnyAsync(
            x => x.EventType == RiderVehicleAssignmentEventType.Returned, ct));

        var replay = await fixture.Service.ReturnAsync(fixture.Request(reading), [], "return-test", ct);
        Assert.True(replay.IsSuccess, replay.Error.Description);
        Assert.Equal(2, await fixture.Db.VehicleOdometerReadings.CountAsync(ct));
    }

    [Theory]
    [InlineData("negative")]
    [InlineData("stale-version")]
    [InlineData("before-handover")]
    public async Task ReturnStillRejectsInvalidReadingVersionAndDate(string invalidInput)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(ct);
        var request = fixture.Request(5_000);
        request = invalidInput switch
        {
            "negative" => request with { EndOdometer = -1 },
            "stale-version" => request with { RowVersion = "AA==" },
            _ => request with { EndedAtUtc = fixture.Assignment.StartedAtUtc.AddSeconds(-1) }
        };
        var result = await fixture.Service.ReturnAsync(request, [], "invalid-return", ct);
        Assert.Equal(FleetErrors.InvalidRequest.Code, result.Error.Code);
        Assert.Null(fixture.Assignment.EndedAtUtc);
        Assert.Equal(fixture.Assignment.Id, fixture.Vehicle.CurrentAssignmentId);
        Assert.Empty(await fixture.Db.FleetCommandReceipts.ToArrayAsync(ct));
        Assert.False(await fixture.Db.VehicleOdometerReadings.AnyAsync(
            x => x.SourceType == VehicleOdometerSourceType.AssignmentReturn, ct));
    }

    private sealed class Fixture(ApplicationDbContext db, IdentityDbContext identity, Vehicle vehicle,
        RiderVehicleAssignment assignment, FleetService service) : IAsyncDisposable
    {
        public ApplicationDbContext Db => db;
        public Vehicle Vehicle => vehicle;
        public RiderVehicleAssignment Assignment => assignment;
        public FleetService Service => service;

        public ReturnVehicleRequest Request(long reading) => new(assignment.Id,
            assignment.StartedAtUtc.AddDays(1), reading, VehicleCondition.Good, 50,
            "Returned", Convert.ToBase64String(assignment.RowVersion));

        public static async Task<Fixture> CreateAsync(CancellationToken ct)
        {
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"VehicleReturn_{Guid.NewGuid():N}", options => options.EnableNullChecks(false))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
            var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"VehicleReturnIdentity_{Guid.NewGuid():N}").Options);
            var employee = new Employee { FullNameAr = "Test rider", Status = EmployeeStatus.Active };
            var rider = new RiderProfile { EmployeeId = employee.Id };
            var vehicle = new Vehicle
            {
                AssetNumber = "RETURN-1", NormalizedAssetNumber = "RETURN1",
                CurrentOdometer = 15_000, TrackedDistanceKm = 15_000.25m,
                CurrentOperationalStatus = VehicleOperationalStatus.Assigned
            };
            var assignment = new RiderVehicleAssignment
            {
                VehicleId = vehicle.Id, RiderProfileId = rider.Id, StartOdometer = 10_000,
                StartedAtUtc = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
                AssignmentReason = "Issued", RowVersion = [1, 2, 3, 4]
            };
            vehicle.CurrentAssignmentId = assignment.Id;
            db.AddRange(employee, rider, vehicle, assignment, new VehicleOdometerReading
            {
                VehicleId = vehicle.Id, Reading = 20_000,
                RecordedAtUtc = assignment.StartedAtUtc.AddDays(-1),
                SourceType = VehicleOdometerSourceType.Manual
            });
            await db.SaveChangesAsync(ct);
            return new Fixture(db, identity, vehicle, assignment,
                new FleetService(db, identity, new FleetServiceSupport(new CurrentUser(), new PermitAll(), TimeProvider.System), new UnusedStorage()));
        }

        public async ValueTask DisposeAsync()
        {
            await db.DisposeAsync();
            await identity.DisposeAsync();
        }
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "vehicle-return-test";
    }
    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class UnusedStorage : IPrivateFileStorage
    {
        public Task<Result<StoredPrivateFile>> StoreAsync(string relativeDirectory, PrivateFileUpload file, long maximumBytes, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<PrivateFileDownload>> OpenReadAsync(string storagePath, string contentType, string downloadFileName, long length, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void DeleteBestEffort(string storagePath) { }
    }
}
