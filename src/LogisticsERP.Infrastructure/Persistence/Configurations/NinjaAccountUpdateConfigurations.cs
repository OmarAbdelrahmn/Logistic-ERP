using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class NinjaAccountUpdateConfigurations : IEntityTypeConfiguration<NinjaAccountUpdateUpload>, IEntityTypeConfiguration<NinjaAccountUpdateRow>
{
    public void Configure(EntityTypeBuilder<NinjaAccountUpdateUpload> b)
    {
        b.ConfigureAuditable("NinjaAccountUpdateUploads", "ninja");
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.SheetName).HasMaxLength(31).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Note).HasMaxLength(4000);
        b.HasOne<ClientPlatform>().WithMany().HasForeignKey(x => x.ClientPlatformId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClientPlatformId, x.CreatedAtUtc, x.Id });
    }

    public void Configure(EntityTypeBuilder<NinjaAccountUpdateRow> b)
    {
        b.ConfigureHistory("NinjaAccountUpdateRows", "ninja");
        b.Property(x => x.CaptainId).HasMaxLength(150).IsRequired();
        b.Property(x => x.SheetOwnerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.SheetCity).HasMaxLength(200).IsRequired();
        b.Property(x => x.SheetIqamaNo).HasMaxLength(100).IsRequired();
        b.Property(x => x.SheetStatus).HasMaxLength(100).IsRequired();
        b.Property(x => x.MatchStatus).HasMaxLength(20).IsRequired();
        b.Property(x => x.ProblemsJson).IsRequired();
        b.HasOne<NinjaAccountUpdateUpload>().WithMany().HasForeignKey(x => x.UploadId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UploadId, x.SourceRowNumber }).IsUnique();
        b.HasIndex(x => new { x.UploadId, x.CaptainId }).IsUnique();
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.CreatedAtUtc, x.Id });
        b.ToTable(t => t.HasCheckConstraint("CK_NinjaAccountUpdateRows_MatchStatus", "[MatchStatus] IN ('Found', 'NotFound', 'Ambiguous')"));
    }
}
