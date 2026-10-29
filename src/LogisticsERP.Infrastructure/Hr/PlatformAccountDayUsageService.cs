using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class PlatformAccountDayUsageService(ApplicationDbContext db, ICurrentUser user,
    IPermissionChecker permissions, TimeProvider clock) : IPlatformAccountDayUsageService
{
    public async Task<Result<PlatformAssignmentContextResponse>> AssignmentContextAsync(Guid accountId, DateOnly date, Guid riderProfileId, CancellationToken ct = default)
    {
        if (await Access(accountId, date, false, ct) is { } error) return Result.Failure<PlatformAssignmentContextResponse>(error);
        if (riderProfileId == Guid.Empty) return Result.Failure<PlatformAssignmentContextResponse>(HrErrors.InvalidRequest);
        return await Allocation.Context(accountId, date, riderProfileId, ct);
    }
    private PlatformAccountDayAllocationService Allocation => new(db);
    private async Task<OperationError?> Access(Guid accountId, DateOnly date, bool write, CancellationToken ct)
    {
        if (date == default || date > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime))
            return new("platform.day_date", "Specify a valid Riyadh business date.", ErrorType.Validation);
        var account = await (from a in db.PlatformRiderAccounts join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
            where a.Id == accountId select new { a.ClientPlatformId, p.Code }).SingleOrDefaultAsync(ct);
        if (account is null) return HrErrors.NotFound;
        if (user.UserId is not { } id || user.AuthorizationVersion is not { } version) return HrErrors.Forbidden;
        var keys = account.Code switch
        {
            "JAHEZ" => write ? new[] { PermissionKeys.Jahez.HandoversUpdate, LogisticsERP.Application.Features.Jahez.JahezWorkflowPermissions.Withdraw }
                : [PermissionKeys.Jahez.Read],
            "NINJA" => write ? new[] { PermissionKeys.Ninja.AssignmentsDelete, PermissionKeys.Ninja.AccountsUpdate } : [PermissionKeys.Ninja.Read],
            _ => write ? new[] { PermissionKeys.Operations.PlatformAssignmentsUpdate } : [PermissionKeys.Operations.PlatformAssignmentsRead]
        };
        foreach (var key in keys)
            if (!await permissions.HasPermissionAsync(id, version, key, new(AccessScopeType.ClientPlatform, account.ClientPlatformId), ct)) return HrErrors.Forbidden;
        return null;
    }

    public async Task<Result<PlatformAccountDayResponse>> GetAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        if (await Access(accountId, date, false, ct) is { } error) return Result.Failure<PlatformAccountDayResponse>(error);
        return Result.Success(await Response(accountId, date, ct));
    }

    private async Task<PlatformAccountDayResponse> Response(Guid accountId, DateOnly date, CancellationToken ct)
    {
        var assignments = await Allocation.Candidates(accountId, date, ct);
        var usages = (await Allocation.Usages(accountId, date, ct)).ToDictionary(u => u.RiderClientAssignmentId);
        var order = await Allocation.Ordered(accountId, date, ct);
        var platformCode = await (from a in db.PlatformRiderAccounts join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
            where a.Id == accountId select p.Code).SingleAsync(ct);
        var relevantMetrics = platformCode switch
        {
            "NINJA" => new[] { PlatformDayMetric.OrdersCount, PlatformDayMetric.WorkingHours, PlatformDayMetric.CollectedAmount, PlatformDayMetric.DeductionsAmount },
            "CHEFZ" or "THECHEFZ" or "CHEFSZ" or "SHIFTZ" => [PlatformDayMetric.OrdersCount, PlatformDayMetric.TotalValidOffers, PlatformDayMetric.DeclinedOffers, PlatformDayMetric.MissedOffers],
            _ => [PlatformDayMetric.OrdersCount]
        };
        var orderedIds = order.IsSuccess ? order.Value!.Select((a, i) => (a.Id, Sequence: i + 1)).ToDictionary(x => x.Id, x => x.Sequence) : [];
        var response = new PlatformAccountDayResponse(accountId, date, assignments.Select(a =>
        {
            var usage = usages.GetValueOrDefault(a.Id); var figures = PlatformAccountDayAllocationService.Figures(usage);
            return new PlatformAccountDayParticipant(a.Id, a.RiderProfileId, orderedIds.TryGetValue(a.Id, out var sequence) ? sequence : null,
                figures, usage is null ? null : Convert.ToBase64String(usage.RowVersion),
                orderedIds.TryGetValue(a.Id, out var position) && position == assignments.Length ? []
                    : relevantMetrics.Where(m => PlatformAccountDayAllocationService.Value(figures, m) is null).Select(m => m.ToString())
                        .Concat(order.IsFailure ? ["Sequence"] : Array.Empty<string>()).ToArray());
        }).OrderBy(a => a.Sequence).ThenBy(a => a.AssignmentId).ToArray());
        if (order.IsSuccess)
        {
            var context = await Allocation.Context(accountId, date, order.Value![^1].RiderProfileId, ct);
            if (context.IsSuccess) response = response with { Contributions = context.Value!.Contributions };
        }
        return response;
    }

    public async Task<Result<PlatformAccountDayResponse>> UpdateAsync(Guid accountId, DateOnly date,
        PlatformAccountDayUpdateRequest request, CancellationToken ct = default)
    {
        if (await Access(accountId, date, true, ct) is { } error) return Result.Failure<PlatformAccountDayResponse>(error);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 2000 || request.Assignments is null
            || request.Assignments.Count is 0 or > 100 || request.Assignments.Any(a => !PlatformAccountDayAllocationService.Valid(a.Figures)))
            return Result.Failure<PlatformAccountDayResponse>(HrErrors.InvalidRequest);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                // A retry must reload rowversions and figures after the previous transaction rolled back.
                db.ChangeTracker.Clear();
                return await UpdateCoreAsync(accountId, date, request, ct);
            });
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure<PlatformAccountDayResponse>(HrErrors.ConcurrencyConflict);
        }
    }

    private async Task<Result<PlatformAccountDayResponse>> UpdateCoreAsync(Guid accountId, DateOnly date,
        PlatformAccountDayUpdateRequest request, CancellationToken ct)
    {
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var candidates = await Allocation.Candidates(accountId, date, ct);
        var ids = request.Assignments.Select(a => a.AssignmentId).ToArray();
        if (ids.Distinct().Count() != ids.Length || !ids.Order().SequenceEqual(candidates.Select(a => a.Id).Order()))
            return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Include every eligible assignment exactly once, in handover order."));
        if (candidates.Length > 1 && await Allocation.IsFullTime(accountId, ct))
            return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Chefz FullTime accounts cannot be shared."));
        var byId = candidates.ToDictionary(a => a.Id);
        for (var i = 0; i < ids.Length - 1; i++)
            if (byId[ids[i]].EffectiveTo != date || byId[ids[i + 1]].EffectiveFrom != date)
                return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Only a boundary day can be shared; the sequence must agree with assignment dates."));
        if (!await Allocation.SequenceAgreesWithSessions(ids, ct))
            return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "The requested order contradicts Jahez session timestamps."));
        var usages = (await Allocation.Usages(accountId, date, ct)).ToDictionary(u => u.RiderClientAssignmentId);
        foreach (var contribution in await Allocation.Contributions(accountId, date, ct))
        {
            var members = PlatformAccountDayAllocationService.Ids(contribution);
            var position = Array.IndexOf(ids, members[0]);
            if (position < 0 || !ids.Skip(position).Take(members.Length).SequenceEqual(members))
                return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Saved group membership and order cannot be changed."));
        }
        var frozen = await Allocation.Frozen(accountId, date, ct);
        if (frozen.Count > 0)
        {
            var existingOrder = await Allocation.Ordered(accountId, date, ct);
            if (existingOrder.IsFailure || !ids.SequenceEqual(existingOrder.Value!.Select(a => a.Id)))
                return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Saved allocation sequence cannot be changed."));
        }
        var groupedIds = (await Allocation.Contributions(accountId, date, ct)).SelectMany(PlatformAccountDayAllocationService.Ids).ToHashSet();
        foreach (var change in request.Assignments)
        {
            if (groupedIds.Contains(change.AssignmentId) && change.Figures is not null
                && change.Figures != PlatformAccountDayAllocationService.Figures(usages.GetValueOrDefault(change.AssignmentId)))
                return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date, "Correct the grouped contribution instead of individual figures."));
            var usage = usages.GetValueOrDefault(change.AssignmentId);
            if (usage is null ? change.RowVersion is not null : !HrServiceSupport.MatchesRowVersion(usage.RowVersion, change.RowVersion ?? ""))
                return Result.Failure<PlatformAccountDayResponse>(HrErrors.ConcurrencyConflict);
            if (change.Figures is not null && frozen.Any(m => PlatformAccountDayAllocationService.Value(change.Figures, m)
                != PlatformAccountDayAllocationService.Value(PlatformAccountDayAllocationService.Figures(usage), m)))
                return Result.Failure<PlatformAccountDayResponse>(PlatformAccountDayAllocationService.Error(accountId, date,
                    "Saved allocations reference these figures; use their correction workflow.", change.AssignmentId));
        }
        if (request.Contributions is { Count: > 0 })
        {
            // The last existing rider makes only earlier contributions mandatory.
            var orderedContext = await Allocation.Ordered(accountId, date, ct);
            if (orderedContext.IsFailure) return Result.Failure<PlatformAccountDayResponse>(orderedContext.Error);
            var corrected = await Allocation.PrepareAssignment(accountId, date, orderedContext.Value![^1].RiderProfileId,
                request.Contributions, request.Reason, ct, true);
            if (corrected.IsFailure) { db.ChangeTracker.Clear(); return Result.Failure<PlatformAccountDayResponse>(corrected.Error); }
        }
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        // Release unique sequence slots inside the transaction before applying a reordered list.
        foreach (var usage in usages.Values) { usage.Sequence = null; usage.Reason = request.Reason; }
        if (usages.Count > 0) await db.SaveChangesAsync(ct);
        for (var i = 0; i < ids.Length; i++)
        {
            var change = request.Assignments[i];
            if (!usages.TryGetValue(change.AssignmentId, out var usage))
            {
                usage = new() { PlatformRiderAccountId = accountId, RiderClientAssignmentId = change.AssignmentId, BusinessDate = date };
                db.Add(usage);
            }
            usage.Sequence = i + 1; usage.Reason = request.Reason;
            if (change.Figures is not null) PlatformAccountDayAllocationService.Apply(usage, change.Figures);
        }
        await db.SaveChangesAsync(ct);
        var response = await Response(accountId, date, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return Result.Success(response);
    }
}
