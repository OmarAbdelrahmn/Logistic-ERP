using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private async Task ValidateCommissionStart(JahezAccountSession h, DateOnly start, DateOnly through,
        Guid? excludedOperationId, CancellationToken ct)
    {
        Require(start >= JahezRules.RiyadhDate(h.StartedAtUtc) && start <= through,
            JahezErrors.Invalid("بداية احتساب العمولة يجب أن تكون داخل فترة الاستخدام وألا تتجاوز نهاية التصفية."));
        var previousEnd = await db.JahezOperations.Where(x => x.HandoverId == h.Id && x.Id != excludedOperationId
            && x.Status == JahezOperationStatus.Executed && x.Mode != JahezOperationMode.Withdraw && x.ThroughDate <= through)
            .Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
        Require(!previousEnd.HasValue || start > previousEnd.Value,
            JahezErrors.Conflict("بداية احتساب العمولة تتداخل مع تصفية سابقة."));
    }

    public async Task<Result<JahezOperation>> UpdateOperationCommissionStartAsync(string key, Guid id,
        JahezOperationCommissionStartUpdate request, CancellationToken ct = default)
    {
        try
        {
            // Recheck all permissions even when returning an idempotency receipt.
            await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.AdjustmentApprove, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Receive, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Disburse, ct);
            return await ExecuteAsync(key, "v2.operation.commission-start.update", new { id, request },
                JahezWorkflowPermissions.AdjustmentApprove, async () =>
                {
                    Reason(request.Reason);
                    var o = await db.JahezOperations.SingleOrDefaultAsync(x => x.Id == id, ct);
                    Require(o is not null, JahezErrors.NotFound);
                    Require(o!.Mode != JahezOperationMode.Withdraw && o.Purpose == JahezMoneyPurpose.Settlement
                        && !o.TransferToRider && o.Status is JahezOperationStatus.Ready or JahezOperationStatus.Executed,
                        JahezErrors.Conflict("تصحيح البداية متاح لتصفية جاهزة أو منفذة دون ترحيل؛ أكمل المراجعة المفتوحة أولاً."));
                    Require(Convert.ToBase64String(o.RowVersion) == request.RowVersion,
                        JahezErrors.Conflict("تغيرت التصفية؛ أعد تحميلها."));
                    var h = await Handover(o.HandoverId, ct);
                    await Account(h.PlatformRiderAccountId, ct);
                    await ValidateCommissionStart(h, request.StartDate, o.ThroughDate, o.Id, ct);
                    var calculation = await Commission(h, o.ThroughDate, ct, request.StartDate);
                    Require(calculation.Through == o.ThroughDate && calculation.Problems.Count == 0,
                        JahezErrors.Conflict("تعذر احتساب العمولة للفترة كاملة. " + string.Join(" ", calculation.Problems)));
                    var executed = o.Status == JahezOperationStatus.Executed;
                    Require(executed || !h.CommissionPostedThrough.HasValue || request.StartDate > h.CommissionPostedThrough.Value,
                        JahezErrors.Conflict("بداية التصفية الجاهزة يجب أن تلي آخر عمولة مرحلة."));
                    // A paid operation has already consumed its unpaid balance; retain the balance in its saved evidence.
                    var unpaid = executed ? SavedUnpaidCommission(o) : await BucketBalance(h.Id, JahezLedgerBucket.Commission, o.ThroughDate, ct);
                    var commission = JahezRules.Money(Math.Max(0, unpaid + calculation.Amount));
                    var calculationJson = JsonSerializer.Serialize(new { Accrual = JsonSerializer.Deserialize<JsonElement>(calculation.Evidence), UnpaidCommission = unpaid });
                    var remaining = JahezSettlementRules.Remaining(o.Net, commission);
                    WorkflowMoney(commission, true); WorkflowMoney(remaining);
                    var direction = JahezSettlementRules.Direction(remaining);
                    var fund = request.Fund ?? o.Fund;
                    var reference = request.PaymentReference ?? o.PaymentReference;
                    Require(!fund.HasValue || Enum.IsDefined(fund.Value), JahezErrors.Invalid("جهة المال غير صالحة."));
                    Require(reference is null || reference.Length <= 500, JahezErrors.Invalid("مرجع الدفع طويل."));
                    if (direction != JahezMoneyDirection.None && (executed || fund.HasValue))
                        Require(fund.HasValue && JahezSettlementRules.Allows(fund.Value, direction, o.Purpose),
                            JahezErrors.Invalid("اختر جهة مال تسمح بالقبض أو الصرف بعد تصحيح العمولة."));
                    if (direction != JahezMoneyDirection.None) ValidateMoneyReference(reference);
                    if (!executed && remaining == 0) fund = null;

                    var movement = await db.JahezMoneyMovements.SingleOrDefaultAsync(x => x.OperationId == id, ct);
                    Require(executed || movement is null, JahezErrors.Conflict("توجد حركة مال لعملية غير منفذة."));
                    if (executed)
                    {
                        Require(o.ExecutedAtUtc.HasValue && o.PaymentStatus is JahezOperationPaymentStatus.Paid or JahezOperationPaymentStatus.None,
                            JahezErrors.Conflict("حالة دفع التصفية غير متسقة."));
                        Require(movement is null ? o.Remaining == 0 : movement.HandoverId == h.Id
                            && movement.RiderProfileId == h.RiderProfileId && movement.Purpose == o.Purpose
                            && movement.Fund == o.Fund && movement.Reference == o.PaymentReference
                            && movement.Amount == Math.Abs(o.Remaining) && movement.Direction == JahezSettlementRules.Direction(o.Remaining),
                            JahezErrors.Conflict("حركة مال التصفية غير متسقة؛ راجع السجل المالي."));
                    }
                    if (o.CommissionStartDate == request.StartDate && o.Commission == commission
                        && o.CalculatedCommission == commission && o.CalculationJson == calculationJson
                        && o.Fund == fund && o.PaymentReference == reference)
                        return o;
                    if (movement is not null)
                        Require(!await db.JahezCashboxHandoverMovements.AnyAsync(link => link.MoneyMovementId == movement.Id
                            && db.JahezCashboxHandovers.Any(x => x.Id == link.CashboxHandoverId && x.Status != JahezCashboxHandoverStatus.Cancelled), ct),
                            JahezErrors.Conflict("الحركة ضمن تسليم أموال معلق أو مؤكد؛ لا يمكن تغيير قيم التسليم المسجلة."));

                    var correctionId = Guid.CreateVersion7();
                    var evidence = JsonSerializer.Serialize(new
                    {
                        Action = "CommissionStartCorrected", OperationId = id, ActorUserId = Actor,
                        PreviousStartDate = o.CommissionStartDate, PreviousCalculationJson = o.CalculationJson,
                        StartDate = request.StartDate, EndDate = o.ThroughDate,
                        PreviousCommission = o.Commission, Commission = commission,
                        PreviousCalculatedCommission = o.CalculatedCommission,
                        PreviousRemaining = o.Remaining, Remaining = remaining,
                        PreviousFund = o.Fund, Fund = fund, PreviousPaymentReference = o.PaymentReference, PaymentReference = reference
                    });
                    db.Add(new JahezLedgerEntry { HandoverId = h.Id, Bucket = JahezLedgerBucket.Commission,
                        Kind = JahezLedgerKind.StatusChange, Amount = 0, SourceId = correctionId,
                        OccurredAtUtc = Now, Reason = request.Reason, CalculationJson = evidence });
                    if (executed)
                    {
                        // Append the accrual/payment difference; do not rewrite prior ledger evidence or advance the session cursor.
                        var delta = commission - o.CalculatedCommission;
                        if (delta != 0)
                        {
                            Entry(h, JahezLedgerBucket.Commission, JahezLedgerKind.Adjustment, delta, correctionId, request.Reason);
                            Entry(h, JahezLedgerBucket.Commission, JahezLedgerKind.Payment, -delta, correctionId, request.Reason);
                        }
                        if (movement is null && direction != JahezMoneyDirection.None)
                        {
                            movement = new JahezMoneyMovement { HandoverId = h.Id, RiderProfileId = h.RiderProfileId,
                                OperationId = id, ActorUserId = Actor, OccurredAtUtc = o.ExecutedAtUtc!.Value,
                                Purpose = o.Purpose, Reason = request.Reason };
                            db.Add(movement);
                        }
                        if (movement is not null)
                        {
                            movement.Amount = Math.Abs(remaining); movement.Direction = direction;
                            movement.Fund = fund!.Value; movement.Reference = reference!;
                        }
                        o.PaymentStatus = direction == JahezMoneyDirection.None ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid;
                        db.JahezCommissionStartCorrectionOperationId = id;
                    }
                    o.CommissionStartDate = request.StartDate;
                    o.Commission = commission; o.CalculatedCommission = commission; o.Remaining = remaining;
                    o.CalculationJson = calculationJson;
                    o.Fund = fund; o.PaymentReference = reference; o.ApprovedRequestId = null;
                    return o;
                }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezOperation>(e.Error); }
        finally { db.JahezCommissionStartCorrectionOperationId = null; }
    }

    private static decimal SavedUnpaidCommission(JahezOperation operation)
    {
        try
        {
            using var document = JsonDocument.Parse(operation.CalculationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("UnpaidCommission", out var value)
                && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var unpaid)
                && unpaid is >= -1_000_000_000m and <= 1_000_000_000m) return unpaid;
        }
        catch (JsonException) { }
        throw new JahezBusinessException(JahezErrors.Conflict("بيانات احتساب التصفية الأصلية غير صالحة؛ راجع السجل المالي."));
    }
}
