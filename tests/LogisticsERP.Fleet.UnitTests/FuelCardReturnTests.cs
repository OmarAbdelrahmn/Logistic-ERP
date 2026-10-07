using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class FuelCardReturnTests
{
    [Fact]
    public async Task ReturnClosesAssignmentWithPersistenceAuditAndRejectsInvalidRequests()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = new CurrentUser();
        var clock = new FixedClock();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FuelCardReturn_{Guid.NewGuid():N}")
            .AddInterceptors(new ApplicationPersistenceInterceptor(user, clock)).Options);
        var employee = new Employee { FullNameAr = "Test rider", Status = EmployeeStatus.Active };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var card = new FuelCard
        {
            Provider = FuelCardProvider.PetroApp,
            IdentifierType = FuelCardIdentifierType.InternalNumber,
            CardNumber = "BW201",
            NormalizedCardNumber = "BW201"
        };
        var assignment = new FuelCardRiderAssignment
        {
            FuelCardId = card.Id,
            RiderProfileId = rider.Id,
            EmployeeId = employee.Id,
            EffectiveFrom = new DateOnly(2026, 10, 1),
            AssignmentReason = "Issued",
            AssignedByUserId = user.UserId!.Value,
            RowVersion = [1, 2, 3, 4]
        };
        db.AddRange(employee, rider, card, assignment);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var service = new FuelCardService(db, user, new PermitAll(), clock);
        var request = new StopFuelCardRiderRequest(new DateOnly(2026, 10, 6),
            " Returned ", Convert.ToBase64String(assignment.RowVersion));

        var stale = await service.StopRiderAsync(card.Id, request with { RowVersion = "AA==" }, ct);
        Assert.Equal(FuelErrors.ConcurrencyConflict.Code, stale.Error.Code);
        var invalidDate = await service.StopRiderAsync(card.Id,
            request with { EffectiveTo = new DateOnly(2026, 9, 30) }, ct);
        Assert.Equal(FuelErrors.InvalidDateRange.Code, invalidDate.Error.Code);
        Assert.Null((await db.FuelCardRiderAssignments.SingleAsync(ct)).EffectiveTo);

        var result = await service.StopRiderAsync(card.Id, request, ct);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(assignment.Id, result.Value!.Id);
        Assert.Equal(request.EffectiveTo, result.Value.EffectiveTo);
        Assert.Equal("Returned", result.Value.EndReason);
        Assert.Equal(user.UserId, result.Value.ClosedByUserId);
        db.ChangeTracker.Clear();
        var saved = await db.FuelCardRiderAssignments.SingleAsync(ct);
        Assert.Equal(clock.GetUtcNow(), saved.ClosedAtUtc);
        Assert.Equal(user.UserId, saved.ClosedByUserId);
        Assert.Equal("Issued", saved.AssignmentReason);
        Assert.Equal(assignment.EffectiveFrom, saved.EffectiveFrom);
        Assert.False(await db.FuelCardRiderAssignments.AnyAsync(x => x.EffectiveTo == null, ct));
        Assert.True(await db.AuditEntries.AnyAsync(x => x.EntityId == assignment.Id && x.Action == "Closed", ct));
        var replay = await service.StopRiderAsync(card.Id, request, ct);
        Assert.Equal(FuelErrors.AssignmentNotFound.Code, replay.Error.Code);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 6, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? SessionId => null;
        public long? AuthorizationVersion => 1;
        public string? CorrelationId => "fuel-return-test";
    }

    private sealed class PermitAll : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey,
            PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
}
