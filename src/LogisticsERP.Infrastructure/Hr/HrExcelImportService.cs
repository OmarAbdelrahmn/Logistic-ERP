using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed partial class HrExcelImportService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ISensitiveValueProtector sensitiveValueProtector,
    TimeProvider timeProvider,
    ILogger<HrExcelImportService> logger) : IHrExcelImportService
{
    private const int MaximumRows = 5_000;
    private static readonly Guid AnonymousImportActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000001");

    private static readonly string[] RequiredHeaders = ["الرقم الوظيفي", "الاسم"];

    private static readonly FrozenPlatform[] PlatformDefinitions =
    [
        new("KEETA", "كيتا", "Keeta", "ايدي كيتا", ["كيتا"]),
        new("HUNGER", "هنقرستيشن", "HungerStation", "ايدي هنقر", ["هنقر", "هانجر"]),
        new("AMAZON", "أمازون", "Amazon", "ايدي امازون", ["امازون", "أمازون"]),
        new("JAHEZ", "جاهز", "Jahez", "ايدي جاهز", ["جاهز"]),
        new("NINJA", "نينجا", "Ninja", "ايدي نينجا", ["نينجا"]),
        new("SHIFTZ", "شفز", "Shiftz", "ايدي شفز", ["شفز", "شيفز"])
    ];

    private static readonly HashSet<string> DirectlyImportedHeaders = new(StringComparer.Ordinal)
    {
        "الرقم الوظيفي", "العمل الفعلي", "رقم الاقامة", "الاسم", "تاريخ التعين", "الجنسية",
        "المهنة بالاقامة", "المسمي الوظيفي", "الفرع", "تاريخ انتهاء الاقامة", "صلاحية الاقامة",
        "هوية صاحب العمل", "نوع الرخصة", "حالة الحجز", "حالة الكفالة", "تطبيق العمل",
        "ايدي كيتا", "ايدي هنقر", "ايدي امازون", "ايدي جاهز", "ايدي نينجا", "ايدي شفز"
    };

    public async Task<Result<HrExcelImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        bool validateOnly,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
        }
        var actorUserId = currentUser.UserId ?? AnonymousImportActorId;

        try
        {
            using var workbook = new XLWorkbook(content);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet is null)
            {
                return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
            }

            var headerRow = worksheet.RowsUsed().Take(10).FirstOrDefault(row =>
                RequiredHeaders.All(required => row.CellsUsed().Any(cell => HeaderKey(CellText(cell)) == HeaderKey(required))));
            var usedRange = worksheet.RangeUsed(XLCellsUsedOptions.Contents);
            if (headerRow is null || usedRange is null || usedRange.RowCount() > MaximumRows)
            {
                return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
            }

            var headers = headerRow.CellsUsed()
                .Select(cell => (Column: cell.Address.ColumnNumber, Name: HeaderKey(CellText(cell))))
                .Where(item => item.Name.Length > 0)
                .GroupBy(item => item.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Column, StringComparer.Ordinal);
            if (RequiredHeaders.Any(required => !headers.ContainsKey(HeaderKey(required))))
            {
                return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
            }

            var issues = new List<HrExcelImportIssue>();
            var parsedRows = ParseRows(worksheet, headerRow.RowNumber(), usedRange.LastRow().RowNumber(), headers, issues);
            if (parsedRows.Count == 0)
            {
                return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
            }

            var result = await ApplyRowsAsync(parsedRows, actorUserId, validateOnly, issues, cancellationToken);
            var workbookHeaders = headers.Keys.ToHashSet(StringComparer.Ordinal);
            var importedColumns = workbookHeaders.Where(DirectlyImportedHeaders.Contains).Order(StringComparer.Ordinal).ToArray();
            var ignoredColumns = workbookHeaders.Where(header => !DirectlyImportedHeaders.Contains(header)).Order(StringComparer.Ordinal).ToArray();

            return Result.Success(new HrExcelImportResponse(
                validateOnly,
                worksheet.Name,
                parsedRows.Count,
                parsedRows.Count(row => !issues.Any(issue => issue.RowNumber == row.RowNumber && issue.Severity == "Error")),
                result.CreatedEmployees,
                result.UpdatedEmployees,
                result.CreatedRiders,
                result.CreatedResidencyPermits,
                result.CreatedDriverLicenses,
                result.CreatedPlatformAccounts,
                result.CreatedPlatformAssignments,
                importedColumns,
                ignoredColumns,
                issues.OrderBy(issue => issue.RowNumber).ThenBy(issue => issue.Severity).ToArray()));
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception.Message);
            return Result.Failure<HrExcelImportResponse>(HrImportErrors.InvalidWorkbook);
        }
        catch (DbUpdateException exception)
        {
            var reason = DescribeDatabaseFailure(exception);
            LogImportFailure(logger, reason);
            return Result.Failure<HrExcelImportResponse>(new OperationError(
                "hr_import.database_write_failed",
                reason,
                ErrorType.Conflict));
        }
    }

    private static List<ParsedRow> ParseRows(
        IXLWorksheet worksheet,
        int headerRowNumber,
        int lastRowNumber,
        IReadOnlyDictionary<string, int> headers,
        List<HrExcelImportIssue> issues)
    {
        var result = new List<ParsedRow>();
        var employeeNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var employeeNumber = Value(row, headers, "الرقم الوظيفي");
            var name = Value(row, headers, "الاسم");
            if (string.IsNullOrWhiteSpace(employeeNumber) && string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            employeeNumber = NormalizeIdentifier(employeeNumber);
            if (string.IsNullOrWhiteSpace(employeeNumber) || string.IsNullOrWhiteSpace(name))
            {
                issues.Add(new(rowNumber, employeeNumber, "Error", "Employee number and Arabic name are required."));
                continue;
            }
            if (!employeeNumbers.Add(employeeNumber))
            {
                issues.Add(new(rowNumber, employeeNumber, "Error", "Duplicate employee number in the workbook; the later row was skipped."));
                continue;
            }

            var nationality = MapNationality(Value(row, headers, "الجنسية"));
            if (nationality is null && HasValue(row, headers, "الجنسية"))
            {
                issues.Add(new(rowNumber, employeeNumber, "Warning", "Nationality was not recognized as an ISO country code and was not imported."));
            }

            var workTypeText = Value(row, headers, "العمل الفعلي");
            var workTypeId = MapWorkType(workTypeText);
            if (workTypeId is null && !string.IsNullOrWhiteSpace(workTypeText))
            {
                issues.Add(new(rowNumber, employeeNumber, "Warning", $"Operational work value '{workTypeText}' is not a supported work type and was ignored."));
            }

            var cityText = Value(row, headers, "الفرع");
            var (globalCityId, operatingCityId) = MapCity(cityText);
            if (operatingCityId is null && !string.IsNullOrWhiteSpace(cityText))
            {
                issues.Add(new(rowNumber, employeeNumber, "Warning", $"Operating city '{cityText}' is not configured and was ignored."));
            }

            var sponsorIdentity = DigitsOnly(Value(row, headers, "هوية صاحب العمل"));
            var sponsorshipText = Value(row, headers, "حالة الكفالة");
            var relationship = sponsorshipText.Contains("خارج", StringComparison.Ordinal)
                ? EmployeeRelationshipType.OutsideRider
                : EmployeeRelationshipType.SponsoredInternal;

            result.Add(new ParsedRow(
                rowNumber,
                employeeNumber,
                CollapseWhitespace(name),
                ParseDate(Cell(row, headers, "تاريخ التعين")),
                nationality,
                CollapseWhitespace(Value(row, headers, "المهنة بالاقامة")),
                CollapseWhitespace(Value(row, headers, "المسمي الوظيفي")),
                workTypeId,
                globalCityId,
                operatingCityId,
                sponsorIdentity,
                relationship,
                NormalizeIdentifier(Value(row, headers, "رقم الاقامة")),
                ParseDate(Cell(row, headers, "تاريخ انتهاء الاقامة")),
                Value(row, headers, "صلاحية الاقامة"),
                Value(row, headers, "نوع الرخصة"),
                Value(row, headers, "حالة الحجز"),
                PlatformDefinitions.ToDictionary(
                    platform => platform.Code,
                    platform => ParseExternalAccountId(Value(row, headers, platform.Header)),
                    StringComparer.Ordinal),
                Value(row, headers, "تطبيق العمل")));
        }
        return result;
    }

    private async Task<ImportCounts> ApplyRowsAsync(
        IReadOnlyList<ParsedRow> rows,
        Guid actorUserId,
        bool validateOnly,
        List<HrExcelImportIssue> issues,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var numbers = rows.Select(row => row.EmployeeNumber).ToArray();
        var employees = await dbContext.Employees
            .Where(employee => numbers.Contains(employee.EmployeeNumber))
            .ToDictionaryAsync(employee => employee.EmployeeNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingIds = employees.Values.Select(employee => employee.Id).ToArray();
        var sponsors = await dbContext.Sponsors.ToDictionaryAsync(sponsor => sponsor.EmployerIdentityNumber, StringComparer.Ordinal, cancellationToken);
        var jobTitles = (await dbContext.JobTitles.ToArrayAsync(cancellationToken))
            .ToDictionary(item => HrServiceSupport.NormalizeText(item.NameAr), StringComparer.Ordinal);
        var professions = (await dbContext.ResidencyProfessions.ToArrayAsync(cancellationToken))
            .ToDictionary(item => HrServiceSupport.NormalizeText(item.NameAr), StringComparer.Ordinal);
        var riderProfiles = await dbContext.RiderProfiles.Where(item => existingIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var sponsoredDetails = await dbContext.SponsoredInternalDetails.Where(item => existingIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var outsideDetails = await dbContext.OutsideRiderDetails.Where(item => existingIds.Contains(item.EmployeeId))
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentStatuses = await dbContext.EmployeeStatusPeriods.Where(item => existingIds.Contains(item.EmployeeId) && item.EffectiveTo == null)
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentRelationships = await dbContext.EmployeeRelationshipPeriods.Where(item => existingIds.Contains(item.EmployeeId) && item.EffectiveTo == null)
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentSponsorships = await dbContext.EmployeeSponsorshipPeriods.Where(item => existingIds.Contains(item.EmployeeId) && item.EffectiveTo == null)
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentJobs = await dbContext.EmployeeJobTitlePeriods.Where(item => existingIds.Contains(item.EmployeeId) && item.EffectiveTo == null)
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentPermits = await dbContext.EmployeeResidencyPermits.Where(item => existingIds.Contains(item.EmployeeId) && item.IsCurrent)
            .ToDictionaryAsync(item => item.EmployeeId, cancellationToken);
        var currentLicenses = (await dbContext.EmployeeDriverLicenses
                .Where(item => existingIds.Contains(item.EmployeeId) && item.IsCurrent)
                .ToArrayAsync(cancellationToken))
            .ToDictionary(item => (item.EmployeeId, item.DriverLicenseCategoryId));

        var platformBundle = await LoadPlatformsAsync(rows, cancellationToken);
        var counts = new ImportCounts();

        foreach (var row in rows)
        {
            if (issues.Any(issue => issue.RowNumber == row.RowNumber && issue.Severity == "Error"))
            {
                continue;
            }

            if (!employees.TryGetValue(row.EmployeeNumber, out var employee))
            {
                employee = new Employee
                {
                    EmployeeNumber = row.EmployeeNumber,
                    FullNameAr = row.FullNameAr,
                    NormalizedNameAr = HrServiceSupport.NormalizeText(row.FullNameAr),
                    HireDate = row.HireDate,
                    NationalityCountryCode = row.NationalityCountryCode,
                    CurrentStatus = EmployeeStatus.Active,
                    CurrentRelationshipType = row.RelationshipType
                };
                employees.Add(employee.EmployeeNumber, employee);
                dbContext.Employees.Add(employee);
                counts.CreatedEmployees++;

                var effectiveFrom = row.HireDate ?? today;
                var status = new EmployeeStatusPeriod
                {
                    EmployeeId = employee.Id,
                    Status = EmployeeStatus.Active,
                    EffectiveFrom = effectiveFrom,
                    ReasonCode = "EXCEL_IMPORT",
                    Reason = "Initial status imported from the HR workbook.",
                    ChangedByUserId = actorUserId
                };
                dbContext.EmployeeStatusPeriods.Add(status);
                currentStatuses[employee.Id] = status;

                var relationship = CreateRelationship(employee.Id, row.RelationshipType, effectiveFrom, actorUserId);
                dbContext.EmployeeRelationshipPeriods.Add(relationship);
                currentRelationships[employee.Id] = relationship;
            }
            else
            {
                employee.FullNameAr = row.FullNameAr;
                employee.NormalizedNameAr = HrServiceSupport.NormalizeText(row.FullNameAr);
                employee.HireDate = row.HireDate ?? employee.HireDate;
                employee.NationalityCountryCode = row.NationalityCountryCode ?? employee.NationalityCountryCode;
                employee.CurrentStatus = EmployeeStatus.Active;
                counts.UpdatedEmployees++;
            }

            Sponsor? sponsor = null;
            if (!string.IsNullOrWhiteSpace(row.SponsorIdentityNumber)
                && !sponsors.TryGetValue(row.SponsorIdentityNumber, out sponsor))
            {
                issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", $"Sponsor '{row.SponsorIdentityNumber}' is not configured; sponsor-dependent fields were skipped."));
            }

            ApplyRelationship(employee, row, sponsor, actorUserId, today, currentRelationships, sponsoredDetails, outsideDetails);
            ApplySponsorship(employee, row, sponsor, actorUserId, today, currentSponsorships);

            JobTitle? jobTitle = null;
            if (!string.IsNullOrWhiteSpace(row.JobTitleAr))
            {
                jobTitle = GetOrCreateJobTitle(row.JobTitleAr, jobTitles);
            }
            ResidencyProfession? profession = null;
            if (!string.IsNullOrWhiteSpace(row.ResidencyProfessionAr) && !IsFormulaError(row.ResidencyProfessionAr))
            {
                profession = GetOrCreateProfession(row.ResidencyProfessionAr, professions);
            }

            ApplyOperationalAssignment(employee, row, jobTitle, actorUserId, today, currentJobs);
            var isRider = row.WorkTypeId is not null && row.WorkTypeId != OperationalWorkType.AdministrativeId
                || row.JobTitleAr.Contains("مندوب", StringComparison.Ordinal)
                || row.PlatformIds.Values.Any(value => value is not null)
                || TryParseCurrentApplication(row.CurrentApplication, out _, out _);
            riderProfiles.TryGetValue(employee.Id, out var rider);
            if (isRider && rider is null)
            {
                rider = new RiderProfile
                {
                    EmployeeId = employee.Id,
                    Status = RiderStatus.Active,
                    RiderStartDate = row.HireDate,
                    PreferredCityId = row.GlobalCityId,
                    OperationalNotes = "Created from the HR workbook import."
                };
                dbContext.RiderProfiles.Add(rider);
                riderProfiles[employee.Id] = rider;
                counts.CreatedRiders++;
            }
            else if (isRider && rider is not null)
            {
                rider.Status = RiderStatus.Active;
                rider.PreferredCityId = row.GlobalCityId ?? rider.PreferredCityId;
            }

            ApplyResidency(employee, row, sponsor, profession, currentPermits, issues, counts);
            ApplyDriverLicenses(employee, row, currentLicenses, issues, counts);
            ApplyPlatformAccounts(employee, row, sponsor, platformBundle, issues, counts);
        }

        ApplyPlatformAssignments(rows, employees, riderProfiles, platformBundle, actorUserId, today, issues, counts);

        if (validateOnly)
        {
            dbContext.ChangeTracker.Clear();
            return counts;
        }

        try
        {
            var executionStrategy = dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
            return counts;
        }
        catch (Exception exception)
        {
            LogImportFailure(logger, exception.Message);
            throw;
        }
    }

    private void ApplyRelationship(
        Employee employee,
        ParsedRow row,
        Sponsor? sponsor,
        Guid actorUserId,
        DateOnly today,
        Dictionary<Guid, EmployeeRelationshipPeriod> currentRelationships,
        Dictionary<Guid, SponsoredInternalDetails> sponsoredDetails,
        Dictionary<Guid, OutsideRiderDetails> outsideDetails)
    {
        if (employee.CurrentRelationshipType != row.RelationshipType)
        {
            if (currentRelationships.TryGetValue(employee.Id, out var current))
            {
                current.EffectiveTo = CloseDate(current.EffectiveFrom, today);
            }
            var period = CreateRelationship(employee.Id, row.RelationshipType, today, actorUserId);
            dbContext.EmployeeRelationshipPeriods.Add(period);
            currentRelationships[employee.Id] = period;
            employee.CurrentRelationshipType = row.RelationshipType;
        }

        if (row.RelationshipType == EmployeeRelationshipType.SponsoredInternal)
        {
            if (!sponsoredDetails.TryGetValue(employee.Id, out var details))
            {
                details = new SponsoredInternalDetails { EmployeeId = employee.Id };
                dbContext.SponsoredInternalDetails.Add(details);
                sponsoredDetails[employee.Id] = details;
            }
            details.CurrentSponsorId = sponsor?.Id;
            details.Profession = string.IsNullOrWhiteSpace(row.ResidencyProfessionAr) ? details.Profession : row.ResidencyProfessionAr;
        }
        else if (!outsideDetails.ContainsKey(employee.Id))
        {
            var details = new OutsideRiderDetails
            {
                EmployeeId = employee.Id,
                EngagementNotes = "Imported as outside rider from the HR workbook."
            };
            dbContext.OutsideRiderDetails.Add(details);
            outsideDetails[employee.Id] = details;
        }
    }

    private void ApplySponsorship(
        Employee employee,
        ParsedRow row,
        Sponsor? sponsor,
        Guid actorUserId,
        DateOnly today,
        Dictionary<Guid, EmployeeSponsorshipPeriod> currentSponsorships)
    {
        if (row.RelationshipType != EmployeeRelationshipType.SponsoredInternal || sponsor is null)
        {
            return;
        }
        if (currentSponsorships.TryGetValue(employee.Id, out var current) && current.SponsorId == sponsor.Id)
        {
            return;
        }
        if (current is not null)
        {
            current.EffectiveTo = CloseDate(current.EffectiveFrom, today);
        }
        var period = new EmployeeSponsorshipPeriod
        {
            EmployeeId = employee.Id,
            SponsorId = sponsor.Id,
            Status = SponsorshipStatus.Active,
            EffectiveFrom = current is null ? row.HireDate ?? today : today,
            Reason = "Imported from the HR workbook.",
            SourceReference = "HR_EXCEL",
            ChangedByUserId = actorUserId
        };
        dbContext.EmployeeSponsorshipPeriods.Add(period);
        currentSponsorships[employee.Id] = period;
    }

    private void ApplyOperationalAssignment(
        Employee employee,
        ParsedRow row,
        JobTitle? jobTitle,
        Guid actorUserId,
        DateOnly today,
        Dictionary<Guid, EmployeeJobTitlePeriod> currentJobs)
    {
        if (jobTitle is null || row.WorkTypeId is null || row.OperatingCityId is null)
        {
            return;
        }
        if (!dbContext.JobTitleOperationalWorkTypes.Local.Any(item => item.JobTitleId == jobTitle.Id && item.OperationalWorkTypeId == row.WorkTypeId)
            && !dbContext.JobTitleOperationalWorkTypes.Any(item => item.JobTitleId == jobTitle.Id && item.OperationalWorkTypeId == row.WorkTypeId))
        {
            dbContext.JobTitleOperationalWorkTypes.Add(new JobTitleOperationalWorkType
            {
                JobTitleId = jobTitle.Id,
                OperationalWorkTypeId = row.WorkTypeId.Value
            });
        }
        if (currentJobs.TryGetValue(employee.Id, out var current)
            && current.JobTitleId == jobTitle.Id
            && current.OperationalWorkTypeId == row.WorkTypeId
            && current.OperatingCityId == row.OperatingCityId)
        {
            return;
        }
        if (current is not null)
        {
            current.EffectiveTo = CloseDate(current.EffectiveFrom, today);
        }
        var assignment = new EmployeeJobTitlePeriod
        {
            EmployeeId = employee.Id,
            JobTitleId = jobTitle.Id,
            OperationalWorkTypeId = row.WorkTypeId.Value,
            OperatingCityId = row.OperatingCityId.Value,
            EffectiveFrom = current is null ? row.HireDate ?? today : today,
            Reason = "Imported from the HR workbook.",
            ChangedByUserId = actorUserId
        };
        dbContext.EmployeeJobTitlePeriods.Add(assignment);
        currentJobs[employee.Id] = assignment;
    }

    private void ApplyResidency(
        Employee employee,
        ParsedRow row,
        Sponsor? sponsor,
        ResidencyProfession? profession,
        Dictionary<Guid, EmployeeResidencyPermit> currentPermits,
        List<HrExcelImportIssue> issues,
        ImportCounts counts)
    {
        if (string.IsNullOrWhiteSpace(row.ResidencyNumber))
        {
            return;
        }
        if (sponsor is null || profession is null || row.ResidencyExpiryDate is null)
        {
            issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", "Residency number was present but sponsor, profession, or expiry date was missing; the permit was not imported."));
            return;
        }
        var hash = sensitiveValueProtector.CreateLookupHash(row.ResidencyNumber);
        var status = MapResidencyStatus(row.ResidencyStatus, row.ResidencyExpiryDate.Value);
        if (currentPermits.TryGetValue(employee.Id, out var current) && current.PermitNumberLookupHash == hash)
        {
            current.SponsorId = sponsor.Id;
            current.ResidencyProfessionId = profession.Id;
            current.ExpiryDate = row.ResidencyExpiryDate.Value;
            current.Status = status;
            return;
        }
        if (current is not null)
        {
            current.IsCurrent = false;
        }
        var permit = new EmployeeResidencyPermit
        {
            EmployeeId = employee.Id,
            SponsorId = sponsor.Id,
            ResidencyProfessionId = profession.Id,
            PermitNumberCiphertext = sensitiveValueProtector.Protect(row.ResidencyNumber),
            PermitNumberLookupHash = hash,
            PermitNumberLastFour = HrServiceSupport.LastFour(row.ResidencyNumber),
            ExpiryDate = row.ResidencyExpiryDate.Value,
            Status = status,
            PreviousPermitId = current?.Id,
            IsCurrent = true,
            Notes = "Imported from the HR workbook."
        };
        dbContext.EmployeeResidencyPermits.Add(permit);
        currentPermits[employee.Id] = permit;
        counts.CreatedResidencyPermits++;
    }

    private void ApplyDriverLicenses(
        Employee employee,
        ParsedRow row,
        Dictionary<(Guid EmployeeId, Guid CategoryId), EmployeeDriverLicense> currentLicenses,
        List<HrExcelImportIssue> issues,
        ImportCounts counts)
    {
        var categories = MapLicenseCategories(row.LicenseType, out var unsupportedPart);
        if (unsupportedPart)
        {
            issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", $"Unsupported license type content '{row.LicenseType}' was ignored."));
        }
        foreach (var categoryId in categories)
        {
            var bookingStatus = MapBookingStatus(row.LicenseBookingStatus);
            if (currentLicenses.TryGetValue((employee.Id, categoryId), out var current))
            {
                current.BookingStatus = bookingStatus;
                continue;
            }
            var license = new EmployeeDriverLicense
            {
                EmployeeId = employee.Id,
                DriverLicenseCategoryId = categoryId,
                BookingStatus = bookingStatus,
                IssuanceStatus = bookingStatus is DriverLicenseBookingStatus.Booked or DriverLicenseBookingStatus.WaitingForAppointment
                    ? DriverLicenseIssuanceStatus.InProgress
                    : DriverLicenseIssuanceStatus.NotStarted,
                LicenseStatus = DriverLicenseStatus.Application,
                IsCurrent = true,
                Notes = "License type and booking state imported from the HR workbook; number and dates were not supplied."
            };
            dbContext.EmployeeDriverLicenses.Add(license);
            currentLicenses[(employee.Id, categoryId)] = license;
            counts.CreatedDriverLicenses++;
        }
    }

    private async Task<PlatformBundle> LoadPlatformsAsync(IReadOnlyList<ParsedRow> rows, CancellationToken cancellationToken)
    {
        var definitionsUsed = PlatformDefinitions.Where(definition => rows.Any(row =>
            row.PlatformIds[definition.Code] is not null || CurrentApplicationMatches(row.CurrentApplication, definition))).ToArray();
        var platforms = (await dbContext.ClientPlatforms.ToArrayAsync(cancellationToken)).ToDictionary(item => item.Code, StringComparer.Ordinal);
        var contracts = (await dbContext.ClientContracts.ToArrayAsync(cancellationToken)).ToDictionary(item => item.Code, StringComparer.Ordinal);
        foreach (var definition in definitionsUsed)
        {
            if (!platforms.TryGetValue(definition.Code, out var platform))
            {
                platform = new ClientPlatform
                {
                    Code = definition.Code,
                    NameAr = definition.NameAr,
                    NameEn = definition.NameEn,
                    Status = CatalogStatus.Active,
                    Notes = "Created by the HR Excel import."
                };
                dbContext.ClientPlatforms.Add(platform);
                platforms[definition.Code] = platform;
            }
            var contractCode = $"IMPORT_{definition.Code}";
            if (!contracts.TryGetValue(contractCode, out var contract))
            {
                contract = new ClientContract
                {
                    ClientPlatformId = platform.Id,
                    Code = contractCode,
                    DisplayNameAr = $"عقد الاستيراد - {definition.NameAr}",
                    DisplayNameEn = $"Imported accounts - {definition.NameEn}",
                    Status = ClientContractStatus.Active,
                    StartDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                    Notes = "Default contract created only to preserve legacy Excel platform accounts."
                };
                dbContext.ClientContracts.Add(contract);
                contracts[contractCode] = contract;
            }
        }
        var platformIds = platforms.Values.Select(item => item.Id).ToArray();
        var accounts = (await dbContext.PlatformRiderAccounts.Where(item => platformIds.Contains(item.ClientPlatformId)).ToArrayAsync(cancellationToken))
            .ToDictionary(item => (item.ClientPlatformId, item.NormalizedExternalAccountId));
        var activeAssignments = (await dbContext.RiderClientAssignments.Where(item => item.EffectiveTo == null).ToArrayAsync(cancellationToken))
            .ToDictionary(item => item.ActualEmployeeId);
        return new(platforms, contracts, accounts, activeAssignments);
    }

    private void ApplyPlatformAccounts(
        Employee employee,
        ParsedRow row,
        Sponsor? sponsor,
        PlatformBundle bundle,
        List<HrExcelImportIssue> issues,
        ImportCounts counts)
    {
        foreach (var definition in PlatformDefinitions)
        {
            var externalId = row.PlatformIds[definition.Code];
            if (externalId is null)
            {
                continue;
            }
            if (row.OperatingCityId is null || !bundle.Platforms.TryGetValue(definition.Code, out var platform)
                || !bundle.Contracts.TryGetValue($"IMPORT_{definition.Code}", out var contract))
            {
                issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", $"Platform account {definition.Code}/{externalId} was skipped because its city or platform contract could not be resolved."));
                continue;
            }
            var normalized = NormalizeIdentifier(externalId).ToUpperInvariant();
            if (bundle.Accounts.TryGetValue((platform.Id, normalized), out var existing))
            {
                if (existing.RegisteredEmployeeId is null)
                {
                    existing.RegisteredEmployeeId = employee.Id;
                }
                continue;
            }
            var registrationType = sponsor is null ? PlatformRegistrationType.Freelancer : PlatformRegistrationType.Sponsored;
            var account = new PlatformRiderAccount
            {
                ClientContractId = contract.Id,
                ClientPlatformId = platform.Id,
                RegisteredEmployeeId = employee.Id,
                SponsorId = sponsor?.Id,
                OperatingCityId = row.OperatingCityId.Value,
                RegistrationType = registrationType,
                BillingMode = ParseBillingMode(row, definition.Code),
                Code = AccountCode(definition.Code, normalized),
                ExternalAccountId = externalId,
                NormalizedExternalAccountId = normalized,
                LabelAr = $"{definition.NameAr} - {employee.EmployeeNumber}",
                LabelEn = $"{definition.NameEn} - {employee.EmployeeNumber}",
                Status = PlatformRiderAccountStatus.Available,
                OwnershipNotes = "Registered owner imported from the HR workbook."
            };
            dbContext.PlatformRiderAccounts.Add(account);
            bundle.Accounts[(platform.Id, normalized)] = account;
            counts.CreatedPlatformAccounts++;
        }
    }

    private void ApplyPlatformAssignments(
        IReadOnlyList<ParsedRow> rows,
        Dictionary<string, Employee> employees,
        Dictionary<Guid, RiderProfile> riders,
        PlatformBundle bundle,
        Guid actorUserId,
        DateOnly today,
        List<HrExcelImportIssue> issues,
        ImportCounts counts)
    {
        foreach (var row in rows)
        {
            if (!TryParseCurrentApplication(row.CurrentApplication, out var platformCode, out var externalId)
                || !employees.TryGetValue(row.EmployeeNumber, out var employee)
                || !riders.TryGetValue(employee.Id, out var rider)
                || !bundle.Platforms.TryGetValue(platformCode, out var platform))
            {
                continue;
            }
            var normalized = NormalizeIdentifier(externalId).ToUpperInvariant();
            if (!bundle.Accounts.TryGetValue((platform.Id, normalized), out var account))
            {
                issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", $"Current application account {platformCode}/{externalId} was not found in the platform ID columns; no assignment was created."));
                continue;
            }
            if (bundle.ActiveAssignments.TryGetValue(employee.Id, out var current)
                && current.PlatformRiderAccountId == account.Id)
            {
                account.Status = PlatformRiderAccountStatus.Assigned;
                continue;
            }
            if (bundle.ActiveAssignments.Values.Any(item => item.PlatformRiderAccountId == account.Id && item.ActualEmployeeId != employee.Id))
            {
                issues.Add(new(row.RowNumber, row.EmployeeNumber, "Warning", $"Platform account {platformCode}/{externalId} is already assigned to another rider; the conflicting assignment was not changed."));
                continue;
            }
            if (current is not null)
            {
                current.EffectiveTo = today < current.EffectiveFrom ? current.EffectiveFrom : today;
                current.Status = RiderAssignmentStatus.Ended;
                current.EndReason = "Replaced by the HR workbook import.";
                current.EndedByUserId = actorUserId;
                var oldAccount = bundle.Accounts.Values.FirstOrDefault(item => item.Id == current.PlatformRiderAccountId);
                if (oldAccount is not null)
                {
                    oldAccount.Status = PlatformRiderAccountStatus.Available;
                }
                dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
                {
                    RiderClientAssignmentId = current.Id,
                    FromStatus = RiderAssignmentStatus.Active,
                    ToStatus = RiderAssignmentStatus.Ended,
                    OccurredAtUtc = timeProvider.GetUtcNow(),
                    ActorUserId = actorUserId,
                    Reason = "Replaced by the HR workbook import."
                });
            }
            var assignment = new RiderClientAssignment
            {
                ActualEmployeeId = employee.Id,
                RiderProfileId = rider.Id,
                ClientContractId = account.ClientContractId,
                PlatformRiderAccountId = account.Id,
                EffectiveFrom = today,
                Status = RiderAssignmentStatus.Active,
                StartReason = "Imported from the current work application column.",
                AssignedByUserId = actorUserId
            };
            dbContext.RiderClientAssignments.Add(assignment);
            dbContext.RiderAssignmentEvents.Add(new RiderAssignmentEvent
            {
                RiderClientAssignmentId = assignment.Id,
                FromStatus = RiderAssignmentStatus.Planned,
                ToStatus = RiderAssignmentStatus.Active,
                OccurredAtUtc = timeProvider.GetUtcNow(),
                ActorUserId = actorUserId,
                Reason = "Imported from the current work application column."
            });
            account.Status = PlatformRiderAccountStatus.Assigned;
            bundle.ActiveAssignments[employee.Id] = assignment;
            counts.CreatedPlatformAssignments++;
        }
    }

    private JobTitle GetOrCreateJobTitle(string nameAr, Dictionary<string, JobTitle> items)
    {
        var normalized = HrServiceSupport.NormalizeText(nameAr);
        if (items.TryGetValue(normalized, out var existing))
        {
            return existing;
        }
        var entity = new JobTitle
        {
            Code = StableCode("JOB", normalized),
            NameAr = nameAr,
            NameEn = nameAr,
            Status = CatalogStatus.Active,
            DescriptionAr = "تم إنشاؤه من ملف الموارد البشرية."
        };
        dbContext.JobTitles.Add(entity);
        items[normalized] = entity;
        return entity;
    }

    private ResidencyProfession GetOrCreateProfession(string nameAr, Dictionary<string, ResidencyProfession> items)
    {
        var normalized = HrServiceSupport.NormalizeText(nameAr);
        if (items.TryGetValue(normalized, out var existing))
        {
            return existing;
        }
        var entity = new ResidencyProfession
        {
            Code = StableCode("PROF", normalized),
            NameAr = nameAr,
            NameEn = nameAr,
            Status = CatalogStatus.Active
        };
        dbContext.ResidencyProfessions.Add(entity);
        items[normalized] = entity;
        return entity;
    }

    private static EmployeeRelationshipPeriod CreateRelationship(Guid employeeId, EmployeeRelationshipType relationship, DateOnly effectiveFrom, Guid actorUserId) => new()
    {
        EmployeeId = employeeId,
        RelationshipType = relationship,
        EffectiveFrom = effectiveFrom,
        ReasonCode = "EXCEL_IMPORT",
        Reason = "Imported from the HR workbook.",
        SourceReference = "HR_EXCEL",
        ChangedByUserId = actorUserId
    };

    private static string Value(IXLRow row, IReadOnlyDictionary<string, int> headers, string header) =>
        Cell(row, headers, header) is { } cell ? CellText(cell) : string.Empty;

    private static IXLCell? Cell(IXLRow row, IReadOnlyDictionary<string, int> headers, string header) =>
        headers.TryGetValue(HeaderKey(header), out var column) ? row.Cell(column) : null;

    private static bool HasValue(IXLRow row, IReadOnlyDictionary<string, int> headers, string header) =>
        !string.IsNullOrWhiteSpace(Value(row, headers, header));

    private static string CellText(IXLCell cell) => CollapseWhitespace(cell.GetFormattedString());

    private static string HeaderKey(string value) => CollapseWhitespace(value).Replace("أ", "ا", StringComparison.Ordinal).Replace("إ", "ا", StringComparison.Ordinal);

    private static string CollapseWhitespace(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : WhitespaceRegex().Replace(value.Trim(), " ");

    private static string NormalizeIdentifier(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : new(value.Where(character => !char.IsWhiteSpace(character) && character is not ',' && character is not '\u066C').ToArray());

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).Select(ToLatinDigit).ToArray());

    private static char ToLatinDigit(char value) => value switch
    {
        >= '\u0660' and <= '\u0669' => (char)('0' + value - '\u0660'),
        >= '\u06F0' and <= '\u06F9' => (char)('0' + value - '\u06F0'),
        _ => value
    };

    private static DateOnly? ParseDate(IXLCell? cell)
    {
        if (cell is null || cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var dateTime)) return DateOnly.FromDateTime(dateTime);
        if (cell.TryGetValue<double>(out var serial) && serial is > 1 and < 2_958_466)
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        }
        var text = CellText(cell);
        string[] formats = ["d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"];
        return DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private static string? MapNationality(string value) => HeaderKey(value) switch
    {
        "مصر" => "EG",
        "بنجلاديش" or "بنغلاديش" => "BD",
        "باكستان" => "PK",
        "اليمن" => "YE",
        "سري لنكا" or "سريلانكا" => "LK",
        "السودان" => "SD",
        "الهند" => "IN",
        "السعودية" => "SA",
        _ when value.Length == 2 => value.ToUpperInvariant(),
        _ => null
    };

    private static Guid? MapWorkType(string value)
    {
        var normalized = HeaderKey(value);
        if (normalized.Contains("اداري", StringComparison.Ordinal)) return OperationalWorkType.AdministrativeId;
        if (normalized.Contains("سيارة", StringComparison.Ordinal)) return OperationalWorkType.CarId;
        if (normalized.Contains("دباب", StringComparison.Ordinal) || normalized.Contains("دراجة", StringComparison.Ordinal)) return OperationalWorkType.MotorcycleId;
        return null;
    }

    private static (Guid? GlobalCityId, Guid? OperatingCityId) MapCity(string value)
    {
        var normalized = HeaderKey(value);
        if (normalized.Contains("جدة", StringComparison.Ordinal)) return (GlobalCity.JeddahId, OperatingCity.JeddahId);
        if (normalized.Contains("رياض", StringComparison.Ordinal)) return (GlobalCity.RiyadhId, OperatingCity.RiyadhId);
        return (null, null);
    }

    private static ResidencyPermitStatus MapResidencyStatus(string value, DateOnly expiryDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (value.Contains("انته", StringComparison.Ordinal) || expiryDate < today) return ResidencyPermitStatus.Expired;
        if (value.Contains("تجديد", StringComparison.Ordinal)) return ResidencyPermitStatus.RenewalInProgress;
        return ResidencyPermitStatus.Active;
    }

    private static List<Guid> MapLicenseCategories(string value, out bool unsupportedPart)
    {
        var normalized = HeaderKey(value);
        var result = new List<Guid>(2);
        if (normalized.Contains("نقل خفيف", StringComparison.Ordinal)) result.Add(DriverLicenseCategory.LightTransportId);
        if (normalized.Contains("دراجة", StringComparison.Ordinal) || normalized.Contains("الية", StringComparison.Ordinal)) result.Add(DriverLicenseCategory.MotorcycleId);
        unsupportedPart = !string.IsNullOrWhiteSpace(value) && !normalized.Contains("لا يوجد", StringComparison.Ordinal)
            && (result.Count == 0 || normalized.Contains("خصوصي", StringComparison.Ordinal) || normalized.Contains("ثقيل", StringComparison.Ordinal));
        return result;
    }

    private static DriverLicenseBookingStatus MapBookingStatus(string value)
    {
        var normalized = HeaderKey(value);
        if (string.IsNullOrWhiteSpace(normalized)) return DriverLicenseBookingStatus.Unknown;
        if (normalized.Contains("تم الحجز", StringComparison.Ordinal)) return DriverLicenseBookingStatus.Booked;
        if (normalized.Contains("لم يتم", StringComparison.Ordinal)) return DriverLicenseBookingStatus.NotBooked;
        if (normalized.Contains("انتظار", StringComparison.Ordinal) || normalized.Contains("اعادة", StringComparison.Ordinal)) return DriverLicenseBookingStatus.WaitingForAppointment;
        if (normalized.Contains("ملغي", StringComparison.Ordinal)) return DriverLicenseBookingStatus.Cancelled;
        return DriverLicenseBookingStatus.Unknown;
    }

    private static string? ParseExternalAccountId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("لا يوجد", StringComparison.Ordinal) || IsFormulaError(value)) return null;
        var match = ExternalIdRegex().Match(value.ToUpperInvariant());
        return match.Success ? match.Value : null;
    }

    private static bool TryParseCurrentApplication(string value, out string platformCode, out string externalId)
    {
        platformCode = string.Empty;
        externalId = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || value.Contains("لا يعمل", StringComparison.Ordinal)) return false;
        var definition = PlatformDefinitions.FirstOrDefault(platform => CurrentApplicationMatches(value, platform));
        var parsed = ParseExternalAccountId(value);
        if (definition is null || parsed is null) return false;
        platformCode = definition.Code;
        externalId = parsed;
        return true;
    }

    private static bool CurrentApplicationMatches(string value, FrozenPlatform platform) =>
        platform.ApplicationAliases.Any(alias => value.Contains(alias, StringComparison.OrdinalIgnoreCase));

    private static PlatformBillingMode ParseBillingMode(ParsedRow row, string platformCode)
    {
        var raw = row.RawPlatformValues.GetValueOrDefault(platformCode) ?? string.Empty;
        if (raw.Contains("Slab", StringComparison.OrdinalIgnoreCase)) return PlatformBillingMode.Slab;
        if (raw.Contains("Per order", StringComparison.OrdinalIgnoreCase)) return PlatformBillingMode.PerOrder;
        return PlatformBillingMode.Unknown;
    }

    private static string StableCode(string prefix, string value)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..20];
        return $"{prefix}_{hash}";
    }

    private static string AccountCode(string platformCode, string externalId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(externalId)))[..20];
        return $"{platformCode[..Math.Min(platformCode.Length, 8)]}_{hash}";
    }

    private static DateOnly CloseDate(DateOnly effectiveFrom, DateOnly today) => today < effectiveFrom ? effectiveFrom : today;
    private static bool IsFormulaError(string value) => value.StartsWith('#');

    private static string DescribeDatabaseFailure(DbUpdateException exception)
    {
        var message = exception.GetBaseException().Message.ReplaceLineEndings(" ").Trim();
        return message.Length <= 1_000
            ? message
            : $"{message[..1_000]}…";
    }

    [GeneratedRegex(@"[A-Z0-9]{5,24}", RegexOptions.CultureInvariant)]
    private static partial Regex ExternalIdRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "Rejected HR workbook {FileName}: {Reason}")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, string reason);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "HR workbook import transaction failed: {Reason}")]
    private static partial void LogImportFailure(ILogger logger, string reason);

    private sealed record FrozenPlatform(string Code, string NameAr, string NameEn, string Header, string[] ApplicationAliases);

    private sealed record ParsedRow(
        int RowNumber,
        string EmployeeNumber,
        string FullNameAr,
        DateOnly? HireDate,
        string? NationalityCountryCode,
        string ResidencyProfessionAr,
        string JobTitleAr,
        Guid? WorkTypeId,
        Guid? GlobalCityId,
        Guid? OperatingCityId,
        string SponsorIdentityNumber,
        EmployeeRelationshipType RelationshipType,
        string ResidencyNumber,
        DateOnly? ResidencyExpiryDate,
        string ResidencyStatus,
        string LicenseType,
        string LicenseBookingStatus,
        Dictionary<string, string?> PlatformIds,
        string CurrentApplication)
    {
        public IReadOnlyDictionary<string, string?> RawPlatformValues => PlatformIds;
    }

    private sealed record PlatformBundle(
        Dictionary<string, ClientPlatform> Platforms,
        Dictionary<string, ClientContract> Contracts,
        Dictionary<(Guid PlatformId, string ExternalId), PlatformRiderAccount> Accounts,
        Dictionary<Guid, RiderClientAssignment> ActiveAssignments);

    private sealed class ImportCounts
    {
        public int CreatedEmployees { get; set; }
        public int UpdatedEmployees { get; set; }
        public int CreatedRiders { get; set; }
        public int CreatedResidencyPermits { get; set; }
        public int CreatedDriverLicenses { get; set; }
        public int CreatedPlatformAccounts { get; set; }
        public int CreatedPlatformAssignments { get; set; }
    }
}
