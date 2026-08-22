using System.Reflection;
using LogisticsERP.Application.Abstractions.Persistence;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Entities.Tags;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using HousingEntity = LogisticsERP.Domain.Entities.Housing.Housing;
using HousingResidencePeriod = LogisticsERP.Domain.Entities.Housing.HousingResidencePeriod;
using HousingSupervisorPeriod = LogisticsERP.Domain.Entities.Housing.HousingSupervisorPeriod;

namespace LogisticsERP.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<GlobalCity> GlobalCities => Set<GlobalCity>();
    public DbSet<OperatingCity> OperatingCities => Set<OperatingCity>();
    public DbSet<ClientPlatform> ClientPlatforms => Set<ClientPlatform>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeStatusPeriod> EmployeeStatusPeriods => Set<EmployeeStatusPeriod>();
    public DbSet<EmployeeRelationshipPeriod> EmployeeRelationshipPeriods => Set<EmployeeRelationshipPeriod>();
    public DbSet<SponsoredInternalDetails> SponsoredInternalDetails => Set<SponsoredInternalDetails>();
    public DbSet<OutsideRiderDetails> OutsideRiderDetails => Set<OutsideRiderDetails>();
    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<EmployeeJobTitlePeriod> EmployeeJobTitlePeriods => Set<EmployeeJobTitlePeriod>();
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

    public IQueryable<TEntity> Query<TEntity>() where TEntity : Entity => Set<TEntity>();

    public void AddEntity<TEntity>(TEntity entity) where TEntity : Entity => Set<TEntity>().Add(entity);

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
}
