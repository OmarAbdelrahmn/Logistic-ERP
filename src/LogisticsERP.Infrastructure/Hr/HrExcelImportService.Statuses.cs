using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HrExcelImportService
{
    private const string StatusImportReason = "Updated by the employee/rider status Excel import.";

    public async Task<Result<HrEmployeeStatusImportResponse>> UpdateStatusesAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            return Result.Failure<HrEmployeeStatusImportResponse>(HrImportErrors.InvalidEmployeeStatusWorkbook);
        }

        try
        {
            var workbook = HrEmployeeStatusImportParser.Parse(content);
            var issues = workbook.Issues.ToList();
            var iqamas = workbook.Rows.Select(row => row.IqamaNo).ToArray();
            var employees = (await dbContext.Employees
                .IgnoreQueryFilters()
                .Where(employee => employee.IqamaNo != null && iqamas.Contains(employee.IqamaNo))
                .ToArrayAsync(cancellationToken))
                .GroupBy(employee => employee.IqamaNo!, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(employee => employee.IsDeleted).ThenByDescending(employee => employee.CreatedAtUtc).First(),
                    StringComparer.Ordinal);

            var employeeIds = employees.Values.Select(employee => employee.Id).ToArray();
            var riderIdsByEmployeeId = await dbContext.RiderProfiles
                .IgnoreQueryFilters()
                .Where(rider => employeeIds.Contains(rider.EmployeeId))
                .ToDictionaryAsync(rider => rider.EmployeeId, rider => rider.Id, cancellationToken);
            var riderIds = riderIdsByEmployeeId.Values.ToArray();
            var assignedRiderIds = await dbContext.RiderClientAssignments
                .Where(assignment => assignment.EffectiveTo == null && riderIds.Contains(assignment.RiderProfileId))
                .Select(assignment => assignment.RiderProfileId)
                .Concat(dbContext.RiderVehicleAssignments
                    .Where(assignment => assignment.EndedAtUtc == null && riderIds.Contains(assignment.RiderProfileId))
                    .Select(assignment => assignment.RiderProfileId))
                .Distinct()
                .ToHashSetAsync(cancellationToken);

            foreach (var row in workbook.Rows)
            {
                if (!employees.TryGetValue(row.IqamaNo, out var employee))
                {
                    issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "No employee or rider was found for this Iqama number."));
                    continue;
                }

                if (row.Status == EmployeeStatus.Active
                    && (employee.EngagementType == EmployeeRelationshipType.SponsoredInternal && employee.SponsorId is null
                        || !employee.IsEmployee && !riderIdsByEmployeeId.ContainsKey(employee.Id)))
                {
                    issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "This employee or rider does not meet the requirements for Active status."));
                }

                if (row.Status == EmployeeStatus.Archived
                    && riderIdsByEmployeeId.TryGetValue(employee.Id, out var riderId)
                    && assignedRiderIds.Contains(riderId))
                {
                    issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "A rider with an active platform or vehicle assignment cannot be archived."));
                }
            }

            var canUpdate = !issues.Any(IsError);
            var matchedEmployees = 0;
            var matchedRiders = 0;
            var changedStatuses = 0;
            var unchangedStatuses = 0;
            var now = timeProvider.GetUtcNow();
            var effectiveDate = DateOnly.FromDateTime(now.ToOffset(RiyadhOffset).DateTime);

            foreach (var row in workbook.Rows)
            {
                if (!employees.TryGetValue(row.IqamaNo, out var employee))
                {
                    continue;
                }

                if (employee.IsEmployee)
                {
                    matchedEmployees++;
                }
                else
                {
                    matchedRiders++;
                }

                if (employee.Status == row.Status)
                {
                    unchangedStatuses++;
                    continue;
                }

                changedStatuses++;
                if (!validateOnly && canUpdate)
                {
                    ApplyImportedStatus(employee, row.Status, effectiveDate, now);
                }
            }

            if (!validateOnly && canUpdate)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                dbContext.ChangeTracker.Clear();
            }

            var errorRows = issues
                .Where(IsError)
                .Select(issue => issue.RowNumber)
                .Distinct()
                .Count();

            return Result.Success(new HrEmployeeStatusImportResponse(
                validateOnly,
                canUpdate,
                !validateOnly && canUpdate,
                workbook.Worksheet,
                workbook.TotalRows,
                workbook.TotalRows - errorRows,
                matchedEmployees,
                matchedRiders,
                changedStatuses,
                unchangedStatuses,
                issues.OrderBy(issue => issue.RowNumber).ThenBy(issue => issue.Severity).ToArray()));
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidEmployeeStatusWorkbook(logger, fileName, exception);
            return Result.Failure<HrEmployeeStatusImportResponse>(HrImportErrors.InvalidEmployeeStatusWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogEmployeeStatusUpdateFailure(logger, exception);
            return Result.Failure<HrEmployeeStatusImportResponse>(HrImportErrors.EmployeeStatusUpdateFailed);
        }
    }

    private static void ApplyImportedStatus(
        Employee employee,
        EmployeeStatus status,
        DateOnly effectiveDate,
        DateTimeOffset now)
    {
        employee.Status = status;
        employee.StatusReason = status is EmployeeStatus.Suspended or EmployeeStatus.Terminated
            or EmployeeStatus.Fleeing or EmployeeStatus.Accident or EmployeeStatus.Sick
            ? StatusImportReason
            : null;

        if (status == EmployeeStatus.Terminated)
        {
            employee.TerminationDate ??= effectiveDate;
        }

        if (status == EmployeeStatus.Archived)
        {
            employee.IsDeleted = true;
            employee.DeletedAtUtc ??= now;
            employee.DeletionReason = StatusImportReason;
            return;
        }

        employee.IsDeleted = false;
        employee.DeletedAtUtc = null;
        employee.DeletedByUserId = null;
        employee.DeletionReason = null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HR employee-status workbook {FileName}")]
    private static partial void LogInvalidEmployeeStatusWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "HR employee-status workbook database write failed")]
    private static partial void LogEmployeeStatusUpdateFailure(ILogger logger, Exception exception);
}
