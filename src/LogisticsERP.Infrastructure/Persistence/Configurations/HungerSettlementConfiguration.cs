using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HungerSettlementConfiguration : IEntityTypeConfiguration<HungerSettlementDocument>
{
    public void Configure(EntityTypeBuilder<HungerSettlementDocument> b)
    {
        b.ConfigureHistory("SettlementDocuments", "hunger");
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerSettlementDocument>().WithMany().HasForeignKey(x => x.InvoiceDocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<HungerSettlementDocument>().WithMany().HasForeignKey(x => x.ClosingSnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.SourceDocumentType).HasMaxLength(64);
        b.Property(x => x.SourceReference).HasMaxLength(200);
        b.Property(x => x.ReconciliationEvidence).HasMaxLength(1000);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.InvoicePeriod, x.SourceDocumentType, x.SourceReference }).IsUnique()
            .HasFilter("[Kind]='Adjustment' AND [PlatformRiderAccountId] IS NOT NULL AND [SourceDocumentType] IS NOT NULL AND [SourceReference] IS NOT NULL");
        b.Property(x => x.Kind).HasMaxLength(32); b.Property(x => x.Status).HasMaxLength(32);
        b.Property(x => x.IdempotencyKey).HasMaxLength(150); b.Property(x => x.RequestHash).HasMaxLength(64);
        b.Property(x => x.EvidenceReference).HasMaxLength(1000);
        b.Property(x => x.WalletBalance).HasPrecision(18, 2); b.Property(x => x.CompanyControlTotal).HasPrecision(18, 2); b.Property(x => x.CanonicalGrossTotal).HasPrecision(18, 2);
        b.Property(x => x.RiderBalanceTotal).HasPrecision(18, 2);
        b.HasIndex(x => new { x.ClientPlatformId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.Kind, x.ChargeFrom, x.ChargeThrough }).IsUnique().HasFilter("[Kind]='MonthClose' AND [PlatformRiderAccountId] IS NOT NULL");
    }
}
