using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
namespace LogisticsERP.Infrastructure.Hr;
internal sealed partial class PlatformAccountDayAllocationService
{
    internal static Guid[] Ids(PlatformAccountDayContribution c) => JsonSerializer.Deserialize<Guid[]>(c.AssignmentIdsJson)!;
    internal async Task<PlatformAccountDayContribution[]> Contributions(Guid account, DateOnly date, CancellationToken ct)
    {
        if (cachedContributions is not null) return cachedContributions[(account, date)].ToArray();
        var rows = await db.Set<PlatformAccountDayContribution>().Where(c => c.PlatformRiderAccountId == account && c.BusinessDate == date).ToArrayAsync(ct);
        return rows.Concat(db.Set<PlatformAccountDayContribution>().Local.Where(c => c.PlatformRiderAccountId == account && c.BusinessDate == date))
            .DistinctBy(c => c.Id).Where(c => !c.IsDeleted).ToArray();
    }
    internal static PlatformAccountDayUsageInput ContributionFigures(PlatformAccountDayContribution c, IReadOnlyDictionary<Guid, PlatformAccountDayUsage> usages)
    {
        if (c.FiguresJson is not null) return JsonSerializer.Deserialize<PlatformAccountDayUsageInput>(c.FiguresJson)!;
        var inputs = Ids(c).Select(id => Figures(usages.GetValueOrDefault(id))).ToArray();
        decimal? Sum(PlatformDayMetric m) => inputs.All(i => Value(i, m).HasValue) ? inputs.Sum(i => Value(i, m)!.Value) : null;
        return new((int?)Sum(PlatformDayMetric.OrdersCount), Sum(PlatformDayMetric.WorkingHours), Sum(PlatformDayMetric.CollectedAmount),
            Sum(PlatformDayMetric.DeductionsAmount), (int?)Sum(PlatformDayMetric.TotalValidOffers), (int?)Sum(PlatformDayMetric.DeclinedOffers), (int?)Sum(PlatformDayMetric.MissedOffers));
    }
    internal async Task<Result<PlatformAssignmentContextResponse>> Context(Guid account, DateOnly date, Guid rider, CancellationToken ct)
    {
        var candidates = await Candidates(account, date, ct);
        if (candidates.Length == 0) return Result.Success(new PlatformAssignmentContextResponse(account, date, rider, false, []));
        var order = await Ordered(account, date, ct);
        if (order.IsFailure) return Result.Failure<PlatformAssignmentContextResponse>(order.Error);
        var contributions = await Contributions(account, date, ct);
        var usages = (await Usages(account, date, ct)).ToDictionary(u => u.RiderClientAssignmentId);
        var covered = contributions.SelectMany(c => Ids(c).SkipLast(1)).ToHashSet();
        var representatives = order.Value!.Where(a => !covered.Contains(a.Id)).ToArray();
        var riderIds = representatives.Select(a => a.RiderProfileId).ToArray();
        var names = await (from r in db.RiderProfiles join e in db.Employees on r.EmployeeId equals e.Id
            where riderIds.Contains(r.Id) select new { r.Id, e.FullNameAr }).ToDictionaryAsync(x => x.Id, x => x.FullNameAr, ct);
        var code = await (from a in db.PlatformRiderAccounts join platform in db.ClientPlatforms on a.ClientPlatformId equals platform.Id
            where a.Id == account select platform.Code).SingleOrDefaultAsync(ct);
        var optional = code == "NINJA" ? new[] { PlatformDayMetric.WorkingHours, PlatformDayMetric.CollectedAmount, PlatformDayMetric.DeductionsAmount }
            : code is "CHEFZ" or "THECHEFZ" or "CHEFSZ" or "SHIFTZ" ? new[] { PlatformDayMetric.TotalValidOffers, PlatformDayMetric.DeclinedOffers, PlatformDayMetric.MissedOffers } : [];
        var result = representatives.Select((a, i) => {
            var c = contributions.SingleOrDefault(c => c.RepresentativeAssignmentId == a.Id);
            var ids = c is null ? new[] { a.Id } : Ids(c);
            var figures = c is null ? Figures(usages.GetValueOrDefault(a.Id)) : ContributionFigures(c, usages);
            var required = i < representatives.Length - 1 || a.RiderProfileId != rider;
            var token = c is null ? usages.GetValueOrDefault(a.Id)?.RowVersion : c.RowVersion;
            return new PlatformDayContributionResponse(ids, a.RiderProfileId, names.GetValueOrDefault(a.RiderProfileId, ""), figures,
                token is null ? null : Convert.ToBase64String(token), ids.ToDictionary(id => id, id => usages.TryGetValue(id, out var u) ? Convert.ToBase64String(u.RowVersion) : null),
                required && figures.OrdersCount is null ? ["OrdersCount"] : [], optional.Where(m => Value(figures, m) is null).Select(m => m.ToString()).ToArray());
        }).ToArray();
        return Result.Success(new PlatformAssignmentContextResponse(account, date, rider, result.Any(c => c.MissingRequiredFields.Count > 0), result));
    }
    internal async Task<Result> PrepareAssignment(Guid account, DateOnly date, Guid rider,
        IReadOnlyList<PreviousDayUsageInput>? changes, string reason, CancellationToken ct, bool correction = false)
    {
        var context = await Context(account, date, rider, ct);
        if (context.IsFailure) return Result.Failure(context.Error);
        if (changes is { Count: > 100 } || changes?.Any(c => c is null || c.AssignmentIds is null || c.Figures is null || !Valid(c.Figures)) == true)
            return Result.Failure(HrErrors.InvalidRequest);
        var groups = context.Value!.Contributions;
        var updates = new List<(PlatformDayContributionResponse Group, PreviousDayUsageInput Change)>();
        foreach (var change in changes ?? [])
        {
            var group = groups.SingleOrDefault(g => g.AssignmentIds.SequenceEqual(change.AssignmentIds));
            if (group is null || updates.Any(u => u.Group == group)) return Result.Failure(Error(account, date, "Contribution membership changed; reload assignment context."));
            if (group.RowVersion != change.RowVersion || change.UsageRowVersions is null || group.UsageRowVersions.Any(v => !change.UsageRowVersions.TryGetValue(v.Key, out var token) || token != v.Value))
                return Result.Failure(HrErrors.ConcurrencyConflict);
            foreach (var metric in Enum.GetValues<PlatformDayMetric>())
                if (!correction && Value(group.Figures, metric) is { } saved && Value(change.Figures, metric) != saved)
                    return Result.Failure(Error(account, date, "Use the day-usage correction workflow to change saved figures."));
            if (group.AssignmentIds.Count > 1)
            {
                var individual = (await Usages(account, date, ct)).Where(u => group.AssignmentIds.Contains(u.RiderClientAssignmentId)).ToArray();
                foreach (var metric in Enum.GetValues<PlatformDayMetric>().Where(m => m != PlatformDayMetric.DeductionsAmount))
                    if (Value(change.Figures, metric) is { } total && individual.Sum(u => Value(Figures(u), metric) ?? 0) > total)
                        return Result.Failure(Error(account, date, "Group total contradicts recorded individual figures."));
            }
            updates.Add((group, change));
        }
        foreach (var group in groups)
            if (group.MissingRequiredFields.Count > 0 && updates.SingleOrDefault(u => u.Group == group).Change?.Figures.OrdersCount is null)
                return Result.Failure(Error(account, date, "Previous rider OrdersCount is required before assignment.", group.AssignmentIds[^1], "OrdersCount"));
        var frozen = await Frozen(account, date, ct);
        foreach (var (group, change) in updates)
            if (frozen.Any(m => Value(group.Figures, m) != Value(change.Figures, m)))
                return Result.Failure(Error(account, date, "Saved allocations reference these figures; use their correction workflow."));
        var contributions = await Contributions(account, date, ct);
        foreach (var (group, change) in updates)
        {
            if (group.AssignmentIds.Count == 1)
            {
                var staged = await Stage(account, group.AssignmentIds[0], date, change.Figures, reason, false, ct);
                if (staged.IsFailure) return staged;
            }
            else
            {
                var c = contributions.Single(c => c.RepresentativeAssignmentId == group.AssignmentIds[^1]);
                c.FiguresJson = JsonSerializer.Serialize(change.Figures); c.Reason = reason;
            }
        }
        return Result.Success();
    }
    internal async Task LinkContinuation(Guid account, DateOnly date, string reason, CancellationToken ct)
    {
        var ordered = await Ordered(account, date, ct);
        if (ordered.IsFailure || ordered.Value!.Length < 2) return;
        var last = ordered.Value[^1];
        var run = ordered.Value.Reverse().TakeWhile(a => a.RiderProfileId == last.RiderProfileId).Reverse().ToArray();
        if (run.Length < 2) return;
        foreach (var old in await Contributions(account, date, ct))
            if (Ids(old).Intersect(run.Select(a => a.Id)).Any()) { old.IsDeleted = true; old.Reason = reason; }
        db.Add(new PlatformAccountDayContribution { PlatformRiderAccountId = account, BusinessDate = date,
            RiderProfileId = last.RiderProfileId, RepresentativeAssignmentId = last.Id,
            AssignmentIdsJson = JsonSerializer.Serialize(run.Select(a => a.Id)), Reason = reason });
    }
}
