using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Platform;
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

internal sealed class VehicleSupplierConfiguration : IEntityTypeConfiguration<VehicleSupplier>
{
    public void Configure(EntityTypeBuilder<VehicleSupplier> builder)
    {
        builder.ConfigureOperational("VehicleSuppliers");
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CommercialRegistrationNumber).HasMaxLength(100);
        builder.Property(x => x.TaxNumber).HasMaxLength(100);
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.OwnsOne(x => x.Address, owned => owned.ConfigureAddress("Address"));
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CommercialRegistrationNumber).IsUnique().HasFilter("[CommercialRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.TaxNumber).IsUnique().HasFilter("[TaxNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.Status, x.NameAr });
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ConfigureOperational("Vehicles");
        builder.Property(x => x.AssetNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedAssetNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.NormalizedSerialNumber).HasMaxLength(100);
        builder.Property(x => x.PlateNumberAr).HasMaxLength(32);
        builder.Property(x => x.NormalizedPlateNumberAr).HasMaxLength(32);
        builder.Property(x => x.PlateNumberEn).HasMaxLength(32);
        builder.Property(x => x.NormalizedPlateNumberEn).HasMaxLength(32);
        builder.Property(x => x.PlateLettersAr).HasMaxLength(8);
        builder.Property(x => x.PlateLettersEn).HasMaxLength(8);
        builder.Property(x => x.PlateDigits).HasMaxLength(8);
        builder.Property(x => x.Vin).HasMaxLength(64);
        builder.Property(x => x.ChassisNumber).HasMaxLength(100);
        builder.Property(x => x.NormalizedChassisNumber).HasMaxLength(100);
        builder.Property(x => x.EngineNumber).HasMaxLength(100);
        builder.Property(x => x.ColorAr).HasMaxLength(100);
        builder.Property(x => x.ColorEn).HasMaxLength(100);
        builder.Property(x => x.OwnerName).HasMaxLength(200);
        builder.Property(x => x.LeaseReference).HasMaxLength(200);
        builder.Property(x => x.DecommissionReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.TrackedDistanceKm).HasPrecision(18, 2);
        builder.HasOne<VehicleManufacturer>().WithMany().HasForeignKey(x => x.VehicleManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleModel>().WithMany()
            .HasForeignKey(x => new { x.VehicleModelId, x.VehicleManufacturerId })
            .HasPrincipalKey(x => new { x.Id, x.VehicleManufacturerId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperatingCity>().WithMany().HasForeignKey(x => x.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleSupplier>().WithMany().HasForeignKey(x => x.PurchasedFromSupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.NormalizedAssetNumber).IsUnique();
        builder.HasIndex(x => x.NormalizedSerialNumber).IsUnique().HasFilter("[NormalizedSerialNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.NormalizedChassisNumber).IsUnique().HasFilter("[NormalizedChassisNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.NormalizedPlateNumberAr).IsUnique().HasFilter("[NormalizedPlateNumberAr] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.NormalizedPlateNumberEn).IsUnique().HasFilter("[NormalizedPlateNumberEn] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.Vin).IsUnique().HasFilter("[Vin] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.CurrentOperationalStatus, x.OperatingCityId });
        builder.HasIndex(x => new { x.SponsorId, x.RegistrationType });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Vehicles_Odometer", "[CurrentOdometer] >= 0");
            table.HasCheckConstraint("CK_Vehicles_TrackedDistanceKm", "[TrackedDistanceKm] >= 0");
            table.HasCheckConstraint("CK_Vehicles_ModelYear", "[ModelYear] IS NULL OR ([ModelYear] >= 1950 AND [ModelYear] <= 2200)");
            table.HasCheckConstraint("CK_Vehicles_RegistrationType", "[RegistrationType] IS NULL OR [RegistrationType] BETWEEN 1 AND 8");
        });
    }
}

internal sealed class VehicleIdentityCorrectionConfiguration : IEntityTypeConfiguration<VehicleIdentityCorrection>
{
    public void Configure(EntityTypeBuilder<VehicleIdentityCorrection> builder)
    {
        builder.ConfigureHistory("VehicleIdentityCorrections");
        builder.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AfterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DocumentVersionReferencesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.EffectiveAtUtc });
    }
}

