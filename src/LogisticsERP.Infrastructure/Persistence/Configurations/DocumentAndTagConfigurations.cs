using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Tags;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class DocumentRequirementConfiguration : IEntityTypeConfiguration<DocumentRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentRequirement> builder)
    {
        builder.ConfigureOperational("DocumentRequirements");
        builder.Property(entity => entity.ReminderOffsetsDays).HasMaxLength(100).IsRequired();
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(entity => entity.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new
        {
            entity.DocumentTypeId,
            entity.RelationshipType,
            entity.AppliesToRiderProfile,
            entity.EffectiveFrom
        }).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.EffectiveTo });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_DocumentRequirements_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
    }
}

internal sealed class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ConfigureOperational("EmployeeDocuments");
        builder.Property(entity => entity.DocumentNumber).HasMaxLength(150);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(entity => entity.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocumentVersion>().WithMany().HasForeignKey(entity => entity.CurrentVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.DocumentTypeId, entity.Status });
        builder.HasIndex(entity => new { entity.Status, entity.ExpiryDate, entity.EmployeeId })
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.DocumentTypeId, entity.DocumentNumber })
            .IsUnique()
            .HasFilter("[DocumentNumber] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> 3");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeDocuments_DateRange",
            "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]"));
    }
}

internal sealed class EmployeeDocumentVersionConfiguration : IEntityTypeConfiguration<EmployeeDocumentVersion>
{
    public void Configure(EntityTypeBuilder<EmployeeDocumentVersion> builder)
    {
        builder.ConfigureHistory("EmployeeDocumentVersions");
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(entity => entity.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.PreviewStatus).HasMaxLength(50);
        builder.Property(entity => entity.PreviewStoragePath).HasMaxLength(1000);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocumentVersion>().WithMany().HasForeignKey(entity => entity.SupersededVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeDocumentId, entity.VersionNumber }).IsUnique();
        builder.HasIndex(entity => entity.Sha256Checksum);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_EmployeeDocumentVersions_VersionNumber", "[VersionNumber] > 0");
            table.HasCheckConstraint("CK_EmployeeDocumentVersions_FileSize", "[FileSizeBytes] > 0");
        });
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ConfigureOperational("Tags");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Color).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

internal sealed class EmployeeTagConfiguration : IEntityTypeConfiguration<EmployeeTag>
{
    public void Configure(EntityTypeBuilder<EmployeeTag> builder)
    {
        builder.ConfigureOperational("EmployeeTags");
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>().WithMany().HasForeignKey(entity => entity.TagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.TagId }).IsUnique();
    }
}

internal sealed class HousingTagConfiguration : IEntityTypeConfiguration<HousingTag>
{
    public void Configure(EntityTypeBuilder<HousingTag> builder)
    {
        builder.ConfigureOperational("HousingTags");
        builder.HasOne<Housing>().WithMany().HasForeignKey(entity => entity.HousingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>().WithMany().HasForeignKey(entity => entity.TagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.HousingId, entity.TagId }).IsUnique();
    }
}

internal sealed class ClientContractTagConfiguration : IEntityTypeConfiguration<ClientContractTag>
{
    public void Configure(EntityTypeBuilder<ClientContractTag> builder)
    {
        builder.ConfigureOperational("ClientContractTags");
        builder.HasOne<ClientContract>().WithMany().HasForeignKey(entity => entity.ClientContractId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>().WithMany().HasForeignKey(entity => entity.TagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.ClientContractId, entity.TagId }).IsUnique();
    }
}

internal sealed class PlatformRiderAccountTagConfiguration : IEntityTypeConfiguration<PlatformRiderAccountTag>
{
    public void Configure(EntityTypeBuilder<PlatformRiderAccountTag> builder)
    {
        builder.ConfigureOperational("PlatformRiderAccountTags");
        builder.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(entity => entity.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tag>().WithMany().HasForeignKey(entity => entity.TagId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PlatformRiderAccountId, entity.TagId }).IsUnique();
    }
}
