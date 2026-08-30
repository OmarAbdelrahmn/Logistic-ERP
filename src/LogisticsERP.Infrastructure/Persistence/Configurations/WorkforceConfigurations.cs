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
        builder.Property(entity => entity.IqamaNo).HasMaxLength(10).IsUnicode(false);
        builder.Property(entity => entity.ResidencyProfession).HasMaxLength(200);
        builder.Property(entity => entity.WorkingForMeAs).HasMaxLength(200);
        builder.Property(entity => entity.FullNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.FullNameEn).HasMaxLength(200);
        builder.Property(entity => entity.Nationality).HasMaxLength(100);
        builder.Property(entity => entity.Iban).HasMaxLength(34).IsUnicode(false);
        builder.Property(entity => entity.PrimaryPhone).HasMaxLength(32);
        builder.Property(entity => entity.SecondaryPhone).HasMaxLength(32);
        builder.Property(entity => entity.Email).HasMaxLength(320);
        builder.Property(entity => entity.EmergencyContactName).HasMaxLength(200);
        builder.Property(entity => entity.EmergencyContactRelationship).HasMaxLength(100);
        builder.Property(entity => entity.EmergencyContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.StatusReason).HasMaxLength(500);
        builder.Property(entity => entity.AlternateContactName).HasMaxLength(200);
        builder.Property(entity => entity.AlternateContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.OwnsOne(entity => entity.Address, owned => owned.ConfigureAddress("Address"));

        builder.HasOne<EmployeeDocument>().WithMany().HasForeignKey(entity => entity.ProfilePhotoDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalWorkType>().WithMany().HasForeignKey(entity => entity.OperationalWorkTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperatingCity>().WithMany().HasForeignKey(entity => entity.OperatingCityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany().HasForeignKey(entity => entity.SponsorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.IqamaNo)
            .IsUnique()
            .HasFilter("[IqamaNo] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => entity.FullNameAr);
        builder.HasIndex(entity => entity.FullNameEn);
        builder.HasIndex(entity => entity.PrimaryPhone);
        builder.HasIndex(entity => new { entity.IsEmployee, entity.EngagementType, entity.Status });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Employees_IqamaNo", "[IqamaNo] IS NULL OR (LEN([IqamaNo]) = 10 AND [IqamaNo] NOT LIKE '%[^0-9]%')");
            table.HasCheckConstraint("CK_Employees_ActiveIqama", "[Status] <> 3 OR [IqamaNo] IS NOT NULL");
            table.HasCheckConstraint("CK_Employees_OutsideIsRider", "[EngagementType] <> 2 OR [IsEmployee] = 0");
            table.HasCheckConstraint("CK_Employees_ActiveInternalSponsor", "[Status] <> 3 OR [EngagementType] <> 1 OR [SponsorId] IS NOT NULL");
            table.HasCheckConstraint("CK_Employees_ContractRange", "[ContractEndDate] IS NULL OR [ContractStartDate] IS NULL OR [ContractEndDate] >= [ContractStartDate]");
        });
    }
}

internal sealed class RiderProfileConfiguration : IEntityTypeConfiguration<RiderProfile>
{
    public void Configure(EntityTypeBuilder<RiderProfile> builder)
    {
        builder.ConfigureOperational("RiderProfiles");
        builder.Property(entity => entity.OperationalNotes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithOne().HasForeignKey<RiderProfile>(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.EmployeeId).IsUnique();
    }
}

internal sealed class EmployeeWorkHistoryConfiguration : IEntityTypeConfiguration<EmployeeWorkHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkHistory> builder)
    {
        builder.ConfigureHistory("EmployeeWorkHistory");
        builder.Property(entity => entity.OldValue).HasMaxLength(1000);
        builder.Property(entity => entity.NewValue).HasMaxLength(1000);
        builder.Property(entity => entity.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.EffectiveDate, entity.ChangeType });
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
