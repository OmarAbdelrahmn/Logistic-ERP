using System.Reflection;
using System.Text.Json;
using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.Controllers;
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
    [InlineData(false, 125.555, 200.56)]
    [InlineData(false, -125, -50)]
    [InlineData(false, -75, 0)]
    [InlineData(true, 125.555, 200.56)]
    public async Task V2NetCorrectionRecalculatesCashboxAndRetainsCommissionAndHistory(bool sql, decimal net, decimal remaining)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(sql); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-correction");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw), ct));
        var original = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var movementBefore = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        var sessionBefore = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        var profitBefore = Success(await f.Service.ProfitReportAsync(ct: ct));
        var request = new JahezOperationNetUpdate(net, "تصحيح صافي مبلغ التصفية", Convert.ToBase64String(original.RowVersion));
        var corrected = Success(await f.Service.UpdateOperationNetAsync("correct", original.Id, request, ct));
        Assert.Equal(Math.Round(net, 2, MidpointRounding.AwayFromZero), corrected.Net);
        Assert.Equal(remaining, corrected.Remaining);
        Assert.Equal(original.Commission, corrected.Commission); Assert.Equal(original.CalculatedCommission, corrected.CalculatedCommission);
        Assert.Equal(original.CalculationJson, corrected.CalculationJson); Assert.Equal(original.ThroughDate, corrected.ThroughDate);
        Assert.Equal(original.CommissionStartDate, corrected.CommissionStartDate); Assert.Equal(original.EffectiveAtUtc, corrected.EffectiveAtUtc);
        Assert.Equal(original.ExecutedAtUtc, corrected.ExecutedAtUtc); Assert.Equal(original.ExecutedByUserId, corrected.ExecutedByUserId);
        Assert.Equal(original.Reason, corrected.Reason); Assert.Equal(original.Status, corrected.Status);
        Assert.Equal(remaining == 0 ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid, corrected.PaymentStatus);
        var m = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(Math.Abs(remaining), m.Amount);
        Assert.Equal(remaining > 0 ? JahezMoneyDirection.Receipt : remaining < 0 ? JahezMoneyDirection.Disbursement : JahezMoneyDirection.None, m.Direction);
        Assert.Equal(movementBefore.Id, m.Id); Assert.Equal(movementBefore.RiderProfileId, m.RiderProfileId);
        Assert.Equal(movementBefore.OccurredAtUtc, m.OccurredAtUtc); Assert.Equal(movementBefore.CreatedAtUtc, m.CreatedAtUtc);
        Assert.Equal(movementBefore.ActorUserId, m.ActorUserId); Assert.Equal(movementBefore.Reference, m.Reference);
        Assert.Equal(movementBefore.Reason, m.Reason);
        var sessionAfter = await f.Db.JahezAccountSessions.AsNoTracking().SingleAsync(ct);
        Assert.Equal(sessionBefore.EndedAtUtc, sessionAfter.EndedAtUtc);
        Assert.Equal(sessionBefore.CommissionPostedThrough, sessionAfter.CommissionPostedThrough);
        Assert.Equal(sessionBefore.LastSettlementPaymentAtUtc, sessionAfter.LastSettlementPaymentAtUtc);
        Assert.Equal(remaining, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == JahezMoneyFund.Cashbox).Balance);
        Assert.Equal(profitBefore, Success(await f.Service.ProfitReportAsync(ct: ct)));
        var row = Assert.Single(Success(await f.Service.MovementsV2Async(1, 50, ct: ct)).Items);
        Assert.Equal(corrected.Net, row.Net); Assert.Equal(75, row.Commission);
        var correction = await f.Db.JahezLedgerEntries.SingleAsync(x => x.Reason == request.Reason, ct);
        Assert.Equal(0, correction.Amount);
        using var evidence = JsonDocument.Parse(correction.CalculationJson!);
        Assert.Equal(100, evidence.RootElement.GetProperty("PreviousNet").GetDecimal());
        Assert.Equal(corrected.Net, evidence.RootElement.GetProperty("Net").GetDecimal());
        Assert.Contains(await f.Db.Set<AuditEntry>().ToArrayAsync(ct), x => x.EntityId == m.Id && x.Action == "Updated");
        Assert.Equal(0, await f.Db.JahezLedgerEntries.Where(x => x.Bucket == JahezLedgerBucket.Commission).SumAsync(x => x.Amount, ct));
        var count = await f.Db.JahezLedgerEntries.CountAsync(ct);
        Success(await f.Service.UpdateOperationNetAsync("correct", original.Id, request, ct));
        Success(await f.Service.UpdateOperationNetAsync("noop", original.Id, request with { RowVersion = Convert.ToBase64String(corrected.RowVersion) }, ct));
        Assert.Equal(count, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationNetAsync("correct", original.Id, request with { Net = 10 }, ct)).Error.Type);
        if (sql)
        {
            Assert.NotEqual(original.RowVersion, corrected.RowVersion);
            Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationNetAsync("stale", original.Id, request with { Net = 10 }, ct)).Error.Type);
        }
        Assert.Null(f.Db.JahezNetCorrectionOperationId);
        var protectedOperation = await f.Db.JahezOperations.SingleAsync(ct);
        protectedOperation.Net += 1; protectedOperation.Remaining += 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task V2NetCorrectionReversesTreasuryPostingsHandlesZeroAndRollsBackInsufficientFunds()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("net-treasury");
        await FundCustody(f, account.SponsorId!.Value, 100);
        var custody = await TreasuryAccount(f, account.SponsorId.Value, TreasuryLocation.Supervisor);
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var m = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        var owner = await f.Db.Set<TreasuryOwnership>().AsNoTracking().SingleAsync(x => x.Source == TreasurySource.Movement && x.SourceId == m.Id, ct);
        foreach (var net in new[] { -75m, -125m, 25m })
        {
            o = Success(await f.Service.UpdateOperationNetAsync($"net-{net}", o.Id, new(net, "تصحيح الخزنة", Convert.ToBase64String(o.RowVersion)), ct));
            Assert.Equal(100 + o.Remaining, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == custody.Id).Balance);
            Assert.Equal(o.Remaining, (await f.Service.PreviewTreasuryHandoverAsync(new(2026, 10, 6), ct)).Value!.PhysicalCash - 100);
            Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        }
        Assert.Equal(owner.SponsorId, (await f.Db.Set<TreasuryOwnership>().SingleAsync(x => x.Id == owner.Id, ct)).SponsorId);
        Assert.Equal(2, await f.Db.Set<TreasuryPosting>().CountAsync(x => x.Kind == TreasuryEntryKind.Reversal, ct));
        var postingCount = await f.Db.Set<TreasuryPosting>().CountAsync(ct);
        var ledgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        var failed = await f.Service.UpdateOperationNetAsync("insufficient", o.Id, new(-500, "تصحيح غير ممول", Convert.ToBase64String(o.RowVersion)), ct);
        Assert.Equal(ErrorType.Conflict, failed.Error.Type); Assert.False(f.Db.ChangeTracker.HasChanges());
        Assert.Equal(postingCount, await f.Db.Set<TreasuryPosting>().CountAsync(ct));
        Assert.Equal(ledgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.Equal(25, (await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct)).Net);
        Assert.Equal(100, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Amount);
        Assert.False(await f.Db.JahezCommandReceipts.AnyAsync(x => x.CommandKey == "insufficient", ct));
        Assert.Null(f.Db.JahezNetCorrectionOperationId);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task V2NetCorrectionProtectsAccountantSnapshotsAndAllowsCancelledHandovers(bool treasury, bool confirmed)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        if (treasury) await EnableTreasury(f);
        var account = await f.Account("net-snapshot");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        TreasuryHandoverDetail? detail = treasury
            ? Success(await f.Service.SubmitTreasuryHandoverAsync("submit", new(new(2026, 10, 6), "تسليم"), ct)) : null;
        var handover = detail?.Handover ?? Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
        if (confirmed)
        {
            f.User.UserId = Guid.NewGuid();
            if (treasury)
                Success(await f.Service.ConfirmTreasuryHandoverAsync("confirm", handover.Id,
                    new(detail!.Lines.Select(l => new TreasuryCashCount(l.SubAccountId, l.Amount)).ToArray(), "تأكيد", Convert.ToBase64String(handover.RowVersion)), ct));
            else Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", handover.Id, CashboxConfirmation(handover), ct));
        }
        var request = new JahezOperationNetUpdate(50, "تصحيح", Convert.ToBase64String(o.RowVersion));
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationNetAsync("correct", o.Id, request, ct)).Error.Type);
        Assert.Equal(175, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Amount);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        if (!confirmed)
        {
            var cancel = new JahezCashboxHandoverCancel("إلغاء للتصحيح", Convert.ToBase64String(handover.RowVersion));
            if (treasury) Success(await f.Service.CancelTreasuryHandoverAsync("cancel", handover.Id, cancel, ct));
            else Success(await f.Service.CancelCashboxHandoverV2Async("cancel", handover.Id, cancel, ct));
            Assert.Equal(125, Success(await f.Service.UpdateOperationNetAsync("correct", o.Id, request, ct)).Remaining);
        }
    }

    [Fact]
    public async Task V2NetCorrectionRejectsInvalidValuesAndRevokedPermissionsIncludingReplay()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-invalid");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var request = new JahezOperationNetUpdate(50, "تصحيح", Convert.ToBase64String(o.RowVersion));
        foreach (var invalid in new[] { request with { Reason = " " }, request with { Reason = new string('x', 2001) },
            request with { Net = decimal.MinValue }, request with { Net = decimal.MaxValue }, request with { Net = 1_000_000_000m },
            request with { RowVersion = "stale" }, request with { Fund = (JahezMoneyFund)99 },
            request with { PaymentReference = " " }, request with { PaymentReference = new string('x', 501) } })
        {
            Assert.True((await f.Service.UpdateOperationNetAsync(Guid.NewGuid().ToString(), o.Id, invalid, ct)).IsFailure);
            Assert.False(f.Db.ChangeTracker.HasChanges());
            Assert.Null(f.Db.JahezNetCorrectionOperationId);
        }
        Assert.Equal(ErrorType.NotFound, (await f.Service.UpdateOperationNetAsync("missing", Guid.NewGuid(), request, ct)).Error.Type);
        foreach (var denied in new[] { JahezWorkflowPermissions.Settle, JahezWorkflowPermissions.AdjustmentApprove,
            JahezWorkflowPermissions.Receive, JahezWorkflowPermissions.Disburse })
        {
            f.Permissions.Denied = denied;
            Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateOperationNetAsync("correct", o.Id, request, ct)).Error.Type);
        }
        f.Permissions.Denied = null;
        Success(await f.Service.UpdateOperationNetAsync("correct", o.Id, request, ct));
        foreach (var denied in new[] { JahezWorkflowPermissions.Settle, JahezWorkflowPermissions.AdjustmentApprove,
            JahezWorkflowPermissions.Receive, JahezWorkflowPermissions.Disburse })
        {
            f.Permissions.Denied = denied;
            Assert.Equal(ErrorType.Forbidden, (await f.Service.UpdateOperationNetAsync("correct", o.Id, request, ct)).Error.Type);
        }
        var permission = typeof(JahezWorkflowController).GetMethod(nameof(JahezWorkflowController.UpdateNet))!.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.Equal(AuthenticationPolicies.PermissionPrefix + JahezWorkflowPermissions.AdjustmentApprove, permission!.Policy);
    }

    [Theory]
    [InlineData("ready")]
    [InlineData("review")]
    [InlineData("withdraw")]
    [InlineData("transfer")]
    [InlineData("fuel")]
    [InlineData("zero")]
    public async Task V2NetCorrectionOnlyAllowsExecutedSettlementsWithMovements(string state)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-status");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var input = state switch
        {
            "withdraw" => Settle(h.Id, 0, JahezOperationMode.Withdraw, null),
            "transfer" => Settle(h.Id, 100, JahezOperationMode.SettleAndWithdraw, null, true),
            "bank" => Settle(h.Id, -125, fund: JahezMoneyFund.Bank),
            "petroapp" => Settle(h.Id, 100, fund: JahezMoneyFund.PetroApp),
            "fuel" => Settle(h.Id, 100) with { Purpose = JahezMoneyPurpose.Fuel },
            "zero" => Settle(h.Id, -75, fund: null),
            _ => Settle(h.Id, 100)
        };
        var o = Success(await f.Service.CreateOperationAsync("draft", input, ct));
        if (state == "review")
            Success(await f.Service.CreateWorkflowRequestAsync("review", new(h.Id, JahezApprovalKind.Adjustment,
                "مراجعة", OperationId: o.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement), ct));
        else if (state == "transfer")
        {
            var request = Success(await f.Service.CreateWorkflowRequestAsync("transfer", new(h.Id, JahezApprovalKind.ResetAccount, "ترحيل", OperationId: o.Id), ct));
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.DecideWorkflowRequestAsync("approve", request.Request.Id, new(true, "اعتماد"), ct));
        }
        else if (state != "ready") o = Success(await f.Service.ExecuteOperationAsync("execute", o.Id, ct));
        var current = await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct);
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationNetAsync("correct", o.Id,
            new(50, "تصحيح", Convert.ToBase64String(current.RowVersion)), ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task V2NetCorrectionPreservesApprovedCommissionOverride()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-override");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var approval = Success(await f.Service.CreateWorkflowRequestAsync("review", new(h.Id, JahezApprovalKind.Adjustment,
            "عمولة معتمدة", OperationId: draft.Id, AdjustmentTarget: JahezAdjustmentTarget.Settlement, ProposedCommission: 50), ct));
        f.User.UserId = Guid.NewGuid();
        Success(await f.Service.DecideWorkflowRequestAsync("approve", approval.Request.Id, new(true, "اعتماد"), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var corrected = Success(await f.Service.UpdateOperationNetAsync("correct", o.Id, new(25, "تصحيح", Convert.ToBase64String(o.RowVersion)), ct));
        Assert.Equal(50, corrected.Commission); Assert.Equal(75, corrected.CalculatedCommission); Assert.Equal(75, corrected.Remaining);
        Assert.Equal(o.CalculationJson, corrected.CalculationJson); Assert.Null(corrected.ApprovedRequestId);
        Assert.Equal(JahezApprovalStatus.Approved, (await f.Db.JahezApprovalRequests.SingleAsync(ct)).Status);
    }

    [Theory]
    [InlineData(JahezMoneyFund.Cashbox, 100, 125, 200)]
    [InlineData(JahezMoneyFund.Bank, -125, -175, -100)]
    [InlineData(JahezMoneyFund.PetroApp, 100, 125, 200)]
    [InlineData(JahezMoneyFund.Bank, -125, -75, 0)]
    [InlineData(JahezMoneyFund.PetroApp, 100, -75, 0)]
    public async Task V2NetCorrectionSupportsEverySettlementFundAndRetainsLegacyCommandHashes(
        JahezMoneyFund fund, decimal originalNet, decimal net, decimal expected)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-all-funds");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, originalNet, fund: fund), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var request = new JahezOperationNetUpdate(net, "تصحيح الصافي", Convert.ToBase64String(o.RowVersion));
        var corrected = Success(await f.Service.UpdateOperationNetAsync("all-funds", o.Id, request, ct));
        Assert.Equal(expected, corrected.Remaining); Assert.Equal(fund, corrected.Fund);
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(Math.Abs(expected), movement.Amount); Assert.Equal(fund, movement.Fund);
        Assert.Equal(expected, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == fund).Balance);
        var legacyPayload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        { id = o.Id, request = new { request.Net, request.Reason, request.RowVersion } });
        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(legacyPayload));
        Assert.Equal(expectedHash, (await f.Db.JahezCommandReceipts.SingleAsync(x => x.CommandKey == "all-funds", ct)).PayloadHash);
        Success(await f.Service.UpdateOperationNetAsync("all-funds", o.Id, request, ct));
        if (expected == 0)
        {
            var reactivated = Success(await f.Service.UpdateOperationNetAsync("reactivate", o.Id,
                request with { Net = originalNet, RowVersion = Convert.ToBase64String(corrected.RowVersion) }, ct));
            Assert.Equal(originalNet + 75, reactivated.Remaining);
            Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
        }
    }

    [Theory]
    [InlineData(JahezMoneyFund.Bank, -125, 50)]
    [InlineData(JahezMoneyFund.PetroApp, 100, -125)]
    public async Task V2NetCorrectionCanChangeFundWhenTheNewDirectionRequiresIt(JahezMoneyFund originalFund, decimal originalNet, decimal net)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-new-direction");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, originalNet, fund: originalFund), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var original = await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct);
        var request = new JahezOperationNetUpdate(net, "تصحيح", Convert.ToBase64String(o.RowVersion));
        Assert.Equal(ErrorType.Validation, (await f.Service.UpdateOperationNetAsync("invalid-direction", o.Id, request, ct)).Error.Type);
        Assert.False(f.Db.ChangeTracker.HasChanges());
        var corrected = Success(await f.Service.UpdateOperationNetAsync("new-fund", o.Id,
            request with { Fund = JahezMoneyFund.Cashbox, PaymentReference = "corrected-fund-reference" }, ct));
        Assert.Equal(net + 75, corrected.Remaining); Assert.Equal(JahezMoneyFund.Cashbox, corrected.Fund);
        Assert.Equal("corrected-fund-reference", corrected.PaymentReference);
        var movement = Assert.Single(await f.Db.JahezMoneyMovements.AsNoTracking().ToArrayAsync(ct));
        Assert.Equal(original.Id, movement.Id); Assert.Equal(original.OccurredAtUtc, movement.OccurredAtUtc);
        Assert.Equal(JahezMoneyFund.Cashbox, movement.Fund); Assert.Equal(corrected.PaymentReference, movement.Reference);
        Assert.Equal(0, Success(await f.Service.FundsV2Async(ct)).Single(x => x.Fund == originalFund).Balance);
    }

    [Fact]
    public async Task V2NetCorrectionSynchronizesPetroAppAndCashboxTreasuryBalances()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await EnableTreasury(f);
        var account = await f.Account("net-petro-treasury");
        var petro = await TreasuryAccount(f, account.SponsorId!.Value, TreasuryLocation.PetroApp);
        var cash = await TreasuryAccount(f, account.SponsorId.Value, TreasuryLocation.Supervisor);
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, fund: JahezMoneyFund.PetroApp), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var corrected = Success(await f.Service.UpdateOperationNetAsync("petro-net", o.Id, new(125, "تصحيح", Convert.ToBase64String(o.RowVersion)), ct));
        Assert.Equal(200, Success(await f.Service.TreasuryBalancesAsync(ct)).Single(x => x.SubAccountId == petro.Id).Balance);
        corrected = Success(await f.Service.UpdateOperationNetAsync("move-to-cash", o.Id,
            new(50, "تصحيح الجهة", Convert.ToBase64String(corrected.RowVersion), JahezMoneyFund.Cashbox, "cash-ref"), ct));
        var balances = Success(await f.Service.TreasuryBalancesAsync(ct));
        Assert.Equal(0, balances.Single(x => x.SubAccountId == petro.Id).Balance);
        Assert.Equal(125, balances.Single(x => x.SubAccountId == cash.Id).Balance);
        Assert.Equal(2, await f.Db.Set<TreasuryPosting>().CountAsync(x => x.Kind == TreasuryEntryKind.Reversal, ct));
        Assert.Single(await f.Db.JahezMoneyMovements.ToArrayAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2NetCorrectionProtectsPetroAppAccountantSnapshots(bool confirmed)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-petro-handover");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100, fund: JahezMoneyFund.PetroApp), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var handover = Success(await f.Service.SubmitCashboxHandoverV2Async("submit", new(new(2026, 10, 6), "تسليم"), ct));
        if (confirmed)
        {
            f.User.UserId = Guid.NewGuid();
            Success(await f.Service.ConfirmCashboxHandoverV2Async("confirm", handover.Id, CashboxConfirmation(handover), ct));
        }
        Assert.Equal(ErrorType.Conflict, (await f.Service.UpdateOperationNetAsync("locked", o.Id,
            new(125, "تصحيح", Convert.ToBase64String(o.RowVersion)), ct)).Error.Type);
        Assert.Equal(175, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Amount);
    }

    [Fact]
    public async Task V2NetCorrectionRollsBackAllWritesOnLateSqlFailure()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("LOGISTICS_JAHEZ_SQL_TESTS") == "1", "Enable disposable SQL tests.");
        await using var f = await Fixture.Create(true); var ct = TestContext.Current.CancellationToken;
        var account = await f.Account("net-rollback");
        var h = Success(await f.Service.HandoverAsync("start", new(account.Id, f.RiderId, At(1), "استلام", 0), ct));
        var draft = Success(await f.Service.CreateOperationAsync("draft", Settle(h.Id, 100), ct));
        var o = Success(await f.Service.ExecuteOperationAsync("execute", draft.Id, ct));
        var ledgerCount = await f.Db.JahezLedgerEntries.CountAsync(ct);
        await f.Db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER jahez_v2.FailNetCorrectionReceipt ON jahez_v2.CommandReceipts AFTER INSERT AS
            BEGIN
              IF EXISTS(SELECT 1 FROM inserted WHERE Operation='v2.operation.net.update')
                THROW 51000, 'Deliberate late failure in disposable test database.', 1;
            END;
            """, ct);
        Assert.True((await f.Service.UpdateOperationNetAsync("correct", o.Id, new(50, "تصحيح", Convert.ToBase64String(o.RowVersion)), ct)).IsFailure);
        Assert.Equal(100, (await f.Db.JahezOperations.AsNoTracking().SingleAsync(ct)).Net);
        Assert.Equal(175, (await f.Db.JahezMoneyMovements.AsNoTracking().SingleAsync(ct)).Amount);
        Assert.Equal(ledgerCount, await f.Db.JahezLedgerEntries.CountAsync(ct));
        Assert.False(await f.Db.JahezCommandReceipts.AnyAsync(x => x.CommandKey == "correct", ct));
        Assert.False(f.Db.ChangeTracker.HasChanges()); Assert.Null(f.Db.JahezNetCorrectionOperationId);
    }
}
