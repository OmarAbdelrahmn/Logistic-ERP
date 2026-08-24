using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IHrExcelImportService
{
    Task<Result<HrExcelImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public sealed record HrExcelImportResponse(
    bool ValidateOnly,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int CreatedEmployees,
    int UpdatedEmployees,
    int CreatedRiders,
    int CreatedDriverLicenses,
    int CreatedPlatformAccounts,
    int CreatedPlatformAssignments,
    IReadOnlyList<string> ImportedColumns,
    IReadOnlyList<string> IgnoredColumns,
    IReadOnlyList<HrExcelImportIssue> Issues);

public sealed record HrExcelImportIssue(int RowNumber, string? IqamaNo, string Severity, string Message);

public static class HrImportErrors
{
    public static readonly OperationError InvalidWorkbook = new(
        "hr_import.invalid_workbook",
        "The uploaded workbook is invalid or does not contain the required HR headers.",
        ErrorType.Validation);

    public static readonly OperationError ImportFailed = new(
        "hr_import.failed",
        "The HR workbook could not be imported. No partial database changes were committed.",
        ErrorType.Conflict);
}
