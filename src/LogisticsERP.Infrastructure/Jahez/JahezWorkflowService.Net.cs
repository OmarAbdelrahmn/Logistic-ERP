using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public async Task<Result<JahezOperation>> UpdateOperationNetAsync(string key, Guid id,
        JahezOperationNetUpdate request, CancellationToken ct = default)
    {
        try
        {
            // A net correction can switch between receipt and payout. Check every permission before replay.
            await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.AdjustmentApprove, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Receive, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Disburse, ct);
            // Preserve the original payload shape for retries from clients that omit the new optional fields.
            object commandRequest = request.Fund is null && request.PaymentReference is null
                ? new { request.Net, request.Reason, request.RowVersion } : request;
            return await ExecuteAsync(key, "v2.operation.net.update", new { id, request = commandRequest },
                JahezWorkflowPermissions.AdjustmentApprove, async () =>
                {
                    Reason(request.Reason);
                    Require(request.Net is >= -1_000_000_000m and <= 1_000_000_000m,
                        JahezErrors.Invalid("المبلغ خارج الحدود المسموحة."));
                    var o = await db.JahezOperations.SingleOrDefaultAsync(x => x.Id == id, ct);
                    Require(o is not null, JahezErrors.NotFound);
                    Require(o!.Status == JahezOperationStatus.Executed && o.ExecutedAtUtc.HasValue
                        && o.Mode != JahezOperationMode.Withdraw && o.Purpose == JahezMoneyPurpose.Settlement
                        && !o.TransferToRider && o.Fund.HasValue && Enum.IsDefined(o.Fund.Value)
                        && o.Remaining == JahezSettlementRules.Remaining(o.Net, o.Commission)
                        && o.PaymentStatus == (o.Remaining == 0 ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid),
                        JahezErrors.Conflict("تعديل صافي المبلغ متاح للتصفية المنفذة ذات حركة مالية دون ترحيل فقط."));
                    Require(Convert.ToBase64String(o.RowVersion) == request.RowVersion,
                        JahezErrors.Conflict("تغيرت التصفية؛ أعد تحميلها."));
                    var h = await Handover(o.HandoverId, ct);
                    await Account(h.PlatformRiderAccountId, ct);
                    var movement = await db.JahezMoneyMovements.SingleOrDefaultAsync(x => x.OperationId == id, ct);
                    Require(movement is not null && movement.HandoverId == o.HandoverId
                        && movement.RiderProfileId == h.RiderProfileId
                        && movement.TransferredDebtId == null && movement.Fund == o.Fund
                        && movement.Purpose == o.Purpose && movement.Reference == o.PaymentReference
                        && movement.Amount == Math.Abs(o.Remaining)
                        && movement.Direction == JahezSettlementRules.Direction(o.Remaining),
                        JahezErrors.Conflict("حركة مال التصفية غير متسقة؛ راجع السجل المالي."));
                    var m = movement!;
                    Require(!await db.JahezCashboxHandoverMovements.AnyAsync(link => link.MoneyMovementId == m.Id
                        && db.JahezCashboxHandovers.Any(h => h.Id == link.CashboxHandoverId && h.Status != JahezCashboxHandoverStatus.Cancelled), ct)
                        && !await db.Set<TreasuryHandoverPosting>().AnyAsync(link =>
                            db.Set<TreasuryPosting>().Any(p => p.Id == link.PostingId && p.MoneyMovementId == m.Id)
                            && db.JahezCashboxHandovers.Any(h => h.Id == link.HandoverId && h.Status != JahezCashboxHandoverStatus.Cancelled), ct),
                        JahezErrors.Conflict("التصفية ضمن تسليم أموال معلق أو مؤكد؛ لا يمكن تغيير صافي المبلغ."));

                    var net = JahezRules.Money(request.Net);
                    var remaining = JahezSettlementRules.Remaining(net, o.Commission);
                    WorkflowMoney(remaining);
                    var direction = JahezSettlementRules.Direction(remaining);
                    var fund = request.Fund ?? o.Fund!.Value;
                    var reference = request.PaymentReference ?? o.PaymentReference;
                    Require(Enum.IsDefined(fund), JahezErrors.Invalid("جهة المال غير صالحة."));
                    Require(reference is null || reference.Length <= 500, JahezErrors.Invalid("مرجع الدفع طويل."));
                    if (direction != JahezMoneyDirection.None)
                    {
                        Require(JahezSettlementRules.Allows(fund, direction, o.Purpose),
                            JahezErrors.Invalid("اختر جهة مالية تسمح بالقبض أو الصرف بعد تعديل الصافي."));
                        ValidateMoneyReference(reference);
                    }
                    if (net == o.Net && fund == o.Fund && reference == o.PaymentReference) return o;
                    var evidence = JsonSerializer.Serialize(new
                    {
                        Action = "SettlementNetCorrected", OperationId = id, MovementId = m.Id, ActorUserId = Actor,
                        PreviousNet = o.Net, Net = net, o.Commission, o.CalculatedCommission,
                        PreviousRemaining = o.Remaining, Remaining = remaining,
                        PreviousAmount = m.Amount, Amount = Math.Abs(remaining),
                        PreviousDirection = m.Direction, Direction = direction, PreviousApprovedRequestId = o.ApprovedRequestId,
                        PreviousFund = o.Fund, Fund = fund, PreviousPaymentReference = o.PaymentReference, PaymentReference = reference
                    });
                    db.Add(new JahezLedgerEntry { HandoverId = o.HandoverId, Bucket = JahezLedgerBucket.Commission,
                        Kind = JahezLedgerKind.StatusChange, Amount = 0, SourceId = Guid.CreateVersion7(),
                        OccurredAtUtc = Now, Reason = request.Reason, CalculationJson = evidence });
                    o.Net = net;
                    o.Remaining = remaining;
                    o.PaymentStatus = direction == JahezMoneyDirection.None ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid;
                    o.ApprovedRequestId = null;
                    o.Fund = fund; o.PaymentReference = reference;
                    m.Amount = Math.Abs(remaining);
                    m.Direction = direction;
                    m.Fund = fund; m.Reference = reference!;
                    db.JahezNetCorrectionOperationId = id;
                    return o;
                }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezOperation>(e.Error); }
        finally { db.JahezNetCorrectionOperationId = null; }
    }
}
