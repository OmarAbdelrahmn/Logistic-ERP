using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class VehicleManufacturerConfiguration : IEntityTypeConfiguration<VehicleManufacturer>
{
    public void Configure(EntityTypeBuilder<VehicleManufacturer> builder)
    {
        builder.ConfigureOperational("VehicleManufacturers");
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        builder.ConfigureOperational("VehicleModels");
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.HasOne<VehicleManufacturer>().WithMany().HasForeignKey(x => x.VehicleManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.Id, x.VehicleManufacturerId });
        builder.HasIndex(x => new { x.VehicleManufacturerId, x.Code }).IsUnique();
    }
}

internal sealed class FleetLocationConfiguration : IEntityTypeConfiguration<FleetLocation>
{
    public void Configure(EntityTypeBuilder<FleetLocation> builder)
    {
        builder.ConfigureOperational("FleetLocations");
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.HasOne<Housing>().WithMany().HasForeignKey(x => x.HousingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.LocationType, x.Status });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_FleetLocations_Housing",
            "([LocationType] = 2 AND [HousingId] IS NOT NULL) OR ([LocationType] <> 2 AND [HousingId] IS NULL)"));
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ConfigureOperational("Vehicles");
        builder.Property(x => x.AssetNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedAssetNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PlateNumberAr).HasMaxLength(32);
        builder.Property(x => x.NormalizedPlateNumberAr).HasMaxLength(32);
        builder.Property(x => x.PlateNumberEn).HasMaxLength(32);
        builder.Property(x => x.NormalizedPlateNumberEn).HasMaxLength(32);
        builder.Property(x => x.PlateLettersAr).HasMaxLength(8);
        builder.Property(x => x.PlateLettersEn).HasMaxLength(8);
        builder.Property(x => x.PlateDigits).HasMaxLength(8);
        builder.Property(x => x.Vin).HasMaxLength(64);
        builder.Property(x => x.ChassisNumber).HasMaxLength(100);
        builder.Property(x => x.EngineNumber).HasMaxLength(100);
        builder.Property(x => x.ColorAr).HasMaxLength(100);
        builder.Property(x => x.ColorEn).HasMaxLength(100);
        builder.Property(x => x.OwnerName).HasMaxLength(200);
        builder.Property(x => x.LeaseReference).HasMaxLength(200);
        builder.Property(x => x.DecommissionReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<VehicleManufacturer>().WithMany().HasForeignKey(x => x.VehicleManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleModel>().WithMany()
            .HasForeignKey(x => new { x.VehicleModelId, x.VehicleManufacturerId })
            .HasPrincipalKey(x => new { x.Id, x.VehicleManufacturerId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FleetLocation>().WithMany().HasForeignKey(x => x.CurrentLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.NormalizedAssetNumber).IsUnique();
        builder.HasIndex(x => x.NormalizedPlateNumberAr).IsUnique().HasFilter("[NormalizedPlateNumberAr] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.NormalizedPlateNumberEn).IsUnique().HasFilter("[NormalizedPlateNumberEn] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.Vin).IsUnique().HasFilter("[Vin] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.CurrentOperationalStatus, x.CurrentLocationId });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Vehicles_Odometer", "[CurrentOdometer] >= 0");
            table.HasCheckConstraint("CK_Vehicles_ModelYear", "[ModelYear] IS NULL OR ([ModelYear] >= 1950 AND [ModelYear] <= 2200)");
        });
    }
}

internal sealed class VehicleOperationalStatusPeriodConfiguration : IEntityTypeConfiguration<VehicleOperationalStatusPeriod>
{
    public void Configure(EntityTypeBuilder<VehicleOperationalStatusPeriod> builder)
    {
        builder.ConfigureOperational("VehicleOperationalStatusPeriods");
        builder.Property(x => x.ReasonCode).HasMaxLength(100);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.EffectiveFromUtc });
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[EffectiveToUtc] IS NULL AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint("CK_VehicleStatusPeriods_Range", "[EffectiveToUtc] IS NULL OR [EffectiveToUtc] >= [EffectiveFromUtc]"));
    }
}

