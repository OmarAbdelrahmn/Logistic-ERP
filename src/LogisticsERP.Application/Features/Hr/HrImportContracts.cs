using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public interface IHrExcelImportService
{
    Task<Result<HrExcelImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);

    Task<Result<HrPhoneNumberImportResponse>> UpdatePhoneNumbersAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);

    Task<Result<HrEmployeeStatusImportResponse>> UpdateStatusesAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public interface IExternalRiderImportService
{
    Task<Result<ExternalRiderImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public sealed record ExternalRiderImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int CreatedEmployees,
    int UpdatedEmployees,
    int CreatedRiderProfiles,
    IReadOnlyList<ExternalRiderImportRowPreview> Rows,
    IReadOnlyList<HrExcelImportIssue> Issues);

public sealed record ExternalRiderImportRowPreview(
    int RowNumber,
    string IqamaNo,
    string FullNameAr,
    string? Nationality,
    string PhoneNumber,
    Guid OperatingCityId,
    string OperatingCityNameAr,
    bool WillCreateEmployee,
    bool WillCreateRiderProfile);

public sealed record HrExcelImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int CreatedEmployees,
    int UpdatedEmployees,
    int CreatedRiders,
    int CreatedResidencyDocuments,
    int UpdatedResidencyDocuments,
    int CreatedDriverLicenses,
    int UpdatedDriverLicenses,
    int DefaultedExpiryDates,
    int CreatedPlatformAccounts,
    int CreatedPlatformAssignments,
    IReadOnlyList<string> ImportedColumns,
    IReadOnlyList<string> IgnoredColumns,
    IReadOnlyList<HrExcelImportIssue> Issues);

public sealed record HrExcelImportIssue(int RowNumber, string? IqamaNo, string Severity, string Message);

public sealed record HrPhoneNumberImportResponse(
    bool ValidateOnly,
    bool CanUpdate,
    bool Updated,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int MatchedEmployees,
    int MatchedRiders,
    int ChangedPhoneNumbers,
    int UnchangedPhoneNumbers,
    IReadOnlyList<HrExcelImportIssue> Issues);

public sealed record HrEmployeeStatusImportResponse(
    bool ValidateOnly,
    bool CanUpdate,
    bool Updated,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int MatchedEmployees,
    int MatchedRiders,
    int ChangedStatuses,
    int UnchangedStatuses,
    IReadOnlyList<HrExcelImportIssue> Issues);

public static class HrImportErrors
{
    public static readonly OperationError InvalidWorkbook = new(
        "hr_import.invalid_workbook",
        "ملف الموظفين غير صالح أو لا يحتوي على الأعمدة المطلوبة.",
        ErrorType.Validation);

    public static readonly OperationError ImportFailed = new(
        "hr_import.failed",
        "تعذر استيراد ملف الموظفين. لم تُحفظ أي تغييرات جزئية.",
        ErrorType.Conflict);

    public static readonly OperationError InvalidPhoneNumberWorkbook = new(
        "hr_phone_import.invalid_workbook",
        "ملف أرقام الجوال غير صالح. يجب أن يحتوي العمودان الأول والثاني على رقم الإقامة ورقم الجوال.",
        ErrorType.Validation);

    public static readonly OperationError PhoneNumberUpdateFailed = new(
        "hr_phone_import.failed",
        "تعذر تحديث أرقام جوال الموظفين والسائقين. لم تُحفظ أي تغييرات جزئية.",
        ErrorType.Conflict);

    public static readonly OperationError InvalidEmployeeStatusWorkbook = new(
        "hr_status_import.invalid_workbook",
        "ملف حالات الموظفين غير صالح. يجب أن يحتوي العمودان الأول والثاني على رقم الإقامة ورمز الحالة الرقمي.",
        ErrorType.Validation);

    public static readonly OperationError InvalidExternalRiderWorkbook = new(
        "hr_external_rider_import.invalid_workbook",
        "ملف السائقين الخارجيين غير صالح أو لا يحتوي على الأعمدة المطلوبة.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError ExternalRiderImportFailed = new(
        "hr_external_rider_import.failed",
        "تعذر استيراد السائقين الخارجيين.",
        ErrorType.Conflict,
        "file");

    public static readonly OperationError EmployeeStatusUpdateFailed = new(
        "hr_status_import.failed",
        "تعذر تحديث حالات الموظفين والسائقين. لم تُحفظ أي تغييرات جزئية.",
        ErrorType.Conflict);
}
