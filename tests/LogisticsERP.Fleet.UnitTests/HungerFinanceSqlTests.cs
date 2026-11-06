using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerFinanceTests
{
    private static void RequireSql() => Assert.SkipUnless(
        Environment.GetEnvironmentVariable("LOGISTICS_HUNGER_SQL_TESTS") == "1"
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HUNGER_SQL_TEST_SERVER")),
        "Enable disposable SQL tests with LOGISTICS_HUNGER_SQL_TESTS=1 (optional LOGISTICS_HUNGER_SQL_TEST_CONNECTION).");

    [Fact]
    public async Task HungerSqlConcurrentWalletConfirmCreatesOneAssignmentAndOneFinancialEvent()
    {
        RequireSql(); await using var f = await Fixture.Create(intercept: true, sql: true);
        var ct = TestContext.Current.CancellationToken;
        f.Db.RemoveRange(await f.Db.RiderClientAssignments.ToArrayAsync(ct)); await f.Db.SaveChangesAsync(ct);
        var review = Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f) with { MobileBalance = -100 }, ct));
        await using var first = f.OtherContext(); await using var second = f.OtherContext();
        var results = await Task.WhenAll(f.OtherService(first).ConfirmWalletAsync(review.ReviewId, ct), f.OtherService(second).ConfirmWalletAsync(review.ReviewId, ct));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Description));
        Assert.Equal(results[0].Value, results[1].Value);
        f.Db.ChangeTracker.Clear();
        Assert.Single(await f.Db.RiderClientAssignments.ToArrayAsync(ct));
        var movement = Assert.Single(await f.Db.Set<HungerFinancialEvent>().ToArrayAsync(ct));
        Assert.Equal(100, movement.RiderBalanceChange);
        Assert.Empty(await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct));
    }

    [Fact]
    public async Task HungerSqlConcurrentWithdrawalArrivalPostsOnlyOnce()
    {
        RequireSql(); await using var f = await Fixture.Create(intercept: true, sql: true);
        var ct = TestContext.Current.CancellationToken;
        var pending = Value(await f.Service.PostMoneyAsync(await Withdrawal(f, "withdraw"), ct));
        var request = new HungerWithdrawalAction("arrival", new(2026, 11, 2), "verified arrival");
        await using var first = f.OtherContext(); await using var second = f.OtherContext();
        var results = await Task.WhenAll(f.OtherService(first).ConfirmWithdrawalArrivalAsync(pending.EventId, request, ct),
            f.OtherService(second).ConfirmWithdrawalArrivalAsync(pending.EventId, request, ct));
        Assert.All(results, r => Assert.True(r.IsSuccess, r.Error.Description));
        Assert.Equal(results[0].Value, results[1].Value);
        f.Db.ChangeTracker.Clear();
        Assert.Equal("Arrived", (await f.Db.Set<HungerFinancialEvent>().SingleAsync(ct)).Status);
        var postings = await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().ToArrayAsync(ct);
        Assert.Equal(2, postings.Length); Assert.Equal(0, postings.Sum(p => p.Amount));
        Assert.Equal(40, postings.Single(p => p.SubAccountId == f.Bank.Id).Amount);
    }

    [Fact]
    public async Task HungerSqlConcurrentReceiptConfirmationCannotOverAllocate()
    {
        RequireSql(); await using var f = await Fixture.Create(intercept: true, sql: true);
        var ct = TestContext.Current.CancellationToken;
        var source = await Collected(f);
        var batch = Value(await f.Service.SubmitRemittanceAsync(Batch(f, source.EventId), ct));
        var request = new HungerRemittanceConfirmation(f.Bank.Id, [new(source.EventId, 60)], "receipt-a", new(2026, 11, 2), "receipt proof");
        await using var first = f.OtherContext(); await using var second = f.OtherContext();
        var results = await Task.WhenAll(f.OtherService(first).ConfirmRemittanceAsync(batch.EventId, request, ct),
            f.OtherService(second).ConfirmRemittanceAsync(batch.EventId, request with { IdempotencyKey = "receipt-b" }, ct));
        Assert.Single(results, r => r.IsSuccess); Assert.Single(results, r => r.IsFailure);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(60, (await f.Db.Set<HungerRemittanceLine>().SingleAsync(ct)).ConfirmedAmount);
        Assert.Single(await f.Db.Set<HungerReceiptAllocation>().ToArrayAsync(ct));
        Assert.Equal(0, await f.Db.Set<TreasuryPosting>().IgnoreQueryFilters().SumAsync(p => p.Amount, ct));
        Value(await f.Service.ConfirmRemittanceAsync(batch.EventId, request with { Sources = [new(source.EventId, 40)], IdempotencyKey = "receipt-rest" }, ct));
        Assert.Equal(100, (await f.Db.Set<HungerRemittanceLine>().SingleAsync(ct)).ConfirmedAmount);
    }
}
