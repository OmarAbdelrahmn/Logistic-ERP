using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Features.Hr;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed record ParsedHrImportWorkbook(
    string Worksheet,
    int TotalRows,
    IReadOnlyList<ParsedHrImportRow> Rows,
    IReadOnlyList<string> ImportedColumns,
    IReadOnlyList<string> IgnoredColumns,
    IReadOnlyList<HrExcelImportIssue> Issues);

internal sealed record ParsedHrImportRow(
    int RowNumber,
    string IqamaNo,
    string FullNameAr,
    string Nationality,
    string ResidencyProfession,
    string WorkingForMeAs,
    DateOnly? BirthDate,
    string Gender,
    DateOnly? HireDate,
    DateOnly? ResidencyIssueDate,
    DateOnly? ResidencyExpiryDate,
    DateOnly? DriverLicenseIssueDate,
    DateOnly? DriverLicenseExpiryDate,
    bool IsEmployee,
    string SponsorshipStatus,
    string EmployeeStatus,
    string City,
    string SponsorIdentity,
    string ActualWork,
    IReadOnlyList<string> LicenseTypes,
    IReadOnlyDictionary<string, string> PlatformIds);

internal static partial class HrExcelImportParser
{
    private const int MaximumRows = 5_000;
    private const string Iqama = "iqama";
    private const string Name = "name";
    private const string Gender = "gender";
    private const string Nationality = "nationality";
    private const string Profession = "profession";
    private const string ResidencyProfession = "residency-profession";
    private const string JobTitle = "job-title";
    private const string ActualWork = "actual-work";
    private const string BirthDate = "birth-date";
    private const string HireDate = "hire-date";
    private const string ResidencyIssueDate = "residency-issue-date";
    private const string ResidencyExpiryDate = "residency-expiry-date";
    private const string DriverLicenseIssueDate = "driver-license-issue-date";
    private const string DriverLicenseExpiryDate = "driver-license-expiry-date";
    private const string SponsorIdentity = "sponsor-identity";
    private const string SponsorshipStatus = "sponsorship-status";
    private const string EmployeeStatus = "employee-status";
    private const string City = "city";
    private const string LicenseType = "license-type";

    private static readonly string[] RequiredFields = [Iqama, Name];
    private static readonly PlatformColumn[] PlatformColumns =
    [
        new("KEETA", "platform-keeta"),
        new("HUNGER", "platform-hunger"),
        new("AMAZON", "platform-amazon"),
        new("JAHEZ", "platform-jahez"),
        new("NINJA", "platform-ninja"),
        new("SHIFTZ", "platform-shiftz")
    ];

