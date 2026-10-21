using System.Reflection;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    private static async Task<JahezTransferredDebt> TransferForRider(Fixture f, Guid riderId, string key, decimal net = 100)
    {
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account(key);
        var h = Success(await f.Service.HandoverAsync(key + "-start", new(account.Id, riderId, At(1), "استلام"), ct));
        var o = Success(await f.Service.CreateOperationAsync(key + "-draft", Settle(h.Id, net, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        var r = Success(await f.Service.CreateWorkflowRequestAsync(key + "-request", new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: o.Id), ct));
        Assert.Equal(new JahezSettlementAmounts(net, 75, net + 75), r.Amounts);
        f.User.UserId = Guid.NewGuid();
        var approved = Success(await f.Service.DecideWorkflowRequestAsync(key + "-approve", r.Request.Id, new(true, "اعتماد"), ct));
        Assert.Equal(r.Amounts, approved.Amounts);
        return await f.Db.JahezTransferredDebts.SingleAsync(x => x.OperationId == o.Id, ct);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RiderDebtsGroupUnpaidAmountsAndRepayAllAtomically(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        // Test runtime SQL transactions against the current model in a disposable database.
        // Migration compatibility remains covered separately by the default fixture path.
        await using var f = await Fixture.Create(sql, createCurrentSchema: sql);
        var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var first = await TransferForRider(f, f.RiderId, "first");
        var second = await TransferForRider(f, f.RiderId, "second", 200);
        await TransferForRider(f, f.OtherRiderId, "other", 10);
        var summary = Success(await f.Service.RidersWithTransferredDebtsAsync(1, 50, ct)).Items.Single(x => x.RiderProfileId == f.RiderId);
        Assert.Equal(2, summary.UnpaidDebtCount);
        Assert.Equal(450, summary.OutstandingAmount);
        Assert.Equal(new JahezSettlementAmounts(300, 150, 450), summary.Amounts);
        Assert.Equal(f.RiderId, summary.UserRider!.RiderProfileId);
        Assert.Equal(2, Success(await f.Service.RiderTransferredDebtsAsync(f.RiderId, 1, 50, ct)).Items.Count);
        Assert.Single(Success(await f.Service.RidersWithTransferredDebtsAsync(1, 1, ct)).Items);
        var money = new JahezRiderDebtRepayment(450, JahezMoneyFund.Cashbox, "full-receipt", "تصفية المديونية المرحلة بالكامل");
        f.Permissions.Denied = JahezWorkflowPermissions.Receive;
        Assert.True((await f.Service.RepayRiderTransferredDebtsAsync("denied", f.RiderId, money, ct)).IsFailure);
        f.Permissions.Denied = null;
        Assert.True((await f.Service.RepayRiderTransferredDebtsAsync("stale", f.RiderId, money with { ExpectedAmount = 449 }, ct)).IsFailure);
        Assert.True((await f.Service.RepayRiderTransferredDebtsAsync("invalid-fund", f.RiderId, money with { Fund = JahezMoneyFund.Bank }, ct)).IsFailure);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        var result = Success(await f.Service.RepayRiderTransferredDebtsAsync("repay-all", f.RiderId, money, ct));
        Assert.Equal(450, result.PaidAmount);
        Assert.Equal(0, result.RemainingAmount);
        Assert.Contains(first.Id, result.PaidDebtIds);
        Assert.Contains(second.Id, result.PaidDebtIds);
        Assert.Empty(Success(await f.Service.RiderTransferredDebtsAsync(f.RiderId, 1, 50, ct)).Items);
        Assert.DoesNotContain(Success(await f.Service.RidersWithTransferredDebtsAsync(1, 50, ct)).Items, x => x.RiderProfileId == f.RiderId);
        var movements = await f.Db.JahezMoneyMovements.ToArrayAsync(ct);
        Assert.Equal(2, movements.Length);
        Assert.Equal(450, movements.Sum(x => x.Amount));
        Assert.All(movements, x => { Assert.Equal(f.RiderId, x.RiderProfileId); Assert.NotNull(x.TransferredDebtId); });
        var movementIds = movements.Select(x => x.Id).ToArray();
        var postings = await f.Db.Set<TreasuryPosting>().Where(x => x.MoneyMovementId.HasValue
            && movementIds.Contains(x.MoneyMovementId.Value)).ToArrayAsync(ct);
        Assert.Equal(2, postings.Length);
        Assert.Equal(450, postings.Sum(x => x.Amount));
        Success(await f.Service.RepayRiderTransferredDebtsAsync("repay-all", f.RiderId, money, ct));
        Assert.Equal(2, await f.Db.JahezMoneyMovements.CountAsync(ct));
        Assert.True((await f.Service.RepayRiderTransferredDebtsAsync("repeat", f.RiderId, money, ct)).IsFailure);
        Assert.DoesNotContain(await f.Db.Set<Notification>().ToArrayAsync(ct), x => x.EventType.StartsWith("jahez.rider.block.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RiderDebtRepaymentFailureLeavesEarlierDebtsUnpaid(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql, createCurrentSchema: sql);
        var ct = TestContext.Current.CancellationToken;
        var valid = await TransferForRider(f, f.RiderId, "valid");
        // This obligation sorts after the valid one, so failure occurs after staging the first receipt.
        var otherAccount = await f.Account("mismatch");
        var otherHandover = Success(await f.Service.HandoverAsync("other-start",
            new(otherAccount.Id, f.OtherRiderId, At(1), "استلام"), ct));
        var original = await f.Db.JahezOperations.SingleAsync(x => x.Id == valid.OperationId, ct);
        var missing = new JahezOperation { HandoverId = otherHandover.Id, Mode = JahezOperationMode.Settle,
            ThroughDate = original.ThroughDate, EffectiveAtUtc = original.EffectiveAtUtc, Reason = "سجل تاريخي غير متسق" };
        f.Db.Add(missing);
        f.Db.Add(new JahezTransferredDebt { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            RiderProfileId = f.RiderId, OperationId = missing.Id, Amount = 10 });
        await f.Db.SaveChangesAsync(ct);
        var failed = await f.Service.RepayRiderTransferredDebtsAsync("fail", f.RiderId,
            new(185, JahezMoneyFund.Cashbox, "receipt", "سداد"), ct);
        Assert.True(failed.IsFailure);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.All(await f.Db.JahezTransferredDebts.ToArrayAsync(ct), x => Assert.Null(x.PaidAtUtc));
    }

    [Fact]
    public void RiderDebtSummaryTranslatesToSqlWithGroupedAmountsAndPagination()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedRiderDebts;Trusted_Connection=True").Options);
        var service = new JahezWorkflowService(db, new MutableUser(), new TestPermissions(), new MutableClock { UtcNow = At(6) });
        var query = (IQueryable<JahezRiderTransferredDebtSummary>)typeof(JahezWorkflowService)
            .GetMethod("WorkflowRiderDebtSummaries", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, [2, 10])!;
        var sql = query.ToQueryString();
        Assert.Contains("GROUP BY", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH NEXT", sql);
        Assert.Contains("LEFT JOIN", sql);
        Assert.Contains("PaidAtUtc", sql);
    }
}
