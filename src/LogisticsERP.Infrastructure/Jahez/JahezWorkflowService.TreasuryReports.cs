using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private static (DateTimeOffset From, DateTimeOffset To) TreasuryDates(TreasuryFilter filter)
    {
        Page(filter.Page, filter.PageSize);
        Require(filter.FromDate > DateOnly.MinValue && filter.ToDate < DateOnly.MaxValue && filter.FromDate <= filter.ToDate,
            JahezErrors.Invalid("نطاق التاريخ غير صالح."));
        return (JahezRules.StartOfDay(filter.FromDate).ToUniversalTime(), JahezRules.StartOfDay(filter.ToDate).ToUniversalTime().AddDays(1));
    }

    private IQueryable<TreasuryPosting> FilterPostings(TreasuryFilter filter, bool period = true)
    {
        var (from, to) = TreasuryDates(filter);
        var query = db.Set<TreasuryPosting>().AsNoTracking();
        if (period) query = query.Where(x => x.OccurredAtUtc >= from && x.OccurredAtUtc < to);
        if (filter.SponsorId.HasValue) query = query.Where(x => x.SponsorId == filter.SponsorId);
        if (filter.SubAccountId.HasValue) query = query.Where(x => x.SubAccountId == filter.SubAccountId);
        if (filter.Purpose.HasValue) query = query.Where(x => db.Set<TreasurySubAccount>().Any(a => a.Id == x.SubAccountId && a.Purpose == filter.Purpose));
        return query;
    }

    private IQueryable<TreasuryJournalRow> JournalRows(IQueryable<TreasuryPosting> postings) =>
        from p in postings
        join account in db.Set<TreasurySubAccount>().IgnoreQueryFilters() on p.SubAccountId equals account.Id
        join sponsor in db.Sponsors.IgnoreQueryFilters() on p.SponsorId equals sponsor.Id
        join movement in db.JahezMoneyMovements on p.MoneyMovementId equals movement.Id into movements
        from movement in movements.DefaultIfEmpty()
        join owner in db.Set<TreasuryOwnership>().Where(x => x.Source == TreasurySource.Movement)
            on p.MoneyMovementId equals owner.SourceId into owners from owner in owners.DefaultIfEmpty()
        join rider in db.RiderProfiles.IgnoreQueryFilters() on movement.RiderProfileId equals rider.Id into riders
        from rider in riders.DefaultIfEmpty()
        join employee in db.Employees.IgnoreQueryFilters() on rider.EmployeeId equals employee.Id into employees
        from employee in employees.DefaultIfEmpty()
        select new TreasuryJournalRow(p, account.Code, account.NameAr, owner == null ? sponsor.RegistryNameAr : owner.SponsorNameAr,
            owner == null ? null : owner.PlatformAccountId, movement == null ? null : movement.OperationId,
            movement == null ? null : movement.TransferredDebtId, movement == null ? null : movement.Purpose,
            p.CashboxHandoverId != null || db.JahezCashboxHandoverMovements.Any(l => l.MoneyMovementId == p.MoneyMovementId
                && db.JahezCashboxHandovers.Any(h => h.Id == l.CashboxHandoverId && h.Status == JahezCashboxHandoverStatus.Confirmed)),
            rider == null ? null : rider.Id, employee == null ? null : employee.FullNameAr, employee == null ? null : employee.IqamaNo);

    public Task<Result<JahezPage<TreasuryJournalRow>>> TreasuryJournalAsync(TreasuryFilter filter, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.Read, async () =>
        {
            var query = FilterPostings(filter).OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
                .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
            return new JahezPage<TreasuryJournalRow>(await JournalRows(query).ToArrayAsync(ct), filter.Page, filter.PageSize);
        }, ct);

    public Task<Result<IReadOnlyList<TreasuryBalance>>> TreasuryBalancesAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<TreasuryBalance>>(JahezTreasuryPermissions.Read, async () =>
        {
            var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().OrderBy(x => x.Code).ToArrayAsync(ct);
            var totals = await db.Set<TreasuryPosting>().GroupBy(x => x.SubAccountId)
                .Select(g => new { Id = g.Key, Amount = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.Id, x => x.Amount, ct);
            decimal Sum(TreasurySubAccount a) => a.IsPosting ? totals.GetValueOrDefault(a.Id) : accounts.Where(x => x.ParentId == a.Id).Sum(Sum);
            return accounts.Select(a => new TreasuryBalance(a.Id, a.Code, a.NameAr, a.ParentId, a.SponsorId, a.Location, a.IsPosting, Sum(a))).ToArray();
        }, ct);

    public Task<Result<PetroAppEvidence>> RecordPetroEvidenceAsync(string key, PetroEvidenceCreate request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v3.petro.evidence", request, JahezTreasuryPermissions.EvidenceCreate, async () =>
        {
            ValidateMoneyReference(request.Reference); Reason(request.Reason); WorkflowMoney(request.Amount, true); WorkflowMoney(request.AllocatedCollections, true);
            Require(request.Amount == JahezRules.Money(request.Amount) && request.AllocatedCollections == JahezRules.Money(request.AllocatedCollections),
                JahezErrors.Invalid("المبالغ يجب أن تكون بدقة هللتين."));
            Require(Enum.IsDefined(request.Kind) && request.BusinessDate > DateOnly.MinValue && request.BusinessDate <= Today
                && (request.Kind != PetroEvidenceKind.Statement || request.AllocatedCollections == 0), JahezErrors.Invalid("بيانات دليل بترو آب غير صالحة."));
            Require(await db.Sponsors.AnyAsync(x => x.Id == request.SponsorId, ct), JahezErrors.NotFound);
            Require(!await db.Set<PetroAppEvidence>().AnyAsync(x => x.SponsorId == request.SponsorId && x.Reference == request.Reference.Trim(), ct), JahezErrors.Conflict("مرجع الدليل مستخدم."));
            if (request.ReplacesId.HasValue)
            {
                var original = await db.Set<PetroAppEvidence>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.ReplacesId, ct);
                Require(original is not null && original.SponsorId == request.SponsorId && original.Kind == request.Kind,
                    JahezErrors.Invalid("الدليل البديل يجب أن يخص نفس الكفيل والنوع."));
                Require(!await db.Set<PetroAppEvidence>().AnyAsync(x => x.ReplacesId == request.ReplacesId, ct), JahezErrors.Conflict("تم استبدال الدليل سابقاً."));
            }
            var evidence = new PetroAppEvidence { SponsorId = request.SponsorId, Kind = request.Kind, BusinessDate = request.BusinessDate,
                Amount = request.Amount, AllocatedCollections = request.AllocatedCollections, Reference = request.Reference.Trim(),
                Reason = request.Reason, ReplacesId = request.ReplacesId };
            db.Add(evidence); return evidence;
        }, ct);

    public Task<Result<JahezPage<PetroAppEvidence>>> PetroEvidenceAsync(TreasuryFilter filter, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.Read, async () =>
        {
            TreasuryDates(filter);
            var query = db.Set<PetroAppEvidence>().AsNoTracking().Where(x => x.BusinessDate >= filter.FromDate && x.BusinessDate <= filter.ToDate);
            if (filter.SponsorId.HasValue) query = query.Where(x => x.SponsorId == filter.SponsorId);
            return new JahezPage<PetroAppEvidence>(await query.OrderByDescending(x => x.BusinessDate).ThenBy(x => x.Id)
                .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArrayAsync(ct), filter.Page, filter.PageSize);
        }, ct);

    private async Task<IReadOnlyList<PetroReconciliation>> PetroReport(TreasuryFilter filter, CancellationToken ct)
    {
        var sponsors = await db.Sponsors.IgnoreQueryFilters().AsNoTracking()
            .Where(s => db.Set<TreasurySubAccount>().Any(a => a.SponsorId == s.Id && a.Location == TreasuryLocation.PetroApp))
            .Where(s => !filter.SponsorId.HasValue || s.Id == filter.SponsorId).ToArrayAsync(ct);
        var activeEvidence = db.Set<PetroAppEvidence>().AsNoTracking().Where(x => !db.Set<PetroAppEvidence>().Any(y => y.ReplacesId == x.Id));
        var result = new List<PetroReconciliation>();
        foreach (var sponsor in sponsors)
        {
            var collections = await FilterPostings(filter with { SponsorId = sponsor.Id }).Where(p => db.Set<TreasurySubAccount>()
                .Any(a => a.Id == p.SubAccountId && a.Location == TreasuryLocation.PetroApp)).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
            var funding = await activeEvidence.Where(e => e.SponsorId == sponsor.Id && e.Kind == PetroEvidenceKind.Funding
                && e.BusinessDate >= filter.FromDate && e.BusinessDate <= filter.ToDate).ToArrayAsync(ct);
            var statements = activeEvidence.Where(x => x.SponsorId == sponsor.Id && x.Kind == PetroEvidenceKind.Statement);
            var openingDate = filter.FromDate.AddDays(-1);
            var opening = await statements.Where(x => x.BusinessDate == openingDate).OrderByDescending(x => x.Id)
                .Select(x => (decimal?)x.Amount).FirstOrDefaultAsync(ct);
            var closing = await statements.Where(x => x.BusinessDate == filter.ToDate).OrderByDescending(x => x.Id).Select(x => (decimal?)x.Amount).FirstOrDefaultAsync(ct);
            decimal? consumption = null;
            // Existing fuel imports contain monthly totals. Never present a partial-month total as exact daily usage.
            if (filter.FromDate.Day == 1 && filter.ToDate.Day == DateTime.DaysInMonth(filter.ToDate.Year, filter.ToDate.Month))
            {
                var imports = await db.FuelCardImports.AsNoTracking().Where(i => i.Provider == FuelCardProvider.PetroApp
                    && i.ReportMonth >= filter.FromDate && i.ReportMonth <= filter.ToDate)
                    .Select(i => new { i.ReportMonth, i.ReportThroughAtUtc, i.CreatedAtUtc, i.Id }).ToArrayAsync(ct);
                var expectedMonths = (filter.ToDate.Year - filter.FromDate.Year) * 12 + filter.ToDate.Month - filter.FromDate.Month + 1;
                var completeMonths = imports.GroupBy(i => i.ReportMonth).Select(g => g.OrderByDescending(i => i.CreatedAtUtc).ThenByDescending(i => i.Id).First())
                    .Count(i => !i.ReportThroughAtUtc.HasValue || JahezRules.RiyadhDate(i.ReportThroughAtUtc.Value).Day == DateTime.DaysInMonth(i.ReportMonth.Year, i.ReportMonth.Month));
                var changedCardOwnership = await db.Set<AuditEntry>().AnyAsync(a => a.EntityType == "FuelCard" && a.BeforeJson != null
                    && a.BeforeJson.Contains("SponsorId") && db.FuelCards.Any(c => c.Id == a.EntityId && c.SponsorId == sponsor.Id), ct);
                if (completeMonths == expectedMonths && !changedCardOwnership)
                    consumption = await (from usage in db.FuelCardMonthlyUsages.AsNoTracking()
                        join card in db.FuelCards on usage.FuelCardId equals card.Id
                        where card.SponsorId == sponsor.Id && card.Provider == FuelCardProvider.PetroApp
                            && usage.ReportMonth >= filter.FromDate && usage.ReportMonth <= filter.ToDate
                        select (decimal?)usage.TotalAmount).SumAsync(ct) ?? 0;
            }
            decimal? total = funding.Length == 0 ? null : funding.Sum(x => x.Amount);
            decimal? allocated = funding.Length == 0 ? null : funding.Sum(x => x.AllocatedCollections);
            var status = total.HasValue && opening.HasValue && closing.HasValue && consumption.HasValue
                ? allocated == collections && allocated == total && opening + total - consumption == closing ? "Matched" : "Difference"
                : "Unverified";
            result.Add(new(sponsor.Id, sponsor.RegistryNameAr, collections, total, allocated,
                allocated.HasValue ? collections - allocated : null, total.HasValue ? allocated - total : null,
                opening, closing, consumption, status));
        }
        return result;
    }

    private async Task<TreasuryReport> BuildTreasuryReport(string type, TreasuryFilter filter, Guid? handoverId, bool export, CancellationToken ct)
    {
        Require(type is "movements" or "subaccounts" or "trading" or "petroapp" or "handover", JahezErrors.Invalid("نوع التقرير غير صالح."));
        var (from, to) = TreasuryDates(filter);
        if (type == "trading") filter = filter with { SponsorId = Sponsor.AlBawabaCommercialEstablishmentId };
        if (type == "handover")
        {
            Require(handoverId.HasValue, JahezErrors.Invalid("رقم التسليم مطلوب."));
            var detail = await HandoverDetail(handoverId!.Value, ct);
            Require(detail.Handover.Status == JahezCashboxHandoverStatus.Confirmed, JahezErrors.Conflict("محضر التسليم المعتمد متاح بعد التأكيد."));
            // Read-only enrichment for legacy records; preserve recorded names whenever available.
            if (!HasTreasuryDisplayName(detail.Handover.RequestedByNameAr))
                detail.Handover.RequestedByNameAr = await TreasuryUserName(detail.Handover.RequestedByUserId, ct);
            if (!HasTreasuryDisplayName(detail.Handover.AccountantNameAr) && detail.Handover.AccountantUserId is { } accountant)
                detail.Handover.AccountantNameAr = await TreasuryUserName(accountant, ct);
            return new(type, filter, [], [], [], detail, 0);
        }
        var query = FilterPostings(filter);
        var count = await query.CountAsync(ct);
        Require(!export || count <= 10000, JahezErrors.Invalid("التصدير محدود بعشرة آلاف حركة؛ ضيق نطاق الفلاتر."));
        var rows = type is "petroapp" or "subaccounts" ? [] : await JournalRows(query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip(export ? 0 : (filter.Page - 1) * filter.PageSize).Take(export ? 10000 : filter.PageSize)).ToArrayAsync(ct);
        var missingNames = rows.Where(x => !HasTreasuryDisplayName(x.Posting.ActorNameAr)).GroupBy(x => x.Posting.ActorUserId).ToArray();
        var userNames = await TreasuryUserNames(missingNames.Select(x => x.Key).ToArray(), ct);
        foreach (var group in missingNames)
        {
            var name = userNames.GetValueOrDefault(group.Key, "اسم المستخدم غير متاح");
            foreach (var row in group) row.Posting.ActorNameAr = name;
        }
        var accounts = await db.Set<TreasurySubAccount>().AsNoTracking().OrderBy(x => x.Code).ToArrayAsync(ct);
        var data = await FilterPostings(filter, period: false).Where(x => x.OccurredAtUtc < to)
            .GroupBy(x => x.SubAccountId).Select(g => new { Id = g.Key,
                Opening = g.Where(x => x.OccurredAtUtc < from).Sum(x => x.Amount),
                Receipts = g.Where(x => x.OccurredAtUtc >= from && x.Amount > 0 && x.Kind != TreasuryEntryKind.Transfer).Sum(x => x.Amount),
                Payments = -g.Where(x => x.OccurredAtUtc >= from && x.Amount < 0 && x.Kind != TreasuryEntryKind.Transfer).Sum(x => x.Amount),
                TransfersIn = g.Where(x => x.OccurredAtUtc >= from && x.Amount > 0 && x.Kind == TreasuryEntryKind.Transfer).Sum(x => x.Amount),
                TransfersOut = -g.Where(x => x.OccurredAtUtc >= from && x.Amount < 0 && x.Kind == TreasuryEntryKind.Transfer).Sum(x => x.Amount),
                Closing = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.Id, ct);
        TreasuryAccountPeriod Period(TreasurySubAccount a)
        {
            var children = a.IsPosting ? [] : accounts.Where(c => c.ParentId == a.Id).Select(Period).ToArray();
            var d = data.GetValueOrDefault(a.Id);
            return new(a.Id, a.Code, a.NameAr, a.ParentId, a.SponsorId, a.IsPosting, d?.Opening ?? children.Sum(c => c.Opening),
                d?.Receipts ?? children.Sum(c => c.Receipts), d?.Payments ?? children.Sum(c => c.Payments),
                d?.TransfersIn ?? children.Sum(c => c.TransfersIn), d?.TransfersOut ?? children.Sum(c => c.TransfersOut), d?.Closing ?? children.Sum(c => c.Closing));
        }
        var periods = accounts.Where(a => !a.IsPosting || (!filter.SponsorId.HasValue || a.SponsorId == filter.SponsorId)
            && (!filter.SubAccountId.HasValue || a.Id == filter.SubAccountId)
            && (!filter.Purpose.HasValue || a.Purpose == filter.Purpose)).Select(Period).ToArray();
        return new(type, filter, periods, rows, type == "petroapp" ? await PetroReport(filter, ct) : [], null, count);
    }

    public Task<Result<TreasuryReport>> TreasuryReportAsync(string type, TreasuryFilter filter, Guid? handoverId = null, CancellationToken ct = default) =>
        ReadAsync(JahezTreasuryPermissions.ReportsRead, () => BuildTreasuryReport(type, filter, handoverId, false, ct), ct);
}
