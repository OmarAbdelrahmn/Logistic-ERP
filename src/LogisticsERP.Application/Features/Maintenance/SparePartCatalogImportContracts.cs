using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Maintenance;

public interface ISparePartCatalogImportService
{
    Task<Result<SparePartCatalogImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default);
}

public sealed record SparePartCatalogImportResponse(
    bool ValidateOnly,
    bool CanImport,
    bool Imported,
    string Worksheet,
    int TotalRows,
    int WouldCreateItems,
    int CreatedItems,
    int AlreadyExistingItems,
    IReadOnlyList<SparePartCatalogImportRow> Rows,
    IReadOnlyList<SparePartCatalogImportIssue> Issues);

public sealed record SparePartCatalogImportRow(
    int RowNumber, string NameAr, VehicleType VehicleType, string Sku, string Action);

public sealed record SparePartCatalogImportIssue(
    int RowNumber, string? NameAr, string Field, string Message);

public static class SparePartCatalogImportErrors
{
    public static readonly OperationError InvalidWorkbook = new(
        "maintenance.spare_part_import.invalid_workbook",
        "ملف الأصناف غير صالح أو لا يحتوي على أعمدة اسم الصنف والتصنيف.",
        ErrorType.Validation,
        "file");

    public static readonly OperationError ImportFailed = new(
        "maintenance.spare_part_import.failed",
        "تعذر استيراد الأصناف ولم يتم حفظ أي جزء من الملف.",
        ErrorType.Conflict,
        "file");
}
