using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace LogisticsERP.Fleet.UnitTests;
public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task SqlSaveFailureRollsBackPreviousFiguresCredentialsAndNewAssignment()
    {
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER")), "Configure NINJA_SQL_TEST_SERVER for disposable SQL Server integration tests.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", first.Assignment.RowVersion), ct));
        var group = Assert.Single(Value(await new PlatformAccountDayAllocationService(f.Db).Context(f.Account.Id, date, f.OwnerRider.Id, ct)).Contributions);
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER app.RejectTestAssignment ON app.RiderClientAssignments AFTER INSERT AS BEGIN IF EXISTS (SELECT 1 FROM inserted WHERE StartReason = 'Reject test assignment') THROW 51000, 'Injected save failure', 1; END", ct);
        var result = await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0502222222", Reason: "Reject test assignment", RiderType: "InternalCar", PaymentModel: "PayPerOrder",
            PreviousDayUsages: [new(group.AssignmentIds, new(2), group.RowVersion, group.UsageRowVersions)]), ct);
        Assert.True(result.IsFailure); f.Db.ChangeTracker.Clear();
        Assert.Single(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        Assert.Null((await f.Db.PlatformAccountDayUsages.SingleAsync(u => u.RiderClientAssignmentId == first.Assignment.Id, ct)).OrdersCount);
        Assert.Equal("0501111111", (await f.Db.NinjaAccountDetails.SingleAsync(ct)).PhoneNumber);
    }
    [Fact]
    public async Task SqlConcurrentAssignmentsCommitExactlyOnePreviousContribution()
    {
        Assert.SkipUnless(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER")), "Configure NINJA_SQL_TEST_SERVER for disposable SQL Server integration tests.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", first.Assignment.RowVersion), ct));
        var group = Assert.Single(Value(await new PlatformAccountDayAllocationService(f.Db).Context(f.Account.Id, date, f.OwnerRider.Id, ct)).Contributions);
        var request = new NinjaAssignRequest(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder",
            PreviousDayUsages: [new(group.AssignmentIds, new(2), group.RowVersion, group.UsageRowVersions)]);
        await using var otherDb = f.OtherContext();
        var other = f.CreateService(otherDb);
        var results = await Task.WhenAll(f.Service.AssignAsync(f.Account.Id, request, ct), other.AssignAsync(f.Account.Id, request, ct));
        Assert.Single(results, r => r.IsSuccess); Assert.Single(results, r => r.IsFailure);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(2, await f.Db.RiderClientAssignments.CountAsync(ct));
        Assert.Equal(2, (await f.Db.PlatformAccountDayUsages.SingleAsync(u => u.RiderClientAssignmentId == first.Assignment.Id, ct)).OrdersCount);
    }
    [Fact]
    public async Task DifferentRiderRequiresOrdersAndFailureDoesNotChangePhoneOrAssignment()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", first.Assignment.RowVersion), ct));
        var failed = await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct);
        Assert.True(failed.IsFailure);
        Assert.Single(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        Assert.Equal("0501111111", (await f.Db.NinjaAccountDetails.SingleAsync(ct)).PhoneNumber);
        Assert.Null((await f.Db.PlatformAccountDayUsages.FirstAsync(ct)).OrdersCount);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task AssignmentSavesPreviousOrdersAtomicallyAndOtherMetricsRemainOptional(int orders)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", first.Assignment.RowVersion), ct));
        var context = Value(await new PlatformAccountDayUsageService(f.Db, f.User, f.Checker, f.Clock).AssignmentContextAsync(f.Account.Id, date, f.OwnerRider.Id, ct));
        var group = Assert.Single(context.Contributions);
        var input = new PreviousDayUsageInput(group.AssignmentIds, new(orders), group.RowVersion, group.UsageRowVersions);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder", PreviousDayUsages: [input]), ct));
        Assert.Equal(orders, (await f.Db.PlatformAccountDayUsages.SingleAsync(u => u.RiderClientAssignmentId == first.Assignment.Id, ct)).OrdersCount);
        var allocator = new PlatformAccountDayAllocationService(f.Db);
        var allocation = Value(await allocator.AllocateAsync(f.Account.Id, date, new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 6 }, ct));
        Assert.Equal(6, allocation.Sum(a => a.Values[PlatformDayMetric.OrdersCount]));
        Assert.True((await allocator.AllocateAsync(f.Account.Id, date, new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.WorkingHours] = 10 }, ct)).IsFailure);
    }
    [Fact]
    public async Task ConsecutiveSameRiderNeedsNoSplitAndLaterTransferUsesCombinedContribution()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", first.Assignment.RowVersion), ct));
        var second = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        var allocator = new PlatformAccountDayAllocationService(f.Db);
        var full = Value(await allocator.AllocateAsync(f.Account.Id, date, new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 9 }, ct));
        Assert.Equal(second.Assignment.Id, Assert.Single(full).AssignmentId);
        Assert.Equal(2, full[0].CoveredAssignmentIds.Count);
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Return", second.Assignment.RowVersion), ct));
        var group = Assert.Single(Value(await allocator.Context(f.Account.Id, date, f.OwnerRider.Id, ct)).Contributions);
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder",
            PreviousDayUsages: [new(group.AssignmentIds, new(5), group.RowVersion, group.UsageRowVersions)]), ct));
        var split = Value(await allocator.AllocateAsync(f.Account.Id, date, new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = 9 }, ct));
        Assert.Equal(2, split.Count); Assert.Equal(5, split[0].Values[PlatformDayMetric.OrdersCount]); Assert.Equal(4, split[1].Values[PlatformDayMetric.OrdersCount]);
        var usageService = new PlatformAccountDayUsageService(f.Db, f.User, f.Checker, f.Clock);
        var day = Value(await usageService.GetAsync(f.Account.Id, date, ct));
        var declaration = day.Contributions[0];
        var completed = Value(await usageService.UpdateAsync(f.Account.Id, date,
            new(day.Assignments.Select(a => new PlatformAccountDayUsageUpdate(a.AssignmentId, null, a.RowVersion)).ToArray(), "Complete hours",
                [new(declaration.AssignmentIds, new(5, 3), declaration.RowVersion, declaration.UsageRowVersions)]), ct));
        Assert.Empty(Value(await f.Service.ImportAsync(false, date, "grouped.xlsx", Sheet(false, "272578", 9, 8), ct)).Issues);
        var frozen = completed.Contributions[0];
        Assert.True((await usageService.UpdateAsync(f.Account.Id, date,
            new(completed.Assignments.Select(a => new PlatformAccountDayUsageUpdate(a.AssignmentId, null, a.RowVersion)).ToArray(), "Cannot rewrite imported group",
                [new(frozen.AssignmentIds, new(6, 3), frozen.RowVersion, frozen.UsageRowVersions)]), ct)).IsFailure);
        Assert.Equal(9, await f.Db.NinjaDailyPerformance.SumAsync(p => p.TotalOrders, ct));

    }
}
