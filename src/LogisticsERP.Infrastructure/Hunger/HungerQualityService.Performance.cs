using System.Globalization;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService
{
    public async Task<Result<HungerFile>> ExportPerformanceAsync(HungerPerformanceQuery query, CancellationToken ct = default)
    {
        var result = await ReadPerformanceAsync(query with { Page = 1, PageSize = int.MaxValue }, true, ct);
        if (result.IsFailure) return Result.Failure<HungerFile>(result.Error);
        using var book = new ClosedXML.Excel.XLWorkbook();
        var sheet = book.AddWorksheet("Performance");
        string[] headers = ["EntityId", "Period", "CompletedOrders", "WorkingHours", "WorkedDays", "QualifiedDays", "UnknownDays", "ScheduledDays", "ElapsedDays", "RemainingDays", "AcceptedOrders", "StackedOrders", "Sponsorship", "ActualVehicle", "Target", "Deficit", "TargetAchieved", "Forecast", "RequiredPace", "Alerts", "Catalog", "ActualVehicleType", "ForecastObservedOrders", "CoverageThrough", "RejectedOrders", "CancelledOrders", "NoShowCount"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var catalogSheet = book.AddWorksheet("Catalog");
        string[] catalogHeaders = ["EntityId", "ReportPeriod", "RuleId", "Code", "Kind", "Metric", "EvaluationDate", "Actual", "Threshold", "Matches", "Severity", "Evidence", "Deficit"];
        for (var i = 0; i < catalogHeaders.Length; i++) catalogSheet.Cell(1, i + 1).Value = catalogHeaders[i];
        var alertsSheet = book.AddWorksheet("Alerts"); alertsSheet.Cell(1, 1).Value = "EntityId"; alertsSheet.Cell(1, 2).Value = "ReportPeriod"; alertsSheet.Cell(1, 3).Value = "Alert";
        var catalogRow = 2; var alertRow = 2;
        var n = 2;
        foreach (var row in result.Value!.Items)
        {
            sheet.Cell(n, 1).Value = row.EntityId.ToString(); sheet.Cell(n, 2).Value = row.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sheet.Cell(n, 3).Value = row.CompletedOrders; sheet.Cell(n, 4).Value = row.WorkingHours;
            sheet.Cell(n, 5).Value = row.WorkedDays; sheet.Cell(n, 6).Value = row.QualifiedDays; sheet.Cell(n, 7).Value = row.UnknownDays;
            sheet.Cell(n, 8).Value = row.ScheduledDays; sheet.Cell(n, 9).Value = row.ElapsedDays; sheet.Cell(n, 10).Value = row.RemainingDays;
            if (row.AcceptedOrders.HasValue) sheet.Cell(n, 11).Value = row.AcceptedOrders.Value;
            if (row.StackedOrders.HasValue) sheet.Cell(n, 12).Value = row.StackedOrders.Value;
            sheet.Cell(n, 13).Value = row.Sponsorship; sheet.Cell(n, 14).Value = row.ActualVehicle;
            if (row.Target.HasValue) sheet.Cell(n, 15).Value = row.Target.Value;
            if (row.Deficit.HasValue) sheet.Cell(n, 16).Value = row.Deficit.Value;
            if (row.TargetAchieved.HasValue) sheet.Cell(n, 17).Value = row.TargetAchieved.Value;
            if (row.Forecast.HasValue) sheet.Cell(n, 18).Value = row.Forecast.Value;
            if (row.RequiredPace.HasValue) sheet.Cell(n, 19).Value = row.RequiredPace.Value;
            foreach (var evaluation in row.Catalog)
            {
                catalogSheet.Cell(catalogRow, 1).Value = row.EntityId.ToString(); catalogSheet.Cell(catalogRow, 2).Value = row.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                catalogSheet.Cell(catalogRow, 3).Value = evaluation.RuleId.ToString(); catalogSheet.Cell(catalogRow, 4).Value = evaluation.Code;
                catalogSheet.Cell(catalogRow, 5).Value = evaluation.Kind; catalogSheet.Cell(catalogRow, 6).Value = evaluation.Metric;
                catalogSheet.Cell(catalogRow, 7).Value = evaluation.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (evaluation.Actual.HasValue) catalogSheet.Cell(catalogRow, 8).Value = evaluation.Actual.Value;
                catalogSheet.Cell(catalogRow, 9).Value = evaluation.Threshold; catalogSheet.Cell(catalogRow, 10).Value = evaluation.Matches;
                catalogSheet.Cell(catalogRow, 11).Value = evaluation.Severity; catalogSheet.Cell(catalogRow, 12).Value = evaluation.Evidence;
                if (evaluation.Deficit.HasValue) catalogSheet.Cell(catalogRow, 13).Value = evaluation.Deficit.Value;
                catalogRow++;
            }
            foreach (var alert in row.Alerts)
            {
                alertsSheet.Cell(alertRow, 1).Value = row.EntityId.ToString(); alertsSheet.Cell(alertRow, 2).Value = row.Period.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                alertsSheet.Cell(alertRow, 3).Value = alert; alertRow++;
            }
            sheet.Cell(n, 20).Value = row.Alerts.Count; sheet.Cell(n, 21).Value = row.Catalog.Count; sheet.Cell(n, 22).Value = row.ActualVehicleType; sheet.Cell(n, 23).Value = row.ForecastObservedOrders; sheet.Cell(n, 24).Value = row.CoverageThrough.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (row.RejectedOrders.HasValue) sheet.Cell(n, 25).Value = row.RejectedOrders.Value;
            if (row.CancelledOrders.HasValue) sheet.Cell(n, 26).Value = row.CancelledOrders.Value;
            if (row.NoShowCount.HasValue) sheet.Cell(n, 27).Value = row.NoShowCount.Value;
            n++;
        }
        var metadata = book.AddWorksheet("Scope"); metadata.Cell(1, 1).Value = "Query"; metadata.Cell(2, 1).Value = Serialize(query);
        metadata.Cell(3, 1).Value = "Coverage cutoff (Riyadh completed day)"; metadata.Cell(4, 1).Value = BusinessToday.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return Result.Success(ExportBook(book, "Hunger-Performance.xlsx"));
    }
    private sealed record PerformanceFact(Guid Account, Guid Entity, DateOnly Day, Guid Platform, Guid City,
        Guid? Sponsor, string Vehicle, decimal? Orders, decimal? Hours, int? Accepted, int? Stacked,
        int? Rejected, int? Cancelled, int? NoShow)
    {
        public bool Scheduled { get; init; }
        public decimal? Responsibility { get; init; }
        public string VehicleType { get; init; } = "Unknown";
    }

    public Task<Result<HungerPerformanceReport>> PerformanceAsync(HungerPerformanceQuery query, CancellationToken ct = default)
        => ReadPerformanceAsync(query, false, ct);

    private async Task<Result<HungerPerformanceReport>> ReadPerformanceAsync(HungerPerformanceQuery query, bool export, CancellationToken ct)
    {
        var platforms = await Platforms(export ? PermissionKeys.Hunger.PerformanceExport : PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0 || export && !await FieldAccess(platforms, [PermissionKeys.Hunger.Read], ct)) return Result.Failure<HungerPerformanceReport>(HungerErrors.Forbidden);
        if (query.From == default || query.Through < query.From || query.Through == DateOnly.MaxValue || query.Through.DayNumber - query.From.DayNumber > 366
            || query.GroupBy is not ("account" or "rider") || query.Period is not ("day" or "month") || query.Layout is not ("horizontal" or "vertical")
            || query.Page < 1 || (query.PageSize < 1 || !export && query.PageSize > 100))
            return Result.Failure<HungerPerformanceReport>(HungerErrors.Invalid("Invalid performance period, grouping or paging."));
        var scheduleThrough = query.Period == "month" ? new DateOnly(query.Through.Year, query.Through.Month, DateTime.DaysInMonth(query.Through.Year, query.Through.Month)) : query.Through;
        if (scheduleThrough == DateOnly.MaxValue) return Result.Failure<HungerPerformanceReport>(HungerErrors.Invalid("Unsupported month boundary."));
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId)
            && (query.AccountId == null || a.Id == query.AccountId) && (query.CityId == null || a.OperatingCityId == query.CityId)).ToArrayAsync(ct);
        if (query.Accounts is not null)
        {
            var filtered = await ExportAccountIds(query.Accounts, export ? PermissionKeys.Hunger.PerformanceExport : PermissionKeys.Hunger.Read, ct);
            if (filtered.IsFailure) return Result.Failure<HungerPerformanceReport>(filtered.Error);
            accounts = accounts.Where(a => filtered.Value!.Contains(a.Id)).ToArray();
        }
        var ids = accounts.Select(a => a.Id).ToArray();
        var operations = await db.Set<HungerDailyOperation>().AsNoTracking().Where(o => ids.Contains(o.PlatformRiderAccountId)
            && o.ReportDate >= query.From && o.ReportDate <= query.Through).ToArrayAsync(ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId)
            && a.Status != RiderAssignmentStatus.Planned && a.Status != RiderAssignmentStatus.Cancelled
            && a.EffectiveFrom <= scheduleThrough && (a.EffectiveTo == null || a.EffectiveTo >= query.From)).ToArrayAsync(ct);
        var riders = assignments.Select(a => a.RiderProfileId).Concat(operations.SelectMany(o => Read<PlatformDayShare[]>(o.AllocationsJson)).Select(a => a.RiderProfileId)).Distinct().ToArray();
        var sponsorship = await (from r in db.RiderProfiles.AsNoTracking() join e in db.Employees.AsNoTracking() on r.EmployeeId equals e.Id
            where riders.Contains(r.Id) select new { r.Id, e.SponsorId }).ToDictionaryAsync(r => r.Id, r => r.SponsorId, ct);
        var contexts = await PlatformAccountBusinessService.ResolveContextsAsync(db, clock, configuration, accounts, query.From, scheduleThrough, ct);
        var rules = await db.Set<HungerAlertRule>().AsNoTracking().Where(r => platforms.Contains(r.ClientPlatformId) && r.EffectiveFrom <= scheduleThrough
            && (r.EffectiveThrough == null || r.EffectiveThrough >= query.From)).ToArrayAsync(ct);
        static string Dimension(IEnumerable<string> values)
        { var unique = values.Distinct().ToArray(); return unique.Length == 1 ? unique[0] : unique.Length == 0 ? "Unknown" : "Mixed"; }
        string Vehicle(Guid account, Guid? rider, DateOnly day, bool type = false)
        {
            var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
            return Dimension(contexts[account].ActualVehiclePeriods.Where(p => (!rider.HasValue || p.RiderProfileId == rider)
                && p.FromUtc < start.AddDays(1) && p.ThroughUtc > start).Select(p => type ? p.VehicleType ?? "Unknown" : p.Ownership));
        }
        var facts = new List<PerformanceFact>();
        foreach (var account in accounts)
        {
            var source = operations.Where(o => o.PlatformRiderAccountId == account.Id).ToDictionary(o => o.ReportDate);
            var schedule = assignments.Where(a => a.PlatformRiderAccountId == account.Id).ToArray();
            for (var day = query.From; day <= scheduleThrough; day = day.AddDays(1))
            {
                var scheduled = schedule.Where(a => a.EffectiveFrom <= day && (a.EffectiveTo == null || a.EffectiveTo >= day)).ToArray();
                source.TryGetValue(day, out var operation);
                var shares = operation == null ? [] : Read<PlatformDayShare[]>(operation.AllocationsJson);
                if (query.GroupBy == "account")
                {
                    if (query.RiderId.HasValue && !scheduled.Any(a => a.RiderProfileId == query.RiderId) && !shares.Any(s => s.RiderProfileId == query.RiderId)) continue;
                    if (scheduled.Length == 0 && operation == null) continue;
                    facts.Add(new(account.Id, account.Id, day, account.ClientPlatformId, account.OperatingCityId, account.SponsorId,
                        Vehicle(account.Id, null, day), operation?.CompletedOrders, operation?.ActualWorkingHours, operation?.AcceptedOrders,
                        operation?.StackedOrders, operation?.RejectedOrders, operation?.CancelledOrders, operation?.NoShowCount) { VehicleType = Vehicle(account.Id, null, day, true), Scheduled = scheduled.Length > 0, Responsibility = scheduled.Length > 0 ? 1 : null });
                }
                else
                {
                    foreach (var rider in scheduled.Select(a => a.RiderProfileId).Concat(shares.Select(s => s.RiderProfileId)).Distinct())
                    {
                        if (query.RiderId.HasValue && rider != query.RiderId) continue;
                        var own = shares.Where(s => s.RiderProfileId == rider).ToArray();
                        decimal? Metric(PlatformDayMetric metric) => own.Length > 0 && own.All(s => s.Values.ContainsKey(metric)) ? own.Sum(s => s.Values[metric]) : null;
                        // Optional counts have no allocation contract. Attribute only an unambiguous sole rider source.
                        var sole = shares.Select(s => s.RiderProfileId).Distinct().Count() == 1;
                        facts.Add(new(account.Id, rider, day, account.ClientPlatformId, account.OperatingCityId, sponsorship.GetValueOrDefault(rider), Vehicle(account.Id, rider, day),
                            Metric(PlatformDayMetric.OrdersCount), Metric(PlatformDayMetric.WorkingHours), sole ? operation?.AcceptedOrders : null,
                            sole ? operation?.StackedOrders : null, sole ? operation?.RejectedOrders : null, sole ? operation?.CancelledOrders : null, sole ? operation?.NoShowCount : null)
                            { VehicleType = Vehicle(account.Id, rider, day, true), Scheduled = scheduled.Any(a => a.RiderProfileId == rider), Responsibility = scheduled.Select(a => a.RiderProfileId).Distinct().Count() == 1 ? 1 : null });
                    }
                }
            }
        }
        if (query.SponsorId.HasValue) facts = facts.Where(f => f.Sponsor == query.SponsorId).ToList();
        var cutoff = query.Through < BusinessToday ? query.Through : BusinessToday.AddDays(-1);
        var output = new List<HungerPerformanceRow>();
        foreach (var group in facts.GroupBy(f => (f.Entity, Period: query.Period == "day" ? f.Day : new DateOnly(f.Day.Year, f.Day.Month, 1))))
        {
            var days = group.GroupBy(f => f.Day).Select(g => new { Day = g.Key, Scheduled = g.Any(f => f.Scheduled), Orders = g.All(f => f.Orders.HasValue) ? g.Sum(f => f.Orders) : null,
                Hours = g.Any(f => f.Hours.HasValue) ? g.Sum(f => f.Hours) : null, Complete = g.All(f => f.Hours.HasValue && f.Orders.HasValue) }).ToArray();
            var elapsed = days.Where(d => d.Scheduled && d.Day <= cutoff).ToArray();
            var orders = group.Sum(f => f.Orders ?? 0); var hours = group.Sum(f => f.Hours ?? 0);
            var worked = days.Count(d => d.Hours > 0); var qualified = days.Count(d => d.Hours >= 8);
            var unknown = elapsed.Count(d => !d.Hours.HasValue);
            var remaining = days.Count(d => d.Scheduled && d.Day > cutoff);
            var alerts = new List<string>();
            var evaluations = new List<HungerCatalogEvaluation>();
            foreach (var day in days.Where(d => d.Day <= cutoff))
            {
                var scopes = group.Where(f => f.Day == day.Day).Select(f => (f.Platform, f.City)).Distinct().ToArray();
                HungerAlertRule? Override(string code) => scopes.Length == 1 ? rules.Where(r => r.Code == code && r.Kind == "Alert" && r.Period == "Day"
                    && r.ClientPlatformId == scopes[0].Platform && (r.OperatingCityId == null || r.OperatingCityId == scopes[0].City)
                    && r.EffectiveFrom <= day.Day && (r.EffectiveThrough == null || r.EffectiveThrough >= day.Day))
                    .OrderByDescending(r => r.OperatingCityId.HasValue).ThenByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).FirstOrDefault() : null;
                bool HoursAlert(string code, decimal threshold, string severity)
                {
                    var rule = Override(code);
                    if (rule is { Enabled: false }) return false;
                    threshold = rule?.Threshold ?? threshold; severity = rule?.Severity ?? severity;
                    var matches = day.Complete && day.Hours.HasValue && HungerPerformanceRules.Compare(day.Hours.Value, rule?.Operator ?? "LessThan", threshold);
                    evaluations.Add(new(rule?.Id ?? Guid.Empty, code, "Alert", "WorkingHours", day.Day, day.Hours, threshold, matches, severity, rule?.EvidenceReference ?? "InternalHoursV1"));
                    if (matches) alerts.Add($"{code}:{severity}:{day.Day:yyyy-MM-dd}:{rule?.Id}");
                    return matches;
                }
                if (!day.Complete) alerts.Add($"MissingCoverage:{day.Day:yyyy-MM-dd}");
                else if (!HoursAlert("HoursCritical", 8, "High")) HoursAlert("HoursFollowUp", 10, "Warning");
                if (day.Scheduled && HungerPerformanceRules.Mandatory(day.Day) && Override("MandatoryDays") is not { Enabled: false })
                {
                    if (day.Complete && day.Hours == 0) alerts.Add($"MandatoryDayNotWorked:{day.Day:yyyy-MM-dd}");
                    if (!day.Complete) alerts.Add($"MandatoryDayCoverageUnknown:{day.Day:yyyy-MM-dd}");
                }
                if (group.Any(f => f.Day == day.Day && f.NoShow > 0)) alerts.Add($"NoShowPotentialFine:{day.Day:yyyy-MM-dd}");
            }
            decimal? Metric(string metric, IEnumerable<PerformanceFact> values)
            {
                var set = values.ToArray();
                decimal? Sum(Func<PerformanceFact, decimal?> get) => set.All(f => get(f).HasValue) ? set.Sum(f => get(f)) : null;
                if (set.Length == 0) return null;
                return metric switch
                {
                    "CompletedOrders" => Sum(f => f.Orders), "WorkingHours" => Sum(f => f.Hours),
                    "AcceptedOrders" => Sum(f => f.Accepted), "StackedOrders" => Sum(f => f.Stacked),
                    "RejectedOrders" => Sum(f => f.Rejected), "CancelledOrders" => Sum(f => f.Cancelled), "NoShowCount" => Sum(f => f.NoShow),
                    "WorkedDays" => set.All(f => f.Hours.HasValue) ? set.GroupBy(f => f.Day).Count(g => g.Sum(f => f.Hours) > 0) : null,
                    "QualifiedDays" => set.All(f => f.Hours.HasValue) ? set.GroupBy(f => f.Day).Count(g => g.Sum(f => f.Hours) >= 8) : null, _ => null
                };
            }
            foreach (var periodGroup in group.GroupBy(f => new { f.Platform, f.City }))
            {
                foreach (var ruleGroup in rules.Where(r => r.ClientPlatformId == periodGroup.Key.Platform && (r.OperatingCityId == null || r.OperatingCityId == periodGroup.Key.City)).GroupBy(r => r.Code))
                {
                    foreach (var bucket in periodGroup.GroupBy(f => f.Day))
                    {
                        if (bucket.Key > cutoff) continue;
                        var rule = ruleGroup.Where(r => r.EffectiveFrom <= bucket.Key && (r.EffectiveThrough == null || r.EffectiveThrough >= bucket.Key))
                            .OrderByDescending(r => r.OperatingCityId.HasValue).ThenByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).FirstOrDefault();
                        if (rule == null || !rule.Enabled || rule.Period != "Day" || rule.Code is "HoursCritical" or "HoursFollowUp" or "MandatoryDays") continue;
                        var actual = Metric(rule.Metric, bucket);
                        var triggered = actual.HasValue && HungerPerformanceRules.Compare(actual.Value, rule.Operator, rule.Threshold);
                        evaluations.Add(new(rule.Id, rule.Code, rule.Kind, rule.Metric, bucket.Key, actual, rule.Threshold, triggered, rule.Severity, rule.EvidenceReference) { Deficit = rule.Kind == "Target" && actual.HasValue ? HungerPerformanceRules.Deficit(actual.Value, rule.Threshold, rule.Operator) : null });
                        if (rule.Kind == "Alert" && triggered) alerts.Add($"{rule.Code}:{rule.Severity}:{bucket.Key:yyyy-MM-dd}:{rule.Id}");
                    }
                    var monthlyBuckets = periodGroup.GroupBy(f => f.Day).Select(bucket => new
                    {
                        Facts = bucket.ToArray(),
                        Rule = ruleGroup.Where(r => r.EffectiveFrom <= bucket.Key && (r.EffectiveThrough == null || r.EffectiveThrough >= bucket.Key))
                            .OrderByDescending(r => r.OperatingCityId.HasValue).ThenByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).FirstOrDefault()
                    }).Where(b => b.Rule is { Enabled: true, Period: "Month" }).GroupBy(b => b.Rule!.Id);
                    foreach (var applicable in monthlyBuckets)
                    {
                        var monthly = applicable.First().Rule!;
                        var covered = applicable.SelectMany(b => b.Facts).ToArray();
                        if (monthly.Kind == "Target" && (!covered.Any(f => f.Scheduled) || covered.Any(f => f.Scheduled && !f.Responsibility.HasValue))) continue;
                        var observed = covered.Where(f => f.Day <= cutoff).ToArray();
                        var value = observed.Length == 0 ? 0 : Metric(monthly.Metric, observed);
                        var month = group.Key.Period;
                        var threshold = monthly.Kind == "Target" ? monthly.Threshold * covered.Where(f => f.Scheduled).Sum(f => f.Responsibility ?? 0) / DateTime.DaysInMonth(month.Year, month.Month) : monthly.Threshold;
                        var matches = value.HasValue && HungerPerformanceRules.Compare(value.Value, monthly.Operator, threshold);
                        evaluations.Add(new(monthly.Id, monthly.Code, monthly.Kind, monthly.Metric, month, value, threshold, matches, monthly.Severity, monthly.EvidenceReference)
                            { Deficit = monthly.Kind == "Target" && value.HasValue ? HungerPerformanceRules.Deficit(value.Value, threshold, monthly.Operator) : null });
                        if (monthly.Kind == "Alert" && matches) alerts.Add($"{monthly.Code}:{monthly.Severity}:{monthly.Id}");
                    }

                }
            }
            var targets = evaluations.Where(e => e.Kind == "Target" && e.Metric == "CompletedOrders" && rules.Any(r => r.Id == e.RuleId && r.Period == "Month")).ToArray();
            var targetOperators = targets.Select(t => rules.Single(r => r.Id == t.RuleId).Operator).Distinct().ToArray();
            var goalOperator = targetOperators.FirstOrDefault() ?? "GreaterThanOrEqual";
            decimal? target = targets.Length > 0 && targetOperators.Length == 1 && targets.Select(t => t.Code).Distinct().Count() == 1 ? targets.Sum(t => t.Threshold) : null;
            var cutoffOrders = group.Where(f => f.Day <= cutoff).Sum(f => f.Orders ?? 0);
            var forecast = target.HasValue ? HungerPerformanceRules.Forecast(cutoffOrders, target.Value, elapsed.Length, remaining,
                elapsed.All(d => d.Complete)) : (Forecast: (decimal?)null, RequiredPace: (decimal?)null);
            var metricFacts = group.Where(f => f.Day <= cutoff || f.Orders.HasValue || f.Hours.HasValue).ToArray();
            long? Counts(Func<PerformanceFact, int?> selector) => metricFacts.Length > 0 && metricFacts.All(f => selector(f).HasValue) ? metricFacts.Sum(f => (long?)selector(f)) : null;
            var row = new HungerPerformanceRow(group.Key.Entity, group.Key.Period, orders, hours, worked, qualified, unknown, days.Count(d => d.Scheduled),
                elapsed.Length, remaining, Counts(f => f.Accepted), Counts(f => f.Stacked), Dimension(group.Select(f => f.Sponsor?.ToString() ?? "Unknown")),
                Dimension(group.Select(f => f.Vehicle)), target, target.HasValue ? HungerPerformanceRules.Deficit(orders, target.Value, goalOperator) : null,
                target.HasValue && elapsed.All(d => d.Orders.HasValue) ? HungerPerformanceRules.Compare(cutoffOrders, goalOperator, target.Value) : null, forecast.Forecast, goalOperator is "GreaterThan" or "GreaterThanOrEqual" ? forecast.RequiredPace : null, alerts) { Catalog = evaluations, CoverageThrough = cutoff, ForecastObservedOrders = cutoffOrders, ActualVehicleType = Dimension(group.Select(f => f.VehicleType)), RejectedOrders = Counts(f => f.Rejected), CancelledOrders = Counts(f => f.Cancelled), NoShowCount = Counts(f => f.NoShow) };
            if ((query.ActualVehicle == null || row.ActualVehicle == query.ActualVehicle) && (query.ActualVehicleType == null || row.ActualVehicleType == query.ActualVehicleType) && (query.TargetAchieved == null || row.TargetAchieved == query.TargetAchieved)) output.Add(row);
        }
        var ordered = output.OrderBy(r => r.Period).ThenBy(r => r.EntityId).ToArray();
        var keys = ordered.Select(r => (r.EntityId, r.Period)).ToHashSet();
        var retained = facts.Where(f => keys.Contains((f.Entity, query.Period == "day" ? f.Day : new DateOnly(f.Day.Year, f.Day.Month, 1)))).ToArray();
        var retainedDates = retained.Select(f => (f.Account, f.Day)).ToHashSet();
        var distinctRiders = query.GroupBy == "rider" ? retained.Select(f => f.Entity).Distinct().Count()
            : assignments.Where(a => retained.Any(f => f.Account == a.PlatformRiderAccountId && f.Day >= a.EffectiveFrom && (a.EffectiveTo == null || f.Day <= a.EffectiveTo)))
                .Select(a => a.RiderProfileId).Concat(operations.Where(o => retainedDates.Contains((o.PlatformRiderAccountId, o.ReportDate)))
                    .SelectMany(o => Read<PlatformDayShare[]>(o.AllocationsJson)).Select(s => s.RiderProfileId)).Distinct().Count();
        return Result.Success(new HungerPerformanceReport(ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray(), ordered.Length,
            ordered.Sum(r => r.CompletedOrders), ordered.Sum(r => r.WorkingHours), retained.Select(f => f.Account).Distinct().Count(),
            distinctRiders, query.GroupBy, query.Period, query.Layout));
    }

    public async Task<Result<IReadOnlyList<HungerPotentialFineItem>>> PotentialFinesAsync(DateOnly from, DateOnly through, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<IReadOnlyList<HungerPotentialFineItem>>(HungerErrors.Forbidden);
        if (from == default || through < from || through.DayNumber - from.DayNumber > 366) return Result.Failure<IReadOnlyList<HungerPotentialFineItem>>(HungerErrors.Invalid("Invalid period."));
        var start = from;
        var rows = await (from fine in db.Set<HungerPotentialFine>().AsNoTracking() join operation in db.Set<HungerDailyOperation>().AsNoTracking() on fine.DailyOperationId equals operation.Id
            join account in db.PlatformRiderAccounts.AsNoTracking() on operation.PlatformRiderAccountId equals account.Id
            where platforms.Contains(account.ClientPlatformId) && operation.ReportDate >= start && operation.ReportDate <= through
            orderby operation.ReportDate, fine.Id select new { Fine = fine, Account = account.Id, operation.ReportDate }).ToArrayAsync(ct);
        return Result.Success<IReadOnlyList<HungerPotentialFineItem>>(rows.Select(r => new HungerPotentialFineItem(r.Fine.Id, r.Fine.DailyOperationId, r.Account,
            r.ReportDate, r.Fine.NoShowCount, r.Fine.Status, r.Fine.InvoiceDocumentId, r.Fine.InvoiceSourceKey, r.Fine.ResolutionEvidence, Convert.ToBase64String(r.Fine.RowVersion))).ToArray());
    }

    public async Task<Result> ResolvePotentialFineAsync(Guid id, HungerPotentialFineResolution request, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.FinanceManage, ct);
        var source = await (from f in db.Set<HungerPotentialFine>().AsNoTracking() join o in db.Set<HungerDailyOperation>().AsNoTracking() on f.DailyOperationId equals o.Id
            join a in db.PlatformRiderAccounts.AsNoTracking() on o.PlatformRiderAccountId equals a.Id
            where f.Id == id && platforms.Contains(a.ClientPlatformId) select new { Fine = f, Operation = o, Platform = a.ClientPlatformId }).SingleOrDefaultAsync(ct);
        if (source == null) return Result.Failure(HungerErrors.Forbidden);
        if (request.Status is not ("Invoiced" or "Waived") || string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 1000
            || request.Status == "Waived" && (request.InvoiceDocumentId.HasValue || request.InvoiceSourceKey != null)) return Result.Failure(HungerErrors.Invalid("Resolution evidence and valid status required."));
        var result = await FinancialWrite([source.Platform, source.Operation.PlatformRiderAccountId], async () =>
        {
            var fine = await db.Set<HungerPotentialFine>().SingleAsync(f => f.Id == id, ct);
            if (fine.Status != "Potential" || Convert.ToBase64String(fine.RowVersion) != request.RowVersion)
                return Result.Failure<Guid>(HungerErrors.Conflict("Potential fine already resolved or changed."));
            if (request.Status == "Invoiced")
            {
                var invoice = await db.Set<HungerSettlementDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.Id == request.InvoiceDocumentId
                    && d.ClientPlatformId == source.Platform && d.Kind == "Invoice" && d.Status == "Matched"
                    && d.ChargeFrom <= source.Operation.ReportDate && d.ChargeThrough >= source.Operation.ReportDate, ct);
                var line = invoice == null ? null : Read<HungerSettlementRequest>(invoice.DataJson).Lines?.SingleOrDefault(l => l.SourceKey == request.InvoiceSourceKey && l.Level == Read<HungerSettlementRequest>(invoice.DataJson).CanonicalLevel);
                if (line == null || line.AccountId != source.Operation.PlatformRiderAccountId)
                    return Result.Failure<Guid>(HungerErrors.Invalid("Matched invoice canonical line must identify the same account and cover the source day."));
                var sourceRiders = Read<PlatformDayShare[]>(source.Operation.AllocationsJson).Select(s => s.RiderProfileId).Distinct().ToArray();
                if (line.RiderProfileId.HasValue && (sourceRiders.Length != 1 || sourceRiders[0] != line.RiderProfileId))
                    return Result.Failure<Guid>(HungerErrors.Invalid("Invoice rider does not match an unambiguous source rider."));
                var reconciled = await db.Set<HungerPotentialFine>().Where(f => f.InvoiceDocumentId == invoice!.Id && f.InvoiceSourceKey == request.InvoiceSourceKey
                    && f.Status == "Invoiced").SumAsync(f => (long)f.NoShowCount, ct);
                if (line.NoShowCount.HasValue ? reconciled + fine.NoShowCount > line.NoShowCount : reconciled > 0)
                    return Result.Failure<Guid>(HungerErrors.Conflict("Invoice NoShow capacity is exceeded or unknown for an additional source."));
            }
            fine.Status = request.Status; fine.InvoiceDocumentId = request.InvoiceDocumentId; fine.InvoiceSourceKey = request.InvoiceSourceKey; fine.ResolutionEvidence = request.Evidence;
            return Result.Success(fine.Id);
        }, ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}

