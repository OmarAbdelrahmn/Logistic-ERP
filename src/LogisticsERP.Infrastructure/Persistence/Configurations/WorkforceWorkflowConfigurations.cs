using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ConfigureOperational("LeaveTypes");
        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_LeaveTypes_MaximumCalendarDays",
            "[MaximumCalendarDays] IS NULL OR [MaximumCalendarDays] > 0"));
    }
}

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ConfigureOperational("LeaveRequests");
        builder.Property(entity => entity.RequestNumber).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.DestinationCountryCode).HasMaxLength(2).IsFixedLength();
        builder.Property(entity => entity.ContactPhoneDuringLeave).HasMaxLength(32);
        builder.Property(entity => entity.EmergencyContactName).HasMaxLength(200);
        builder.Property(entity => entity.EmergencyContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.ApprovalWorkflowSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.CurrentApprovalStepKey).HasMaxLength(100);
        builder.Property(entity => entity.RejectionReason).HasMaxLength(1000);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(1000);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(entity => entity.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveApprovalWorkflow>().WithMany().HasForeignKey(entity => entity.ApprovalWorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientContract>().WithMany().HasForeignKey(entity => entity.RelatedClientContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RequestNumber).IsUnique();
        builder.HasIndex(entity => new { entity.EmployeeId, entity.StartDate, entity.EndDate });
        builder.HasIndex(entity => new { entity.Status, entity.CurrentApprovalStepKey, entity.SubmittedAtUtc });
        builder.HasIndex(entity => new { entity.HrStatus, entity.StartDate });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeaveRequests_DateRange", "[EndDate] >= [StartDate]");
            table.HasCheckConstraint("CK_LeaveRequests_ExpectedReturn", "[ExpectedReturnDate] >= [EndDate]");
            table.HasCheckConstraint("CK_LeaveRequests_CalendarDays", "[CalendarDays] = DATEDIFF(DAY, [StartDate], [EndDate]) + 1");
        });
    }
}

internal sealed class LeaveApprovalWorkflowConfiguration : IEntityTypeConfiguration<LeaveApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<LeaveApprovalWorkflow> builder)
    {
        builder.ConfigureOperational("LeaveApprovalWorkflows");
        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(150).IsRequired();
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(entity => entity.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientPlatform>().WithMany().HasForeignKey(entity => entity.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.Code, entity.Version }).IsUnique();
        builder.HasIndex(entity => new
        {
            entity.Status,
            entity.Priority,
            entity.LeaveTypeId,
            entity.RelationshipType,
            entity.AppliesToRider,
            entity.ClientPlatformId
        });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeaveApprovalWorkflows_Version", "[Version] > 0");
            table.HasCheckConstraint("CK_LeaveApprovalWorkflows_DateRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
    }
}

internal sealed class LeaveApprovalWorkflowStepConfiguration : IEntityTypeConfiguration<LeaveApprovalWorkflowStep>
{
    public void Configure(EntityTypeBuilder<LeaveApprovalWorkflowStep> builder)
    {
        builder.ConfigureOperational("LeaveApprovalWorkflowSteps");
        builder.Property(entity => entity.StepKey).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.RequiredPermissionKey).HasMaxLength(150).IsRequired();
        builder.HasOne<LeaveApprovalWorkflow>().WithMany().HasForeignKey(entity => entity.LeaveApprovalWorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveApprovalWorkflowId, entity.StepKey }).IsUnique();
        builder.HasIndex(entity => new { entity.LeaveApprovalWorkflowId, entity.Sequence }).IsUnique();
        builder.HasIndex(entity => entity.RequiredPermissionKey);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeaveApprovalWorkflowSteps_Sequence", "[Sequence] > 0");
            table.HasCheckConstraint("CK_LeaveApprovalWorkflowSteps_TargetHours", "[TargetResponseHours] IS NULL OR [TargetResponseHours] > 0");
        });
    }
}

internal sealed class LeaveApprovalDecisionConfiguration : IEntityTypeConfiguration<LeaveApprovalDecision>
{
    public void Configure(EntityTypeBuilder<LeaveApprovalDecision> builder)
    {
        builder.ConfigureHistory("LeaveApprovalDecisions");
        builder.Property(entity => entity.StepKey).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RequiredPermissionKey).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.ReturnedToStepKey).HasMaxLength(100);
        builder.Property(entity => entity.Comment).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.AuthorizationSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasOne<LeaveRequest>().WithMany().HasForeignKey(entity => entity.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveRequestId, entity.StepSequence, entity.DecidedAtUtc });
        builder.HasIndex(entity => new { entity.DecidedByUserId, entity.DecidedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_LeaveApprovalDecisions_StepSequence", "[StepSequence] > 0"));
    }
}

internal sealed class LeaveDateChangeRequestConfiguration : IEntityTypeConfiguration<LeaveDateChangeRequest>
{
    public void Configure(EntityTypeBuilder<LeaveDateChangeRequest> builder)
    {
        builder.ConfigureOperational("LeaveDateChangeRequests");
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ResolutionReason).HasMaxLength(1000);
        builder.HasOne<LeaveRequest>().WithMany().HasForeignKey(entity => entity.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveRequestId, entity.RequestedAtUtc });
        builder.HasIndex(entity => entity.LeaveRequestId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeaveDateChangeRequests_PreviousRange", "[PreviousEndDate] >= [PreviousStartDate]");
            table.HasCheckConstraint("CK_LeaveDateChangeRequests_RequestedRange", "[RequestedEndDate] >= [RequestedStartDate]");
        });
    }
}

