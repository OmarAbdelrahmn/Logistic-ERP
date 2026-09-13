using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HrExcelImportService
{
    public async Task<Result<HrPhoneNumberImportResponse>> UpdatePhoneNumbersAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            return Result.Failure<HrPhoneNumberImportResponse>(HrImportErrors.InvalidPhoneNumberWorkbook);
        }

        try
        {
            var workbook = HrPhoneNumberImportParser.Parse(content);
            var issues = workbook.Issues.ToList();
            var iqamas = workbook.Rows.Select(row => row.IqamaNo).ToArray();
            var employees = await dbContext.Employees
                .Where(employee => employee.IqamaNo != null && iqamas.Contains(employee.IqamaNo))
                .ToDictionaryAsync(employee => employee.IqamaNo!, StringComparer.Ordinal, cancellationToken);

            foreach (var row in workbook.Rows)
            {
                if (!employees.ContainsKey(row.IqamaNo))
                {
                    issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "No employee or rider was found for this Iqama number."));
                }
            }

            var canUpdate = !issues.Any(IsError);
            var matchedEmployees = 0;
            var matchedRiders = 0;
            var changedPhoneNumbers = 0;
            var unchangedPhoneNumbers = 0;

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

                if (string.Equals(employee.PrimaryPhone, row.PhoneNumber, StringComparison.Ordinal))
                {
                    unchangedPhoneNumbers++;
                    continue;
                }

                changedPhoneNumbers++;
                if (!validateOnly && canUpdate)
                {
                    employee.PrimaryPhone = row.PhoneNumber;
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

            return Result.Success(new HrPhoneNumberImportResponse(
                validateOnly,
                canUpdate,
                !validateOnly && canUpdate,
                workbook.Worksheet,
                workbook.TotalRows,
                workbook.TotalRows - errorRows,
                matchedEmployees,
                matchedRiders,
                changedPhoneNumbers,
                unchangedPhoneNumbers,
                issues.OrderBy(issue => issue.RowNumber).ThenBy(issue => issue.Severity).ToArray()));
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidPhoneNumberWorkbook(logger, fileName, exception);
            return Result.Failure<HrPhoneNumberImportResponse>(HrImportErrors.InvalidPhoneNumberWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogPhoneNumberUpdateFailure(logger, exception);
            return Result.Failure<HrPhoneNumberImportResponse>(HrImportErrors.PhoneNumberUpdateFailed);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HR phone-number workbook {FileName}")]
    private static partial void LogInvalidPhoneNumberWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "HR phone-number workbook database write failed")]
    private static partial void LogPhoneNumberUpdateFailure(ILogger logger, Exception exception);
}
