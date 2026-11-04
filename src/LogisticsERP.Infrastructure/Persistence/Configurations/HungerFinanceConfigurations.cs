using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HungerFinanceConfigurations : IEntityTypeConfiguration<HungerFinanceConfiguration>,
    IEntityTypeConfiguration<HungerFinancialEvent>, IEntityTypeConfiguration<HungerRemittanceLine>, IEntityTypeConfiguration<HungerWalletReview>
{
    public void Configure(EntityTypeBuilder<HungerFinanceConfiguration> b)
    {
        b.ConfigureAuditable("FinanceConfigurations", "hunger");
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.SupervisorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.CustodySubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.CollectionsSubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.WalletSubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.RiderClearingSubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.EvidenceReference).HasMaxLength(1000).IsRequired();
        b.HasIndex(x => new { x.ClientPlatformId, x.SupervisorEmployeeId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
    public void Configure(EntityTypeBuilder<HungerFinancialEvent> b)
    {
        b.ConfigureHistory("FinancialEvents", "hunger");
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.SupervisorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.RelatedEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.FromSubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreasurySubAccount>().WithMany().HasForeignKey(x => x.ToSubAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.RiderBalanceChange).HasPrecision(18, 2);
        b.Property(x => x.Kind).HasMaxLength(40); b.Property(x => x.IdempotencyKey).HasMaxLength(150);
        b.Property(x => x.RequestHash).HasMaxLength(64); b.Property(x => x.EvidenceReference).HasMaxLength(1000);
        b.HasIndex(x => new { x.ClientPlatformId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate });
        b.HasIndex(x => new { x.SupervisorEmployeeId, x.BusinessDate });
    }
    public void Configure(EntityTypeBuilder<HungerRemittanceLine> b)
    {
        b.ConfigureAuditable("RemittanceLines", "hunger");
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.RemittanceEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.SourceEventId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.ConfirmedAmount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.RemittanceEventId, x.SourceEventId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_HungerRemittance_Amounts", "[Amount]>0 AND [ConfirmedAmount]>=0 AND [ConfirmedAmount]<=[Amount]"));
    }
    public void Configure(EntityTypeBuilder<HungerWalletReview> b)
    {
        b.ConfigureAuditable("WalletReviews", "hunger");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.InputRevision).HasMaxLength(64);
        b.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
    }
}
