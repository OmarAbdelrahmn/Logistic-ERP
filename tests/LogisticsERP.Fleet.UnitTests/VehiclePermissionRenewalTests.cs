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
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class VehiclePermissionRenewalTests
{
    [Theory]
    [InlineData(2026, 10, 6)]
    [InlineData(2030, 1, 27)]
    [InlineData(2030, 1, 28)]
    public async Task RenewAllowsStartBeforeOnOrAfterExistingExpiry(int year, int month, int day)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(ct);
        var startsOn = new DateOnly(year, month, day);
        var request = fixture.Request() with { PermissionStartsOn = startsOn };

        var result = await fixture.Service.RenewPermissionAsync(fixture.Assignment.Id, request, "renew-test", ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(startsOn, result.Value!.PermissionStartsOn);
        Assert.Equal(startsOn.AddYears(1).AddDays(-1), result.Value.PermissionEndsOn);
        Assert.Equal("NEW-PERMIT", result.Value.PermissionReference);
        Assert.Equal(RiderVehicleAssignmentStatus.Active, fixture.Assignment.Status);
        Assert.Null(fixture.Assignment.EndedAtUtc);
        Assert.Equal(fixture.Assignment.Id, fixture.Vehicle.CurrentAssignmentId);
        Assert.Equal(VehicleOperationalStatus.Assigned, fixture.Vehicle.CurrentOperationalStatus);
        Assert.Equal(RiderVehicleAssignmentEventType.PermissionRenewed,
            (await fixture.Db.RiderVehicleAssignmentEvents.SingleAsync(ct)).EventType);

        var replay = await fixture.Service.RenewPermissionAsync(fixture.Assignment.Id, request, "renew-test", ct);
        Assert.True(replay.IsSuccess, replay.Error.Description);
        Assert.Single(await fixture.Db.RiderVehicleAssignmentEvents.ToArrayAsync(ct));
        Assert.Single(await fixture.Db.FleetCommandReceipts.ToArrayAsync(ct));
    }

    [Theory]
    [InlineData("reference")]
    [InlineData("reason")]
    [InlineData("version")]
    public async Task RenewStillRequiresReferenceReasonAndMatchingAssignmentVersion(string invalidField)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await Fixture.CreateAsync(ct);
        var request = fixture.Request();
        request = invalidField switch
        {
            "reference" => request with { PermissionReference = " " },
            "reason" => request with { Reason = " " },
            _ => request with { RowVersion = "AA==" }
        };

        var result = await fixture.Service.RenewPermissionAsync(fixture.Assignment.Id, request, "invalid-renew", ct);

        Assert.Equal(FleetErrors.InvalidRequest.Code, result.Error.Code);
        Assert.Equal(new DateOnly(2030, 1, 27), fixture.Assignment.PermissionEndsOn);
        Assert.Equal("OLD-PERMIT", fixture.Assignment.PermissionReference);
        Assert.Empty(await fixture.Db.RiderVehicleAssignmentEvents.ToArrayAsync(ct));
        Assert.Empty(await fixture.Db.FleetCommandReceipts.ToArrayAsync(ct));
    }

    private sealed class Fixture(ApplicationDbContext db, IdentityDbContext identity,
        Vehicle vehicle, RiderVehicleAssignment assignment, FleetService service) : IAsyncDisposable
    {
        public ApplicationDbContext Db => db;
        public Vehicle Vehicle => vehicle;
        public RiderVehicleAssignment Assignment => assignment;
        public FleetService Service => service;
        public RenewVehiclePermissionRequest Request() => new(new DateOnly(2026, 10, 6),
            " NEW-PERMIT ", "Early renewal", Convert.ToBase64String(assignment.RowVersion));

        public static async Task<Fixture> CreateAsync(CancellationToken ct)
        {
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"VehicleRenew_{Guid.NewGuid():N}", options => options.EnableNullChecks(false)).Options);
            var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase($"VehicleRenewIdentity_{Guid.NewGuid():N}").Options);
            var employee = new Employee { FullNameAr = "Test rider", Status = EmployeeStatus.Active };
            var rider = new RiderProfile { EmployeeId = employee.Id };
            var vehicle = new Vehicle
            {
                AssetNumber = "RENEW-1", NormalizedAssetNumber = "RENEW1",
                CurrentOperationalStatus = VehicleOperationalStatus.Assigned
            };
            var assignment = new RiderVehicleAssignment
            {
                VehicleId = vehicle.Id, RiderProfileId = rider.Id,
                StartedAtUtc = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
                PermissionStartsOn = new DateOnly(2029, 1, 28), PermissionEndsOn = new DateOnly(2030, 1, 27),
                PermissionReference = "OLD-PERMIT", AssignmentReason = "Issued", RowVersion = [1, 2, 3, 4]
            };
            vehicle.CurrentAssignmentId = assignment.Id;
            db.AddRange(employee, rider, vehicle, assignment);
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
        public string? CorrelationId => "vehicle-renew-test";
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
