using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<JahezPage<JahezApprovalRequestResponse>>> RequestsV2Async(
        int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            var requests = db.JahezApprovalRequests.Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize);
            return new JahezPage<JahezApprovalRequestResponse>(
                await WorkflowApprovalRequests(requests).ToArrayAsync(ct), page, pageSize);
        }, ct);

    public Task<Result<JahezWorkflowApprovalDetails>> RequestV2Async(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            var request = await WorkflowApprovalRequests(db.JahezApprovalRequests.Where(x => x.Id == id && !x.IsDeleted))
                .SingleOrDefaultAsync(ct);
            Require(request is not null, JahezErrors.NotFound);
            var decisions = await db.Set<JahezApprovalDecision>().AsNoTracking().Where(x => x.RequestId == id)
                .OrderBy(x => x.DecidedAtUtc).ThenBy(x => x.Id).ToArrayAsync(ct);
            return new JahezWorkflowApprovalDetails(request!, decisions);
        }, ct);

    private IQueryable<JahezApprovalRequestResponse> WorkflowApprovalRequests(IQueryable<JahezApprovalRequest> requests)
    {
        // Follow the recorded handover, including archived references. Callers explicitly exclude
        // deleted requests before pagination because IgnoreQueryFilters applies to the whole query.
        return from request in requests.IgnoreQueryFilters().AsNoTracking()
            join handover in db.JahezAccountSessions on request.HandoverId equals handover.Id into handovers
            from handover in handovers.DefaultIfEmpty()
            join rider in db.RiderProfiles on handover.RiderProfileId equals rider.Id into riders
            from rider in riders.DefaultIfEmpty()
            join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            join assignment in db.RiderClientAssignments on handover.RiderClientAssignmentId equals assignment.Id into assignments
            from assignment in assignments.DefaultIfEmpty()
            join account in db.PlatformRiderAccounts on handover.PlatformRiderAccountId equals account.Id into accounts
            from account in accounts.DefaultIfEmpty()
            join city in db.OperatingCities on account.OperatingCityId equals city.Id into cities
            from city in cities.DefaultIfEmpty()
            join globalCity in db.GlobalCities on city.GlobalCityId equals globalCity.Id into globalCities
            from globalCity in globalCities.DefaultIfEmpty()
            join sponsor in db.Sponsors on account.SponsorId equals sponsor.Id into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            join target in db.PlatformRiderAccounts on request.TargetAccountId equals target.Id into targets
            from target in targets.DefaultIfEmpty()
            join targetCity in db.OperatingCities on target.OperatingCityId equals targetCity.Id into targetCities
            from targetCity in targetCities.DefaultIfEmpty()
            join targetGlobalCity in db.GlobalCities on targetCity.GlobalCityId equals targetGlobalCity.Id into targetGlobalCities
            from targetGlobalCity in targetGlobalCities.DefaultIfEmpty()
            join targetSponsor in db.Sponsors on target.SponsorId equals targetSponsor.Id into targetSponsors
            from targetSponsor in targetSponsors.DefaultIfEmpty()
            join operation in db.JahezOperations on request.OperationId equals operation.Id into operations
            from operation in operations.DefaultIfEmpty()
            orderby request.CreatedAtUtc descending, request.Id
            select new JahezApprovalRequestResponse(request.Id, request.HandoverId, request.Kind,
                request.Status, request.RequestedByUserId, request.OperationId, request.TargetAccountId,
                request.WaiverAmount, request.FromDate, request.ToDate, request.EffectiveAtUtc,
                request.Reason, request.AdjustmentTarget, request.ProposedNet, request.ProposedCommission,
                request.DebtAdjustment, request.CreatedAtUtc, request.CreatedByUserId,
                request.UpdatedAtUtc, request.UpdatedByUserId, request.RowVersion, request.IsDeleted,
                request.DeletedAtUtc, request.DeletedByUserId, request.DeletionReason,
                employee == null ? null : new JahezAccountPerson(employee.Id, rider.Id, employee.FullNameAr,
                    employee.FullNameEn, employee.IqamaNo, employee.PrimaryPhone, employee.SponsorId),
                handover == null ? null : new JahezApprovalAssignment(handover.RiderClientAssignmentId,
                    handover.Id, handover.PlatformRiderAccountId, handover.RiderProfileId,
                    handover.StartedAtUtc, handover.EndedAtUtc,
                    assignment == null ? null : assignment.EffectiveFrom,
                    assignment == null ? null : assignment.EffectiveTo,
                    assignment == null ? null : assignment.Status.ToString(),
                    assignment == null ? null : assignment.StartReason,
                    assignment == null ? null : assignment.EndReason,
                    handover.CommissionStartsOn, handover.CommissionPostedThrough, handover.LastSettlementPaymentAtUtc),
                account == null ? null : new JahezApprovalAccount(account.Id, account.Code, account.ExternalAccountId,
                    account.UserName, account.Status.ToString(), account.StatusReason, account.OperatingCityId,
                    globalCity == null ? null : globalCity.NameAr, account.SponsorId,
                    sponsor == null ? null : sponsor.RegistryNameAr),
                target == null ? null : new JahezApprovalAccount(target.Id, target.Code, target.ExternalAccountId,
                    target.UserName, target.Status.ToString(), target.StatusReason, target.OperatingCityId,
                    targetGlobalCity == null ? null : targetGlobalCity.NameAr, target.SponsorId,
                    targetSponsor == null ? null : targetSponsor.RegistryNameAr),
                operation);
    }
}
