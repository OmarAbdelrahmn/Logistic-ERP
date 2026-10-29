using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task FeeRefundDeductsOrWaivesTwoDaysAndWithdrawsAtomically(bool waive, bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("refund-two-days");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var preview = Success(await f.Service.PreviewFeeRefundAsync(new(h.Id, waive), ct));
        Assert.Equal(200, preview.GrossFeeRefund); Assert.Equal(30, preview.CalculatedCommission);
        Assert.Equal(waive ? 200 : 170, preview.RefundAmount);
        // Completed orders do not make the account ineligible.
        var import = Success(await f.Service.UploadAsync("orders", new(JahezImportKind.DailyDispatches,
            [new("orders.xlsx", Dispatches(account.ExternalAccountId, "2026-10-05", 8))]), ct));
        Success(await f.Service.CommitImportAsync("commit-orders", import.BatchId, new(), ct));
        var request = new JahezFeeRefundCreate(h.Id, waive, "refund-receipt", "استرداد رسوم فتح حساب");
        var refund = Success(await f.Service.RefundFeeAsync("refund", request, ct));
        Assert.Equal(preview.RefundAmount, refund.RefundAmount);
        Assert.Equal(waive ? 0 : 30, refund.DeductedCommission);
        Assert.Equal(refund.Id, Success(await f.Service.RefundFeeAsync("refund", request, ct)).Id);
        Assert.Equal(ErrorType.Conflict, (await f.Service.RefundFeeAsync("other-key", request, ct)).Error.Type);
        Assert.Equal(ErrorType.Conflict, (await f.Service.RefundFeeAsync("refund", request with { WaiveCommission = !waive }, ct)).Error.Type);
        Assert.Single(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        Assert.Single(await f.Db.JahezMoneyMovements.Where(x => x.Purpose == JahezMoneyPurpose.AccountFeeRefund).ToArrayAsync(ct));
        Assert.Equal(0, await f.Db.Set<JahezLedgerEntry>().Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.AccountFees).SumAsync(x => x.Amount, ct));
        Assert.Equal(0, await f.Db.Set<JahezLedgerEntry>().Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
        var closed = await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h.Id, ct);
        Assert.Equal(f.Clock.UtcNow, closed.EndedAtUtc);
        Assert.Equal(new DateOnly(2026, 10, 6), closed.CommissionPostedThrough);
        Assert.Equal(RiderAssignmentStatus.Ended, (await f.Db.RiderClientAssignments.SingleAsync(x => x.Id == closed.RiderClientAssignmentId, ct)).Status);
        Assert.Equal(PlatformRiderAccountStatus.Available, (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct)).Status);
        var profit = Success(await f.Service.ProfitReportAsync(ct: ct));
        Assert.Equal(0, profit.AccountOpeningFeesTotal); Assert.Equal(waive ? 0 : 30, profit.SettlementCommissionTotal);
        Assert.Single(Success(await f.Service.FeeRefundsAsync(h.Id, ct)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task FeeRefundEnforcesExactSevenDayDeadline(long ticksAfter, bool allowed)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-deadline");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 200, "fee"), ct));
        f.Clock.UtcNow = At(8).AddTicks(ticksAfter).ToUniversalTime();
        var result = await f.Service.RefundFeeAsync("refund", new(h.Id, false, "refund", "استرداد"), ct);
        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed)
        {
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Null((await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
            Assert.Empty(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        }
    }

    [Fact]
    public async Task FeeRefundOnlyReturnsCollectedFeesAndCancelsUnpaidRemainder()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-partial");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 80, "fee"), ct));
        var refund = Success(await f.Service.RefundFeeAsync("refund", new(h.Id, false, "refund", "استرداد"), ct));
        Assert.Equal(80, refund.GrossFeeRefund); Assert.Equal(120, refund.CancelledUnpaidFee); Assert.Equal(50, refund.RefundAmount);
        Assert.Equal(0, await f.Db.Set<JahezLedgerEntry>().Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.AccountFees).SumAsync(x => x.Amount, ct));
    }

    [Theory]
    [InlineData(JahezWorkflowPermissions.FeeApprove)]
    [InlineData(JahezWorkflowPermissions.Withdraw)]
    [InlineData(JahezWorkflowPermissions.Disburse)]
    [InlineData(JahezWorkflowPermissions.CommissionApprove)]
    public async Task FeeRefundRequiresAllPermissionsIncludingOnReplay(string permission)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-permissions");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var request = new JahezFeeRefundCreate(h.Id, true, "refund", "استرداد");
        f.Permissions.Denied = permission;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.RefundFeeAsync("refund", request, ct)).Error.Type);
        Assert.Empty(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        f.Permissions.Denied = null; Success(await f.Service.RefundFeeAsync("refund", request, ct));
        f.Permissions.Denied = permission;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.RefundFeeAsync("refund", request, ct)).Error.Type);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 500)]
    [InlineData(true, 500)]
    public async Task FeeRefundAfterHandoverOverdrawsCustodyAndDeductsFromNextSignedHandover(bool sql, decimal otherCash)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("refund-overdraft");
        var sponsor = account.SponsorId!.Value;
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var first = Success(await f.Service.SubmitTreasuryHandoverAsync("first", new(new(2026, 10, 6), "تسليم الرسوم"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm-first", first.Handover.Id,
            new(first.Lines.Select(x => new TreasuryCashCount(x.SubAccountId, x.Amount)).ToArray(), "استلام", Convert.ToBase64String(first.Handover.RowVersion)), ct));
        // Subsequent sponsor changes must not move the refund away from the original fee owner.
        var tracked = await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct);
        tracked.SponsorId = await f.Db.Sponsors.Where(x => x.Id != sponsor).Select(x => x.Id).FirstAsync(ct);
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        var refund = Success(await f.Service.RefundFeeAsync("refund", new(h.Id, false, "refund", "استرداد"), ct));
        var custody = await TreasuryAccount(f, sponsor, TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee);
        Assert.Equal(-170, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == custody.Id).Balance);
        var posting = await f.Db.Set<TreasuryPosting>().SingleAsync(x => x.MoneyMovementId == refund.MoneyMovementId, ct);
        Assert.Equal(sponsor, posting.SponsorId);
        if (otherCash > 0) await FundCustody(f, sponsor, otherCash);
        var preview = Success(await f.Service.PreviewTreasuryHandoverAsync(new(2026, 10, 6), ct));
        Assert.Equal(otherCash - 170, preview.PhysicalCash); Assert.Contains(preview.Lines, x => x.Amount == -170);
        var next = Success(await f.Service.SubmitTreasuryHandoverAsync("next", new(new(2026, 10, 6), "التسليم بعد خصم الاسترداد"), ct));
        f.User.UserId = Guid.NewGuid();
        var confirmation = new TreasuryHandoverConfirm(next.Lines.Select(x => new TreasuryCashCount(x.SubAccountId, x.Amount)).ToArray(),
            "مطابقة صافي التسليم", Convert.ToBase64String(next.Handover.RowVersion));
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm-next", next.Handover.Id, confirmation, ct));
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm-next", next.Handover.Id, confirmation, ct));
        Assert.Equal(0, Success(await f.Service.PreviewTreasuryHandoverAsync(new(2026, 10, 6), ct)).MovementCount);
        Assert.Equal(0, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == custody.Id).Balance);
        Assert.True(Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items, x => x.Id == refund.MoneyMovementId).IsHandedToAccountant);
    }

    [Fact]
    public async Task FeeRefundDoesNotChargePreviouslySettledCommissionAgain()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-settled");
        f.Clock.UtcNow = At(5).ToUniversalTime();
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var operation = Success(await f.Service.CreateOperationAsync("settle", new(h.Id, JahezOperationMode.Settle,
            new(2026, 10, 5), At(5), -15, "تصفية اليوم الأول"), ct));
        Success(await f.Service.ExecuteOperationAsync("execute", operation.Id, ct));
        f.Clock.UtcNow = At(6).ToUniversalTime();
        var refund = Success(await f.Service.RefundFeeAsync("refund", new(h.Id, false, "refund", "استرداد"), ct));
        Assert.Equal(15, refund.CalculatedCommission); Assert.Equal(185, refund.RefundAmount);
        Assert.Equal(30, Success(await f.Service.ProfitReportAsync(ct: ct)).TotalProfit);
    }

    [Theory]
    [InlineData("unpaid")]
    [InlineData("pending")]
    [InlineData("shortfall")]
    public async Task FeeRefundRejectsUnpaidFeesPendingWorkAndCommissionShortfall(string scenario)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-rejected");
        var amount = scenario == "unpaid" ? 0 : scenario == "shortfall" ? 10 : 200;
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", amount, "fee"), ct));
        if (scenario == "pending")
            Success(await f.Service.CreateOperationAsync("pending", Settle(h.Id, 0), ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.RefundFeeAsync("refund", new(h.Id, false, "refund", "استرداد"), ct)).Error.Type);
        Assert.Null((await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.Empty(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task FeeRefundReceiptCanGraduallyReplenishNegativeCashbox()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("refund-replenish");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var handover = Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm", handover.Handover.Id,
            new(handover.Lines.Select(x => new TreasuryCashCount(x.SubAccountId, x.Amount)).ToArray(), "استلام", Convert.ToBase64String(handover.Handover.RowVersion)), ct));
        Success(await f.Service.RefundFeeAsync("refund", new(h.Id, true, "refund", "استرداد"), ct));
        var context = Success(await new LogisticsERP.Infrastructure.Hr.PlatformAccountDayAllocationService(f.Db).Context(account.Id, new(2026, 10, 6), f.OtherRiderId, ct));
        var prior = Assert.Single(context.Contributions);
        var next = Success(await f.Service.HandoverAsync("next", new(account.Id, f.OtherRiderId, At(6, 23), "استلام جديد", 50, "new-fee",
            PreviousDayUsages: [new(prior.AssignmentIds, new(0), prior.RowVersion, prior.UsageRowVersions)]), ct));
        var custody = await TreasuryAccount(f, account.SponsorId!.Value, TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee);
        Assert.Equal(-150, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == custody.Id).Balance);
        Assert.NotEqual(h.Id, next.Id);
    }

    [Fact]
    public async Task FeeRefundLegacyNegativeHandoverBackfillsWithoutPayingAgain()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-legacy");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var collected = Success(await f.Service.SubmitCashboxHandoverV2Async("collected", new(new(2026, 10, 6), "تسليم الرسوم"), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm-collected", collected.Id, CashboxConfirmation(collected), ct));
        Success(await f.Service.RefundFeeAsync("refund", new(h.Id, true, "refund", "استرداد"), ct));
        var refunded = Success(await f.Service.SubmitCashboxHandoverV2Async("refunded", new(new(2026, 10, 6), "خصم الاسترداد"), ct));
        Assert.Equal(-200, refunded.CashboxAmount);
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm-refunded", refunded.Id, CashboxConfirmation(refunded), ct));
        Assert.Equal(4, Success(await f.Service.BackfillTreasuryAsync("backfill", "ترحيل الاسترداد", ct)));
        Assert.Equal(0, Success(await f.Service.BackfillTreasuryAsync("again", "ترحيل الاسترداد", ct)));
        foreach (var balance in Success(await f.Service.TreasuryBalancesAsync(ct)).Where(x => x.IsPosting))
        {
            Assert.Equal(0, balance.Balance);
            Success(await f.Service.VerifyTreasuryBalanceAsync($"verify-{balance.SubAccountId}", new(balance.SubAccountId, balance.Balance, "verified", "مطابقة"), ct));
        }
        var ready = Success(await f.Service.TreasuryReadinessAsync(ct));
        Success(await f.Service.ActivateTreasuryAsync("activate", new(ready.RowVersion, "تفعيل"), ct));
        Assert.Equal(0, Success(await f.Service.PreviewTreasuryHandoverAsync(new(2026, 10, 6), ct)).MovementCount);
    }

    [Fact]
    public async Task FeeRefundConcurrentSqlRequestsPayOnlyOnce()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql: true); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("refund-concurrency");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var options = new DbContextOptionsBuilder<LogisticsERP.Infrastructure.Persistence.ApplicationDbContext>()
            .UseSqlServer(f.Db.Database.GetConnectionString())
            .AddInterceptors(new LogisticsERP.Infrastructure.Persistence.Interceptors.ApplicationPersistenceInterceptor(f.User, f.Clock)).Options;
        await using var first = new LogisticsERP.Infrastructure.Persistence.ApplicationDbContext(options, f.Clock);
        await using var second = new LogisticsERP.Infrastructure.Persistence.ApplicationDbContext(options, f.Clock);
        var a = new LogisticsERP.Infrastructure.Jahez.JahezWorkflowService(first, f.User, f.Permissions, f.Clock);
        var b = new LogisticsERP.Infrastructure.Jahez.JahezWorkflowService(second, f.User, f.Permissions, f.Clock);
        var request = new JahezFeeRefundCreate(h.Id, false, "refund", "استرداد");
        var results = await Task.WhenAll(a.RefundFeeAsync("first", request, ct), b.RefundFeeAsync("second", request, ct));
        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsFailure && x.Error.Type == ErrorType.Conflict);
        Assert.Single(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        Assert.Single(await f.Db.JahezMoneyMovements.Where(x => x.Purpose == JahezMoneyPurpose.AccountFeeRefund).ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FeeRefundLateTreasuryFailureLeavesNoPayoutOrWithdrawal(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("refund-rollback");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(4), "استلام", 200, "fee"), ct));
        var custody = await TreasuryAccount(f, account.SponsorId!.Value, TreasuryLocation.Supervisor, TreasuryPurpose.AccountFee);
        f.Db.ChangeTracker.Clear(); f.Db.Attach(custody); custody.IsActive = false;
        await f.Db.SaveChangesAsync(ct); f.Db.ChangeTracker.Clear();
        Assert.False(await f.Db.Set<TreasurySubAccount>().Where(x => x.Id == custody.Id).Select(x => x.IsActive).SingleAsync(ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.RefundFeeAsync("refund", new(h.Id, true, "refund", "استرداد"), ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.Empty(await f.Db.JahezAccountFeeRefunds.ToArrayAsync(ct));
        Assert.False(await f.Db.JahezMoneyMovements.AnyAsync(x => x.Purpose == JahezMoneyPurpose.AccountFeeRefund, ct));
        Assert.Null((await f.Db.JahezAccountSessions.SingleAsync(x => x.Id == h.Id, ct)).EndedAtUtc);
        Assert.Equal(PlatformRiderAccountStatus.Assigned, (await f.Db.PlatformRiderAccounts.SingleAsync(x => x.Id == account.Id, ct)).Status);
    }
}
