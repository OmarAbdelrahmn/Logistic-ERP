using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Reporting;

/// <summary>
/// Read-only HR metrics intended for dashboard cards and charts. Platform coverage is based on
/// currently active riders and their current account assignment; a rider can therefore be missing
/// an account on more than one active platform.
/// </summary>
public sealed record HrDashboardReportResponse(
    DateTimeOffset GeneratedAtUtc,
    HrHeadcountSummaryResponse Headcount,
    IReadOnlyList<SponsorHeadcountResponse> Sponsors,
    IReadOnlyList<PlatformCoverageResponse> ActivePlatforms);

public sealed record HrHeadcountSummaryResponse(
    int TotalPeople,
    int TotalEmployees,
    int TotalRiders,
    int ActivePeople,
    int ActiveEmployees,
    int ActiveRiders,
    int PeopleWithoutSponsor,
    int ActiveRidersWithoutAnyPlatformAccount,
    int ActiveRiderPlatformCoverageGaps);

public sealed record SponsorHeadcountResponse(
    Guid SponsorId,
    string SponsorNameAr,
    string? SponsorNameEn,
    string Status,
    int TotalPeople,
    int Employees,
    int Riders,
    int ActivePeople,
    int ActiveEmployees,
    int ActiveRiders);

public sealed record PlatformCoverageResponse(
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    string PlatformNameEn,
    int OperationalAccountCount,
    int AssignedAccountCount,
    int ActiveRidersWithAccount,
    int ActiveRidersWithoutAccount);

public sealed record SystemDashboardReportResponse(
    DateTimeOffset GeneratedAtUtc,
    HrHeadcountSummaryResponse Hr,
    PeopleComplianceDashboardReportResponse PeopleCompliance,
    FleetDashboardReportResponse Fleet,
    OperationsDashboardReportResponse Operations,
    MaintenanceInventoryDashboardReportResponse MaintenanceInventory);

public sealed record PeopleComplianceDashboardReportResponse(
    int PayrollEmployees,
    int ActiveEmployeeDocuments,
    int ExpiredEmployeeDocuments,
    int ActiveDriverLicenses,
    int ExpiredDriverLicenses,
    int ActiveMedicalInsurancePolicies,
    int PendingLeaveRequests,
    int ActiveLeaveRequests,
    int OpenAbsenceComplianceCases);

public sealed record FleetDashboardReportResponse(
    int TotalVehicles,
    int AvailableVehicles,
    int AssignedVehicles,
    int HeldVehicles,
    int DecommissionedVehicles,
    int ActiveRiderVehicleAssignments,
    int OpenVehicleIssues,
    int UnclosedAccidents);

public sealed record OperationsDashboardReportResponse(
    int ActivePlatforms,
    int OperationalPlatformAccounts,
    int AssignedPlatformAccounts,
    int ActiveHousingLocations,
    int TotalActiveHousingCapacity,
    int CurrentHousingResidents,
    int TotalPhoneSims,
    int AvailablePhoneSims,
    int AssignedPhoneSims,
    int PhoneSimsNeedingAttention,
    int TotalFuelCards,
    int AssignedFuelCards);

public sealed record MaintenanceInventoryDashboardReportResponse(
    int ActiveMaintenanceLocations,
    int ActiveInventoryItems,
    int StockBalanceRecords,
    int LowStockItems,
    decimal InventoryValue,
    int OpenWorkOrders,
    int InProgressWorkOrders,
    int PendingSupplyRequests);

public interface IReportingService
{
    Task<Result<SystemDashboardReportResponse>> GetSystemDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<HrDashboardReportResponse>> GetHrDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PeopleComplianceDashboardReportResponse>> GetPeopleComplianceDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<FleetDashboardReportResponse>> GetFleetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OperationsDashboardReportResponse>> GetOperationsDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<MaintenanceInventoryDashboardReportResponse>> GetMaintenanceInventoryDashboardAsync(
        CancellationToken cancellationToken = default);
}