internal sealed class VehicleRegistrationTransitionConfiguration : IEntityTypeConfiguration<VehicleRegistrationTransition>
{
    public void Configure(EntityTypeBuilder<VehicleRegistrationTransition> builder)
    {
        builder.ConfigureHistory("VehicleRegistrationTransitions");
        builder.Property(x => x.OldPlateNumberAr).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OldPlateNumberEn).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NewPlateNumberAr).HasMaxLength(32).IsRequired();
        builder.Property(x => x.NewPlateNumberEn).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OldPlateLettersAr).HasMaxLength(8);
        builder.Property(x => x.OldPlateLettersEn).HasMaxLength(8);
        builder.Property(x => x.OldPlateDigits).HasMaxLength(8);
        builder.Property(x => x.NewPlateLettersAr).HasMaxLength(8);
        builder.Property(x => x.NewPlateLettersEn).HasMaxLength(8);
        builder.Property(x => x.NewPlateDigits).HasMaxLength(8);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAttachmentVersion>().WithMany().HasForeignKey(x => x.IstimaraVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleAttachmentVersion>().WithMany().HasForeignKey(x => x.OperationCardVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.EffectiveAtUtc });
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

internal sealed class VehicleDailyDistanceConfiguration : IEntityTypeConfiguration<VehicleDailyDistance>
{
    public void Configure(EntityTypeBuilder<VehicleDailyDistance> builder)
    {
        builder.ConfigureOperational("VehicleDailyDistances");
        builder.Property(x => x.GpsDistanceKm).HasPrecision(18, 2);
        builder.Property(x => x.ManualDistanceKm).HasPrecision(18, 2);
        builder.Property(x => x.AppliedDistanceKm).HasPrecision(18, 2);
        builder.Property(x => x.GpsPlateNumber).HasMaxLength(64);
        builder.Property(x => x.ManualNotes).HasMaxLength(1000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleDailyDistanceImport>().WithMany().HasForeignKey(x => x.LastGpsImportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.WorkDate }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.WorkDate, x.AppliedSource });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_VehicleDailyDistances_GpsDistance", "[GpsDistanceKm] IS NULL OR [GpsDistanceKm] >= 0");
            table.HasCheckConstraint("CK_VehicleDailyDistances_ManualDistance", "[ManualDistanceKm] IS NULL OR [ManualDistanceKm] >= 0");
            table.HasCheckConstraint("CK_VehicleDailyDistances_AppliedDistance", "[AppliedDistanceKm] >= 0");
            table.HasCheckConstraint("CK_VehicleDailyDistances_Source", "[AppliedSource] BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_VehicleDailyDistances_ManualOdometer", "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= [ManualBaselineOdometerReading])");
        });
    }
}

internal sealed class VehicleDailyDistanceImportConfiguration : IEntityTypeConfiguration<VehicleDailyDistanceImport>
{
    public void Configure(EntityTypeBuilder<VehicleDailyDistanceImport> builder)
    {
        builder.ConfigureHistory("VehicleDailyDistanceImports");
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.RowErrorsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.WorkDate, x.Sha256Checksum }).IsUnique();
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_VehicleDailyDistanceImports_Counts",
            "[TotalVehicleRows] >= 0 AND [GpsRows] >= 0 AND [NoGpsRows] >= 0 AND [MatchedRows] >= 0 AND [CreatedRows] >= 0 AND [UpdatedRows] >= 0 AND [UnmatchedRows] >= 0 AND [InvalidRows] >= 0"));
    }
}

internal sealed class RiderVehicleAssignmentConfiguration : IEntityTypeConfiguration<RiderVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<RiderVehicleAssignment> builder)
    {
        builder.ConfigureOperational("RiderVehicleAssignments");
        builder.Property(x => x.IsRealRider).HasDefaultValue(true);
        builder.Property(x => x.PermissionReference).HasMaxLength(200);
        builder.Property(x => x.AssignmentReason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.StartLocationSnapshot).HasMaxLength(400);
        builder.Property(x => x.EndLocationSnapshot).HasMaxLength(400);
        builder.Property(x => x.CompletionReason).HasMaxLength(1000);
        builder.Property(x => x.BackdatedReason).HasMaxLength(1000);
        builder.Property(x => x.CorrectionReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
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

internal sealed class RealRiderConfiguration : IEntityTypeConfiguration<RealRider>
{
    public void Configure(EntityTypeBuilder<RealRider> builder)
    {
        builder.ConfigureHistory("RealRiders");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IqamaNo).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(x => x.RelationshipToAssignedRider).HasMaxLength(200).IsRequired();
        builder.HasOne<RiderVehicleAssignment>().WithOne()
            .HasForeignKey<RealRider>(x => x.RiderVehicleAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.RiderVehicleAssignmentId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RealRiders_IqamaNo",
            "LEN([IqamaNo]) = 10 AND [IqamaNo] NOT LIKE '%[^0-9]%'"));
    }
}

