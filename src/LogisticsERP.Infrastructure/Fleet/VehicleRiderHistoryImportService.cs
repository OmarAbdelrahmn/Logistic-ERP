using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehicleRiderHistoryImportService(
    ApplicationDbContext dbContext,
    ILogger<VehicleRiderHistoryImportService> logger) : IVehicleRiderHistoryImportService
{
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000003");
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);
    private const string ImportReason = "Historical Tamm authorization imported from Excel.";

    public async Task<Result<VehicleRiderHistoryImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<VehicleRiderHistoryImportResponse>(VehicleImportErrors.InvalidRiderHistoryWorkbook);

        try
        {
            return await ProcessAsync(VehicleRiderHistorySpreadsheetParser.Parse(content), validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehicleRiderHistoryImportResponse>(VehicleImportErrors.InvalidRiderHistoryWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehicleRiderHistoryImportResponse>(VehicleImportErrors.RiderHistoryImportFailed);
        }
    }

    private async Task<Result<VehicleRiderHistoryImportResponse>> ProcessAsync(
        ParsedVehicleRiderHistoryWorkbook workbook, bool validateOnly, CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var serials = workbook.Rows.Select(row => row.NormalizedSerialNumber).Distinct().ToArray();
        var iqamas = workbook.Rows.Select(row => row.IqamaNo).Distinct().ToArray();
        var references = workbook.Rows.Select(row => row.PermissionReference).Distinct().ToArray();
        var vehicles = await dbContext.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.NormalizedSerialNumber != null && serials.Contains(vehicle.NormalizedSerialNumber))
            .ToArrayAsync(cancellationToken);
        var employees = await dbContext.Employees.AsNoTracking()
            .Where(employee => employee.IqamaNo != null && iqamas.Contains(employee.IqamaNo))
            .ToArrayAsync(cancellationToken);
        var employeeIds = employees.Select(employee => employee.Id).ToArray();
        var profiles = await dbContext.RiderProfiles.AsNoTracking()
            .Where(profile => employeeIds.Contains(profile.EmployeeId))
            .ToArrayAsync(cancellationToken);
        var existing = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(assignment => assignment.PermissionReference != null && references.Contains(assignment.PermissionReference))
            .ToArrayAsync(cancellationToken);

        var vehiclesBySerial = vehicles.GroupBy(vehicle => vehicle.NormalizedSerialNumber!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var employeesByIqama = employees.GroupBy(employee => employee.IqamaNo!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var profilesByEmployee = profiles.GroupBy(profile => profile.EmployeeId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var existingByReference = existing.GroupBy(assignment => assignment.PermissionReference!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var rows = new List<VehicleRiderHistoryImportRow>();
        var plans = new List<(ParsedVehicleRiderHistoryRow Row, Guid VehicleId, Guid RiderProfileId, Guid? EmployeeIdForNewProfile)>();
        var newProfileIdsByEmployee = new Dictionary<Guid, Guid>();
        var seenReferences = new HashSet<string>(StringComparer.Ordinal);
        var alreadyImported = 0;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(RiyadhOffset).DateTime);

        foreach (var row in workbook.Rows)
        {
            if (!seenReferences.Add(row.PermissionReference))
                issues.Add(Error(row, "permissionReference", "رقم التفويض مكرر داخل الملف."));
            if (row.EndsOn >= today)
                issues.Add(Error(row, "endsOn", "تاريخ نهاية التفويض يجب أن يكون قبل اليوم لإنشاء سجل تاريخي فقط."));
            if (!vehiclesBySerial.TryGetValue(row.NormalizedSerialNumber, out var vehicleMatches) || vehicleMatches.Length != 1)
                issues.Add(Error(row, "serialNumber", "لم يتم العثور على مركبة واحدة مطابقة للرقم التسلسلي."));
            if (!employeesByIqama.TryGetValue(row.IqamaNo, out var employeeMatches) || employeeMatches.Length != 1)
                issues.Add(Error(row, "riderIqamaNo", "لم يتم العثور على موظف/سائق واحد مطابق لرقم الهوية."));

            Guid? riderProfileId = null;
            Guid? employeeIdForNewProfile = null;
            if (employeeMatches is { Length: 1 })
            {
                if (profilesByEmployee.TryGetValue(employeeMatches[0].Id, out var profileMatches) && profileMatches.Length == 1)
                    riderProfileId = profileMatches[0].Id;
                else if (profileMatches is null)
                {
                    employeeIdForNewProfile = employeeMatches[0].Id;
                    if (!newProfileIdsByEmployee.TryGetValue(employeeIdForNewProfile.Value, out var newProfileId))
                    {
                        newProfileId = Guid.CreateVersion7();
                        newProfileIdsByEmployee.Add(employeeIdForNewProfile.Value, newProfileId);
                    }
                    riderProfileId = newProfileId;
                }
                else
                    issues.Add(Error(row, "riderIqamaNo", "يوجد أكثر من ملف سائق مطابق للموظف."));
            }
            if (issues.Any(issue => issue.RowNumber == row.RowNumber && issue.Severity == "Error")) continue;

            var vehicleId = vehicleMatches![0].Id;
            var profileId = riderProfileId!.Value;
            var start = StartOfDay(row.StartsOn);
            var end = StartOfDay(row.EndsOn.AddDays(1)); // End date is inclusive in the spreadsheet.
            if (existingByReference.TryGetValue(row.PermissionReference, out var existingMatches))
            {
                if (existingMatches.Length != 1 || existingMatches[0].VehicleId != vehicleId
                    || existingMatches[0].RiderProfileId != profileId
                    || existingMatches[0].StartedAtUtc != start || existingMatches[0].EndedAtUtc != end
                    || existingMatches[0].PermissionStartsOn != row.StartsOn
                    || existingMatches[0].PermissionEndsOn != row.EndsOn
                    || existingMatches[0].Status != RiderVehicleAssignmentStatus.Completed)
                {
                    issues.Add(Error(row, "permissionReference", "رقم التفويض موجود بالفعل ببيانات مختلفة."));
                    continue;
                }
                alreadyImported++;
                rows.Add(new(row.RowNumber, vehicleId, row.SerialNumber, profileId, row.IqamaNo,
                    row.PermissionReference, row.StartsOn, row.EndsOn, "AlreadyImported"));
                continue;
            }

            plans.Add((row, vehicleId, profileId, employeeIdForNewProfile));
            rows.Add(new(row.RowNumber, vehicleId, row.SerialNumber, profileId, row.IqamaNo,
                row.PermissionReference, row.StartsOn, row.EndsOn, "Create"));
        }

        if (!validateOnly)
        {
            var createdProfiles = new HashSet<Guid>();
            foreach (var plan in plans)
            {
                if (plan.EmployeeIdForNewProfile is { } employeeId && createdProfiles.Add(employeeId))
                {
                    dbContext.RiderProfiles.Add(new RiderProfile
                    {
                        Id = plan.RiderProfileId,
                        EmployeeId = employeeId,
                        OperationalNotes = "Virtual rider profile created by Tamm vehicle-assignment history import.",
                        CreatedByUserId = SystemActorId
                    });
                }
                var operationId = Guid.CreateVersion7();
                var start = StartOfDay(plan.Row.StartsOn);
                var end = StartOfDay(plan.Row.EndsOn.AddDays(1));
                var assignment = new RiderVehicleAssignment
                {
                    RiderProfileId = plan.RiderProfileId,
                    VehicleId = plan.VehicleId,
                    IsRealRider = true,
                    OperationId = operationId,
                    StartedAtUtc = start,
                    EndedAtUtc = end,
                    StartOdometer = 0, // The source has no odometer readings.
                    StartVehicleCondition = VehicleCondition.Unknown,
                    EndVehicleCondition = VehicleCondition.Unknown,
                    PermissionReference = plan.Row.PermissionReference,
                    PermissionStartsOn = plan.Row.StartsOn,
                    PermissionEndsOn = plan.Row.EndsOn,
                    Status = RiderVehicleAssignmentStatus.Completed,
                    AssignmentReason = ImportReason,
                    CompletionReason = ImportReason,
                    AssignedByUserId = SystemActorId,
                    EndedByUserId = SystemActorId,
                    WasBackdated = true,
                    BackdatedReason = ImportReason,
                    Notes = "Imported authorization history; actual vehicle handover and odometer were not supplied."
                };
                dbContext.RiderVehicleAssignments.Add(assignment);
            }
            if (plans.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new VehicleRiderHistoryImportResponse(
            validateOnly, plans.Count > 0, !validateOnly && plans.Count > 0,
            workbook.Worksheet, workbook.TotalRows, rows.Count,
            validateOnly ? 0 : plans.Count, alreadyImported,
            rows.OrderBy(row => row.RowNumber).ToArray(),
            issues.OrderBy(issue => issue.RowNumber).ToArray()));
    }

    private static DateTimeOffset StartOfDay(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), RiyadhOffset);
    private static VehicleRiderAssignmentImportIssue Error(ParsedVehicleRiderHistoryRow row, string field, string message) =>
        new(row.RowNumber, row.SerialNumber, "Error", field, message);

    [LoggerMessage(LogLevel.Warning, "Vehicle/rider history workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);
    [LoggerMessage(LogLevel.Error, "Vehicle/rider history workbook {FileName} failed during import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);
}

internal sealed record ParsedVehicleRiderHistoryRow(
    int RowNumber, string SerialNumber, string NormalizedSerialNumber, string PermissionReference,
    string IqamaNo, DateOnly StartsOn, DateOnly EndsOn);

internal sealed record ParsedVehicleRiderHistoryWorkbook(
    string Worksheet, int TotalRows, IReadOnlyList<ParsedVehicleRiderHistoryRow> Rows,
    IReadOnlyList<VehicleRiderAssignmentImportIssue> Issues);

internal static class VehicleRiderHistorySpreadsheetParser
{
    public static ParsedVehicleRiderHistoryWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidDataException("Workbook has no worksheet.");
        var range = sheet.RangeUsed() ?? throw new InvalidDataException("Worksheet is empty.");
        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in range.FirstRow().Cells())
            headers.TryAdd(NormalizeHeader(VehicleRiderAssignmentSpreadsheetParser.Text(cell)), cell.Address.ColumnNumber);
        string[] names = ["الرقم التسلسلي", "رقم التفويض", "رقم هوية المفوض", "تاريخ بداية التفويض", "تاريخ نهاية التفويض / الإلغاء"];
        if (names.Any(name => !headers.ContainsKey(NormalizeHeader(name))))
            throw new InvalidDataException("Required history columns are missing.");
        var columns = names.Select(name => headers[NormalizeHeader(name)]).ToArray();
        var rows = new List<ParsedVehicleRiderHistoryRow>();
        var issues = new List<VehicleRiderAssignmentImportIssue>();
        var total = 0;
        foreach (var sheetRow in sheet.Rows(range.FirstRow().RowNumber() + 1, range.LastRow().RowNumber()))
        {
            var values = columns.Select(column => VehicleRiderAssignmentSpreadsheetParser.Text(sheetRow.Cell(column))).ToArray();
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            total++;
            var serial = values[0];
            var reference = VehicleRiderAssignmentSpreadsheetParser.Digits(values[1]);
            var iqama = VehicleRiderAssignmentSpreadsheetParser.Digits(values[2]);
            if (string.IsNullOrWhiteSpace(serial)) issues.Add(new(sheetRow.RowNumber(), null, "Error", "serialNumber", "الرقم التسلسلي مطلوب."));
            if (string.IsNullOrWhiteSpace(reference) || reference.Length != values[1].Length)
                issues.Add(new(sheetRow.RowNumber(), serial, "Error", "permissionReference", "رقم التفويض غير صالح."));
            if (iqama.Length != 10 || !iqama.All(char.IsAsciiDigit))
                issues.Add(new(sheetRow.RowNumber(), serial, "Error", "riderIqamaNo", "هوية المفوض يجب أن تتكون من 10 أرقام."));
            var hasStart = VehicleRiderAssignmentSpreadsheetParser.TryDate(sheetRow.Cell(columns[3]), out var start);
            var hasEnd = VehicleRiderAssignmentSpreadsheetParser.TryDate(sheetRow.Cell(columns[4]), out var end);
            if (!hasStart) issues.Add(new(sheetRow.RowNumber(), serial, "Error", "startsOn", "تاريخ بداية التفويض غير صالح."));
            if (!hasEnd || hasStart && end < start)
                issues.Add(new(sheetRow.RowNumber(), serial, "Error", "endsOn", "تاريخ نهاية التفويض غير صالح أو يسبق البداية."));
            if (issues.Any(issue => issue.RowNumber == sheetRow.RowNumber())) continue;
            rows.Add(new(sheetRow.RowNumber(), serial, VehicleRiderAssignmentSpreadsheetParser.NormalizeLookup(serial),
                reference, iqama, start, end));
        }
        if (total == 0) throw new InvalidDataException("Worksheet has no data rows.");
        return new(sheet.Name, total, rows, issues);
    }

    private static string NormalizeHeader(string value) => string.Concat(value.Normalize(System.Text.NormalizationForm.FormKC)
        .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '_' and not '/' and not '.'));
}
