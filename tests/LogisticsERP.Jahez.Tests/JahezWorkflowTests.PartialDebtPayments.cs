using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Infrastructure.Jahez;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Fact]
    public void DebtCommissionRequestRequiresExplicitCommissionEvenWhenZeroIsAllowed()
    {
        var options = System.Text.Json.JsonSerializerOptions.Web;
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<JahezRiderDebtCommissionUpdate>(
            """{"expectedCommission":200,"expectedAmounts":{"dashboardDebt":300,"commission":200,"total":500},"reason":"adjust"}""", options));
        var explicitZero = System.Text.Json.JsonSerializer.Deserialize<JahezRiderDebtCommissionUpdate>(
            """{"commission":0,"expectedCommission":200,"expectedAmounts":{"dashboardDebt":300,"commission":200,"total":500},"reason":"adjust"}""", options);
        Assert.Equal(0, explicitZero!.Commission);
    }

    private static async Task<JahezTransferredDebt> TransferWithCommission(Fixture f, string key, decimal net = 300, decimal commission = 200)
    {
        var ct = TestContext.Current.CancellationToken;
        var account = await f.Account(key);
        var h = Success(await f.Service.HandoverAsync(key + "-start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var o = Success(await f.Service.CreateOperationAsync(key + "-draft", Settle(h.Id, net, JahezOperationMode.SettleAndWithdraw, null, true), ct));
        var adjustment = Success(await f.Service.CreateWorkflowRequestAsync(key + "-adjust", new(h.Id, JahezApprovalKind.Adjustment,
            "عمولة معتمدة", OperationId: o.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedCommission: commission), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync(key + "-approve-adjust", adjustment.Request.Id, new(true, "اعتماد"), ct));
        var transfer = Success(await f.Service.CreateWorkflowRequestAsync(key + "-transfer", new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: o.Id), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync(key + "-approve-transfer", transfer.Request.Id, new(true, "اعتماد"), ct));
        return await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(x => x.OperationId == o.Id, ct);
    }

    private static async Task<JahezRiderTransferredDebtSummary> RiderDebtSummary(Fixture f) =>
        Success(await f.Service.RidersWithTransferredDebtsAsync(1, 100, TestContext.Current.CancellationToken)).Items.Single(x => x.RiderProfileId == f.RiderId);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialDebtPaymentPaysDashboardFirstAndLegacyFullRepaymentPaysOnlyTheRemainder(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql, createCurrentSchema: sql); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var debt = await TransferWithCommission(f, "partial-user-example");
        var input = new JahezRiderDebtPartialRepayment(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "first-350", "دفعة جزئية");
        var result = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input, ct));
        Assert.Equal(300, result.DashboardDebtPaid); Assert.Equal(50, result.CommissionPaid);
        Assert.Equal(new JahezSettlementAmounts(0, 150, 150), result.RemainingAmounts);
        Assert.False(Assert.Single(result.Allocations).IsPaid);
        var summary = await RiderDebtSummary(f);
        Assert.Equal(result.RemainingAmounts, summary.Amounts); Assert.Equal(150, summary.OutstandingAmount);
        Assert.Equal(350, summary.PaidAmount); Assert.Equal(200, summary.TotalCommission);
        var row = Assert.Single(Success(await f.Service.RiderTransferredDebtsAsync(f.RiderId, 1, 50, ct)).Items);
        Assert.Equal(500, row.Amount); Assert.Equal(350, row.PaidAmount); Assert.Equal(150, row.OutstandingAmount);
        Assert.Equal(result.RemainingAmounts, row.Amounts); Assert.Null(row.PaidAtUtc);
        var movement = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(300, movement.DashboardDebtPaid); Assert.Equal(50, movement.CommissionPaid);
        Assert.Equal(350, movement.Amount); Assert.Equal(debt.Id, movement.TransferredDebtId);
        Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input, ct));
        Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Success(await f.Service.RepayRiderTransferredDebtsAsync("full-remainder", f.RiderId,
            new(150, JahezMoneyFund.Cashbox, "remaining-150", "إكمال السداد"), ct));
        var receipts = await f.Db.JahezMoneyMovements.OrderBy(x => x.Amount).ToArrayAsync(ct);
        Assert.Equal(2, receipts.Length); Assert.Equal(500, receipts.Sum(x => x.Amount));
        Assert.Equal(0, receipts[0].DashboardDebtPaid); Assert.Equal(150, receipts[0].CommissionPaid);
        var paid = await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct);
        Assert.Equal(500, paid.PaidAmount); Assert.NotNull(paid.PaidAtUtc);
        Assert.Empty(Success(await f.Service.RidersWithTransferredDebtsAsync(1, 50, ct)).Items);
        Assert.Equal(500, (await f.Db.Set<TreasuryPosting>().Where(x => x.MoneyMovementId != null).ToArrayAsync(ct)).Sum(x => x.Amount));
        Assert.Equal(200, Success(await f.Service.ProfitReportAsync(ct: ct)).SettlementCommissionTotal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DebtCommissionReductionTo50Makes350ClearTheDebtBeforeOrAfterPayment(bool payFirst)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var debt = await TransferWithCommission(f, "commission-user-example");
        if (payFirst)
        {
            Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId,
                new(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة"), ct));
            // Handing a prior receipt to the accountant must not prevent reducing the unpaid commission.
            var handover = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", handover.Id, CashboxConfirmation(handover), ct));
        }
        var before = await RiderDebtSummary(f);
        var input = new JahezRiderDebtCommissionUpdate(50, 200, before.Amounts!, "تخفيض العمولة");
        var result = Success(await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId, input, ct));
        Assert.Equal(200, result.PreviousCommission); Assert.Equal(50, result.Commission); Assert.Contains(debt.Id, result.AdjustedDebtIds);
        Assert.Equal(new JahezSettlementAmounts(payFirst ? 0 : 300, payFirst ? 0 : 50, payFirst ? 0 : 350), result.RemainingAmounts);
        var operation = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
        Assert.Equal(50, operation.Commission); Assert.Equal(75, operation.CalculatedCommission);
        Assert.Equal(350, operation.Remaining); Assert.Equal(300, operation.Net); Assert.Null(operation.ApprovedRequestId);
        Assert.Equal(50, Success(await f.Service.ProfitReportAsync(ct: ct)).SettlementCommissionTotal);
        Success(await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId, input, ct));
        if (!payFirst)
        {
            Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
            Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("payment", f.RiderId,
                new(350, result.RemainingAmounts, JahezMoneyFund.Cashbox, "350", "سداد"), ct));
        }
        var m = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(350, m.Amount); Assert.Equal(300, m.DashboardDebtPaid);
        Assert.Equal(50, m.CommissionPaid);
        var settled = await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct);
        Assert.Equal(350, settled.Amount); Assert.Equal(350, settled.PaidAmount); Assert.NotNull(settled.PaidAtUtc);
        Assert.Empty(Success(await f.Service.RiderTransferredDebtsAsync(f.RiderId, 1, 50, ct)).Items);
        Assert.Empty(f.Db.JahezDebtCommissionCorrectionOperationIds);
    }

    [Fact]
    public async Task PartialDebtPaymentPrioritizesDashboardAcrossAllDebtsAndCommissionChangesAreDeterministic()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var oldest = await TransferWithCommission(f, "oldest", 100, 200);
        f.Clock.UtcNow = At(6, 23).AddMinutes(1).ToUniversalTime();
        var newest = await TransferWithCommission(f, "newest", 300, 100);
        var result = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("first", f.RiderId,
            new(350, new(400, 300, 700), JahezMoneyFund.Cashbox, "350", "دفعة"), ct));
        Assert.Equal(350, result.DashboardDebtPaid); Assert.Equal(0, result.CommissionPaid);
        Assert.Equal(new JahezSettlementAmounts(50, 300, 350), result.RemainingAmounts);
        Assert.All(result.Allocations, x => Assert.Equal(0, x.CommissionPaid));
        result = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("second", f.RiderId,
            new(100, result.RemainingAmounts, JahezMoneyFund.Cashbox, "100", "دفعة"), ct));
        Assert.Equal(50, result.DashboardDebtPaid); Assert.Equal(50, result.CommissionPaid);
        var corrected = Success(await f.Service.UpdateRiderDebtCommissionAsync("reduce", f.RiderId,
            new(200, 300, result.RemainingAmounts, "تخفيض"), ct));
        Assert.Equal(new JahezSettlementAmounts(0, 150, 150), corrected.RemainingAmounts);
        Assert.Equal(newest.Id, Assert.Single(corrected.AdjustedDebtIds));
        Assert.NotNull((await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(x => x.Id == newest.Id, ct)).PaidAtUtc);
        Assert.Null((await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(x => x.Id == oldest.Id, ct)).PaidAtUtc);
        var summary = await RiderDebtSummary(f);
        Assert.Equal(1, summary.UnpaidDebtCount); Assert.Equal(150, summary.OutstandingAmount);
    }

    [Fact]
    public async Task DebtCommissionCannotEraseCollectedCommissionAndCanIncreaseTheUnpaidBalance()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var debt = await TransferWithCommission(f, "increase");
        var result = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId,
            new(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة"), ct));
        var invalid = await f.Service.UpdateRiderDebtCommissionAsync("too-low", f.RiderId, new(49, 200, result.RemainingAmounts, "تخفيض"), ct);
        Assert.Equal(ErrorType.Conflict, invalid.Error.Type); Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.Equal(500, (await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct)).Amount);
        var increased = Success(await f.Service.UpdateRiderDebtCommissionAsync("increase", f.RiderId,
            new(250, 200, result.RemainingAmounts, "زيادة معتمدة"), ct));
        Assert.Equal(new JahezSettlementAmounts(0, 200, 200), increased.RemainingAmounts);
        Success(await f.Service.RepayTransferredDebtAsync("remainder", debt.Id, new(JahezMoneyFund.Cashbox, "200", "سداد"), ct));
        Assert.Equal(550, (await f.Db.JahezMoneyMovements.ToArrayAsync(ct)).Sum(x => x.Amount));
        Assert.Empty(Success(await f.Service.RidersWithTransferredDebtsAsync(1, 50, ct)).Items);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-50, 150, 100)]
    public async Task DebtCommissionSupportsFullWaiverAndNetCreditsWithoutInventingReceipts(decimal net, decimal commission, decimal payment)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await TransferWithCommission(f, "credit", net, 200);
        if (payment > 0)
        {
            var before = await RiderDebtSummary(f);
            Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId,
                new(payment, before.Amounts!, JahezMoneyFund.Cashbox, "receipt", "دفعة"), ct));
        }
        var summary = await RiderDebtSummary(f);
        var result = Success(await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId,
            new(commission, 200, summary.Amounts!, "تسوية"), ct));
        Assert.Equal(0, result.RemainingAmounts.Total);
        Assert.Equal(payment > 0 ? 1 : 0, await f.Db.JahezMoneyMovements.CountAsync(ct));
        Assert.NotNull((await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct)).PaidAtUtc);
    }

    [Fact]
    public async Task PartialDebtPaymentAndCommissionValidateSnapshotsAndPermissionsBeforeReplay()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await TransferWithCommission(f, "validation");
        var input = new JahezRiderDebtPartialRepayment(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة");
        foreach (var invalid in new[] { input with { Amount = 0 }, input with { Amount = 501 }, input with { Amount = 1.001m },
            input with { Amount = decimal.MinValue }, input with { Reason = " " }, input with { Reference = " " },
            input with { Fund = JahezMoneyFund.Bank }, input with { Fund = JahezMoneyFund.PetroApp }, input with { ExpectedAmounts = new(0, 200, 500) } })
        {
            Assert.True((await f.Service.PartiallyRepayRiderTransferredDebtsAsync(Guid.NewGuid().ToString(), f.RiderId, invalid, ct)).IsFailure);
            Assert.False(f.Db.ChangeTracker.HasChanges());
        }
        foreach (var denied in new[] { JahezWorkflowPermissions.Repay, JahezWorkflowPermissions.Receive })
        {
            f.Permissions.Denied = denied;
            Assert.Equal(ErrorType.Forbidden, (await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input, ct)).Error.Type);
        }
        f.Permissions.Denied = null;
        var result = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input, ct));
        f.Permissions.Denied = JahezWorkflowPermissions.Receive;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input, ct)).Error.Type);
        f.Permissions.Denied = null;
        Assert.Equal(ErrorType.Conflict, (await f.Service.PartiallyRepayRiderTransferredDebtsAsync("partial", f.RiderId, input with { Amount = 1 }, ct)).Error.Type);
        var change = new JahezRiderDebtCommissionUpdate(100, 200, result.RemainingAmounts, "تخفيض");
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateRiderDebtCommissionAsync("stale", f.RiderId, change with { ExpectedCommission = 199 }, ct)).Error.Type);
        Assert.Equal(ErrorType.Validation, (await f.Service.UpdateRiderDebtCommissionAsync("invalid", f.RiderId, change with { Commission = -1 }, ct)).Error.Type);
        foreach (var denied in new[] { JahezWorkflowPermissions.Settle, JahezWorkflowPermissions.AdjustmentApprove })
        {
            f.Permissions.Denied = denied;
            Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId, change, ct)).Error.Type);
        }
        f.Permissions.Denied = null;
        Success(await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId, change, ct));
        f.Permissions.Denied = JahezWorkflowPermissions.AdjustmentApprove;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateRiderDebtCommissionAsync("commission", f.RiderId, change, ct)).Error.Type);
        Assert.Empty(f.Db.JahezDebtCommissionCorrectionOperationIds);
    }

    [Fact]
    public async Task PartialDebtPaymentsAndCommissionKeepReceiptsAndClosedDebtsImmutable()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await TransferWithCommission(f, "immutable-partial");
        var paid = Success(await f.Service.PartiallyRepayRiderTransferredDebtsAsync("pay", f.RiderId,
            new(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة"), ct));
        var debt = await f.Db.JahezTransferredDebts.SingleAsync(ct);
        debt.PaidAmount = 349;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        var movement = await f.Db.JahezMoneyMovements.SingleAsync(ct);
        movement.CommissionPaid = 49;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        var operation = await f.Db.JahezOperations.SingleAsync(ct);
        operation.Commission -= 1; operation.Remaining -= 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        Success(await f.Service.UpdateRiderDebtCommissionAsync("close", f.RiderId, new(50, 200, paid.RemainingAmounts, "تسوية"), ct));
        var closed = await f.Db.JahezTransferredDebts.SingleAsync(ct);
        closed.PaidAtUtc = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task PartialDebtCommandsRollBackReceiptsTreasuryAndCommissionOnLateSqlFailure()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql: true, createCurrentSchema: true); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        await TransferWithCommission(f, "partial-rollback");
        var ledgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        await f.Db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER jahez_v2.FailPartialDebtReceipt ON jahez_v2.CommandReceipts AFTER INSERT AS
            BEGIN
              IF EXISTS(SELECT 1 FROM inserted WHERE Operation IN ('v2.rider-debts.partial-repay', 'v2.rider-debts.commission.update'))
                THROW 51000, 'Deliberate late failure in disposable test database.', 1;
            END;
            """, ct);
        Assert.True((await f.Service.PartiallyRepayRiderTransferredDebtsAsync("pay-fail", f.RiderId,
            new(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة"), ct)).IsFailure);
        Assert.True((await f.Service.UpdateRiderDebtCommissionAsync("commission-fail", f.RiderId,
            new(50, 200, new(300, 200, 500), "تخفيض"), ct)).IsFailure);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Empty(await f.Db.Set<TreasuryPosting>().ToArrayAsync(ct));
        var debt = await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct);
        Assert.Equal(0, debt.PaidAmount); Assert.Equal(500, debt.Amount); Assert.Null(debt.PaidAtUtc);
        Assert.Equal(200, (await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct)).Commission);
        Assert.Equal(ledgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.Empty(f.Db.JahezDebtCommissionCorrectionOperationIds);
    }

    [Fact]
    public async Task ConcurrentPartialDebtPaymentsCannotSpendTheSameOutstandingBalanceTwice()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql: true, createCurrentSchema: true); var ct = TestContext.Current.CancellationToken;
        await TransferWithCommission(f, "partial-concurrent");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(f.Db.Database.GetConnectionString())
            .AddInterceptors(new ApplicationPersistenceInterceptor(f.User, f.Clock)).Options;
        await using var first = new ApplicationDbContext(options, f.Clock);
        await using var second = new ApplicationDbContext(options, f.Clock);
        var a = new JahezWorkflowService(first, f.User, f.Permissions, f.Clock);
        var b = new JahezWorkflowService(second, f.User, f.Permissions, f.Clock);
        var request = new JahezRiderDebtPartialRepayment(350, new(300, 200, 500), JahezMoneyFund.Cashbox, "350", "دفعة");
        var results = await Task.WhenAll(a.PartiallyRepayRiderTransferredDebtsAsync("a", f.RiderId, request, ct),
            b.PartiallyRepayRiderTransferredDebtsAsync("b", f.RiderId, request, ct));
        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.IsFailure && x.Error.Type == ErrorType.Conflict);
        Assert.Equal(350, (await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct)).PaidAmount);
        Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }

    [Fact]
    public async Task PartialDebtMigrationKeepsExistingPaidDebtsClosed()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql: true); var ct = TestContext.Current.CancellationToken;
        var debt = await TransferWithCommission(f, "migration-paid");
        var h = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        var migrator = f.Db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261021071103_AddPlatformAccountDayAllocations", ct);
        var now = f.Clock.GetUtcNow(); var movementId = Guid.NewGuid(); var actorId = f.User.UserId!.Value;
        await f.Db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO jahez_v2.MoneyMovements
                (Id, HandoverId, RiderProfileId, TransferredDebtId, Fund, Direction, Purpose, Amount, ActorUserId,
                 OccurredAtUtc, Reference, Reason, CreatedAtUtc, CreatedByUserId)
            VALUES ({movementId}, {h.Id}, {f.RiderId}, {debt.Id}, 1, 1, 4, 500, {actorId}, {now}, N'legacy-full', N'سداد قديم', {now}, {actorId});
            UPDATE jahez_v2.TransferredDebts SET PaidAtUtc = {now}, PaidByUserId = {actorId} WHERE Id = {debt.Id};
            """, ct);
        await migrator.MigrateAsync("20261024115050_SupportJahezPartialDebtRepayments", ct);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(500, (await f.Db.JahezTransferredDebts.AsNoTracking().SingleAsync(ct)).PaidAmount);
        Assert.Empty(Success(await f.Service.RidersWithTransferredDebtsAsync(1, 50, ct)).Items);
        var legacy = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        Assert.Null(legacy.DashboardDebtPaid); Assert.Null(legacy.CommissionPaid); Assert.Equal(500, legacy.Amount);
    }
}
