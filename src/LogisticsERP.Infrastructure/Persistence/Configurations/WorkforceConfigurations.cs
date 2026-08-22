using LogisticsERP.Domain.Entities.Documents;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ConfigureOperational("Employees");
        builder.Property(entity => entity.EmployeeNumber).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.FullNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NormalizedNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NormalizedNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PrimaryPhone).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasIndex(entity => entity.EmployeeNumber).IsUnique();
        builder.HasIndex(entity => entity.NormalizedNameAr);
        builder.HasIndex(entity => entity.NormalizedNameEn);
        builder.HasIndex(entity => entity.PrimaryPhone);
        builder.HasIndex(entity => entity.CurrentStatus);
    }
}

internal sealed class EmployeeStatusPeriodConfiguration : IEntityTypeConfiguration<EmployeeStatusPeriod>
{
    public void Configure(EntityTypeBuilder<EmployeeStatusPeriod> builder)
    {
        builder.ConfigureHistory("EmployeeStatusPeriods");
        builder.Property(entity => entity.ReasonCode).HasMaxLength(100);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeStatusPeriods_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
    }
}

internal sealed class EmployeeRelationshipPeriodConfiguration : IEntityTypeConfiguration<EmployeeRelationshipPeriod>
{
    public void Configure(EntityTypeBuilder<EmployeeRelationshipPeriod> builder)
    {
        builder.ConfigureHistory("EmployeeRelationshipPeriods");
        builder.Property(entity => entity.ReasonCode).HasMaxLength(100);
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.SourceReference).HasMaxLength(200);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeRelationshipPeriods_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
    }
}

internal sealed class SponsoredInternalDetailsConfiguration : IEntityTypeConfiguration<SponsoredInternalDetails>
{
    public void Configure(EntityTypeBuilder<SponsoredInternalDetails> builder)
    {
        builder.ConfigureOperational("SponsoredInternalDetails");
        builder.Property(entity => entity.NationalityCountryCode).HasMaxLength(2).IsFixedLength();
        builder.Property(entity => entity.SecondaryPhone).HasMaxLength(32);
        builder.Property(entity => entity.Email).HasMaxLength(320);
        builder.Property(entity => entity.EducationLevel).HasMaxLength(100);
        builder.Property(entity => entity.EducationDetails).HasMaxLength(1000);
        builder.Property(entity => entity.Profession).HasMaxLength(200);
        builder.Property(entity => entity.EmergencyContactName).HasMaxLength(200);
        builder.Property(entity => entity.EmergencyContactRelationship).HasMaxLength(100);
        builder.Property(entity => entity.EmergencyContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.SponsorLegalReference).HasMaxLength(200);
        builder.Property(entity => entity.InternalNotes).HasMaxLength(4000);
        builder.OwnsOne(entity => entity.HomeAddress, owned => owned.ConfigureAddress("HomeAddress"));
        builder.HasOne<Employee>().WithOne().HasForeignKey<SponsoredInternalDetails>(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.ManagerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobTitle>().WithMany().HasForeignKey(entity => entity.CurrentJobTitleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.ProfilePhotoDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_SponsoredInternalDetails_Dependents", "[DependentsCount] IS NULL OR [DependentsCount] >= 0");
            table.HasCheckConstraint("CK_SponsoredInternalDetails_ContractRange", "[ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate]");
        });
    }
}

internal sealed class OutsideRiderDetailsConfiguration : IEntityTypeConfiguration<OutsideRiderDetails>
{
    public void Configure(EntityTypeBuilder<OutsideRiderDetails> builder)
    {
        builder.ConfigureOperational("OutsideRiderDetails");
        builder.Property(entity => entity.NationalityCountryCode).HasMaxLength(2).IsFixedLength();
        builder.Property(entity => entity.AlternateContactName).HasMaxLength(200);
        builder.Property(entity => entity.AlternateContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.EngagementReference).HasMaxLength(200);
        builder.Property(entity => entity.EngagementNotes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithOne().HasForeignKey<OutsideRiderDetails>(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
    }
}

internal sealed class RiderProfileConfiguration : IEntityTypeConfiguration<RiderProfile>
{
    public void Configure(EntityTypeBuilder<RiderProfile> builder)
    {
        builder.ConfigureOperational("RiderProfiles");
        builder.Property(entity => entity.OperationalNotes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithOne().HasForeignKey<RiderProfile>(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GlobalCity>().WithMany().HasForeignKey(entity => entity.PreferredCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.LicenseDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_RiderProfiles_DateRange",
            "[RiderEndDate] IS NULL OR [RiderStartDate] IS NULL OR [RiderEndDate] >= [RiderStartDate]"));
    }
}

internal sealed class JobTitleConfiguration : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.ConfigureOperational("JobTitles");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500);
        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}

internal sealed class EmployeeJobTitlePeriodConfiguration : IEntityTypeConfiguration<EmployeeJobTitlePeriod>
{
    public void Configure(EntityTypeBuilder<EmployeeJobTitlePeriod> builder)
    {
        builder.ConfigureHistory("EmployeeJobTitlePeriods");
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobTitle>().WithMany().HasForeignKey(entity => entity.JobTitleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_EmployeeJobTitlePeriods_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
    }
}
