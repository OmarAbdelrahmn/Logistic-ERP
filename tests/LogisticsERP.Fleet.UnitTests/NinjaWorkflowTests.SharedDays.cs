using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    private static readonly int[] SharedOrderSplit = [2, 4];
    private static readonly decimal[] SharedHourSplit = [3m, 7m];
    private static readonly int[] SharedZeroSplit = [0, 6];

    [Fact]
    public async Task NewlyConfiguredPlatformUsesSharedLifecycleAndAllocatorWithoutPlatformSpecificCode()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        (await f.Db.ClientPlatforms.SingleAsync(ct)).Code = "NEW-PLATFORM"; await f.Db.SaveChangesAsync(ct);
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Simple.AssignAccountAsync(f.Account.Id, new(f.Rider.Id, date, "Start", false, null), ct));
        Value(await f.Simple.ReleaseAccountAsync(f.Account.Id, new(date, "Ended", "Return", first.RowVersion, new(2)), ct));
        var last = Value(await f.Simple.AssignAccountAsync(f.Account.Id, new(f.OwnerRider.Id, date, "Next", false, null), ct));
        var allocator = new PlatformAccountDayAllocationService(f.Db);
        var shares = Value(await allocator.AllocateAsync(f.Account.Id, date,
            new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct));
        Assert.Equal(2, shares.Single(s => s.AssignmentId == first.Id).Values[PlatformDayMetric.OrdersCount]);
        Assert.Equal(4, shares.Single(s => s.AssignmentId == last.Id).Values[PlatformDayMetric.OrdersCount]);
    }

    [Fact]
    public async Task SupplementaryApiResolvesLegacyOrderAndRejectsStaleUsageTokensAndMissingPermissions()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", first.Assignment.RowVersion), ct));
        var last = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0503333333"), ct));
        foreach (var u in await f.Db.PlatformAccountDayUsages.ToArrayAsync(ct)) u.Sequence = null;
        await f.Db.SaveChangesAsync(ct);
        var service = new PlatformAccountDayUsageService(f.Db, f.User, f.Checker, f.Clock);
        var day = Value(await service.GetAsync(f.Account.Id, date, ct));
        Assert.All(day.Assignments, a => Assert.Null(a.Sequence));
        var changes = new PlatformAccountDayUpdateRequest(new[] { first.Assignment.Id, last.Assignment.Id }.Select(id =>
            new PlatformAccountDayUsageUpdate(id, id == first.Assignment.Id ? new(2, 3) : null,
                day.Assignments.Single(a => a.AssignmentId == id).RowVersion)).ToArray(), "Resolve legacy order");
        var updated = Value(await service.UpdateAsync(f.Account.Id, date, changes, ct));
        Assert.Equal(first.Assignment.Id, updated.Assignments[0].AssignmentId);
        Assert.Equal(2, updated.Assignments[0].Figures.OrdersCount);
        // The InMemory fixture generates rowversions only on insert; simulate SQL's update token.
        f.Db.PlatformAccountDayUsages.Single(u => u.RiderClientAssignmentId == first.Assignment.Id).RowVersion = [2];
        await f.Db.SaveChangesAsync(ct);
        Assert.True((await service.UpdateAsync(f.Account.Id, date, changes, ct)).IsFailure);
        f.Checker.Denied = LogisticsERP.Application.Authorization.PermissionKeys.Ninja.AssignmentsDelete;
        Assert.True((await service.UpdateAsync(f.Account.Id, date, changes, ct)).IsFailure);
        f.Checker.Denied = LogisticsERP.Application.Authorization.PermissionKeys.Ninja.Read;
        Assert.True((await service.GetAsync(f.Account.Id, date, ct)).IsFailure);
    }
    [Fact]
    public async Task SharedDayKeepsSourceTotalsAndSplitsPerformanceAndMoneyIndependently()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", first.Assignment.RowVersion,
            DayUsage: new(OrdersCount: 2, WorkingHours: 3)), ct));
        var last = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0503333333"), ct));
        var imported = Value(await f.Service.ImportAsync(false, date, "performance.xlsx", Sheet(false, "272578", 6, 10), ct));
        Assert.Empty(imported.Issues); Assert.Equal(1, imported.ImportedRows);
        var shares = await f.Db.NinjaDailyPerformance.OrderBy(p => p.TotalOrders).ToArrayAsync(ct);
        Assert.Equal(SharedOrderSplit, shares.Select(p => p.TotalOrders));
        Assert.Equal(SharedHourSplit, shares.Select(p => p.TotalWorkingHours));
        Assert.Equal(first.Assignment.Id, shares[0].RiderClientAssignmentId);
        Assert.Equal(last.Assignment.Id, shares[1].RiderClientAssignmentId);
        Assert.Equal(6, Assert.Single(f.Db.NinjaDailyPerformanceSources).TotalOrders);
        Assert.All(shares, p => Assert.Equal(Assert.Single(f.Db.NinjaDailyPerformanceSources).Id, p.SourceId));

        var unresolved = Value(await f.Service.ImportAsync(true, date, "money.xlsx", Sheet(true, "272578", 100, -20), ct));
        Assert.NotEmpty(unresolved.Issues); Assert.Empty(f.Db.NinjaDailyDeductions);
        var usageService = new PlatformAccountDayUsageService(f.Db, f.User, f.Checker, f.Clock);
        var day = Value(await usageService.GetAsync(f.Account.Id, date, ct));
        var update = new PlatformAccountDayUpdateRequest(day.Assignments.Select(p => new PlatformAccountDayUsageUpdate(p.AssignmentId,
            p.AssignmentId == first.Assignment.Id ? p.Figures with { CollectedAmount = 30, DeductionsAmount = -5 } : p.Figures,
            p.RowVersion)).ToArray(), "Complete financial totals");
        Value(await usageService.UpdateAsync(f.Account.Id, date, update, ct));
        var money = Value(await f.Service.ImportAsync(true, date, "money.xlsx", Sheet(true, "272578", 100, -20), ct));
        Assert.Empty(money.Issues);
        Assert.Equal(100, f.Db.NinjaDailyDeductions.Sum(p => p.TotalCollectedAmountAtDelivery));
        Assert.Equal(-20, f.Db.NinjaDailyDeductions.Sum(p => p.TotalDeductions));
        Assert.Equal(-5, f.Db.NinjaDailyDeductions.Single(p => p.RiderClientAssignmentId == first.Assignment.Id).TotalDeductions);
        var summary = Value(await f.Service.GetPerformanceReportAsync(date, date, null, null, ct));
        Assert.Equal(6, summary.Metrics.TotalOrders);
        Assert.Equal(1, summary.Metrics.PerformanceRecords);
        Assert.Equal(1, summary.Metrics.DeductionRecords);
        // Published performance cannot be changed through the supplementary endpoint.
        day = Value(await usageService.GetAsync(f.Account.Id, date, ct));
        var conflict = await usageService.UpdateAsync(f.Account.Id, date, new(day.Assignments.Select(p =>
            new PlatformAccountDayUsageUpdate(p.AssignmentId, p.Figures with { OrdersCount = 0 }, p.RowVersion)).ToArray(), "Change orders"), ct);
        Assert.True(conflict.IsFailure);
    }

    [Fact]
    public async Task MissingCountsPermitReturnAndExplicitZeroPermitsImport()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", first.Assignment.RowVersion), ct));
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0503333333"), ct));
        Assert.Null(f.Db.PlatformAccountDayUsages.Single(u => u.RiderClientAssignmentId == first.Assignment.Id).OrdersCount);
        Assert.NotEmpty(Value(await f.Service.ImportAsync(false, date, "x.xlsx", Sheet(false, "272578", 6, 10), ct)).Issues);
        var usage = f.Db.PlatformAccountDayUsages.Single(u => u.RiderClientAssignmentId == first.Assignment.Id);
        usage.OrdersCount = 0; usage.WorkingHours = 0; await f.Db.SaveChangesAsync(ct);
        Assert.Empty(Value(await f.Service.ImportAsync(false, date, "x.xlsx", Sheet(false, "272578", 6, 10), ct)).Issues);
        Assert.Equal(SharedZeroSplit, f.Db.NinjaDailyPerformance.OrderBy(p => p.TotalOrders).Select(p => p.TotalOrders));
    }

    [Fact]
    public async Task ThreeHoldersIncludingRepeatedRiderReceiveTwoThreeAndFourOrders()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", first.Assignment.RowVersion, DayUsage: new(2, 1)), ct));
        var second = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0503333333"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0502222222", null, "Return", second.Assignment.RowVersion, DayUsage: new(3, 2)), ct));
        var third = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0503333333"), ct));
        Assert.Empty(Value(await f.Service.ImportAsync(false, date, "x.xlsx", Sheet(false, "272578", 9, 8), ct)).Issues);
        Assert.Equal(2, f.Db.NinjaDailyPerformance.Single(p => p.RiderClientAssignmentId == first.Assignment.Id).TotalOrders);
        Assert.Equal(3, f.Db.NinjaDailyPerformance.Single(p => p.RiderClientAssignmentId == second.Assignment.Id).TotalOrders);
        Assert.Equal(4, f.Db.NinjaDailyPerformance.Single(p => p.RiderClientAssignmentId == third.Assignment.Id).TotalOrders);
        Assert.Equal(3, f.Db.PlatformAccountDayUsages.Select(u => u.Sequence).Distinct().Count());
    }
}
