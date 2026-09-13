using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HrExcelImportService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<HrExcelImportService> logger) : IHrExcelImportService
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);
    private static readonly PlatformColumn[] PlatformColumns =
    [
        new("KEETA"), new("HUNGER"), new("AMAZON"),
        new("JAHEZ"), new("NINJA"), new("SHIFTZ")
    ];
    private static readonly Dictionary<string, string> LicenseCategoryCodeAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["دراجهاليه"] = "MOTORCYCLE",
        ["دراجهناريه"] = "MOTORCYCLE",
        ["دباب"] = "MOTORCYCLE",
        ["motorcycle"] = "MOTORCYCLE",
        ["نقلخفيف"] = "LIGHT_TRANSPORT",
        ["lighttransport"] = "LIGHT_TRANSPORT",
        ["خصوصي"] = "PRIVATE",
        ["private"] = "PRIVATE",
        ["نقلثقيل"] = "HEAVY_TRANSPORT",
        ["heavytransport"] = "HEAVY_TRANSPORT"
    };
    private static readonly HashSet<string> NoLicenseValues = new(StringComparer.Ordinal)
    {
        "لايوجد", "بدون", "غيرمتوفر", "none", "nolicense", "فيانتظارالاصدار"
    };

    public async Task<Result<HrExcelImportResponse>> ImportAsync(Stream content, string fileName, bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead) return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
        try
        {
            var workbook = HrExcelImportParser.Parse(content);
            var issues = workbook.Issues.ToList();
            var counts = await ApplyRowsAsync(workbook.Rows, validateOnly, issues, cancellationToken);
            var canImport = !issues.Any(IsError);
            return Result.Success(new HrExcelImportResponse(validateOnly, canImport, !validateOnly && canImport,
                workbook.Worksheet, workbook.TotalRows,
                workbook.TotalRows - issues.Where(IsError).Select(item => item.RowNumber).Distinct().Count(),
                counts.CreatedEmployees, counts.UpdatedEmployees, counts.CreatedRiders,
                counts.CreatedResidencyDocuments, counts.UpdatedResidencyDocuments,
                counts.CreatedDriverLicenses, counts.UpdatedDriverLicenses, counts.DefaultedExpiryDates,
                counts.CreatedPlatformAccounts, 0,
                workbook.ImportedColumns, workbook.IgnoredColumns,
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

    private async Task<ImportCounts> ApplyRowsAsync(IReadOnlyList<ParsedHrImportRow> rows, bool validateOnly,
        List<HrExcelImportIssue> issues, CancellationToken cancellationToken)
    {
        var iqamas = rows.Select(item => item.IqamaNo).ToArray();
        var employees = (await dbContext.Employees.IgnoreQueryFilters()
            .Where(item => iqamas.Contains(item.IqamaNo!))
            .ToArrayAsync(cancellationToken))
            .GroupBy(item => item.IqamaNo!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.IsDeleted).ThenByDescending(item => item.CreatedAtUtc).First(),
                StringComparer.Ordinal);
        var existingEmployeeIds = employees.Values.Select(item => item.Id).ToArray();
        var riders = await dbContext.RiderProfiles.IgnoreQueryFilters().Where(item => existingEmployeeIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var riderIds = riders.Values.Select(rider => rider.Id).ToArray();
        var protectedRiderIds = await dbContext.RiderClientAssignments
            .Where(item => item.EffectiveTo == null && riderIds.Contains(item.RiderProfileId))
            .Select(item => item.RiderProfileId)
            .Concat(dbContext.RiderVehicleAssignments
                .Where(item => item.EndedAtUtc == null && riderIds.Contains(item.RiderProfileId))
                .Select(item => item.RiderProfileId))
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var sponsors = await dbContext.Sponsors.ToDictionaryAsync(item => item.EmployerIdentityNumber, StringComparer.Ordinal, cancellationToken);
        var platforms = await dbContext.ClientPlatforms.ToDictionaryAsync(item => item.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var operatingCities = await (from city in dbContext.OperatingCities
                                     join global in dbContext.GlobalCities on city.GlobalCityId equals global.Id
                                     select new { city.Id, global.NameAr, global.NameEn }).ToArrayAsync(cancellationToken);
        var workTypes = await dbContext.OperationalWorkTypes.ToArrayAsync(cancellationToken);
        var licenseCategories = await dbContext.DriverLicenseCategories
            .Where(item => item.Status == CatalogStatus.Active)
            .ToArrayAsync(cancellationToken);
        var currentLicenses = (await dbContext.EmployeeDriverLicenses
            .Where(item => existingEmployeeIds.Contains(item.EmployeeId) && item.IsCurrent)
            .ToArrayAsync(cancellationToken))
            .ToDictionary(item => (item.EmployeeId, item.DriverLicenseCategoryId));
        var residencyDocuments = (await dbContext.EmployeeDocuments
            .Where(item => existingEmployeeIds.Contains(item.EmployeeId)
                && item.DocumentTypeId == DocumentType.ResidencyPermitId
                && item.Status != DocumentStatus.Superseded)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken))
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());
        var counts = new ImportCounts();
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(RiyadhOffset).DateTime);

        foreach (var row in rows)
        {
            sponsors.TryGetValue(row.SponsorIdentity, out var sponsor);
            var isEmployee = row.IsEmployee;
            var engagement = isEmployee
                ? EmployeeRelationshipType.SponsoredInternal
                : ParseEngagement(row.SponsorshipStatus);
            var city = operatingCities.FirstOrDefault(item => EqualsText(item.NameAr, row.City) || EqualsText(item.NameEn, row.City));
            var workType = ResolveWorkType(row.ActualWork, workTypes);
            var status = ParseStatus(row.EmployeeStatus, row.RowNumber, row.IqamaNo, issues)
                ?? EmployeeStatus.Active;
            if (engagement == EmployeeRelationshipType.SponsoredInternal && sponsor is null && status == EmployeeStatus.Active)
            {
                status = EmployeeStatus.Onboarding;
                issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", "Sponsor was not found; employee was imported as Onboarding."));
            }
            if (city is null && row.City.Length > 0)
            {
                issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", $"Operating city '{row.City}' is not configured and was ignored."));
            }
            if (workType is null && row.ActualWork.Length > 0 && !IsNoOperationalWork(row.ActualWork))
            {
                issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", $"Operational work type '{row.ActualWork}' is not configured and was ignored."));
            }

            if (employees.TryGetValue(row.IqamaNo, out var existingEmployee)
                && riders.TryGetValue(existingEmployee.Id, out var existingRider)
                && protectedRiderIds.Contains(existingRider.Id)
                && (!existingEmployee.IsEmployee && isEmployee || status == EmployeeStatus.Archived))
            {
                issues.Add(new(
                    row.RowNumber,
                    row.IqamaNo,
                    "Error",
                    status == EmployeeStatus.Archived
                        ? "A rider with an active platform or vehicle assignment cannot be archived."
                        : "A rider with an active platform or vehicle assignment cannot be converted to an employee."));
                continue;
            }

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
            employee.BirthDate = row.BirthDate;
            employee.Gender = ParseGender(row.Gender, row.RowNumber, row.IqamaNo, issues);
            employee.HireDate = row.HireDate;
            employee.IsEmployee = isEmployee;
            employee.EngagementType = engagement;
            employee.Status = status;
            ApplyArchiveState(employee, status, now);
            employee.SponsorId = engagement == EmployeeRelationshipType.SponsoredInternal ? sponsor?.Id : null;
            employee.OperatingCityId = city?.Id;
            employee.OperationalWorkTypeId = workType?.Id;

            if (!residencyDocuments.TryGetValue(employee.Id, out var residencyDocument))
            {
                residencyDocument = new EmployeeDocument
                {
                    EmployeeId = employee.Id,
                    DocumentTypeId = DocumentType.ResidencyPermitId,
                    Notes = "Residency metadata created from the employee/rider Excel import; no file was supplied."
                };
                dbContext.EmployeeDocuments.Add(residencyDocument);
                residencyDocuments[employee.Id] = residencyDocument;
                counts.CreatedResidencyDocuments++;
            }
            else
            {
                counts.UpdatedResidencyDocuments++;
            }

            var residencyDates = ResolveDates(
                row.ResidencyIssueDate,
                row.ResidencyExpiryDate,
                residencyDocument.IssueDate,
                residencyDocument.ExpiryDate,
                today,
                counts);
            residencyDocument.DocumentNumber = row.IqamaNo;
            residencyDocument.IssueDate = residencyDates.IssueDate;
            residencyDocument.ExpiryDate = residencyDates.ExpiryDate;
            residencyDocument.Status = residencyDates.ExpiryDate < today
                ? DocumentStatus.Expired
                : DocumentStatus.Active;

            if (!isEmployee && !riders.TryGetValue(employee.Id, out _))
            {
                var rider = new RiderProfile { EmployeeId = employee.Id };
                dbContext.RiderProfiles.Add(rider);
                riders[employee.Id] = rider;
                counts.CreatedRiders++;
            }

            foreach (var licenseType in row.LicenseTypes)
            {
                if (NoLicenseValues.Contains(HrExcelImportParser.NormalizeKey(licenseType)))
                {
                    continue;
                }

                var category = ResolveLicenseCategory(licenseType, licenseCategories);
                if (category is null)
                {
                    issues.Add(new(row.RowNumber, row.IqamaNo, "Warning", $"Driver-license category '{licenseType}' is not configured and was ignored."));
                    continue;
                }
                if (!currentLicenses.TryGetValue((employee.Id, category.Id), out var license))
                {
                    license = new EmployeeDriverLicense
                    {
                        EmployeeId = employee.Id,
                        DriverLicenseCategoryId = category.Id,
                        BookingStatus = DriverLicenseBookingStatus.NotApplicable,
                        IssuanceStatus = DriverLicenseIssuanceStatus.Issued,
                        LicenseStatus = DriverLicenseStatus.Active,
                        IsCurrent = true,
                        Notes = "Created from the employee/rider Excel import."
                    };
                    dbContext.EmployeeDriverLicenses.Add(license);
                    currentLicenses[(employee.Id, category.Id)] = license;
                    counts.CreatedDriverLicenses++;
                }
                else
                {
                    counts.UpdatedDriverLicenses++;
                }

                var licenseDates = ResolveDates(
                    row.DriverLicenseIssueDate,
                    row.DriverLicenseExpiryDate,
                    license.IssueDate,
                    license.ExpiryDate,
                    today,
                    counts);
                license.IssueDate = licenseDates.IssueDate;
                license.ExpiryDate = licenseDates.ExpiryDate;
                license.BookingStatus = DriverLicenseBookingStatus.NotApplicable;
                license.IssuanceStatus = DriverLicenseIssuanceStatus.Issued;
                license.LicenseStatus = licenseDates.ExpiryDate < today
                    ? DriverLicenseStatus.Expired
                    : DriverLicenseStatus.Active;
                license.IsCurrent = true;
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

        if (validateOnly || issues.Any(IsError))
        {
            dbContext.ChangeTracker.Clear();
            return counts;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return counts;
    }

    private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
    private static bool EqualsText(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && HrExcelImportParser.NormalizeKey(left) == HrExcelImportParser.NormalizeKey(right);

    private static bool IsError(HrExcelImportIssue issue) =>
        string.Equals(issue.Severity, "Error", StringComparison.Ordinal);

    private static EmployeeRelationshipType ParseEngagement(string value) =>
        HrExcelImportParser.NormalizeKey(value) is "خارجالكفاله" or "ليسعليالكفاله" or "غيرعليالكفاله" or "outsiderider"
            ? EmployeeRelationshipType.OutsideRider
            : EmployeeRelationshipType.SponsoredInternal;

    private static EmployeeStatus? ParseStatus(
        string value,
        int rowNumber,
        string iqama,
        List<HrExcelImportIssue> issues)
    {
        var normalized = HrExcelImportParser.NormalizeKey(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        var status = normalized switch
        {
            "active" or "نشط" => EmployeeStatus.Active,
            "vacation" or "onleave" or "اجازه" => EmployeeStatus.OnLeave,
            "archived" or "مؤرشف" => EmployeeStatus.Archived,
            "suspended" or "موقوف" => EmployeeStatus.Suspended,
            "terminated" or "منتهي" => EmployeeStatus.Terminated,
            "fleeing" or "هارب" => EmployeeStatus.Fleeing,
            "accident" or "حادث" => EmployeeStatus.Accident,
            "sick" or "مرضي" => EmployeeStatus.Sick,
            "draft" or "مسوده" => EmployeeStatus.Draft,
            "onboarding" or "تهيئه" => EmployeeStatus.Onboarding,
            _ => (EmployeeStatus?)null
        };
        if (status is null)
        {
            issues.Add(new(rowNumber, iqama, "Warning", $"Employee status '{value}' is not recognized; the default status was used."));
        }
        return status;
    }

    private static Gender? ParseGender(
        string value,
        int rowNumber,
        string iqama,
        List<HrExcelImportIssue> issues)
    {
        var normalized = HrExcelImportParser.NormalizeKey(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        var gender = normalized switch
        {
            "ذكر" or "male" => Gender.Male,
            "انثي" or "female" => Gender.Female,
            "اخر" or "other" => Gender.Other,
            _ => (Gender?)null
        };
        if (gender is null)
        {
            issues.Add(new(rowNumber, iqama, "Warning", $"Gender '{value}' is not recognized and was ignored."));
        }
        return gender;
    }

    private static OperationalWorkType? ResolveWorkType(
        string value,
        IReadOnlyList<OperationalWorkType> workTypes)
    {
        var normalized = HrExcelImportParser.NormalizeKey(value);
        var exact = workTypes.FirstOrDefault(item =>
            HrExcelImportParser.NormalizeKey(item.Code) == normalized
            || HrExcelImportParser.NormalizeKey(item.NameAr) == normalized
            || HrExcelImportParser.NormalizeKey(item.NameEn) == normalized);
        if (exact is not null)
        {
            return exact;
        }

        var code = normalized switch
        {
            "دباب" or "دراجهناريه" or "motorcycle" => "MOTORCYCLE",
            "سياره" or "car" => "CAR",
            "اداري" or "administrative" => "ADMIN",
            _ => null
        };
        return code is null
            ? null
            : workTypes.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNoOperationalWork(string value) =>
        HrExcelImportParser.NormalizeKey(value) is "لايوجد" or "الاستعلامعنه";

    private static void ApplyArchiveState(Employee employee, EmployeeStatus status, DateTimeOffset now)
    {
        if (status == EmployeeStatus.Archived)
        {
            employee.IsDeleted = true;
            employee.DeletedAtUtc ??= now;
            employee.DeletionReason ??= "Archived by the employee/rider Excel import.";
            return;
        }

        employee.IsDeleted = false;
        employee.DeletedAtUtc = null;
        employee.DeletedByUserId = null;
        employee.DeletionReason = null;
    }

    private static ImportDates ResolveDates(
        DateOnly? importedIssueDate,
        DateOnly? importedExpiryDate,
        DateOnly? existingIssueDate,
        DateOnly? existingExpiryDate,
        DateOnly today,
        ImportCounts counts)
    {
        var defaultedExpiry = importedExpiryDate is null && existingExpiryDate is null;
        var expiryDate = importedExpiryDate ?? existingExpiryDate ?? today.AddYears(1);
        var issueDate = importedIssueDate
            ?? existingIssueDate
            ?? (expiryDate < today ? expiryDate.AddYears(-1) : today);
        if (expiryDate < issueDate)
        {
            if (importedExpiryDate is null)
            {
                expiryDate = issueDate.AddYears(1);
                defaultedExpiry = true;
            }
            else
            {
                issueDate = expiryDate.AddYears(-1);
            }
        }
        if (defaultedExpiry)
        {
            counts.DefaultedExpiryDates++;
        }

        return new ImportDates(issueDate, expiryDate);
    }

    internal static DriverLicenseCategory? ResolveLicenseCategory(
        string value,
        IReadOnlyList<DriverLicenseCategory> categories)
    {
        var normalized = HrExcelImportParser.NormalizeKey(value);
        var exact = categories.FirstOrDefault(item =>
            HrExcelImportParser.NormalizeKey(item.Code) == normalized
            || HrExcelImportParser.NormalizeKey(item.NameAr) == normalized
            || HrExcelImportParser.NormalizeKey(item.NameEn) == normalized);
        if (exact is not null)
        {
            return exact;
        }

        if (!LicenseCategoryCodeAliases.TryGetValue(normalized, out var code))
        {
            return null;
        }
        return categories.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid HR workbook {FileName}")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "HR workbook database write failed")]
    private static partial void LogDatabaseFailure(ILogger logger, Exception exception);

    private sealed record PlatformColumn(string Code);
    private sealed record ImportDates(DateOnly IssueDate, DateOnly ExpiryDate);
    private sealed class ImportCounts
    {
        public int CreatedEmployees { get; set; }
        public int UpdatedEmployees { get; set; }
        public int CreatedRiders { get; set; }
        public int CreatedResidencyDocuments { get; set; }
        public int UpdatedResidencyDocuments { get; set; }
        public int CreatedDriverLicenses { get; set; }
        public int UpdatedDriverLicenses { get; set; }
        public int DefaultedExpiryDates { get; set; }
        public int CreatedPlatformAccounts { get; set; }
    }
}
