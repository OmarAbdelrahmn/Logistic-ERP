using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    private sealed class PerformanceMetric
    {
        public Guid AccountId { get; init; }
        public Guid RiderProfileId { get; init; }
        public DateOnly Date { get; init; }
        public long Orders { get; init; }
    }
    private sealed class PerformanceMember
    {
        public Guid AccountId { get; init; }
        public string ExternalAccountId { get; init; } = string.Empty;
        public Guid RiderProfileId { get; init; }
        public string RiderName { get; init; } = string.Empty;
        public string? RiderIqamaNo { get; init; }
    }

    // Historical account ownership is independent of who currently uses the account.
    private IQueryable<PlatformRiderAccount> PerformanceAccounts =>
        db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking()
            .Where(a => db.ClientPlatforms.Any(p => p.Id == a.ClientPlatformId && p.Code == "JAHEZ" && !p.IsDeleted));

    private IQueryable<PerformanceMetric> RiderPerformanceMetrics(DateOnly startDate, DateOnly endDate,
        Guid? riderId, Guid? accountId) =>
        from d in db.Set<JahezDailyDispatch>().AsNoTracking()
        join h in db.JahezAccountSessions.AsNoTracking() on d.HandoverId equals h.Id
        join a in PerformanceAccounts on h.PlatformRiderAccountId equals a.Id
        where !h.IsDeleted && d.Date >= startDate && d.Date <= endDate && (!riderId.HasValue || h.RiderProfileId == riderId)
            && (!accountId.HasValue || a.Id == accountId)
            && db.Set<JahezImportBatch>().Any(b => b.Id == d.BatchId && !b.IsDeleted && b.Kind == JahezImportKind.DailyDispatches && b.CommittedAtUtc != null)
            && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && !b.IsDeleted && b.CommittedAtUtc != null)
        group d by new { AccountId = a.Id, h.RiderProfileId, d.Date } into g
        select new PerformanceMetric { AccountId = g.Key.AccountId, RiderProfileId = g.Key.RiderProfileId,
            Date = g.Key.Date, Orders = g.Sum(x => (long)x.Count) };

    private IQueryable<PerformanceMember> RiderPerformanceMembers(DateOnly startDate, DateOnly endDate,
        Guid? riderId, Guid? accountId)
    {
        var fromUtc = JahezRules.StartOfDay(startDate).ToUniversalTime();
        var toExclusiveUtc = JahezRules.StartOfDay(endDate).ToUniversalTime().AddDays(1);
        var roster = (from h in db.JahezAccountSessions.AsNoTracking()
            join a in PerformanceAccounts on h.PlatformRiderAccountId equals a.Id
            where !h.IsDeleted && h.StartedAtUtc < toExclusiveUtc && (h.EndedAtUtc == null || h.EndedAtUtc > fromUtc)
                && (!riderId.HasValue || h.RiderProfileId == riderId) && (!accountId.HasValue || a.Id == accountId)
            select new { AccountId = a.Id, h.RiderProfileId })
            .Union(RiderPerformanceMetrics(startDate, endDate, riderId, accountId)
                .Select(m => new { m.AccountId, m.RiderProfileId }));
        return from member in roster
            join account in PerformanceAccounts on member.AccountId equals account.Id
            join rider in db.RiderProfiles.AsNoTracking() on member.RiderProfileId equals rider.Id
            join employee in db.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
            where !rider.IsDeleted && !employee.IsDeleted
            select new PerformanceMember { AccountId = account.Id, ExternalAccountId = account.ExternalAccountId,
                RiderProfileId = rider.Id, RiderName = employee.FullNameAr, RiderIqamaNo = employee.IqamaNo };
    }

    private static JahezPerformanceDay[] PerformanceDays(IEnumerable<PerformanceMetric> metrics, DateOnly start, int periodDays)
    {
        var byDate = metrics.GroupBy(m => m.Date).ToDictionary(g => g.Key, g => g.Sum(m => m.Orders));
        return Enumerable.Range(0, periodDays).Select(offset =>
        {
            var date = start.AddDays(offset);
            return new JahezPerformanceDay(date, byDate.TryGetValue(date, out var orders) ? orders : null);
        }).ToArray();
    }

    private static JahezPerformanceSummary PerformanceSummary(JahezPerformanceDay[] days)
    {
        var known = days.Where(d => d.Orders.HasValue).Select(d => d.Orders!.Value).ToArray();
        var orders = known.Sum();
        return new(days.Length, known.Length, known.Count(x => x > 0), known.Count(x => x == 0), days.Length - known.Length,
            orders, known.Length == 0 ? null : decimal.Round(orders / (decimal)known.Length, 2, MidpointRounding.AwayFromZero),
            known.Length == 0 ? null : known.Max());
    }

    public Task<Result<JahezRidersPerformanceReport>> RiderPerformanceReportAsync(DateOnly startDate, DateOnly endDate,
        Guid? riderId = null, Guid? accountId = null, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            Require(startDate > DateOnly.MinValue && startDate <= endDate && endDate <= Today
                && endDate.DayNumber - startDate.DayNumber < 366,
                JahezErrors.Invalid("أدخل تاريخ بداية ونهاية صحيحين حتى اليوم، بفترة لا تتجاوز 366 يوماً."));
            Require(riderId != Guid.Empty && accountId != Guid.Empty, JahezErrors.Invalid("معرف المندوب أو الحساب غير صالح."));
            if (riderId.HasValue)
                Require(await db.RiderProfiles.AsNoTracking().AnyAsync(r => r.Id == riderId, ct), JahezErrors.NotFound);
            if (accountId.HasValue)
                Require(await PerformanceAccounts.AnyAsync(a => a.Id == accountId, ct), JahezErrors.NotFound);
            var members = RiderPerformanceMembers(startDate, endDate, riderId, accountId);
            var riderQuery = members.Select(m => new { m.RiderProfileId, m.RiderName, m.RiderIqamaNo }).Distinct();
            var total = await riderQuery.CountAsync(ct);
            var selected = await riderQuery.OrderBy(r => r.RiderName).ThenBy(r => r.RiderProfileId)
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            var ids = selected.Select(r => r.RiderProfileId).ToArray();
            var periodDays = endDate.DayNumber - startDate.DayNumber + 1;
            if (ids.Length == 0) return new JahezRidersPerformanceReport(startDate, endDate, riderId, accountId, page, pageSize, total, []);
            // Page riders in SQL before fetching their account roster and grouped daily metrics.
            var accounts = await members.Where(m => ids.Contains(m.RiderProfileId))
                .OrderBy(m => m.ExternalAccountId).ThenBy(m => m.AccountId).ToArrayAsync(ct);
            var metrics = await RiderPerformanceMetrics(startDate, endDate, riderId, accountId)
                .Where(m => ids.Contains(m.RiderProfileId)).ToArrayAsync(ct);
            var byRider = metrics.ToLookup(m => m.RiderProfileId);
            var byAccountRider = metrics.ToLookup(m => (m.AccountId, m.RiderProfileId));
            var byRiderAccounts = accounts.ToLookup(a => a.RiderProfileId);
            var items = selected.Select(r =>
            {
                var daily = PerformanceDays(byRider[r.RiderProfileId], startDate, periodDays);
                var breakdown = byRiderAccounts[r.RiderProfileId].Select(a =>
                {
                    var accountDaily = PerformanceDays(byAccountRider[(a.AccountId, a.RiderProfileId)], startDate, periodDays);
                    return new JahezAccountPerformanceReport(a.AccountId, a.ExternalAccountId, PerformanceSummary(accountDaily), accountDaily);
                }).ToArray();
                return new JahezRiderPerformanceReport(r.RiderProfileId, r.RiderName, r.RiderIqamaNo, PerformanceSummary(daily), daily, breakdown);
            }).ToArray();
            return new JahezRidersPerformanceReport(startDate, endDate, riderId, accountId, page, pageSize, total, items);
        }, ct);
}
