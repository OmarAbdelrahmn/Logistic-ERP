using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fleet;

public interface IVehicleImportValidationService
{
    Task<Result<VehicleImportResponse>> ValidateAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}

public interface IVehicleImportService
{
    Task<Result<VehicleImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}

public interface IVehiclePurchaseSupplierImportService
{
    Task<Result<VehiclePurchaseSupplierImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public interface IVehicleRiderAssignmentImportService
{
    Task<Result<VehicleRiderAssignmentImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default);
}

public sealed record VehicleRiderAssignmentImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int CreatedAssignments,
    IReadOnlyList<VehicleRiderAssignmentImportRowPreview> Rows,
    IReadOnlyList<VehicleRiderAssignmentImportIssue> Issues);

public sealed record VehicleRiderAssignmentImportRowPreview(
    int RowNumber,
    Guid VehicleId,
    string SerialNumber,
    string AssetNumber,
    Guid RiderProfileId,
    string RiderIqamaNo,
    string RiderName,
    DateOnly PermissionStartsOn);

public sealed record VehicleRiderAssignmentImportIssue(
    int RowNumber,
    string? SerialNumber,
    string Severity,
    string Field,
    string Message);

public sealed record VehicleImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int CreatedVehicles,
    int CreatedManufacturers,
    int CreatedModels,
    IReadOnlyList<VehicleImportRowPreview> Rows,
    IReadOnlyList<VehicleImportIssue> Issues);

public sealed record VehicleImportRowPreview(
    int RowNumber,
    string PlateNumberAr,
    string PlateNumberEn,
    string RegistrationType,
    string Manufacturer,
    string Model,
    int ModelYear,
    string City,
    string OwnerNumber,
    string OwnerType,
    string? OwnerName,
    string UserNumber,
    string? UserName,
    string OwnershipType);

public sealed record VehicleImportIssue(
    int RowNumber,
    string? PlateNumber,
    string Severity,
    string Field,
    string Message);

public sealed record VehiclePurchaseSupplierImportResponse(
    bool ValidateOnly,
    bool CanUpdate,
    bool Updated,
    string Worksheet,
    int TotalRows,
    int ValidRows,
    int MatchedVehicles,
    int ChangedVehicles,
    int UnchangedVehicles,
    IReadOnlyList<VehiclePurchaseSupplierImportRowPreview> Rows,
    IReadOnlyList<VehiclePurchaseSupplierImportIssue> Issues);

public sealed record VehiclePurchaseSupplierImportRowPreview(
    int RowNumber,
    Guid VehicleId,
    string SerialNumber,
    string AssetNumber,
    string? PlateNumberAr,
    Guid SupplierId,
    string SupplierRegistryNumber,
    string SupplierName,
    bool WillChange);

public sealed record VehiclePurchaseSupplierImportIssue(
    int RowNumber,
    string? SerialNumber,
    string Severity,
    string Field,
    string Message);

public static class VehicleImportErrors
{
    public static readonly OperationError InvalidWorkbook = new(
        "fleet.vehicle_import.invalid_workbook",
        "ملف المركبات غير صالح أو لا يحتوي على الأعمدة العربية المطلوبة.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError ImportFailed = new(
        "fleet.vehicle_import.failed",
        "تعذر استيراد المركبات ولم يتم حفظ أي جزء من الملف.",
        ErrorType.Conflict,
        "file");

    public static readonly OperationError InvalidPurchaseSupplierWorkbook = new(
        "fleet.vehicle_purchase_supplier_import.invalid_workbook",
        "ملف موردي شراء المركبات غير صالح أو لا يحتوي على الرقم التسلسلي ورقم سجل المورد.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError PurchaseSupplierImportFailed = new(
        "fleet.vehicle_purchase_supplier_import.failed",
        "تعذر تحديث موردي شراء المركبات ولم يتم حفظ أي جزء من الملف.",
        ErrorType.Conflict,
        "file");

    public static readonly OperationError InvalidRiderAssignmentWorkbook = new(
        "fleet.vehicle_rider_assignment_import.invalid_workbook",
        "ملف إسناد المركبات للسائقين غير صالح أو لا يحتوي على الأعمدة المطلوبة.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError RiderAssignmentImportFailed = new(
        "fleet.vehicle_rider_assignment_import.failed",
        "تعذر استيراد إسنادات المركبات ولم يتم حفظ أي جزء من الملف.",
        ErrorType.Conflict,
        "file");
}
