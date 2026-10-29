using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaEvaluationService(ApplicationDbContext db, ICurrentUser user,
    IPermissionChecker permissions, TimeProvider clock, IConfiguration configuration) : INinjaEvaluationService
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
    private bool Enabled => configuration.GetValue<bool>("Features:NinjaEvaluation");
    private static OperationError Invalid(string message) => NinjaErrors.Invalid(message);
    private static OperationError Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    private static bool MonthValid(int year, int month) => year is >= 1900 and <= 9998 && month is >= 1 and <= 12;
    private static bool ReasonValid(string? reason) => !string.IsNullOrWhiteSpace(reason) && reason.Length <= 2000;
    private async Task<Guid[]> Platforms(string key, CancellationToken ct)
    {
        if (!Enabled || user.UserId is not { } id || user.AuthorizationVersion is not { } version) return [];
        var candidates = await db.ClientPlatforms.AsNoTracking().Where(x => x.Code == "NINJA").Select(x => x.Id).ToArrayAsync(ct);
        var allowed = new List<Guid>();
        foreach (var platform in candidates)
            if (await permissions.HasPermissionAsync(id, version, key, new(AccessScopeType.ClientPlatform, platform), ct)) allowed.Add(platform);
        return allowed.ToArray();
    }
    private async Task<OperationError?> Access(Guid account, string key, CancellationToken ct)
    {
        var ids = await Platforms(key, ct);
        if (ids.Length == 0) return NinjaErrors.Forbidden;
        return await db.PlatformRiderAccounts.IgnoreQueryFilters().AnyAsync(a => a.Id == account && ids.Contains(a.ClientPlatformId), ct)
            ? null : NinjaErrors.NotFound;
    }
    public async Task<Result<NinjaEvaluationPolicyResponse>> PolicyAsync(CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Ninja.Read, ct)).Length == 0) return Result.Failure<NinjaEvaluationPolicyResponse>(NinjaErrors.Forbidden);
        return Result.Success(new NinjaEvaluationPolicyResponse(NinjaSuggestionCalculator.PolicyVersion, "CalendarDays", .85m, 8, .15m,
            "AwayFromZero:2", NinjaSuggestionCalculator.Rates("BIKE")!, NinjaSuggestionCalculator.Rates("CAR")!,
            ["Acceptance", "Timing", "Uniform", "LeaveCompliance"]));
    }
    public Task<Result<NinjaEvaluationReport>> EvaluateAsync(NinjaEvaluationQuery query, CancellationToken ct = default) => Report(query, false, ct);

    private async Task<Result<NinjaEvaluationReport>> Report(NinjaEvaluationQuery query, bool all, CancellationToken ct)
    {
        var platformIds = await Platforms(PermissionKeys.Ninja.Read, ct);
        if (platformIds.Length == 0) return Result.Failure<NinjaEvaluationReport>(NinjaErrors.Forbidden);
        if (!MonthValid(query.Year, query.Month) || query.View is not ("accounts" or "riders")
            || query.Page is < 1 or > 100000 || query.PageSize is < 1 or > 100 || query.Search?.Length > 200
            || query.PricingType is not (null or "CAR" or "BIKE") || query.Status is not (null or "TargetAchieved" or "BelowTarget" or "Unknown")) return Result.Failure<NinjaEvaluationReport>(Invalid("حدد الشهر والعرض والفلاتر الصحيحة."));
        var start = new DateOnly(query.Year, query.Month, 1); var end = start.AddMonths(1).AddDays(-1);
        if (start > Today || query.AsOf is { } requested && (requested < start || requested > end || requested > Today))
            return Result.Failure<NinjaEvaluationReport>(Invalid("حد التقييم يجب أن يقع في الشهر وحتى اليوم بتوقيت الرياض."));
        var accountQuery = db.PlatformRiderAccounts.IgnoreQueryFilters().AsNoTracking().Where(a => platformIds.Contains(a.ClientPlatformId));
        if (query.AccountId is { } accountId && !await accountQuery.AnyAsync(a => a.Id == accountId, ct)) return Result.Failure<NinjaEvaluationReport>(NinjaErrors.NotFound);
        if (query.RiderId is { } riderId && !await db.RiderProfiles.IgnoreQueryFilters().AnyAsync(r => r.Id == riderId, ct)) return Result.Failure<NinjaEvaluationReport>(NinjaErrors.NotFound);
        // Load the complete permitted account context before rider filtering. Account estimates never become rider estimates.
        var accounts = await accountQuery.Where(a => (query.AccountId == null || a.Id == query.AccountId)
            && (query.CityId == null || a.OperatingCityId == query.CityId) && (query.SponsorId == null || a.SponsorId == query.SponsorId)).ToArrayAsync(ct);
        var ids = accounts.Select(a => a.Id).ToArray();
        var assignments = await db.RiderClientAssignments.IgnoreQueryFilters().AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId)
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
            && a.EffectiveFrom <= end && (a.EffectiveTo == null || a.EffectiveTo >= start)).ToArrayAsync(ct);
        var sources = await db.Set<NinjaDailyPerformanceSource>().AsNoTracking().Where(s => ids.Contains(s.PlatformRiderAccountId) && s.ReportDate >= start && s.ReportDate <= end).ToArrayAsync(ct);
        var facts = await db.NinjaDailyPerformance.AsNoTracking().Where(s => ids.Contains(s.PlatformRiderAccountId) && s.ReportDate >= start && s.ReportDate <= end).ToArrayAsync(ct);
        var sourceIds = sources.Select(s => s.Id).ToArray();
        var importIds = sources.Select(s => s.ImportId).Distinct().ToArray();
        var origins = await db.Set<NinjaAllocationProvenance>().AsNoTracking().Where(x => sourceIds.Contains(x.SourceId)).ToArrayAsync(ct);
        var imports = await db.NinjaDailyImports.AsNoTracking().Where(i => importIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        var contexts = await db.Set<NinjaEvaluationContextRevision>().AsNoTracking().Where(c => ids.Contains(c.PlatformRiderAccountId)).ToArrayAsync(ct);
        var revisions = await db.Set<NinjaResponsibilityRevision>().AsNoTracking().Where(r => ids.Contains(r.PlatformRiderAccountId) && r.Month == start).ToArrayAsync(ct);
        var revisionIds = revisions.Select(r => r.Id).ToArray();
        var lines = await db.Set<NinjaResponsibilityLine>().AsNoTracking().Where(l => revisionIds.Contains(l.RevisionId)).ToArrayAsync(ct);
        var peopleIds = assignments.Select(a => a.RiderProfileId).Concat(facts.Select(f => f.RiderProfileId)).Distinct().ToArray();
        var people = await (from r in db.RiderProfiles.IgnoreQueryFilters().AsNoTracking()
            join e in db.Employees.IgnoreQueryFilters().AsNoTracking() on r.EmployeeId equals e.Id
            where peopleIds.Contains(r.Id) select new { r.Id, Name = e.FullNameAr }).ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        var sponsors = await db.Sponsors.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.RegistryNameAr, ct);
        var cities = await (from c in db.OperatingCities.IgnoreQueryFilters().AsNoTracking()
            join g in db.GlobalCities.IgnoreQueryFilters().AsNoTracking() on c.GlobalCityId equals g.Id select new { c.Id, g.NameAr }).ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);
        var relevantDates = sources.Where(s => query.RiderId == null || facts.Any(f => f.SourceId == s.Id && f.RiderProfileId == query.RiderId))
            .Select(s => (DateOnly?)s.ReportDate);
        var asOf = query.AsOf ?? relevantDates.Max() ?? (Today < end ? Today : end);
        var accountRows = new List<NinjaEvaluationRow>(); var riderDays = new List<NinjaEvaluationDay>();
        var openRiders = new HashSet<Guid>();
        var holdersByAccount = assignments.ToLookup(a => a.PlatformRiderAccountId);
        var sourcesByAccount = sources.ToLookup(s => s.PlatformRiderAccountId);
        var factsByDay = facts.ToLookup(f => (f.PlatformRiderAccountId, f.ReportDate));
        var contextsByAccount = contexts.ToLookup(c => c.PlatformRiderAccountId);
        var revisionsByAccount = revisions.ToLookup(r => r.PlatformRiderAccountId);
        var linesByRevision = lines.ToLookup(l => l.RevisionId);
        var originsByShare = origins.ToDictionary(o => (o.SourceId, o.RiderClientAssignmentId), o => o.Origin);
        foreach (var account in accounts)
        {
            ct.ThrowIfCancellationRequested();
            var holders = holdersByAccount[account.Id].ToArray();
            var accountSources = sourcesByAccount[account.Id].ToDictionary(s => s.ReportDate);
            if (holders.Length == 0 && accountSources.Count == 0) continue;
            var context = contextsByAccount[account.Id].MaxBy(c => c.Revision);
            var pricing = context is null ? [] : JsonSerializer.Deserialize<NinjaPricingPeriod[]>(context.PricingPeriodsJson, Json)!;
            var responsibility = revisionsByAccount[account.Id].MaxBy(r => r.Revision);
            var weights = linesByRevision[responsibility?.Id ?? Guid.Empty].ToDictionary(l => (l.BusinessDate, l.RiderClientAssignmentId), l => l.Weight);
            var accountDays = new List<NinjaEvaluationDay>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (context?.ConfirmedFirstDeliveryDate is { } first && date < first) continue;
                var dayHolders = holders.Where(a => a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date)).ToArray();
                var types = dayHolders.Select(a => Pricing(a.NinjaRiderType)).Distinct().ToArray();
                var type = pricing.SingleOrDefault(p => p.From <= date && p.To >= date)?.PricingType
                    ?? (types.Length == 1 ? types[0] : null);
                accountSources.TryGetValue(date, out var source);
                imports.TryGetValue(source?.ImportId ?? Guid.Empty, out var import);
                accountDays.Add(Day(date, account, null, type, 1, source?.TotalOrders, source?.TotalWorkingHours, source, import, "Reported", true, start));
                var saved = factsByDay[(account.Id, date)].ToArray();
                var reconciled = source is not null && saved.Length > 0 && saved.All(f => f.SourceId == source.Id && dayHolders.Any(a => a.Id == f.RiderClientAssignmentId && a.RiderProfileId == f.RiderProfileId))
                    && saved.Sum(f => (long)f.TotalOrders) == source.TotalOrders && saved.Sum(f => f.TotalWorkingHours) == source.TotalWorkingHours;
                foreach (var holder in dayHolders)
                {
                    var fact = saved.SingleOrDefault(f => f.RiderClientAssignmentId == holder.Id);
                    decimal? weight = weights.TryGetValue((date, holder.Id), out var w) ? w : dayHolders.Length == 1 ? 1 : null;
                    var origin = originsByShare.GetValueOrDefault((source?.Id ?? Guid.Empty, holder.Id)) ?? "LegacyUnknown";
                    riderDays.Add(Day(date, account, holder, type ?? Pricing(holder.NinjaRiderType), weight,
                        fact?.TotalOrders, fact?.TotalWorkingHours, source, import, origin, reconciled, start));
                    if (holder.EffectiveTo is null) openRiders.Add(holder.RiderProfileId);
                }
            }
            accountRows.Add(BuildRow($"account:{account.Id}", account.Id, null, null, accountDays, asOf, start, Today,
                account.OperatingCityId, cities.GetValueOrDefault(account.OperatingCityId), account.SponsorId,
                account.SponsorId is { } sponsor ? sponsors.GetValueOrDefault(sponsor) : null,
                context?.ConfirmedFirstDeliveryDate is null ? ["AccountStartAssumedMonthStart"] : [], holders.Any(h => h.EffectiveTo is null)));
        }
        var riderRows = riderDays.GroupBy(d => d.RiderProfileId!.Value).Select(g =>
        {
            var rowAccountIds = g.Select(d => d.AccountId).Distinct().ToArray();
            var singleAccount = rowAccountIds.Length == 1 ? accounts.Single(a => a.Id == rowAccountIds[0]) : null;
            var row = BuildRow($"rider:{g.Key}", null, g.Key, people.GetValueOrDefault(g.Key), g.ToArray(), asOf, start, Today,
                singleAccount?.OperatingCityId, singleAccount is null ? null : cities.GetValueOrDefault(singleAccount.OperatingCityId),
                singleAccount?.SponsorId, singleAccount?.SponsorId is { } s ? sponsors.GetValueOrDefault(s) : null, [], openRiders.Contains(g.Key));
            var overlap = g.GroupBy(d => d.Date).Any(day => day.Sum(d => d.Responsibility ?? 0) > 1);
            return overlap ? row with { SuggestedMoney = null, Forecast = null, ContinuationScenario = null,
                Reasons = row.Reasons.Append("OverlappingRiderResponsibility").ToArray() } : row;
        }).ToArray();
        IEnumerable<NinjaEvaluationRow> selected = query.View == "accounts" ? accountRows : riderRows;
        if (query.RiderId is { } selectedRider) selected = selected.Where(r => r.RiderProfileId == selectedRider || r.AccountIds.Any(a => riderDays.Any(d => d.AccountId == a && d.RiderProfileId == selectedRider)));
        if (query.PricingType is not null) selected = selected.Where(r => r.Days.Any(d => d.PricingType == query.PricingType));
        if (query.Status is not null) selected = selected.Where(r => r.TargetStatus == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search)) selected = selected.Where(r => (r.RiderName?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false)
            || r.CaptainIds.Any(c => c.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
            || (query.View == "accounts" && riderRows.Any(p => p.AccountIds.Any(r.AccountIds.Contains) && (p.RiderName?.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ?? false))));
        var rows = selected.OrderBy(r => r.RiderName, StringComparer.Ordinal).ThenBy(r => r.Key, StringComparer.Ordinal).ToArray();
        var includedAccounts = rows.SelectMany(r => r.AccountIds).Distinct().ToArray();
        var comparisons = accountRows.Where(a => includedAccounts.Contains(a.AccountId!.Value)).Select(a =>
        {
            // A rider's combined estimate is not reused as the share of a single account.
            var shares = riderDays.Where(d => d.AccountId == a.AccountId && a.LatestCompleteDate.HasValue && d.Date <= a.LatestCompleteDate.Value)
                .GroupBy(d => d.RiderProfileId).Select(g =>
                BuildRow("comparison", a.AccountId, g.Key, null, g.ToArray(), a.LatestCompleteDate!.Value, start, Today, null, null, null, null, [], false)).ToArray();
            var complete = shares.Length > 0 && shares.All(r => r.SuggestedMoney is not null && r.Days.All(d => d.Coverage == "Complete"));
            decimal? sum = complete ? shares.Sum(r => r.SuggestedMoney!.SuggestedAmount) : null;
            return new NinjaAccountSuggestionComparison(a.AccountId!.Value, a.SuggestedMoney?.SuggestedAmount, sum,
                sum is { } total && a.SuggestedMoney is { } m ? m.SuggestedAmount - total : null, a.LatestCompleteDate,
                query.RiderId is null);
        }).ToArray();
        var summary = new NinjaEvaluationSummary(rows.Length, includedAccounts.Length,
            riderDays.Where(d => includedAccounts.Contains(d.AccountId) && (query.RiderId == null || d.RiderProfileId == query.RiderId)).Select(d => d.RiderProfileId).Distinct().Count(),
            rows.Sum(r => r.Observed.Orders), rows.Sum(r => r.Observed.Hours), rows.All(r => r.SuggestedMoney is not null)
                ? rows.Sum(r => r.SuggestedMoney!.SuggestedAmount) : null,
            rows.Select(r => r.LatestCompleteDate).Distinct().Count() > 1, rows.Count(r => r.SuggestedMoney is null), comparisons);
        var inputRevision = Hash(JsonSerializer.Serialize(new { Policy = NinjaSuggestionCalculator.PolicyVersion,
            EvaluationRules = "AvailableDaysWithBikeDefault",
            query = query with { Page = 1, PageSize = 100 }, asOf, today = Today, accounts = accounts.OrderBy(a => a.Id), assignments = assignments.OrderBy(a => a.Id),
            sources = sources.OrderBy(s => s.Id), facts = facts.OrderBy(f => f.Id), contexts = contexts.OrderBy(c => c.Id),
            revisions = revisions.OrderBy(r => r.Id), lines = lines.OrderBy(l => l.Id), origins = origins.OrderBy(o => o.Id) }, Json));
        return Result.Success(new NinjaEvaluationReport(query.Year, query.Month, Today, asOf, query.View,
            NinjaSuggestionCalculator.PolicyVersion, inputRevision, clock.GetUtcNow(), summary,
            all ? rows : rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToArray(), query.Page, query.PageSize));
    }

    private static string? Pricing(NinjaRiderType? type) => type switch
    {
        NinjaRiderType.InternalCar or NinjaRiderType.ExternalCar => "CAR",
        NinjaRiderType.InternalMotorcycle or NinjaRiderType.ExternalMotorcycle => "BIKE", _ => null
    };
    private static NinjaEvaluationDay Day(DateOnly date, PlatformRiderAccount account, RiderClientAssignment? assignment,
        string? type, decimal? weight, int? orders, decimal? hours, NinjaDailyPerformanceSource? source,
        NinjaDailyImport? import, string origin, bool reconciled, DateOnly month)
    {
        type ??= "BIKE";
        var rates = NinjaSuggestionCalculator.Rates(type);
        decimal? target = rates is not null && weight is { } w ? rates.Target * w / DateTime.DaysInMonth(month.Year, month.Month) : null;
        var coverage = orders is null || hours is null ? "Missing" : !reconciled ? "AllocationMismatch" : "Complete";
        var status = hours is null ? "Unknown" : weight is null || weight is > 0 and < 1 ? "PartialShiftUnknown" : weight == 0 ? "NotScheduled" : hours >= 8 ? "Qualified" : "ShortHours";
        return new(date, account.Id, account.ExternalAccountId, assignment?.Id, assignment?.RiderProfileId,
            type, weight, orders, hours, target, orders is { } o && target is { } t ? o - t : null, status,
            coverage, source?.Id, source?.ImportId, source?.SourceRowNumber, origin, import?.CreatedAtUtc);
    }
}
