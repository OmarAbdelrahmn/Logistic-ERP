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

        // Assignment dates are inclusive; one account cannot have two riders on the same day.
        if (await db.RiderClientAssignments.AnyAsync(a => a.Id != assignment.Id
            && a.PlatformRiderAccountId == assignment.PlatformRiderAccountId && a.Status != RiderAssignmentStatus.Cancelled
            && (assignment.EffectiveTo == null || a.EffectiveFrom <= assignment.EffectiveTo)
            && (a.EffectiveTo == null || a.EffectiveTo >= request.EffectiveFrom), ct))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("تاريخ البداية يتداخل مع تكليف آخر لنفس الحساب."));

        if (await db.ChefzDailyPerformance.AnyAsync(p => p.RiderClientAssignmentId == assignmentId
                && p.ReportDate < request.EffectiveFrom, ct)
            || await db.ChefzWalletSettlements.AnyAsync(s => s.RiderClientAssignmentId == assignmentId
                && s.ChargedFrom < request.EffectiveFrom, ct))
            return Result.Failure<ChefzAssignmentStartDateResponse>(ChefzErrors.Conflict("تاريخ البداية يستبعد أداءً أو تصفية محفوظة لهذا التكليف."));

        var previousEffectiveFrom = assignment.EffectiveFrom;
        assignment.EffectiveFrom = request.EffectiveFrom;
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
