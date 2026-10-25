using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private sealed record RiderDebtState(JahezTransferredDebt Debt, JahezOperation Operation, JahezAccountSession Handover)
    {
        public decimal DashboardRemaining => Math.Max(0m, Math.Max(0m, Operation.Net) - Debt.PaidAmount);
        public decimal CommissionRemaining => Debt.Amount - Debt.PaidAmount - DashboardRemaining;
    }

    private static JahezSettlementAmounts DebtAmounts(IEnumerable<RiderDebtState> states) => new(
        states.Sum(x => x.DashboardRemaining), states.Sum(x => x.CommissionRemaining),
        states.Sum(x => x.Debt.Amount - x.Debt.PaidAmount));

    private static void DebtMoney(decimal amount) => Require(amount is >= 0 and <= 1_000_000_000m && amount == JahezRules.Money(amount),
        JahezErrors.Invalid("أدخل مبلغاً غير سالب بدقة هللتين ضمن الحد المسموح."));

    private async Task<List<RiderDebtState>> UnpaidRiderDebtStates(Guid riderId, CancellationToken ct)
    {
        var debts = await db.JahezTransferredDebts.Where(x => x.RiderProfileId == riderId && x.PaidAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id).ToArrayAsync(ct);
        Require(debts.Length > 0, JahezErrors.Conflict("لا توجد مديونيات مرحلة غير مسددة لهذا المندوب."));
        var operationIds = debts.Select(x => x.OperationId).ToArray();
        var operations = await db.JahezOperations.IgnoreQueryFilters().Where(x => operationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var handoverIds = operations.Values.Select(x => x.HandoverId).ToArray();
        var handovers = await db.JahezAccountSessions.IgnoreQueryFilters().Where(x => handoverIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var result = new List<RiderDebtState>();
        foreach (var debt in debts)
        {
            Require(operations.TryGetValue(debt.OperationId, out var operation) && operation.TransferToRider
                && operation.Status == JahezOperationStatus.Executed && operation.PaymentStatus == JahezOperationPaymentStatus.Transferred
                && operation.Remaining == debt.Amount && debt.Amount == operation.Net + operation.Commission
                && debt.PaidAmount >= 0 && debt.PaidAmount < debt.Amount,
                JahezErrors.Conflict("بيانات أصل المديونية أو الرصيد غير متسقة؛ راجع السجل المالي."));
            Require(handovers.TryGetValue(operation!.HandoverId, out var handover) && handover.RiderProfileId == riderId,
                JahezErrors.Conflict("بيانات المندوب الأصلي للمديونية غير متسقة؛ راجع السجل."));
            var state = new RiderDebtState(debt, operation, handover!);
            Require(state.CommissionRemaining >= 0, JahezErrors.Conflict("توزيع المديونية غير متسق."));
            result.Add(state);
        }
        return result;
    }

    private async Task<Result<T>> ExecuteDebtRepaymentAsync<T>(string key, string operation, object payload,
        Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await AuthorizeAsync(JahezWorkflowPermissions.Repay, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.Receive, ct);
            return await ExecuteAsync(key, operation, payload, JahezWorkflowPermissions.Repay, action, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<T>(e.Error); }
    }

    private async Task<JahezMoneyMovement> StageDebtPayment(RiderDebtState state, decimal dashboard, decimal commission,
        JahezMoneyRecord request, CancellationToken ct)
    {
        var debt = state.Debt;
        Require(dashboard >= 0 && commission >= 0 && dashboard <= state.DashboardRemaining && commission <= state.CommissionRemaining
            && dashboard + commission > 0, JahezErrors.Conflict("توزيع الدفعة يتجاوز الرصيد غير المسدد."));
        var movement = await StageMoney(state.Handover, request.Fund, JahezMoneyDirection.Receipt, request.Purpose,
            dashboard + commission, request.Reference, request.Reason, null, debt.Id, ct);
        movement.DashboardDebtPaid = dashboard;
        movement.CommissionPaid = commission;
        debt.PaidAmount += movement.Amount;
        if (debt.PaidAmount == debt.Amount) { debt.PaidAtUtc = Now; debt.PaidByUserId = Actor; }
        return movement;
    }

    public Task<Result<JahezRiderDebtPartialRepaymentResponse>> PartiallyRepayRiderTransferredDebtsAsync(
        string key, Guid riderId, JahezRiderDebtPartialRepayment request, CancellationToken ct = default) =>
        ExecuteDebtRepaymentAsync(key, "v2.rider-debts.partial-repay", new { riderId, request }, async () =>
        {
            Reason(request.Reason); ValidateMoneyReference(request.Reference); DebtMoney(request.Amount);
            Require(request.Amount > 0, JahezErrors.Invalid("أدخل مبلغ السداد الفعلي."));
            var states = await UnpaidRiderDebtStates(riderId, ct);
            var before = DebtAmounts(states);
            Require(request.ExpectedAmounts == before, JahezErrors.Conflict("تغيرت مبالغ المديونية؛ أعد تحميلها قبل السداد."));
            Require(request.Amount <= before.Total, JahezErrors.Conflict("الدفعة تتجاوز إجمالي المديونية غير المسددة."));
            var dashboardBudget = Math.Min(request.Amount, before.DashboardDebt);
            var commissionBudget = request.Amount - dashboardBudget;
            var dashboardTotal = dashboardBudget;
            var commissionTotal = commissionBudget;
            var allocations = new List<JahezRiderDebtPaymentAllocation>();
            foreach (var state in states)
            {
                var dashboard = Math.Min(state.DashboardRemaining, dashboardBudget);
                var commission = Math.Min(state.CommissionRemaining, commissionBudget);
                dashboardBudget -= dashboard; commissionBudget -= commission;
                if (dashboard + commission == 0) continue;
                var movement = await StageDebtPayment(state, dashboard, commission,
                    new(request.Fund, request.Reference, request.Reason), ct);
                allocations.Add(new(state.Debt.Id, movement.Id, dashboard, commission,
                    state.Debt.Amount - state.Debt.PaidAmount, state.Debt.PaidAtUtc.HasValue));
            }
            var remaining = DebtAmounts(states);
            if (remaining.Total == 0) await WorkflowDebtNotifications(riderId, states[0].Debt.Id, true, ct);
            return new JahezRiderDebtPartialRepaymentResponse(riderId, request.Amount, dashboardTotal, commissionTotal, remaining, allocations);
        }, ct);

    public async Task<Result<JahezRiderDebtCommissionUpdateResponse>> UpdateRiderDebtCommissionAsync(
        string key, Guid riderId, JahezRiderDebtCommissionUpdate request, CancellationToken ct = default)
    {
        try
        {
            await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
            await AuthorizeAsync(JahezWorkflowPermissions.AdjustmentApprove, ct);
            return await ExecuteAsync(key, "v2.rider-debts.commission.update", new { riderId, request },
                JahezWorkflowPermissions.AdjustmentApprove, async () =>
                {
                    Reason(request.Reason); DebtMoney(request.Commission); DebtMoney(request.ExpectedCommission);
                    var states = await UnpaidRiderDebtStates(riderId, ct);
                    var before = DebtAmounts(states);
                    var previous = states.Sum(x => x.Operation.Commission);
                    Require(request.ExpectedAmounts == before && request.ExpectedCommission == previous,
                        JahezErrors.Conflict("تغيرت مبالغ المديونية أو العمولة؛ أعد تحميلها."));
                    var reduction = previous - request.Commission;
                    Require(reduction <= before.Commission, JahezErrors.Conflict("لا يمكن خفض العمولة إلى أقل من الجزء المسدد أو المقاص بالفعل؛ يلزم إجراء رد مستقل."));
                    var remainingChange = Math.Abs(reduction);
                    var adjusted = new List<Guid>();
                    // Reduce newest unpaid commissions first; increases go to the oldest debt with capacity.
                    var ordered = reduction > 0 ? states.AsEnumerable().Reverse() : states;
                    foreach (var state in ordered)
                    {
                        if (remainingChange == 0) break;
                        var capacity = reduction > 0 ? state.CommissionRemaining
                            : Math.Min(1_000_000_000m - state.Operation.Commission, 1_000_000_000m - state.Debt.Amount);
                        var change = Math.Min(remainingChange, Math.Max(0, capacity));
                        if (change == 0) continue;
                        var delta = reduction > 0 ? -change : change;
                        var o = state.Operation;
                        db.Add(new JahezLedgerEntry { HandoverId = o.HandoverId, Bucket = JahezLedgerBucket.Commission,
                            Kind = JahezLedgerKind.StatusChange, Amount = 0, SourceId = Guid.CreateVersion7(), OccurredAtUtc = Now,
                            Reason = request.Reason, CalculationJson = JsonSerializer.Serialize(new
                            {
                                Action = "TransferredDebtCommissionCorrected", RiderProfileId = riderId, DebtId = state.Debt.Id,
                                OperationId = o.Id, ActorUserId = Actor, PreviousCommission = o.Commission,
                                Commission = o.Commission + delta, PreviousAmount = state.Debt.Amount,
                                Amount = state.Debt.Amount + delta, state.Debt.PaidAmount, PreviousApprovedRequestId = o.ApprovedRequestId
                            }) });
                        db.JahezDebtCommissionCorrectionOperationIds.Add(o.Id);
                        o.Commission += delta; o.Remaining += delta; o.ApprovedRequestId = null;
                        state.Debt.Amount += delta;
                        if (state.Debt.Amount == state.Debt.PaidAmount) { state.Debt.PaidAtUtc = Now; state.Debt.PaidByUserId = Actor; }
                        adjusted.Add(state.Debt.Id); remainingChange -= change;
                    }
                    Require(remainingChange == 0, JahezErrors.Invalid("العمولة المطلوبة تتجاوز حدود المبالغ للمديونيات الحالية."));
                    var remaining = DebtAmounts(states);
                    if (remaining.Total == 0) await WorkflowDebtNotifications(riderId, states[0].Debt.Id, true, ct, adjusted: true);
                    return new JahezRiderDebtCommissionUpdateResponse(riderId, previous, request.Commission, remaining, adjusted);
                }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezRiderDebtCommissionUpdateResponse>(e.Error); }
        finally { db.JahezDebtCommissionCorrectionOperationIds.Clear(); }
    }
}
