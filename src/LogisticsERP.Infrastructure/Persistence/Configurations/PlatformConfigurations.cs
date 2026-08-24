using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ConfigureAuditable("CompanyProfile", "platform");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.LegalNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.LegalNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DisplayNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DisplayNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CommercialRegistrationNumber).HasMaxLength(100);
        builder.Property(entity => entity.UnifiedNationalNumber).HasMaxLength(100);
        builder.Property(entity => entity.VatNumber).HasMaxLength(100);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ContactEmail).HasMaxLength(320);
        builder.Property(entity => entity.LogoAssetKey).HasMaxLength(500);
        builder.Property(entity => entity.DefaultLocale).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.SuspensionReason).HasMaxLength(500);
        builder.OwnsOne(entity => entity.Address, owned => owned.ConfigureAddress("Address"));
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CompanyProfile_SingleRow",
            $"[Id] = '{CompanyProfile.FixedId}'"));
        builder.HasData(new
        {
            Id = CompanyProfile.FixedId,
            Code = "ALBAWABA",
            LegalNameAr = "البوابة للخدمات اللوجستية",
            LegalNameEn = "Al Bawaba Logistics Services",
            DisplayNameAr = "البوابة للخدمات اللوجستية",
            DisplayNameEn = "Al Bawaba Logistics",
            ContactPhone = string.Empty,
            DefaultLocale = "ar",
            TimeZoneId = "Asia/Riyadh",
            NextEmployeeSequence = 1L,
            Status = LogisticsERP.Domain.Enums.CompanyStatus.Setup,
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsDeleted = false
        });
    }
}

internal sealed class GlobalCityConfiguration : IEntityTypeConfiguration<GlobalCity>
{
    public void Configure(EntityTypeBuilder<GlobalCity> builder)
    {
        builder.ConfigureAuditable("GlobalCities", "platform");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.RegionAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.RegionEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CountryCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(entity => entity.Latitude).HasPrecision(9, 6);
        builder.Property(entity => entity.Longitude).HasPrecision(9, 6);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.NameAr, entity.NameEn });
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new
            {
                Id = GlobalCity.JeddahId,
                Code = "JEDDAH",
                NameAr = "جدة",
                NameEn = "Jeddah",
                RegionAr = "منطقة مكة المكرمة",
                RegionEn = "Makkah Region",
                CountryCode = "SA",
                Latitude = (decimal?)21.4858m,
                Longitude = (decimal?)39.1925m,
                DisplayOrder = 1,
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            },
            new
            {
                Id = GlobalCity.RiyadhId,
                Code = "RIYADH",
                NameAr = "الرياض",
                NameEn = "Riyadh",
                RegionAr = "منطقة الرياض",
                RegionEn = "Riyadh Region",
                CountryCode = "SA",
                Latitude = (decimal?)24.7136m,
                Longitude = (decimal?)46.6753m,
                DisplayOrder = 2,
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            });
    }
}

internal sealed class OperatingCityConfiguration : IEntityTypeConfiguration<OperatingCity>
{
    public void Configure(EntityTypeBuilder<OperatingCity> builder)
    {
        builder.ConfigureOperational("OperatingCities");
        builder.HasOne<GlobalCity>().WithMany().HasForeignKey(entity => entity.GlobalCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.GlobalCityId).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_OperatingCities_DateRange",
            "[DisabledAt] IS NULL OR [DisabledAt] >= [EnabledFrom]"));
        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new
            {
                Id = OperatingCity.JeddahId,
                GlobalCityId = GlobalCity.JeddahId,
                EnabledFrom = new DateOnly(2026, 1, 1),
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            },
            new
            {
                Id = OperatingCity.RiyadhId,
                GlobalCityId = GlobalCity.RiyadhId,
                EnabledFrom = new DateOnly(2026, 1, 1),
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            });
    }
}