internal sealed class SponsorVehicleLeaseAgreementConfiguration : IEntityTypeConfiguration<SponsorVehicleLeaseAgreement>
{
    public void Configure(EntityTypeBuilder<SponsorVehicleLeaseAgreement> builder)
    {
        builder.ConfigureTemporal("SponsorVehicleLeaseAgreements");
        builder.Property(x => x.AgreementReference).HasMaxLength(200);
        builder.Property(x => x.EndReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<ClientPlatform>().WithMany()
            .HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany()
            .HasForeignKey(x => x.LessorSponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany()
            .HasForeignKey(x => x.LesseeSponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClientPlatformId, x.LessorSponsorId, x.LesseeSponsorId, x.EffectiveFrom });
        builder.HasIndex(x => new { x.ClientPlatformId, x.EffectiveFrom, x.EffectiveTo });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_SponsorVehicleLeaseAgreements_DifferentSponsors",
            "[LessorSponsorId] <> [LesseeSponsorId]"));
    }
}

internal sealed class SponsorVehicleLeaseAgreementVehicleConfiguration : IEntityTypeConfiguration<SponsorVehicleLeaseAgreementVehicle>
{
    public void Configure(EntityTypeBuilder<SponsorVehicleLeaseAgreementVehicle> builder)
    {
        builder.ConfigureHistory("SponsorVehicleLeaseAgreementVehicles");
        builder.HasOne<SponsorVehicleLeaseAgreement>().WithMany()
            .HasForeignKey(x => x.SponsorVehicleLeaseAgreementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany()
            .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SponsorVehicleLeaseAgreementId, x.VehicleId }).IsUnique();
        builder.HasIndex(x => new { x.VehicleId, x.SponsorVehicleLeaseAgreementId });
    }
}

internal sealed class VehiclePlatformAccountAssignmentConfiguration : IEntityTypeConfiguration<VehiclePlatformAccountAssignment>
{
    public void Configure(EntityTypeBuilder<VehiclePlatformAccountAssignment> builder)
    {
        builder.ConfigureOperational("VehiclePlatformAccountAssignments");
        builder.Property(x => x.AssignmentReason).HasMaxLength(1000);
        builder.Property(x => x.EndReason).HasMaxLength(1000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.EndedAtUtc });
        builder.HasIndex(x => new { x.PlatformRiderAccountId, x.EndedAtUtc });
        builder.HasIndex(x => new { x.VehicleId, x.ApprovedAtUtc });
        builder.HasIndex(x => new { x.Status, x.ApprovedAtUtc });
        builder.HasIndex(x => new { x.VehicleId, x.PlatformRiderAccountId })
            .HasFilter("[EndedAtUtc] IS NULL AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountAssignments_AlwaysApproved",
                "[ApprovalStatus] = 1");
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountAssignments_Status",
                "([Status] = 1 AND [EndedAtUtc] IS NULL) OR ([Status] = 2 AND [EndedAtUtc] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountAssignments_TimeRange",
                "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [AssignedAtUtc]");
        });
    }
}

internal sealed class VehiclePlatformAccountSwitchConfiguration : IEntityTypeConfiguration<VehiclePlatformAccountSwitch>
{
    public void Configure(EntityTypeBuilder<VehiclePlatformAccountSwitch> builder)
    {
        builder.ConfigureOperational("VehiclePlatformAccountSwitches");
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<VehiclePlatformAccountAssignment>().WithMany()
            .HasForeignKey(x => x.SourceAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany()
            .HasForeignKey(x => x.SourceVehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehicle>().WithMany()
            .HasForeignKey(x => x.TargetVehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformRiderAccount>().WithMany()
            .HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehiclePlatformAccountAssignment>().WithMany()
            .HasForeignKey(x => x.NewAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.RequestedAtUtc });
        builder.HasIndex(x => new { x.PlatformRiderAccountId, x.Status });
        builder.HasIndex(x => new { x.TargetVehicleId, x.Status });
        builder.HasIndex(x => x.SourceAssignmentId).IsUnique()
            .HasFilter("[Status] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountSwitches_DifferentVehicles",
                "[SourceVehicleId] <> [TargetVehicleId]");
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountSwitches_ModeStatus",
                "([Mode] = 1 AND [Status] = 2) OR ([Mode] = 2 AND [Status] IN (1, 2))");
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountSwitches_Acceptance",
                "([Status] = 1 AND [EffectiveAtUtc] IS NULL AND [AcceptedAtUtc] IS NULL AND [AcceptedByUserId] IS NULL AND [NewAssignmentId] IS NULL) OR " +
                "([Status] = 2 AND [EffectiveAtUtc] IS NOT NULL AND [AcceptedAtUtc] IS NOT NULL AND [AcceptedByUserId] IS NOT NULL AND [NewAssignmentId] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_VehiclePlatformAccountSwitches_AcceptedAfterRequested",
                "[AcceptedAtUtc] IS NULL OR [AcceptedAtUtc] >= [RequestedAtUtc]");
        });
    }
}

