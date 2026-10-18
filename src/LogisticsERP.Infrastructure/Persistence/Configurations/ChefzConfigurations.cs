using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class ChefzConfigurations : IEntityTypeConfiguration<ChefzAccountDetails>,
    IEntityTypeConfiguration<ChefzWalletSettlement>, IEntityTypeConfiguration<ChefzDailyPerformance>,
    IEntityTypeConfiguration<ChefzPerformanceImport>, IEntityTypeConfiguration<ChefzPerformanceSettings>,
    IEntityTypeConfiguration<ChefzSponsorStatistics>, IEntityTypeConfiguration<ChefzOrderSettlementImport>
{
    public void Configure(EntityTypeBuilder<ChefzAccountDetails> b)
    {
        b.ConfigureAuditable("AccountDetails", "chefz");
        b.Property(x => x.ReportIdNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.VehicleType).HasMaxLength(20).IsRequired();
        b.Property(x => x.DashboardStatus).HasMaxLength(100).IsRequired();
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PlatformRiderAccountId).IsUnique();
        b.HasIndex(x => x.ReportIdNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzDetails_Type", "[AccountType] IN (1, 2)"));
    }

    public void Configure(EntityTypeBuilder<ChefzPerformanceSettings> b)
    {
        b.ConfigureAuditable("PerformanceSettings", "chefz");
        foreach (var p in new[] { nameof(ChefzPerformanceSettings.AAcceptanceRate), nameof(ChefzPerformanceSettings.BAcceptanceRate), nameof(ChefzPerformanceSettings.CAcceptanceRate) })
            b.Property<decimal>(p).HasPrecision(5, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzSettings_Thresholds",
            "[OverallAOrders] > [OverallBOrders] AND [OverallBOrders] > [OverallCOrders] AND [OverallCOrders] > 0 AND [DailyAOrders] > [DailyBOrders] AND [DailyBOrders] > [DailyCOrders] AND [DailyCOrders] > 0 AND [AAcceptanceRate] BETWEEN 0 AND 100 AND [BAcceptanceRate] BETWEEN 0 AND [AAcceptanceRate] AND [CAcceptanceRate] BETWEEN 0 AND [BAcceptanceRate] AND [ChangeAfterNoOrderDays] > 0"));
        b.HasData(new ChefzPerformanceSettings { Id = ChefzPerformanceSettings.SingletonId,
            CreatedAtUtc = new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero) });
    }

    public void Configure(EntityTypeBuilder<ChefzSponsorStatistics> b)
    {
        b.ConfigureAuditable("SponsorStatistics", "chefz");
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SponsorId).IsUnique().HasFilter("[IsDeleted] = 0");
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzStatistics_Nonnegative",
            "[SponsorshipCount] >= 0 AND [Working] >= 0 AND [NotWorking] >= 0 AND [Blocked] >= 0 AND [PermanentlyBlocked] >= 0 AND [UpdateBlocked] >= 0 AND [Uploaded] >= 0 AND [PrivateLicense] >= 0 AND [PendingUpload] >= 0 AND [NoLicense] >= 0 AND [ManagementSuspended] >= 0 AND [Motorcycle] >= 0 AND [WorkingOutsideSponsorship] >= 0 AND [WorkingWithoutPerformance] >= 0"));
    }

    public void Configure(EntityTypeBuilder<ChefzWalletSettlement> b)
    {
        b.ConfigureHistory("WalletSettlements", "chefz");
        b.Property(x => x.Source).HasMaxLength(20).HasDefaultValue("Wallet").IsRequired();
        b.Property(x => x.DriverId).HasMaxLength(100).HasDefaultValue("").IsRequired();
        b.Property(x => x.RiderName).HasMaxLength(500).HasDefaultValue("").IsRequired();
        b.HasOne<ChefzOrderSettlementImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RiderClientAssignmentId, x.PeriodFrom }).IsUnique();
        b.HasIndex(x => new { x.RiderProfileId, x.PeriodFrom });
        foreach (var property in new[] { nameof(ChefzWalletSettlement.StartWalletAmount), nameof(ChefzWalletSettlement.EndWalletAmount),
            nameof(ChefzWalletSettlement.GrossEarnings), nameof(ChefzWalletSettlement.DailyFee),
            nameof(ChefzWalletSettlement.CompanyAmount), nameof(ChefzWalletSettlement.RiderAmount),
            nameof(ChefzWalletSettlement.DeliveryFees), nameof(ChefzWalletSettlement.VatAmount),
            nameof(ChefzWalletSettlement.Tips), nameof(ChefzWalletSettlement.Compensation),
            nameof(ChefzWalletSettlement.Bonuses), nameof(ChefzWalletSettlement.Deductions) })
            b.Property<decimal>(property).HasPrecision(18, 2);
        b.Property(x => x.CommissionRate).HasPrecision(5, 4);
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzSettlement_Amounts",
            "(([Source] = 'Wallet' AND [ImportId] IS NULL AND [StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount]) OR ([Source] = 'Orders' AND [ImportId] IS NOT NULL AND [SourceRows] > 0 AND [StartWalletAmount] = 0 AND [EndWalletAmount] = 0 AND [Deductions] >= 0 AND [GrossEarnings] = [DeliveryFees] - [VatAmount] + [Tips] + [Compensation] + [Bonuses] - [Deductions])) AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2)"));
    }

    public void Configure(EntityTypeBuilder<ChefzOrderSettlementImport> b)
    {
        b.ConfigureHistory("OrderSettlementImports", "chefz");
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.ContentHash).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzOrderImport_Rows", "[SourceRows] > 0"));
    }

    public void Configure(EntityTypeBuilder<ChefzDailyPerformance> b)
    {
        b.ConfigureHistory("DailyPerformance", "chefz");
        b.Property(x => x.ReportIdNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.AcceptanceRate).HasPrecision(5, 2);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ChefzPerformanceImport>().WithMany().HasForeignKey(x => x.ImportId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ReportDate }).IsUnique();
        b.HasIndex(x => new { x.RiderProfileId, x.ReportDate });
        b.ToTable(t => t.HasCheckConstraint("CK_ChefzPerformance_Values",
            "[TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100"));
    }

    public void Configure(EntityTypeBuilder<ChefzPerformanceImport> b)
    {
        b.ConfigureHistory("PerformanceImports", "chefz");
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.ReportDate, x.ContentHash }).IsUnique();
    }
}