internal sealed class VehicleOdometerReadingConfiguration : IEntityTypeConfiguration<VehicleOdometerReading>
{
    public void Configure(EntityTypeBuilder<VehicleOdometerReading> builder)
    {
        builder.ConfigureHistory("VehicleOdometerReadings");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CorrectionReason).HasMaxLength(1000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.RecordedAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_VehicleOdometerReadings_Value", "[Reading] >= 0");
            table.HasCheckConstraint("CK_VehicleOdometerReadings_Correction", "[IsCorrection] = 0 OR [CorrectionReason] IS NOT NULL");
        });
    }
}

internal sealed class RiderVehicleAssignmentConfiguration : IEntityTypeConfiguration<RiderVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<RiderVehicleAssignment> builder)
    {
        builder.ConfigureOperational("RiderVehicleAssignments");
        builder.Property(x => x.PermissionReference).HasMaxLength(200);
        builder.Property(x => x.AssignmentReason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CompletionReason).HasMaxLength(1000);
        builder.Property(x => x.BackdatedReason).HasMaxLength(1000);
        builder.Property(x => x.CorrectionReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany()
            .HasForeignKey(x => new { x.RiderProfileId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.Id, x.EmployeeId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FleetLocation>().WithMany().HasForeignKey(x => x.StartLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FleetLocation>().WithMany().HasForeignKey(x => x.EndLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.PreviousAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.CorrectionOfAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.RiderProfileId).IsUnique().HasFilter("[EndedAtUtc] IS NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[EndedAtUtc] IS NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.RiderProfileId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.VehicleId, x.StartedAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_RiderVehicleAssignments_TimeRange", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
            table.HasCheckConstraint("CK_RiderVehicleAssignments_Odometer", "[StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= [StartOdometer] OR [CorrectionReason] IS NOT NULL)");
            table.HasCheckConstraint("CK_RiderVehicleAssignments_StartFuel", "[StartFuelLevelPercentage] IS NULL OR [StartFuelLevelPercentage] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_RiderVehicleAssignments_EndFuel", "[EndFuelLevelPercentage] IS NULL OR [EndFuelLevelPercentage] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_RiderVehicleAssignments_Permission", "[PermissionEndsOn] IS NULL OR [PermissionStartsOn] IS NULL OR [PermissionEndsOn] >= [PermissionStartsOn]");
            table.HasCheckConstraint("CK_RiderVehicleAssignments_Backdated", "[WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL");
        });
    }
}

internal sealed class RiderVehicleAssignmentEventConfiguration : IEntityTypeConfiguration<RiderVehicleAssignmentEvent>
{
    public void Configure(EntityTypeBuilder<RiderVehicleAssignmentEvent> builder)
    {
        builder.ConfigureHistory("RiderVehicleAssignmentEvents");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ChangeSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RiderVehicleAssignmentId, x.OccurredAtUtc });
        builder.HasIndex(x => x.OperationId);
    }
}

internal sealed class FleetCommandReceiptConfiguration : IEntityTypeConfiguration<FleetCommandReceipt>
{
    public void Configure(EntityTypeBuilder<FleetCommandReceipt> builder)
    {
        builder.ConfigureHistory("FleetCommandReceipts");
        builder.Property(x => x.CommandName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(x => new { x.CommandName, x.IdempotencyKey }).IsUnique();
    }
}

internal sealed class VehicleRegistrationConfiguration : IEntityTypeConfiguration<VehicleRegistration>
{
    public void Configure(EntityTypeBuilder<VehicleRegistration> builder) => ConfigureCompliance(builder, "VehicleRegistrations", x => x.RegistrationNumber);