internal sealed class LeaveCancellationRequestConfiguration : IEntityTypeConfiguration<LeaveCancellationRequest>
{
    public void Configure(EntityTypeBuilder<LeaveCancellationRequest> builder)
    {
        builder.ConfigureOperational("LeaveCancellationRequests");
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ResolutionReason).HasMaxLength(1000);
        builder.HasOne<LeaveRequest>().WithMany().HasForeignKey(entity => entity.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveRequestId, entity.RequestedAtUtc });
        builder.HasIndex(entity => entity.LeaveRequestId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");
    }
}

internal sealed class LeaveRequestDocumentConfiguration : IEntityTypeConfiguration<LeaveRequestDocument>
{
    public void Configure(EntityTypeBuilder<LeaveRequestDocument> builder)
    {
        builder.ConfigureOperational("LeaveRequestDocuments");
        builder.Property(entity => entity.ReferenceNumber).HasMaxLength(150);
        builder.Property(entity => entity.Notes).HasMaxLength(2000);
        builder.HasOne<LeaveRequest>().WithMany().HasForeignKey(entity => entity.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveRequestDocumentVersion>().WithMany().HasForeignKey(entity => entity.CurrentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveRequestId, entity.Kind });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_LeaveRequestDocuments_DateRange",
            "[ExpiresOn] IS NULL OR [IssuedOn] IS NULL OR [ExpiresOn] >= [IssuedOn]"));
    }
}

internal sealed class LeaveRequestDocumentVersionConfiguration : IEntityTypeConfiguration<LeaveRequestDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LeaveRequestDocumentVersion> builder)
    {
        builder.ConfigureHistory("LeaveRequestDocumentVersions");
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(entity => entity.StoragePath).HasMaxLength(1000).IsRequired();
        builder.HasOne<LeaveRequestDocument>().WithMany().HasForeignKey(entity => entity.LeaveRequestDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveRequestDocumentVersion>().WithMany().HasForeignKey(entity => entity.SupersededVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.LeaveRequestDocumentId, entity.VersionNumber }).IsUnique();
        builder.HasIndex(entity => entity.Sha256Checksum);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeaveRequestDocumentVersions_Version", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_LeaveRequestDocumentVersions_FileSize", "[FileSizeBytes] > 0");
        });
    }
}

internal sealed class EmployeeAbsenceComplianceCaseConfiguration : IEntityTypeConfiguration<EmployeeAbsenceComplianceCase>
{
    public void Configure(EntityTypeBuilder<EmployeeAbsenceComplianceCase> builder)
    {
        builder.ConfigureOperational("EmployeeAbsenceComplianceCases");
        builder.Property(entity => entity.CaseNumber).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AuthorityReportReference).HasMaxLength(150);
        builder.Property(entity => entity.ExitVisaNumber).HasMaxLength(150);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.Property(entity => entity.ResolutionCode).HasMaxLength(100);
        builder.Property(entity => entity.ResolutionNotes).HasMaxLength(2000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.CaseNumber).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.RemovalDeadline });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[Status] IN (1, 2, 3, 4) AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_EmployeeAbsenceComplianceCases_PathData",
                "([CurrentPath] = 1 AND [ReportedToAuthoritiesDate] IS NOT NULL AND [ExitOrOutageDate] IS NULL) OR " +
                "([CurrentPath] = 2 AND [ExitOrOutageDate] IS NOT NULL AND [ReportedToAuthoritiesDate] IS NULL)");
            table.HasCheckConstraint(
                "CK_EmployeeAbsenceComplianceCases_Deadline",
                "([CurrentPath] = 1 AND [RemovalDeadline] >= [ReportedToAuthoritiesDate]) OR " +
                "([CurrentPath] = 2 AND [RemovalDeadline] >= [ExitOrOutageDate])");
        });
    }
}

internal sealed class EmployeeAbsenceComplianceCaseEventConfiguration : IEntityTypeConfiguration<EmployeeAbsenceComplianceCaseEvent>
{
    public void Configure(EntityTypeBuilder<EmployeeAbsenceComplianceCaseEvent> builder)
    {
        builder.ConfigureHistory("EmployeeAbsenceComplianceCaseEvents");
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.AfterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100).IsRequired();
        builder.HasOne<EmployeeAbsenceComplianceCase>().WithMany().HasForeignKey(entity => entity.EmployeeAbsenceComplianceCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeAbsenceComplianceCaseId, entity.OccurredAtUtc });
    }
}

internal sealed class EmployeeStatusChangeRequestConfiguration : IEntityTypeConfiguration<EmployeeStatusChangeRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeStatusChangeRequest> builder)
    {
        builder.ConfigureOperational("EmployeeStatusChangeRequests");
        builder.Property(entity => entity.RequestNumber).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ResolutionReason).HasMaxLength(1000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeStatusPeriod>().WithMany().HasForeignKey(entity => entity.ResultingStatusPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RequestNumber).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.RequestedAtUtc });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeStatusChangeRequests_StatusChanged",
            "[FromStatus] <> [RequestedStatus]"));
    }
}