internal sealed class RiderVehicleAssignmentPromissoryFileConfiguration : IEntityTypeConfiguration<RiderVehicleAssignmentPromissoryFile>
{
    public void Configure(EntityTypeBuilder<RiderVehicleAssignmentPromissoryFile> builder)
    {
        builder.ConfigureHistory("RiderVehicleAssignmentPromissoryFiles");
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RiderVehicleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderPromissoryFileVersion>().WithMany().HasForeignKey(x => x.RiderPromissoryFileVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RiderVehicleAssignmentId, x.RiderPromissoryFileVersionId }).IsUnique();
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

internal sealed class VehicleOperationCardConfiguration : IEntityTypeConfiguration<VehicleOperationCard>
{
    public void Configure(EntityTypeBuilder<VehicleOperationCard> builder)
    {
        builder.ConfigureOperational("VehicleOperationCards");
        builder.Property(x => x.CardNumber).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IssuingAuthority).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleOperationCard>().WithMany().HasForeignKey(x => x.PreviousRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleId, x.CardNumber }).IsUnique();
        builder.HasIndex(x => x.VehicleId).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.ExpiryDate, x.IsCurrent });
        builder.ToTable(t => t.HasCheckConstraint("CK_VehicleOperationCards_DateRange", "[ExpiryDate] >= [IssueDate]"));
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
        builder.HasIndex(x => new { x.VehicleId, x.Kind }).IsUnique().HasFilter("[Kind] <> 99 AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.VehicleId, x.IsDeleted });
    }
}

internal sealed class RiderPromissoryFileConfiguration : IEntityTypeConfiguration<RiderPromissoryFile>
{
    public void Configure(EntityTypeBuilder<RiderPromissoryFile> builder)
    {
        builder.ConfigureOperational("RiderPromissoryFiles");
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderPromissoryFileVersion>().WithMany().HasForeignKey(x => x.CurrentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RiderProfileId, x.IsDeleted });
    }
}

internal sealed class RiderPromissoryFileVersionConfiguration : IEntityTypeConfiguration<RiderPromissoryFileVersion>
{
    public void Configure(EntityTypeBuilder<RiderPromissoryFileVersion> builder)
    {
        builder.ConfigureHistory("RiderPromissoryFileVersions");
        VehicleAttachmentVersionConfiguration.ConfigureFile(builder);
        builder.HasOne<RiderPromissoryFile>().WithMany().HasForeignKey(x => x.RiderPromissoryFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderPromissoryFileVersion>().WithMany().HasForeignKey(x => x.SupersededVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RiderPromissoryFileId, x.VersionNumber }).IsUnique();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_RiderPromissoryFileVersions_Version", "[VersionNumber] > 0");
            t.HasCheckConstraint("CK_RiderPromissoryFileVersions_Size", "[FileSizeBytes] > 0");
        });
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
        builder.Property(x => x.LocationDescription).HasMaxLength(400);
        builder.Property(x => x.EstimatedRepairCost).HasPrecision(18, 2);
        builder.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderVehicleAssignment>().WithMany().HasForeignKey(x => x.RelatedAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IssueNumber).IsUnique();
        builder.HasIndex(x => new { x.VehicleId, x.Status, x.BlocksOperation });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_VehicleIssues_EstimatedRepairCost",
            "[EstimatedRepairCost] IS NULL OR [EstimatedRepairCost] >= 0"));
    }
}

internal sealed class VehicleIssueEvidenceConfiguration : IEntityTypeConfiguration<VehicleIssueEvidence>
{
    public void Configure(EntityTypeBuilder<VehicleIssueEvidence> builder)
    {
        builder.ConfigureOperational("VehicleIssueEvidenceFiles");
        VehicleAttachmentVersionConfiguration.ConfigureFile(builder);
        builder.HasOne<VehicleIssue>().WithMany().HasForeignKey(x => x.VehicleIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VehicleIssueId, x.UploadedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_VehicleIssueEvidenceFiles_Size",
            "[FileSizeBytes] > 0"));
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