    private static void ConfigureCompliance(EntityTypeBuilder<VehicleRegistration> builder, string table, System.Linq.Expressions.Expression<Func<VehicleRegistration, string>> number)
    {
        builder.ConfigureOperational(table);
        builder.Property(number).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IssuingAuthority).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleRegistration>().WithMany().HasForeignKey(x => x.PreviousRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.ExpiryDate, x.IsCurrent });
        builder.ToTable(t => t.HasCheckConstraint("CK_VehicleRegistrations_DateRange", "[ExpiryDate] >= [IssueDate]"));
    }
}

internal sealed class VehicleInsurancePolicyConfiguration : IEntityTypeConfiguration<VehicleInsurancePolicy>
{
    public void Configure(EntityTypeBuilder<VehicleInsurancePolicy> builder)
    {
        builder.ConfigureOperational("VehicleInsurancePolicies");
        builder.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PolicyNumber).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CoverageType).HasMaxLength(200);
        builder.Property(x => x.ClaimReference).HasMaxLength(200);
        builder.Property(x => x.ClaimContact).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleInsurancePolicy>().WithMany().HasForeignKey(x => x.PreviousRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.PolicyNumber }).IsUnique();
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.ExpiryDate, x.IsCurrent });
        builder.ToTable(t => t.HasCheckConstraint("CK_VehicleInsurancePolicies_DateRange", "[ExpiryDate] >= [EffectiveFrom]"));
    }
}

internal sealed class VehiclePeriodicInspectionConfiguration : IEntityTypeConfiguration<VehiclePeriodicInspection>
{
    public void Configure(EntityTypeBuilder<VehiclePeriodicInspection> builder)
    {
        builder.ConfigureOperational("VehiclePeriodicInspections");
        builder.Property(x => x.InspectionNumber).HasMaxLength(150).IsRequired();
        builder.Property(x => x.StationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FailureNotes).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehiclePeriodicInspection>().WithMany().HasForeignKey(x => x.PreviousRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.InspectionNumber }).IsUnique();
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.ExpiryDate, x.IsCurrent });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VehiclePeriodicInspections_DateRange", "[ExpiryDate] >= [InspectionDate]");
            t.HasCheckConstraint("CK_VehiclePeriodicInspections_Odometer", "[Odometer] IS NULL OR [Odometer] >= 0");
        });
    }
}

internal sealed class VehicleAttachmentConfiguration : IEntityTypeConfiguration<VehicleAttachment>
{
    public void Configure(EntityTypeBuilder<VehicleAttachment> builder)
    {
        builder.ConfigureOperational("VehicleAttachments");
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAttachmentVersion>().WithMany().HasForeignKey(x => x.CurrentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.IsDeleted });
    }
}

internal sealed class VehicleAttachmentVersionConfiguration : IEntityTypeConfiguration<VehicleAttachmentVersion>
{
    public void Configure(EntityTypeBuilder<VehicleAttachmentVersion> builder)
    {
        builder.ConfigureHistory("VehicleAttachmentVersions");
        ConfigureFile(builder);
        builder.HasOne<VehicleAttachment>().WithMany().HasForeignKey(x => x.VehicleAttachmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAttachmentVersion>().WithMany().HasForeignKey(x => x.SupersededVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleAttachmentId, x.VersionNumber }).IsUnique();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VehicleAttachmentVersions_Version", "[VersionNumber] > 0");
            t.HasCheckConstraint("CK_VehicleAttachmentVersions_Size", "[FileSizeBytes] > 0");
        });
    }

    internal static void ConfigureFile<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property("OriginalFileName").HasMaxLength(255).IsRequired();
        builder.Property("StoredFileName").HasMaxLength(255).IsRequired();
        builder.Property("ContentType").HasMaxLength(150).IsRequired();
        builder.Property("Sha256Checksum").HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property("StoragePath").HasMaxLength(1000).IsRequired();
    }
}

internal sealed class VehicleIssueConfiguration : IEntityTypeConfiguration<VehicleIssue>
{
    public void Configure(EntityTypeBuilder<VehicleIssue> builder)
    {
        builder.ConfigureOperational("VehicleIssues");
        builder.Property(x => x.IssueNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ResolutionSummary).HasMaxLength(4000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FleetLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RelatedAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IssueNumber).IsUnique();
        builder.HasIndex(x => new { x.VehicleId, x.Status, x.BlocksOperation });
    }
}

