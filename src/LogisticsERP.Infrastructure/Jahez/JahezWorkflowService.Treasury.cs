using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private async Task<bool> TreasuryActive(CancellationToken ct) =>
        await db.Set<TreasuryState>().AnyAsync(x => x.Id == TreasuryState.SingletonId && x.IsActive, ct);

    private async Task RequireTreasury(CancellationToken ct) =>
        Require(await TreasuryActive(ct), JahezErrors.Conflict("الخزينة الجديدة غير مفعلة؛ أكمل مراجعة الترحيل أولاً."));

    private async Task<TreasuryOwnership?> Ownership(TreasurySource source, Guid id, CancellationToken ct) =>
        db.Set<TreasuryOwnership>().Local.FirstOrDefault(x => x.Source == source && x.SourceId == id)
        ?? await db.Set<TreasuryOwnership>().AsNoTracking().SingleOrDefaultAsync(x => x.Source == source && x.SourceId == id, ct);

    private async Task<TreasuryOwnership> CaptureOwnership(TreasurySource source, Guid sourceId, Guid accountId,
        Guid sponsorId, string evidence, CancellationToken ct)
    {
        var existing = await Ownership(source, sourceId, ct);
        if (existing is not null)
        {
            Require(existing.SponsorId == sponsorId && existing.PlatformAccountId == accountId,
                JahezErrors.Conflict("ملكية الحركة المثبتة لا يمكن تغييرها."));
            return existing;
        }
        var sponsor = await db.Sponsors.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == sponsorId, ct);
        Require(sponsor is not null && !string.IsNullOrWhiteSpace(sponsor.EmployerIdentityNumber),
            JahezErrors.Invalid("هوية كفيل الحساب غير مكتملة."));
        var owner = new TreasuryOwnership { Source = source, SourceId = sourceId, PlatformAccountId = accountId,
            SponsorId = sponsorId, SponsorIdentifier = sponsor!.EmployerIdentityNumber,
            SponsorNameAr = sponsor.RegistryNameAr, Evidence = evidence };
        db.Add(owner); return owner;
    }

    private async Task<(Guid AccountId, Guid SponsorId)> CurrentOwnership(Guid handoverId, CancellationToken ct)
    {
        var session = db.Set<JahezAccountSession>().Local.FirstOrDefault(x => x.Id == handoverId)
            ?? await db.Set<JahezAccountSession>().IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Id == handoverId, ct);
        var account = await db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(x => x.Id == session.PlatformRiderAccountId, ct);
        Require(account.SponsorId.HasValue && account.SponsorId != Guid.Empty, JahezErrors.Invalid("كفيل حساب جاهز مطلوب."));
        return (account.Id, account.SponsorId!.Value);
    }

    // One transaction owner invokes this before saving. No independently committed treasury writes.
    private async Task SynchronizeTreasuryAsync(CancellationToken ct)
    {
        var active = await TreasuryActive(ct);
        var fees = db.ChangeTracker.Entries<JahezAccountFee>().Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToArray();
        foreach (var fee in fees)
        {
            var o = await CurrentOwnership(fee.HandoverId, ct);
            await CaptureOwnership(TreasurySource.Fee, fee.Id, o.AccountId, o.SponsorId, "Fee obligation creation", ct);
        }
        foreach (var entry in db.ChangeTracker.Entries<JahezOperation>().Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray())
        {
            var operation = entry.Entity;
            var current = await CurrentOwnership(operation.HandoverId, ct);
            if (entry.State == EntityState.Added && operation.Mode == JahezOperationMode.Withdraw)
                operation.ExpectedSponsorId = current.SponsorId;
            if (operation.Status != JahezOperationStatus.Executed)
                operation.ExpectedSponsorId = current.SponsorId;
            else if (await Ownership(TreasurySource.Operation, operation.Id, ct) is null
                && (entry.State == EntityState.Added || entry.Property(x => x.Status).OriginalValue != JahezOperationStatus.Executed))
            {
                Require(!active || operation.ExpectedSponsorId == current.SponsorId,
                    JahezErrors.Conflict("تغير كفيل الحساب بعد المعاينة؛ أعد تحميل وتحديث العملية."));
                await CaptureOwnership(TreasurySource.Operation, operation.Id, current.AccountId, current.SponsorId, "Settlement execution", ct);
            }
        }
        foreach (var debt in db.ChangeTracker.Entries<JahezTransferredDebt>().Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToArray())
        {
            var owner = await Ownership(TreasurySource.Operation, debt.OperationId, ct);
            Require(!active || owner is not null, JahezErrors.Conflict("ملكية أصل الدين غير موثقة."));
            if (owner is not null) await CaptureOwnership(TreasurySource.Debt, debt.Id, owner.PlatformAccountId, owner.SponsorId, $"Origin operation {debt.OperationId}", ct);
        }
        foreach (var entry in db.ChangeTracker.Entries<JahezMoneyMovement>().Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray())
        {
            var m = entry.Entity;
            var owner = await Ownership(TreasurySource.Movement, m.Id, ct);
            if (owner is null)
            {
                TreasuryOwnership? origin = null;
                if (m.TransferredDebtId.HasValue) origin = await Ownership(TreasurySource.Debt, m.TransferredDebtId.Value, ct);
                else if (m.OperationId.HasValue) origin = await Ownership(TreasurySource.Operation, m.OperationId.Value, ct);
                else if (m.Purpose is JahezMoneyPurpose.AccountFee or JahezMoneyPurpose.AccountFeeRefund)
                {
                    var fee = db.Set<JahezAccountFee>().Local.FirstOrDefault(x => x.HandoverId == m.HandoverId)
                        ?? await db.Set<JahezAccountFee>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.HandoverId == m.HandoverId, ct);
                    if (fee is not null) origin = await Ownership(TreasurySource.Fee, fee.Id, ct);
                }
                Require(!active || origin is not null, JahezErrors.Conflict("ملكية مصدر الحركة غير موثقة؛ يلزم قرار المالية."));
                if (origin is not null) owner = await CaptureOwnership(TreasurySource.Movement, m.Id,
                    origin.PlatformAccountId, origin.SponsorId, $"Inherited from {origin.Source}:{origin.SourceId}", ct);
            }
            // Once a legacy movement is migrated, permitted source corrections must keep its journal
            // synchronized even while finance is completing the pre-activation reconciliation.
            if (owner is not null && (active || await db.Set<TreasuryPosting>().AnyAsync(p => p.MoneyMovementId == m.Id, ct)))
                await PostMoney(m, owner, entry.State == EntityState.Modified, ct);
        }
    }

    private async Task<TreasurySubAccount> PostingAccount(Guid id, CancellationToken ct)
    {
        var account = await db.Set<TreasurySubAccount>().SingleOrDefaultAsync(x => x.Id == id, ct);
        Require(account is { IsPosting: true, IsActive: true } && account.SponsorId.HasValue,
            JahezErrors.Invalid("الحساب المالي غير نشط أو ليس حساب ترحيل."));
        return account!;
    }

    private async Task<TreasurySubAccount> Route(TreasuryOwnership owner, JahezMoneyFund fund,
        JahezMoneyPurpose purpose, CancellationToken ct, bool central = false)
    {
        var location = central ? TreasuryLocation.Central : fund switch
        { JahezMoneyFund.Cashbox => TreasuryLocation.Supervisor, JahezMoneyFund.Bank => TreasuryLocation.Bank, _ => TreasuryLocation.PetroApp };
        var classification = purpose is JahezMoneyPurpose.AccountFee or JahezMoneyPurpose.AccountFeeRefund ? TreasuryPurpose.AccountFee : TreasuryPurpose.Settlement;
        var account = await db.Set<TreasurySubAccount>().SingleOrDefaultAsync(x => x.IsDefault && x.IsActive
            && x.IsPosting && x.SponsorId == owner.SponsorId && x.Location == location
            && (x.Purpose == classification || x.Purpose == TreasuryPurpose.Combined), ct);
        Require(account is not null, JahezErrors.Conflict("لم يتم إعداد حساب مالي لهذه الجهة والغرض."));
        return account!;
    }

    private async Task<decimal> AccountBalance(Guid id, CancellationToken ct)
    {
        var persisted = await db.Set<TreasuryPosting>().Where(x => x.SubAccountId == id).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        return persisted + db.ChangeTracker.Entries<TreasuryPosting>().Where(x => x.State == EntityState.Added && x.Entity.SubAccountId == id).Sum(x => x.Entity.Amount);
    }

    private async Task EnsureAvailable(TreasurySubAccount account, decimal outgoing, CancellationToken ct, bool confirming = false)
    {
        if (account.Location is TreasuryLocation.PetroApp) return;
        var reserved = confirming ? 0 : await (from line in db.Set<TreasuryHandoverLine>()
            join h in db.JahezCashboxHandovers on line.HandoverId equals h.Id
            where h.Status == JahezCashboxHandoverStatus.Pending && line.SubAccountId == account.Id
            select (decimal?)line.Amount).SumAsync(ct) ?? 0;
        Require(await AccountBalance(account.Id, ct) - reserved >= outgoing, JahezErrors.Conflict("الرصيد المتاح لهذا الكفيل غير كافٍ أو محجوز للتسليم؛ سجل التمويل أولاً."));
    }

    private TreasuryPosting AddPosting(TreasurySubAccount account, decimal signed, TreasuryEntryKind kind,
        string reference, string reason, Guid? batch = null, Guid? movement = null, Guid? handover = null,
        int revision = 0, Guid? reverses = null, DateTimeOffset? at = null)
    {
        Require(signed != 0 && account.SponsorId.HasValue, JahezErrors.Invalid("قيمة أو ملكية القيد غير صالحة."));
        var posting = new TreasuryPosting { SubAccountId = account.Id, SponsorId = account.SponsorId!.Value,
            Amount = signed, Kind = kind, Reference = reference, Reason = reason, BatchId = batch ?? Guid.CreateVersion7(),
            MoneyMovementId = movement, CashboxHandoverId = handover, Revision = revision,
            ReversesPostingId = reverses, OccurredAtUtc = at ?? Now, ActorUserId = Actor, ActorNameAr = commandActorName ?? "اسم المستخدم غير متاح" };
        db.Add(posting); return posting;
    }

    private async Task PostMoney(JahezMoneyMovement m, TreasuryOwnership owner, bool correction, CancellationToken ct, bool historical = false)
    {
        var prior = await db.Set<TreasuryPosting>().Where(x => x.MoneyMovementId == m.Id && x.Kind == TreasuryEntryKind.Business
                && !db.Set<TreasuryPosting>().Any(r => r.ReversesPostingId == x.Id))
            .OrderByDescending(x => x.Revision).FirstOrDefaultAsync(ct);
        if (prior is not null && !correction) return;
        var batch = Guid.CreateVersion7();
        var lastRevision = await db.Set<TreasuryPosting>().Where(x => x.MoneyMovementId == m.Id && x.Kind == TreasuryEntryKind.Business)
            .Select(x => (int?)x.Revision).MaxAsync(ct);
        var revision = (lastRevision ?? -1) + 1;
        if (prior is not null)
        {
            var previous = await PostingAccount(prior.SubAccountId, ct);
            AddPosting(previous, -prior.Amount, TreasuryEntryKind.Reversal, m.Reference, m.Reason, batch,
                handover: null, reverses: prior.Id);
        }
        if (m.Amount == 0 || m.Direction == JahezMoneyDirection.None)
        {
            if (!historical && prior is not null) await EnsureAvailable(await PostingAccount(prior.SubAccountId, ct), 0, ct);
            return;
        }
        var account = await Route(owner, m.Fund, m.Purpose, ct);
        var amount = m.Direction == JahezMoneyDirection.Receipt ? m.Amount : -m.Amount;
        var feeRefund = m.Purpose == JahezMoneyPurpose.AccountFeeRefund && m.Fund == JahezMoneyFund.Cashbox
            && m.Direction == JahezMoneyDirection.Disbursement && !correction
            && (db.JahezAccountFeeRefunds.Local.Any(x => x.MoneyMovementId == m.Id)
                || await db.JahezAccountFeeRefunds.AnyAsync(x => x.MoneyMovementId == m.Id, ct));
        Require(feeRefund || account.AllowedDirections == TreasuryDirections.Dual
            || amount > 0 && account.AllowedDirections == TreasuryDirections.InOnly
            || amount < 0 && account.AllowedDirections == TreasuryDirections.OutOnly, JahezErrors.Invalid("اتجاه الحركة غير مسموح للحساب."));
        if (!historical && !feeRefund && amount < 0) await EnsureAvailable(account, -amount, ct);
        var businessPosting = AddPosting(account, amount, TreasuryEntryKind.Business, m.Reference, m.Reason, batch, m.Id, revision: revision,
            at: historical ? m.OccurredAtUtc : Now);
        if (historical)
        {
            businessPosting.ActorUserId = m.ActorUserId;
            businessPosting.ActorNameAr = await TreasuryUserName(m.ActorUserId, ct);
        }
        // Receipts can replenish an overdrawn refund cashbox without needing to clear it in one payment.
        if (!historical && !feeRefund && amount < 0) await EnsureAvailable(account, 0, ct);
        if (!historical && prior is not null && prior.SubAccountId != account.Id)
            await EnsureAvailable(await PostingAccount(prior.SubAccountId, ct), 0, ct);
    }

    private async Task<Guid> Transfer(TreasurySubAccount from, TreasurySubAccount to, decimal amount,
        string reference, string reason, CancellationToken ct, Guid? handover = null, bool historical = false, DateTimeOffset? at = null)
    {
        Require(from.Id != to.Id && from.SponsorId == to.SponsorId && amount > 0,
            JahezErrors.Invalid("التحويل يجب أن يكون بين حسابين مختلفين للكفيل نفسه."));
        Require(from.Location != TreasuryLocation.PetroApp && to.Location != TreasuryLocation.PetroApp,
            JahezErrors.Invalid("حساب بترو آب حساب مقاصة؛ سجل تمويل المحفظة كدليل مستقل."));
        if (!historical) await EnsureAvailable(from, amount, ct, confirming: handover.HasValue);
        var batch = Guid.CreateVersion7();
        AddPosting(from, -amount, TreasuryEntryKind.Transfer, reference, reason, batch, handover: handover, at: at);
        AddPosting(to, amount, TreasuryEntryKind.Transfer, reference, reason, batch, handover: handover, at: at);
        return batch;
    }

    public Task<Result<IReadOnlyList<TreasurySubAccount>>> TreasuryAccountsAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<TreasurySubAccount>>(JahezTreasuryPermissions.Read, async () =>
            await db.Set<TreasurySubAccount>().AsNoTracking().OrderBy(x => x.Code).ToArrayAsync(ct), ct);

    public Task<Result<TreasurySubAccount>> CreateTreasuryAccountAsync(string key, TreasuryAccountCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.account.create", request, JahezTreasuryPermissions.AccountsCreate, async () =>
        {
            Require(!string.IsNullOrWhiteSpace(request.Code) && request.Code.Length <= 100
                && !string.IsNullOrWhiteSpace(request.NameAr) && request.NameAr.Length <= 200
                && !string.IsNullOrWhiteSpace(request.NameEn) && request.NameEn.Length <= 200
                && Enum.IsDefined(request.Location) && Enum.IsDefined(request.Purpose) && Enum.IsDefined(request.AllowedDirections), JahezErrors.Invalid("بيانات الحساب غير صالحة."));
            var parent = await db.Set<TreasurySubAccount>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ParentId, ct);
            Require(parent is { IsPosting: false, IsActive: true } && parent.Location == request.Location,
                JahezErrors.Invalid("الحساب الأب غير صالح."));
            Require(await db.Sponsors.AnyAsync(x => x.Id == request.SponsorId, ct), JahezErrors.NotFound);
            Require(!await db.Set<TreasurySubAccount>().AnyAsync(x => x.Code == request.Code.Trim(), ct), JahezErrors.Conflict("كود الحساب مستخدم."));
            if (request.IsDefault)
                Require(!await db.Set<TreasurySubAccount>().AnyAsync(x => x.IsDefault && x.SponsorId == request.SponsorId && x.Location == request.Location
                    && (x.Purpose == request.Purpose || x.Purpose == TreasuryPurpose.Combined || request.Purpose == TreasuryPurpose.Combined), ct),
                    JahezErrors.Conflict("يوجد حساب توجيه افتراضي متداخل لهذا الكفيل."));
            var a = new TreasurySubAccount { Code = request.Code.Trim(), NameAr = request.NameAr.Trim(), NameEn = request.NameEn.Trim(),
                ParentId = request.ParentId, SponsorId = request.SponsorId, Location = request.Location,
                Purpose = request.Purpose, AllowedDirections = request.AllowedDirections, IsPosting = true, IsDefault = request.IsDefault };
            db.Add(a); return a;
        }, ct);

    public Task<Result<TreasurySubAccount>> UpdateTreasuryAccountAsync(string key, Guid id, TreasuryAccountUpdate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.account.update", new { id, request }, JahezTreasuryPermissions.AccountsUpdate, async () =>
        {
            var a = await db.Set<TreasurySubAccount>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(a is not null, JahezErrors.NotFound);
            Require(Convert.ToBase64String(a!.RowVersion) == request.RowVersion, JahezErrors.Conflict("تغير الحساب؛ أعد تحميله."));
            Require(!string.IsNullOrWhiteSpace(request.NameAr) && request.NameAr.Length <= 200
                && !string.IsNullOrWhiteSpace(request.NameEn) && request.NameEn.Length <= 200, JahezErrors.Invalid("اسم الحساب مطلوب."));
            if (!request.IsActive)
            {
                Require(!a.IsDefault, JahezErrors.Conflict("لا يمكن تعطيل حساب التوجيه الافتراضي."));
                Require(!await db.Set<TreasurySubAccount>().AnyAsync(x => x.ParentId == id && x.IsActive, ct)
                    && await AccountBalance(id, ct) == 0, JahezErrors.Conflict("أغلق الحسابات الفرعية وانقل الرصيد أولاً."));
            }
            a.NameAr = request.NameAr.Trim(); a.NameEn = request.NameEn.Trim(); a.IsActive = request.IsActive; return a;
        }, ct);

    public Task<Result<Guid>> TransferTreasuryAsync(string key, TreasuryTransferCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.transfer", request, JahezTreasuryPermissions.TransfersCreate, async () =>
        {
            var active = await TreasuryActive(ct);
            ValidateMoneyReference(request.Reference); Reason(request.Reason); WorkflowMoney(request.Amount, true);
            Require(!request.OccurredAtUtc.HasValue || !active && request.OccurredAtUtc > DateTimeOffset.MinValue && request.OccurredAtUtc <= Now,
                JahezErrors.Invalid("التأريخ السابق متاح للترحيل قبل التفعيل فقط."));
            Require(request.Amount == LogisticsERP.Domain.Jahez.JahezRules.Money(request.Amount), JahezErrors.Invalid("المبلغ يجب أن يكون بدقة هللتين."));
            var from = await PostingAccount(request.FromAccountId, ct); var to = await PostingAccount(request.ToAccountId, ct);
            Require(active || from.Location == TreasuryLocation.Central && to.Location == TreasuryLocation.Supervisor
                && request.Reference.StartsWith("legacy:", StringComparison.Ordinal),
                JahezErrors.Conflict("قبل التفعيل يسمح فقط بتمويل تاريخي موثق بمرجع legacy:رقم-التسليم."));
            if (!active)
            {
                Require(Guid.TryParse(request.Reference.AsSpan(7), out var historicalId)
                    && await db.JahezCashboxHandovers.AnyAsync(h => h.Id == historicalId && h.Status == JahezCashboxHandoverStatus.Confirmed, ct)
                    && await (from link in db.JahezCashboxHandoverMovements
                        join movement in db.JahezMoneyMovements on link.MoneyMovementId equals movement.Id
                        join owner in db.Set<TreasuryOwnership>() on movement.Id equals owner.SourceId
                        where link.CashboxHandoverId == historicalId && movement.Fund == JahezMoneyFund.Cashbox
                            && owner.Source == TreasurySource.Movement && owner.SponsorId == to.SponsorId
                        select link.Id).AnyAsync(ct), JahezErrors.Invalid("مرجع التمويل التاريخي لا يطابق تسليماً مؤكداً وملكية موثقة."));
            }
            Require(from.Location != TreasuryLocation.Supervisor, JahezErrors.Invalid("خروج عهدة المشرف يتم عبر تسليم العهدة فقط."));
            Require(to.Location != TreasuryLocation.Supervisor || to.Purpose == TreasuryPurpose.Settlement,
                JahezErrors.Invalid("تمويل عهدة الرسوم غير مسموح."));
            return await Transfer(from, to, request.Amount, request.Reference, request.Reason, ct, at: request.OccurredAtUtc?.ToUniversalTime());
        }, ct);

    public Task<Result<TreasuryPosting>> RecordTreasuryCashAsync(string key, TreasuryCashCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.cash", request, request.Kind == TreasuryEntryKind.Business ? JahezTreasuryPermissions.EntriesCreate : JahezTreasuryPermissions.Adjust, async () =>
        {
            ValidateMoneyReference(request.Reference); Reason(request.Reason); WorkflowMoney(request.Amount);
            Require(!request.OccurredAtUtc.HasValue || !await TreasuryActive(ct) && request.OccurredAtUtc > DateTimeOffset.MinValue && request.OccurredAtUtc <= Now,
                JahezErrors.Invalid("التأريخ السابق متاح للترحيل قبل التفعيل فقط."));
            Require(request.Amount == LogisticsERP.Domain.Jahez.JahezRules.Money(request.Amount), JahezErrors.Invalid("المبلغ يجب أن يكون بدقة هللتين."));
            Require(request.Amount != 0 && request.Kind is TreasuryEntryKind.Business or TreasuryEntryKind.Opening or TreasuryEntryKind.Adjustment,
                JahezErrors.Invalid("نوع القيد أو قيمته غير صالح."));
            var a = await PostingAccount(request.SubAccountId, ct);
            Require(a.Location is TreasuryLocation.Central or TreasuryLocation.Bank || request.Kind == TreasuryEntryKind.Adjustment,
                JahezErrors.Invalid("القبض والصرف المباشر للخزينة المركزية والبنك فقط."));
            if (request.Kind == TreasuryEntryKind.Opening)
            {
                Require(!await TreasuryActive(ct) && request.Amount > 0
                    && !await db.Set<TreasuryPosting>().AnyAsync(x => x.SubAccountId == a.Id && x.Kind == TreasuryEntryKind.Opening, ct),
                    JahezErrors.Conflict("الرصيد الافتتاحي موجب ويسجل مرة واحدة قبل التفعيل."));
            }
            else if (request.Kind == TreasuryEntryKind.Business)
            {
                await RequireTreasury(ct);
                Require(a.AllowedDirections == TreasuryDirections.Dual
                    || request.Amount > 0 && a.AllowedDirections == TreasuryDirections.InOnly
                    || request.Amount < 0 && a.AllowedDirections == TreasuryDirections.OutOnly, JahezErrors.Invalid("اتجاه الحركة غير مسموح."));
            }
            if (request.Amount < 0) await EnsureAvailable(a, -request.Amount, ct);
            return AddPosting(a, request.Amount, request.Kind, request.Reference, request.Reason, at: request.OccurredAtUtc?.ToUniversalTime());
        }, ct);

    public Task<Result<Guid>> ReverseTreasuryAsync(string key, TreasuryReverseCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.reverse", request, JahezTreasuryPermissions.Adjust, async () =>
        {
            ValidateMoneyReference(request.Reference); Reason(request.Reason);
            var original = await db.Set<TreasuryPosting>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.PostingId, ct);
            Require(original is not null, JahezErrors.NotFound);
            Require(original!.MoneyMovementId is null && original.CashboxHandoverId is null
                && original.Kind != TreasuryEntryKind.Reversal, JahezErrors.Conflict("صحح حركة جاهز من مسارها؛ قيود التسليم المؤكد مقفلة."));
            var entries = original.Kind == TreasuryEntryKind.Transfer
                ? await db.Set<TreasuryPosting>().AsNoTracking().Where(x => x.BatchId == original.BatchId).ToArrayAsync(ct) : [original];
            var ids = entries.Select(x => x.Id).ToArray();
            Require(!await db.Set<TreasuryHandoverPosting>().AnyAsync(l => ids.Contains(l.PostingId)
                && db.JahezCashboxHandovers.Any(h => h.Id == l.HandoverId && h.Status != JahezCashboxHandoverStatus.Cancelled), ct),
                JahezErrors.Conflict("التمويل ضمن تسليم معلق أو مؤكد ولا يمكن عكسه."));
            Require(!await db.Set<TreasuryPosting>().AnyAsync(x => x.ReversesPostingId.HasValue && ids.Contains(x.ReversesPostingId.Value), ct), JahezErrors.Conflict("تم عكس القيد سابقاً."));
            var batch = Guid.CreateVersion7();
            // Stage all reversals before testing resulting balances (a transfer reverses as one unit).
            foreach (var e in entries)
                AddPosting(await PostingAccount(e.SubAccountId, ct), -e.Amount, TreasuryEntryKind.Reversal,
                    request.Reference, request.Reason, batch, reverses: e.Id);
            foreach (var accountId in entries.Select(x => x.SubAccountId).Distinct())
                await EnsureAvailable(await PostingAccount(accountId, ct), 0, ct);
            return batch;
        }, ct);
}
