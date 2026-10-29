using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaPerformanceReport>> GetPerformanceReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, CancellationToken ct = default)
    {
        var source = await ReportSource(dateFrom, dateTo, accountId, riderId, ct);
        if (source.IsFailure) return Result.Failure<NinjaPerformanceReport>(source.Error);
        var totals = await GroupReport(source.Value!, false, false).SingleOrDefaultAsync(ct) ?? new ReportGroup();
        return Result.Success(new NinjaPerformanceReport(dateFrom, dateTo, accountId, riderId, totals.PerformanceDays,
            totals.DeductionDays, totals.Accounts, totals.Riders, Metrics(totals)));
    }

    public async Task<Result<NinjaPage<NinjaDailyReport>>> GetDailyReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId,
        int page, int pageSize, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize)) return Result.Failure<NinjaPage<NinjaDailyReport>>(NinjaErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var source = await ReportSource(dateFrom, dateTo, accountId, riderId, ct);
        if (source.IsFailure) return Result.Failure<NinjaPage<NinjaDailyReport>>(source.Error);
        var facts = source.Value!;
        var dates = facts.Select(f => f.Date).Distinct().OrderBy(date => date)
            .Skip((page - 1) * pageSize).Take(pageSize);
        var rows = await GroupReport(facts.Where(f => dates.Contains(f.Date)), true, false, sponsors: true)
            .OrderBy(r => r.Date).ThenBy(r => r.SponsorId).ToArrayAsync(ct);
        if (rows.Length == 0) return Result.Success(new NinjaPage<NinjaDailyReport>([], page, pageSize));
        var sponsorIds = rows.Where(r => r.SponsorId.HasValue).Select(r => r.SponsorId!.Value).Distinct().ToArray();
        var names = await db.Sponsors.IgnoreQueryFilters().AsNoTracking().Where(s => sponsorIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.RegistryNameAr, ct);
        return Result.Success(new NinjaPage<NinjaDailyReport>(rows.GroupBy(r => r.Date).Select(day => new NinjaDailyReport(day.Key,
            // Each source belongs to one account/sponsor, so distinct source counts are additive across sponsors.
            // Build totals from this same read to keep them consistent with the breakdown during concurrent imports.
            Metrics(new ReportGroup
            {
                PerformanceRecords = day.Sum(s => s.PerformanceRecords), DeductionRecords = day.Sum(s => s.DeductionRecords),
                Orders = day.Sum(s => s.Orders), Hours = day.Sum(s => s.Hours),
                Collected = day.Sum(s => s.Collected), Deductions = day.Sum(s => s.Deductions)
            }))
        {
            Sponsors = day.Select(s => new NinjaSponsorReport(s.SponsorId,
                s.SponsorId is { } id ? names.GetValueOrDefault(id) : null, Metrics(s))).ToArray()
        }).ToArray(), page, pageSize));
    }

    public async Task<Result<NinjaPage<NinjaRiderReport>>> GetRiderReportAsync(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId,
        int page, int pageSize, CancellationToken ct = default)
    {
        if (!ValidPage(page, pageSize)) return Result.Failure<NinjaPage<NinjaRiderReport>>(NinjaErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var source = await ReportSource(dateFrom, dateTo, accountId, riderId, ct);
        if (source.IsFailure) return Result.Failure<NinjaPage<NinjaRiderReport>>(source.Error);
        var rows = await GroupReport(source.Value!, false, true).OrderByDescending(r => r.Orders).ThenBy(r => r.RiderId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        var riderIds = rows.Select(r => r.RiderId).ToArray();
        var riders = await db.RiderProfiles.AsNoTracking().Where(r => riderIds.Contains(r.Id))
            .Select(r => new { r.Id, r.EmployeeId }).ToDictionaryAsync(r => r.Id, ct);
        var people = await People(riders.Values.Select(r => r.EmployeeId).ToArray(), ct);
        return Result.Success(new NinjaPage<NinjaRiderReport>(rows.Select(r => new NinjaRiderReport(r.RiderId,
            riders.TryGetValue(r.RiderId, out var rider) ? people.GetValueOrDefault(rider.EmployeeId) : null,
            r.Accounts, r.PerformanceDays, r.DeductionDays, Metrics(r))).ToArray(), page, pageSize));
    }

    private async Task<Result<IQueryable<ReportFact>>> ReportSource(DateOnly dateFrom, DateOnly dateTo, Guid? accountId, Guid? riderId, CancellationToken ct)
    {
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<IQueryable<ReportFact>>(NinjaErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (dateFrom == default || dateTo < dateFrom || dateTo > today || dateTo.DayNumber - dateFrom.DayNumber >= 366)
            return Result.Failure<IQueryable<ReportFact>>(NinjaErrors.Invalid("حدد فترة حتى اليوم لا تتجاوز 366 يوماً."));
        // Historical facts remain visible when an account is archived or soft deleted.
        var accounts = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => ids.Contains(a.ClientPlatformId));
        if (accountId is not null && !await accounts.AnyAsync(a => a.Id == accountId, ct))
            return Result.Failure<IQueryable<ReportFact>>(NinjaErrors.NotFound);
        if (riderId is not null && !await db.RiderProfiles.IgnoreQueryFilters().AnyAsync(r => r.Id == riderId, ct))
            return Result.Failure<IQueryable<ReportFact>>(NinjaErrors.NotFound);
        var performance = from p in db.NinjaDailyPerformance.AsNoTracking()
            join a in accounts on p.PlatformRiderAccountId equals a.Id
            where p.ReportDate >= dateFrom && p.ReportDate <= dateTo && (accountId == null || p.PlatformRiderAccountId == accountId)
                && (riderId == null || p.RiderProfileId == riderId)
            select new ReportFact { Date = p.ReportDate, AccountId = p.PlatformRiderAccountId, SponsorId = a.SponsorId, RiderId = p.RiderProfileId, SourceId = p.SourceId == Guid.Empty ? p.Id : p.SourceId,
                PerformanceRecords = 1, DeductionRecords = 0, Orders = (long)p.TotalOrders, Hours = p.TotalWorkingHours, Collected = 0m, Deductions = 0m };
        var deductions = from p in db.NinjaDailyDeductions.AsNoTracking()
            join a in accounts on p.PlatformRiderAccountId equals a.Id
            where p.ReportDate >= dateFrom && p.ReportDate <= dateTo && (accountId == null || p.PlatformRiderAccountId == accountId)
                && (riderId == null || p.RiderProfileId == riderId)
            select new ReportFact { Date = p.ReportDate, AccountId = p.PlatformRiderAccountId, SponsorId = a.SponsorId, RiderId = p.RiderProfileId, SourceId = p.SourceId == Guid.Empty ? p.Id : p.SourceId,
                PerformanceRecords = 0, DeductionRecords = 1, Orders = 0L, Hours = 0m, Collected = p.TotalCollectedAmountAtDelivery, Deductions = p.TotalDeductions };
        // UNION ALL keeps both independent observations, avoiding a many-to-many join that multiplies totals.
        return Result.Success(performance.Concat(deductions));
    }

    // The bucket is always one, and keeps summary grouping tied to SQL columns rather than a constant object.
    private static IQueryable<ReportGroup> GroupReport(IQueryable<ReportFact> facts, bool daily, bool riders, bool sponsors = false)
    {
        // Capture the placeholder so EF sends a typed uniqueidentifier parameter. A GUID
        // literal projected through UNION/GROUP BY can otherwise be returned as SQL text.
        var ungroupedRiderId = Guid.Empty;
        return facts.GroupBy(f => new { Date = daily ? f.Date : DateOnly.MinValue, RiderId = riders ? f.RiderId : ungroupedRiderId,
            SponsorId = sponsors ? f.SponsorId : null,
            Bucket = f.PerformanceRecords + f.DeductionRecords })
            .Select(g => new ReportGroup { Date = g.Key.Date, RiderId = g.Key.RiderId, SponsorId = g.Key.SponsorId,
                PerformanceRecords = g.Where(f => f.PerformanceRecords == 1).Select(f => f.SourceId).Distinct().Count(),
                DeductionRecords = g.Where(f => f.DeductionRecords == 1).Select(f => f.SourceId).Distinct().Count(),
                Orders = g.Sum(f => f.Orders), Hours = g.Sum(f => f.Hours), Collected = g.Sum(f => f.Collected), Deductions = g.Sum(f => f.Deductions),
                PerformanceDays = g.Where(f => f.PerformanceRecords == 1).Select(f => f.Date).Distinct().Count(),
                DeductionDays = g.Where(f => f.DeductionRecords == 1).Select(f => f.Date).Distinct().Count(),
                Accounts = g.Select(f => f.AccountId).Distinct().Count(), Riders = g.Select(f => f.RiderId).Distinct().Count() });
    }

    private static NinjaReportMetrics Metrics(ReportGroup r) => new(r.PerformanceRecords, r.DeductionRecords, r.Orders,
        r.Hours, r.Hours == 0 ? null : decimal.Round(r.Orders / r.Hours, 4), r.Collected, r.Deductions);

    private sealed class ReportFact
    {
        public Guid? SponsorId { get; init; }
        public Guid SourceId { get; init; }
        public DateOnly Date { get; init; }
        public Guid AccountId { get; init; }
        public Guid RiderId { get; init; }
        public int PerformanceRecords { get; init; }
        public int DeductionRecords { get; init; }
        public long Orders { get; init; }
        public decimal Hours { get; init; }
        public decimal Collected { get; init; }
        public decimal Deductions { get; init; }
    }

    private sealed class ReportGroup
    {
        public Guid? SponsorId { get; init; }
        public DateOnly Date { get; init; }
        public Guid RiderId { get; init; }
        public int PerformanceRecords { get; init; }
        public int DeductionRecords { get; init; }
        public long Orders { get; init; }
        public decimal Hours { get; init; }
        public decimal Collected { get; init; }
        public decimal Deductions { get; init; }
        public int PerformanceDays { get; init; }
        public int DeductionDays { get; init; }
        public int Accounts { get; init; }
        public int Riders { get; init; }
    }
}
