using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Ninja;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class PlatformDailySourceConfigurations : IEntityTypeConfiguration<NinjaDailyPerformanceSource>,
    IEntityTypeConfiguration<NinjaDailyDeductionSource>, IEntityTypeConfiguration<ChefzDailyPerformanceSource>,
    IEntityTypeConfiguration<ChefzImportedOrderRow>
{
    public void Configure(EntityTypeBuilder<NinjaDailyPerformanceSource> b)
    {
        NinjaSource(b, "NinjaDailyPerformanceSources");
        b.Property(x => x.TotalWorkingHours).HasPrecision(18, 8);
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaPerformanceSource_Values", "[TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24"));
    }
    public void Configure(EntityTypeBuilder<NinjaDailyDeductionSource> b)
    {
        NinjaSource(b, "NinjaDailyDeductionSources");
        b.Property(x => x.TotalCollectedAmountAtDelivery).HasPrecision(18, 2);
        b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaDeductionSource_Values", "[TotalCollectedAmountAtDelivery] >= 0"));
    }
    private static void NinjaSource<T>(EntityTypeBuilder<T> b, string table) where T : NinjaDailySource
    {
        b.HasBaseType((Type?)null);
        b.ConfigureHistory(table, "ninja");
        b.Property(x => x.CaptainId).HasMaxLength(150).IsRequired();
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NinjaDailyImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<ChefzDailyPerformanceSource> b)
    {
        b.ConfigureAuditable("DailyPerformanceSources", "chefz");
        b.Property(x => x.ReportIdNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.AcceptanceRate).HasPrecision(5, 2);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ChefzPerformanceImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzPerformanceSource_Values", "[TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100"));
    }
    public void Configure(EntityTypeBuilder<ChefzImportedOrderRow> b)
    {
        b.ConfigureHistory("ImportedOrderRows", "chefz");
        b.HasOne<ChefzOrderSettlementImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.OrderId).HasMaxLength(100).IsRequired();
        foreach (var property in new[] { "DeliveryFees", "Tips", "Compensation", "Bonuses", "Deductions", "CashCollected" })
            b.Property<decimal>(property).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ImportId, x.SourceRowNumber }).IsUnique();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate });
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzImportedRow_Values", "[UsageSequence] > 0 AND [SourceRowNumber] > 1 AND [Deductions] >= 0 AND [CashCollected] >= 0"));
    }
}
