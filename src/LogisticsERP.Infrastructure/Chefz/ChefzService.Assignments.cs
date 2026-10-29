using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Chefz;

internal sealed partial class ChefzService
{
    public async Task<Result<ChefzAssignmentStartDateResponse>> UpdateAssignmentStartDateAsync(Guid assignmentId,
        ChefzAssignmentStartDateRequest request, CancellationToken ct = default)
    {
        if (!await Allowed(PermissionKeys.Chefz.AccountsUpdate, ct))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Forbidden);
        if (request.EffectiveFrom == default)
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Invalid("تاريخ بداية التكليف مطلوب."));

        var accountId = await db.RiderClientAssignments.AsNoTracking().Where(a => a.Id == assignmentId).Select(a => a.PlatformRiderAccountId).SingleOrDefaultAsync(ct);
        await using var accountScope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var assignment = await (from a in db.RiderClientAssignments
            join account in Accounts on a.PlatformRiderAccountId equals account.Id
            where a.Id == assignmentId select a).SingleOrDefaultAsync(ct);
        if (assignment is null) return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.NotFound);
        if (!HrServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("تم تعديل التكليف؛ أعد تحميله وحاول مجدداً."));
        if (assignment.Status == RiderAssignmentStatus.Cancelled)
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("لا يمكن تعديل تاريخ بداية تكليف ملغى."));
        if (assignment.EffectiveTo is { } end && request.EffectiveFrom > end)
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Invalid("تاريخ البداية يجب ألا يتجاوز تاريخ نهاية التكليف."));
        if (assignment.EffectiveFrom == request.EffectiveFrom)
            return Result.Success(AssignmentStartDateResponse(assignment));

        if (await db.Set<PlatformAccountDayContribution>().AnyAsync(c => c.PlatformRiderAccountId == accountId
            && c.AssignmentIdsJson.Contains(assignmentId.ToString())
            && (c.BusinessDate < request.EffectiveFrom), ct))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("The date change would exclude a saved grouped contribution."));
        var allocator = new PlatformAccountDayAllocationService(db);
        var validDates = await allocator.ValidateStart(accountId, request.EffectiveFrom, assignment.EffectiveTo, assignment.Id, ct);
        if (validDates.IsFailure) return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict(validDates.Error.Description));
        if ((await allocator.Frozen(accountId, assignment.EffectiveFrom, ct)).Count > 0
            || (await allocator.Usages(accountId, assignment.EffectiveFrom, ct)).Any(u => u.RiderClientAssignmentId == assignment.Id &&
                (u.OrdersCount.HasValue || u.WorkingHours.HasValue || u.CollectedAmount.HasValue || u.DeductionsAmount.HasValue
                || u.TotalValidOffers.HasValue || u.DeclinedOffers.HasValue || u.MissedOffers.HasValue)))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("Correct saved usage/imports before moving the assignment start date."));

        if (await db.ChefzDailyPerformance.AnyAsync(p => p.RiderClientAssignmentId == assignmentId
                && p.ReportDate < request.EffectiveFrom, ct)
            || await db.ChefzWalletSettlements.AnyAsync(s => s.RiderClientAssignmentId == assignmentId
                && s.ChargedFrom < request.EffectiveFrom, ct))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("تاريخ البداية يستبعد أداءً أو تصفية محفوظة لهذا التكليف."));

        var oldUsages = await db.Set<LogisticsERP.Domain.Entities.Clients.PlatformAccountDayUsage>().Where(u => u.RiderClientAssignmentId == assignment.Id
            && u.BusinessDate < request.EffectiveFrom).ToArrayAsync(ct);
        if (oldUsages.Any(u => u.OrdersCount.HasValue || u.WorkingHours.HasValue || u.CollectedAmount.HasValue
            || u.DeductionsAmount.HasValue || u.TotalValidOffers.HasValue || u.DeclinedOffers.HasValue || u.MissedOffers.HasValue))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("The new start date would discard recorded day usage; correct it first."));
        foreach (var usage in oldUsages) { usage.IsDeleted = true; usage.Sequence = null; usage.Reason = "Assignment start date corrected"; }
        var previousEffectiveFrom = assignment.EffectiveFrom;
        assignment.EffectiveFrom = request.EffectiveFrom;
        var stagedUsage = await allocator.Stage(accountId, assignment.Id, request.EffectiveFrom, null, "Assignment start date corrected", false, ct);
        if (stagedUsage.IsFailure) { db.ChangeTracker.Clear(); return Result.Failure<ChefzAssignmentStartDateResponse>(stagedUsage.Error); }
        db.RiderAssignmentEvents.Add(new RiderAssignmentEvent
        {
            RiderClientAssignmentId = assignment.Id,
            FromStatus = assignment.Status,
            ToStatus = assignment.Status,
            OccurredAtUtc = clock.GetUtcNow(),
            ActorUserId = user.UserId!.Value,
            Reason = "Assignment start date corrected.",
            ChangeSnapshotJson = JsonSerializer.Serialize(new { PreviousEffectiveFrom = previousEffectiveFrom, assignment.EffectiveFrom })
        });
        var result = await Persist(true, ct);
        return result.IsFailure ? Result.Failure<ChefzAssignmentStartDateResponse>(result.Error)
            : Result.Success(AssignmentStartDateResponse(assignment));
    }

    private static ChefzAssignmentStartDateResponse AssignmentStartDateResponse(RiderClientAssignment assignment) =>
        new(assignment.Id, assignment.PlatformRiderAccountId, assignment.RiderProfileId, assignment.EffectiveFrom,
            assignment.EffectiveTo, assignment.Status.ToString(), Convert.ToBase64String(assignment.RowVersion));
}
