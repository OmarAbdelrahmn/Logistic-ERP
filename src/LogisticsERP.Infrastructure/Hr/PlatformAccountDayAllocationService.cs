using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class PlatformAccountDayAllocationService(ApplicationDbContext db) : IPlatformAccountDayAllocationService
{
    private ILookup<Guid, RiderClientAssignment>? cachedAssignments;
    private ILookup<(Guid AccountId, DateOnly Date), PlatformAccountDayUsage>? cachedUsages;
    private ILookup<Guid, JahezAccountSession>? cachedSessions;
    private HashSet<Guid>? fullTimeAccounts;
    private ILookup<(Guid AccountId, DateOnly Date), PlatformAccountDayContribution>? cachedContributions;

    internal async Task Preload(Guid[] accountIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        cachedContributions = (await db.Set<PlatformAccountDayContribution>().AsNoTracking().Where(c => accountIds.Contains(c.PlatformRiderAccountId)
            && c.BusinessDate >= from && c.BusinessDate <= to).ToArrayAsync(ct)).ToLookup(c => (c.PlatformRiderAccountId, c.BusinessDate));
        cachedAssignments = (await db.RiderClientAssignments.AsNoTracking().Where(a => accountIds.Contains(a.PlatformRiderAccountId)
            && a.EffectiveFrom <= to && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArrayAsync(ct)).ToLookup(a => a.PlatformRiderAccountId);
        cachedUsages = (await db.Set<PlatformAccountDayUsage>().AsNoTracking().Where(u => accountIds.Contains(u.PlatformRiderAccountId)
            && u.BusinessDate >= from && u.BusinessDate <= to).ToArrayAsync(ct)).ToLookup(u => (u.PlatformRiderAccountId, u.BusinessDate));
        cachedSessions = (await db.Set<JahezAccountSession>().AsNoTracking().Where(h => accountIds.Contains(h.PlatformRiderAccountId)).ToArrayAsync(ct))
            .ToLookup(h => h.RiderClientAssignmentId);
        fullTimeAccounts = (await db.ChefzAccountDetails.Where(d => accountIds.Contains(d.PlatformRiderAccountId)
            && d.AccountType == ChefzAccountType.FullTime).Select(d => d.PlatformRiderAccountId).ToArrayAsync(ct)).ToHashSet();
    }
    internal static OperationError Error(Guid account, DateOnly date, string message, Guid? assignment = null, string? metric = null) =>
        new("platform.day_allocation", $"Account {account}, {date:yyyy-MM-dd}: {message}", ErrorType.Conflict,
            metric, new Dictionary<string, object?> { ["accountId"] = account, ["businessDate"] = date,
                ["assignmentId"] = assignment, ["metric"] = metric });

    internal static PlatformAccountDayUsageInput Figures(PlatformAccountDayUsage? u) => u is null ? new() :
        new(u.OrdersCount, u.WorkingHours, u.CollectedAmount, u.DeductionsAmount, u.TotalValidOffers, u.DeclinedOffers, u.MissedOffers);
    internal static decimal? Value(PlatformAccountDayUsageInput u, PlatformDayMetric metric) => metric switch
    {
        PlatformDayMetric.OrdersCount => u.OrdersCount,
        PlatformDayMetric.WorkingHours => u.WorkingHours,
        PlatformDayMetric.CollectedAmount => u.CollectedAmount,
        PlatformDayMetric.DeductionsAmount => u.DeductionsAmount,
        PlatformDayMetric.TotalValidOffers => u.TotalValidOffers,
        PlatformDayMetric.DeclinedOffers => u.DeclinedOffers,
        PlatformDayMetric.MissedOffers => u.MissedOffers,
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };

    internal static bool Valid(PlatformAccountDayUsageInput? u) => u is null ||
        (u.OrdersCount is null or >= 0 && u.WorkingHours is null or >= 0 and <= 24
        && u.CollectedAmount is null or >= 0 && u.TotalValidOffers is null or >= 0
        && u.DeclinedOffers is null or >= 0 && u.MissedOffers is null or >= 0
        && (u.WorkingHours is null || decimal.Round(u.WorkingHours.Value, 8) == u.WorkingHours)
        && (u.CollectedAmount is null || decimal.Round(u.CollectedAmount.Value, 2) == u.CollectedAmount)
        && (u.DeductionsAmount is null || decimal.Round(u.DeductionsAmount.Value, 2) == u.DeductionsAmount)
        && u.CollectedAmount is null or <= 1000000000m && u.DeductionsAmount is null or >= -1000000000m and <= 1000000000m);

    internal async Task<RiderClientAssignment[]> Candidates(Guid account, DateOnly date, CancellationToken ct)
    {
        var rows = cachedAssignments?[account].ToArray() ?? await db.RiderClientAssignments.Where(a => a.PlatformRiderAccountId == account
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
            && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date)).ToArrayAsync(ct);
        var candidates = rows.Concat(cachedAssignments is null ? db.RiderClientAssignments.Local.Where(a => a.PlatformRiderAccountId == account) : [])
            .DistinctBy(a => a.Id).Where(a => a.Status is RiderAssignmentStatus.Active or RiderAssignmentStatus.Ended
                && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date)).ToArray();
        var ids = candidates.Select(a => a.Id).ToArray();
        var sessions = cachedSessions is not null ? ids.SelectMany(id => cachedSessions[id]).ToArray()
            : await db.Set<JahezAccountSession>().Where(h => ids.Contains(h.RiderClientAssignmentId)).ToArrayAsync(ct);
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
        return candidates.Where(a => !sessions.Any(h => h.RiderClientAssignmentId == a.Id)
            || sessions.Any(h => h.RiderClientAssignmentId == a.Id && h.StartedAtUtc < start.AddDays(1)
                && (h.EndedAtUtc == null || h.EndedAtUtc > start))).ToArray();
    }

    internal async Task<PlatformAccountDayUsage[]> Usages(Guid account, DateOnly date, CancellationToken ct)
    {
        var rows = cachedUsages?[(account, date)].ToArray()
            ?? await db.Set<PlatformAccountDayUsage>().Where(u => u.PlatformRiderAccountId == account && u.BusinessDate == date).ToArrayAsync(ct);
        return rows.Concat(cachedUsages is null ? db.Set<PlatformAccountDayUsage>().Local.Where(u => u.PlatformRiderAccountId == account && u.BusinessDate == date) : [])
            .DistinctBy(u => u.Id).Where(u => !u.IsDeleted).ToArray();
    }

    internal async Task<bool> SequenceAgreesWithSessions(Guid[] ids, CancellationToken ct)
    {
        var sessions = cachedSessions is not null ? ids.SelectMany(id => cachedSessions[id]).ToArray()
            : await db.Set<JahezAccountSession>().AsNoTracking().Where(h => ids.Contains(h.RiderClientAssignmentId)).ToArrayAsync(ct);
        if (sessions.Length != ids.Length) return true;
        var byId = sessions.ToDictionary(h => h.RiderClientAssignmentId);
        return ids.Zip(ids.Skip(1)).All(p => byId[p.First].EndedAtUtc.HasValue
            && byId[p.First].StartedAtUtc <= byId[p.Second].StartedAtUtc
            && byId[p.First].EndedAtUtc <= byId[p.Second].StartedAtUtc);
    }

    internal async Task<Result<RiderClientAssignment[]>> Ordered(Guid account, DateOnly date, CancellationToken ct)
    {
        var candidates = await Candidates(account, date, ct);
        if (candidates.Length == 0) return Result.Failure<RiderClientAssignment[]>(Error(account, date, "No actual rider assignment covers this day."));
        if (candidates.Length == 1) return Result.Success(candidates);
        if (await IsFullTime(account, ct)) return Result.Failure<RiderClientAssignment[]>(Error(account, date, "Chefz FullTime accounts cannot be shared on the same day."));
        // Only an inclusive boundary day may be shared, never several overlapping days.
        for (var i = 0; i < candidates.Length; i++)
            for (var j = i + 1; j < candidates.Length; j++)
            {
                var start = candidates[i].EffectiveFrom > candidates[j].EffectiveFrom ? candidates[i].EffectiveFrom : candidates[j].EffectiveFrom;
                var end = (candidates[i].EffectiveTo ?? DateOnly.MaxValue) < (candidates[j].EffectiveTo ?? DateOnly.MaxValue)
                    ? candidates[i].EffectiveTo ?? DateOnly.MaxValue : candidates[j].EffectiveTo ?? DateOnly.MaxValue;
                if (end > start) return Result.Failure<RiderClientAssignment[]>(Error(account, date, "Assignment periods overlap beyond a boundary day."));
            }
        var usages = (await Usages(account, date, ct)).ToDictionary(u => u.RiderClientAssignmentId);
        if (candidates.All(a => usages.GetValueOrDefault(a.Id)?.Sequence is > 0)
            && candidates.Select(a => usages[a.Id].Sequence).Distinct().Count() == candidates.Length)
        {
            var ordered = candidates.OrderBy(a => usages[a.Id].Sequence).ToArray();
            if (ordered.Zip(ordered.Skip(1)).Any(p => p.First.EffectiveTo != date || p.Second.EffectiveFrom != date))
                return Result.Failure<RiderClientAssignment[]>(Error(account, date, "The sequence contradicts assignment dates."));
            if (!await SequenceAgreesWithSessions(ordered.Select(a => a.Id).ToArray(), ct))
                return Result.Failure<RiderClientAssignment[]>(Error(account, date, "The sequence contradicts Jahez session timestamps."));
            return Result.Success(ordered);
        }
        // Jahez timestamps establish actual order, including several transfers on one day.
        var ids = candidates.Select(a => a.Id).ToArray();
        var sessions = cachedSessions is not null ? ids.SelectMany(id => cachedSessions[id]).ToArray()
            : await db.Set<JahezAccountSession>().Where(h => ids.Contains(h.RiderClientAssignmentId)).ToArrayAsync(ct);
        if (sessions.Length == candidates.Length)
        {
            var ordered = sessions.OrderBy(h => h.StartedAtUtc).ToArray();
            if (ordered.Zip(ordered.Skip(1)).All(p => p.First.EndedAtUtc.HasValue && p.First.EndedAtUtc <= p.Second.StartedAtUtc))
                return Result.Success(ordered.Select(h => candidates.Single(a => a.Id == h.RiderClientAssignmentId)).ToArray());
            return Result.Failure<RiderClientAssignment[]>(Error(account, date, "Jahez session timestamps overlap or do not establish an order."));
        }
        // Dates alone establish order only when every adjacent end/start is a boundary.
        var byDates = candidates.OrderBy(a => a.EffectiveFrom).ThenBy(a => a.EffectiveTo ?? DateOnly.MaxValue).ToArray();
        if (byDates.Length == 2 && byDates[0].EffectiveFrom < byDates[1].EffectiveFrom
            && byDates[0].EffectiveTo == date && byDates[1].EffectiveFrom == date)
            return Result.Success(byDates);
        return Result.Failure<RiderClientAssignment[]>(Error(account, date, "Daily handover order is unresolved; supply the ordered assignment list."));
    }

    public async Task<Result<IReadOnlyList<PlatformDayShare>>> AllocateAsync(Guid accountId, DateOnly date,
        IReadOnlyDictionary<PlatformDayMetric, decimal> totals, CancellationToken ct = default)
    {
        var result = await Ordered(accountId, date, ct);
        if (result.IsFailure) return Result.Failure<IReadOnlyList<PlatformDayShare>>(result.Error);
        var ordered = result.Value!;
        var contributions = await Contributions(accountId, date, ct);
        var covered = contributions.SelectMany(c => Ids(c).SkipLast(1)).ToHashSet();
        ordered = ordered.Where(a => !covered.Contains(a.Id)).ToArray();
        var usages = (await Usages(accountId, date, ct)).ToDictionary(u => u.RiderClientAssignmentId);
        var remaining = totals.ToDictionary();
        var shares = new List<PlatformDayShare>();
        for (var i = 0; i < ordered.Length; i++)
        {
            var assignment = ordered[i];
            var contribution = contributions.SingleOrDefault(c => c.RepresentativeAssignmentId == assignment.Id);
            var figures = contribution is null ? Figures(usages.GetValueOrDefault(assignment.Id))
                : ContributionFigures(contribution, usages);
            var values = new Dictionary<PlatformDayMetric, decimal>();
            foreach (var (metric, total) in totals)
            {
                var entered = Value(figures, metric);
                if (i < ordered.Length - 1 && entered is null)
                    return Result.Failure<IReadOnlyList<PlatformDayShare>>(Error(accountId, date,
                        $"Rider {assignment.RiderProfileId} is missing {metric}; blank is not zero.", assignment.Id, metric.ToString()));
                var value = i == ordered.Length - 1 ? remaining[metric] : entered!.Value;
                // Deductions can be signed in Ninja reports; other metrics cannot.
                if (metric != PlatformDayMetric.DeductionsAmount && (total < 0 || value < 0 || value > remaining[metric])
                    || i == ordered.Length - 1 && entered.HasValue && entered.Value != value)
                    return Result.Failure<IReadOnlyList<PlatformDayShare>>(Error(accountId, date,
                        $"{metric} does not reconcile to the imported total {total}.", assignment.Id, metric.ToString()));
                values[metric] = value; remaining[metric] -= value;
            }
            if (contribution is not null && contribution.FiguresJson is null)
                foreach (var metric in totals.Keys.Where(m => m != PlatformDayMetric.DeductionsAmount))
                    if (Ids(contribution).Sum(id => Value(Figures(usages.GetValueOrDefault(id)), metric) ?? 0) > values[metric])
                        return Result.Failure<IReadOnlyList<PlatformDayShare>>(Error(accountId, date, "Group total contradicts recorded individual figures."));
            shares.Add(new PlatformDayShare(assignment.Id, assignment.RiderProfileId, i + 1, values)
            { ContributionId = contribution?.Id, CoveredAssignmentIds = contribution is null ? [assignment.Id] : Ids(contribution) });
        }
        return Result.Success<IReadOnlyList<PlatformDayShare>>(shares);
    }

    internal Task<bool> IsFullTime(Guid account, CancellationToken ct) => fullTimeAccounts is not null ? Task.FromResult(fullTimeAccounts.Contains(account)) : db.ChefzAccountDetails.AnyAsync(
        d => d.PlatformRiderAccountId == account && d.AccountType == ChefzAccountType.FullTime, ct);

    internal async Task<Result> ValidateEnd(RiderClientAssignment assignment, DateOnly end, bool cancelled, CancellationToken ct)
    {
        var id = assignment.Id;
        if (await db.NinjaDailyPerformance.AnyAsync(p => p.RiderClientAssignmentId == id && (cancelled || p.ReportDate > end), ct)
            || await db.NinjaDailyDeductions.AnyAsync(p => p.RiderClientAssignmentId == id && (cancelled || p.ReportDate > end), ct)
            || await db.ChefzDailyPerformance.AnyAsync(p => p.RiderClientAssignmentId == id && (cancelled || p.ReportDate > end), ct)
            || await db.ChefzWalletSettlements.AnyAsync(p => p.RiderClientAssignmentId == id && (cancelled || p.ChargedTo > end), ct))
            return Result.Failure(Error(assignment.PlatformRiderAccountId, end, "Closing or cancelling would exclude saved performance or settlements.", id));
        return Result.Success();
    }

    internal async Task<HashSet<PlatformDayMetric>> Frozen(Guid account, DateOnly date, CancellationToken ct)
    {
        var frozen = new HashSet<PlatformDayMetric>();
        if (await db.NinjaDailyPerformance.AnyAsync(p => p.PlatformRiderAccountId == account && p.ReportDate == date, ct))
            frozen.UnionWith([PlatformDayMetric.OrdersCount, PlatformDayMetric.WorkingHours]);
        if (await db.NinjaDailyDeductions.AnyAsync(p => p.PlatformRiderAccountId == account && p.ReportDate == date, ct))
            frozen.UnionWith([PlatformDayMetric.CollectedAmount, PlatformDayMetric.DeductionsAmount]);
        if (await db.ChefzDailyPerformance.AnyAsync(p => p.PlatformRiderAccountId == account && p.ReportDate == date, ct))
            frozen.UnionWith([PlatformDayMetric.OrdersCount, PlatformDayMetric.TotalValidOffers, PlatformDayMetric.DeclinedOffers, PlatformDayMetric.MissedOffers]);
        if (await db.Set<ChefzImportedOrderRow>().AnyAsync(p => p.PlatformRiderAccountId == account && p.BusinessDate == date, ct)
            || await db.ChefzWalletSettlements.AnyAsync(s => s.PlatformRiderAccountId == account && s.ChargedFrom <= date && s.ChargedTo >= date, ct)
            || await (from d in db.Set<JahezDailyDispatch>() join h in db.Set<JahezAccountSession>() on d.HandoverId equals h.Id
                where h.PlatformRiderAccountId == account && d.Date == date
                    && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && b.CommittedAtUtc != null) select d.Id).AnyAsync(ct))
            frozen.Add(PlatformDayMetric.OrdersCount);
        return frozen;
    }

    internal async Task<Result> ValidateStart(Guid account, DateOnly from, DateOnly? to, Guid? excluded, CancellationToken ct)
    {
        var others = await db.RiderClientAssignments.Where(a => a.PlatformRiderAccountId == account && a.Id != excluded
            && a.Status != RiderAssignmentStatus.Cancelled && (to == null || a.EffectiveFrom <= to)
            && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArrayAsync(ct);
        var fullTime = await IsFullTime(account, ct);
        if (others.Any(a => fullTime || (a.EffectiveTo ?? DateOnly.MaxValue) > from
            && (to ?? DateOnly.MaxValue) > a.EffectiveFrom))
            return Result.Failure(Error(account, from, "Assignment overlaps another usage period or shares a Chefz FullTime day."));
        if ((await Frozen(account, from, ct)).Count > 0 && others.Length > 0)
            return Result.Failure(Error(account, from, "Saved allocations already cover this boundary day; correct the import before changing assignments."));
        if (to.HasValue && to != from && (await Frozen(account, to.Value, ct)).Count > 0 && others.Length > 0)
            return Result.Failure(Error(account, to.Value, "Saved allocations already cover this boundary day."));
        return Result.Success();
    }

    internal async Task<Result> Stage(Guid account, Guid assignment, DateOnly date,
        PlatformAccountDayUsageInput? input, string reason, bool append, CancellationToken ct)
    {
        if (!Valid(input)) return Result.Failure(Error(account, date, "Invalid handover figures."));
        var usages = await Usages(account, date, ct);
        var usage = usages.SingleOrDefault(u => u.RiderClientAssignmentId == assignment);
        var frozen = await Frozen(account, date, ct);
        if (input is not null && frozen.Any(m => Value(input, m) != Value(Figures(usage), m)))
            return Result.Failure(Error(account, date, "These figures are referenced by a saved allocation; use its correction workflow."));
        if (usage is null)
        {
            var candidates = await Candidates(account, date, ct);
            int? sequence = null;
            if (candidates.Length == 1) sequence = usages.Select(u => u.Sequence ?? 0).DefaultIfEmpty().Max() + 1;
            else if (append && candidates.Where(a => a.Id != assignment).All(a => usages.Any(u => u.RiderClientAssignmentId == a.Id && u.Sequence.HasValue)))
                sequence = (usages.Max(u => u.Sequence) ?? 0) + 1;
            else
            {
                var order = await Ordered(account, date, ct);
                if (order.IsSuccess)
                {
                    for (var i = 0; i < order.Value!.Length; i++)
                    {
                        var a = order.Value[i];
                        var existing = usages.SingleOrDefault(u => u.RiderClientAssignmentId == a.Id);
                        if (existing is null)
                        {
                            existing = new() { PlatformRiderAccountId = account, RiderClientAssignmentId = a.Id,
                                BusinessDate = date, Sequence = i + 1, Reason = reason };
                            db.Add(existing);
                        }
                        if (a.Id == assignment) usage = existing;
                    }
                }
            }
            if (usage is null)
            {
                usage = new() { PlatformRiderAccountId = account, RiderClientAssignmentId = assignment,
                    BusinessDate = date, Sequence = sequence, Reason = reason };
                db.Add(usage);
            }
        }
        if (input is not null) Apply(usage, input);
        usage.Reason = reason;
        return Result.Success();
    }

    internal static void Apply(PlatformAccountDayUsage u, PlatformAccountDayUsageInput input)
    {
        u.OrdersCount = input.OrdersCount; u.WorkingHours = input.WorkingHours;
        u.CollectedAmount = input.CollectedAmount; u.DeductionsAmount = input.DeductionsAmount;
        u.TotalValidOffers = input.TotalValidOffers; u.DeclinedOffers = input.DeclinedOffers; u.MissedOffers = input.MissedOffers;
    }
}
