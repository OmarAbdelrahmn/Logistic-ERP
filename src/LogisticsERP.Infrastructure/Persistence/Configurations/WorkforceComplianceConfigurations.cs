using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class SponsorConfiguration : IEntityTypeConfiguration<Sponsor>
{
    public void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.ConfigureOperational("Sponsors");
        builder.Property(entity => entity.EmployerIdentityNumber).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RegistryNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.RegistryNameEn).HasMaxLength(200);
        builder.Property(entity => entity.CommercialRegistrationNumber).HasMaxLength(100);
        builder.Property(entity => entity.UnifiedNationalNumber).HasMaxLength(100);
        builder.Property(entity => entity.ContactName).HasMaxLength(200);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.ContactEmail).HasMaxLength(320);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.OwnsOne(entity => entity.Address, owned => owned.ConfigureAddress("Address"));
        builder.HasOne<CompanyProfile>().WithMany().HasForeignKey(entity => entity.CompanyProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployerIdentityNumber).IsUnique();
        builder.HasIndex(entity => entity.CommercialRegistrationNumber)
            .IsUnique()
            .HasFilter("[CommercialRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.Status, entity.RegistryNameAr });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Sponsors_ActiveRange",
            "[ActiveTo] IS NULL OR [ActiveFrom] IS NULL OR [ActiveTo] >= [ActiveFrom]"));

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var activeFrom = new DateOnly(2026, 1, 1);
        builder.HasData(
            new
            {
                Id = Sponsor.AlBawabaCommercialEstablishmentId,
                CompanyProfileId = CompanyProfile.FixedId,
                EmployerIdentityNumber = "7038745530",
                RegistryNameAr = "مؤسسة البوابة التجارية",
                SponsorType = LogisticsERP.Domain.Enums.SponsorType.Establishment,
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                ActiveFrom = (DateOnly?)activeFrom,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            },
            new
            {
                Id = Sponsor.AlBawabaNextCompanyId,
                CompanyProfileId = CompanyProfile.FixedId,
                EmployerIdentityNumber = "7015658094",
                RegistryNameAr = "شركة البوابة المقبلة",
                SponsorType = LogisticsERP.Domain.Enums.SponsorType.Company,
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                ActiveFrom = (DateOnly?)activeFrom,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            },
            new
            {
                Id = Sponsor.ExpressGateId,
                CompanyProfileId = CompanyProfile.FixedId,
                EmployerIdentityNumber = "7034861059",
                RegistryNameAr = "اكسبرس جايت",
                SponsorType = LogisticsERP.Domain.Enums.SponsorType.Company,
                Status = LogisticsERP.Domain.Enums.CatalogStatus.Active,
                ActiveFrom = (DateOnly?)activeFrom,
                CreatedAtUtc = seededAt,
                IsDeleted = false
            });
    }
}

internal sealed class ResidencyProfessionConfiguration : IEntityTypeConfiguration<ResidencyProfession>
{
    public void Configure(EntityTypeBuilder<ResidencyProfession> builder)
    {
        builder.ConfigureOperational("ResidencyProfessions");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.NameAr);
    }
}

internal sealed class OperationalWorkTypeConfiguration : IEntityTypeConfiguration<OperationalWorkType>
{
    public void Configure(EntityTypeBuilder<OperationalWorkType> builder)
    {
        builder.ConfigureOperational("OperationalWorkTypes");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new { Id = OperationalWorkType.AdministrativeId, Code = "ADMIN", NameAr = "إداري", NameEn = "Administrative", Status = LogisticsERP.Domain.Enums.CatalogStatus.Active, CreatedAtUtc = seededAt, IsDeleted = false },
            new { Id = OperationalWorkType.CarId, Code = "CAR", NameAr = "سيارة", NameEn = "Car", Status = LogisticsERP.Domain.Enums.CatalogStatus.Active, CreatedAtUtc = seededAt, IsDeleted = false },
            new { Id = OperationalWorkType.MotorcycleId, Code = "MOTORCYCLE", NameAr = "دراجة نارية", NameEn = "Motorcycle", Status = LogisticsERP.Domain.Enums.CatalogStatus.Active, CreatedAtUtc = seededAt, IsDeleted = false });
    }
}

