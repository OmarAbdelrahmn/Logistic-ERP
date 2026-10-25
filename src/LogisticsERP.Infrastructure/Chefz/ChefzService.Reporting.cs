using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    private static ChefzSettingsResponse SettingsResponse(ChefzPerformanceSettings s) => new(new(
        s.OverallAOrders, s.OverallBOrders, s.OverallCOrders, s.DailyAOrders, s.DailyBOrders, s.DailyCOrders,
        s.AAcceptanceRate, s.BAcceptanceRate, s.CAcceptanceRate, s.ChangeAfterNoOrderDays,
        s.FollowUpOnZeroOrdersWithMissedOffers, Convert.ToBase64String(s.RowVersion)), Convert.ToBase64String(s.RowVersion));

    public async Task<Result<ChefzSettingsResponse>> GetSettingsAsync(CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzSettingsResponse>(ChefzErrors.Forbidden);
        var s = await db.ChefzPerformanceSettings.AsNoTracking().SingleAsync(ct);
        return Result.Success(SettingsResponse(s));
    }

    public async Task<Result<ChefzSettingsResponse>> SaveSettingsAsync(ChefzSettingsRequest r, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct)) return Result.Failure<ChefzSettingsResponse>(ChefzErrors.Forbidden);
        if (!(r.OverallAOrders > r.OverallBOrders && r.OverallBOrders > r.OverallCOrders && r.OverallCOrders > 0
            && r.DailyAOrders > r.DailyBOrders && r.DailyBOrders > r.DailyCOrders && r.DailyCOrders > 0
            && r.AAcceptanceRate is >= 0 and <= 100 && r.BAcceptanceRate >= r.CAcceptanceRate
            && r.AAcceptanceRate >= r.BAcceptanceRate && r.CAcceptanceRate >= 0
            && r.ChangeAfterNoOrderDays is >= 1 and <= 365
            && decimal.Round(r.AAcceptanceRate, 2) == r.AAcceptanceRate
            && decimal.Round(r.BAcceptanceRate, 2) == r.BAcceptanceRate
            && decimal.Round(r.CAcceptanceRate, 2) == r.CAcceptanceRate))
            return Result.Failure<ChefzSettingsResponse>(ChefzErrors.Invalid("تحقق من ترتيب حدود الطلبات ونسب القبول وعدد أيام عدم وجود طلبات."));
        var s = await db.ChefzPerformanceSettings.SingleAsync(ct);
        if (!HrServiceSupport.MatchesRowVersion(s.RowVersion, r.RowVersion)) return Result.Failure<ChefzSettingsResponse>(ChefzErrors.Conflict("أعد تحميل الإعدادات."));
        s.OverallAOrders = r.OverallAOrders; s.OverallBOrders = r.OverallBOrders; s.OverallCOrders = r.OverallCOrders;
        s.DailyAOrders = r.DailyAOrders; s.DailyBOrders = r.DailyBOrders; s.DailyCOrders = r.DailyCOrders;
        s.AAcceptanceRate = r.AAcceptanceRate; s.BAcceptanceRate = r.BAcceptanceRate; s.CAcceptanceRate = r.CAcceptanceRate;
        s.ChangeAfterNoOrderDays = r.ChangeAfterNoOrderDays;
        s.FollowUpOnZeroOrdersWithMissedOffers = r.FollowUpOnZeroOrdersWithMissedOffers;
        var result = await Persist(true, ct);
        return result.IsSuccess ? Result.Success(SettingsResponse(s)) : Result.Failure<ChefzSettingsResponse>(result.Error);
    }

    private sealed record ReportMetricsDay(DateOnly ReportDate, long Orders, long ValidOffers);

    private async Task<Dictionary<Guid, string>> ReportAccountTypes(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var accountIds = ids.Distinct().ToArray();
        if (accountIds.Length == 0) return [];
        var details = await db.ChefzAccountDetails.AsNoTracking()
            .Where(d => accountIds.Contains(d.PlatformRiderAccountId))
            .Select(d => new { d.PlatformRiderAccountId, d.AccountType }).ToArrayAsync(ct);
        return details.ToDictionary(d => d.PlatformRiderAccountId, d => d.AccountType.ToString());
    }

    private static ChefzReportSummary Summarize(ChefzDailyPerformance[] rows, ChefzPerformanceSettings s) =>
        Summarize(rows.Select(p => new ReportMetricsDay(p.ReportDate, p.AcceptedOffers, p.TotalValidOffers)).ToArray(), s);

    private static ChefzReportSummary Summarize(ReportMetricsDay[] rows, ChefzPerformanceSettings s)
    {
        var halves = rows.Select(p => (p.ReportDate.Year, p.ReportDate.Month, Half: p.ReportDate.Day <= 15 ? 1 : 2)).Distinct().Count();
        var days = rows.Select(p => p.ReportDate).Distinct().Count();
        var orders = rows.Sum(p => p.Orders);
        var rate = ChefzPerformanceRules.Acceptance(orders, rows.Sum(p => p.ValidOffers));
        var average = halves == 0 ? (decimal?)null : orders / (decimal)halves;
        var rating = average is null ? null : ChefzPerformanceRules.Classify(average.Value, rate, false, s);
        var daily = rows.GroupBy(p => p.ReportDate).Select(g => (Date: g.Key, Orders: g.Sum(p => p.Orders)));
        if (ChefzPerformanceRules.NeedsChange(daily, s.ChangeAfterNoOrderDays)) rating = "CHANGE";
        return new(halves, days, orders, average is null ? null : decimal.Round(average.Value, 2), rate, rating);
    }

    public async Task<Result<ChefzRiderReport>> GetRiderReportAsync(Guid? accountId, Guid? riderId,
        int year, int month, int half, int halves, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzRiderReport>(ChefzErrors.Forbidden);
        if ((accountId is null && riderId is null) || halves is < 1 or > 120 || year is < 1 or > 9999 || month is < 1 or > 12 || half is not (1 or 2))
            return Result.Failure<ChefzRiderReport>(ChefzErrors.Invalid("اختر الحساب أو المندوب ونهاية الفترة وعدد أنصاف الشهور من 1 إلى 120."));
        var index = (year - 1) * 24 + (month - 1) * 2 + half - 1;
        if (index - halves + 1 < 0) return Result.Failure<ChefzRiderReport>(ChefzErrors.Invalid("الفترة المطلوبة تسبق بداية التقويم."));
        if (accountId is not null && !await Accounts.AnyAsync(a => a.Id == accountId, ct))
            return Result.Failure<ChefzRiderReport>(ChefzErrors.NotFound);
        if (riderId is not null && !await db.RiderProfiles.AnyAsync(r => r.Id == riderId, ct))
            return Result.Failure<ChefzRiderReport>(ChefzErrors.NotFound);
        var s = await db.ChefzPerformanceSettings.AsNoTracking().SingleAsync(ct);
        var query = db.ChefzDailyPerformance.AsNoTracking().Where(p => Accounts.Any(a => a.Id == p.PlatformRiderAccountId)
            && (accountId == null || p.PlatformRiderAccountId == accountId) && (riderId == null || p.RiderProfileId == riderId));
        var periods = Enumerable.Range(index - halves + 1, halves).Select(i => ChefzRules.Period(i / 24 + 1, i % 24 / 2 + 1, i % 2 + 1)).ToArray();
        var from = periods[0].From; var to = periods[^1].To;
        var selected = await query.Where(p => p.ReportDate >= from && p.ReportDate <= to).ToArrayAsync(ct);
        var messages = new List<string>();
        var reports = periods.Select(period =>
        {
            var rows = selected.Where(p => p.ReportDate >= period.From && p.ReportDate <= period.To).ToArray();
            var summary = Summarize(rows, s);
            var message = rows.Length == 0 ? $"لا توجد بيانات للفترة {period.From:yyyy-MM-dd} إلى {period.To:yyyy-MM-dd}." : null;
            if (message is not null) messages.Add(message);
            return new ChefzHalfReport(period.From, period.To, summary.DaysWithData, summary.Orders, summary.AcceptanceRate, summary.Rating, message);
        }).ToArray();
        var daily = selected.GroupBy(p => p.ReportDate).OrderBy(g => g.Key).Select(g =>
        {
            var orders = g.Sum(p => (long)p.AcceptedOffers); var valid = g.Sum(p => (long)p.TotalValidOffers);
            var missed = g.Sum(p => (long)p.MissedOffers); var rate = ChefzPerformanceRules.Acceptance(orders, valid);
            var rating = s.FollowUpOnZeroOrdersWithMissedOffers && orders == 0 && missed > 0 ? "متابعه شديدة"
                : ChefzPerformanceRules.Classify(orders, rate, true, s);
            return new ChefzDailyRating(g.Key, orders, valid, missed, rate, rating);
        }).ToArray();
        // Aggregate the all-time history in SQL; do not load an unbounded daily history into memory.
        var allTime = await query.GroupBy(p => new { p.ReportDate.Year, p.ReportDate.Month, Half = p.ReportDate.Day <= 15 ? 1 : 2 })
            .Select(g => new { Orders = g.Sum(p => (long)p.AcceptedOffers), Valid = g.Sum(p => (long)p.TotalValidOffers), Days = g.Select(p => p.ReportDate).Distinct().Count() }).ToArrayAsync(ct);
        var allOrders = allTime.Sum(p => p.Orders); var allRate = ChefzPerformanceRules.Acceptance(allOrders, allTime.Sum(p => p.Valid));
        var allAverage = allTime.Length == 0 ? (decimal?)null : allOrders / (decimal)allTime.Length;
        var allRating = allAverage is null ? null : ChefzPerformanceRules.Classify(allAverage.Value, allRate, false, s);
        var latest = await query.Select(p => (DateOnly?)p.ReportDate).MaxAsync(ct);
        if (latest is { } last)
        {
            var recentFrom = DateOnly.FromDayNumber(Math.Max(0, last.DayNumber - s.ChangeAfterNoOrderDays));
            var recent = await query.Where(p => p.ReportDate >= recentFrom && p.ReportDate <= last).GroupBy(p => p.ReportDate)
                .Select(g => new { Date = g.Key, Orders = g.Sum(p => (long)p.AcceptedOffers) }).ToArrayAsync(ct);
            if (ChefzPerformanceRules.NeedsChange(recent.Select(p => (p.Date, p.Orders)), s.ChangeAfterNoOrderDays)) allRating = "CHANGE";
        }
        var allSummary = new ChefzReportSummary(allTime.Length, allTime.Sum(p => p.Days), allOrders,
            allAverage is null ? null : decimal.Round(allAverage.Value, 2), allRate,
            allRating);
        var accountRows = await PerformanceResponses(selected, ct);
        var accountTypes = await ReportAccountTypes(accountRows.Select(p => p.AccountId), ct);
        var byAccountRider = accountRows.ToLookup(p => (p.AccountId, p.RiderProfileId));
        var accounts = selected.GroupBy(p => new { p.PlatformRiderAccountId, p.RiderProfileId }).Select(g =>
        {
            var rows = byAccountRider[(g.Key.PlatformRiderAccountId, g.Key.RiderProfileId)]
                .OrderBy(p => p.ReportDate).ThenBy(p => p.Id).ToArray();
            return new ChefzAccountPerformanceReport(g.Key.PlatformRiderAccountId, rows[0].ExternalAccountId,
                g.Key.RiderProfileId, rows[0].RiderName, Summarize(g.ToArray(), s), rows)
                { RiderIqamaNo = rows[0].RiderIqamaNo,
                    AccountType = accountTypes.GetValueOrDefault(g.Key.PlatformRiderAccountId, "Unconfigured") };
        }).OrderBy(a => a.RiderName, StringComparer.Ordinal).ThenBy(a => a.ExternalAccountId, StringComparer.Ordinal)
            .ThenBy(a => a.RiderProfileId).ThenBy(a => a.AccountId).ToArray();
        // Account-only reports can span several workers; only label the overall report for an explicit rider filter.
        var identity = riderId is { } id ? (await RiderIdentities([id], ct)).GetValueOrDefault(id) : null;
        return Result.Success(new ChefzRiderReport(accountId, riderId, halves, reports, Summarize(selected, s), allSummary, daily, messages)
            { Accounts = accounts, RiderName = identity?.RiderName, RiderIqamaNo = identity?.RiderIqamaNo });
    }

    public async Task<Result<ChefzRidersPerformanceReport>> GetAllRidersReportAsync(
        int year, int month, int half, int halves, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct))
            return Result.Failure<ChefzRidersPerformanceReport>(ChefzErrors.Forbidden);
        if (halves is < 1 or > 120 || year is < 1 or > 9999 || month is < 1 or > 12 || half is not (1 or 2))
            return Result.Failure<ChefzRidersPerformanceReport>(ChefzErrors.Invalid("اختر نهاية الفترة وعدد أنصاف الشهور من 1 إلى 120."));
        var index = (year - 1) * 24 + (month - 1) * 2 + half - 1;
        if (index - halves + 1 < 0)
            return Result.Failure<ChefzRidersPerformanceReport>(ChefzErrors.Invalid("الفترة المطلوبة تسبق بداية التقويم."));
        var periods = Enumerable.Range(index - halves + 1, halves)
            .Select(i => ChefzRules.Period(i / 24 + 1, i % 24 / 2 + 1, i % 2 + 1)).ToArray();
        var from = periods[0].From; var to = periods[^1].To;
        var settings = await db.ChefzPerformanceSettings.AsNoTracking().SingleAsync(ct);
        var performance = db.ChefzDailyPerformance.AsNoTracking().Where(p => p.ReportDate >= from && p.ReportDate <= to
            && Accounts.Any(a => a.Id == p.PlatformRiderAccountId));
        // Include historical assignees and riders without uploads, as well as recorded performance.
        var roster = db.RiderClientAssignments.AsNoTracking().Where(a => Accounts.Any(account => account.Id == a.PlatformRiderAccountId)
                && a.EffectiveFrom <= to && (a.EffectiveTo == null || a.EffectiveTo >= from)
                && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended))
            .Select(a => new { AccountId = a.PlatformRiderAccountId, a.RiderProfileId })
            .Union(performance.Select(p => new { AccountId = p.PlatformRiderAccountId, p.RiderProfileId }));
        var members = await (from member in roster
            join account in Accounts.AsNoTracking() on member.AccountId equals account.Id
            join rider in db.RiderProfiles.AsNoTracking() on member.RiderProfileId equals rider.Id
            join employee in db.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
            select new { member.AccountId, member.RiderProfileId, account.ExternalAccountId, RiderName = employee.FullNameAr,
                RiderIqamaNo = employee.IqamaNo })
            .ToArrayAsync(ct);
        // Fetch only metrics in the bounded period, with a fixed number of queries for every rider.
        var metrics = await performance.Select(p => new { AccountId = p.PlatformRiderAccountId, p.RiderProfileId,
            p.ReportDate, Orders = (long)p.AcceptedOffers, ValidOffers = (long)p.TotalValidOffers }).ToArrayAsync(ct);
        var accountTypes = await ReportAccountTypes(members.Select(p => p.AccountId), ct);
        var byAccount = metrics.ToLookup(p => (p.AccountId, p.RiderProfileId));
        var byRider = metrics.ToLookup(p => p.RiderProfileId);
        var riders = members.GroupBy(p => p.RiderProfileId).Select(group =>
        {
            var days = byRider[group.Key].Select(p => new ReportMetricsDay(p.ReportDate, p.Orders, p.ValidOffers)).ToArray();
            var accounts = group.OrderBy(p => p.ExternalAccountId, StringComparer.Ordinal).ThenBy(p => p.AccountId)
                .Select(p => new ChefzAccountPerformanceSummary(p.AccountId, p.ExternalAccountId,
                    Summarize(byAccount[(p.AccountId, p.RiderProfileId)]
                        .Select(d => new ReportMetricsDay(d.ReportDate, d.Orders, d.ValidOffers)).ToArray(), settings))
                    { AccountType = accountTypes.GetValueOrDefault(p.AccountId, "Unconfigured") }).ToArray();
            var halfReports = periods.Select(period =>
            {
                var summary = Summarize(days.Where(p => p.ReportDate >= period.From && p.ReportDate <= period.To).ToArray(), settings);
                var message = summary.DaysWithData == 0 ? $"لا توجد بيانات للفترة {period.From:yyyy-MM-dd} إلى {period.To:yyyy-MM-dd}." : null;
                return new ChefzHalfReport(period.From, period.To, summary.DaysWithData, summary.Orders,
                    summary.AcceptanceRate, summary.Rating, message);
            }).ToArray();
            return new ChefzRiderPerformanceSummary(group.Key, group.First().RiderName,
                Summarize(days, settings), halfReports, accounts) { RiderIqamaNo = group.First().RiderIqamaNo };
        }).OrderBy(p => p.RiderName, StringComparer.Ordinal).ThenBy(p => p.RiderProfileId).ToArray();
        var counts = new ChefzRatingCounts(riders.Count(p => p.SelectedSummary.Rating == "A"),
            riders.Count(p => p.SelectedSummary.Rating == "B"), riders.Count(p => p.SelectedSummary.Rating == "C"),
            riders.Count(p => p.SelectedSummary.Rating == "D"), riders.Count(p => p.SelectedSummary.Rating == "CHANGE"),
            riders.Count(p => p.SelectedSummary.Rating == null));
        return Result.Success(new ChefzRidersPerformanceReport(from, to, halves, riders.Length, counts, riders));
    }

    public async Task<Result<IReadOnlyList<ChefzManagementReport>>> GetManagementReportAsync(DateOnly reportDate, Guid? sponsorId, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<IReadOnlyList<ChefzManagementReport>>(ChefzErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        if (reportDate == default || reportDate > today || reportDate == DateOnly.MinValue)
            return Result.Failure<IReadOnlyList<ChefzManagementReport>>(ChefzErrors.Invalid("اختر تاريخ تقرير صحيحاً حتى اليوم."));
        var yesterday = reportDate.AddDays(-1);
        var cycle = ChefzRules.Period(yesterday.Year, yesterday.Month, yesterday.Day <= 15 ? 1 : 2);
        var sponsors = await db.Sponsors.AsNoTracking().Where(s => sponsorId == null || s.Id == sponsorId).OrderBy(s => s.RegistryNameAr).ToArrayAsync(ct);
        if (sponsorId is not null && sponsors.Length == 0) return Result.Failure<IReadOnlyList<ChefzManagementReport>>(ChefzErrors.NotFound);
        var accounts = await Accounts.AsNoTracking().Select(a => new { a.Id, a.SponsorId }).ToArrayAsync(ct);
        var performance = await db.ChefzDailyPerformance.AsNoTracking().Where(p => p.ReportDate >= cycle.From && p.ReportDate <= yesterday
            && Accounts.Any(a => a.Id == p.PlatformRiderAccountId)).ToArrayAsync(ct);
        IReadOnlyList<ChefzManagementReport> reports = sponsors.Select(s =>
        {
            var sponsorAccounts = accounts.Where(a => a.SponsorId == s.Id).Select(a => a.Id).ToHashSet();
            var data = performance.Where(p => sponsorAccounts.Contains(p.PlatformRiderAccountId)).ToArray();
            var cycleTotals = data.GroupBy(p => p.PlatformRiderAccountId).ToDictionary(g => g.Key, g => g.Sum(p => (long)p.AcceptedOffers));
            var dailyTotals = data.Where(p => p.ReportDate == yesterday).GroupBy(p => p.RiderProfileId)
                .ToDictionary(g => g.Key, g => g.Sum(p => (long)p.AcceptedOffers));
            // Both daily buckets partition riders who actually completed orders, after combining their accounts.
            return new ChefzManagementReport(s.Id, s.RegistryNameAr, "ذا شفز", reportDate, yesterday, cycle.From,
                data.Sum(p => (long)p.AcceptedOffers), sponsorAccounts.Count(id => cycleTotals.GetValueOrDefault(id) < 3),
                dailyTotals.Count(p => p.Value > 0), dailyTotals.Count(p => p.Value is > 0 and <= 3), dailyTotals.Count(p => p.Value > 3));
        }).ToArray();
        return Result.Success(reports);
    }

    public async Task<Result<ChefzPage<ChefzAccountReminder>>> GetRemindersAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.Read, ct)) return Result.Failure<ChefzPage<ChefzAccountReminder>>(ChefzErrors.Forbidden);
        if (!ValidPage(page, pageSize)) return Result.Failure<ChefzPage<ChefzAccountReminder>>(ChefzErrors.Invalid("حجم الصفحة من 1 إلى 100."));
        var rows = await (from a in Accounts.AsNoTracking() join e in db.Employees.AsNoTracking() on a.RegisteredEmployeeId equals e.Id
            where e.Status == EmployeeStatus.Terminated || e.EngagementType == EmployeeRelationshipType.OutsideRider || e.SponsorId == null
            orderby a.ExternalAccountId, a.Id select new { AccountId = a.Id, Employee = e })
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return Result.Success(new ChefzPage<ChefzAccountReminder>(rows.Select(r => new ChefzAccountReminder(r.AccountId,
            r.Employee.Id, r.Employee.FullNameAr, r.Employee.Status.ToString(),
            r.Employee.Status == EmployeeStatus.Terminated ? "FinalExit" : "OutsideSponsorship",
            r.Employee.Status == EmployeeStatus.Terminated ? "صاحب الحساب خروج نهائي؛ الحساب معرض للبلوك." : "صاحب الحساب خارج الكفالة؛ الحساب اقترب من البلوك.")).ToArray(), page, pageSize));
    }
}
