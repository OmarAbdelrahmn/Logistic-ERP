using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaAssignmentDatesResponse>> UpdateAssignmentDatesAsync(Guid accountId, Guid assignmentId,
        NinjaAssignmentDatesRequest request, CancellationToken ct = default)
    {
        try
        {
            await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
            var account = await Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
            if (account is null) return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.NotFound);
            if (!await Allowed(PermissionKeys.Ninja.AssignmentDatesUpdate, account.ClientPlatformId, ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Forbidden);
            var assignment = await db.RiderClientAssignments.SingleOrDefaultAsync(a => a.Id == assignmentId && a.PlatformRiderAccountId == accountId, ct);
            if (assignment is null) return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentNotFound);
            if (!HrServiceSupport.MatchesRowVersion(assignment.RowVersion, request.RowVersion))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Conflict);
            var current = assignment.Status == RiderAssignmentStatus.Active && assignment.EffectiveTo is null;
            var ended = assignment.Status == RiderAssignmentStatus.Ended && assignment.EffectiveTo is not null;
            if (!current && !ended)
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Invalid("يمكن تعديل تواريخ التكليف الحالي أو المنتهي فقط."));
            if (current && request.EffectiveTo is not null || ended && request.EffectiveTo is null)
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Invalid(
                    "التكليف الحالي يسمح بتعديل تاريخ البداية فقط، والتكليف المنتهي يتطلب تاريخ نهاية."));
            var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
            if (request.EffectiveFrom == default || request.EffectiveFrom > today || request.EffectiveTo > today
                || request.EffectiveTo < request.EffectiveFrom || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Invalid("حدد تواريخ صحيحة حتى اليوم وسبباً لا يتجاوز 1000 حرف."));
            if (request.EffectiveFrom == assignment.EffectiveFrom && request.EffectiveTo == assignment.EffectiveTo)
                return Result.Success(AssignmentDatesResponse(assignment));

            var start = request.EffectiveFrom; var end = request.EffectiveTo ?? DateOnly.MaxValue;
            // Inclusive boundary days can be shared. Overlap beyond a handover boundary is invalid.
            if (await db.RiderClientAssignments.AsNoTracking().AnyAsync(a => a.PlatformRiderAccountId == accountId && a.Id != assignmentId
                && a.Status != RiderAssignmentStatus.Cancelled && a.EffectiveFrom < end && (a.EffectiveTo == null || a.EffectiveTo > start), ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict("التواريخ تتداخل مع تكليف آخر للحساب."));
            if (await db.NinjaDailyPerformance.AnyAsync(p => p.RiderClientAssignmentId == assignmentId && (p.ReportDate < start || p.ReportDate > end), ct)
                || await db.NinjaDailyDeductions.AnyAsync(p => p.RiderClientAssignmentId == assignmentId && (p.ReportDate < start || p.ReportDate > end), ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict("التواريخ الجديدة تستبعد أداءً أو خصومات مسجلة للتكليف."));

            if (await db.Set<PlatformAccountDayContribution>().AnyAsync(c => c.PlatformRiderAccountId == accountId
                && c.AssignmentIdsJson.Contains(assignmentId.ToString())
                && (c.BusinessDate < request.EffectiveFrom || c.BusinessDate > request.EffectiveTo), ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict("The date change would exclude a saved grouped contribution."));
            var oldFrom = assignment.EffectiveFrom; var oldTo = assignment.EffectiveTo ?? DateOnly.MaxValue;
            var observedDates = db.NinjaDailyPerformance.AsNoTracking().Where(p => p.PlatformRiderAccountId == accountId).Select(p => p.ReportDate)
                .Concat(db.NinjaDailyDeductions.AsNoTracking().Where(p => p.PlatformRiderAccountId == accountId).Select(p => p.ReportDate))
                .Concat(db.NinjaDailyPerformanceSources.AsNoTracking().Where(p => p.PlatformRiderAccountId == accountId).Select(p => p.ReportDate))
                .Concat(db.NinjaDailyDeductionSources.AsNoTracking().Where(p => p.PlatformRiderAccountId == accountId).Select(p => p.ReportDate));
            if (await observedDates.AnyAsync(date => (date >= oldFrom && date <= oldTo) != (date >= start && date <= end), ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict(
                    "تغيير التواريخ يغير المشاركين في يوم له بيانات مستوردة؛ صحح الاستيراد أولاً."));
            var responsibility = from line in db.NinjaResponsibilityLines.AsNoTracking()
                                 join revision in db.NinjaResponsibilityRevisions.AsNoTracking() on line.RevisionId equals revision.Id
                                 where line.RiderClientAssignmentId == assignmentId && (line.BusinessDate < start || line.BusinessDate > end)
                                     && !db.NinjaResponsibilityRevisions.Any(r => r.PlatformRiderAccountId == revision.PlatformRiderAccountId
                                         && r.Month == revision.Month && r.Revision > revision.Revision)
                                 select line.Id;
            if (await responsibility.AnyAsync(ct))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict("التواريخ الجديدة تستبعد مسؤولية يومية مراجعة؛ صحح توزيع المسؤولية أولاً."));
            var excludedUsages = await db.PlatformAccountDayUsages.Where(u => u.RiderClientAssignmentId == assignmentId
                && (u.BusinessDate < start || u.BusinessDate > end)).ToArrayAsync(ct);
            if (excludedUsages.Any(u => u.OrdersCount.HasValue || u.WorkingHours.HasValue || u.CollectedAmount.HasValue
                || u.DeductionsAmount.HasValue || u.TotalValidOffers.HasValue || u.DeclinedOffers.HasValue || u.MissedOffers.HasValue))
                return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.AssignmentDatesConflict("التواريخ الجديدة تستبعد أرقام تسليم يومية محفوظة."));
            var boundaryDates = new[] { request.EffectiveFrom, request.EffectiveTo }.OfType<DateOnly>().Distinct().ToArray();
            var existingBoundaries = await db.PlatformAccountDayUsages.Where(u => u.RiderClientAssignmentId == assignmentId
                && boundaryDates.Contains(u.BusinessDate)).Select(u => u.BusinessDate).ToArrayAsync(ct);

            var previousFrom = assignment.EffectiveFrom; var previousTo = assignment.EffectiveTo;
            assignment.EffectiveFrom = request.EffectiveFrom; assignment.EffectiveTo = request.EffectiveTo;
            // Empty markers are operational metadata; retire markers excluded by the corrected period.
            foreach (var usage in excludedUsages) { usage.IsDeleted = true; usage.Sequence = null; usage.DeletionReason = request.Reason.Trim(); }
            foreach (var date in boundaryDates)
            {
                // A new marker has no assumed sequence. Dates resolve ordinary transfers; ambiguous
                // same-day transfers still require the existing day-usage ordering workflow.
                if (!existingBoundaries.Contains(date)) db.PlatformAccountDayUsages.Add(new PlatformAccountDayUsage
                {
                    PlatformRiderAccountId = accountId, RiderClientAssignmentId = assignmentId,
                    BusinessDate = date, Reason = request.Reason.Trim()
                });
            }
            db.RiderAssignmentEvents.Add(new RiderAssignmentEvent
            {
                RiderClientAssignmentId = assignmentId, FromStatus = assignment.Status, ToStatus = assignment.Status,
                OccurredAtUtc = clock.GetUtcNow(), ActorUserId = user.UserId!.Value, Reason = request.Reason.Trim(),
                ChangeSnapshotJson = JsonSerializer.Serialize(new
                {
                    Action = "NinjaAssignmentDatesCorrected", AccountId = accountId,
                    PreviousEffectiveFrom = previousFrom, PreviousEffectiveTo = previousTo,
                    EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo
                })
            });
            await db.SaveChangesAsync(ct);
            return Result.Success(AssignmentDatesResponse(assignment));
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return Result.Failure<NinjaAssignmentDatesResponse>(NinjaErrors.Conflict);
        }
    }

    private static NinjaAssignmentDatesResponse AssignmentDatesResponse(RiderClientAssignment assignment) => new(
        assignment.PlatformRiderAccountId, assignment.Id, assignment.EffectiveFrom, assignment.EffectiveTo,
        assignment.Status.ToString(), Convert.ToBase64String(assignment.RowVersion));
}
