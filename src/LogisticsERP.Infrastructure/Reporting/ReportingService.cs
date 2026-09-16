using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Reporting;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Reporting;

internal sealed class ReportingService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : IReportingService
{
    public async Task<Result<SystemDashboardReportResponse>> GetSystemDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var headcount = await GetHeadcountAsync(cancellationToken);
        var peopleCompliance = await GetPeopleComplianceDashboardCoreAsync(cancellationToken);
        var fleet = await GetFleetDashboardCoreAsync(cancellationToken);
        var operations = await GetOperationsDashboardCoreAsync(cancellationToken);
        var maintenanceInventory = await GetMaintenanceInventoryDashboardCoreAsync(cancellationToken);

        return Result.Success(new SystemDashboardReportResponse(
            timeProvider.GetUtcNow(),
            headcount,
            peopleCompliance,
            fleet,
            operations,
            maintenanceInventory));
    }

    public async Task<Result<HrDashboardReportResponse>> GetHrDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var people = dbContext.Employees.AsNoTracking();
        var activeRiders = people.Where(item => !item.IsEmployee && item.Status == EmployeeStatus.Active);
        var headcount = await GetHeadcountAsync(cancellationToken);

        var sponsors = await dbContext.Sponsors.AsNoTracking()
            .OrderBy(item => item.RegistryNameAr)
            .Select(sponsor => new SponsorHeadcountResponse(
                sponsor.Id,
                sponsor.RegistryNameAr,
                sponsor.RegistryNameEn,
                sponsor.Status.ToString(),
                people.Count(person => person.SponsorId == sponsor.Id),
                people.Count(person => person.SponsorId == sponsor.Id && person.IsEmployee),
                people.Count(person => person.SponsorId == sponsor.Id && !person.IsEmployee),
                people.Count(person => person.SponsorId == sponsor.Id && person.Status == EmployeeStatus.Active),
                people.Count(person => person.SponsorId == sponsor.Id && person.IsEmployee && person.Status == EmployeeStatus.Active),
                people.Count(person => person.SponsorId == sponsor.Id && !person.IsEmployee && person.Status == EmployeeStatus.Active)))
            .ToArrayAsync(cancellationToken);

        var activePlatforms = await dbContext.ClientPlatforms.AsNoTracking()
            .Where(item => item.Status == CatalogStatus.Active)
            .OrderBy(item => item.NameAr)
            .Select(platform => new PlatformCoverageProjection(
                platform.Id,
                platform.Code,
                platform.NameAr,
                platform.NameEn,
                dbContext.PlatformRiderAccounts.Count(account =>
                    account.ClientPlatformId == platform.Id
                    && (account.Status == PlatformRiderAccountStatus.Available
                        || account.Status == PlatformRiderAccountStatus.Assigned)),
                dbContext.PlatformRiderAccounts.Count(account =>
                    account.ClientPlatformId == platform.Id
                    && account.Status == PlatformRiderAccountStatus.Assigned),
                (from assignment in dbContext.RiderClientAssignments
                 join account in dbContext.PlatformRiderAccounts
                     on assignment.PlatformRiderAccountId equals account.Id
                 join rider in dbContext.RiderProfiles
                     on assignment.RiderProfileId equals rider.Id
                 join employee in activeRiders
                     on rider.EmployeeId equals employee.Id
                 where assignment.EffectiveTo == null
                     && assignment.Status == RiderAssignmentStatus.Active
                     && account.ClientPlatformId == platform.Id
                     && account.Status == PlatformRiderAccountStatus.Assigned
                 select rider.Id).Distinct().Count()))
            .ToArrayAsync(cancellationToken);

        var activeRidersWithAnyPlatformAccount = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                                                         join account in dbContext.PlatformRiderAccounts.AsNoTracking()
                                                             on assignment.PlatformRiderAccountId equals account.Id
                                                         join rider in dbContext.RiderProfiles.AsNoTracking()
                                                             on assignment.RiderProfileId equals rider.Id
                                                         join employee in activeRiders
                                                             on rider.EmployeeId equals employee.Id
                                                         where assignment.EffectiveTo == null
                                                             && assignment.Status == RiderAssignmentStatus.Active
                                                             && account.Status == PlatformRiderAccountStatus.Assigned
                                                         select rider.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var platformCoverage = activePlatforms
            .Select(platform => new PlatformCoverageResponse(
                platform.Id,
                platform.Code,
                platform.NameAr,
                platform.NameEn,
                platform.OperationalAccountCount,
                platform.AssignedAccountCount,
                platform.ActiveRidersWithAccount,
                Math.Max(0, headcount.ActiveRiders - platform.ActiveRidersWithAccount)))
            .ToArray();

        var response = new HrDashboardReportResponse(
            timeProvider.GetUtcNow(),
            new HrHeadcountSummaryResponse(
                headcount.TotalPeople,
                headcount.TotalEmployees,
                headcount.TotalRiders,
                headcount.ActivePeople,
                headcount.ActiveEmployees,
                headcount.ActiveRiders,
                headcount.PeopleWithoutSponsor,
                Math.Max(0, headcount.ActiveRiders - activeRidersWithAnyPlatformAccount),
                platformCoverage.Sum(item => item.ActiveRidersWithoutAccount)),
            sponsors,
            platformCoverage);

        return Result.Success(response);
    }

    public async Task<Result<FleetDashboardReportResponse>> GetFleetDashboardAsync(
        CancellationToken cancellationToken = default) =>
        Result.Success(await GetFleetDashboardCoreAsync(cancellationToken));

    public async Task<Result<PeopleComplianceDashboardReportResponse>> GetPeopleComplianceDashboardAsync(
        CancellationToken cancellationToken = default) =>
        Result.Success(await GetPeopleComplianceDashboardCoreAsync(cancellationToken));

    public async Task<Result<OperationsDashboardReportResponse>> GetOperationsDashboardAsync(
        CancellationToken cancellationToken = default) =>
        Result.Success(await GetOperationsDashboardCoreAsync(cancellationToken));

    public async Task<Result<MaintenanceInventoryDashboardReportResponse>> GetMaintenanceInventoryDashboardAsync(
        CancellationToken cancellationToken = default) =>
        Result.Success(await GetMaintenanceInventoryDashboardCoreAsync(cancellationToken));

    private async Task<HrHeadcountSummaryResponse> GetHeadcountAsync(CancellationToken cancellationToken)
    {
        var people = dbContext.Employees.AsNoTracking();
        var activeRiders = people.Where(item => !item.IsEmployee && item.Status == EmployeeStatus.Active);
        var summary = await people
            .GroupBy(_ => 1)
            .Select(group => new HeadcountProjection(
                group.Count(),
                group.Count(item => item.IsEmployee),
                group.Count(item => !item.IsEmployee),
                group.Count(item => item.Status == EmployeeStatus.Active),
                group.Count(item => item.IsEmployee && item.Status == EmployeeStatus.Active),
                group.Count(item => !item.IsEmployee && item.Status == EmployeeStatus.Active),
                group.Count(item => item.SponsorId == null)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new HeadcountProjection(0, 0, 0, 0, 0, 0, 0);

        var activeRidersWithAnyPlatformAccount = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                                                         join account in dbContext.PlatformRiderAccounts.AsNoTracking()
                                                             on assignment.PlatformRiderAccountId equals account.Id
                                                         join rider in dbContext.RiderProfiles.AsNoTracking()
                                                             on assignment.RiderProfileId equals rider.Id
                                                         join employee in activeRiders
                                                             on rider.EmployeeId equals employee.Id
                                                         where assignment.EffectiveTo == null
                                                             && assignment.Status == RiderAssignmentStatus.Active
                                                             && account.Status == PlatformRiderAccountStatus.Assigned
                                                         select rider.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        var activePlatformCount = await dbContext.ClientPlatforms.AsNoTracking()
            .CountAsync(item => item.Status == CatalogStatus.Active, cancellationToken);

        var activeRiderPlatformAccounts = await (from assignment in dbContext.RiderClientAssignments.AsNoTracking()
                                                  join account in dbContext.PlatformRiderAccounts.AsNoTracking()
                                                      on assignment.PlatformRiderAccountId equals account.Id
                                                  join platform in dbContext.ClientPlatforms.AsNoTracking()
                                                      on account.ClientPlatformId equals platform.Id
                                                  join rider in dbContext.RiderProfiles.AsNoTracking()
                                                      on assignment.RiderProfileId equals rider.Id
                                                  join employee in activeRiders
                                                      on rider.EmployeeId equals employee.Id
                                                  where assignment.EffectiveTo == null
                                                      && assignment.Status == RiderAssignmentStatus.Active
                                                      && account.Status == PlatformRiderAccountStatus.Assigned
                                                      && platform.Status == CatalogStatus.Active
                                                  select new { rider.Id, PlatformId = platform.Id })
            .Distinct()
            .CountAsync(cancellationToken);

        return new HrHeadcountSummaryResponse(
            summary.TotalPeople,
            summary.TotalEmployees,
            summary.TotalRiders,
            summary.ActivePeople,
            summary.ActiveEmployees,
            summary.ActiveRiders,
            summary.PeopleWithoutSponsor,
            Math.Max(0, summary.ActiveRiders - activeRidersWithAnyPlatformAccount),
            Math.Max(0, summary.ActiveRiders * activePlatformCount - activeRiderPlatformAccounts));
    }

    private async Task<FleetDashboardReportResponse> GetFleetDashboardCoreAsync(CancellationToken cancellationToken)
    {
        var fleet = await dbContext.Vehicles.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new FleetProjection(
                group.Count(),
                group.Count(item => item.CurrentOperationalStatus == VehicleOperationalStatus.Available),
                group.Count(item => item.CurrentOperationalStatus == VehicleOperationalStatus.Assigned),
                group.Count(item => item.CurrentOperationalStatus == VehicleOperationalStatus.ProblemHold
                    || item.CurrentOperationalStatus == VehicleOperationalStatus.AccidentHold
                    || item.CurrentOperationalStatus == VehicleOperationalStatus.Stolen
                    || item.CurrentOperationalStatus == VehicleOperationalStatus.OutOfService),
                group.Count(item => item.CurrentOperationalStatus == VehicleOperationalStatus.Decommissioned)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new FleetProjection(0, 0, 0, 0, 0);

        var activeAssignments = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .CountAsync(item => item.Status == RiderVehicleAssignmentStatus.Active, cancellationToken);
        var openIssues = await dbContext.VehicleIssues.AsNoTracking()
            .CountAsync(item => item.Status == VehicleIssueStatus.Open || item.Status == VehicleIssueStatus.UnderReview, cancellationToken);
        var unclosedAccidents = await dbContext.VehicleAccidents.AsNoTracking()
            .CountAsync(item => item.Status != VehicleAccidentStatus.Closed, cancellationToken);

        return new FleetDashboardReportResponse(
            fleet.TotalVehicles,
            fleet.AvailableVehicles,
            fleet.AssignedVehicles,
            fleet.HeldVehicles,
            fleet.DecommissionedVehicles,
            activeAssignments,
            openIssues,
            unclosedAccidents);
    }

    private async Task<PeopleComplianceDashboardReportResponse> GetPeopleComplianceDashboardCoreAsync(
        CancellationToken cancellationToken)
    {
        var payrollEmployees = await dbContext.PayrollEmployees.AsNoTracking().CountAsync(cancellationToken);
        var documents = await dbContext.EmployeeDocuments.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new DocumentProjection(
                group.Count(item => item.Status == DocumentStatus.Active),
                group.Count(item => item.Status == DocumentStatus.Expired)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new DocumentProjection(0, 0);
        var licenses = await dbContext.EmployeeDriverLicenses.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new LicenseProjection(
                group.Count(item => item.IsCurrent && item.LicenseStatus == DriverLicenseStatus.Active),
                group.Count(item => item.IsCurrent && item.LicenseStatus == DriverLicenseStatus.Expired)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new LicenseProjection(0, 0);
        var activePolicies = await dbContext.EmployeeMedicalInsurancePolicies.AsNoTracking()
            .CountAsync(item => item.IsCurrent && item.Status == MedicalInsurancePolicyStatus.Active, cancellationToken);
        var leaves = await dbContext.LeaveRequests.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new LeaveProjection(
                group.Count(item => item.Status == LeaveWorkflowStatus.PendingApproval
                    || item.Status == LeaveWorkflowStatus.CancellationPending),
                group.Count(item => item.Status == LeaveWorkflowStatus.Active)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new LeaveProjection(0, 0);
        var openAbsenceCases = await dbContext.EmployeeAbsenceComplianceCases.AsNoTracking()
            .CountAsync(item => item.Status == AbsenceCaseStatus.Open
                || item.Status == AbsenceCaseStatus.UnderReview
                || item.Status == AbsenceCaseStatus.DeadlineApproaching
                || item.Status == AbsenceCaseStatus.Overdue,
                cancellationToken);

        return new PeopleComplianceDashboardReportResponse(
            payrollEmployees,
            documents.ActiveDocuments,
            documents.ExpiredDocuments,
            licenses.ActiveLicenses,
            licenses.ExpiredLicenses,
            activePolicies,
            leaves.PendingLeaveRequests,
            leaves.ActiveLeaveRequests,
            openAbsenceCases);
    }

    private async Task<OperationsDashboardReportResponse> GetOperationsDashboardCoreAsync(CancellationToken cancellationToken)
    {
        var activePlatforms = await dbContext.ClientPlatforms.AsNoTracking()
            .CountAsync(item => item.Status == CatalogStatus.Active, cancellationToken);
        var accounts = await dbContext.PlatformRiderAccounts.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new PlatformAccountProjection(
                group.Count(item => item.Status == PlatformRiderAccountStatus.Available || item.Status == PlatformRiderAccountStatus.Assigned),
                group.Count(item => item.Status == PlatformRiderAccountStatus.Assigned)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PlatformAccountProjection(0, 0);
        var activeHousingIds = dbContext.Housing.AsNoTracking()
            .Where(item => item.Status == HousingStatus.Active)
            .Select(item => item.Id);
        var activeHousingLocations = await activeHousingIds.CountAsync(cancellationToken);
        var totalActiveHousingCapacity = await dbContext.HousingRooms.AsNoTracking()
            .Where(item => activeHousingIds.Contains(item.HousingId))
            .SumAsync(item => (int?)item.Capacity, cancellationToken) ?? 0;
        var residents = await dbContext.HousingRooms.AsNoTracking()
            .Where(item => activeHousingIds.Contains(item.HousingId))
            .SumAsync(item => (int?)item.CurrentOccupancy, cancellationToken) ?? 0;
        var phoneSims = await dbContext.PhoneSimCards.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new PhoneSimProjection(
                group.Count(),
                group.Count(item => item.Status == PhoneSimStatus.Available),
                group.Count(item => item.Status == PhoneSimStatus.Assigned),
                group.Count(item => item.Status == PhoneSimStatus.Suspended || item.Status == PhoneSimStatus.Lost)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new PhoneSimProjection(0, 0, 0, 0);
        var fuelCards = await dbContext.FuelCards.AsNoTracking().CountAsync(cancellationToken);
        var assignedFuelCards = await dbContext.FuelCardRiderAssignments.AsNoTracking()
            .Where(item => item.EffectiveTo == null)
            .Select(item => item.FuelCardId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new OperationsDashboardReportResponse(
            activePlatforms,
            accounts.OperationalAccountCount,
            accounts.AssignedAccountCount,
            activeHousingLocations,
            totalActiveHousingCapacity,
            residents,
            phoneSims.TotalPhoneSims,
            phoneSims.AvailablePhoneSims,
            phoneSims.AssignedPhoneSims,
            phoneSims.PhoneSimsNeedingAttention,
            fuelCards,
            assignedFuelCards);
    }

    private async Task<MaintenanceInventoryDashboardReportResponse> GetMaintenanceInventoryDashboardCoreAsync(
        CancellationToken cancellationToken)
    {
        var activeLocations = await dbContext.MaintenanceLocations.AsNoTracking()
            .CountAsync(item => item.Status == CatalogStatus.Active, cancellationToken);
        var activeItems = await dbContext.InventoryItems.AsNoTracking()
            .CountAsync(item => item.Status == CatalogStatus.Active, cancellationToken);
        var stock = await dbContext.StockBalances.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new StockProjection(
                group.Count(),
                group.Sum(item => item.QuantityOnHand * item.ReportingAverageUnitCost)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new StockProjection(0, 0m);
        var lowStockItems = await (from item in dbContext.InventoryItems.AsNoTracking()
                                   where item.Status == CatalogStatus.Active
                                   join balance in dbContext.StockBalances.AsNoTracking()
                                       on item.Id equals balance.InventoryItemId into balances
                                   select new
                                   {
                                       item.MinimumStockLevel,
                                       QuantityOnHand = balances.Sum(balance => (decimal?)balance.QuantityOnHand) ?? 0m
                                   })
            .CountAsync(item => item.QuantityOnHand < item.MinimumStockLevel, cancellationToken);
        var workOrders = await dbContext.MaintenanceWorkOrders.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new WorkOrderProjection(
                group.Count(item => item.Status == MaintenanceWorkOrderStatus.Open),
                group.Count(item => item.Status == MaintenanceWorkOrderStatus.InProgress)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new WorkOrderProjection(0, 0);
        var pendingRequests = await dbContext.InventorySupplyRequests.AsNoTracking()
            .CountAsync(item => item.Status == InventorySupplyRequestStatus.PendingWarehouseApproval, cancellationToken);

        return new MaintenanceInventoryDashboardReportResponse(
            activeLocations,
            activeItems,
            stock.StockBalanceRecords,
            lowStockItems,
            stock.InventoryValue,
            workOrders.OpenWorkOrders,
            workOrders.InProgressWorkOrders,
            pendingRequests);
    }

    private sealed record HeadcountProjection(
        int TotalPeople,
        int TotalEmployees,
        int TotalRiders,
        int ActivePeople,
        int ActiveEmployees,
        int ActiveRiders,
        int PeopleWithoutSponsor);

    private sealed record PlatformCoverageProjection(
        Guid Id,
        string Code,
        string NameAr,
        string NameEn,
        int OperationalAccountCount,
        int AssignedAccountCount,
        int ActiveRidersWithAccount);

    private sealed record FleetProjection(
        int TotalVehicles,
        int AvailableVehicles,
        int AssignedVehicles,
        int HeldVehicles,
        int DecommissionedVehicles);

    private sealed record DocumentProjection(int ActiveDocuments, int ExpiredDocuments);
    private sealed record LicenseProjection(int ActiveLicenses, int ExpiredLicenses);
    private sealed record LeaveProjection(int PendingLeaveRequests, int ActiveLeaveRequests);
    private sealed record PlatformAccountProjection(int OperationalAccountCount, int AssignedAccountCount);
    private sealed record PhoneSimProjection(int TotalPhoneSims, int AvailablePhoneSims, int AssignedPhoneSims, int PhoneSimsNeedingAttention);
    private sealed record StockProjection(int StockBalanceRecords, decimal InventoryValue);
    private sealed record WorkOrderProjection(int OpenWorkOrders, int InProgressWorkOrders);
}