    private static readonly Dictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["رقمالاقامه"] = Iqama,
            ["الاسم"] = Name,
            ["الجنس"] = Gender,
            ["الجنسيه"] = Nationality,
            ["المهنه"] = Profession,
            ["المهنهبالاقامه"] = ResidencyProfession,
            ["المسميالوظيفي"] = JobTitle,
            ["العملالفعلي"] = ActualWork,
            ["تاريخالميلاد"] = BirthDate,
            ["تاريخالتعيين"] = HireDate,
            ["تاريخالتعين"] = HireDate,
            ["تاريخاصدارالاقامه"] = ResidencyIssueDate,
            ["تاريخانتهاءالاقامه"] = ResidencyExpiryDate,
            ["تاريخاصدارالرخصه"] = DriverLicenseIssueDate,
            ["تاريخانتهاءالرخصه"] = DriverLicenseExpiryDate,
            ["تاريخاصداررخصهالقياده"] = DriverLicenseIssueDate,
            ["تاريخانتهاءرخصهالقياده"] = DriverLicenseExpiryDate,
            ["اصدارالرخصه"] = DriverLicenseIssueDate,
            ["انتهاءالرخصه"] = DriverLicenseExpiryDate,
            ["اصداررخصهالقياده"] = DriverLicenseIssueDate,
            ["انتهاءرخصهالقياده"] = DriverLicenseExpiryDate,
            ["رقمصاحبالعمل"] = SponsorIdentity,
            ["هويهصاحبالعمل"] = SponsorIdentity,
            ["حالهالكفاله"] = SponsorshipStatus,
            ["حالهالموظف"] = EmployeeStatus,
            ["الفرع"] = City,
            ["الفرعالمدينه"] = City,
            ["نوعالرخصه"] = LicenseType,
            ["ايديكيتا"] = "platform-keeta",
            ["ايديهنقر"] = "platform-hunger",
            ["ايديامازون"] = "platform-amazon",
            ["ايديجاهز"] = "platform-jahez",
            ["ايدينينجا"] = "platform-ninja",
            ["ايديشفز"] = "platform-shiftz"
        };

    public static ParsedHrImportWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        var used = worksheet?.RangeUsed(XLCellsUsedOptions.Contents);
        if (worksheet is null || used is null)
        {
            throw new InvalidDataException("The workbook does not contain a populated worksheet.");
        }

        var headerRow = worksheet.RowsUsed().Take(10).FirstOrDefault(row => RequiredFields.All(required =>
            row.CellsUsed().Any(cell => ResolveField(CellText(cell)) == required)));
        if (headerRow is null)
        {
            throw new InvalidDataException("The workbook does not contain the required HR headers.");
        }

        if (used.LastRow().RowNumber() - headerRow.RowNumber() > MaximumRows)
        {
            throw new InvalidDataException($"The workbook contains more than {MaximumRows} data rows.");
        }

        var headerCells = headerRow.CellsUsed()
            .Select(cell => new HeaderCell(
                cell.Address.ColumnNumber,
                CollapseWhitespace(CellText(cell)),
                ResolveField(CellText(cell))))
            .Where(item => item.Original.Length > 0)
            .ToArray();
        var headers = headerCells
            .Where(item => item.Field is not null)
            .GroupBy(item => item.Field!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().ColumnNumber, StringComparer.Ordinal);
        var issues = new List<HrExcelImportIssue>();
        var rows = new List<ParsedHrImportRow>();
        var iqamas = new HashSet<string>(StringComparer.Ordinal);
        var totalRows = 0;

        for (var rowNumber = headerRow.RowNumber() + 1; rowNumber <= used.LastRow().RowNumber(); rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (!row.Cells(used.FirstColumn().ColumnNumber(), used.LastColumn().ColumnNumber()).Any(cell => !cell.IsEmpty()))
            {
                continue;
            }

            totalRows++;
            var iqama = DigitsOnly(Value(row, headers, Iqama));
            var name = CollapseWhitespace(Value(row, headers, Name));
            var rowHasError = false;
            if (iqama.Length != 10 || name.Length == 0)
            {
                issues.Add(new(rowNumber, EmptyToNull(iqama), "Error", "A 10-digit Iqama number and a name are required."));
                rowHasError = true;
            }
            else if (!iqamas.Add(iqama))
            {
                issues.Add(new(rowNumber, iqama, "Error", "Duplicate Iqama number in the workbook."));
                rowHasError = true;
            }

            rowHasError |= ValidateLength(rowNumber, iqama, name, 200, "Name", issues);
            var nationality = CollapseWhitespace(Value(row, headers, Nationality));
            rowHasError |= ValidateLength(rowNumber, iqama, nationality, 100, "Nationality", issues);
            var residencyProfession = CollapseWhitespace(Value(row, headers, ResidencyProfession));
            if (residencyProfession.Length == 0)
            {
                residencyProfession = CollapseWhitespace(Value(row, headers, Profession));
            }
            rowHasError |= ValidateLength(rowNumber, iqama, residencyProfession, 200, "Residency profession", issues);
            var actualWork = CollapseWhitespace(Value(row, headers, ActualWork));
            var workingForMeAs = CollapseWhitespace(Value(row, headers, JobTitle));
            if (workingForMeAs.Length == 0)
            {
                workingForMeAs = actualWork;
            }
            rowHasError |= ValidateLength(rowNumber, iqama, workingForMeAs, 200, "Job title", issues);
            var birthDate = ParseOptionalDate(rowNumber, iqama, "Birth date", Cell(row, headers, BirthDate), issues, ref rowHasError);
            var hireDate = ParseOptionalDate(rowNumber, iqama, "Hire date", Cell(row, headers, HireDate), issues, ref rowHasError);
            var residencyIssueDate = ParseOptionalDate(rowNumber, iqama, "Residency issue date", Cell(row, headers, ResidencyIssueDate), issues, ref rowHasError);
            var residencyExpiryDate = ParseOptionalDate(rowNumber, iqama, "Residency expiry date", Cell(row, headers, ResidencyExpiryDate), issues, ref rowHasError);
            var driverLicenseIssueDate = ParseOptionalDate(rowNumber, iqama, "Driver-license issue date", Cell(row, headers, DriverLicenseIssueDate), issues, ref rowHasError);
            var driverLicenseExpiryDate = ParseOptionalDate(rowNumber, iqama, "Driver-license expiry date", Cell(row, headers, DriverLicenseExpiryDate), issues, ref rowHasError);
            rowHasError |= ValidateDateRange(rowNumber, iqama, "Residency", residencyIssueDate, residencyExpiryDate, issues);
            rowHasError |= ValidateDateRange(rowNumber, iqama, "Driver-license", driverLicenseIssueDate, driverLicenseExpiryDate, issues);

            if (rowHasError)
            {
                continue;
            }

            rows.Add(new ParsedHrImportRow(
                rowNumber,
                iqama,
                name,
                nationality,
                residencyProfession,
                workingForMeAs,
                birthDate,
                CollapseWhitespace(Value(row, headers, Gender)),
                hireDate,
                residencyIssueDate,
                residencyExpiryDate,
                driverLicenseIssueDate,
                driverLicenseExpiryDate,
                IsAdministrative(actualWork, workingForMeAs),
                CollapseWhitespace(Value(row, headers, SponsorshipStatus)),
                CollapseWhitespace(Value(row, headers, EmployeeStatus)),
                CollapseWhitespace(Value(row, headers, City)),
                DigitsOnly(Value(row, headers, SponsorIdentity)),
                actualWork,
                SplitLicenseTypes(Value(row, headers, LicenseType)),
                PlatformColumns.ToDictionary(
                    item => item.Code,
                    item => CollapseWhitespace(Value(row, headers, item.Field)),
                    StringComparer.Ordinal)));
        }

        if (totalRows == 0)
        {
            throw new InvalidDataException("The workbook does not contain any data rows.");
        }

        return new ParsedHrImportWorkbook(
            worksheet.Name,
            totalRows,
            rows,
            headerCells.Where(item => item.Field is not null).Select(item => item.Original).Order(StringComparer.Ordinal).ToArray(),
            headerCells.Where(item => item.Field is null).Select(item => item.Original).Order(StringComparer.Ordinal).ToArray(),
            issues);
    }

    internal static IReadOnlyList<string> SplitLicenseTypes(string value) => LicenseSeparatorRegex()
        .Split(value.Trim())
        .Select(CollapseWhitespace)
        .Where(item => item.Length > 0)
        .DistinctBy(NormalizeKey, StringComparer.Ordinal)
        .ToArray();

    internal static string NormalizeKey(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.Format or UnicodeCategory.Control or UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var mapped = character switch
            {
                '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1', '\u0662' or '\u06F2' => '2',
                '\u0663' or '\u06F3' => '3', '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
                '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7', '\u0668' or '\u06F8' => '8',
                '\u0669' or '\u06F9' => '9',
                '\u0622' or '\u0623' or '\u0625' => '\u0627',
                '\u0649' => '\u064A',
                '\u0629' => '\u0647',
                _ => character
            };
            if (char.IsLetterOrDigit(mapped))
            {
                builder.Append(mapped);
            }
        }

        return builder.ToString();
    }

    private static string? ResolveField(string value) => HeaderAliases.GetValueOrDefault(NormalizeKey(value));

    private static bool IsAdministrative(string actualWork, string jobTitle)
    {
        var workKey = NormalizeKey(actualWork);
        var titleKey = NormalizeKey(jobTitle);
        return workKey.Contains("اداري", StringComparison.Ordinal)
            || titleKey.Contains("اداري", StringComparison.Ordinal)
            || workKey.Contains("administrative", StringComparison.Ordinal);
    }

    private static bool ValidateLength(
        int rowNumber,
        string iqama,
        string value,
        int maximumLength,
        string fieldName,
        List<HrExcelImportIssue> issues)
    {
        if (value.Length <= maximumLength)
        {
            return false;
        }

        issues.Add(new(rowNumber, EmptyToNull(iqama), "Error", $"{fieldName} exceeds {maximumLength} characters."));
        return true;
    }

    private static DateOnly? ParseOptionalDate(
        int rowNumber,
        string iqama,
        string fieldName,
        IXLCell? cell,
        List<HrExcelImportIssue> issues,
        ref bool rowHasError)
    {
        if (cell is null || cell.IsEmpty())
        {
            return null;
        }
        if (cell.TryGetValue<DateTime>(out var date))
        {
            return DateOnly.FromDateTime(date);
        }

        var value = CellText(cell);
        if (DateOnly.TryParseExact(
            value,
            ["yyyy-MM-dd", "yyyy/M/d", "d/M/yyyy", "dd/MM/yyyy"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed))
        {
            return parsed;
        }

        issues.Add(new(rowNumber, EmptyToNull(iqama), "Error", $"{fieldName} is not a valid date."));
        rowHasError = true;
        return null;
    }

    private static bool ValidateDateRange(
        int rowNumber,
        string iqama,
        string fieldName,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        List<HrExcelImportIssue> issues)
    {
        if (issueDate is null || expiryDate is null || expiryDate >= issueDate)
        {
            return false;
        }

        issues.Add(new(rowNumber, EmptyToNull(iqama), "Error", $"{fieldName} expiry date cannot be before its issue date."));
        return true;
    }

    private static IXLCell? Cell(IXLRow row, IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var column) ? row.Cell(column) : null;

    private static string Value(IXLRow row, IReadOnlyDictionary<string, int> headers, string name) =>
        CellText(Cell(row, headers, name));

    private static string CellText(IXLCell? cell) =>
        cell?.GetFormattedString(CultureInfo.InvariantCulture).Trim() ?? string.Empty;

    private static string CollapseWhitespace(string value) => WhitespaceRegex().Replace(value.Trim(), " ");

    private static string DigitsOnly(string value) =>
        string.Concat(NormalizeDigits(value).Where(char.IsAsciiDigit));

    private static string NormalizeDigits(string value) => string.Concat(value.Select(character => character switch
    {
        '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1', '\u0662' or '\u06F2' => '2',
        '\u0663' or '\u06F3' => '3', '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
        '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7', '\u0668' or '\u06F8' => '8',
        '\u0669' or '\u06F9' => '9',
        _ => character
    }));

    private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;

    [GeneratedRegex(@"\s*(?:\+|،|,|/|&|;|\r?\n|\s+و\s+)\s*", RegexOptions.CultureInvariant)]
    private static partial Regex LicenseSeparatorRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    private sealed record HeaderCell(int ColumnNumber, string Original, string? Field);
    private sealed record PlatformColumn(string Code, string Field);
}
