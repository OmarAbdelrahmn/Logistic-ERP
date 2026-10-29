using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace LogisticsERP.Fleet.UnitTests;
public sealed class MonthlyAccountUsageTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    [Fact]
    public async Task MonthBoundaryRetainsOngoingCustodyAndResetsReturnedAccounts()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false)).Options);
        var ongoing = new RiderClientAssignment { PlatformRiderAccountId = Guid.NewGuid(), RiderProfileId = Guid.NewGuid(), EffectiveFrom = new(2026, 10, 1), Status = RiderAssignmentStatus.Active };
        var returned = new RiderClientAssignment { PlatformRiderAccountId = Guid.NewGuid(), RiderProfileId = Guid.NewGuid(), EffectiveFrom = new(2026, 10, 1), EffectiveTo = new(2026, 10, 20), Status = RiderAssignmentStatus.Ended };
        var cancelled = new RiderClientAssignment { PlatformRiderAccountId = Guid.NewGuid(), EffectiveFrom = new(2026, 10, 1), Status = RiderAssignmentStatus.Cancelled };
        var future = new RiderClientAssignment { PlatformRiderAccountId = Guid.NewGuid(), EffectiveFrom = new(2026, 11, 2), Status = RiderAssignmentStatus.Active };
        db.AddRange(ongoing, returned, cancelled, future); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Guid[] ids = [ongoing.PlatformRiderAccountId, returned.PlatformRiderAccountId, cancelled.PlatformRiderAccountId, future.PlatformRiderAccountId, Guid.NewGuid()];
        var october = await new MonthlyAccountUsageService(db, new Clock(new(2026, 10, 31, 20, 59, 59, TimeSpan.Zero))).GetAsync(ids, TestContext.Current.CancellationToken);
        Assert.Equal("2026-10", october.UsageMonth); Assert.Equal(2, october.UsedAccountIds.Count);
        var november = await new MonthlyAccountUsageService(db, new Clock(new(2026, 10, 31, 21, 0, 0, TimeSpan.Zero))).GetAsync(ids, TestContext.Current.CancellationToken);
        Assert.Equal("2026-11", november.UsageMonth); Assert.Equal(ongoing.PlatformRiderAccountId, Assert.Single(november.UsedAccountIds));
    }
    [Fact]
    public async Task JahezSessionEndingExactlyAtMonthStartDoesNotCount()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false)).Options);
        var a = new RiderClientAssignment { PlatformRiderAccountId = Guid.NewGuid(), EffectiveFrom = new(2026, 10, 1), EffectiveTo = new(2026, 11, 1), Status = RiderAssignmentStatus.Ended };
        var midnight = new DateTimeOffset(2026, 10, 31, 21, 0, 0, TimeSpan.Zero);
        db.AddRange(a, new JahezAccountSession { PlatformRiderAccountId = a.PlatformRiderAccountId, RiderClientAssignmentId = a.Id, StartedAtUtc = midnight.AddDays(-1), EndedAtUtc = midnight });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var result = await new MonthlyAccountUsageService(db, new Clock(midnight.AddHours(1))).GetAsync([a.PlatformRiderAccountId], TestContext.Current.CancellationToken);
        Assert.Empty(result.UsedAccountIds);
    }
}
