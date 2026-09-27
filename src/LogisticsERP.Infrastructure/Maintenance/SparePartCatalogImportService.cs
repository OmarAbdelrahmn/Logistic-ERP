using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class SparePartCatalogImportService(
    ApplicationDbContext dbContext,
    ILogger<SparePartCatalogImportService> logger) : ISparePartCatalogImportService
{
    public async Task<Result<SparePartCatalogImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<SparePartCatalogImportResponse>(SparePartCatalogImportErrors.InvalidWorkbook);

        ParsedSparePartWorkbook workbook;
        try
        {
            workbook = SparePartCatalogSpreadsheetParser.Parse(content);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<SparePartCatalogImportResponse>(SparePartCatalogImportErrors.InvalidWorkbook);
        }

        var issues = workbook.Issues.ToList();
        var existing = await dbContext.InventoryItems.AsNoTracking()
            .Select(item => new { item.NameAr, item.ItemType, item.CompatibleVehicleTypesMask,
                item.Sku, item.NormalizedSku })
            .ToArrayAsync(cancellationToken);
        var existingNames = existing.Where(item => item.ItemType == InventoryItemType.SparePart)
            .GroupBy(item => (SparePartCatalogSpreadsheetParser.NormalizeName(item.NameAr),
                item.CompatibleVehicleTypesMask))
            .ToDictionary(group => group.Key, group => group.First().Sku);
        var existingSkus = existing.Select(item => item.NormalizedSku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = new List<SparePartCatalogImportRow>();
        var toCreate = new List<InventoryItem>();
        foreach (var row in workbook.Rows)
        {
            var vehicleMask = InventoryItemVehicleCompatibility.For(row.VehicleType);
            if (existingNames.TryGetValue((SparePartCatalogSpreadsheetParser.NormalizeName(row.NameAr), vehicleMask),
                    out var existingSku))
            {
                rows.Add(new(row.RowNumber, row.NameAr, row.VehicleType, existingSku, "AlreadyExists"));
                continue;
            }

            var sku = NewSku(row.NameAr, row.VehicleType);
            if (!existingSkus.Add(sku))
            {
                issues.Add(new(row.RowNumber, row.NameAr, "nameAr",
                    "يوجد صنف آخر بنفس رمز الصنف المُنشأ؛ راجع اسم الصنف وتصنيفه."));
                continue;
            }
            toCreate.Add(new InventoryItem
            {
                Sku = sku,
                NormalizedSku = sku,
                ItemType = InventoryItemType.SparePart,
                CompatibleVehicleTypesMask = vehicleMask,
                NameAr = row.NameAr,
                NameEn = row.NameAr,
                BaseUnitOfMeasure = InventoryUnitOfMeasure.Piece,
                PurchaseUnitOfMeasure = InventoryUnitOfMeasure.Piece,
                Status = CatalogStatus.Active
            });
            rows.Add(new(row.RowNumber, row.NameAr, row.VehicleType, sku,
                validateOnly ? "WouldCreate" : "Created"));
        }

        if (issues.Count > 0)
            return Result.Success(new SparePartCatalogImportResponse(
                validateOnly, false, false, workbook.Worksheet, workbook.TotalRows, 0, 0,
                rows.Count(row => row.Action == "AlreadyExists"),
                rows.Select(row => row.Action is "Created" or "WouldCreate"
                        ? row with { Action = "NotImported" } : row)
                    .OrderBy(row => row.RowNumber).ToArray(),
                issues.OrderBy(issue => issue.RowNumber).ToArray()));

        if (validateOnly)
            return Result.Success(new SparePartCatalogImportResponse(
                true, true, false, workbook.Worksheet, workbook.TotalRows, toCreate.Count, 0,
                rows.Count(row => row.Action == "AlreadyExists"),
                rows.OrderBy(row => row.RowNumber).ToArray(), []));

        try
        {
            dbContext.InventoryItems.AddRange(toCreate);
            if (toCreate.Count > 0)
                await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            LogImportFailure(logger, fileName, exception);
            return Result.Failure<SparePartCatalogImportResponse>(SparePartCatalogImportErrors.ImportFailed);
        }

        return Result.Success(new SparePartCatalogImportResponse(
            false, true, true, workbook.Worksheet, workbook.TotalRows, toCreate.Count, toCreate.Count,
            rows.Count(row => row.Action == "AlreadyExists"),
            rows.OrderBy(row => row.RowNumber).ToArray(), []));
    }

    private static string NewSku(string name, VehicleType vehicleType)
    {
        var key = $"{(int)vehicleType}:{SparePartCatalogSpreadsheetParser.NormalizeName(name)}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..24];
        return $"SP-{(vehicleType == VehicleType.Car ? "C" : "M")}-{hash}";
    }

    [LoggerMessage(LogLevel.Warning, "Spare part workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Spare part workbook {FileName} failed during catalog import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);
}

internal sealed record ParsedSparePartRow(int RowNumber, string NameAr, VehicleType VehicleType);

internal sealed record ParsedSparePartWorkbook(
    string Worksheet, int TotalRows, IReadOnlyList<ParsedSparePartRow> Rows,
    IReadOnlyList<SparePartCatalogImportIssue> Issues);

internal static class SparePartCatalogSpreadsheetParser
{
    private const string Name = "nameAr";
    private const string VehicleTypeField = "vehicleType";
    private static readonly Dictionary<string, string> Headers = new(StringComparer.Ordinal)
    {
        [Key("اسم الصنف / القطعة")] = Name,
        [Key("اسم الصنف")] = Name,
        [Key("اسم القطعة")] = Name,
        [Key("Spare Part Name")] = Name,
        [Key("التصنيف")] = VehicleTypeField,
        [Key("نوع المركبة")] = VehicleTypeField,
        [Key("Vehicle Type")] = VehicleTypeField
    };

    public static ParsedSparePartWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook has no worksheet.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");
        var headerRow = usedRange.FirstRow();
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in headerRow.Cells())
        {
            if (Headers.TryGetValue(Key(cell.GetString()), out var field) && !columns.ContainsKey(field))
                columns.Add(field, cell.Address.ColumnNumber);
        }
        if (!columns.TryGetValue(Name, out var nameColumn)
            || !columns.TryGetValue(VehicleTypeField, out var typeColumn))
            throw new InvalidDataException("The workbook is missing the part-name or vehicle-type column.");

        var rows = new List<ParsedSparePartRow>();
        var issues = new List<SparePartCatalogImportIssue>();
        var seen = new HashSet<(string Name, VehicleType Type)>();
        var totalRows = 0;
        foreach (var row in worksheet.Rows(headerRow.RowNumber() + 1, usedRange.LastRow().RowNumber()))
        {
            var name = CollapseWhitespace(row.Cell(nameColumn).GetString());
            var typeText = CollapseWhitespace(row.Cell(typeColumn).GetString());
            if (name.Length == 0 && typeText.Length == 0) continue;
            totalRows++;
            if (name.Length is 0 or > 200)
            {
                issues.Add(new(row.RowNumber(), name.Length == 0 ? null : name, Name,
                    "اسم الصنف مطلوب ويجب ألا يزيد عن 200 حرف."));
                continue;
            }
            var vehicleType = Key(typeText) switch
            {
                "دباب" or "دراجهناريه" or "motorcycle" => VehicleType.Motorcycle,
                "سياره" or "car" => VehicleType.Car,
                _ => (VehicleType?)null
            };
            if (!vehicleType.HasValue)
            {
                issues.Add(new(row.RowNumber(), name, VehicleTypeField,
                    "التصنيف يجب أن يكون سيارة أو دباب فقط."));
                continue;
            }
            if (!seen.Add((NormalizeName(name), vehicleType.Value)))
            {
                issues.Add(new(row.RowNumber(), name, Name,
                    "اسم الصنف والتصنيف مكرران داخل الملف."));
                continue;
            }
            rows.Add(new(row.RowNumber(), name, vehicleType.Value));
        }
        if (totalRows == 0)
            throw new InvalidDataException("The workbook has no data rows.");
        return new(worksheet.Name, totalRows, rows, issues);
    }

    internal static string NormalizeName(string value) =>
        CollapseWhitespace(value.Normalize(NormalizationForm.FormKC)).ToUpperInvariant();

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Key(string value) =>
        new(value.Normalize(NormalizationForm.FormKC).ToLowerInvariant()
            .Where(char.IsLetterOrDigit).Select(character => character switch
            {
                'أ' or 'إ' or 'آ' => 'ا',
                'ة' => 'ه',
                'ى' => 'ي',
                _ => character
            }).ToArray());
}
