using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehiclePurchaseSupplierImportService(
    ApplicationDbContext dbContext,
    ILogger<VehiclePurchaseSupplierImportService> logger) : IVehiclePurchaseSupplierImportService
{
    public async Task<Result<VehiclePurchaseSupplierImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            return Result.Failure<VehiclePurchaseSupplierImportResponse>(
                VehicleImportErrors.InvalidPurchaseSupplierWorkbook);
        }

        try
        {
            var workbook = VehiclePurchaseSupplierSpreadsheetParser.Parse(content);
            return await ProcessAsync(workbook, validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehiclePurchaseSupplierImportResponse>(
                VehicleImportErrors.InvalidPurchaseSupplierWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehiclePurchaseSupplierImportResponse>(
                VehicleImportErrors.PurchaseSupplierImportFailed);
        }
    }

    private async Task<Result<VehiclePurchaseSupplierImportResponse>> ProcessAsync(
        ParsedVehiclePurchaseSupplierWorkbook workbook,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var serialNumbers = workbook.Rows.Select(row => row.NormalizedSerialNumber).Distinct(StringComparer.Ordinal).ToArray();
        var registryNumbers = workbook.Rows.Select(row => row.NormalizedSupplierRegistryNumber).Distinct(StringComparer.Ordinal).ToArray();

        Vehicle[] vehicles;
        if (validateOnly)
        {
            vehicles = await dbContext.Vehicles.AsNoTracking()
                .Where(vehicle => vehicle.NormalizedSerialNumber != null
                    && serialNumbers.Contains(vehicle.NormalizedSerialNumber))
                .ToArrayAsync(cancellationToken);
        }
        else
        {
            vehicles = await dbContext.Vehicles
                .Where(vehicle => vehicle.NormalizedSerialNumber != null
                    && serialNumbers.Contains(vehicle.NormalizedSerialNumber))
                .ToArrayAsync(cancellationToken);
        }

        var vehicleBySerial = vehicles
            .GroupBy(vehicle => vehicle.NormalizedSerialNumber!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var suppliers = await dbContext.VehicleSuppliers.AsNoTracking()
            .Where(supplier => supplier.Status == VehicleCatalogStatus.Active
                && supplier.CommercialRegistrationNumber != null)
            .ToArrayAsync(cancellationToken);
        var supplierByRegistry = suppliers
            .Select(supplier => new
            {
                Supplier = supplier,
                Registry = VehiclePurchaseSupplierSpreadsheetParser.NormalizeLookup(supplier.CommercialRegistrationNumber!)
            })
            .Where(item => registryNumbers.Contains(item.Registry))
            .GroupBy(item => item.Registry, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Supplier, StringComparer.Ordinal);

        var previews = new List<VehiclePurchaseSupplierImportRowPreview>();
        var updates = new List<(Vehicle Vehicle, VehicleSupplier Supplier)>();
        var seenSerials = new HashSet<string>(StringComparer.Ordinal);
        var unchanged = 0;
        foreach (var row in workbook.Rows)
        {
            if (!seenSerials.Add(row.NormalizedSerialNumber))
            {
                issues.Add(Error(row, "serialNumber", "الرقم التسلسلي مكرر داخل الملف."));
                continue;
            }
            if (!vehicleBySerial.TryGetValue(row.NormalizedSerialNumber, out var vehicle))
            {
                issues.Add(Error(row, "serialNumber", $"لم يتم العثور على مركبة بالرقم التسلسلي '{row.SerialNumber}'."));
                continue;
            }
            if (!supplierByRegistry.TryGetValue(row.NormalizedSupplierRegistryNumber, out var supplier))
            {
                issues.Add(Error(row, "supplierRegistryNumber", $"لم يتم العثور على مورد فعال برقم السجل '{row.SupplierRegistryNumber}'."));
                continue;
            }

            var willChange = vehicle.PurchasedFromSupplierId != supplier.Id;
            previews.Add(new VehiclePurchaseSupplierImportRowPreview(
                row.RowNumber,
                vehicle.Id,
                vehicle.SerialNumber ?? row.SerialNumber,
                vehicle.AssetNumber,
                vehicle.PlateNumberAr,
                supplier.Id,
                supplier.CommercialRegistrationNumber!,
                supplier.NameAr,
                willChange));
            if (willChange)
            {
                updates.Add((vehicle, supplier));
            }
            else
            {
                unchanged++;
            }
        }

        var canUpdate = !issues.Any(IsError);
        var updated = false;
        if (!validateOnly && canUpdate)
        {
            foreach (var (vehicle, supplier) in updates)
            {
                vehicle.PurchasedFromSupplierId = supplier.Id;
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            updated = true;
        }

        var errorRows = issues.Where(IsError).Select(issue => issue.RowNumber).Distinct().Count();
        return Result.Success(new VehiclePurchaseSupplierImportResponse(
            ValidateOnly: validateOnly,
            CanUpdate: canUpdate,
            Updated: updated,
            Worksheet: workbook.Worksheet,
            TotalRows: workbook.TotalRows,
            ValidRows: workbook.TotalRows - errorRows,
            MatchedVehicles: previews.Count,
            ChangedVehicles: canUpdate ? updates.Count : 0,
            UnchangedVehicles: canUpdate ? unchanged : 0,
            Rows: previews.OrderBy(row => row.RowNumber).ToArray(),
            Issues: issues.OrderBy(issue => issue.RowNumber).ThenBy(issue => issue.Severity).ToArray()));
    }

    private static VehiclePurchaseSupplierImportIssue Error(
        ParsedVehiclePurchaseSupplierRow row,
        string field,
        string message) => new(row.RowNumber, row.SerialNumber, "Error", field, message);

    private static bool IsError(VehiclePurchaseSupplierImportIssue issue) =>
        string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(LogLevel.Warning, "Vehicle purchase-supplier workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Vehicle purchase-supplier workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);
}

internal sealed record ParsedVehiclePurchaseSupplierRow(
    int RowNumber,
    string SerialNumber,
    string NormalizedSerialNumber,
    string SupplierRegistryNumber,
    string NormalizedSupplierRegistryNumber);

internal sealed record ParsedVehiclePurchaseSupplierWorkbook(
    string Worksheet,
    int TotalRows,
    IReadOnlyList<ParsedVehiclePurchaseSupplierRow> Rows,
    IReadOnlyList<VehiclePurchaseSupplierImportIssue> Issues);

internal static class VehiclePurchaseSupplierSpreadsheetParser
{
    private const string SerialNumber = "serialNumber";
    private const string SupplierRegistryNumber = "supplierRegistryNumber";

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.Ordinal)
    {
        [VehicleSpreadsheetParser.NormalizeKey("الرقم التسلسلي")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("رقم المركبة التسلسلي")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("Serial Number")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("Vehicle Serial Number")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("رقم سجل المورد")] = SupplierRegistryNumber,
        [VehicleSpreadsheetParser.NormalizeKey("رقم السجل التجاري للمورد")] = SupplierRegistryNumber,
        [VehicleSpreadsheetParser.NormalizeKey("رقم سجل مورد الشراء")] = SupplierRegistryNumber,
        [VehicleSpreadsheetParser.NormalizeKey("Purchased From Supplier Registry Number")] = SupplierRegistryNumber,
        [VehicleSpreadsheetParser.NormalizeKey("PurchasedFromSupplierId Registry Number")] = SupplierRegistryNumber
    };

    public static ParsedVehiclePurchaseSupplierWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook does not contain a worksheet.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");
        var headerRow = usedRange.FirstRow();
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in headerRow.Cells())
        {
            if (HeaderAliases.TryGetValue(VehicleSpreadsheetParser.NormalizeKey(CellText(cell)), out var field)
                && !headers.ContainsKey(field))
            {
                headers[field] = cell.Address.ColumnNumber;
            }
        }

        if (!headers.TryGetValue(SerialNumber, out var serialColumn)
            || !headers.TryGetValue(SupplierRegistryNumber, out var supplierRegistryColumn))
        {
            throw new InvalidDataException("The workbook is missing the serial-number or supplier-registry column.");
        }

        var rows = new List<ParsedVehiclePurchaseSupplierRow>();
        var issues = new List<VehiclePurchaseSupplierImportIssue>();
        var totalRows = 0;
        foreach (var row in worksheet.Rows(headerRow.RowNumber() + 1, usedRange.LastRow().RowNumber()))
        {
            var serial = CellText(row.Cell(serialColumn));
            var registry = CellText(row.Cell(supplierRegistryColumn));
            if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(registry))
            {
                continue;
            }

            totalRows++;
            if (string.IsNullOrWhiteSpace(serial))
            {
                issues.Add(new(row.RowNumber(), null, "Error", SerialNumber, "الرقم التسلسلي مطلوب."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(registry))
            {
                issues.Add(new(row.RowNumber(), serial, "Error", SupplierRegistryNumber, "رقم سجل المورد مطلوب."));
                continue;
            }

            var normalizedSerial = NormalizeLookup(serial);
            var normalizedRegistry = NormalizeLookup(registry);
            if (normalizedSerial.Length == 0 || normalizedRegistry.Length == 0)
            {
                issues.Add(new(row.RowNumber(), serial, "Error", "row", "الرقم التسلسلي أو رقم سجل المورد غير صالح."));
                continue;
            }
            rows.Add(new(row.RowNumber(), serial.Trim(), normalizedSerial, registry.Trim(), normalizedRegistry));
        }

        if (totalRows == 0)
        {
            throw new InvalidDataException("The workbook does not contain any data rows.");
        }

        return new ParsedVehiclePurchaseSupplierWorkbook(worksheet.Name, totalRows, rows, issues);
    }

    internal static string NormalizeLookup(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character) || character is '-' or '_' or '/' or '.')
            {
                continue;
            }
            builder.Append(character switch
            {
                '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
                '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
                '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
                '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
                '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9',
                _ => char.ToUpperInvariant(character)
            });
        }
        return builder.ToString();
    }

    private static string CellText(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();
}