internal sealed class ClientPlatformConfiguration : IEntityTypeConfiguration<ClientPlatform>
{
    public void Configure(EntityTypeBuilder<ClientPlatform> builder)
    {
        builder.ConfigureAuditable("ClientPlatforms", "platform");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500);
        builder.Property(entity => entity.LogoAssetKey).HasMaxLength(500);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

internal sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        builder.ConfigureAuditable("PermissionDefinitions", "platform");
        builder.Property(entity => entity.Key).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.GrantabilityRule).HasMaxLength(500);
        builder.Property(entity => entity.ReplacementKey).HasMaxLength(150);
        builder.HasIndex(entity => entity.Key).IsUnique();
        builder.HasIndex(entity => new { entity.Category, entity.DisplayOrder });

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(PermissionSeedCatalog.All.Select(permission => new PermissionDefinition
        {
            Id = permission.Id,
            Key = permission.Key,
            Category = permission.Category,
            NameAr = permission.NameAr,
            NameEn = permission.NameEn,
            DescriptionAr = permission.DescriptionAr,
            DescriptionEn = permission.DescriptionEn,
            RequiresHousingScope = permission.RequiresHousingScope,
            RequiresClientScope = permission.RequiresClientScope,
            IsSensitive = permission.IsSensitive,
            IsHighTrust = permission.IsHighTrust,
            GrantabilityRule = permission.GrantabilityRule,
            Version = 1,
            IsDeprecated = false,
            DisplayOrder = permission.DisplayOrder,
            CreatedAtUtc = seededAt,
            IsDeleted = false
        }));
    }
}

internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ConfigureAuditable("DocumentTypes", "platform");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500);
        builder.Property(entity => entity.AllowedMimeTypes).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.MaxFileSizeBytes).HasDefaultValue(10 * 1024 * 1024);
        builder.ToTable(table => table.HasCheckConstraint("CK_DocumentTypes_MaxFileSize", "[MaxFileSizeBytes] > 0"));
        builder.HasIndex(entity => entity.Code).IsUnique();

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        const string allowedMimeTypes = "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp";
        const long maxFileSizeBytes = 10 * 1024 * 1024;
        builder.HasData(
            CreateDocumentTypeSeed(DocumentType.ResidencyPermitId, "RESIDENCY_PERMIT", "الإقامة", "Residency Permit", true, false, false, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.DriverLicenseId, "DRIVER_LICENSE", "رخصة القيادة", "Driver License", true, true, true, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.RiderCardId, "RIDER_CARD", "بطاقة السائق", "Rider Card", true, true, true, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.HealthCardId, "HEALTH_CARD", "البطاقة الصحية", "Health Card", true, true, true, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.PromissoryNoteId, "PROMISSORY_NOTE", "سند الأمر", "Promissory Note", true, true, false, true, true, false, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.MedicalInsuranceId, "MEDICAL_INSURANCE", "التأمين الطبي", "Medical Insurance", true, true, true, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt),
            CreateDocumentTypeSeed(DocumentType.AjeerContractId, "AJEER_CONTRACT", "عقود اجير", "Ajeer Contracts", true, true, true, true, true, true, true, allowedMimeTypes, maxFileSizeBytes, seededAt));
    }

    private static object CreateDocumentTypeSeed(
        Guid id,
        string code,
        string nameAr,
        string nameEn,
        bool appliesToSponsoredInternal,
        bool appliesToOutsideRider,
        bool appliesToRiderProfile,
        bool requiresNumber,
        bool requiresIssueDate,
        bool requiresExpiryDate,
        bool requiresFile,
        string allowedMimeTypes,
        long maxFileSizeBytes,
        DateTimeOffset createdAtUtc) => new
        {
            Id = id,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            AppliesToSponsoredInternal = appliesToSponsoredInternal,
            AppliesToOutsideRider = appliesToOutsideRider,
            AppliesToRiderProfile = appliesToRiderProfile,
            RequiresNumber = requiresNumber,
            RequiresIssueDate = requiresIssueDate,
            RequiresExpiryDate = requiresExpiryDate,
            RequiresFile = requiresFile,
            AllowedMimeTypes = allowedMimeTypes,
            MaxFileSizeBytes = maxFileSizeBytes,
            Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
            CreatedAtUtc = createdAtUtc,
            IsDeleted = false
        };
}
