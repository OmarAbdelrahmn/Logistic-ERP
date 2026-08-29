using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HrFormTemplateConfiguration : IEntityTypeConfiguration<HrFormTemplate>
{
    public void Configure(EntityTypeBuilder<HrFormTemplate> builder)
    {
        builder.ConfigureOperational("HrFormTemplates");
        builder.Property(entity => entity.Code).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(2000);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(2000);
        builder.HasOne<HrFormTemplateVersion>()
            .WithMany()
            .HasForeignKey(entity => entity.CurrentDraftVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HrFormTemplateVersion>()
            .WithMany()
            .HasForeignKey(entity => entity.CurrentPublishedVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.IsActive, entity.Category, entity.NameAr })
            .HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class HrFormTemplateVersionConfiguration : IEntityTypeConfiguration<HrFormTemplateVersion>
{
    public void Configure(EntityTypeBuilder<HrFormTemplateVersion> builder)
    {
        builder.ConfigureHistory("HrFormTemplateVersions");
        builder.Property(entity => entity.DefinitionJson).IsRequired();
        builder.Property(entity => entity.DefinitionSha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(entity => entity.ChangeNote).HasMaxLength(500);
        builder.HasOne<HrFormTemplate>()
            .WithMany()
            .HasForeignKey(entity => entity.HrFormTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.HrFormTemplateId, entity.VersionNumber }).IsUnique();
        builder.HasIndex(entity => entity.DefinitionSha256);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_HrFormTemplateVersions_VersionNumbers",
            "[VersionNumber] > 0 AND [DefinitionSchemaVersion] > 0"));
    }
}