internal sealed class JobTitleOperationalWorkTypeConfiguration : IEntityTypeConfiguration<JobTitleOperationalWorkType>
{
    public void Configure(EntityTypeBuilder<JobTitleOperationalWorkType> builder)
    {
        builder.ConfigureOperational("JobTitleOperationalWorkTypes");
        builder.HasOne<JobTitle>().WithMany().HasForeignKey(entity => entity.JobTitleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalWorkType>().WithMany().HasForeignKey(entity => entity.OperationalWorkTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.JobTitleId, entity.OperationalWorkTypeId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class DriverLicenseCategoryConfiguration : IEntityTypeConfiguration<DriverLicenseCategory>
{
    public void Configure(EntityTypeBuilder<DriverLicenseCategory> builder)
    {
        builder.ConfigureOperational("DriverLicenseCategories");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new { Id = DriverLicenseCategory.LightTransportId, Code = "LIGHT_TRANSPORT", NameAr = "نقل خفيف", NameEn = "Light Transport", Status = LogisticsERP.Domain.Enums.CatalogStatus.Active, CreatedAtUtc = seededAt, IsDeleted = false },
            new { Id = DriverLicenseCategory.MotorcycleId, Code = "MOTORCYCLE", NameAr = "دراجة نارية", NameEn = "Motorcycle", Status = LogisticsERP.Domain.Enums.CatalogStatus.Active, CreatedAtUtc = seededAt, IsDeleted = false });
    }
}

internal sealed class EmployeeDriverLicenseConfiguration : IEntityTypeConfiguration<EmployeeDriverLicense>
{
    public void Configure(EntityTypeBuilder<EmployeeDriverLicense> builder)
    {
        builder.ConfigureOperational("EmployeeDriverLicenses");
        builder.Property(entity => entity.LicenseNumberLookupHash).HasMaxLength(64).IsFixedLength();
        builder.Property(entity => entity.LicenseNumberLastFour).HasMaxLength(4).IsFixedLength();
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DriverLicenseCategory>().WithMany().HasForeignKey(entity => entity.DriverLicenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDriverLicense>().WithMany().HasForeignKey(entity => entity.PreviousLicenseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.LicenseNumberLookupHash).HasFilter("[LicenseNumberLookupHash] IS NOT NULL");
        builder.HasIndex(entity => new { entity.EmployeeId, entity.DriverLicenseCategoryId, entity.LicenseStatus });
        builder.HasIndex(entity => new { entity.EmployeeId, entity.DriverLicenseCategoryId })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeDriverLicenses_DateRange",
            "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]"));
    }
}

internal sealed class RiderCardConfiguration : IEntityTypeConfiguration<RiderCard>
{
    public void Configure(EntityTypeBuilder<RiderCard> builder)
    {
        builder.ConfigureOperational("RiderCards");
        builder.Property(entity => entity.CardNumber).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.NormalizedCardNumber).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(entity => entity.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderCard>().WithMany().HasForeignKey(entity => entity.PreviousCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.NormalizedCardNumber);
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.CardType, entity.Status });
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.CardType })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(entity => entity.NormalizedCardNumber)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RiderCards_DateRange",
            "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]"));
    }
}

internal sealed class RiderHealthCardConfiguration : IEntityTypeConfiguration<RiderHealthCard>
{
    public void Configure(EntityTypeBuilder<RiderHealthCard> builder)
    {
        builder.ConfigureOperational("RiderHealthCards");
        builder.Property(entity => entity.CardNumberCiphertext).IsRequired();
        builder.Property(entity => entity.CardNumberLookupHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(entity => entity.CardNumberLastFour).HasMaxLength(4).IsFixedLength().IsRequired();
        builder.Property(entity => entity.CardType).HasMaxLength(100);
        builder.Property(entity => entity.IssuingAuthority).HasMaxLength(200);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(entity => entity.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderHealthCard>().WithMany().HasForeignKey(entity => entity.PreviousCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.CardNumberLookupHash);
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.CardType, entity.Status });
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.CardType })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(entity => entity.CardNumberLookupHash)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RiderHealthCards_DateRange",
            "[ExpiryDate] IS NULL OR [IssueDate] IS NULL OR [ExpiryDate] >= [IssueDate]"));
    }
}

