using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class LeaveExtensionTests
{
    [Fact]
    public async Task ActiveVacationCanBeExtendedAfterApproval()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new Fixture();
        var leave = await fixture.SeedLeaveAsync(LeaveWorkflowStatus.Active, 20, ct);

        var requested = await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 15), "Need more time", Version(leave)), ct);

        Assert.True(requested.IsSuccess, requested.Error.Description);
        Assert.Equal(new DateOnly(2026, 10, 1), requested.Value!.RequestedStartDate);
        Assert.Equal(new DateOnly(2026, 10, 10), leave.EndDate);
        var change = await fixture.Db.LeaveDateChangeRequests.SingleAsync(ct);
        change.RowVersion = [2];
        await fixture.Db.SaveChangesAsync(ct);

        var resolved = await fixture.Service.ResolveLeaveDateChangeAsync(leave.Id, change.Id,
            new LeaveChangeResolveRequest(true, "Approved", Version(change)), ct);

        Assert.True(resolved.IsSuccess, resolved.Error.Description);
        Assert.Equal(new DateOnly(2026, 10, 15), leave.EndDate);
        Assert.Equal(new DateOnly(2026, 10, 16), leave.ExpectedReturnDate);
        Assert.Equal(15, leave.CalendarDays);
        Assert.Equal(LeaveChangeRequestStatus.Approved, change.Status);
    }

    [Fact]
    public async Task ExtensionRejectsInactiveStaleAndOverLimitRequests()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new Fixture();
        var leave = await fixture.SeedLeaveAsync(LeaveWorkflowStatus.Approved, 12, ct);
        var request = new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 12), "More time", Version(leave));

        Assert.Equal("hr.conflict", (await fixture.Service.RequestLeaveExtensionAsync(leave.Id, request, ct)).Error.Code);
        leave.Status = LeaveWorkflowStatus.Active;
        Assert.Equal("hr.concurrency_conflict", (await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            request with { RowVersion = Convert.ToBase64String([9]) }, ct)).Error.Code);
        Assert.Equal("hr.invalid_request", (await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            request with { NewEndDate = new DateOnly(2026, 10, 13) }, ct)).Error.Code);
        Assert.Empty(await fixture.Db.LeaveDateChangeRequests.ToArrayAsync(ct));
    }

    [Fact]
    public async Task ApprovalRejectsExtensionIfVacationHasChanged()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new Fixture();
        var leave = await fixture.SeedLeaveAsync(LeaveWorkflowStatus.Active, 20, ct);
        var requested = await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 15), "More time", Version(leave)), ct);
        Assert.True(requested.IsSuccess, requested.Error.Description);
        var change = await fixture.Db.LeaveDateChangeRequests.SingleAsync(ct);
        change.RowVersion = [2];
        leave.Status = LeaveWorkflowStatus.Completed;
        await fixture.Db.SaveChangesAsync(ct);

        var resolved = await fixture.Service.ResolveLeaveDateChangeAsync(leave.Id, change.Id,
            new LeaveChangeResolveRequest(true, "Approved", Version(change)), ct);

        Assert.Equal("hr.conflict", resolved.Error.Code);
        Assert.Equal(new DateOnly(2026, 10, 10), leave.EndDate);
        Assert.Equal(LeaveChangeRequestStatus.Pending, change.Status);
    }

    [Fact]
    public async Task ExtensionRejectsOverlapAndSecondPendingRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new Fixture();
        var leave = await fixture.SeedLeaveAsync(LeaveWorkflowStatus.Active, 30, ct);
        var anotherLeave = new LeaveRequest
        {
            LeaveTypeId = leave.LeaveTypeId,
            EmployeeId = leave.EmployeeId,
            StartDate = new DateOnly(2026, 10, 15),
            EndDate = new DateOnly(2026, 10, 18),
            ExpectedReturnDate = new DateOnly(2026, 10, 19),
            CalendarDays = 4,
            Status = LeaveWorkflowStatus.Approved
        };
        fixture.Db.LeaveRequests.Add(anotherLeave);
        await fixture.Db.SaveChangesAsync(ct);

        var overlapping = await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 16), "Need more time", Version(leave)), ct);
        Assert.Equal("hr.conflict", overlapping.Error.Code);

        var first = await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 14), "Need more time", Version(leave)), ct);
        Assert.True(first.IsSuccess, first.Error.Description);
        var second = await fixture.Service.RequestLeaveExtensionAsync(leave.Id,
            new LeaveExtensionCreateRequest(new DateOnly(2026, 10, 13), "Another request", Version(leave)), ct);
        Assert.Equal("hr.conflict", second.Error.Code);
        Assert.Single(await fixture.Db.LeaveDateChangeRequests.ToArrayAsync(ct));
    }

    private static string Version(LogisticsERP.Domain.Common.AuditableEntity entity) => Convert.ToBase64String(entity.RowVersion);

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false)).Options);
        private readonly IdentityDbContext identityDb = new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public HrWorkflowService Service { get; }

        public Fixture() => Service = new HrWorkflowService(Db, identityDb, new TestUser(), new PermitAll(), TimeProvider.System);

        public async Task<LeaveRequest> SeedLeaveAsync(LeaveWorkflowStatus status, int maximumDays, CancellationToken ct)
        {
            var leaveType = new LeaveType { Code = "ANNUAL", NameAr = "إجازة", NameEn = "Annual", MaximumCalendarDays = maximumDays };
            var leave = new LeaveRequest
            {
                LeaveTypeId = leaveType.Id,
                EmployeeId = Guid.NewGuid(),
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2026, 10, 10),
                ExpectedReturnDate = new DateOnly(2026, 10, 11),
                CalendarDays = 10,
                Status = status,
                RowVersion = [1]
            };
            Db.AddRange(leaveType, leave);
            await Db.SaveChangesAsync(ct);
            return leave;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await identityDb.DisposeAsync();
        }
    }

    private sealed class TestUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
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
}
