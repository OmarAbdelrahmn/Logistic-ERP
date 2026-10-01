using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehicleStatusImportService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<VehicleStatusImportService> logger) : IVehicleStatusImportService
{
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000003");

    public async Task<Result<VehicleStatusImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<VehicleStatusImportResponse>(VehicleStatusImportErrors.InvalidWorkbook);

        try
        {
            var workbook = VehicleStatusSpreadsheetParser.Parse(content);
            return await ProcessAsync(workbook, validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehicleStatusImportResponse>(VehicleStatusImportErrors.InvalidWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehicleStatusImportResponse>(VehicleStatusImportErrors.ImportFailed);
        }
    }

    private async Task<Result<VehicleStatusImportResponse>> ProcessAsync(
        ParsedVehicleStatusWorkbook workbook, bool validateOnly, CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var serials = workbook.Rows.Select(row => row.NormalizedSerialNumber).Distinct().ToArray();
        var query = dbContext.Vehicles.Where(vehicle => vehicle.NormalizedSerialNumber != null
            && serials.Contains(vehicle.NormalizedSerialNumber));
        var vehicles = validateOnly
            ? await query.AsNoTracking().ToArrayAsync(cancellationToken)
            : await query.ToArrayAsync(cancellationToken);
        var bySerial = vehicles.GroupBy(vehicle => vehicle.NormalizedSerialNumber!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToArray();
        var activeAssignmentIds = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(assignment => vehicleIds.Contains(assignment.VehicleId) && assignment.EndedAtUtc == null)
            .Select(assignment => assignment.VehicleId).ToArrayAsync(cancellationToken);
        var activeAssignments = activeAssignmentIds.ToHashSet();
        var blockingIssueIds = await dbContext.VehicleIssues.AsNoTracking()
            .Where(issue => vehicleIds.Contains(issue.VehicleId) && issue.BlocksOperation
                && (issue.Status == VehicleIssueStatus.Open || issue.Status == VehicleIssueStatus.UnderReview))
            .Select(issue => issue.VehicleId).ToArrayAsync(cancellationToken);
        var blockingIssues = blockingIssueIds.ToHashSet();
        var periodQuery = dbContext.VehicleOperationalStatusPeriods
            .Where(period => vehicleIds.Contains(period.VehicleId) && period.EffectiveToUtc == null);
        var periods = validateOnly
            ? await periodQuery.AsNoTracking().ToArrayAsync(cancellationToken)
            : await periodQuery.ToArrayAsync(cancellationToken);
        var periodsByVehicle = periods.GroupBy(period => period.VehicleId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var effectiveAtUtc = timeProvider.GetUtcNow();

        var previews = new List<VehicleStatusImportRowPreview>();
        var changes = new List<(Vehicle Vehicle, ParsedVehicleStatusRow Row, VehicleOperationalStatusPeriod? CurrentPeriod)>();
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
            if (!bySerial.TryGetValue(row.NormalizedSerialNumber, out var matches) || matches.Length != 1)
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "serialNumber",
                    "لم يتم العثور على مركبة واحدة مطابقة للرقم التسلسلي."));
                continue;
            }
            var vehicle = matches[0];
            if (vehicle.CurrentAssignmentId.HasValue || activeAssignments.Contains(vehicle.Id))
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "status",
                    "المركبة مرتبطة بتعيين سائق نشط؛ يجب إنهاء التعيين أولاً."));
                continue;
            }
            if (vehicle.CurrentOperationalStatus == VehicleOperationalStatus.Decommissioned
                && row.Status != VehicleOperationalStatus.Decommissioned)
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "status",
                    "لا يمكن تغيير مركبة مستبعدة نهائياً من خلال الاستيراد."));
                continue;
            }
            if (row.Status == VehicleOperationalStatus.Available && blockingIssues.Contains(vehicle.Id))
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "status",
                    "للمركبة بلاغ عطل مانع للتشغيل؛ لا يمكن جعلها جاهزة."));
                continue;
            }
            periodsByVehicle.TryGetValue(vehicle.Id, out var openPeriods);
            if (openPeriods is { Length: > 1 }
                || openPeriods is { Length: 1 } && openPeriods[0].EffectiveFromUtc > effectiveAtUtc)
            {
                issues.Add(new(row.RowNumber, row.SerialNumber, "Error", "status",
                    "سجل حالات المركبة الحالي غير متسق أو يبدأ في المستقبل."));
                continue;
            }

            var willChange = vehicle.CurrentOperationalStatus != row.Status;
            previews.Add(new(row.RowNumber, vehicle.Id, vehicle.SerialNumber ?? row.SerialNumber,
                vehicle.AssetNumber, vehicle.CurrentOperationalStatus, row.Status,
                row.SourceStatus, row.IssueNote, willChange));
            if (willChange) changes.Add((vehicle, row, openPeriods?.SingleOrDefault()));
            else unchanged++;
        }

        var canUpdate = !issues.Any(issue => issue.Severity == "Error");
        var updated = false;
        if (!validateOnly && canUpdate)
        {
            var actor = currentUser.UserId ?? SystemActorId;
            foreach (var (vehicle, row, currentPeriod) in changes)
            {
                if (currentPeriod is not null) currentPeriod.EffectiveToUtc = effectiveAtUtc;
                vehicle.CurrentOperationalStatus = row.Status;
                var reason = row.IssueNote is null
                    ? $"Imported vehicle status from inventory sheet: {row.SourceStatus}."
                    : $"Imported vehicle status from inventory sheet: {row.SourceStatus}. {row.IssueNote}";
                dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod
                {
                    VehicleId = vehicle.Id,
                    Status = row.Status,
                    EffectiveFromUtc = effectiveAtUtc,
                    Reason = reason,
                    SourceType = VehicleStatusSourceType.Administrative,
                    SourceEntityId = vehicle.Id,
                    ChangedByUserId = actor
                });
                if (row.Status == VehicleOperationalStatus.Decommissioned)
                {
                    vehicle.DecommissionedAtUtc = effectiveAtUtc;
                    vehicle.DecommissionReason = reason;
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            updated = true;
        }

        var errorRows = issues.Where(issue => issue.Severity == "Error")
            .Select(issue => issue.RowNumber).Distinct().Count();
        return Result.Success(new VehicleStatusImportResponse(
            validateOnly, canUpdate, updated, workbook.Worksheet, workbook.TotalRows,
            workbook.TotalRows - errorRows, previews.Count,
            canUpdate ? changes.Count : 0, canUpdate ? unchanged : 0,
            previews.OrderBy(row => row.RowNumber).ToArray(),
            issues.OrderBy(issue => issue.RowNumber).ToArray()));
    }

    [LoggerMessage(LogLevel.Warning, "Vehicle status workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Vehicle status workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);
}

internal sealed record ParsedVehicleStatusRow(
    int RowNumber, string SerialNumber, string NormalizedSerialNumber,
    string SourceStatus, VehicleOperationalStatus Status, string? IssueNote);

internal sealed record ParsedVehicleStatusWorkbook(
    string Worksheet, int TotalRows, IReadOnlyList<ParsedVehicleStatusRow> Rows,
    IReadOnlyList<VehicleStatusImportIssue> Issues);

internal static class VehicleStatusSpreadsheetParser
{
    private const string SerialNumber = "serialNumber";
    private const string Status = "status";
    private const string IssueNote = "issueNote";

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.Ordinal)
    {
        [VehicleSpreadsheetParser.NormalizeKey("serial")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("serial number")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("الرقم التسلسلي")] = SerialNumber,
        [VehicleSpreadsheetParser.NormalizeKey("status")] = Status,
        [VehicleSpreadsheetParser.NormalizeKey("الحالة")] = Status,
        [VehicleSpreadsheetParser.NormalizeKey("حالة المركبة")] = Status,
        [VehicleSpreadsheetParser.NormalizeKey("اعطال السياره")] = IssueNote,
        [VehicleSpreadsheetParser.NormalizeKey("issue note")] = IssueNote
    };

    private static readonly Dictionary<string, VehicleOperationalStatus> StatusAliases = new(StringComparer.Ordinal)
    {
        [VehicleSpreadsheetParser.NormalizeKey("ready")] = VehicleOperationalStatus.Available,
        [VehicleSpreadsheetParser.NormalizeKey("available")] = VehicleOperationalStatus.Available,
        [VehicleSpreadsheetParser.NormalizeKey("جاهز")] = VehicleOperationalStatus.Available,
        [VehicleSpreadsheetParser.NormalizeKey("صيانه")] = VehicleOperationalStatus.OutOfService,
        [VehicleSpreadsheetParser.NormalizeKey("out of service")] = VehicleOperationalStatus.OutOfService,
        [VehicleSpreadsheetParser.NormalizeKey("حادث")] = VehicleOperationalStatus.AccidentHold,
        [VehicleSpreadsheetParser.NormalizeKey("accident")] = VehicleOperationalStatus.AccidentHold,
        [VehicleSpreadsheetParser.NormalizeKey("stolen")] = VehicleOperationalStatus.Stolen,
        [VehicleSpreadsheetParser.NormalizeKey("stolen property")] = VehicleOperationalStatus.Stolen,
        [VehicleSpreadsheetParser.NormalizeKey("مسروق")] = VehicleOperationalStatus.Stolen,
        [VehicleSpreadsheetParser.NormalizeKey("مسروقة")] = VehicleOperationalStatus.Stolen,
        [VehicleSpreadsheetParser.NormalizeKey("تالف")] = VehicleOperationalStatus.Decommissioned,
        [VehicleSpreadsheetParser.NormalizeKey("decommissioned")] = VehicleOperationalStatus.Decommissioned,
        [VehicleSpreadsheetParser.NormalizeKey("تحت مسؤلية الحركة")] = VehicleOperationalStatus.UnderMovementResponsibility,
        [VehicleSpreadsheetParser.NormalizeKey("تحت مسؤولية الحركة")] = VehicleOperationalStatus.UnderMovementResponsibility,
        [VehicleSpreadsheetParser.NormalizeKey("under movement responsibility")] = VehicleOperationalStatus.UnderMovementResponsibility
    };

    public static ParsedVehicleStatusWorkbook Parse(Stream content)
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
            || !headers.TryGetValue(Status, out var statusColumn))
            throw new InvalidDataException("The workbook is missing the serial or status column.");

        var rows = new List<ParsedVehicleStatusRow>();
        var issues = new List<VehicleStatusImportIssue>();
        var totalRows = 0;
        foreach (var row in worksheet.Rows(headerRow.RowNumber() + 1, usedRange.LastRow().RowNumber()))
        {
            var serial = CellText(row.Cell(serialColumn));
            var sourceStatus = CellText(row.Cell(statusColumn));
            var issueNote = headers.TryGetValue(IssueNote, out var noteColumn)
                ? CellText(row.Cell(noteColumn)) : string.Empty;
            if (serial.Length == 0 && sourceStatus.Length == 0 && issueNote.Length == 0) continue;
            totalRows++;
            if (serial.Length == 0 || FleetServiceSupport.NormalizeIdentifier(serial).Length == 0)
            {
                issues.Add(new(row.RowNumber(), null, "Error", SerialNumber,
                    "الرقم التسلسلي مطلوب وغير صالح إذا كان فارغاً."));
                continue;
            }
            if (!StatusAliases.TryGetValue(VehicleSpreadsheetParser.NormalizeKey(sourceStatus), out var status))
            {
                issues.Add(new(row.RowNumber(), serial, "Error", Status,
                    $"حالة المركبة '{sourceStatus}' غير معروفة."));
                continue;
            }
            if (issueNote.Length > 800)
            {
                issues.Add(new(row.RowNumber(), serial, "Error", IssueNote,
                    "ملاحظة الأعطال تتجاوز 800 حرف."));
                continue;
            }
            rows.Add(new(row.RowNumber(), serial, FleetServiceSupport.NormalizeIdentifier(serial),
                sourceStatus, status, issueNote.Length == 0 ? null : issueNote));
        }
        if (totalRows == 0)
            throw new InvalidDataException("The workbook does not contain any data rows.");
        return new(worksheet.Name, totalRows, rows, issues);
    }

    private static string CellText(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();
}
