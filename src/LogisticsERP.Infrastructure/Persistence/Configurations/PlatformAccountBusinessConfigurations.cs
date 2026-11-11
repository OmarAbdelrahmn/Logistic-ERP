using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class PlatformAccountBusinessConfigurations : IEntityTypeConfiguration<PlatformAccountBusinessDetails>, IEntityTypeConfiguration<PlatformAccountBusinessRevision>
{
    public void Configure(EntityTypeBuilder<PlatformAccountBusinessDetails> b)
    {
        b.ConfigureAuditable("PlatformAccountBusinessDetails", "app");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.SourceActualRiderProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.PlatformRiderAccountId).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Property(x => x.PhoneNumber).HasMaxLength(32); b.Property(x => x.RegistryIdentifier).HasMaxLength(150);
        b.Property(x => x.SettlementModel).HasMaxLength(150); b.Property(x => x.RegisteredVehicleType).HasMaxLength(32);
        b.Property(x => x.RegisteredVehiclePlate).HasMaxLength(32); b.Property(x => x.RegisteredVehicleSerial).HasMaxLength(100);
        b.Property(x => x.OperationalDisposition).HasMaxLength(32); b.Property(x => x.DispositionReason).HasMaxLength(2000);
        b.Property(x => x.SimOwnership).HasMaxLength(32); b.Property(x => x.SourceStatus).HasMaxLength(150);
        b.Property(x => x.SourceActualIqama).HasMaxLength(50); b.Property(x => x.SourceCompensationModel).HasMaxLength(150);
        b.Property(x => x.SourceVehicleDescription).HasMaxLength(150); b.Property(x => x.SourceReference).HasMaxLength(500);
        b.Property(x => x.SourceOpeningWallet).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_PlatformBusiness_Disposition", "[OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth','WalletPending')"));
    }
    public void Configure(EntityTypeBuilder<PlatformAccountBusinessRevision> b)
    {
        b.ConfigureHistory("PlatformAccountBusinessRevisions", "app");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.EffectiveFrom });
    }
}
