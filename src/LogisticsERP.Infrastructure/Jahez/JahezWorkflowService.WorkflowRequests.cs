using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<JahezWorkflowApproval>> CreateWorkflowRequestAsync(string key, JahezWorkflowRequestCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.request.create", request, Enum.IsDefined(request.Kind) ? JahezWorkflowPermissions.Request(request.Kind) : PermissionKeys.Jahez.Read, async () =>
        {
            Require(Enum.IsDefined(request.Kind), JahezErrors.Invalid("نوع الطلب غير صالح."));
            Reason(request.Reason);
            var h = await Handover(request.HandoverId, ct);
            var entity = new JahezApprovalRequest { HandoverId = h.Id, Kind = request.Kind,
                Status = JahezApprovalStatus.Pending, RequestedByUserId = Actor, Reason = request.Reason,
                OperationId = request.OperationId, TargetAccountId = request.TargetAccountId,
                WaiverAmount = JahezRules.Money(request.WaiverAmount), FromDate = request.FromDate, ToDate = request.ToDate,
                EffectiveAtUtc = request.EffectiveAtUtc?.ToUniversalTime(), AdjustmentTarget = request.AdjustmentTarget,
                ProposedNet = request.ProposedNet.HasValue ? JahezRules.Money(request.ProposedNet.Value) : null,
                ProposedCommission = request.ProposedCommission.HasValue ? JahezRules.Money(request.ProposedCommission.Value) : null,
                DebtAdjustment = request.DebtAdjustment.HasValue ? JahezRules.Money(request.DebtAdjustment.Value) : null };
            if (request.Kind is JahezApprovalKind.ResetAccount || request.Kind == JahezApprovalKind.Adjustment && request.AdjustmentTarget == JahezAdjustmentTarget.Settlement)
            {
                Require(request.OperationId.HasValue, JahezErrors.Invalid("حدد عملية التصفية."));
                var o = await db.Set<JahezOperation>().SingleOrDefaultAsync(x => x.Id == request.OperationId && x.HandoverId == h.Id, ct);
                Require(o is not null && o.Status == JahezOperationStatus.Ready && o.Mode != JahezOperationMode.Withdraw,
                    JahezErrors.Conflict("العملية غير متاحة للمراجعة."));
                if (request.Kind == JahezApprovalKind.ResetAccount)
                    Require(o!.TransferToRider && o.Mode == JahezOperationMode.SettleAndWithdraw && o.Remaining > 0,
                        JahezErrors.Invalid("اختر عملية تصفية وسحب مع ترحيل كامل المبلغ الموجب."));
                if (entity.ProposedNet.HasValue) WorkflowMoney(entity.ProposedNet.Value);
                if (entity.ProposedCommission.HasValue) WorkflowMoney(entity.ProposedCommission.Value, true);
                o!.Status = JahezOperationStatus.UnderReview;
            }
            else
            {
                Require(request.OperationId is null, JahezErrors.Invalid("هذا النوع لا يرتبط بعملية تصفية."));
                switch (request.Kind)
                {
                    case JahezApprovalKind.FeeException:
                        Require(entity.WaiverAmount > 0 && entity.WaiverAmount <= JahezRules.AccountFee, JahezErrors.Invalid("مبلغ الإعفاء غير صالح."));
                        break;
                    case JahezApprovalKind.FreeSwitch:
                        Require(h.EndedAtUtc is null && request.TargetAccountId.HasValue && request.TargetAccountId != h.PlatformRiderAccountId
                            && entity.EffectiveAtUtc.HasValue && entity.EffectiveAtUtc >= h.StartedAtUtc && entity.EffectiveAtUtc <= Now
                            && entity.WaiverAmount >= 0 && entity.WaiverAmount <= JahezRules.AccountFee,
                            JahezErrors.Invalid("حدد الحساب البديل ووقت التبديل وقيمة الإعفاء من رسومه."));
                        await Account(request.TargetAccountId!.Value, ct);
                        break;
                    case JahezApprovalKind.PercentageCommission:
                        Require(entity.FromDate.HasValue && entity.ToDate.HasValue && entity.FromDate >= h.CommissionStartsOn
                            && entity.ToDate >= entity.FromDate && entity.ToDate.Value.DayNumber - entity.FromDate.Value.DayNumber <= 731,
                            JahezErrors.Invalid("فترة عمولة النسبة غير صالحة."));
                        break;
                    case JahezApprovalKind.Adjustment:
                        Require(entity.AdjustmentTarget == JahezAdjustmentTarget.AdministrativeDebt && entity.DebtAdjustment.HasValue
                            && entity.DebtAdjustment != 0, JahezErrors.Invalid("حدد حركة تعديل المديونية الإدارية."));
                        WorkflowMoney(entity.DebtAdjustment!.Value);
                        break;
                    default: throw new JahezBusinessException(JahezErrors.Invalid("بيانات الطلب غير صالحة."));
                }
            }
            db.Add(entity);
            return new JahezWorkflowApproval(entity, []);
        }, ct);

    public async Task<Result<JahezWorkflowApproval>> DecideWorkflowRequestAsync(string key, Guid id, JahezWorkflowDecision request, CancellationToken ct = default)
    {
        try
        {
            await AuthorizeAsync(PermissionKeys.Jahez.Read, ct);
            var kind = await db.Set<JahezApprovalRequest>().AsNoTracking().Where(x => x.Id == id).Select(x => (JahezApprovalKind?)x.Kind).SingleOrDefaultAsync(ct);
            Require(kind.HasValue, JahezErrors.NotFound);
            return await ExecuteAsync(key, "v2.request.decide", new { id, request }, JahezWorkflowPermissions.Approve(kind!.Value), async () =>
            {
                Reason(request.Reason);
                var entity = await db.Set<JahezApprovalRequest>().SingleAsync(x => x.Id == id, ct);
                Require(entity.Status == JahezApprovalStatus.Pending, JahezErrors.Conflict("تم البت في الطلب بالفعل."));
                Require(entity.RequestedByUserId != Actor, JahezErrors.Forbidden);
                var h = await Handover(entity.HandoverId, ct);
                var operation = entity.OperationId.HasValue ? await db.Set<JahezOperation>().SingleAsync(x => x.Id == entity.OperationId, ct) : null;
                if (operation is not null)
                {
                    Require(operation.Status == JahezOperationStatus.UnderReview, JahezErrors.Conflict("العملية ليست قيد المراجعة."));
                    operation.Status = JahezOperationStatus.Ready;
                }
                if (request.Approve)
                {
                    switch (entity.Kind)
                    {
                        case JahezApprovalKind.FeeException:
                            var fee = await db.Set<JahezAccountFee>().SingleAsync(x => x.HandoverId == h.Id, ct);
                            var unpaid = await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, Today, ct);
                            Require(entity.WaiverAmount <= unpaid && fee.WaivedAmount + entity.WaiverAmount <= fee.Amount,
                                JahezErrors.Conflict("الإعفاء يتجاوز الرسوم غير المدفوعة."));
                            fee.WaivedAmount += entity.WaiverAmount; fee.ApprovalRequestId = entity.Id;
                            Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Adjustment, -entity.WaiverAmount, entity.Id, request.Reason);
                            break;
                        case JahezApprovalKind.FreeSwitch:
                            await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
                            await AuthorizeAsync(PermissionKeys.Jahez.HandoversCreate, ct);
                            Require(!await db.Set<JahezOperation>().AnyAsync(x => x.HandoverId == h.Id && x.ExecutedAtUtc == null, ct),
                                JahezErrors.Conflict("أكمل العملية المفتوحة قبل تبديل الحساب."));
                            await WithdrawOperationally(h, entity.EffectiveAtUtc!.Value, entity.Reason, ct);
                            db.Add(new JahezOperation { HandoverId = h.Id, Mode = JahezOperationMode.Withdraw,
                                Status = JahezOperationStatus.Executed, PaymentStatus = JahezOperationPaymentStatus.Deferred,
                                ThroughDate = JahezRules.RiyadhDate(entity.EffectiveAtUtc.Value), EffectiveAtUtc = entity.EffectiveAtUtc.Value,
                                Reason = entity.Reason, ApprovedRequestId = entity.Id, ExecutedAtUtc = Now, ExecutedByUserId = Actor });
                            await db.SaveChangesAsync(ct); // Release unique assignment slot within the owning transaction.
                            await CreateHandover(entity.TargetAccountId!.Value, h.RiderProfileId, entity.EffectiveAtUtc.Value,
                                entity.Reason, entity.WaiverAmount, entity.Id, ct);
                            break;
                        case JahezApprovalKind.PercentageCommission:
                            Require(!h.CommissionPostedThrough.HasValue || entity.FromDate > h.CommissionPostedThrough,
                                JahezErrors.Conflict("الفترة تتداخل مع عمولة مرحلة."));
                            Require((!h.EndedAtUtc.HasValue || entity.ToDate <= JahezRules.RiyadhDate(h.EndedAtUtc.Value)),
                                JahezErrors.Conflict("الفترة تتجاوز نهاية احتساب العمولة."));
                            Require(!await db.Set<JahezCommissionPolicyPeriod>().AnyAsync(x => x.HandoverId == h.Id
                                && x.FromDate <= entity.ToDate && x.ToDate >= entity.FromDate, ct), JahezErrors.Conflict("تداخل فترات العمولة."));
                            db.Add(new JahezCommissionPolicyPeriod { HandoverId = h.Id, ApprovalRequestId = id,
                                FromDate = entity.FromDate!.Value, ToDate = entity.ToDate!.Value });
                            break;
                        case JahezApprovalKind.ResetAccount:
                            operation!.ApprovedRequestId = id;
                            await ExecuteWorkflowOperation(operation, true, ct);
                            break;
                        case JahezApprovalKind.Adjustment when entity.AdjustmentTarget == JahezAdjustmentTarget.Settlement:
                            var p = await WorkflowPreview(OperationInput(operation!), ct);
                            Require(p.CommissionComplete, JahezErrors.Conflict(string.Join(" ", p.Problems)));
                            var net = request.UseOriginalValues ? operation!.Net : request.ApprovedNet ?? entity.ProposedNet ?? operation!.Net;
                            var commission = request.UseOriginalValues ? p.Commission : request.ApprovedCommission ?? entity.ProposedCommission ?? p.Commission;
                            WorkflowMoney(net); WorkflowMoney(commission, true);
                            operation!.Net = JahezRules.Money(net); operation.Commission = JahezRules.Money(commission);
                            operation.CalculatedCommission = p.Commission; operation.CalculationJson = p.CalculationJson;
                            operation.Remaining = JahezSettlementRules.Remaining(operation.Net, operation.Commission);
                            // Transfer approval remains a separate, explicit approval type.
                            operation.ApprovedRequestId = id;
                            break;
                        case JahezApprovalKind.Adjustment:
                            await AuthorizeAsync(JahezWorkflowPermissions.DebtAdjust, ct);
                            var delta = request.ApprovedDebtAdjustment ?? entity.DebtAdjustment!.Value;
                            WorkflowMoney(delta);
                            Require(delta != 0, JahezErrors.Invalid("قيمة تعديل المديونية لا تساوي صفراً."));
                            Entry(h, JahezLedgerBucket.PlatformDebt, JahezLedgerKind.Adjustment, JahezRules.Money(delta), entity.Id, request.Reason);
                            break;
                    }
                }
                entity.Status = request.Approve ? JahezApprovalStatus.Approved : JahezApprovalStatus.Rejected;
                var decision = new JahezApprovalDecision { RequestId = id, ActorUserId = Actor, Status = entity.Status,
                    DecidedAtUtc = Now, Reason = request.Reason, DecisionJson = JsonSerializer.Serialize(new { request, operation?.Net, operation?.Commission, operation?.Remaining }) };
                db.Add(decision); NotifyDecision(entity, decision);
                return new JahezWorkflowApproval(entity, [decision]);
            }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezWorkflowApproval>(e.Error); }
    }
}
