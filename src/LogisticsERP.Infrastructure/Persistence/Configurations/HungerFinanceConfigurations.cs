using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HungerFinanceConfigurations : IEntityTypeConfiguration<HungerFinanceConfiguration>,
    IEntityTypeConfiguration<HungerReceiptAllocation>, IEntityTypeConfiguration<HungerFinancialEvent>, IEntityTypeConfiguration<HungerRemittanceLine>, IEntityTypeConfiguration<HungerWalletReview>,
    IEntityTypeConfiguration<HungerEvidenceFile>, IEntityTypeConfiguration<HungerExceptionApproval>
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
        b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Posted");
        b.Property(x => x.SourceWalletBalance).HasPrecision(18, 2);
        b.Property(x => x.DestinationType).HasMaxLength(32);
        b.Property(x => x.TransferReference).HasMaxLength(200);
        b.Property(x => x.CompletionKey).HasMaxLength(150);
        b.Property(x => x.CompletionHash).HasMaxLength(64);
        b.Property(x => x.ArrivalEvidence).HasMaxLength(1000);
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.RiderBalanceChange).HasPrecision(18, 2);
        b.Property(x => x.Kind).HasMaxLength(40); b.Property(x => x.IdempotencyKey).HasMaxLength(150);
        b.Property(x => x.RequestHash).HasMaxLength(64); b.Property(x => x.EvidenceReference).HasMaxLength(1000);
        b.HasIndex(x => new { x.ClientPlatformId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate });
        b.HasIndex(x => new { x.SupervisorEmployeeId, x.BusinessDate });
        b.Property(x => x.ApprovalStatus).HasMaxLength(16).HasDefaultValue("NotRequired");
        b.Property(x => x.RejectionReason).HasMaxLength(1000);
        b.Property(x => x.Role).HasMaxLength(16); b.Property(x => x.DestinationChannel).HasMaxLength(16);
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.SupersedesEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerExceptionApproval>().WithMany().HasForeignKey(x => x.ExceptionApprovalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClientPlatformId, x.ApprovalStatus });
        b.ToTable(t => t.HasCheckConstraint("CK_HungerFinancialEvents_ApprovalStatus",
            "[ApprovalStatus] IN ('NotRequired','PendingSeal','Sealed','Rejected','Cancelled')"));
    }
    public void Configure(EntityTypeBuilder<HungerEvidenceFile> b)
    {
        b.ConfigureAuditable("EvidenceFiles", "hunger");
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.FinancialEventId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.StoragePath).HasMaxLength(500).IsRequired(); b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.Purpose).HasMaxLength(16);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.ObservedOn });
        b.ToTable(t => t.HasCheckConstraint("CK_HungerEvidenceFiles_Purpose", "[Purpose] IN ('WalletBefore','WalletAfter','Wallet','Arrival')"));
    }
    public void Configure(EntityTypeBuilder<HungerExceptionApproval> b)
    {
        b.ConfigureAuditable("ExceptionApprovals", "hunger");
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.SupervisorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Kind).HasMaxLength(32); b.Property(x => x.Status).HasMaxLength(16).HasDefaultValue("Requested");
        b.Property(x => x.Amount).HasPrecision(18, 2); b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.EvidenceReference).HasMaxLength(1000); b.Property(x => x.DecisionNote).HasMaxLength(1000);
        b.Property(x => x.IdempotencyKey).HasMaxLength(150); b.Property(x => x.RequestHash).HasMaxLength(64);
        b.HasIndex(x => new { x.ClientPlatformId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.ClientPlatformId, x.Status });
        b.ToTable(t => t.HasCheckConstraint("CK_HungerExceptionApprovals_Values",
            "[Kind] IN ('NonZeroHandover','CustodyIdentityMismatch') AND [Status] IN ('Requested','Approved','Rejected','Consumed') AND [Amount]<>0"));
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
    public void Configure(EntityTypeBuilder<HungerReceiptAllocation> b)
    {
        b.ConfigureHistory("ReceiptAllocations", "hunger");
        b.HasOne<HungerFinancialEvent>().WithMany().HasForeignKey(x => x.ConfirmationEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerRemittanceLine>().WithMany().HasForeignKey(x => x.RemittanceLineId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ConfirmationEventId, x.RemittanceLineId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_HungerReceiptAllocation_Amount", "[Amount]>0"));
    }
    public void Configure(EntityTypeBuilder<HungerWalletReview> b)
    {
        b.ConfigureAuditable("WalletReviews", "hunger");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.InputRevision).HasMaxLength(64);
        b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.Status });
        b.HasIndex(x => x.SwapId);
        b.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
    }
}