internal sealed class VehicleIssueEventConfiguration : IEntityTypeConfiguration<VehicleIssueEvent>
{
    public void Configure(EntityTypeBuilder<VehicleIssueEvent> builder)
    {
        builder.ConfigureHistory("VehicleIssueEvents");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        builder.HasOne<VehicleIssue>().WithMany().HasForeignKey(x => x.VehicleIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleIssueId, x.OccurredAtUtc });
    }
}

internal sealed class VehicleAccidentConfiguration : IEntityTypeConfiguration<VehicleAccident>
{
    public void Configure(EntityTypeBuilder<VehicleAccident> builder)
    {
        builder.ConfigureOperational("VehicleAccidents");
        builder.Property(x => x.AccidentNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationDescription).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.PoliceReportNumber).HasMaxLength(150);
        builder.Property(x => x.InsuranceClaimNumber).HasMaxLength(150);
        builder.Property(x => x.InjuryDetails).HasMaxLength(4000);
        builder.Property(x => x.ThirdPartyDetails).HasMaxLength(4000);
        builder.Property(x => x.DamageDescription).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.FaultAssessment).HasMaxLength(2000);
        builder.Property(x => x.Narrative).HasMaxLength(8000).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany()
            .HasForeignKey(x => new { x.RiderProfileId, x.EmployeeId })
            .HasPrincipalKey(x => new { x.Id, x.EmployeeId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleIssue>().WithMany().HasForeignKey(x => x.VehicleIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleInsurancePolicy>().WithMany().HasForeignKey(x => x.VehicleInsurancePolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FleetLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAccidentReportVersion>().WithMany().HasForeignKey(x => x.CurrentReportVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AccidentNumber).IsUnique();
        builder.HasIndex(x => new { x.VehicleId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.RiderProfileId, x.OccurredAtUtc });
    }
}

internal sealed class VehicleAccidentEventConfiguration : IEntityTypeConfiguration<VehicleAccidentEvent>
{
    public void Configure(EntityTypeBuilder<VehicleAccidentEvent> builder)
    {
        builder.ConfigureHistory("VehicleAccidentEvents");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        builder.HasOne<VehicleAccident>().WithMany().HasForeignKey(x => x.VehicleAccidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleAccidentId, x.OccurredAtUtc });
    }
}

internal sealed class VehicleAccidentAttachmentConfiguration : IEntityTypeConfiguration<VehicleAccidentAttachment>
{
    public void Configure(EntityTypeBuilder<VehicleAccidentAttachment> builder)
    {
        builder.ConfigureOperational("VehicleAccidentAttachments");
        VehicleAttachmentVersionConfiguration.ConfigureFile(builder);
        builder.HasOne<VehicleAccident>().WithMany().HasForeignKey(x => x.VehicleAccidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleAccidentId, x.IsDeleted });
        builder.ToTable(t => t.HasCheckConstraint("CK_VehicleAccidentAttachments_Size", "[FileSizeBytes] > 0"));
    }
}

internal sealed class VehicleAccidentReportVersionConfiguration : IEntityTypeConfiguration<VehicleAccidentReportVersion>
{
    public void Configure(EntityTypeBuilder<VehicleAccidentReportVersion> builder)
    {
        builder.ConfigureHistory("VehicleAccidentReportVersions");
        builder.Property(x => x.ReportNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.CorrectionReason).HasMaxLength(1000);
        builder.HasOne<VehicleAccident>().WithMany().HasForeignKey(x => x.VehicleAccidentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAccidentReportVersion>().WithMany().HasForeignKey(x => x.SupersedesReportVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleAccidentId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.ReportNumber).IsUnique();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VehicleAccidentReportVersions_Version", "[VersionNumber] > 0");
            t.HasCheckConstraint("CK_VehicleAccidentReportVersions_Size", "[FileSizeBytes] > 0");
        });
    }
}
