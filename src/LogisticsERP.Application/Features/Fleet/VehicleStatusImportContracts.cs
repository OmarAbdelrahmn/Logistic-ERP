using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public interface IVehicleStatusImportService
{
    Task<Result<VehicleStatusImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default);
}

public sealed record VehicleStatusImportResponse(
    bool ValidateOnly,
    bool CanUpdate,
    bool Updated,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int MatchedVehicles,
    int ChangedVehicles,
    int UnchangedVehicles,
    IReadOnlyList<VehicleStatusImportRowPreview> Rows,
    IReadOnlyList<VehicleStatusImportIssue> Issues);

public sealed record VehicleStatusImportRowPreview(
    int RowNumber,
    Guid VehicleId,
    string SerialNumber,
    string AssetNumber,
    VehicleOperationalStatus CurrentStatus,
    VehicleOperationalStatus ImportedStatus,
    string SourceStatus,
    string? IssueNote,
    bool WillChange);

public sealed record VehicleStatusImportIssue(
    int RowNumber, string? SerialNumber, string Severity, string Field, string Message);

public static class VehicleStatusImportErrors
{
    public static readonly OperationError InvalidWorkbook = new(
        "fleet.vehicle_status_import.invalid_workbook",
        "ملف حالات المركبات غير صالح أو لا يحتوي على عمودي الرقم التسلسلي والحالة.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError ImportFailed = new(
        "fleet.vehicle_status_import.failed",
        "تعذر تحديث حالات المركبات ولم يتم حفظ أي جزء من الملف.",
        ErrorType.Conflict,
        "file");
}
