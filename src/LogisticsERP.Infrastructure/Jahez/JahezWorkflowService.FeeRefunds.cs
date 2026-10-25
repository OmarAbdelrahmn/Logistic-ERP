using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private async Task<JahezFeeRefundPreview> FeeRefundPreview(JahezFeeRefundPreviewRequest request,
        DateTimeOffset at, CancellationToken ct)
    {
        var h = await Handover(request.HandoverId, ct);
        Require(h.EndedAtUtc is null, JahezErrors.Conflict("تم سحب الحساب بالفعل."));
        Require(at >= h.StartedAtUtc && at <= h.StartedAtUtc.AddDays(7),
            JahezErrors.Conflict("استرداد رسوم فتح الحساب متاح خلال سبعة أيام من تسليم الحساب فقط."));
        Require(!await db.JahezAccountFeeRefunds.AnyAsync(x => x.HandoverId == h.Id, ct),
            JahezErrors.Conflict("تم استرداد رسوم هذه الفترة بالفعل."));
        Require(!await db.JahezOperations.AnyAsync(x => x.HandoverId == h.Id && x.ExecutedAtUtc == null, ct)
            && !await db.Set<JahezApprovalRequest>().AnyAsync(x => x.HandoverId == h.Id && x.Status == JahezApprovalStatus.Pending, ct),
            JahezErrors.Conflict("أكمل العمليات والطلبات المعلقة قبل استرداد الرسوم."));
        await ValidateStopDate(h, at, ct);
        var fee = await db.JahezAccountFees.AsNoTracking().SingleOrDefaultAsync(x => x.HandoverId == h.Id, ct);
        Require(fee is not null, JahezErrors.NotFound);
        var paid = await db.JahezMoneyMovements.AsNoTracking()
            .Where(x => x.HandoverId == h.Id && x.Purpose == JahezMoneyPurpose.AccountFee && x.Direction == JahezMoneyDirection.Receipt)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        Require(paid > 0, JahezErrors.Conflict("لا توجد رسوم فتح حساب محصلة لاستردادها."));
        var through = JahezRules.RiyadhDate(at);
        var unpaidFee = await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, through, ct);
        Require(unpaidFee >= 0 && paid + unpaidFee == fee!.Amount - fee.WaivedAmount,
            JahezErrors.Conflict("رصيد الرسوم لا يطابق التحصيل؛ راجع السجل قبل الاسترداد."));
        var accrual = await Commission(h, through, ct);
        Require(accrual.Problems.Count == 0, JahezErrors.Conflict(string.Join(" ", accrual.Problems)));
        var unpaidCommission = await BucketBalance(h.Id, JahezLedgerBucket.Commission, through, ct);
        var calculated = JahezRules.Money(unpaidCommission + accrual.Amount);
        Require(calculated >= 0, JahezErrors.Conflict("رصيد العمولة يحتاج مراجعة قبل الاسترداد."));
        var deducted = request.WaiveCommission ? 0 : calculated;
        Require(paid > deducted, JahezErrors.Conflict("العمولة تساوي الرسوم المحصلة أو تتجاوزها؛ راجع التصفية أولاً."));
        return new(h.Id, h.StartedAtUtc.AddDays(7), paid, unpaidFee, calculated, request.WaiveCommission,
            deducted, JahezRules.Money(paid - deducted),
            JsonSerializer.Serialize(new { Accrual = JsonSerializer.Deserialize<JsonElement>(accrual.Evidence), UnpaidCommission = unpaidCommission }));
    }

    public Task<Result<JahezFeeRefundPreview>> PreviewFeeRefundAsync(JahezFeeRefundPreviewRequest request, CancellationToken ct = default) =>
        ReadAsync(JahezWorkflowPermissions.FeeApprove, () => FeeRefundPreview(request, Now, ct), ct);

    public async Task<Result<JahezAccountFeeRefund>> RefundFeeAsync(string key, JahezFeeRefundCreate request, CancellationToken ct = default)
    {
        // Check every permission before idempotency replay as well as before a new payout.
        try
        {
            await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Disburse, ct);
            if (request.WaiveCommission) await AuthorizeAsync(JahezWorkflowPermissions.CommissionApprove, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezAccountFeeRefund>(e.Error); }
        return await ExecuteAsync(key, "v2.fee.refund", request, JahezWorkflowPermissions.FeeApprove, async () =>
        {
            Reason(request.Reason); ValidateMoneyReference(request.Reference);
            Require(PlatformAccountDayAllocationService.Valid(request.DayUsage), JahezErrors.Invalid("Invalid day usage figures."));
            var at = Now;
            var p = await FeeRefundPreview(new(request.HandoverId, request.WaiveCommission), at, ct);
            var h = await Handover(request.HandoverId, ct);
            var fee = await db.JahezAccountFees.SingleAsync(x => x.HandoverId == h.Id, ct);
            var movement = await StageMoney(h, JahezMoneyFund.Cashbox, JahezMoneyDirection.Disbursement,
                JahezMoneyPurpose.AccountFeeRefund, p.RefundAmount, request.Reference, request.Reason, null, null, ct);
            var refund = new JahezAccountFeeRefund { HandoverId = h.Id, AccountFeeId = fee.Id, MoneyMovementId = movement.Id,
                GrossFeeRefund = p.GrossFeeRefund, CancelledUnpaidFee = p.CancelledUnpaidFee,
                WaiveCommission = p.WaiveCommission, CalculatedCommission = p.CalculatedCommission,
                DeductedCommission = p.DeductedCommission, RefundAmount = p.RefundAmount, CalculationJson = p.CalculationJson,
                ActorUserId = Actor, OccurredAtUtc = at, Reference = request.Reference, Reason = request.Reason };
            db.Add(refund);
            // Cancel the fee obligation and reverse the collected fee; the fee balance becomes zero.
            Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Adjustment,
                -(p.GrossFeeRefund + p.CancelledUnpaidFee), refund.Id, request.Reason);
            Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Payment, p.GrossFeeRefund, refund.Id, request.Reason);
            await PostCommission(h, JahezRules.RiyadhDate(at), Guid.CreateVersion7(), request.Reason, ct);
            if (p.CalculatedCommission > 0)
                Entry(h, JahezLedgerBucket.Commission, request.WaiveCommission ? JahezLedgerKind.Adjustment : JahezLedgerKind.Payment,
                    -p.CalculatedCommission, refund.Id, request.Reason);
            await WithdrawOperationally(h, at, request.Reason, ct, request.DayUsage);
            return refund;
        }, ct);
    }

    public Task<Result<IReadOnlyList<JahezAccountFeeRefund>>> FeeRefundsAsync(Guid handoverId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<JahezAccountFeeRefund>>(PermissionKeys.Jahez.Read, async () =>
        {
            await Handover(handoverId, ct);
            return await db.JahezAccountFeeRefunds.AsNoTracking().Where(x => x.HandoverId == handoverId).ToArrayAsync(ct);
        }, ct);
}
