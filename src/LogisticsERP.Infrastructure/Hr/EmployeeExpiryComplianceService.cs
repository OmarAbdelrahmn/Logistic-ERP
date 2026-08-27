using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Workforce;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class EmployeeExpiryComplianceService(
    ApplicationDbContext dbContext,
    IdentityDbContext identityDbContext,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider) : IEmployeeExpiryComplianceService
{
    private static readonly int[] ReminderDays = [30, 7, 1, 0];

    public async Task<Result<EmployeeExpiryCompliancePageResponse>> GetExpiriesAsync(EmployeeExpiryComplianceQuery query, CancellationToken cancellationToken = default)
    {
        if (!TryParse(query.SourceType, out EmployeeExpiryComplianceSourceType? sourceType)
            || !TryParse(query.DueStatus, out EmployeeExpiryComplianceDueStatus? dueStatus)
            || !TryParse(query.EmployeeStatus, out EmployeeStatus? employeeStatus))
        {
            return Result.Failure<EmployeeExpiryCompliancePageResponse>(HrErrors.InvalidRequest);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 50 : query.PageSize, 1, 200);
        var checkDate = query.CheckDate ?? RiyadhDate();
        var items = await BuildItemsAsync(query, checkDate, employeeStatus, cancellationToken);
        var filtered = items
            .Where(item => !sourceType.HasValue || item.SourceType == sourceType.Value)
            .Where(item => !dueStatus.HasValue || item.DueStatus == dueStatus.Value)
            .OrderBy(item => DueOrder(item.DueStatus))
            .ThenBy(item => item.ExpiryDate ?? DateOnly.MinValue)
            .ThenBy(item => item.EmployeeNameAr)
            .ThenBy(item => item.CategoryNameAr)
            .ToArray();

        var summary = Summarize(filtered);
        return Result.Success(new EmployeeExpiryCompliancePageResponse(
            filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), summary, page, pageSize, filtered.Length, checkDate));
    }

    public async Task<Result<EmployeeExpiryCompliancePageResponse>> GetEmployeeExpiriesAsync(Guid employeeId, DateOnly? checkDate, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Employees.AsNoTracking().AnyAsync(item => item.Id == employeeId && item.Status != EmployeeStatus.Archived, cancellationToken))
        {
            return Result.Failure<EmployeeExpiryCompliancePageResponse>(HrErrors.NotFound);
        }

        return await GetExpiriesAsync(new EmployeeExpiryComplianceQuery(
            checkDate, employeeId, null, null, null, null, null, null, 1, 200), cancellationToken);
    }

    public async Task RunDueNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var checkDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(3)).DateTime);
        var items = await BuildItemsAsync(new EmployeeExpiryComplianceQuery(checkDate, null, null, null, null, null, null, null), checkDate, null, cancellationToken);
        var recipients = await identityDbContext.Users.AsNoTracking()
            .Where(user => user.Status == UserAccountStatus.Active && !user.IsDevelopmentOnly)
            .Select(user => new NotificationRecipient(user.Id, user.AuthorizationVersion))
            .ToArrayAsync(cancellationToken);

        if (recipients.Length == 0) return;

        foreach (var item in items)
        {
            var band = ReminderBand(item);
            if (band is null) continue;

            foreach (var recipient in recipients)
            {
                if (!await permissionChecker.HasPermissionAsync(recipient.Id, recipient.AuthorizationVersion, PermissionKeys.Workforce.EmployeesRead, null, cancellationToken)) continue;

                var expiryToken = item.ExpiryDate?.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) ?? "missing";
                var deduplicationKey = $"employee-expiry:{item.SourceType}:{item.SourceId:N}:{expiryToken}:{band}";
                if (await dbContext.Notifications.AnyAsync(notification => notification.RecipientUserId == recipient.Id && notification.DeduplicationKey == deduplicationKey, cancellationToken)) continue;

                var missing = item.DueStatus == EmployeeExpiryComplianceDueStatus.Missing;
                var expired = item.DueStatus == EmployeeExpiryComplianceDueStatus.Expired;
                dbContext.Notifications.Add(new Notification
                {
                    RecipientUserId = recipient.Id,
                    EventType = $"employee.compliance.{(missing ? "missing" : expired ? "expired" : "due")}",
                    Severity = missing || expired ? NotificationSeverity.Error : item.DaysRemaining <= 1 ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                    TitleAr = missing ? "بيانات امتثال ناقصة" : expired ? "انتهاء وثيقة موظف" : "اقتراب انتهاء وثيقة موظف",
                    TitleEn = missing ? "Employee compliance data missing" : expired ? "Employee document expired" : "Employee document expiring",
                    BodyAr = MessageAr(item, missing, expired),
                    BodyEn = MessageEn(item, missing, expired),
                    SourceEntityType = item.SourceType.ToString(),
                    SourceEntityId = item.SourceId,
                    DeepLink = $"/employees/{item.EmployeeId}",
                    ScopeSnapshotJson = "{}",
                    DeduplicationKey = deduplicationKey,
                    VisibleAtUtc = now
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<EmployeeExpiryComplianceItemResponse[]> BuildItemsAsync(
        EmployeeExpiryComplianceQuery query,
        DateOnly checkDate,
        EmployeeStatus? employeeStatus,
        CancellationToken cancellationToken)
    {
        var employees = dbContext.Employees.AsNoTracking().Where(item => item.Status != EmployeeStatus.Archived);
        if (query.EmployeeId.HasValue) employees = employees.Where(item => item.Id == query.EmployeeId.Value);
        if (query.RiderProfileId.HasValue)
        {
            employees = from employee in employees
                        join rider in dbContext.RiderProfiles.AsNoTracking() on employee.Id equals rider.EmployeeId
                        where rider.Id == query.RiderProfileId.Value
                        select employee;
        }
        if (employeeStatus.HasValue) employees = employees.Where(item => item.Status == employeeStatus.Value);
        if (query.OperatingCityId.HasValue) employees = employees.Where(item => item.OperatingCityId == query.OperatingCityId.Value);
        if (query.SponsorId.HasValue) employees = employees.Where(item => item.SponsorId == query.SponsorId.Value);

        var employeeRows = await employees.Select(item => new EmployeeProjection(item.Id, item.FullNameAr, item.Status)).ToArrayAsync(cancellationToken);
        if (employeeRows.Length == 0) return [];

        var employeeById = employeeRows.ToDictionary(item => item.Id);
        var employeeIds = employeeById.Keys.ToArray();
        var riders = await dbContext.RiderProfiles.AsNoTracking()
            .Where(item => employeeIds.Contains(item.EmployeeId))
            .Select(item => new RiderProjection(item.Id, item.EmployeeId))
            .ToArrayAsync(cancellationToken);
        var riderByEmployeeId = riders.GroupBy(item => item.EmployeeId).ToDictionary(group => group.Key, group => (Guid?)group.First().Id);
        var riderById = riders.ToDictionary(item => item.Id);

        var linkedDocumentIds = await dbContext.EmployeeDriverLicenses.AsNoTracking()
            .Where(item => employeeIds.Contains(item.EmployeeId) && item.EmployeeDocumentId != null)
            .Select(item => item.EmployeeDocumentId)
            .Concat(dbContext.RiderCards.AsNoTracking().Where(item => riderById.Keys.Contains(item.RiderProfileId) && item.EmployeeDocumentId != null).Select(item => item.EmployeeDocumentId))
            .Concat(dbContext.RiderHealthCards.AsNoTracking().Where(item => riderById.Keys.Contains(item.RiderProfileId) && item.EmployeeDocumentId != null).Select(item => item.EmployeeDocumentId))
            .Concat(dbContext.EmployeeMedicalInsurancePolicies.AsNoTracking().Where(item => employeeIds.Contains(item.EmployeeId) && item.EmployeeDocumentId != null).Select(item => item.EmployeeDocumentId))
            .Where(item => item != null)
            .Select(item => item!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var candidates = new List<ExpiryCandidate>();
        var documents = await (from document in dbContext.EmployeeDocuments.AsNoTracking()
                               join type in dbContext.DocumentTypes.AsNoTracking() on document.DocumentTypeId equals type.Id
                               where employeeIds.Contains(document.EmployeeId)
                                   && document.Status == DocumentStatus.Active
                                   && type.Status == CatalogStatus.Active
                                   && !linkedDocumentIds.Contains(document.Id)
                                   && (document.ExpiryDate != null || type.RequiresExpiryDate)
                               select new { document, type }).ToArrayAsync(cancellationToken);
        foreach (var row in documents)
        {
            candidates.Add(NewCandidate(employeeById[row.document.EmployeeId], riderByEmployeeId.GetValueOrDefault(row.document.EmployeeId),
                EmployeeExpiryComplianceSourceType.EmployeeDocument, row.document.Id, row.type.Code, row.type.NameAr, row.type.NameEn,
                MaskReference(row.document.DocumentNumber), row.document.Status.ToString(), row.document.ExpiryDate, row.document.Id));
        }

        var licenses = await (from license in dbContext.EmployeeDriverLicenses.AsNoTracking()
                              join category in dbContext.DriverLicenseCategories.AsNoTracking() on license.DriverLicenseCategoryId equals category.Id
                              where employeeIds.Contains(license.EmployeeId) && license.IsCurrent && license.ExpiryDate != null
                                  && license.LicenseStatus != DriverLicenseStatus.Superseded
                                  && license.LicenseStatus != DriverLicenseStatus.Cancelled
                                  && license.LicenseStatus != DriverLicenseStatus.Rejected
                                  && license.LicenseStatus != DriverLicenseStatus.Revoked
                              select new { license, category }).ToArrayAsync(cancellationToken);
        foreach (var row in licenses)
        {
            candidates.Add(NewCandidate(employeeById[row.license.EmployeeId], riderByEmployeeId.GetValueOrDefault(row.license.EmployeeId),
                EmployeeExpiryComplianceSourceType.DriverLicense, row.license.Id, "DRIVER_LICENSE", row.category.NameAr, row.category.NameEn,
                HrServiceSupport.MaskLastFour(row.license.LicenseNumberLastFour), row.license.LicenseStatus.ToString(), row.license.ExpiryDate, row.license.EmployeeDocumentId));
        }

        var riderCards = await dbContext.RiderCards.AsNoTracking()
            .Where(item => riderById.Keys.Contains(item.RiderProfileId) && item.IsCurrent && item.ExpiryDate != null
                && item.Status != RiderCardStatus.Superseded && item.Status != RiderCardStatus.Cancelled)
            .ToArrayAsync(cancellationToken);
        foreach (var card in riderCards)
        {
            var rider = riderById[card.RiderProfileId];
            var (nameAr, nameEn) = RiderCardNames(card.CardType);
            candidates.Add(NewCandidate(employeeById[rider.EmployeeId], card.RiderProfileId,
                EmployeeExpiryComplianceSourceType.RiderCard, card.Id, $"RIDER_CARD_{card.CardType}", nameAr, nameEn,
                MaskReference(card.CardNumber), card.Status.ToString(), card.ExpiryDate, card.EmployeeDocumentId));
        }

        var healthCards = await dbContext.RiderHealthCards.AsNoTracking()
            .Where(item => riderById.Keys.Contains(item.RiderProfileId) && item.IsCurrent && item.ExpiryDate != null
                && item.Status != RiderHealthCardStatus.Superseded && item.Status != RiderHealthCardStatus.Cancelled)
            .ToArrayAsync(cancellationToken);
        foreach (var card in healthCards)
        {
            var rider = riderById[card.RiderProfileId];
            candidates.Add(NewCandidate(employeeById[rider.EmployeeId], card.RiderProfileId,
                EmployeeExpiryComplianceSourceType.HealthCard, card.Id, "HEALTH_CARD", "البطاقة الصحية", "Health card",
                HrServiceSupport.MaskLastFour(card.CardNumberLastFour), card.Status.ToString(), card.ExpiryDate, card.EmployeeDocumentId));
        }

        var policies = await (from policy in dbContext.EmployeeMedicalInsurancePolicies.AsNoTracking()
                              join company in dbContext.InsuranceCompanies.AsNoTracking() on policy.InsuranceCompanyId equals company.Id
                              join plan in dbContext.InsurancePlanLevels.AsNoTracking() on policy.InsurancePlanLevelId equals plan.Id
                              where employeeIds.Contains(policy.EmployeeId) && policy.IsCurrent
                                  && policy.Status != MedicalInsurancePolicyStatus.Superseded
                                  && policy.Status != MedicalInsurancePolicyStatus.Cancelled
                              select new { policy, company, plan }).ToArrayAsync(cancellationToken);
        foreach (var row in policies)
        {
            candidates.Add(NewCandidate(employeeById[row.policy.EmployeeId], riderByEmployeeId.GetValueOrDefault(row.policy.EmployeeId),
                EmployeeExpiryComplianceSourceType.MedicalInsurance, row.policy.Id, "MEDICAL_INSURANCE", row.plan.NameAr, row.plan.NameEn,
                HrServiceSupport.MaskLastFour(row.policy.PolicyNumberLastFour ?? row.policy.MemberNumberLastFour), row.policy.Status.ToString(), row.policy.EndDate, row.policy.EmployeeDocumentId));
        }

        return candidates.Select(candidate => ToResponse(candidate, checkDate)).ToArray();
    }

    private static ExpiryCandidate NewCandidate(EmployeeProjection employee, Guid? riderProfileId,
        EmployeeExpiryComplianceSourceType sourceType, Guid sourceId, string categoryCode, string categoryNameAr, string? categoryNameEn,
        string? referenceMasked, string sourceStatus, DateOnly? expiryDate, Guid? employeeDocumentId) => new(
            employee.Id, riderProfileId, employee.FullNameAr, employee.Status.ToString(), sourceType, sourceId, categoryCode,
            categoryNameAr, categoryNameEn ?? categoryNameAr, referenceMasked, sourceStatus, expiryDate, employeeDocumentId);

    private static EmployeeExpiryComplianceItemResponse ToResponse(ExpiryCandidate candidate, DateOnly checkDate)
    {
        var dueStatus = EmployeeExpiryComplianceStatusCalculator.Calculate(candidate.ExpiryDate, checkDate);
        var daysRemaining = candidate.ExpiryDate?.DayNumber - checkDate.DayNumber;
        return new EmployeeExpiryComplianceItemResponse(candidate.EmployeeId, candidate.RiderProfileId, candidate.EmployeeNameAr,
            candidate.EmployeeStatus, candidate.SourceType, candidate.SourceId, candidate.CategoryCode, candidate.CategoryNameAr,
            candidate.CategoryNameEn, candidate.ReferenceMasked, candidate.SourceStatus, candidate.ExpiryDate, daysRemaining,
            dueStatus, candidate.EmployeeDocumentId);
    }

    private static EmployeeExpiryComplianceSummary Summarize(IEnumerable<EmployeeExpiryComplianceItemResponse> items)
    {
        var materialized = items as EmployeeExpiryComplianceItemResponse[] ?? items.ToArray();
        return new EmployeeExpiryComplianceSummary(
            materialized.Count(item => item.DueStatus == EmployeeExpiryComplianceDueStatus.Valid),
            materialized.Count(item => item.DueStatus == EmployeeExpiryComplianceDueStatus.Upcoming),
            materialized.Count(item => item.DueStatus == EmployeeExpiryComplianceDueStatus.DueToday),
            materialized.Count(item => item.DueStatus == EmployeeExpiryComplianceDueStatus.Expired),
            materialized.Count(item => item.DueStatus == EmployeeExpiryComplianceDueStatus.Missing));
    }

    private static bool TryParse<TEnum>(string? value, out TEnum? parsed) where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<TEnum>(value, true, out var result) || !Enum.IsDefined(result)) return false;
        parsed = result;
        return true;
    }

    private DateOnly RiyadhDate() => DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);

    private static int DueOrder(EmployeeExpiryComplianceDueStatus status) => status switch
    {
        EmployeeExpiryComplianceDueStatus.Missing => 0,
        EmployeeExpiryComplianceDueStatus.Expired => 1,
        EmployeeExpiryComplianceDueStatus.DueToday => 2,
        EmployeeExpiryComplianceDueStatus.Upcoming => 3,
        _ => 4
    };

    private static string? ReminderBand(EmployeeExpiryComplianceItemResponse item) => item.DueStatus switch
    {
        EmployeeExpiryComplianceDueStatus.Missing => "missing",
        EmployeeExpiryComplianceDueStatus.Expired => "expired",
        _ when item.DaysRemaining.HasValue && ReminderDays.Contains(item.DaysRemaining.Value) => item.DaysRemaining.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => null
    };

    private static string MessageAr(EmployeeExpiryComplianceItemResponse item, bool missing, bool expired) => missing
        ? $"لا يوجد تاريخ انتهاء مطلوب لـ {item.CategoryNameAr} للموظف {item.EmployeeNameAr}."
        : expired
            ? $"انتهى {item.CategoryNameAr} للموظف {item.EmployeeNameAr} بتاريخ {item.ExpiryDate:yyyy-MM-dd}."
            : $"ينتهي {item.CategoryNameAr} للموظف {item.EmployeeNameAr} خلال {item.DaysRemaining} يوم/أيام.";

    private static string MessageEn(EmployeeExpiryComplianceItemResponse item, bool missing, bool expired) => missing
        ? $"A required expiry date is missing for {item.CategoryNameEn} of {item.EmployeeNameAr}."
        : expired
            ? $"{item.CategoryNameEn} for {item.EmployeeNameAr} expired on {item.ExpiryDate:yyyy-MM-dd}."
            : $"{item.CategoryNameEn} for {item.EmployeeNameAr} expires in {item.DaysRemaining} day(s).";

    private static string MaskReference(string? value) => HrServiceSupport.MaskLastFour(
        string.IsNullOrWhiteSpace(value) ? null : HrServiceSupport.LastFour(value));

    private static (string NameAr, string NameEn) RiderCardNames(RiderCardType type) => type switch
    {
        RiderCardType.Car => ("بطاقة رايدر سيارة", "Car rider card"),
        RiderCardType.Motorcycle => ("بطاقة رايدر دراجة نارية", "Motorcycle rider card"),
        _ => ("بطاقة رايدر", "Rider card")
    };

    private sealed record EmployeeProjection(Guid Id, string FullNameAr, EmployeeStatus Status);
    private sealed record RiderProjection(Guid Id, Guid EmployeeId);
    private sealed record NotificationRecipient(Guid Id, long AuthorizationVersion);
    private sealed record ExpiryCandidate(Guid EmployeeId, Guid? RiderProfileId, string EmployeeNameAr, string EmployeeStatus,
        EmployeeExpiryComplianceSourceType SourceType, Guid SourceId, string CategoryCode, string CategoryNameAr, string CategoryNameEn,
        string? ReferenceMasked, string SourceStatus, DateOnly? ExpiryDate, Guid? EmployeeDocumentId);
}
