using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformAccountDayAllocationTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData(0, null, true)]
    [InlineData(2, null, true)]
    [InlineData(7, null, false)]
    [InlineData(2, 4, true)]
    [InlineData(2, 3, false)]
    public async Task UnknownZeroNegativeRemainderAndFinalDeclarationAreDistinguished(int? first, int? last, bool valid)
    {
        await using var db = Context(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12); var account = Guid.NewGuid();
        var a = Assignment(account, date.AddDays(-1), date); var b = Assignment(account, date, null);
        db.AddRange(a, b, Usage(a, date, 1, first), Usage(b, date, 2, last)); await db.SaveChangesAsync(ct);
        var result = await new PlatformAccountDayAllocationService(db).AllocateAsync(account, date,
            new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct);
        Assert.Equal(valid, result.IsSuccess);
        if (valid) Assert.Equal(6, result.Value!.Sum(s => s.Values[PlatformDayMetric.OrdersCount]));
        else { Assert.Contains(account.ToString(), result.Error.Description, StringComparison.Ordinal); Assert.NotNull(result.Error.Details); }
    }

    [Fact]
    public async Task CancelledAssignmentIsIgnoredAndReturnedFinalHolderReceivesRemainder()
    {
        await using var db = Context(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12); var account = Guid.NewGuid();
        var a = Assignment(account, date.AddDays(-1), date); var b = Assignment(account, date, date);
        var cancelled = Assignment(account, date, date); cancelled.Status = RiderAssignmentStatus.Cancelled;
        db.AddRange(a, b, cancelled, Usage(a, date, 1, 2), Usage(b, date, 2, null)); await db.SaveChangesAsync(ct);
        var result = await new PlatformAccountDayAllocationService(db).AllocateAsync(account, date,
            new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct);
        Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(2, result.Value!.Count);
        Assert.Equal(b.Id, result.Value[1].AssignmentId); Assert.Equal(4, result.Value[1].Values[PlatformDayMetric.OrdersCount]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AmbiguousLegacyOrderAndGenuineOverlappingPeriodsAreRejected(bool overlap)
    {
        await using var db = Context(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12); var account = Guid.NewGuid();
        var a = Assignment(account, overlap ? date.AddDays(-1) : date, date); var b = Assignment(account, overlap ? date.AddDays(-1) : date, null);
        db.AddRange(a, b); await db.SaveChangesAsync(ct);
        var result = await new PlatformAccountDayAllocationService(db).AllocateAsync(account, date,
            new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RiyadhMidnightSessionBoundaryExcludesPreviousDayHolder()
    {
        await using var db = Context(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12); var account = Guid.NewGuid();
        var a = Assignment(account, date.AddDays(-1), date); var b = Assignment(account, date, null);
        var midnight = new DateTimeOffset(2026, 10, 11, 21, 0, 0, TimeSpan.Zero);
        db.AddRange(a, b, new JahezAccountSession { PlatformRiderAccountId = account, RiderClientAssignmentId = a.Id,
            StartedAtUtc = midnight.AddDays(-1), EndedAtUtc = midnight }, new JahezAccountSession { PlatformRiderAccountId = account,
            RiderClientAssignmentId = b.Id, StartedAtUtc = midnight }); await db.SaveChangesAsync(ct);
        var result = await new PlatformAccountDayAllocationService(db).AllocateAsync(account, date,
            new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct);
        Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(b.Id, Assert.Single(result.Value!).AssignmentId);
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase($"day-allocation-{Guid.NewGuid():N}", m => m.EnableNullChecks(false)).Options);
    private static RiderClientAssignment Assignment(Guid account, DateOnly start, DateOnly? end) => new()
    { PlatformRiderAccountId = account, RiderProfileId = Guid.NewGuid(), EffectiveFrom = start, EffectiveTo = end,
        Status = end.HasValue ? RiderAssignmentStatus.Ended : RiderAssignmentStatus.Active };
    private static PlatformAccountDayUsage Usage(RiderClientAssignment a, DateOnly date, int sequence, int? orders) => new()
    { PlatformRiderAccountId = a.PlatformRiderAccountId, RiderClientAssignmentId = a.Id, BusinessDate = date, Sequence = sequence,
        OrdersCount = orders, Reason = "Test handover" };
}