internal sealed class EmployeePromissoryNoteConfiguration : IEntityTypeConfiguration<EmployeePromissoryNote>
{
    public void Configure(EntityTypeBuilder<EmployeePromissoryNote> builder)
    {
        builder.ConfigureOperational("EmployeePromissoryNotes");
        builder.Property(entity => entity.NoteNumber).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.NormalizedNoteNumber).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany().HasForeignKey(entity => entity.SponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompanyProfile>().WithMany().HasForeignKey(entity => entity.BeneficiaryCompanyProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.Status });
        builder.HasIndex(entity => new { entity.BeneficiaryCompanyProfileId, entity.NormalizedNoteNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_EmployeePromissoryNotes_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_EmployeePromissoryNotes_DateRange", "[DueDate] IS NULL OR [DueDate] >= [IssueDate]");
        });
    }
}

internal sealed class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
{
    public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
    {
        builder.ConfigureOperational("InsuranceCompanies");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.Property(entity => entity.ProviderRegistrationNumber).HasMaxLength(100);
        builder.Property(entity => entity.ContactName).HasMaxLength(200);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.ContactEmail).HasMaxLength(320);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.ProviderRegistrationNumber)
            .IsUnique()
            .HasFilter("[ProviderRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.Status, entity.NameAr });
    }
}

internal sealed class InsurancePlanLevelConfiguration : IEntityTypeConfiguration<InsurancePlanLevel>
{
    public void Configure(EntityTypeBuilder<InsurancePlanLevel> builder)
    {
        builder.ConfigureOperational("InsurancePlanLevels");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200);
        builder.Property(entity => entity.NetworkName).HasMaxLength(200);
        builder.Property(entity => entity.CoverageClass).HasMaxLength(100);
        builder.Property(entity => entity.AnnualCoverageLimit).HasPrecision(18, 2);
        builder.Property(entity => entity.DeductiblePercentage).HasPrecision(5, 2);
        builder.HasOne<InsuranceCompany>().WithMany().HasForeignKey(entity => entity.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(entity => new { entity.Id, entity.InsuranceCompanyId });
        builder.HasIndex(entity => new { entity.InsuranceCompanyId, entity.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.InsuranceCompanyId, entity.Status, entity.Rank });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_InsurancePlanLevels_Rank", "[Rank] >= 0");
            table.HasCheckConstraint("CK_InsurancePlanLevels_AnnualLimit", "[AnnualCoverageLimit] IS NULL OR [AnnualCoverageLimit] >= 0");
            table.HasCheckConstraint("CK_InsurancePlanLevels_Deductible", "[DeductiblePercentage] IS NULL OR ([DeductiblePercentage] >= 0 AND [DeductiblePercentage] <= 100)");
            table.HasCheckConstraint("CK_InsurancePlanLevels_DateRange", "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
    }
}

internal sealed class EmployeeMedicalInsurancePolicyConfiguration : IEntityTypeConfiguration<EmployeeMedicalInsurancePolicy>
{
    public void Configure(EntityTypeBuilder<EmployeeMedicalInsurancePolicy> builder)
    {
        builder.ConfigureOperational("EmployeeMedicalInsurancePolicies");
        builder.Property(entity => entity.PolicyNumberLookupHash).HasMaxLength(64).IsFixedLength();
        builder.Property(entity => entity.PolicyNumberLastFour).HasMaxLength(4).IsFixedLength();
        builder.Property(entity => entity.MemberNumberLookupHash).HasMaxLength(64).IsFixedLength();
        builder.Property(entity => entity.MemberNumberLastFour).HasMaxLength(4).IsFixedLength();
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InsuranceCompany>().WithMany().HasForeignKey(entity => entity.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InsurancePlanLevel>().WithMany()
            .HasForeignKey(entity => new { entity.InsurancePlanLevelId, entity.InsuranceCompanyId })
            .HasPrincipalKey(entity => new { entity.Id, entity.InsuranceCompanyId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeMedicalInsurancePolicy>().WithMany().HasForeignKey(entity => entity.PreviousPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.EmployeeDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PolicyNumberLookupHash).HasFilter("[PolicyNumberLookupHash] IS NOT NULL");
        builder.HasIndex(entity => entity.MemberNumberLookupHash).HasFilter("[MemberNumberLookupHash] IS NOT NULL");
        builder.HasIndex(entity => new { entity.InsuranceCompanyId, entity.Status, entity.EndDate });
        builder.HasIndex(entity => entity.EmployeeId).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeMedicalInsurancePolicies_DateRange",
            "[EndDate] >= [StartDate]"));
    }
}
