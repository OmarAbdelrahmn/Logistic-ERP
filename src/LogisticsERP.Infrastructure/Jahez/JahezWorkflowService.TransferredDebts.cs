using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    public Task<Result<JahezPage<JahezTransferredDebtResponse>>> TransferredDebtsAsync(
        int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            return new JahezPage<JahezTransferredDebtResponse>(
                await WorkflowTransferredDebts(page, pageSize).ToArrayAsync(ct), page, pageSize);
        }, ct);

    public Task<Result<JahezPage<JahezRiderTransferredDebtSummary>>> RidersWithTransferredDebtsAsync(
        int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            return new JahezPage<JahezRiderTransferredDebtSummary>(
                await WorkflowRiderDebtSummaries(page, pageSize).ToArrayAsync(ct), page, pageSize);
        }, ct);

    private IQueryable<JahezRiderTransferredDebtSummary> WorkflowRiderDebtSummaries(int page, int pageSize)
    {
        var obligations = from debt in db.JahezTransferredDebts.IgnoreQueryFilters().AsNoTracking()
            where !debt.IsDeleted && debt.PaidAtUtc == null
            join operation in db.JahezOperations on debt.OperationId equals operation.Id into operations
            from operation in operations.DefaultIfEmpty()
            select new { debt.RiderProfileId, debt.Amount,
                DashboardDebt = operation == null ? (decimal?)null : operation.Net,
                Commission = operation == null ? (decimal?)null : operation.Commission };
        var summaries = obligations.GroupBy(x => x.RiderProfileId)
            .Select(g => new { RiderId = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount),
                Missing = g.Count(x => x.DashboardDebt == null),
                DashboardDebt = g.Sum(x => x.DashboardDebt ?? 0), Commission = g.Sum(x => x.Commission ?? 0) })
            .OrderBy(x => x.RiderId).Skip((page - 1) * pageSize).Take(pageSize);
        return from summary in summaries
            join rider in db.RiderProfiles on summary.RiderId equals rider.Id into riders
            from rider in riders.DefaultIfEmpty()
            join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            orderby summary.RiderId
            select new JahezRiderTransferredDebtSummary(summary.RiderId,
                employee == null ? null : new JahezAccountPerson(employee.Id, rider.Id, employee.FullNameAr,
                    employee.FullNameEn, employee.IqamaNo, employee.PrimaryPhone, employee.SponsorId),
                summary.Count, summary.Total, summary.Missing > 0 ? null
                    : new JahezSettlementAmounts(summary.DashboardDebt, summary.Commission, summary.Total));
    }

    public Task<Result<JahezPage<JahezTransferredDebtResponse>>> RiderTransferredDebtsAsync(
        Guid riderId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            return new JahezPage<JahezTransferredDebtResponse>(
                await WorkflowTransferredDebts(page, pageSize, riderId).ToArrayAsync(ct), page, pageSize);
        }, ct);

    public Task<Result<JahezRiderDebtRepaymentResponse>> RepayRiderTransferredDebtsAsync(
        string key, Guid riderId, JahezRiderDebtRepayment request, CancellationToken ct = default) =>
        ExecuteAsync(key, "v2.rider-debts.repay", new { riderId, request }, JahezWorkflowPermissions.Repay, async () =>
        {
            await AuthorizeAsync(JahezWorkflowPermissions.Receive, ct);
            Reason(request.Reason);
            ValidateMoneyReference(request.Reference);
            WorkflowMoney(request.ExpectedAmount, true);
            var debts = await db.JahezTransferredDebts.Where(x => x.RiderProfileId == riderId && x.PaidAtUtc == null)
                .OrderBy(x => x.Id).ToArrayAsync(ct);
            Require(debts.Length > 0, JahezErrors.Conflict("لا توجد مديونيات مرحلة غير مسددة لهذا المندوب."));
            var total = debts.Sum(x => x.Amount);
            Require(request.ExpectedAmount == total, JahezErrors.Conflict("تغير إجمالي المديونية؛ أعد تحميل المبالغ قبل السداد."));
            foreach (var debt in debts)
                await RepayWorkflowDebt(debt, new(request.Fund, request.Reference, request.Reason), ct);
            await WorkflowDebtNotifications(riderId, debts[0].Id, true, ct);
            return new JahezRiderDebtRepaymentResponse(riderId, total, debts.Select(x => x.Id).ToArray(), 0);
        }, ct);

    private IQueryable<JahezTransferredDebtResponse> WorkflowTransferredDebts(int page, int pageSize, Guid? riderId = null)
    {
        // Historical references may be archived. Ignore soft-delete filters for the joins,
        // but explicitly exclude deleted debts and paginate before enriching each row.
        var debts = db.JahezTransferredDebts.IgnoreQueryFilters().AsNoTracking()
            .Where(d => !d.IsDeleted && (!riderId.HasValue || d.RiderProfileId == riderId && d.PaidAtUtc == null))
            .OrderByDescending(d => d.CreatedAtUtc).ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize).Take(pageSize);
        return from debt in debts
                join rider in db.RiderProfiles on debt.RiderProfileId equals rider.Id into riders
                from rider in riders.DefaultIfEmpty()
                join employee in db.Employees on rider.EmployeeId equals employee.Id into employees
                from employee in employees.DefaultIfEmpty()
                join operation in db.JahezOperations on debt.OperationId equals operation.Id into operations
                from operation in operations.DefaultIfEmpty()
                join handover in db.JahezAccountSessions on operation.HandoverId equals handover.Id into handovers
                from handover in handovers.DefaultIfEmpty()
                join assignment in db.RiderClientAssignments on handover.RiderClientAssignmentId equals assignment.Id into assignments
                from assignment in assignments.DefaultIfEmpty()
                join account in db.PlatformRiderAccounts on handover.PlatformRiderAccountId equals account.Id into accounts
                from account in accounts.DefaultIfEmpty()
                orderby debt.CreatedAtUtc descending, debt.Id
                select new JahezTransferredDebtResponse(debt.Id, debt.OperationId, debt.RiderProfileId,
                    debt.Amount, debt.PaidAtUtc, debt.PaidByUserId, debt.CreatedAtUtc, debt.CreatedByUserId,
                    debt.UpdatedAtUtc, debt.UpdatedByUserId, debt.RowVersion, debt.IsDeleted,
                    debt.DeletedAtUtc, debt.DeletedByUserId, debt.DeletionReason,
                    employee == null ? null : new JahezAccountPerson(employee.Id, rider.Id, employee.FullNameAr,
                        employee.FullNameEn, employee.IqamaNo, employee.PrimaryPhone, employee.SponsorId),
                    handover == null ? null : new JahezTransferredDebtAssignment(handover.RiderClientAssignmentId,
                        handover.Id, handover.PlatformRiderAccountId, account == null ? null : account.ExternalAccountId,
                        handover.RiderProfileId, handover.StartedAtUtc, handover.EndedAtUtc,
                        assignment == null ? null : assignment.EffectiveFrom,
                        assignment == null ? null : assignment.EffectiveTo,
                        assignment == null ? null : assignment.Status.ToString(),
                        assignment == null ? null : assignment.StartReason,
                        assignment == null ? null : assignment.EndReason),
                    operation == null ? null : new JahezTransferredDebtSourceOperation(operation.Id, operation.Mode,
                        operation.ThroughDate, operation.EffectiveAtUtc, operation.Net, operation.Commission,
                        operation.Remaining, operation.Reason, operation.ApprovedRequestId, operation.ExecutedAtUtc));
    }
}
