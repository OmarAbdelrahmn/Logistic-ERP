using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public async Task<Result<JahezOperation>> UpdateOperationPaymentMethodAsync(string key, Guid id,
        JahezOperationPaymentMethodUpdate request, CancellationToken ct = default)
    {
        try
        {
            // Check both permissions before ExecuteAsync can replay an earlier successful command.
            await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Disburse, ct);
            return await ExecuteAsync(key, "v2.operation.payment-method.update", new { id, request },
                JahezWorkflowPermissions.Settle, async () =>
                {
                    Reason(request.Reason);
                    Require(request.Fund is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank,
                        JahezErrors.Invalid("اختر الخزنة أو الحساب البنكي لصرف التصفية."));
                    Require(!string.IsNullOrWhiteSpace(request.PaymentReference) && request.PaymentReference.Length <= 500,
                        JahezErrors.Invalid("مرجع حركة المال الفعلية مطلوب، بحد 500 حرف."));
                    var operation = await db.JahezOperations.SingleOrDefaultAsync(x => x.Id == id, ct);
                    Require(operation is not null, JahezErrors.NotFound);
                    Require(operation!.Status == JahezOperationStatus.Executed
                        && operation.PaymentStatus == JahezOperationPaymentStatus.Paid
                        && operation.Remaining < 0 && !operation.TransferToRider
                        && operation.Fund is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank,
                        JahezErrors.Conflict("تعديل جهة الدفع متاح للتصفيات المنفذة المصروفة للمندوب فقط."));
                    Require(Convert.ToBase64String(operation.RowVersion) == request.RowVersion,
                        JahezErrors.Conflict("تغيرت التصفية؛ أعد تحميلها."));
                    var movement = await db.JahezMoneyMovements.SingleOrDefaultAsync(x => x.OperationId == id, ct);
                    Require(movement is not null && movement.Direction == JahezMoneyDirection.Disbursement
                        && movement.Fund == operation.Fund && movement.Reference == operation.PaymentReference
                        && movement.Amount == -operation.Remaining && movement.Purpose == operation.Purpose
                        && movement.HandoverId == operation.HandoverId,
                        JahezErrors.Conflict("حركة صرف التصفية غير متسقة؛ راجع السجل المالي."));
                    // A repeat with a new command key and identical values is a no-op.
                    if (operation.Fund == request.Fund && operation.PaymentReference == request.PaymentReference)
                        return operation;
                    Require(!await db.JahezCashboxHandoverMovements.AnyAsync(link => link.MoneyMovementId == movement!.Id
                        && db.JahezCashboxHandovers.Any(h => h.Id == link.CashboxHandoverId && h.Status != JahezCashboxHandoverStatus.Cancelled), ct),
                        JahezErrors.Conflict("حركة الصرف ضمن تسليم أموال معلق أو مؤكد ولا يمكن تعديل طريقة دفعها."));

                    db.Add(new JahezLedgerEntry
                    {
                        HandoverId = operation.HandoverId, Bucket = JahezLedgerBucket.Commission,
                        Kind = JahezLedgerKind.StatusChange, Amount = 0, SourceId = Guid.CreateVersion7(),
                        OccurredAtUtc = Now, Reason = request.Reason,
                        CalculationJson = JsonSerializer.Serialize(new
                        {
                            Action = "PaymentMethodUpdated", OperationId = id, MovementId = movement!.Id,
                            ActorUserId = Actor, PreviousFund = operation.Fund, Fund = request.Fund,
                            PreviousPaymentReference = operation.PaymentReference, request.PaymentReference
                        })
                    });
                    operation.Fund = request.Fund;
                    operation.PaymentReference = request.PaymentReference;
                    movement!.Fund = request.Fund;
                    movement.Reference = request.PaymentReference;
                    return operation;
                }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezOperation>(e.Error); }
    }
}
