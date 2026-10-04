using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezService
{
    public Task<Result<JahezApprovalResponse>> RequestApprovalAsync(string key, JahezApprovalCreateRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "request-approval", request, PermissionKeys.Jahez.RequestsCreate, async () =>
        {
            Reason(request.Reason);
            Require(Enum.IsDefined(request.Kind), JahezErrors.Invalid("نوع الطلب غير صالح."));
            var h = await Handover(request.HandoverId, ct);
            if (request.Kind is JahezApprovalKind.FreeSwitch or JahezApprovalKind.ResetAccount)
                Require(h.EndedAtUtc is null && request.EffectiveAtUtc.HasValue && request.EffectiveAtUtc >= h.StartedAtUtc && request.EffectiveAtUtc <= Now,
                    JahezErrors.Invalid("حدد تاريخ العملية ضمن فترة الاستخدام الحالية."));
            if (request.Kind == JahezApprovalKind.FreeSwitch)
            {
                Require(request.TargetAccountId.HasValue && request.TargetAccountId != h.PlatformRiderAccountId, JahezErrors.Invalid("الحساب البديل مطلوب ويجب أن يختلف عن الحالي."));
                await Account(request.TargetAccountId!.Value, ct);
            }
            if (request.Kind == JahezApprovalKind.FeeException)
                Require(request.WaiverAmount > 0 && request.WaiverAmount <= 200m && JahezRules.Money(request.WaiverAmount) == request.WaiverAmount,
                    JahezErrors.Invalid("مبلغ إعفاء الرسوم غير صالح."));
            if (request.Kind == JahezApprovalKind.PercentageCommission)
                Require(request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate >= h.CommissionStartsOn
                    && request.ToDate >= request.FromDate && request.ToDate.Value.DayNumber - request.FromDate.Value.DayNumber <= 731
                    && (!h.EndedAtUtc.HasValue || request.ToDate <= JahezRules.RiyadhDate(h.EndedAtUtc.Value)), JahezErrors.Invalid("فترة عمولة النسبة غير صالحة."));
            Require(request.ExternalResetReference is null || request.ExternalResetReference.Length <= 500, JahezErrors.Invalid("مرجع التصفير طويل."));
            var entity = new JahezApprovalRequest { HandoverId = h.Id, Kind = request.Kind, Status = JahezApprovalStatus.Pending,
                RequestedByUserId = Actor, TargetAccountId = request.TargetAccountId, WaiverAmount = request.Kind == JahezApprovalKind.FreeSwitch ? 200m : request.WaiverAmount,
                FromDate = request.FromDate, ToDate = request.ToDate, EffectiveAtUtc = request.EffectiveAtUtc?.ToUniversalTime(),
                Reason = request.Reason, ExternalResetReference = request.ExternalResetReference };
            db.Add(entity);
            return new JahezApprovalResponse(entity, []);
        }, ct);

    public async Task<Result<JahezApprovalResponse>> DecideApprovalAsync(string key, Guid id, JahezDecisionRequest request, CancellationToken ct = default)
    {
        // Check the specific approval capability before looking up a replay receipt as well.
        try
        {
            await AuthorizeAsync(PermissionKeys.Jahez.Read, ct);
            var kind = await db.Set<JahezApprovalRequest>().AsNoTracking().Where(x => x.Id == id).Select(x => (JahezApprovalKind?)x.Kind).SingleOrDefaultAsync(ct);
            Require(kind.HasValue, JahezErrors.NotFound);
            var permission = kind == JahezApprovalKind.ResetAccount ? PermissionKeys.Jahez.ResetsApprove : PermissionKeys.Jahez.RequestsApprove;
            return await ExecuteAsync(key, "decide-approval", new { id, request }, permission, async () =>
        {
            Reason(request.Reason);
            var entity = await db.Set<JahezApprovalRequest>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(entity is not null, JahezErrors.NotFound);
            await AuthorizeAsync(entity!.Kind == JahezApprovalKind.ResetAccount ? PermissionKeys.Jahez.ResetsApprove : PermissionKeys.Jahez.RequestsApprove, ct);
            Require(entity.Status == JahezApprovalStatus.Pending, JahezErrors.Conflict("تم البت في الطلب بالفعل."));
            Require(entity.RequestedByUserId != Actor, JahezErrors.Forbidden);
            var h = await Handover(entity.HandoverId, ct);
            if (request.Approve)
            {
                switch (entity.Kind)
                {
                    case JahezApprovalKind.FeeException:
                        var fee = await db.Set<JahezAccountFee>().SingleAsync(x => x.HandoverId == h.Id, ct);
                        var remaining = await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, Today, ct);
                        Require(entity.WaiverAmount <= remaining && fee.WaivedAmount + entity.WaiverAmount <= fee.Amount,
                            JahezErrors.Conflict("الإعفاء يتجاوز الرسوم غير المدفوعة؛ لا يُرجع النظام دفعات سابقة تلقائيًا."));
                        fee.WaivedAmount += entity.WaiverAmount;
                        fee.ApprovalRequestId = entity.Id;
                        Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Adjustment, -entity.WaiverAmount, entity.Id, request.Reason);
                        break;
                    case JahezApprovalKind.PercentageCommission:
                        Require(!h.CommissionPostedThrough.HasValue || entity.FromDate > h.CommissionPostedThrough,
                            JahezErrors.Conflict("فترة النسبة تتداخل مع عمولة مصفاة."));
                        Require(!h.EndedAtUtc.HasValue || entity.ToDate <= JahezRules.RiyadhDate(h.EndedAtUtc.Value), JahezErrors.Conflict("الفترة تتجاوز إغلاق الحساب."));
                        Require(!await db.Set<JahezCommissionPolicyPeriod>().AnyAsync(x => x.HandoverId == h.Id && x.FromDate <= entity.ToDate && x.ToDate >= entity.FromDate, ct),
                            JahezErrors.Conflict("الفترة تتداخل مع نسبة معتمدة."));
                        db.Add(new JahezCommissionPolicyPeriod { HandoverId = h.Id, ApprovalRequestId = entity.Id,
                            FromDate = entity.FromDate!.Value, ToDate = entity.ToDate!.Value });
                        break;
                    case JahezApprovalKind.FreeSwitch:
                        await CloseHandover(h, entity.EffectiveAtUtc!.Value, entity.Reason, true, ct);
                        // Release the filtered account-slot index before inserting its replacement, inside the same transaction.
                        await db.SaveChangesAsync(ct);
                        await CreateHandover(entity.TargetAccountId!.Value, h.RiderProfileId, entity.EffectiveAtUtc.Value, entity.Reason, 200m, entity.Id, ct);
                        break;
                    case JahezApprovalKind.ResetAccount:
                        await CloseHandover(h, entity.EffectiveAtUtc!.Value, entity.Reason, true, ct);
                        break;
                    default: throw new JahezBusinessException(JahezErrors.Invalid("نوع الطلب غير صالح."));
                }
            }
            entity.Status = request.Approve ? JahezApprovalStatus.Approved : JahezApprovalStatus.Rejected;
            var decision = new JahezApprovalDecision { RequestId = entity.Id, ActorUserId = Actor, Status = entity.Status,
                DecidedAtUtc = Now, Reason = request.Reason };
            db.Add(decision);
            NotifyDecision(entity, decision);
            return new JahezApprovalResponse(entity, [decision]);
            }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezApprovalResponse>(e.Error); }
    }



    public Task<Result<JahezApprovalResponse>> CancelApprovalAsync(string key, Guid id, string reason, CancellationToken ct = default) =>
        ExecuteAsync(key, "cancel-approval", new { id, reason }, PermissionKeys.Jahez.RequestsCreate, async () =>
        {
            Reason(reason);
            var entity = await db.Set<JahezApprovalRequest>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(entity is not null, JahezErrors.NotFound);
            Require(entity!.RequestedByUserId == Actor, JahezErrors.Forbidden);
            Require(entity.Status == JahezApprovalStatus.Pending, JahezErrors.Conflict("لا يمكن إلغاء طلب تم البت فيه."));
            entity.Status = JahezApprovalStatus.Cancelled;
            var decision = new JahezApprovalDecision { RequestId = id, ActorUserId = Actor, Status = entity.Status, DecidedAtUtc = Now, Reason = reason };
            db.Add(decision);
            return new JahezApprovalResponse(entity, [decision]);
        }, ct);

    private void NotifyDecision(JahezApprovalRequest request, JahezApprovalDecision decision) => db.Add(new Notification
    {
        RecipientUserId = request.RequestedByUserId, EventType = "jahez.approval.decided", TitleAr = "قرار طلب جاهز",
        TitleEn = "Jahez request decision", BodyAr = $"تم {(decision.Status == JahezApprovalStatus.Approved ? "اعتماد" : "رفض")} طلبك: {NotificationReason(decision.Reason)}",
        BodyEn = $"Request {decision.Status}: {NotificationReason(decision.Reason)}", SourceEntityType = "JahezApprovalRequest", SourceEntityId = request.Id,
        DeepLink = $"/jahez/requests/{request.Id}", DeduplicationKey = $"jahez:decision:{request.Id:N}", VisibleAtUtc = Now
    });

    private static string NotificationReason(string reason) => reason.Length <= 1500 ? reason : reason[..1500];

    public Task<Result<JahezApprovalResponse>> GetApprovalAsync(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            var entity = await db.Set<JahezApprovalRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(entity is not null, JahezErrors.NotFound);
            var decisions = await db.Set<JahezApprovalDecision>().AsNoTracking().Where(x => x.RequestId == id).OrderBy(x => x.DecidedAtUtc).ToArrayAsync(ct);
            return new JahezApprovalResponse(entity!, decisions);
        }, ct);

    public Task<Result<JahezPage<JahezApprovalRequest>>> GetApprovalsAsync(JahezApprovalStatus? status, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            Require(!status.HasValue || Enum.IsDefined(status.Value), JahezErrors.Invalid("حالة الطلب غير صالحة."));
            var rows = await db.Set<JahezApprovalRequest>().AsNoTracking().Where(x => !status.HasValue || x.Status == status)
                .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezApprovalRequest>(rows, page, pageSize);
        }, ct);

    public Task<Result<JahezEarningsStatement>> RecordEarningsAsync(string key, JahezEarningsRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "earnings", request, PermissionKeys.Jahez.EarningsManage, async () =>
        {
            Reason(request.Reason);
            var h = await Handover(request.HandoverId, ct);
            decimal?[] values = [request.TotalDeliveryPrice, request.TotalPenalties, request.TotalCashAmount, request.TotalDriverDebit,
                request.TotalServiceDeduction, request.TotalDriverCredit, request.TotalBonuses, request.TotalTips, request.TotalFreeOrders];
            Require(values.All(x => x.HasValue && x >= 0 && x <= 1_000_000_000m && decimal.Round(x.Value, 6) == x.Value),
                JahezErrors.Invalid("جميع تفاصيل الأرباح المالية مطلوبة، وتُدخل كمبالغ غير سالبة بدقة لا تتجاوز ست منازل."));
            Require(request.FromDate >= h.CommissionStartsOn && request.ToDate >= request.FromDate && request.ToDate <= Today
                && (!h.CommissionPostedThrough.HasValue || request.FromDate > h.CommissionPostedThrough)
                && (!h.EndedAtUtc.HasValue || request.ToDate <= JahezRules.RiyadhDate(h.EndedAtUtc.Value)), JahezErrors.Conflict("الفترة غير صالحة أو مصفاة بالفعل."));
            Require(await db.Set<JahezCommissionPolicyPeriod>().AnyAsync(x => x.HandoverId == h.Id && x.FromDate <= request.FromDate && x.ToDate >= request.ToDate, ct),
                JahezErrors.Conflict("لا توجد موافقة عمولة نسبة تغطي هذه الفترة."));
            if (request.SupersedesId.HasValue)
            {
                var old = await db.Set<JahezEarningsStatement>().SingleOrDefaultAsync(x => x.Id == request.SupersedesId, ct);
                Require(old is not null && old.HandoverId == h.Id && old.FromDate == request.FromDate && old.ToDate == request.ToDate,
                    JahezErrors.Conflict("التصحيح يجب أن يحافظ على الحساب والفترة الأصلية."));
                Require(!await db.Set<JahezEarningsStatement>().AnyAsync(x => x.SupersedesId == request.SupersedesId, ct), JahezErrors.Conflict("تم استبدال البيان بالفعل."));
            }
            Require(!await db.Set<JahezEarningsStatement>().AnyAsync(x => x.HandoverId == h.Id && x.Id != request.SupersedesId
                && x.FromDate <= request.ToDate && x.ToDate >= request.FromDate && !db.Set<JahezEarningsStatement>().Any(y => y.SupersedesId == x.Id), ct),
                JahezErrors.Conflict("تفاصيل الأرباح تتداخل مع فترة أخرى."));
            var entity = new JahezEarningsStatement { HandoverId = h.Id, FromDate = request.FromDate, ToDate = request.ToDate,
                TotalDeliveryPrice = request.TotalDeliveryPrice!.Value, TotalPenalties = request.TotalPenalties!.Value,
                TotalCashAmount = request.TotalCashAmount!.Value, TotalDriverDebit = request.TotalDriverDebit!.Value,
                TotalServiceDeduction = request.TotalServiceDeduction!.Value, TotalDriverCredit = request.TotalDriverCredit!.Value,
                TotalBonuses = request.TotalBonuses!.Value, TotalTips = request.TotalTips!.Value, TotalFreeOrders = request.TotalFreeOrders!.Value,
                SupersedesId = request.SupersedesId, Reason = request.Reason };
            db.Add(entity);
            return entity;
        }, ct);

    public Task<Result<JahezRiderSettlement>> PayAsync(string key, JahezPaymentRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "payment", request, PermissionKeys.Jahez.CollectionsManage, async () =>
        {
            Reason(request.Reason);
            decimal[] amounts = [request.FeePayment, request.DebtPayment, request.CommissionPayment];
            Require(amounts.All(x => x >= 0 && x <= 1_000_000_000m && x == JahezRules.Money(x)) && amounts.Sum() > 0,
                JahezErrors.Invalid("سجل دفعة موجبة بالهللة، موزعة على بنودها."));
            Require(request.CountsAsSettlement || request.DebtPayment == 0 && request.CommissionPayment == 0,
                JahezErrors.Invalid("تحصيل المديونية أو العمولة يجب تسجيله كتصفية."));
            var h = await Handover(request.HandoverId, ct);
            Require(request.ThroughDate >= JahezRules.RiyadhDate(h.StartedAtUtc) && request.ThroughDate <= Today,
                JahezErrors.Invalid("تاريخ نهاية التصفية غير صالح."));
            var lastThrough = await db.Set<JahezRiderSettlement>().Where(x => x.HandoverId == h.Id && x.CountsAsSettlement).Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
            Require(!lastThrough.HasValue || request.ThroughDate >= lastThrough.Value, JahezErrors.Invalid("نهاية التصفية لا تسبق التصفية السابقة."));
            Require(!h.CommissionPostedThrough.HasValue || request.ThroughDate >= h.CommissionPostedThrough.Value, JahezErrors.Invalid("نهاية التصفية تسبق عمولة مرحلة."));
            if (request.CountsAsSettlement) await PostCommission(h, request.ThroughDate, Guid.CreateVersion7(), request.Reason, ct);
            var fees = await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, request.ThroughDate, ct);
            var debt = await BucketBalance(h.Id, JahezLedgerBucket.PlatformDebt, request.ThroughDate, ct);
            var commission = await BucketBalance(h.Id, JahezLedgerBucket.Commission, request.ThroughDate, ct);
            Require(request.FeePayment <= Math.Max(0m, JahezRules.Money(fees)) && request.DebtPayment <= Math.Max(0m, JahezRules.Money(debt))
                && request.CommissionPayment <= Math.Max(0m, JahezRules.Money(commission)), JahezErrors.Conflict("الدفعة تتجاوز الرصيد المستحق لأحد البنود."));
            return StagePayment(h, request.ThroughDate, request.FeePayment, request.DebtPayment, request.CommissionPayment, request.CountsAsSettlement, request.Reason);
        }, ct);

    private JahezRiderSettlement StagePayment(JahezAccountHandover h, DateOnly through, decimal fee, decimal debt, decimal commission, bool counts, string reason)
    {
        var payment = new JahezRiderSettlement { HandoverId = h.Id, ThroughDate = through, RecordedAtUtc = Now,
            CollectedByUserId = Actor, FeePayment = fee, DebtPayment = debt, CommissionPayment = commission, CountsAsSettlement = counts, Reason = reason };
        db.Add(payment);
        if (fee > 0) Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Payment, -fee, payment.Id, reason);
        if (debt > 0) Entry(h, JahezLedgerBucket.PlatformDebt, JahezLedgerKind.Payment, -debt, payment.Id, reason);
        if (commission > 0) Entry(h, JahezLedgerBucket.Commission, JahezLedgerKind.Payment, -commission, payment.Id, reason);
        if (fee > 0) db.Add(new JahezCashboxEntry { HandoverId = h.Id, SettlementId = payment.Id,
            Section = JahezCashboxSection.AccountFees, Amount = fee, CollectedByUserId = Actor, ReceivedAtUtc = Now });
        if (debt + commission > 0) db.Add(new JahezCashboxEntry { HandoverId = h.Id, SettlementId = payment.Id,
            Section = JahezCashboxSection.Settlements, Amount = debt + commission, CollectedByUserId = Actor, ReceivedAtUtc = Now });
        if (counts)
        {
            h.LastSettlementPaymentAtUtc = Now;
            // Notification visibility is refreshed by the reminder worker; the financial anchor changes here atomically.
        }
        return payment;
    }

    public Task<Result<JahezLedgerEntry>> AdjustAsync(string key, JahezLedgerAdjustmentRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "adjustment", request, PermissionKeys.Jahez.AdjustmentsManage, async () =>
        {
            Reason(request.Reason);
            Require(Enum.IsDefined(request.Bucket) && request.Amount != 0 && Math.Abs(request.Amount) <= 1_000_000_000m
                && decimal.Round(request.Amount, 6) == request.Amount,
                JahezErrors.Invalid("بند أو مبلغ التسوية غير صالح."));
            var h = await Handover(request.HandoverId, ct);
            if (request.ReversesEntryId.HasValue)
            {
                var original = await db.Set<JahezLedgerEntry>().SingleOrDefaultAsync(x => x.Id == request.ReversesEntryId, ct);
                Require(original is not null && original.Kind != JahezLedgerKind.Payment && original.Kind != JahezLedgerKind.Transfer
                    && original.HandoverId == h.Id && original.Bucket == request.Bucket && original.Amount == -request.Amount,
                    JahezErrors.Conflict("القيد العكسي يجب أن يعكس استحقاقًا في البند والإسناد نفسيهما؛ الدفعات النقدية لا تُلغى بهذه العملية."));
                Require(!await db.Set<JahezLedgerEntry>().AnyAsync(x => x.ReversesEntryId == request.ReversesEntryId, ct), JahezErrors.Conflict("القيد معكوس بالفعل."));
            }
            var entity = new JahezLedgerEntry { HandoverId = h.Id, Bucket = request.Bucket, Kind = JahezLedgerKind.Adjustment,
                Amount = request.Amount, SourceId = Guid.CreateVersion7(), ReversesEntryId = request.ReversesEntryId, OccurredAtUtc = Now, Reason = request.Reason };
            db.Add(entity);
            return entity;
        }, ct);
}
