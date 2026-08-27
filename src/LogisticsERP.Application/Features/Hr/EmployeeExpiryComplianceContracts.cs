using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Hr;

public sealed record EmployeeExpiryComplianceQuery(
    DateOnly? CheckDate,
    Guid? EmployeeId,
    Guid? RiderProfileId,
    string? SourceType,
    string? DueStatus,
    string? EmployeeStatus,
    Guid? OperatingCityId,
    Guid? SponsorId,
    int Page = 1,
    int PageSize = 50);

public sealed record EmployeeExpiryComplianceItemResponse(
    Guid EmployeeId,
    Guid? RiderProfileId,
    string EmployeeNameAr,
    string EmployeeStatus,
    EmployeeExpiryComplianceSourceType SourceType,
    Guid SourceId,
    string CategoryCode,
    string CategoryNameAr,
    string CategoryNameEn,
    string? ReferenceMasked,
    string SourceStatus,
    DateOnly? ExpiryDate,
    int? DaysRemaining,
    EmployeeExpiryComplianceDueStatus DueStatus,
    Guid? EmployeeDocumentId);

public sealed record EmployeeExpiryComplianceSummary(int Valid, int Upcoming, int DueToday, int Expired, int Missing);

public sealed record EmployeeExpiryCompliancePageResponse(
    IReadOnlyList<EmployeeExpiryComplianceItemResponse> Items,
    EmployeeExpiryComplianceSummary Summary,
    int Page,
    int PageSize,
    int TotalCount,
    DateOnly CheckDate);

public interface IEmployeeExpiryComplianceService
{
    Task<Result<EmployeeExpiryCompliancePageResponse>> GetExpiriesAsync(EmployeeExpiryComplianceQuery query, CancellationToken cancellationToken = default);
    Task<Result<EmployeeExpiryCompliancePageResponse>> GetEmployeeExpiriesAsync(Guid employeeId, DateOnly? checkDate, CancellationToken cancellationToken = default);
    Task RunDueNotificationsAsync(CancellationToken cancellationToken = default);
}
