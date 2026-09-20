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

internal sealed partial class VehicleRiderAssignmentImportService(
    ApplicationDbContext dbContext,
    ILogger<VehicleRiderAssignmentImportService> logger) : IVehicleRiderAssignmentImportService
{
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000003");
    private const string ImportReason = "Vehicle/rider assignment imported from Tamm Excel file.";

    public async Task<Result<VehicleRiderAssignmentImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<VehicleRiderAssignmentImportResponse>(VehicleImportErrors.InvalidRiderAssignmentWorkbook);

        try
        {
            return await ProcessAsync(VehicleRiderAssignmentSpreadsheetParser.Parse(content), validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehicleRiderAssignmentImportResponse>(VehicleImportErrors.InvalidRiderAssignmentWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehicleRiderAssignmentImportResponse>(VehicleImportErrors.RiderAssignmentImportFailed);
        }
    }

    private async Task<Result<VehicleRiderAssignmentImportResponse>> ProcessAsync(
        ParsedVehicleRiderAssignmentWorkbook workbook, bool validateOnly, CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var serials = workbook.Rows.Select(x => x.NormalizedSerialNumber).Distinct().ToArray();
        var iqamas = workbook.Rows.Select(x => x.IqamaNo).Distinct().ToArray();
        var vehicles = await dbContext.Vehicles
            .Where(x => x.NormalizedSerialNumber != null && serials.Contains(x.NormalizedSerialNumber))
            .ToArrayAsync(cancellationToken);
        var vehicleBySerial = vehicles.GroupBy(x => x.NormalizedSerialNumber!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var riders = await (from profile in dbContext.RiderProfiles.AsNoTracking()
                            join employee in dbContext.Employees.AsNoTracking() on profile.EmployeeId equals employee.Id
                            where employee.IqamaNo != null && iqamas.Contains(employee.IqamaNo)
                            select new RiderLookup(profile.Id, employee.IqamaNo!, employee.FullNameAr,
                                employee.Status, employee.IsEmployee)).ToArrayAsync(cancellationToken);
        var riderByIqama = riders.GroupBy(x => x.IqamaNo, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var activeVehicleIds = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(x => x.EndedAtUtc == null).Select(x => x.VehicleId).ToArrayAsync(cancellationToken);
        var activeRiderIds = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(x => x.EndedAtUtc == null).Select(x => x.RiderProfileId).ToArrayAsync(cancellationToken);
        var usedVehicles = activeVehicleIds.ToHashSet();
        var usedRiders = activeRiderIds.ToHashSet();
        var previews = new List<VehicleRiderAssignmentImportRowPreview>();
        var plans = new List<AssignmentPlan>();

        foreach (var row in workbook.Rows)
        {
            if (!vehicleBySerial.TryGetValue(row.NormalizedSerialNumber, out var vehicle))
            {
                issues.Add(Error(row, "serialNumber", $"لم يتم العثور على مركبة بالرقم التسلسلي '{row.SerialNumber}'."));
                continue;
            }
            if (!riderByIqama.TryGetValue(row.IqamaNo, out var rider))
            {
                issues.Add(Error(row, "riderIqamaNo", $"لم يتم العثور على سائق برقم الهوية/الإقامة '{row.IqamaNo}'."));
                continue;
            }
            if (rider.Status != EmployeeStatus.Active || rider.IsEmployee)
                issues.Add(Error(row, "riderIqamaNo", "السائق غير فعال أو أن السجل ليس ملف سائق."));
            if (vehicle.CurrentOperationalStatus != VehicleOperationalStatus.Available || vehicle.CurrentAssignmentId.HasValue || !usedVehicles.Add(vehicle.Id))
                issues.Add(Error(row, "serialNumber", "المركبة غير متاحة للإسناد أو مكررة داخل الملف."));
            if (!usedRiders.Add(rider.Id))
                issues.Add(Error(row, "riderIqamaNo", "السائق لديه إسناد فعال أو مكرر داخل الملف."));
            if (issues.Any(x => x.RowNumber == row.RowNumber && IsError(x))) continue;

            previews.Add(new(row.RowNumber, vehicle.Id, vehicle.SerialNumber ?? row.SerialNumber,
                vehicle.AssetNumber, rider.Id, rider.IqamaNo, rider.Name, row.PermissionStartsOn));
            plans.Add(new(row, vehicle, rider));
        }

        // Row errors do not block the rest of the workbook. CanImport means that
        // at least one row is valid and can be committed.
        var canImport = plans.Count > 0;
        var imported = false;
        if (!validateOnly && plans.Count > 0)
        {
            foreach (var plan in plans)
            {
                var startedAt = new DateTimeOffset(plan.Row.PermissionStartsOn.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3));
                var operationId = Guid.CreateVersion7();
                var assignment = new RiderVehicleAssignment
                {
                    RiderProfileId = plan.Rider.Id,
                    IsRealRider = true,
                    VehicleId = plan.Vehicle.Id,
                    OperationId = operationId,
                    StartedAtUtc = startedAt,
                    StartOdometer = plan.Vehicle.CurrentOdometer,
                    StartVehicleCondition = VehicleCondition.Good,
                    PermissionReference = $"Tamm:{plan.Row.IqamaNo}",
                    PermissionStartsOn = plan.Row.PermissionStartsOn,
                    PermissionEndsOn = FleetBusinessRules.PermitEnd(plan.Row.PermissionStartsOn),
                    AssignmentReason = ImportReason,
                    AssignedByUserId = SystemActorId,
                    WasBackdated = startedAt < DateTimeOffset.UtcNow.AddMinutes(-5),
                    BackdatedReason = startedAt < DateTimeOffset.UtcNow.AddMinutes(-5) ? ImportReason : null,
                    Notes = "Imported from Tamm authorization sheet."
                };
                dbContext.RiderVehicleAssignments.Add(assignment);
                dbContext.RiderVehicleAssignmentEvents.Add(new RiderVehicleAssignmentEvent
                {
                    RiderVehicleAssignmentId = assignment.Id, OperationId = operationId,
                    EventType = RiderVehicleAssignmentEventType.Taken, OccurredAtUtc = startedAt,
                    ActorUserId = SystemActorId, Reason = ImportReason
                });
                var currentPeriod = await dbContext.VehicleOperationalStatusPeriods
                    .SingleOrDefaultAsync(x => x.VehicleId == plan.Vehicle.Id && x.EffectiveToUtc == null, cancellationToken);
                if (currentPeriod is not null)
                    currentPeriod.EffectiveToUtc = startedAt < currentPeriod.EffectiveFromUtc ? currentPeriod.EffectiveFromUtc : startedAt;
                plan.Vehicle.CurrentAssignmentId = assignment.Id;
                plan.Vehicle.CurrentOperationalStatus = VehicleOperationalStatus.Assigned;
                dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod
                {
                    VehicleId = plan.Vehicle.Id, Status = VehicleOperationalStatus.Assigned,
                    EffectiveFromUtc = startedAt, Reason = ImportReason,
                    SourceType = VehicleStatusSourceType.Assignment, SourceEntityId = assignment.Id,
                    ChangedByUserId = SystemActorId
                });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            imported = true;
        }

        var errorRows = issues.Where(IsError).Select(x => x.RowNumber).Distinct().Count();
        return Result.Success(new VehicleRiderAssignmentImportResponse(validateOnly, canImport, imported,
            workbook.Worksheet, workbook.TotalRows, workbook.TotalRows - errorRows,
            imported ? plans.Count : 0, previews.OrderBy(x => x.RowNumber).ToArray(),
            issues.OrderBy(x => x.RowNumber).ThenBy(x => x.Severity).ToArray()));
    }

    private static VehicleRiderAssignmentImportIssue Error(ParsedVehicleRiderAssignmentRow row, string field, string message) =>
        new(row.RowNumber, row.SerialNumber, "Error", field, message);
    private static bool IsError(VehicleRiderAssignmentImportIssue issue) =>
        string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(LogLevel.Warning, "Vehicle/rider assignment workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);
    [LoggerMessage(LogLevel.Error, "Vehicle/rider assignment workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);

    private sealed record RiderLookup(Guid Id, string IqamaNo, string Name, EmployeeStatus Status, bool IsEmployee);
    private sealed record AssignmentPlan(ParsedVehicleRiderAssignmentRow Row, Vehicle Vehicle, RiderLookup Rider);
}

internal sealed record ParsedVehicleRiderAssignmentRow(int RowNumber, string SerialNumber,
    string NormalizedSerialNumber, string IqamaNo, DateOnly PermissionStartsOn);
internal sealed record ParsedVehicleRiderAssignmentWorkbook(string Worksheet, int TotalRows,
    IReadOnlyList<ParsedVehicleRiderAssignmentRow> Rows, IReadOnlyList<VehicleRiderAssignmentImportIssue> Issues);

internal static class VehicleRiderAssignmentSpreadsheetParser
{
    private const string Serial = "serialNumber";
    private const string Iqama = "riderIqamaNo";
    private const string StartDate = "permissionStartsOn";
    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.Ordinal)
    {
        [Normalize("الرقم التسلسلي")] = Serial,
        [Normalize("هوية المفوض في تم")] = Iqama,
        [Normalize("هوية المفوض في تَم")] = Iqama,
        [Normalize("تاريخ بداية التفويض")] = StartDate,
        [Normalize("Serial Number")] = Serial,
        [Normalize("Tamm Authorized Person ID")] = Iqama,
        [Normalize("Authorization Start Date")] = StartDate
    };

    public static ParsedVehicleRiderAssignmentWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("Workbook has no worksheet.");
        var range = sheet.RangeUsed() ?? throw new InvalidDataException("Worksheet is empty.");
        var header = range.FirstRow();
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in header.Cells())
            if (HeaderAliases.TryGetValue(Normalize(Text(cell)), out var field)) headers.TryAdd(field, cell.Address.ColumnNumber);
        if (!headers.TryGetValue(Serial, out var serialColumn) || !headers.TryGetValue(Iqama, out var iqamaColumn)
            || !headers.TryGetValue(StartDate, out var dateColumn))
            throw new InvalidDataException("Required columns are missing.");

        var rows = new List<ParsedVehicleRiderAssignmentRow>();
        var issues = new List<VehicleRiderAssignmentImportIssue>();
        var total = 0;
        foreach (var row in sheet.Rows(header.RowNumber() + 1, range.LastRow().RowNumber()))
        {
            var serial = Text(row.Cell(serialColumn)); var iqama = Digits(Text(row.Cell(iqamaColumn)));
            var dateText = Text(row.Cell(dateColumn));
            if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(iqama) && string.IsNullOrWhiteSpace(dateText)) continue;
            total++;
            if (string.IsNullOrWhiteSpace(serial)) issues.Add(new(row.RowNumber(), null, "Error", Serial, "الرقم التسلسلي مطلوب."));
            if (iqama.Length != 10 || !iqama.All(char.IsAsciiDigit)) issues.Add(new(row.RowNumber(), serial, "Error", Iqama, "هوية المفوض يجب أن تتكون من 10 أرقام."));
            if (!TryDate(row.Cell(dateColumn), out var date)) issues.Add(new(row.RowNumber(), serial, "Error", StartDate, "تاريخ بداية التفويض غير صالح."));
            if (issues.Any(x => x.RowNumber == row.RowNumber())) continue;
            rows.Add(new(row.RowNumber(), serial.Trim(), NormalizeLookup(serial), iqama, date));
        }
        if (total == 0) throw new InvalidDataException("Worksheet has no data rows.");
        return new(sheet.Name, total, rows, issues);
    }

    private static bool TryDate(IXLCell cell, out DateOnly date)
    {
        if (cell.TryGetValue<DateTime>(out var value)) { date = DateOnly.FromDateTime(value); return true; }
        var text = Text(cell);
        string[] formats = ["M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd"];
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        { date = DateOnly.FromDateTime(value); return true; }
        date = default; return false;
    }

    private static string Text(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();
    private static string Digits(string value) => new(value.Select(c => c switch
    { '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1', '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3', '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5', '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7', '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9', _ => c }).Where(char.IsAsciiDigit).ToArray());
    private static string NormalizeLookup(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character) || character is '-' or '_' or '/' or '.') continue;
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
    private static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormKC)
        .Where(c => !char.IsWhiteSpace(c) && c is not '-' and not '_' and not '/' and not '.')).ToUpperInvariant();
}
