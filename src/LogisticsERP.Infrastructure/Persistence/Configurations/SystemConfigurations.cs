using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ConfigureOperational("Notifications");
        builder.Property(entity => entity.EventType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.TitleAr).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.TitleEn).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.BodyAr).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.BodyEn).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.SourceEntityType).HasMaxLength(100);
        builder.Property(entity => entity.DeepLink).HasMaxLength(1000);
        builder.Property(entity => entity.ScopeSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.DeduplicationKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(entity => new { entity.RecipientUserId, entity.DeduplicationKey }).IsUnique();
        builder.HasIndex(entity => new { entity.RecipientUserId, entity.ReadAtUtc, entity.VisibleAtUtc });
        builder.HasIndex(entity => entity.ExpiresAtUtc);
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ConfigureHistory("AuditEntries", "audit");
        builder.Property(entity => entity.Sequence)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("NEXT VALUE FOR [audit].[AuditEntrySequence]");
        builder.Property(entity => entity.ActorType).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.TraceId).HasMaxLength(100);
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.UserAgent).HasMaxLength(1000);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.AfterJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.Source).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PreviousHash).HasMaxLength(64).IsFixedLength();
        builder.Property(entity => entity.CurrentHash).HasMaxLength(64).IsFixedLength();
        builder.HasIndex(entity => entity.EventId).IsUnique();
        builder.HasIndex(entity => entity.Sequence).IsUnique();
        builder.HasIndex(entity => entity.OccurredAtUtc);
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.OccurredAtUtc });
        builder.HasIndex(entity => new { entity.ActorUserId, entity.OccurredAtUtc });
    }
}

internal sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.ConfigureOperational("ExportJobs");
        builder.Property(entity => entity.ReportType).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.ScopeSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.FilterSnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.SensitiveExportReason).HasMaxLength(1000);
        builder.Property(entity => entity.ArtifactPath).HasMaxLength(1000);
        builder.Property(entity => entity.ArtifactChecksum).HasMaxLength(64).IsFixedLength();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(100);
        builder.Property(entity => entity.ErrorDetails).HasMaxLength(4000);
        builder.HasIndex(entity => new { entity.RequestedByUserId, entity.RequestedAtUtc });
        builder.HasIndex(entity => new { entity.Status, entity.RequestedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ExportJobs_ProgressPercentage",
            "[ProgressPercentage] >= 0 AND [ProgressPercentage] <= 100"));
    }
}

internal sealed class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> builder)
    {
        builder.ConfigureOperational("SavedViews");
        builder.Property(entity => entity.ModuleKey).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.FiltersJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.SortingJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ColumnsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ColumnOrderJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Density).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.UserId, entity.ModuleKey, entity.Name }).IsUnique();
    }
}

internal sealed class DatasetVersionConfiguration : IEntityTypeConfiguration<DatasetVersion>
{
    public void Configure(EntityTypeBuilder<DatasetVersion> builder)
    {
        builder.ConfigureOperational("DatasetVersions");
        builder.Property(entity => entity.ModuleKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.ModuleKey).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("CK_DatasetVersions_Version", "[Version] >= 0"));
    }
}
