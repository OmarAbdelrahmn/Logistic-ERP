using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HrExcelImportService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<HrExcelImportService> logger) : IHrExcelImportService
{
    private const int MaximumRows = 5_000;
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000001");
    private static readonly string[] RequiredHeaders = ["رقم الاقامة", "الاسم"];
    private static readonly PlatformColumn[] PlatformColumns =
    [
        new("KEETA", "ايدي كيتا"), new("HUNGER", "ايدي هنقر"), new("AMAZON", "ايدي امازون"),
        new("JAHEZ", "ايدي جاهز"), new("NINJA", "ايدي نينجا"), new("SHIFTZ", "ايدي شفز")
    ];
    private static readonly HashSet<string> ImportedHeaders = new(StringComparer.Ordinal)
    {
        "رقم الاقامة", "الاسم", "تاريخ التعين", "الجنسية", "المهنة بالاقامة", "المسمي الوظيفي",
        "العمل الفعلي", "الفرع", "هوية صاحب العمل", "حالة الكفالة", "ايدي كيتا", "ايدي هنقر",
        "ايدي امازون", "ايدي جاهز", "ايدي نينجا", "ايدي شفز"
    };

    public async Task<Result<HrExcelImportResponse>> ImportAsync(Stream content, string fileName, bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead) return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
        try
        {
            using var workbook = new XLWorkbook(content);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            var used = worksheet?.RangeUsed(XLCellsUsedOptions.Contents);
            if (worksheet is null || used is null || used.RowCount() > MaximumRows)
                return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);

            var headerRow = worksheet.RowsUsed().Take(10).FirstOrDefault(row => RequiredHeaders.All(required =>
                row.CellsUsed().Any(cell => HeaderKey(CellText(cell)) == required)));
            if (headerRow is null) return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);

            var headers = headerRow.CellsUsed().Select(cell => (cell.Address.ColumnNumber, Name: HeaderKey(CellText(cell))))
                .Where(item => item.Name.Length > 0).GroupBy(item => item.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().ColumnNumber, StringComparer.Ordinal);
            var issues = new List<HrExcelImportIssue>();
            var rows = ParseRows(worksheet, headerRow.RowNumber(), used.LastRow().RowNumber(), headers, issues);
            if (rows.Count == 0) return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);

            var counts = await ApplyRowsAsync(rows, validateOnly, issues, cancellationToken);
            var imported = headers.Keys.Where(ImportedHeaders.Contains).Order(StringComparer.Ordinal).ToArray();
            var ignored = headers.Keys.Where(item => !ImportedHeaders.Contains(item)).Order(StringComparer.Ordinal).ToArray();
            return Result.Success(new HrExcelImportResponse(validateOnly, worksheet.Name, rows.Count,
                rows.Count(row => !issues.Any(issue => issue.RowNumber == row.RowNumber && issue.Severity == "Error")),
                counts.CreatedEmployees, counts.UpdatedEmployees, counts.CreatedRiders, 0,
                counts.CreatedPlatformAccounts, 0, imported, ignored,
                issues.OrderBy(item => item.RowNumber).ThenBy(item => item.Severity).ToArray()));
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogDatabaseFailure(logger, exception);
            return Result.Failure<HrExcelImportResponse>(HrImportErrors.ImportFailed);
        }
    }

    private static List<ParsedRow> ParseRows(IXLWorksheet worksheet, int headerRow, int lastRow,
        IReadOnlyDictionary<string, int> headers, List<HrExcelImportIssue> issues)
    {
        var result = new List<ParsedRow>();
        var iqamas = new HashSet<string>(StringComparer.Ordinal);
        for (var rowNumber = headerRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var iqama = DigitsOnly(Value(row, headers, "رقم الاقامة"));
            var name = CollapseWhitespace(Value(row, headers, "الاسم"));
            if (iqama.Length == 0 && name.Length == 0) continue;
            if (iqama.Length != 10 || name.Length == 0)
            {
                issues.Add(new(rowNumber, iqama, "Error", "A 10-digit Iqama number and Arabic name are required."));
                continue;
            }
            if (!iqamas.Add(iqama))
            {
                issues.Add(new(rowNumber, iqama, "Error", "Duplicate Iqama number in the workbook."));
                continue;
            }

            var work = CollapseWhitespace(Value(row, headers, "العمل الفعلي"));
            var engagementText = Value(row, headers, "حالة الكفالة");
            result.Add(new ParsedRow(rowNumber, iqama, name,
                CollapseWhitespace(Value(row, headers, "الجنسية")),
                CollapseWhitespace(Value(row, headers, "المهنة بالاقامة")),
                CollapseWhitespace(Value(row, headers, "المسمي الوظيفي")) is { Length: > 0 } title ? title : work,
                ParseDate(Cell(row, headers, "تاريخ التعين")),
                work.Contains("اداري", StringComparison.Ordinal) || work.Contains("إداري", StringComparison.Ordinal),
                engagementText.Contains("خارج", StringComparison.Ordinal) ? EmployeeRelationshipType.OutsideRider : EmployeeRelationshipType.SponsoredInternal,
                CollapseWhitespace(Value(row, headers, "الفرع")), DigitsOnly(Value(row, headers, "هوية صاحب العمل")),
                PlatformColumns.ToDictionary(item => item.Code, item => CollapseWhitespace(Value(row, headers, item.Header)), StringComparer.Ordinal)));
        }
        return result;
    }

    private async Task<ImportCounts> ApplyRowsAsync(IReadOnlyList<ParsedRow> rows, bool validateOnly,
        List<HrExcelImportIssue> issues, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var actor = currentUser.UserId ?? SystemActorId;
        var iqamas = rows.Select(item => item.IqamaNo).ToArray();
        var employees = await dbContext.Employees.Where(item => iqamas.Contains(item.IqamaNo!))
            .ToDictionaryAsync(item => item.IqamaNo!, StringComparer.Ordinal, cancellationToken);
        var existingEmployeeIds = employees.Values.Select(item => item.Id).ToArray();
        var riders = await dbContext.RiderProfiles.Where(item => existingEmployeeIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var sponsors = await dbContext.Sponsors.ToDictionaryAsync(item => item.EmployerIdentityNumber, StringComparer.Ordinal, cancellationToken);
        var platforms = await dbContext.ClientPlatforms.ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var operatingCities = await (from city in dbContext.OperatingCities
                                     join global in dbContext.GlobalCities on city.GlobalCityId equals global.Id
                                     select new { city.Id, global.NameAr, global.NameEn }).ToArrayAsync(cancellationToken);
        var workTypes = await dbContext.OperationalWorkTypes.ToArrayAsync(cancellationToken);
        var counts = new ImportCounts();

        foreach (var row in rows)
        {
            sponsors.TryGetValue(row.SponsorIdentity, out var sponsor);
            var isEmployee = row.IsEmployee;
            var engagement = isEmployee ? EmployeeRelationshipType.SponsoredInternal : row.EngagementType;
            var city = operatingCities.FirstOrDefault(item => EqualsText(item.NameAr, row.City) || EqualsText(item.NameEn, row.City));
            var workType = workTypes.FirstOrDefault(item => EqualsText(item.NameAr, row.WorkingForMeAs) || EqualsText(item.NameEn, row.WorkingForMeAs));
            var status = engagement == EmployeeRelationshipType.SponsoredInternal && sponsor is null
                ? EmployeeStatus.Onboarding : EmployeeStatus.Active;
            if (engagement == EmployeeRelationshipType.SponsoredInternal && sponsor is null)
                issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", "Sponsor was not found; employee was imported as Onboarding."));

            if (!employees.TryGetValue(row.IqamaNo, out var employee))
            {
                employee = new Employee { IqamaNo = row.IqamaNo };
                dbContext.Employees.Add(employee);
                employees.Add(row.IqamaNo, employee);
                counts.CreatedEmployees++;
            }
            else counts.UpdatedEmployees++;

            employee.FullNameAr = row.FullNameAr;
            employee.Nationality = EmptyToNull(row.Nationality);
            employee.ResidencyProfession = EmptyToNull(row.ResidencyProfession);
            employee.WorkingForMeAs = EmptyToNull(row.WorkingForMeAs);
            employee.HireDate = row.HireDate;
            employee.IsEmployee = isEmployee;
            employee.EngagementType = engagement;
            employee.Status = status;
            employee.SponsorId = sponsor?.Id;
            employee.OperatingCityId = city?.Id;
            employee.OperationalWorkTypeId = workType?.Id;

            if (!isEmployee && !riders.TryGetValue(employee.Id, out _))
            {
                var rider = new RiderProfile { EmployeeId = employee.Id };
                dbContext.RiderProfiles.Add(rider);
                riders[employee.Id] = rider;
                counts.CreatedRiders++;
            }

            if (!isEmployee && city is not null)
            {
                foreach (var platformColumn in PlatformColumns)
                {
                    var externalId = row.PlatformIds[platformColumn.Code];
                    if (externalId.Length == 0) continue;
                    if (!platforms.TryGetValue(platformColumn.Code, out var platform))
                    {
                        issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", $"Platform {platformColumn.Code} is not configured."));
                        continue;
                    }
                    if (await dbContext.PlatformRiderAccounts.AnyAsync(item => item.ClientPlatformId == platform.Id && item.ExternalAccountId == externalId, cancellationToken))
                        continue;
                    dbContext.PlatformRiderAccounts.Add(new PlatformRiderAccount
                    {
                        ClientPlatformId = platform.Id,
                        RegisteredEmployeeId = employee.Id,
                        OperatingCityId = city.Id,
                        Code = $"{platform.Code}-{row.IqamaNo}",
                        ExternalAccountId = externalId,
                        Status = PlatformRiderAccountStatus.Available,
                        AcquisitionDate = row.HireDate
                    });
                    counts.CreatedPlatformAccounts++;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (validateOnly) await transaction.RollbackAsync(cancellationToken);
        else await transaction.CommitAsync(cancellationToken);
        return counts;
    }

    private static IXLCell? Cell(IXLRow row, IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var column) ? row.Cell(column) : null;
    private static string Value(IXLRow row, IReadOnlyDictionary<string, int> headers, string name) => CellText(Cell(row, headers, name));
    private static string CellText(IXLCell? cell) => cell?.GetFormattedString(CultureInfo.InvariantCulture).Trim() ?? string.Empty;
    private static string HeaderKey(string value) => CollapseWhitespace(value).Replace("أ", "ا", StringComparison.Ordinal).Replace("إ", "ا", StringComparison.Ordinal);
    private static string CollapseWhitespace(string value) => WhitespaceRegex().Replace(value.Trim(), " ");
    private static string DigitsOnly(string value) => string.Concat(value.Where(char.IsAsciiDigit));
    private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
    private static bool EqualsText(string? left, string? right) => !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static DateOnly? ParseDate(IXLCell? cell)
    {
        if (cell is null || cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var date)) return DateOnly.FromDateTime(date);
        return DateOnly.TryParse(CellText(cell), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HR workbook {FileName}")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "HR workbook database write failed")]
    private static partial void LogDatabaseFailure(ILogger logger, Exception exception);

    private sealed record PlatformColumn(string Code, string Header);
    private sealed record ParsedRow(int RowNumber, string IqamaNo, string FullNameAr, string Nationality,
        string ResidencyProfession, string WorkingForMeAs, DateOnly? HireDate, bool IsEmployee,
        EmployeeRelationshipType EngagementType, string City, string SponsorIdentity,
        IReadOnlyDictionary<string, string> PlatformIds);
    private sealed class ImportCounts
    {
        public int CreatedEmployees { get; set; }
        public int UpdatedEmployees { get; set; }
        public int CreatedRiders { get; set; }
        public int CreatedPlatformAccounts { get; set; }
    }
}
