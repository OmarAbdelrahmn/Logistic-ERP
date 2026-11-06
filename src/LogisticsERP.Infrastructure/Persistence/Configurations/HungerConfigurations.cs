using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HungerConfigurations : IEntityTypeConfiguration<HungerQualitySnapshot>,
    IEntityTypeConfiguration<HungerImportPreview>, IEntityTypeConfiguration<HungerQualityRevision>, IEntityTypeConfiguration<HungerDailyOperation>, IEntityTypeConfiguration<HungerAlertRule>, IEntityTypeConfiguration<HungerPotentialFine>
{
    public void Configure(EntityTypeBuilder<HungerPotentialFine> b)
    {
        b.ConfigureAuditable("PotentialFines", "hunger");
        b.HasOne<HungerDailyOperation>().WithMany().HasForeignKey(x => x.DailyOperationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerSettlementDocument>().WithMany().HasForeignKey(x => x.InvoiceDocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.DailyOperationId).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Property(x => x.Status).HasMaxLength(32);
        b.Property(x => x.InvoiceSourceKey).HasMaxLength(200);
        b.Property(x => x.ResolutionEvidence).HasMaxLength(1000);
        b.ToTable(t => t.HasCheckConstraint("CK_HungerPotentialFine_Status", "[NoShowCount]>0 AND [Status] IN ('Potential','Invoiced','Waived')"));
    }
    public void Configure(EntityTypeBuilder<HungerAlertRule> b)
    {
        b.ConfigureHistory("AlertRules", "hunger");
        b.HasOne<LogisticsERP.Domain.Entities.Platform.ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Code).HasMaxLength(50); b.Property(x => x.Severity).HasMaxLength(32);
        b.Property(x => x.Metric).HasMaxLength(50); b.Property(x => x.Operator).HasMaxLength(32);
        b.Property(x => x.Kind).HasMaxLength(16); b.Property(x => x.Period).HasMaxLength(16);
        b.HasOne<LogisticsERP.Domain.Entities.Platform.OperatingCity>().WithMany().HasForeignKey(x => x.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Threshold).HasPrecision(18, 8); b.Property(x => x.EvidenceReference).HasMaxLength(1000);
        b.HasIndex(x => new { x.ClientPlatformId, x.Code, x.EffectiveFrom, x.CreatedAtUtc });
    }
    public void Configure(EntityTypeBuilder<HungerDailyOperation> b)
    {
        b.ConfigureAuditable("DailyOperations", "hunger");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerImportPreview>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Property(x => x.ActualWorkingHours).HasPrecision(18, 8); b.Property(x => x.ManualHours).HasPrecision(18, 8);
        b.Property(x => x.HoursUnit).HasMaxLength(32);
        b.Property(x => x.CaptainClassification).HasMaxLength(100);
        b.Property(x => x.ManualReason).HasMaxLength(2000); b.Property(x => x.AllocationStatus).HasMaxLength(32);
        b.ToTable(t => t.HasCheckConstraint("CK_HungerDaily_Values", "[CompletedOrders]>=0 AND [ActualWorkingHours] BETWEEN 0 AND 24 AND ([RejectedOrders] IS NULL OR [RejectedOrders]>=0) AND ([CancelledOrders] IS NULL OR [CancelledOrders]>=0) AND ([NoShowCount] IS NULL OR [NoShowCount]>=0) AND ([ManualOrders] IS NULL OR [ManualOrders]>=0) AND ([ManualHours] IS NULL OR [ManualHours] BETWEEN 0 AND 24)"));
    }
    public void Configure(EntityTypeBuilder<HungerQualitySnapshot> b)
    {
        b.ConfigureAuditable("QualitySnapshots", "hunger");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerImportPreview>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.SoleRiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Property(x => x.Segment).HasMaxLength(50).IsRequired();
        b.Property(x => x.AllocationStatus).HasMaxLength(32).IsRequired();
        b.Property(x => x.VerificationSuccessRate).HasPrecision(9, 6);
        b.Property(x => x.OnTimeDeliveryScore).HasPrecision(9, 6);
        b.Property(x => x.FinalDeliveryQualityScore).HasPrecision(9, 6);
        b.ToTable(t => t.HasCheckConstraint("CK_HungerQuality_Rates", "[VerificationSuccessRate] BETWEEN 0 AND 1 AND [OnTimeDeliveryScore] BETWEEN 0 AND 1 AND [FinalDeliveryQualityScore] BETWEEN 0 AND 1"));
    }
    public void Configure(EntityTypeBuilder<HungerImportPreview> b)
    {
        b.ConfigureAuditable("ImportPreviews", "hunger");
        b.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.InputRevision).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
    }
    public void Configure(EntityTypeBuilder<HungerQualityRevision> b)
    {
        b.ConfigureHistory("QualityRevisions", "hunger");
        b.HasOne<HungerQualitySnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerImportPreview>().WithMany().HasForeignKey(x => x.TriggerImportId).OnDelete(DeleteBehavior.Restrict);
    }
}
