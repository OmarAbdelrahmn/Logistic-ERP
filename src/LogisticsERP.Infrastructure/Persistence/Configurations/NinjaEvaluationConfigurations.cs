using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class NinjaEvaluationConfigurations : IEntityTypeConfiguration<NinjaSuggestionPolicy>,
    IEntityTypeConfiguration<NinjaEvaluationContextRevision>, IEntityTypeConfiguration<NinjaResponsibilityRevision>,
    IEntityTypeConfiguration<NinjaResponsibilityLine>, IEntityTypeConfiguration<NinjaPerformancePreview>,
    IEntityTypeConfiguration<NinjaPerformanceArtifact>, IEntityTypeConfiguration<NinjaPerformanceImportArtifact>,
    IEntityTypeConfiguration<NinjaAllocationProvenance>, IEntityTypeConfiguration<NinjaEvaluationSnapshot>
{
    public void Configure(EntityTypeBuilder<NinjaSuggestionPolicy> b)
    {
        b.ConfigureHistory("SuggestionPolicies", "ninja");
        b.Property(x => x.Version).HasMaxLength(64);
        b.HasIndex(x => x.Version).IsUnique();
        b.HasData(new NinjaSuggestionPolicy { Id = Guid.Parse("019c18d5-62e1-7000-8000-000000000390"),
            CreatedAtUtc = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero), Version = NinjaSuggestionCalculator.PolicyVersion,
            DefinitionJson = "{\"monthBasis\":\"CalendarDays\",\"attendance\":0.85,\"hours\":8,\"reduction\":0.15,\"rounding\":\"AwayFromZero\",\"bike\":[450,5700,11,16,17,18,9],\"car\":[460,6500,12,22,22.5,23,10],\"invoice\":\"External\"}" });
    }
    public void Configure(EntityTypeBuilder<NinjaEvaluationContextRevision> b)
    {
        b.ConfigureHistory("EvaluationContexts", "ninja");
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaEvaluationContextRevision>().WithMany().HasForeignKey(x => x.SupersedesId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaEvaluationSnapshot>().WithMany().HasForeignKey(x => x.ReviewedSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.Revision }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaResponsibilityRevision> b)
    {
        b.ConfigureHistory("ResponsibilityRevisions", "ninja");
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaResponsibilityRevision>().WithMany().HasForeignKey(x => x.SupersedesId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaEvaluationSnapshot>().WithMany().HasForeignKey(x => x.ReviewedSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.Month, x.Revision }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaResponsibilityLine> b)
    {
        b.ConfigureHistory("ResponsibilityLines", "ninja");
        b.Property(x => x.Weight).HasPrecision(18, 8);
        b.HasOne<NinjaResponsibilityRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RevisionId, x.BusinessDate, x.RiderClientAssignmentId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaResponsibility_Weight", "[Weight] BETWEEN 0 AND 1"));
    }
    public void Configure(EntityTypeBuilder<NinjaPerformancePreview> b)
    {
        b.ConfigureHistory("PerformancePreviews", "ninja");
        b.Property(x => x.ContentHash).HasMaxLength(64); b.Property(x => x.InputRevision).HasMaxLength(64);
        b.HasIndex(x => x.ExpiresAtUtc);
    }
    public void Configure(EntityTypeBuilder<NinjaPerformanceArtifact> b)
    {
        b.ConfigureHistory("PerformanceArtifacts", "ninja");
        b.Property(x => x.FileName).HasMaxLength(255); b.Property(x => x.ContentHash).HasMaxLength(64);
        b.Property(x => x.Format).HasMaxLength(16);
        b.HasOne<NinjaPerformancePreview>().WithMany().HasForeignKey(x => x.PreviewId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaPerformanceArtifact>().WithMany().HasForeignKey(x => x.OriginalArtifactId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PreviewId).IsUnique().HasFilter("[PreviewId] IS NOT NULL");
        b.HasIndex(x => x.ContentHash);
    }
    public void Configure(EntityTypeBuilder<NinjaPerformanceImportArtifact> b)
    {
        b.ConfigureHistory("PerformanceImportArtifacts", "ninja");
        b.HasOne<NinjaPerformanceArtifact>().WithMany().HasForeignKey(x => x.ArtifactId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaDailyImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ArtifactId, x.ImportId }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaAllocationProvenance> b)
    {
        b.ConfigureHistory("AllocationProvenance", "ninja");
        b.Property(x => x.Origin).HasMaxLength(32); b.Property(x => x.EvidenceReference).HasMaxLength(200);
        b.HasOne<NinjaDailyPerformanceSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SourceId, x.RiderClientAssignmentId }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaEvaluationSnapshot> b)
    {
        b.ConfigureHistory("EvaluationSnapshots", "ninja");
        b.Property(x => x.PolicyVersion).HasMaxLength(64); b.Property(x => x.InputRevision).HasMaxLength(64);
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<NinjaEvaluationSnapshot>().WithMany().HasForeignKey(x => x.SupersedesId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
