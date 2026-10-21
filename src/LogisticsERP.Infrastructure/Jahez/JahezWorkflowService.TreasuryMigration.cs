using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private async Task<(Guid AccountId, Guid? InheritedSponsor)> AttributionSource(TreasurySource source, Guid id, CancellationToken ct)
    {
        Guid handover;
        TreasuryOwnership? inherited = null;
        switch (source)
        {
            case TreasurySource.Fee:
                handover = await db.Set<JahezAccountFee>().IgnoreQueryFilters().Where(x => x.Id == id).Select(x => x.HandoverId).SingleOrDefaultAsync(ct);
                break;
            case TreasurySource.Operation:
                handover = await db.JahezOperations.IgnoreQueryFilters().Where(x => x.Id == id).Select(x => x.HandoverId).SingleOrDefaultAsync(ct);
                break;
            case TreasurySource.Debt:
                var debt = await db.JahezTransferredDebts.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id, ct);
                Require(debt is not null, JahezErrors.NotFound);
                inherited = await Ownership(TreasurySource.Operation, debt!.OperationId, ct);
                handover = await db.JahezOperations.IgnoreQueryFilters().Where(x => x.Id == debt.OperationId).Select(x => x.HandoverId).SingleAsync(ct);
                break;
            case TreasurySource.Movement:
                var movement = await db.JahezMoneyMovements.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id, ct);
                Require(movement is not null, JahezErrors.NotFound);
                handover = movement!.HandoverId;
                if (movement.TransferredDebtId.HasValue) inherited = await Ownership(TreasurySource.Debt, movement.TransferredDebtId.Value, ct);
                else if (movement.OperationId.HasValue) inherited = await Ownership(TreasurySource.Operation, movement.OperationId.Value, ct);
                else if (movement.Purpose == JahezMoneyPurpose.AccountFee)
                {
                    var feeId = await db.Set<JahezAccountFee>().IgnoreQueryFilters().Where(x => x.HandoverId == handover).Select(x => x.Id).SingleOrDefaultAsync(ct);
                    inherited = await Ownership(TreasurySource.Fee, feeId, ct);
                }
                break;
            default: throw new JahezBusinessException(JahezErrors.Invalid("نوع المصدر غير صالح."));
        }
        Require(handover != Guid.Empty, JahezErrors.NotFound);
        var accountId = await db.Set<JahezAccountSession>().IgnoreQueryFilters().Where(x => x.Id == handover).Select(x => x.PlatformRiderAccountId).SingleAsync(ct);
        return (accountId, inherited?.SponsorId);
    }

    public Task<Result<TreasuryOwnership>> AttributeTreasuryAsync(string key, TreasuryAttributionCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.attribute", request, JahezTreasuryPermissions.Adjust, async () =>
        {
            Require(!await TreasuryActive(ct), JahezErrors.Conflict("مراجعة الملكية التاريخية متاحة قبل التفعيل فقط."));
            Reason(request.Evidence);
            var source = await AttributionSource(request.Source, request.SourceId, ct);
            Require(!source.InheritedSponsor.HasValue || source.InheritedSponsor == request.SponsorId,
                JahezErrors.Conflict("الملكية تخالف مصدر الالتزام المعتمد."));
            return await CaptureOwnership(request.Source, request.SourceId, source.AccountId, request.SponsorId, request.Evidence, ct);
        }, ct);

    private sealed class AttributionCandidate
    {
        public TreasurySource Source { get; init; }
        public Guid SourceId { get; init; }
        public Guid PlatformAccountId { get; init; }
        public Guid? CurrentSponsorId { get; init; }
    }

    private IQueryable<AttributionCandidate> AttributionCandidates()
    {
        var owners = db.Set<TreasuryOwnership>();
        var sessions = db.Set<JahezAccountSession>().IgnoreQueryFilters();
        var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters();
        var movements = from m in db.JahezMoneyMovements
            join h in sessions on m.HandoverId equals h.Id join a in accounts on h.PlatformRiderAccountId equals a.Id
            where !owners.Any(o => o.Source == TreasurySource.Movement && o.SourceId == m.Id)
            select new AttributionCandidate { Source = TreasurySource.Movement, SourceId = m.Id, PlatformAccountId = a.Id, CurrentSponsorId = a.SponsorId };
        var fees = from f in db.Set<JahezAccountFee>().IgnoreQueryFilters()
            join h in sessions on f.HandoverId equals h.Id join a in accounts on h.PlatformRiderAccountId equals a.Id
            where !owners.Any(o => o.Source == TreasurySource.Fee && o.SourceId == f.Id)
            select new AttributionCandidate { Source = TreasurySource.Fee, SourceId = f.Id, PlatformAccountId = a.Id, CurrentSponsorId = a.SponsorId };
        var operations = from o in db.JahezOperations.IgnoreQueryFilters()
            join h in sessions on o.HandoverId equals h.Id join a in accounts on h.PlatformRiderAccountId equals a.Id
            where o.Status == JahezOperationStatus.Executed && !owners.Any(x => x.Source == TreasurySource.Operation && x.SourceId == o.Id)
            select new AttributionCandidate { Source = TreasurySource.Operation, SourceId = o.Id, PlatformAccountId = a.Id, CurrentSponsorId = a.SponsorId };
        var debts = from d in db.JahezTransferredDebts.IgnoreQueryFilters()
            join o in db.JahezOperations.IgnoreQueryFilters() on d.OperationId equals o.Id
            join h in sessions on o.HandoverId equals h.Id join a in accounts on h.PlatformRiderAccountId equals a.Id
            where !owners.Any(x => x.Source == TreasurySource.Debt && x.SourceId == d.Id)
            select new AttributionCandidate { Source = TreasurySource.Debt, SourceId = d.Id, PlatformAccountId = a.Id, CurrentSponsorId = a.SponsorId };
        return fees.Concat(operations).Concat(debts).Concat(movements);
    }

    public Task<Result<JahezPage<TreasuryAttributionReview>>> TreasuryAttributionReviewAsync(int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.Adjust, async () =>
        {
            Page(page, pageSize);
            var rows = await AttributionCandidates().OrderBy(x => x.Source).ThenBy(x => x.SourceId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            var ids = rows.Select(x => x.PlatformAccountId).Concat(rows.Select(x => x.SourceId)).ToArray();
            var evidence = await db.Set<AuditEntry>().AsNoTracking().Where(x => x.EntityId.HasValue && ids.Contains(x.EntityId.Value))
                .OrderBy(x => x.OccurredAtUtc).Select(x => new { x.EntityId, x.ActorUserId, x.Action, x.OccurredAtUtc, x.BeforeJson, x.AfterJson, x.Reason }).ToArrayAsync(ct);
            return new JahezPage<TreasuryAttributionReview>(rows.Select(x => new TreasuryAttributionReview(x.Source, x.SourceId, x.PlatformAccountId, x.CurrentSponsorId, true,
                JsonSerializer.Serialize(new { CurrentSponsorIsNotHistoricalProof = true,
                    Events = evidence.Where(e => e.EntityId == x.PlatformAccountId || e.EntityId == x.SourceId) }))).ToArray(), page, pageSize);
        }, ct);

    private async Task<TreasuryReadiness> Readiness(CancellationToken ct)
    {
        var state = await db.Set<TreasuryState>().AsNoTracking().SingleAsync(x => x.Id == TreasuryState.SingletonId, ct);
        var unresolved = AttributionCandidates();
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().Where(x => x.IsPosting && x.IsActive).ToArrayAsync(ct);
        var unverified = new List<Guid>();
        foreach (var account in accounts)
        {
            var verification = await db.Set<TreasuryBalanceVerification>().AsNoTracking().Where(x => x.SubAccountId == account.Id)
                .OrderByDescending(x => x.VerifiedAtUtc).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            var lastPosting = await db.Set<TreasuryPosting>().Where(x => x.SubAccountId == account.Id)
                .Select(x => (DateTimeOffset?)x.CreatedAtUtc).MaxAsync(ct);
            if (verification is null || verification.VerifiedBalance != await AccountBalance(account.Id, ct)
                || lastPosting > verification.VerifiedAtUtc) unverified.Add(account.Id);
        }
        return new(state.IsActive, Convert.ToBase64String(state.RowVersion),
            await unresolved.CountAsync(x => x.Source == TreasurySource.Movement || x.Source == TreasurySource.Debt, ct),
            await unresolved.CountAsync(x => x.Source == TreasurySource.Fee, ct),
            await unresolved.CountAsync(x => x.Source == TreasurySource.Operation, ct),
            await db.JahezCashboxHandovers.CountAsync(x => x.Status == JahezCashboxHandoverStatus.Pending, ct), unverified);
    }

    public Task<Result<TreasuryReadiness>> TreasuryReadinessAsync(CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.Adjust, () => Readiness(ct), ct);

    public Task<Result<TreasuryBalanceVerification>> VerifyTreasuryBalanceAsync(string key, TreasuryVerificationCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.verify", request, JahezTreasuryPermissions.Adjust, async () =>
        {
            ValidateMoneyReference(request.Reference); Reason(request.Reason); WorkflowMoney(request.Balance);
            await PostingAccount(request.SubAccountId, ct);
            Require(await AccountBalance(request.SubAccountId, ct) == request.Balance && request.Balance >= 0,
                JahezErrors.Conflict("الرصيد الفعلي لا يطابق الدفتر أو سالب؛ سجل تسوية معتمدة أولاً."));
            var verification = new TreasuryBalanceVerification { SubAccountId = request.SubAccountId,
                VerifiedBalance = request.Balance, VerifiedAtUtc = Now, Reference = request.Reference, Reason = request.Reason };
            db.Add(verification); return verification;
        }, ct);

    public Task<Result<TreasuryState>> ActivateTreasuryAsync(string key, TreasuryActivationCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.activate", request, JahezTreasuryPermissions.Adjust, async () =>
        {
            Reason(request.Reason);
            var state = await db.Set<TreasuryState>().SingleAsync(x => x.Id == TreasuryState.SingletonId, ct);
            Require(!state.IsActive && Convert.ToBase64String(state.RowVersion) == request.RowVersion, JahezErrors.Conflict("تغيرت حالة الخزينة؛ أعد تحميلها."));
            var readiness = await Readiness(ct);
            Require(readiness.UnresolvedMovements + readiness.UnresolvedFees + readiness.UnresolvedExecutedOperations == 0
                && readiness.PendingLegacyHandovers == 0 && readiness.UnverifiedAccounts.Count == 0,
                JahezErrors.Conflict("لا يمكن التفعيل مع ملكيات غير موثقة أو تسليم معلق أو أرصدة غير مطابقة."));
            Require(!await db.JahezMoneyMovements.AnyAsync(m => m.Amount > 0 && !db.Set<TreasuryPosting>().Any(p => p.MoneyMovementId == m.Id && p.Kind == TreasuryEntryKind.Business), ct),
                JahezErrors.Conflict("أكمل ترحيل الحركات أولاً."));
            Require(!await db.JahezCashboxHandovers.AnyAsync(h => h.Status == JahezCashboxHandoverStatus.Confirmed
                && !db.Set<TreasuryHandoverLine>().Any(l => l.HandoverId == h.Id), ct), JahezErrors.Conflict("أكمل ترحيل التسليمات المؤكدة أولاً."));
            state.IsActive = true; state.ActivatedAtUtc = Now; return state;
        }, ct);

    public Task<Result<int>> BackfillTreasuryAsync(string key, string reason, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.backfill", new { reason }, JahezTreasuryPermissions.Adjust, async () =>
        {
            Reason(reason); Require(!await TreasuryActive(ct), JahezErrors.Conflict("الترحيل التاريخي متاح قبل التفعيل فقط."));
            var debts = await db.JahezTransferredDebts.IgnoreQueryFilters().Where(d => !db.Set<TreasuryOwnership>().Any(o => o.Source == TreasurySource.Debt && o.SourceId == d.Id)).Take(500).ToArrayAsync(ct);
            foreach (var debt in debts)
            {
                var origin = await Ownership(TreasurySource.Operation, debt.OperationId, ct);
                if (origin is not null) await CaptureOwnership(TreasurySource.Debt, debt.Id, origin.PlatformAccountId, origin.SponsorId, $"Approved operation {debt.OperationId}; backfill:{key}", ct);
            }
            var movements = await db.JahezMoneyMovements.Where(m => !db.Set<TreasuryPosting>().Any(p => p.MoneyMovementId == m.Id && p.Kind == TreasuryEntryKind.Business))
                .Where(m => m.Amount > 0).OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id).Take(500).ToArrayAsync(ct);
            var count = 0;
            foreach (var movement in movements)
            {
                var owner = await Ownership(TreasurySource.Movement, movement.Id, ct);
                if (owner is null)
                {
                    var source = await AttributionSource(TreasurySource.Movement, movement.Id, ct);
                    if (!source.InheritedSponsor.HasValue) continue;
                    owner = await CaptureOwnership(TreasurySource.Movement, movement.Id, source.AccountId,
                        source.InheritedSponsor.Value, $"Inherited from approved source; backfill:{key}", ct);
                }
                await PostMoney(movement, owner, false, ct, historical: true); count++;
            }
            // Persist postings before reconstructing legacy handovers; the surrounding executor owns rollback.
            await db.SaveChangesAsync(ct);
            var handovers = await db.JahezCashboxHandovers.Where(h => h.Status == JahezCashboxHandoverStatus.Confirmed
                && !db.Set<TreasuryHandoverLine>().Any(l => l.HandoverId == h.Id)).OrderBy(h => h.ConfirmedAtUtc).Take(100).ToArrayAsync(ct);
            foreach (var h in handovers)
            {
                var linked = await (from link in db.JahezCashboxHandoverMovements join m in db.JahezMoneyMovements on link.MoneyMovementId equals m.Id
                    where link.CashboxHandoverId == h.Id select m).ToArrayAsync(ct);
                var linkedIds = linked.Select(m => m.Id).ToArray();
                var postedIds = await db.Set<TreasuryPosting>().Where(p => p.MoneyMovementId.HasValue && linkedIds.Contains(p.MoneyMovementId.Value)
                    && p.Kind == TreasuryEntryKind.Business).Select(p => p.MoneyMovementId!.Value).Distinct().ToArrayAsync(ct);
                if (linked.Any(m => m.Amount > 0 && !postedIds.Contains(m.Id))) continue;
                var lines = await BuildHandoverLines(linked, h.Id, ct);
                var legacyReference = $"legacy:{h.Id}";
                var historicalFunding = await db.Set<TreasuryPosting>().AsNoTracking().Where(p => p.Reference == legacyReference
                    && p.Kind == TreasuryEntryKind.Transfer && p.Amount > 0 && p.CashboxHandoverId == null).ToArrayAsync(ct);
                foreach (var line in lines)
                {
                    var from = await PostingAccount(line.SubAccountId, ct);
                    if (from.Location == TreasuryLocation.Supervisor)
                    {
                        line.Amount += historicalFunding.Where(p => p.SubAccountId == from.Id).Sum(p => p.Amount);
                        line.MovementCount += historicalFunding.Count(p => p.SubAccountId == from.Id);
                    }
                    if (from.Location == TreasuryLocation.Supervisor && line.Amount != 0)
                    {
                        Require(line.Amount > 0, JahezErrors.Conflict($"التسليم التاريخي {h.Id} يحتوي عجزاً نقدياً؛ يلزم توثيق تسوية التمويل ومراجعة المالية قبل الترحيل."));
                        var to = await Route(new TreasuryOwnership { SponsorId = line.SponsorId }, JahezMoneyFund.Cashbox, from.Purpose == TreasuryPurpose.AccountFee ? JahezMoneyPurpose.AccountFee : JahezMoneyPurpose.Settlement, ct, central: true);
                        await Transfer(from, to, line.Amount, $"legacy:{h.Id}", reason, ct, h.Id, historical: true, at: h.ConfirmedAtUtc);
                    }
                    db.Add(line);
                }
                var movementIds = linked.Select(x => x.Id).ToArray();
                var postingIds = await db.Set<TreasuryPosting>().Where(p => p.MoneyMovementId.HasValue && movementIds.Contains(p.MoneyMovementId.Value))
                    .Select(p => p.Id).ToArrayAsync(ct);
                db.AddRange(postingIds.Select(p => new TreasuryHandoverPosting { HandoverId = h.Id, PostingId = p }));
                db.AddRange(historicalFunding.Select(p => new TreasuryHandoverPosting { HandoverId = h.Id, PostingId = p.Id }));
                count++;
            }
            return count;
        }, ct);
}
