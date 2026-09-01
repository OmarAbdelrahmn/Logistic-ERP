using System.Reflection;
using LogisticsERP.Application.Abstractions.Persistence;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Entities.Tags;
using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using HousingEntity = LogisticsERP.Domain.Entities.Housing.Housing;
using HousingResidencePeriod = LogisticsERP.Domain.Entities.Housing.HousingResidencePeriod;
using HousingSupervisorPeriod = LogisticsERP.Domain.Entities.Housing.HousingSupervisorPeriod;

namespace LogisticsERP.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider? timeProvider = null)
    : DbContext(options), IApplicationDbContext
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<GlobalCity> GlobalCities => Set<GlobalCity>();
    public DbSet<OperatingCity> OperatingCities => Set<OperatingCity>();
    public DbSet<ClientPlatform> ClientPlatforms => Set<ClientPlatform>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollEmployee> PayrollEmployees => Set<PayrollEmployee>();
    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();
    public DbSet<EmployeeWorkHistory> EmployeeWorkHistory => Set<EmployeeWorkHistory>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<ResidencyProfession> ResidencyProfessions => Set<ResidencyProfession>();
    public DbSet<OperationalWorkType> OperationalWorkTypes => Set<OperationalWorkType>();
    public DbSet<JobTitleOperationalWorkType> JobTitleOperationalWorkTypes => Set<JobTitleOperationalWorkType>();
    public DbSet<DriverLicenseCategory> DriverLicenseCategories => Set<DriverLicenseCategory>();
    public DbSet<EmployeeDriverLicense> EmployeeDriverLicenses => Set<EmployeeDriverLicense>();
    public DbSet<RiderCard> RiderCards => Set<RiderCard>();
    public DbSet<RiderHealthCard> RiderHealthCards => Set<RiderHealthCard>();
    public DbSet<EmployeePromissoryNote> EmployeePromissoryNotes => Set<EmployeePromissoryNote>();
    public DbSet<HrFormTemplate> HrFormTemplates => Set<HrFormTemplate>();
    public DbSet<HrFormTemplateVersion> HrFormTemplateVersions => Set<HrFormTemplateVersion>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
    public DbSet<InsurancePlanLevel> InsurancePlanLevels => Set<InsurancePlanLevel>();
    public DbSet<EmployeeMedicalInsurancePolicy> EmployeeMedicalInsurancePolicies => Set<EmployeeMedicalInsurancePolicy>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveApprovalWorkflow> LeaveApprovalWorkflows => Set<LeaveApprovalWorkflow>();
    public DbSet<LeaveApprovalWorkflowStep> LeaveApprovalWorkflowSteps => Set<LeaveApprovalWorkflowStep>();
    public DbSet<LeaveApprovalDecision> LeaveApprovalDecisions => Set<LeaveApprovalDecision>();
    public DbSet<LeaveDateChangeRequest> LeaveDateChangeRequests => Set<LeaveDateChangeRequest>();
    public DbSet<LeaveCancellationRequest> LeaveCancellationRequests => Set<LeaveCancellationRequest>();
    public DbSet<LeaveRequestDocument> LeaveRequestDocuments => Set<LeaveRequestDocument>();
    public DbSet<LeaveRequestDocumentVersion> LeaveRequestDocumentVersions => Set<LeaveRequestDocumentVersion>();
    public DbSet<EmployeeAbsenceComplianceCase> EmployeeAbsenceComplianceCases => Set<EmployeeAbsenceComplianceCase>();
    public DbSet<EmployeeAbsenceComplianceCaseEvent> EmployeeAbsenceComplianceCaseEvents => Set<EmployeeAbsenceComplianceCaseEvent>();
    public DbSet<EmployeeStatusChangeRequest> EmployeeStatusChangeRequests => Set<EmployeeStatusChangeRequest>();
    public DbSet<HousingEntity> Housing => Set<HousingEntity>();
    public DbSet<HousingSupervisorPeriod> HousingSupervisorPeriods => Set<HousingSupervisorPeriod>();
    public DbSet<HousingResidencePeriod> HousingResidencePeriods => Set<HousingResidencePeriod>();
    public DbSet<ClientContract> ClientContracts => Set<ClientContract>();
    public DbSet<PlatformRiderAccount> PlatformRiderAccounts => Set<PlatformRiderAccount>();
    public DbSet<PlatformAccountCredentialVersion> PlatformAccountCredentialVersions => Set<PlatformAccountCredentialVersion>();
    public DbSet<RiderClientAssignment> RiderClientAssignments => Set<RiderClientAssignment>();
    public DbSet<RiderAssignmentEvent> RiderAssignmentEvents => Set<RiderAssignmentEvent>();
    public DbSet<PlatformAccountRegistration> PlatformAccountRegistrations => Set<PlatformAccountRegistration>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<EmployeeDocumentVersion> EmployeeDocumentVersions => Set<EmployeeDocumentVersion>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EmployeeTag> EmployeeTags => Set<EmployeeTag>();
    public DbSet<HousingTag> HousingTags => Set<HousingTag>();
    public DbSet<ClientContractTag> ClientContractTags => Set<ClientContractTag>();
    public DbSet<PlatformRiderAccountTag> PlatformRiderAccountTags => Set<PlatformRiderAccountTag>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<DatasetVersion> DatasetVersions => Set<DatasetVersion>();
    public DbSet<VehicleManufacturer> VehicleManufacturers => Set<VehicleManufacturer>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleSupplier> VehicleSuppliers => Set<VehicleSupplier>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleIdentityCorrection> VehicleIdentityCorrections => Set<VehicleIdentityCorrection>();
    public DbSet<VehicleRegistrationTransition> VehicleRegistrationTransitions => Set<VehicleRegistrationTransition>();
    public DbSet<VehicleOperationalStatusPeriod> VehicleOperationalStatusPeriods => Set<VehicleOperationalStatusPeriod>();
    public DbSet<VehicleOdometerReading> VehicleOdometerReadings => Set<VehicleOdometerReading>();
    public DbSet<RiderVehicleAssignment> RiderVehicleAssignments => Set<RiderVehicleAssignment>();
    public DbSet<RealRider> RealRiders => Set<RealRider>();
    public DbSet<VehiclePlatformAccountAssignment> VehiclePlatformAccountAssignments => Set<VehiclePlatformAccountAssignment>();
    public DbSet<VehiclePlatformAccountSwitch> VehiclePlatformAccountSwitches => Set<VehiclePlatformAccountSwitch>();
    public DbSet<RiderVehicleAssignmentEvent> RiderVehicleAssignmentEvents => Set<RiderVehicleAssignmentEvent>();
    public DbSet<RiderVehicleAssignmentPromissoryFile> RiderVehicleAssignmentPromissoryFiles => Set<RiderVehicleAssignmentPromissoryFile>();
    public DbSet<FleetCommandReceipt> FleetCommandReceipts => Set<FleetCommandReceipt>();
    public DbSet<VehicleRegistration> VehicleRegistrations => Set<VehicleRegistration>();
    public DbSet<VehicleInsurancePolicy> VehicleInsurancePolicies => Set<VehicleInsurancePolicy>();
    public DbSet<VehiclePeriodicInspection> VehiclePeriodicInspections => Set<VehiclePeriodicInspection>();
    public DbSet<VehicleOperationCard> VehicleOperationCards => Set<VehicleOperationCard>();
    public DbSet<VehicleAttachment> VehicleAttachments => Set<VehicleAttachment>();
    public DbSet<VehicleAttachmentVersion> VehicleAttachmentVersions => Set<VehicleAttachmentVersion>();
    public DbSet<RiderPromissoryFile> RiderPromissoryFiles => Set<RiderPromissoryFile>();
    public DbSet<RiderPromissoryFileVersion> RiderPromissoryFileVersions => Set<RiderPromissoryFileVersion>();
    public DbSet<VehicleIssue> VehicleIssues => Set<VehicleIssue>();
    public DbSet<VehicleIssueEvent> VehicleIssueEvents => Set<VehicleIssueEvent>();
    public DbSet<VehicleAccident> VehicleAccidents => Set<VehicleAccident>();
    public DbSet<VehicleAccidentEvent> VehicleAccidentEvents => Set<VehicleAccidentEvent>();
    public DbSet<VehicleAccidentAttachment> VehicleAccidentAttachments => Set<VehicleAccidentAttachment>();
    public DbSet<VehicleAccidentReportVersion> VehicleAccidentReportVersions => Set<VehicleAccidentReportVersion>();
    public DbSet<PhoneSimCard> PhoneSimCards => Set<PhoneSimCard>();
    public DbSet<RiderPhoneSimAssignment> RiderPhoneSimAssignments => Set<RiderPhoneSimAssignment>();
    public DbSet<PhoneSimResponsibilityChange> PhoneSimResponsibilityChanges => Set<PhoneSimResponsibilityChange>();

    public IQueryable<TEntity> Query<TEntity>() where TEntity : Entity => Set<TEntity>();

    public void AddEntity<TEntity>(TEntity entity) where TEntity : Entity => Set<TEntity>().Add(entity);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateDatasetVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await UpdateDatasetVersionsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("AuditEntrySequence", "audit").StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => type.Namespace?.StartsWith("LogisticsERP.Infrastructure.Persistence.Configurations", StringComparison.Ordinal) == true);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            if (!entityType.ClrType.IsAssignableTo(typeof(AuditableEntity)))
            {
                continue;
            }

            var method = GetType().GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find the soft-delete query filter method.");
            method.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => !entity.IsDeleted);

    private void UpdateDatasetVersions()
    {
        var moduleKeys = GetChangedModuleKeys();
        var now = _timeProvider.GetUtcNow();
        foreach (var moduleKey in moduleKeys)
        {
            var version = DatasetVersions.Local.FirstOrDefault(item => item.ModuleKey == moduleKey)
                ?? DatasetVersions.IgnoreQueryFilters().SingleOrDefault(item => item.ModuleKey == moduleKey);
            IncrementDatasetVersion(version, moduleKey, now);
        }
    }

    private async Task UpdateDatasetVersionsAsync(CancellationToken cancellationToken)
    {
        var moduleKeys = GetChangedModuleKeys();
        var now = _timeProvider.GetUtcNow();
        foreach (var moduleKey in moduleKeys)
        {
            var version = DatasetVersions.Local.FirstOrDefault(item => item.ModuleKey == moduleKey)
                ?? await DatasetVersions.IgnoreQueryFilters().SingleOrDefaultAsync(
                    item => item.ModuleKey == moduleKey,
                    cancellationToken);
            IncrementDatasetVersion(version, moduleKey, now);
        }
    }

    private string[] GetChangedModuleKeys() => ChangeTracker.Entries<Entity>()
        .Where(entry => entry.Entity is not AuditEntry and not DatasetVersion
            && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .Select(entry => GetModuleKey(entry.Metadata.ClrType))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    private void IncrementDatasetVersion(DatasetVersion? version, string moduleKey, DateTimeOffset now)
    {
        if (version is null)
        {
            DatasetVersions.Add(new DatasetVersion
            {
                Id = Guid.CreateVersion7(),
                ModuleKey = moduleKey,
                Version = 1,
                LastChangedAtUtc = now
            });
            return;
        }

        version.Version++;
        version.LastChangedAtUtc = now;
    }

    private static string GetModuleKey(Type entityType)
    {
        var entityNamespace = entityType.Namespace ?? string.Empty;
        if (entityNamespace.EndsWith(".Fleet", StringComparison.Ordinal)) return "fleet";
        if (entityNamespace.EndsWith(".Tags", StringComparison.Ordinal)) return "tags";
        if (entityNamespace.EndsWith(".Documents", StringComparison.Ordinal)) return "documents";
        if (entityNamespace.EndsWith(".Housing", StringComparison.Ordinal)) return "housing";
        if (entityNamespace.EndsWith(".Clients", StringComparison.Ordinal)) return "platform-operations";
        if (entityNamespace.EndsWith(".Telecom", StringComparison.Ordinal)) return "telecom";
        if (entityNamespace.EndsWith(".Platform", StringComparison.Ordinal)) return "catalog";
        if (entityNamespace.EndsWith(".Workforce", StringComparison.Ordinal)) return "workforce";

        return entityType.Name switch
        {
            nameof(Notification) => "notifications",
            nameof(ExportJob) => "exports",
            nameof(SavedView) => "saved-views",
            _ => "system"
        };
    }
}
