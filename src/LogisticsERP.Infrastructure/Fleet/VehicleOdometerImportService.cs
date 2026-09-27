using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehicleOdometerImportService(
    ApplicationDbContext dbContext,
    ILogger<VehicleOdometerImportService> logger) : IVehicleOdometerImportService
{
    public async Task<Result<VehicleOdometerImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<VehicleOdometerImportResponse>(VehicleImportErrors.InvalidOdometerWorkbook);

        try
        {
            var workbook = VehicleOdometerSpreadsheetParser.Parse(content);
            return await ProcessAsync(workbook, validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehicleOdometerImportResponse>(VehicleImportErrors.InvalidOdometerWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehicleOdometerImportResponse>(VehicleImportErrors.OdometerImportFailed);
        }
    }

    private async Task<Result<VehicleOdometerImportResponse>> ProcessAsync(
        ParsedVehicleOdometerWorkbook workbook,
        bool validateOnly,
        CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var serials = workbook.Rows.Select(row => row.NormalizedSerialNumber).Distinct(StringComparer.Ordinal).ToArray();
        var query = dbContext.Vehicles.Where(vehicle => vehicle.NormalizedSerialNumber != null
            && serials.Contains(vehicle.NormalizedSerialNumber));
        Vehicle[] vehicles = validateOnly
            ? await query.AsNoTracking().ToArrayAsync(cancellationToken)
            : await query.ToArrayAsync(cancellationToken);
        var vehiclesBySerial = vehicles
            .GroupBy(vehicle => vehicle.NormalizedSerialNumber!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var previews = new List<VehicleOdometerImportRowPreview>();
        var updates = new List<(Vehicle Vehicle, long Reading)>();
        var seenSerials = new HashSet<string>(StringComparer.Ordinal);
        var unchanged = 0;
        foreach (var row in workbook.Rows)
        {
            if (!seenSerials.Add(row.NormalizedSerialNumber))
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "serialNumber",
                    "الرقم التسلسلي مكرر داخل الملف."));
                continue;
            }
            if (!vehiclesBySerial.TryGetValue(row.NormalizedSerialNumber, out var matches) || matches.Length != 1)
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "serialNumber",
                    $"لم يتم العثور على مركبة واحدة بالرقم التسلسلي '{row.SerialNumber}'."));
                continue;
            }

            var vehicle = matches[0];
            var willChange = vehicle.CurrentOdometer != row.CurrentKm;
            previews.Add(new(row.RowNumber, vehicle.Id, vehicle.SerialNumber ?? row.SerialNumber,
                vehicle.AssetNumber, vehicle.CurrentOdometer, row.CurrentKm, willChange));
            if (willChange) updates.Add((vehicle, row.CurrentKm));
            else unchanged++;
        }

        var canUpdate = !issues.Any(issue => issue.Severity == "Error");
        var updated = false;
        if (!validateOnly && canUpdate)
        {
            var recordedAtUtc = DateTimeOffset.UtcNow;
            foreach (var (vehicle, reading) in updates)
                VehicleMileageRules.ApplyVerifiedReading(vehicle, reading, recordedAtUtc);
            await dbContext.SaveChangesAsync(cancellationToken);
            updated = true;
        }

        var errorRows = issues.Where(issue => issue.Severity == "Error")
            .Select(issue => issue.RowNumber).Distinct().Count();
        return Result.Success(new VehicleOdometerImportResponse(
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
            Issues: issues.OrderBy(issue => issue.RowNumber).ToArray()));
    }

    [LoggerMessage(LogLevel.Warning, "Vehicle odometer workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Vehicle odometer workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);
}

internal sealed record ParsedVehicleOdometerRow(
    int RowNumber, string SerialNumber, string NormalizedSerialNumber, long CurrentKm);

internal sealed record ParsedVehicleOdometerWorkbook(
    string Worksheet, int TotalRows, IReadOnlyList<ParsedVehicleOdometerRow> Rows,
    IReadOnlyList<VehicleOdometerImportIssue> Issues);

internal static class VehicleOdometerSpreadsheetParser
{
    private const string SerialNumber = "serialNumber";
    private const string CurrentKm = "currentKm";

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.Ordinal)
    {
        [VehicleSpreadsheetParser.NormalizeKey("الرقم التسلسلي")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("رقم المركبة التسلسلي")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("Serial Number")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("Vehicle Serial Number")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("الكيلومترات الحالية")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("الكيلومتر الحالي")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("قراءة العداد الحالية")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("عداد المركبة الحالي")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("Current KM")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("Current Vehicle KM")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("Current Odometer")] = CurrentKm,
        [VehicleSpreadsheetParser.NormalizeKey("Current Kilometers")] = CurrentKm
    };

    public static ParsedVehicleOdometerWorkbook Parse(Stream content)
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
                headers[field] = cell.Address.ColumnNumber;
        }
        if (!headers.TryGetValue(SerialNumber, out var serialColumn)
            || !headers.TryGetValue(CurrentKm, out var kmColumn))
            throw new InvalidDataException("The workbook is missing the serial-number or current-km column.");

        var rows = new List<ParsedVehicleOdometerRow>();
        var issues = new List<VehicleOdometerImportIssue>();
        var totalRows = 0;
        foreach (var row in worksheet.Rows(headerRow.RowNumber() + 1, usedRange.LastRow().RowNumber()))
        {
            var serial = CellText(row.Cell(serialColumn));
            var km = CellText(row.Cell(kmColumn));
            if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(km)) continue;
            totalRows++;
            if (string.IsNullOrWhiteSpace(serial))
            {
                issues.Add(new(row.RowNumber(), null, "Error", SerialNumber, "الرقم التسلسلي مطلوب."));
                continue;
            }
            var normalizedSerial = FleetServiceSupport.NormalizeIdentifier(serial);
            if (normalizedSerial.Length == 0)
            {
                issues.Add(new(row.RowNumber(), serial, "Error", SerialNumber, "الرقم التسلسلي غير صالح."));
                continue;
            }
            var normalizedKm = NormalizeDigits(km);
            if (!long.TryParse(normalizedKm, NumberStyles.Integer | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out var reading) || reading < 0)
            {
                issues.Add(new(row.RowNumber(), serial, "Error", CurrentKm,
                    "الكيلومترات الحالية يجب أن تكون عدداً صحيحاً غير سالب."));
                continue;
            }
            rows.Add(new(row.RowNumber(), serial.Trim(), normalizedSerial, reading));
        }
        if (totalRows == 0)
            throw new InvalidDataException("The workbook does not contain any data rows.");
        return new(worksheet.Name, totalRows, rows, issues);
    }

    private static string CellText(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();

    private static string NormalizeDigits(string value) => string.Concat(value.Select(character => character switch
    {
        '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
        '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
        '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
        '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
        '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9',
        _ => character
    }));
}
