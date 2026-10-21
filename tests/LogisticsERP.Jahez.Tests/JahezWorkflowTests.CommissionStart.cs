using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Jahez.Tests;

public sealed partial class JahezWorkflowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task V2CommissionStartCorrectsDraftAndPaidCashboxWithoutMovingEnd(bool sql, bool executed)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("start-correction");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var original = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        if (executed) original = Success(await f.Service.ExecuteOperationAsync("pay", original.Id, ct));
        var sessionBefore = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        var movementBefore = await f.Db.JahezMoneyMovements.AsNoTracking().SingleOrDefaultAsync(ct);
        var request = new JahezOperationCommissionStartUpdate(new(2026, 10, 4), "تصحيح بداية العمولة", Convert.ToBase64String(original.RowVersion));
        var corrected = Success(await f.Service.UpdateOperationCommissionStartAsync("correct", original.Id, request, ct));
        Assert.Equal(45, corrected.Commission); Assert.Equal(45, corrected.CalculatedCommission);
        Assert.Equal(145, corrected.Remaining); Assert.Equal(original.Net, corrected.Net);
        Assert.Equal(original.ThroughDate, corrected.ThroughDate); Assert.Equal(original.EffectiveAtUtc, corrected.EffectiveAtUtc);
        Assert.Equal(original.ExecutedAtUtc, corrected.ExecutedAtUtc); Assert.Equal(original.ExecutedByUserId, corrected.ExecutedByUserId);
        Assert.Equal(original.Reason, corrected.Reason); Assert.Equal(new DateOnly(2026, 10, 4), corrected.CommissionStartDate);
        if (sql) Assert.NotEqual(original.RowVersion, corrected.RowVersion);
        var sessionAfter = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        Assert.Equal(sessionBefore.CommissionPostedThrough, sessionAfter.CommissionPostedThrough);
        Assert.Equal(sessionBefore.LastSettlementPaymentAtUtc, sessionAfter.LastSettlementPaymentAtUtc);
        Assert.Equal(sessionBefore.CommissionStartsOn, sessionAfter.CommissionStartsOn);
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
        Success(await f.Service.UpdateOperationCommissionStartAsync("correct", original.Id, request, ct));
        Assert.True((await f.Service.UpdateOperationCommissionStartAsync("correct", original.Id, request with { StartDate = new(2026, 10, 5) }, ct)).IsFailure);
        var ledgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        Success(await f.Service.UpdateOperationCommissionStartAsync("noop", original.Id, request with { RowVersion = Convert.ToBase64String(corrected.RowVersion) }, ct));
        Assert.Equal(ledgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        if (!executed)
        {
            Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
            corrected = Success(await f.Service.ExecuteOperationAsync("pay", corrected.Id, ct));
        }
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(145, movement.Amount);
        if (movementBefore is not null)
        {
            Assert.Equal(movementBefore.Id, movement.Id); Assert.Equal(movementBefore.ActorUserId, movement.ActorUserId);
            Assert.Equal(movementBefore.CreatedAtUtc, movement.CreatedAtUtc); Assert.Equal(movementBefore.OccurredAtUtc, movement.OccurredAtUtc);
            Assert.Contains(await f.Db.Set<AuditEntry>().AsNoTracking().ToArrayAsync(ct), x => x.EntityId == movement.Id && x.Action == "Updated");
        }
        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(45, row.Commission); Assert.Equal(new DateOnly(2026, 10, 4), row.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 6), row.EndDate);
        Assert.Equal(Convert.ToBase64String(corrected.RowVersion), row.OperationRowVersion);
        Assert.Equal(145, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.HandoverId == h.Id && x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
        // Later commission still starts on the next day after the unchanged end date.
        f.Clock.UtcNow = At(8, 23).ToUniversalTime();
        var next = Success(await f.Service.PreviewOperationAsync(Settle(h.Id, 0) with { ThroughDate = new(2026, 10, 8) }, ct));
        Assert.Equal(30, next.Commission);
        var operation = await f.Db.JahezOperations.SingleAsync(x => x.Id == original.Id, ct);
        operation.Commission += 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Theory]
    [InlineData(false, 0, 5, 30, JahezMoneyDirection.Receipt)]
    [InlineData(false, -75, 5, -45, JahezMoneyDirection.Disbursement)]
    [InlineData(false, -30, 5, 0, JahezMoneyDirection.None)]
    [InlineData(false, -125, 5, -95, JahezMoneyDirection.Disbursement)]
    [InlineData(true, -30, 5, 0, JahezMoneyDirection.None)]
    [InlineData(true, -75, 5, -45, JahezMoneyDirection.Disbursement)]
    public async Task V2CommissionStartHandlesZeroAndChangingMoneyDirection(bool sql, decimal net, int day, decimal expected, JahezMoneyDirection direction)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("direction");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var original = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, net, fund: net == -75 ? null : JahezMoneyFund.Cashbox), ct));
        original = Success(await f.Service.ExecuteOperationAsync("pay", original.Id, ct));
        var result = Success(await f.Service.UpdateOperationCommissionStartAsync("correct", original.Id,
            new(new(2026, 10, day), "تصحيح", Convert.ToBase64String(original.RowVersion), JahezMoneyFund.Cashbox, "corrected-ref"), ct));
        Assert.Equal(expected, result.Remaining);
        Assert.Equal(expected == 0 ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid, result.PaymentStatus);
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(Math.Abs(expected), movement.Amount); Assert.Equal(direction, movement.Direction);
        Assert.Equal(expected, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        if (expected == 0)
        {
            // A second correction can reactivate the same zero-valued movement.
            var reactivated = Success(await f.Service.UpdateOperationCommissionStartAsync("reactivate", original.Id,
                new(new(2026, 10, 4), "تصحيح آخر", Convert.ToBase64String(result.RowVersion)), ct));
            Assert.Equal(15, reactivated.Remaining);
            Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        }
    }

    [Fact]
    public async Task V2CommissionStartRejectsInvalidAndOverlappingDatesAndRevokedPermissionsWithoutWrites()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("invalid-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var first = Success(await f.Service.CreateOperationAsync("first", Settle(h.Id, 0) with { ThroughDate = new(2026, 10, 3) }, ct));
        Success(await f.Service.ExecuteOperationAsync("pay-first", first.Id, ct));
        var second = Success(await f.Service.CreateOperationAsync("second", Settle(h.Id, 100), ct));
        second = Success(await f.Service.ExecuteOperationAsync("pay-second", second.Id, ct));
        var request = new JahezOperationCommissionStartUpdate(new(2026, 10, 5), "تصحيح", Convert.ToBase64String(second.RowVersion));
        var beforeCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        foreach (var invalid in new[] { request with { StartDate = new(2026, 9, 30) }, request with { StartDate = new(2026, 10, 3) },
            request with { StartDate = new(2026, 10, 7) }, request with { Reason = " " }, request with { RowVersion = "stale" },
            request with { Fund = (JahezMoneyFund)99 }, request with { PaymentReference = " " } })
        {
            Assert.True((await f.Service.UpdateOperationCommissionStartAsync(Guid.NewGuid().ToString(), second.Id, invalid, ct)).IsFailure);
            Assert.False(f.Db.ChangeTracker.HasChanges());
        }
        Assert.Equal(beforeCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        foreach (var denied in new[] { JahezWorkflowPermissions.Settle, JahezWorkflowPermissions.AdjustmentApprove, JahezWorkflowPermissions.Receive, JahezWorkflowPermissions.Disburse })
        {
            f.Permissions.Denied = denied;
            Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateOperationCommissionStartAsync("correct", second.Id, request, ct)).Error.Type);
        }
        f.Permissions.Denied = null;
        Success(await f.Service.UpdateOperationCommissionStartAsync("correct", second.Id, request, ct));
        f.Permissions.Denied = JahezWorkflowPermissions.AdjustmentApprove;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateOperationCommissionStartAsync("correct", second.Id, request, ct)).Error.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2CommissionStartProtectsAccountantSnapshots(bool confirmed)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("handed-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var o = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        o = Success(await f.Service.ExecuteOperationAsync("pay", o.Id, ct));
        var handover = Success(await f.Service.SubmitCashboxHandoverV2Async("handover", new(new(2026, 10, 6), "تسليم"), ct));
        if (confirmed)
        {
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", handover.Id,
                new(handover.CashboxAmount, handover.PetroAppAmount, handover.BankAmount, "تأكيد", Convert.ToBase64String(handover.RowVersion)), ct));
        }
        var request = new JahezOperationCommissionStartUpdate(new(2026, 10, 4), "تصحيح", Convert.ToBase64String(o.RowVersion));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationCommissionStartAsync("correct", o.Id, request, ct)).Error.Type);
        Assert.Equal(175, (await f.Db.JahezMoneyMovements.SingleAsync(ct)).Amount);
        if (!confirmed)
        {
            Success(await f.Service.CancelCashboxHandoverV2Async("cancel", handover.Id, new("إلغاء", Convert.ToBase64String(handover.RowVersion)), ct));
            Success(await f.Service.UpdateOperationCommissionStartAsync("correct", o.Id, request, ct));
            Assert.Equal(145, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        }
    }

    [Fact]
    public async Task V2CommissionStartUsesPercentagePolicyAndPreservesOriginalUnpaidBalance()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("percentage-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var policy = Success(await f.Service.CreateWorkflowRequestAsync("policy", new(h.Id, JahezApprovalKind.PercentageCommission,
            "نسبة", FromDate: new(2026, 10, 4), ToDate: new(2026, 10, 5)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve-policy", policy.Request.Id, new(true, "اعتماد"), ct));
        Success(await f.Service.RecordEarningsAsync("earnings", new(h.Id, new(2026, 10, 4), new(2026, 10, 5),
            1000, 0, 0, 0, 0, 0, 0, 0, 0, "أرباح"), ct));
        f.Db.Add(new JahezLedgerEntry { HandoverId = h.Id, Bucket = JahezLedgerBucket.Commission,
            Kind = JahezLedgerKind.OpeningBalance, Amount = 10, SourceId = Guid.NewGuid(), OccurredAtUtc = At(1), Reason = "رصيد سابق" });
        await f.Db.SaveChangesAsync(ct);
        var o = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 0), ct));
        Assert.Equal(205, o.Commission);
        o = Success(await f.Service.ExecuteOperationAsync("pay", o.Id, ct));
        var corrected = Success(await f.Service.UpdateOperationCommissionStartAsync("correct", o.Id,
            new(new(2026, 10, 4), "تصحيح", Convert.ToBase64String(o.RowVersion)), ct));
        Assert.Equal(175, corrected.Commission); // 150 percentage + 15 daily + 10 original unpaid.
        using var evidence = JsonDocument.Parse(corrected.CalculationJson);
        Assert.Equal(10, evidence.RootElement.GetProperty("UnpaidCommission").GetDecimal());
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
    }

    [Fact]
    public async Task V2CommissionStartCanCorrectAnEarlierClosedSettlementWithoutChangingLaterHistory()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("earlier-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var first = Success(await f.Service.CreateOperationAsync("first", Settle(h.Id, 100) with { ThroughDate = new(2026, 10, 3) }, ct));
        first = Success(await f.Service.ExecuteOperationAsync("pay-first", first.Id, ct));
        var second = Success(await f.Service.CreateOperationAsync("second", Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw), ct));
        second = Success(await f.Service.ExecuteOperationAsync("pay-second", second.Id, ct));
        var sessionBefore = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        Assert.NotNull(sessionBefore.EndedAtUtc);
        var corrected = Success(await f.Service.UpdateOperationCommissionStartAsync("correct-first", first.Id,
            new(new(2026, 10, 3), "تصحيح التصفية السابقة", Convert.ToBase64String(first.RowVersion)), ct));
        Assert.Equal(15, corrected.Commission); Assert.Equal(first.ThroughDate, corrected.ThroughDate);
        var secondAfter = await f.Db.JahezOperations.AsNoTracking().SingleAsync(x => x.Id == second.Id, ct);
        Assert.Equal(second.CalculationJson, secondAfter.CalculationJson); Assert.Equal(second.Commission, secondAfter.Commission);
        var sessionAfter = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        Assert.Equal(sessionBefore.EndedAtUtc, sessionAfter.EndedAtUtc);
        Assert.Equal(sessionBefore.CommissionPostedThrough, sessionAfter.CommissionPostedThrough);
        Assert.Equal(sessionBefore.LastSettlementPaymentAtUtc, sessionAfter.LastSettlementPaymentAtUtc);
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
    }

    [Fact]
    public async Task V2CommissionStartRejectsIncompletePercentageEvidenceAndPendingReviewWithoutWrites()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("incomplete-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var policy = Success(await f.Service.CreateWorkflowRequestAsync("policy", new(h.Id, JahezApprovalKind.PercentageCommission,
            "نسبة", FromDate: new(2026, 10, 4), ToDate: new(2026, 10, 5)), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve-policy", policy.Request.Id, new(true, "اعتماد"), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var beforeCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        var request = new JahezOperationCommissionStartUpdate(new(2026, 10, 4), "تصحيح", Convert.ToBase64String(draft.RowVersion));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationCommissionStartAsync("correct", draft.Id, request, ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges()); Assert.Empty(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        Assert.Equal(beforeCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Success(await f.Service.CreateWorkflowRequestAsync("review", new(h.Id, JahezApprovalKind.Adjustment,
            "مراجعة", OperationId: draft.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement), ct));
        var current = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationCommissionStartAsync("review-blocked", draft.Id,
            request with { RowVersion = Convert.ToBase64String(current.RowVersion) }, ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task V2CommissionStartRecalculatesApprovedOverrideAndRejectsInvalidPetroAppPayout()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("override-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, -50, fund: JahezMoneyFund.PetroApp), ct));
        var approval = Success(await f.Service.CreateWorkflowRequestAsync("review", new(h.Id, JahezApprovalKind.Adjustment,
            "تعديل العمولة", OperationId: draft.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedCommission: 100), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", approval.Request.Id, new(true, "اعتماد"), ct));
        var paid = Success(await f.Service.ExecuteOperationAsync("pay", draft.Id, ct));
        Assert.Equal(100, paid.Commission); Assert.Equal(75, paid.CalculatedCommission);
        var request = new JahezOperationCommissionStartUpdate(new(2026, 10, 5), "تصحيح", Convert.ToBase64String(paid.RowVersion));
        Assert.Equal(ErrorType.Validation, (await f.Service.UpdateOperationCommissionStartAsync("invalid-route", paid.Id, request, ct)).Error.Type);
        Assert.Equal(50, (await f.Db.JahezMoneyMovements.SingleAsync(ct)).Amount);
        var corrected = Success(await f.Service.UpdateOperationCommissionStartAsync("correct", paid.Id,
            request with { Fund = JahezMoneyFund.Cashbox }, ct));
        Assert.Equal(30, corrected.Commission); Assert.Equal(30, corrected.CalculatedCommission); Assert.Equal(-20, corrected.Remaining);
        Assert.Null(corrected.ApprovedRequestId);
        Assert.Equal(JahezApprovalStatus.Approved, (await f.Db.JahezApprovalRequests.SingleAsync(ct)).Status);
        var movement = await f.Db.JahezMoneyMovements.SingleAsync(ct);
        Assert.Equal(JahezMoneyDirection.Disbursement, movement.Direction); Assert.Equal(20, movement.Amount);
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
    }

    [Fact]
    public async Task V2CommissionStartRollsBackOperationMovementAndLedgerOnLateSqlFailure()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("rollback-start");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var o = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        o = Success(await f.Service.ExecuteOperationAsync("pay", o.Id, ct));
        var ledgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        // Fail the command receipt after the operation, movement and audit have been staged/saved.
        await f.Db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER jahez_v2.FailCommissionCorrectionReceipt ON jahez_v2.CommandReceipts AFTER INSERT AS
            BEGIN
              IF EXISTS(SELECT 1 FROM inserted WHERE Operation='v2.operation.commission-start.update')
                THROW 51000, 'Deliberate late failure in disposable test database.', 1;
            END;
            """, ct);
        var result = await f.Service.UpdateOperationCommissionStartAsync("correct", o.Id,
            new(new(2026, 10, 4), "تصحيح", Convert.ToBase64String(o.RowVersion)), ct);
        Assert.True(result.IsFailure); Assert.False(f.Db.ChangeTracker.HasChanges());
        var unchanged = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
        Assert.Null(unchanged.CommissionStartDate); Assert.Equal(75, unchanged.Commission);
        Assert.Equal(175, unchanged.Remaining);
        Assert.Equal(175, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Amount);
        Assert.Equal(ledgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.False(await f.Db.JahezCommandReceipts.AnyAsync(x => x.CommandKey == "correct", ct));
    }
}
