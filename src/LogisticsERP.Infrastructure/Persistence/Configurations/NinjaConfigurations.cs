using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class NinjaConfigurations : IEntityTypeConfiguration<NinjaAccountDetails>,
    IEntityTypeConfiguration<NinjaDailyImport>, IEntityTypeConfiguration<NinjaDailyPerformance>, IEntityTypeConfiguration<NinjaDailyDeduction>
{
    public void Configure(EntityTypeBuilder<NinjaAccountDetails> b)
    {
        b.ConfigureAuditable("NinjaAccountDetails", "ninja");
        b.Property(x => x.PhoneNumber).HasMaxLength(32);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PlatformRiderAccountId).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaDailyImport> b)
    {
        b.ConfigureHistory("NinjaDailyImports", "ninja");
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.ReportDate, x.IsDeductions, x.ContentHash }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<NinjaDailyPerformance> b)
    {
        ConfigureRecord(b, "NinjaDailyPerformance");
        b.HasOne<NinjaDailyPerformanceSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SourceId, x.RiderClientAssignmentId }).IsUnique();
        b.Property(x => x.TotalWorkingHours).HasPrecision(18, 8);
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaPerformance_Values", "[TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24"));
    }
    public void Configure(EntityTypeBuilder<NinjaDailyDeduction> b)
    {
        ConfigureRecord(b, "NinjaDailyDeductions");
        b.HasOne<NinjaDailyDeductionSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SourceId, x.RiderClientAssignmentId }).IsUnique();
        b.Property(x => x.TotalCollectedAmountAtDelivery).HasPrecision(18, 2);
        b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaDeductions_CollectedAmount", "[TotalCollectedAmountAtDelivery] >= 0"));
    }
    private static void ConfigureRecord<T>(EntityTypeBuilder<T> b, string table) where T : NinjaDailyRecord
    {
        // Each concrete report is a separate history table; the shared CLR base is not an EF entity.
        b.HasBaseType((Type?)null);
        b.ConfigureHistory(table, "ninja");
        b.Property(x => x.CaptainId).HasMaxLength(150).IsRequired();
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaDailyImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate, x.RiderClientAssignmentId }).IsUnique();
        b.HasIndex(x => new { x.RiderProfileId, x.ReportDate });
    }
}
