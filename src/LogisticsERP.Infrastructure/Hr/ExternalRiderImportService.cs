using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Telecom;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class ExternalRiderImportService(
    ApplicationDbContext dbContext,
    ILogger<ExternalRiderImportService> logger) : IExternalRiderImportService
{
    public async Task<Result<ExternalRiderImportResponse>> ImportAsync(
        Stream content, string fileName, bool validateOnly, CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            return Result.Failure<ExternalRiderImportResponse>(HrImportErrors.InvalidExternalRiderWorkbook);

        try
        {
            return await ProcessAsync(ExternalRiderSpreadsheetParser.Parse(content), validateOnly, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<ExternalRiderImportResponse>(HrImportErrors.InvalidExternalRiderWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<ExternalRiderImportResponse>(HrImportErrors.ExternalRiderImportFailed);
        }
    }

    private async Task<Result<ExternalRiderImportResponse>> ProcessAsync(
        ParsedExternalRiderWorkbook workbook, bool validateOnly, CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var iqamas = workbook.Rows.Select(row => row.IqamaNo).Distinct().ToArray();
        var employees = await dbContext.Employees.IgnoreQueryFilters()
            .Where(employee => employee.IqamaNo != null && iqamas.Contains(employee.IqamaNo))
            .ToArrayAsync(cancellationToken);
        var employeeByIqama = employees.GroupBy(employee => employee.IqamaNo!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key,
                group => group.OrderBy(employee => employee.IsDeleted).ThenByDescending(employee => employee.CreatedAtUtc).First(),
                StringComparer.Ordinal);
        var employeeIds = employees.Select(employee => employee.Id).ToArray();
        var profileEmployeeIds = await dbContext.RiderProfiles.IgnoreQueryFilters()
            .Where(profile => employeeIds.Contains(profile.EmployeeId))
            .Select(profile => profile.EmployeeId)
            .ToArrayAsync(cancellationToken);
        var profilesByEmployeeId = profileEmployeeIds.ToHashSet();
        var cities = await (from operating in dbContext.OperatingCities.AsNoTracking()
                            join global in dbContext.GlobalCities.AsNoTracking() on operating.GlobalCityId equals global.Id
                            where operating.Status == CatalogStatus.Active && global.Status == CatalogStatus.Active
                            select new CityLookup(operating.Id, global.NameAr, global.NameEn))
            .ToArrayAsync(cancellationToken);

        var plans = new List<ExternalRiderPlan>();
        var previews = new List<ExternalRiderImportRowPreview>();
        foreach (var row in workbook.Rows)
        {
            var city = cities.FirstOrDefault(item => Same(item.NameAr, row.OperatingCity)
                                                    || Same(item.NameEn, row.OperatingCity));
            if (city is null)
            {
                issues.Add(new(row.RowNumber, row.IqamaNo, "Error",
                    $"مدينة التشغيل '{row.OperatingCity}' غير موجودة أو غير مفعلة."));
                continue;
            }

            employeeByIqama.TryGetValue(row.IqamaNo, out var employee);
            if (employee?.IsDeleted == true)
            {
                issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "يوجد سجل محذوف بنفس رقم الإقامة ويتطلب مراجعة يدوية."));
                continue;
            }
            if (employee?.IsEmployee == true)
            {
                issues.Add(new(row.RowNumber, row.IqamaNo, "Error", "رقم الإقامة مرتبط بموظف داخلي ولا يمكن تحويله تلقائياً إلى سائق خارجي."));
                continue;
            }

            var hasProfile = employee is not null && profilesByEmployeeId.Contains(employee.Id);
            plans.Add(new(row, city, employee, !hasProfile));
            previews.Add(new(row.RowNumber, row.IqamaNo, row.FullNameAr, row.Nationality,
                row.PhoneNumber, city.Id, city.NameAr, employee is null, !hasProfile));
        }

        if (!validateOnly)
        {
            foreach (var plan in plans)
            {
                var employee = plan.Employee;
                if (employee is null)
                {
                    employee = new Employee { IqamaNo = plan.Row.IqamaNo };
                    dbContext.Employees.Add(employee);
                }
                employee.FullNameAr = plan.Row.FullNameAr;
                employee.Nationality = plan.Row.Nationality;
                employee.PrimaryPhone = plan.Row.PhoneNumber;
                employee.OperatingCityId = plan.City.Id;
                employee.IsEmployee = false;
                employee.EngagementType = EmployeeRelationshipType.OutsideRider;
                employee.Status = EmployeeStatus.Active;
                employee.SponsorId = null;
                if (plan.CreateProfile)
                    dbContext.RiderProfiles.Add(new RiderProfile
                    {
                        EmployeeId = employee.Id,
                        OperationalNotes = "Created by external-rider Excel import."
                    });
            }
            if (plans.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            dbContext.ChangeTracker.Clear();
        }

        var errorRows = issues.Where(IsError).Select(issue => issue.RowNumber).Distinct().Count();
        var createdEmployees = plans.Count(plan => plan.Employee is null);
        var updatedEmployees = plans.Count - createdEmployees;
        return Result.Success(new ExternalRiderImportResponse(
            validateOnly,
            plans.Count > 0,
            !validateOnly && plans.Count > 0,
            workbook.Worksheet,
            workbook.TotalRows,
            workbook.TotalRows - errorRows,
            !validateOnly ? createdEmployees : 0,
            !validateOnly ? updatedEmployees : 0,
            !validateOnly ? plans.Count(plan => plan.CreateProfile) : 0,
            previews.OrderBy(row => row.RowNumber).ToArray(),
            issues.OrderBy(issue => issue.RowNumber).ThenBy(issue => issue.Severity).ToArray()));
    }

    private static bool Same(string value, string imported) =>
        HrExcelImportParser.NormalizeKey(value) == HrExcelImportParser.NormalizeKey(imported);
    private static bool IsError(HrExcelImportIssue issue) =>
        string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(LogLevel.Warning, "External-rider workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);
    [LoggerMessage(LogLevel.Error, "External-rider workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);

    private sealed record CityLookup(Guid Id, string NameAr, string NameEn);
    private sealed record ExternalRiderPlan(
        ParsedExternalRiderRow Row, CityLookup City, Employee? Employee, bool CreateProfile);
}

internal sealed record ParsedExternalRiderRow(
    int RowNumber,
    string IqamaNo,
    string FullNameAr,
    string? Nationality,
    string PhoneNumber,
    string OperatingCity);
internal sealed record ParsedExternalRiderWorkbook(
    string Worksheet,
    int TotalRows,
    IReadOnlyList<ParsedExternalRiderRow> Rows,
    IReadOnlyList<HrExcelImportIssue> Issues);

internal static class ExternalRiderSpreadsheetParser
{
    private const int MaximumRows = 5_000;
    private const string Iqama = "iqama";
    private const string Name = "name";
    private const string Nationality = "nationality";
    private const string Phone = "phone";
    private const string City = "city";
    private static readonly Dictionary<string, string> Headers = new(StringComparer.Ordinal)
    {
        [Key("رقم الإقامة")] = Iqama,
        [Key("رقم الاقامة")] = Iqama,
        [Key("Iqama Number")] = Iqama,
        [Key("الاسم")] = Name,
        [Key("Name")] = Name,
        [Key("الجنسية")] = Nationality,
        [Key("Nationality")] = Nationality,
        [Key("رقم الجوال")] = Phone,
        [Key("Mobile Number")] = Phone,
        [Key("مدينة التشغيل")] = City,
        [Key("Operating City")] = City
    };

    public static ParsedExternalRiderWorkbook Parse(Stream content)
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The workbook has no worksheet.");
        var range = worksheet.RangeUsed(XLCellsUsedOptions.Contents)
            ?? throw new InvalidDataException("The worksheet is empty.");
        var header = range.FirstRow();
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cell in header.Cells())
            if (Headers.TryGetValue(Key(Text(cell)), out var field)) columns.TryAdd(field, cell.Address.ColumnNumber);
        if (!columns.TryGetValue(Iqama, out var iqamaColumn)
            || !columns.TryGetValue(Name, out var nameColumn)
            || !columns.TryGetValue(Phone, out var phoneColumn)
            || !columns.TryGetValue(City, out var cityColumn))
            throw new InvalidDataException("Required external-rider columns are missing.");
        var firstDataRow = header.RowNumber() + 1;
        if (range.LastRow().RowNumber() - firstDataRow + 1 > MaximumRows)
            throw new InvalidDataException($"The workbook contains more than {MaximumRows} rows.");

        var rows = new List<ParsedExternalRiderRow>();
        var issues = new List<HrExcelImportIssue>();
        var seenIqamas = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        for (var rowNumber = firstDataRow; rowNumber <= range.LastRow().RowNumber(); rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var iqama = Digits(Text(row.Cell(iqamaColumn)));
            var name = Text(row.Cell(nameColumn));
            var nationality = columns.TryGetValue(Nationality, out var nationalityColumn)
                ? NullIfEmpty(Text(row.Cell(nationalityColumn))) : null;
            var rawPhone = Text(row.Cell(phoneColumn));
            var city = Text(row.Cell(cityColumn));
            if (iqama.Length == 0 && name.Length == 0 && rawPhone.Length == 0 && city.Length == 0 && nationality is null) continue;
            total++;
            var invalid = false;
            if (iqama.Length != 10 || !iqama.All(char.IsAsciiDigit))
            { issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "رقم الإقامة يجب أن يتكون من 10 أرقام.")); invalid = true; }
            else if (!seenIqamas.Add(iqama))
            { issues.Add(new(rowNumber, iqama, "Error", "رقم الإقامة مكرر داخل الملف.")); invalid = true; }
            if (name.Length == 0 || name.Length > 200)
            { issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "الاسم مطلوب ويجب ألا يتجاوز 200 حرف.")); invalid = true; }
            if (!PhoneSimRules.TryNormalizePhoneNumber(rawPhone, out var phone))
            { issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "رقم الجوال غير صالح.")); invalid = true; }
            if (city.Length == 0)
            { issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "مدينة التشغيل مطلوبة.")); invalid = true; }
            if (nationality?.Length > 100)
            { issues.Add(new(rowNumber, NullIfEmpty(iqama), "Error", "الجنسية يجب ألا تتجاوز 100 حرف.")); invalid = true; }
            if (!invalid) rows.Add(new(rowNumber, iqama, name, nationality, phone, city));
        }
        if (total == 0) throw new InvalidDataException("The worksheet has no external-rider rows.");
        return new(worksheet.Name, total, rows, issues);
    }

    private static string Text(IXLCell cell) => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();
    private static string Key(string value) => HrExcelImportParser.NormalizeKey(value);
    private static string Digits(string value) => string.Concat(value.Select(character => character switch
    {
        '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
        '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
        '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
        '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
        '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9', _ => character
    }));
    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
