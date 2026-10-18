using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private static void WorkflowMoney(decimal value, bool nonnegative = false) => Require(
        Math.Abs(value) <= 1_000_000_000m && (!nonnegative || value >= 0), JahezErrors.Invalid("المبلغ خارج الحدود المسموحة."));

    private async Task<JahezOperationPreview> WorkflowPreview(JahezOperationCreate r, CancellationToken ct)
    {
        Reason(r.Reason);
        Require(Enum.IsDefined(r.Mode) && Enum.IsDefined(r.Purpose), JahezErrors.Invalid("نوع العملية أو غرض الحركة غير صالح."));
        WorkflowMoney(r.Net);
        Require(r.PaymentReference is null || r.PaymentReference.Length <= 500, JahezErrors.Invalid("مرجع الحركة طويل."));
        var h = await Handover(r.HandoverId, ct);
        await Account(h.PlatformRiderAccountId, ct);
        Require(r.EffectiveAtUtc >= h.StartedAtUtc && r.EffectiveAtUtc <= Now, JahezErrors.Invalid("تاريخ العملية خارج الفترة المسموحة."));
        var end = h.EndedAtUtc.HasValue ? JahezRules.RiyadhDate(h.EndedAtUtc.Value) : Today;
        Require(r.ThroughDate >= JahezRules.RiyadhDate(h.StartedAtUtc) && r.ThroughDate <= end,
            JahezErrors.Invalid("تاريخ نهاية التصفية خارج فترة الاستخدام."));
        if (r.Mode != JahezOperationMode.Settle)
        {
            Require(h.EndedAtUtc is null && r.EffectiveAtUtc >= h.StartedAtUtc && r.EffectiveAtUtc <= Now,
                JahezErrors.Conflict("فترة السحب غير صالحة."));
            Require(r.ThroughDate == JahezRules.RiyadhDate(r.EffectiveAtUtc), JahezErrors.Invalid("نهاية التصفية يجب أن توافق يوم السحب."));
        }
        if (r.Mode == JahezOperationMode.Withdraw)
        {
            Require(r.Net == 0 && !r.TransferToRider && r.Fund is null, JahezErrors.Invalid("السحب دون تصفية لا يقبل قيماً مالية."));
            return new(0, 0, 0, JahezMoneyDirection.None, [], true, true, [], "[]");
        }
        Require(r.Purpose is JahezMoneyPurpose.Settlement or JahezMoneyPurpose.Fuel, JahezErrors.Invalid("اختر تصفية أو تحصيل بنزين."));
        var last = await db.Set<JahezOperation>().Where(x => x.HandoverId == h.Id && x.Status == JahezOperationStatus.Executed
            && x.Mode != JahezOperationMode.Withdraw).Select(x => (DateOnly?)x.ThroughDate).MaxAsync(ct);
        Require(!last.HasValue || r.ThroughDate > last.Value, JahezErrors.Conflict("هذه الفترة مصفاة بالفعل."));
        Require(!h.CommissionPostedThrough.HasValue || r.ThroughDate >= h.CommissionPostedThrough,
            JahezErrors.Conflict("تاريخ التصفية يسبق عمولة مرحلة."));
        var calculation = await Commission(h, r.ThroughDate, ct);
        // Only commission uses its own ledger. Informational platform debt and fees are never read here.
        var unpaid = await BucketBalance(h.Id, JahezLedgerBucket.Commission, r.ThroughDate, ct);
        var commission = JahezRules.Money(Math.Max(0, unpaid + calculation.Amount));
        var net = JahezRules.Money(r.Net);
        var remaining = JahezSettlementRules.Remaining(net, commission);
        var direction = JahezSettlementRules.Direction(remaining);
        Require(!r.TransferToRider || r.Mode == JahezOperationMode.SettleAndWithdraw && remaining > 0 && r.Fund is null,
            JahezErrors.Invalid("ترحيل كامل المستحق الموجب متاح مع التصفية والسحب فقط، دون حركة قبض."));
        var funds = r.TransferToRider ? [] : Enum.GetValues<JahezMoneyFund>().Where(x => JahezSettlementRules.Allows(x, direction, r.Purpose)).ToArray();
        return new(net, commission, remaining, direction, funds, r.Mode == JahezOperationMode.SettleAndWithdraw,
            calculation.Problems.Count == 0, calculation.Problems,
            JsonSerializer.Serialize(new { Accrual = JsonSerializer.Deserialize<JsonElement>(calculation.Evidence), UnpaidCommission = unpaid }));
    }

    public Task<Result<JahezOperationPreview>> PreviewOperationAsync(JahezOperationCreate request, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, () => WorkflowPreview(request, ct), ct);

    public Task<Result<JahezOperation>> CreateOperationAsync(string key, JahezOperationCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.operation.create", request, request.Mode == JahezOperationMode.Withdraw ? JahezWorkflowPermissions.Withdraw : JahezWorkflowPermissions.Settle, async () =>
        {
            if (request.Mode == JahezOperationMode.SettleAndWithdraw) await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
            var preview = await WorkflowPreview(request, ct);
            Require(!await db.Set<JahezOperation>().AnyAsync(x => x.HandoverId == request.HandoverId && x.ExecutedAtUtc == null, ct),
                JahezErrors.Conflict("توجد عملية غير منفذة لهذه الفترة؛ استخدمها أو اطلب تعديلها."));
            var operation = new JahezOperation
            {
                HandoverId = request.HandoverId, Mode = request.Mode, ThroughDate = request.ThroughDate,
                EffectiveAtUtc = request.EffectiveAtUtc.ToUniversalTime(), Net = preview.Net, Commission = preview.Commission,
                CalculatedCommission = preview.Commission, Remaining = preview.Remaining, CalculationJson = preview.CalculationJson,
                Fund = request.Fund, Purpose = request.Purpose, TransferToRider = request.TransferToRider,
                PaymentReference = request.PaymentReference, Reason = request.Reason
            };
            db.Add(operation);
            return operation;
        }, ct);

    private static JahezOperationCreate OperationInput(JahezOperation o) => new(o.HandoverId, o.Mode, o.ThroughDate,
        o.EffectiveAtUtc, o.Net, o.Reason, o.Fund, o.Purpose, o.TransferToRider, o.PaymentReference);

    public async Task<Result<JahezOperation>> ExecuteOperationAsync(string key, Guid id, CancellationToken ct = default)
    {
        try
        {
            await AuthorizeAsync(PermissionKeys.Jahez.Read, ct);
            var o = await db.Set<JahezOperation>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(o is not null, JahezErrors.NotFound);
            await AuthorizeWorkflowOperation(o!, ct);
            return await ExecuteAsync(key, "v2.operation.execute", new { id }, PermissionKeys.Jahez.Read, async () =>
            {
                var current = await db.Set<JahezOperation>().SingleAsync(x => x.Id == id, ct);
                await ExecuteWorkflowOperation(current, false, ct);
                return current;
            }, ct);
        }
        catch (JahezBusinessException e) { return Result.Failure<JahezOperation>(e.Error); }
    }

    private async Task AuthorizeWorkflowOperation(JahezOperation o, CancellationToken ct)
    {
        if (o.Mode != JahezOperationMode.Withdraw) await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
        if (o.Mode != JahezOperationMode.Settle) await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
        if (!o.TransferToRider && o.Remaining != 0)
            await AuthorizeAsync(o.Remaining > 0 ? JahezWorkflowPermissions.Receive : JahezWorkflowPermissions.Disburse, ct);
    }

    public Task<Result<JahezOperation>> UpdateOperationAsync(string key, Guid id, JahezOperationUpdate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.operation.update", new { id, request }, request.Values.Mode == JahezOperationMode.Withdraw
            ? JahezWorkflowPermissions.Withdraw : JahezWorkflowPermissions.Settle, async () =>
        {
            var o = await db.Set<JahezOperation>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(o is not null, JahezErrors.NotFound);
            Require(o!.Status == JahezOperationStatus.Ready && Convert.ToBase64String(o.RowVersion) == request.RowVersion,
                JahezErrors.Conflict("العملية ليست جاهزة أو تغيرت؛ أعد تحميلها."));
            Require(o.HandoverId == request.Values.HandoverId, JahezErrors.Invalid("لا يمكن نقل العملية إلى فترة أخرى."));
            if (request.Values.Mode != JahezOperationMode.Settle) await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
            var p = await WorkflowPreview(request.Values, ct);
            o.Mode = request.Values.Mode; o.ThroughDate = request.Values.ThroughDate; o.EffectiveAtUtc = request.Values.EffectiveAtUtc.ToUniversalTime();
            o.Net = p.Net; o.Commission = p.Commission; o.CalculatedCommission = p.Commission; o.Remaining = p.Remaining;
            o.CalculationJson = p.CalculationJson; o.Fund = request.Values.Fund; o.Purpose = request.Values.Purpose;
            o.TransferToRider = request.Values.TransferToRider; o.PaymentReference = request.Values.PaymentReference;
            o.Reason = request.Values.Reason; o.ApprovedRequestId = null;
            return o;
        }, ct);

    private async Task ExecuteWorkflowOperation(JahezOperation o, bool approvedTransfer, CancellationToken ct)
    {
        Require(o.Status == JahezOperationStatus.Ready, JahezErrors.Conflict("العملية منفذة أو قيد المراجعة."));
        if (o.Mode != JahezOperationMode.Withdraw) await AuthorizeAsync(JahezWorkflowPermissions.Settle, ct);
        if (o.Mode != JahezOperationMode.Settle) await AuthorizeAsync(JahezWorkflowPermissions.Withdraw, ct);
        var p = await WorkflowPreview(OperationInput(o), ct);
        Require(p.CommissionComplete, JahezErrors.Conflict(string.Join(" ", p.Problems)));
        Require(p.Commission == o.CalculatedCommission && p.CalculationJson == o.CalculationJson,
            JahezErrors.Conflict("تغيرت بيانات العمولة؛ افتح طلب تعديل لتحديث واعتماد المعاينة."));
        if (o.TransferToRider) Require(approvedTransfer && o.ApprovedRequestId.HasValue, JahezErrors.Conflict("ترحيل المبلغ يحتاج اعتماد المدير."));
        var h = await Handover(o.HandoverId, ct);
        if (o.Mode != JahezOperationMode.Withdraw)
        {
            if (!o.TransferToRider && o.Remaining != 0)
            {
                await StageMoney(h, o.Fund, JahezSettlementRules.Direction(o.Remaining), o.Purpose,
                    Math.Abs(o.Remaining), o.PaymentReference, o.Reason, o.Id, null, ct);
            }
            else Require(o.Fund is null, JahezErrors.Invalid("لا تحدد جهة مالية عند عدم وجود حركة أموال."));
            await PostCommission(h, o.ThroughDate, Guid.CreateVersion7(), o.Reason, ct);
            // Consume the calculated period once. Approved commission overrides are preserved on the operation.
            if (o.CalculatedCommission > 0)
                Entry(h, JahezLedgerBucket.Commission, JahezLedgerKind.Payment, -o.CalculatedCommission, o.Id, o.Reason);
            h.LastSettlementPaymentAtUtc = Now;
            o.PaymentStatus = o.TransferToRider ? JahezOperationPaymentStatus.Transferred
                : o.Remaining == 0 ? JahezOperationPaymentStatus.None : JahezOperationPaymentStatus.Paid;
        }
        else o.PaymentStatus = JahezOperationPaymentStatus.Deferred;
        if (o.Mode != JahezOperationMode.Settle) await WithdrawOperationally(h, o.EffectiveAtUtc, o.Reason, ct);
        o.Status = JahezOperationStatus.Executed;
        o.ExecutedAtUtc = Now;
        o.ExecutedByUserId = Actor;
        if (o.TransferToRider)
        {
            Require(o.Remaining > 0, JahezErrors.Invalid("الترحيل يتطلب مبلغاً موجباً."));
            var debt = new JahezTransferredDebt { OperationId = o.Id, RiderProfileId = h.RiderProfileId, Amount = o.Remaining };
            db.Add(debt);
            await WorkflowBlockNotifications(h.RiderProfileId, debt.Id, false, ct);
        }
    }

    private async Task WithdrawOperationally(JahezAccountSession h, DateTimeOffset at, string reason, CancellationToken ct)
    {
        await ValidateStopDate(h, at, ct);
        h.EndedAtUtc = at.ToUniversalTime();
        var assignment = await db.RiderClientAssignments.SingleAsync(x => x.Id == h.RiderClientAssignmentId, ct);
        var previous = assignment.Status;
        assignment.EffectiveTo = JahezRules.RiyadhDate(at);
        assignment.Status = RiderAssignmentStatus.Ended;
        assignment.EndedByUserId = Actor;
        assignment.EndReason = reason;
        db.Add(new RiderAssignmentEvent { RiderClientAssignmentId = assignment.Id, FromStatus = previous,
            ToStatus = RiderAssignmentStatus.Ended, ActorUserId = Actor, OccurredAtUtc = Now, Reason = reason });
        var account = await Account(h.PlatformRiderAccountId, ct);
        if (account.Status == PlatformRiderAccountStatus.Assigned) account.Status = PlatformRiderAccountStatus.Available;
        Entry(h, JahezLedgerBucket.PlatformDebt, JahezLedgerKind.StatusChange, 0, Guid.CreateVersion7(), reason);
    }

    private async Task<JahezMoneyMovement> StageMoney(JahezAccountSession h, JahezMoneyFund? fund,
        JahezMoneyDirection direction, JahezMoneyPurpose purpose, decimal amount, string? reference, string reason,
        Guid? operationId, Guid? debtId, CancellationToken ct)
    {
        Require(fund.HasValue && JahezSettlementRules.Allows(fund.Value, direction, purpose), JahezErrors.Invalid("جهة القبض أو الصرف لا تسمح بهذه الحركة."));
        Require(!string.IsNullOrWhiteSpace(reference) && reference.Length <= 500, JahezErrors.Invalid("مرجع حركة المال الفعلية مطلوب، بحد 500 حرف."));
        await AuthorizeAsync(direction == JahezMoneyDirection.Receipt ? JahezWorkflowPermissions.Receive : JahezWorkflowPermissions.Disburse, ct);
        var movement = new JahezMoneyMovement { HandoverId = h.Id, RiderProfileId = h.RiderProfileId,
            OperationId = operationId, TransferredDebtId = debtId, Fund = fund!.Value, Direction = direction,
            Purpose = purpose, Amount = amount, ActorUserId = Actor, OccurredAtUtc = Now, Reference = reference!, Reason = reason };
        db.Add(movement);
        return movement;
    }

    public Task<Result<JahezTransferredDebt>> RepayTransferredDebtAsync(string key, Guid id, JahezMoneyRecord request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.debt.repay", new { id, request }, JahezWorkflowPermissions.Repay, async () =>
        {
            Reason(request.Reason);
            Require(request.Purpose is JahezMoneyPurpose.TransferredDebtRepayment or JahezMoneyPurpose.Fuel, JahezErrors.Invalid("غرض سداد المبلغ المرحل غير صالح."));
            var debt = await db.Set<JahezTransferredDebt>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(debt is not null, JahezErrors.NotFound);
            Require(debt!.PaidAtUtc is null, JahezErrors.Conflict("سُدد المبلغ المرحل بالفعل."));
            var operation = await db.Set<JahezOperation>().SingleAsync(x => x.Id == debt.OperationId, ct);
            var h = await Handover(operation.HandoverId, ct);
            await StageMoney(h, request.Fund, JahezMoneyDirection.Receipt, request.Purpose, debt.Amount,
                request.Reference, request.Reason, null, debt.Id, ct);
            debt.PaidAtUtc = Now; debt.PaidByUserId = Actor;
            if (!await db.Set<JahezTransferredDebt>().AnyAsync(x => x.RiderProfileId == debt.RiderProfileId && x.Id != id && x.PaidAtUtc == null, ct))
                await WorkflowBlockNotifications(debt.RiderProfileId, debt.Id, true, ct);
            return debt;
        }, ct);

    public Task<Result<JahezMoneyMovement>> RecordFeeV2Async(string key, JahezFeeRecord request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.fee.record", request, JahezWorkflowPermissions.FeesRecord, async () =>
        {
            Reason(request.Reason); WorkflowMoney(request.Amount, true);
            var amount = JahezRules.Money(request.Amount);
            Require(amount > 0, JahezErrors.Invalid("أدخل مبلغ الرسوم المقبوض."));
            var h = await Handover(request.HandoverId, ct);
            var remaining = await BucketBalance(h.Id, JahezLedgerBucket.AccountFees, Today, ct);
            Require(amount <= remaining, JahezErrors.Conflict("المبلغ يتجاوز رسوم الحساب غير المدفوعة."));
            var movement = await StageMoney(h, request.Fund, JahezMoneyDirection.Receipt, JahezMoneyPurpose.AccountFee,
                amount, request.Reference, request.Reason, null, null, ct);
            Entry(h, JahezLedgerBucket.AccountFees, JahezLedgerKind.Payment, -amount, movement.Id, request.Reason);
            return movement;
        }, ct);

    private async Task WorkflowBlockNotifications(Guid riderId, Guid sourceId, bool released, CancellationToken ct)
    {
        var assignments = await (from a in db.RiderClientAssignments join c in db.ClientContracts on a.ClientContractId equals c.Id
            join p in db.ClientPlatforms on c.ClientPlatformId equals p.Id where a.RiderProfileId == riderId && a.EffectiveTo == null
            select new { a.AssignedByUserId, p.Id }).ToArrayAsync(ct);
        var recipients = assignments.Select(x => x.AssignedByUserId).Append(Actor).Distinct();
        var name = await (from r in db.RiderProfiles join e in db.Employees on r.EmployeeId equals e.Id
            where r.Id == riderId select e.FullNameAr).SingleAsync(ct);
        foreach (var recipient in recipients)
            db.Add(new Notification { RecipientUserId = recipient, EventType = released ? "jahez.rider.block.released" : "jahez.rider.block.created",
                Severity = released ? NotificationSeverity.Information : NotificationSeverity.Critical,
                TitleAr = released ? "سداد مديونية جاهز المرحلة" : "إيقاف مندوب بسبب ترحيل مستحق جاهز",
                TitleEn = released ? "Jahez rider debt paid" : "Rider operational block",
                BodyAr = released ? $"{name}: سدد كامل المستحق المرحل. رُفع سبب البلوك المالي فقط."
                    : $"{name}: موقوف داخل النظام على كيتا وشيفز ونينجا حتى السداد. يلزم إيقافه لدى الشركات.",
                BodyEn = released ? "Transferred debt paid; independent suspensions remain." : "Block rider in Keeta, Chefz and Ninja pending full repayment.",
                SourceEntityType = nameof(JahezTransferredDebt), SourceEntityId = sourceId,
                DeepLink = $"/jahez/transferred-debts/{sourceId}", VisibleAtUtc = Now,
                DeduplicationKey = $"jahez:transfer:{sourceId}:{released}:{recipient}" });
    }
}
