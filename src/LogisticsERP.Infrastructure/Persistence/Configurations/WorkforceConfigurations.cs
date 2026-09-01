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

internal sealed class PayrollEmployeeConfiguration : IEntityTypeConfiguration<PayrollEmployee>
{
    public void Configure(EntityTypeBuilder<PayrollEmployee> builder)
    {
        builder.ConfigureOperational("PayrollEmployees");
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NationalId).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Country).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.PersonalIban).HasMaxLength(24).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Salary).HasPrecision(18, 2);
        builder.Property(entity => entity.Status).HasMaxLength(100).IsRequired();

        builder.HasOne<Sponsor>().WithMany()
            .HasForeignKey(entity => entity.SponsorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.Number).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => entity.NationalId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => entity.PersonalIban).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => entity.Name);
        builder.HasIndex(entity => new { entity.Status, entity.JoiningDate });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_PayrollEmployees_Number", "[Number] > 0");
            table.HasCheckConstraint("CK_PayrollEmployees_NationalId", "LEN([NationalId]) = 10 AND [NationalId] NOT LIKE '%[^0-9]%'");
            table.HasCheckConstraint("CK_PayrollEmployees_PersonalIban", "LEN([PersonalIban]) = 24 AND LEFT([PersonalIban], 2) = 'SA' AND SUBSTRING([PersonalIban], 3, 22) NOT LIKE '%[^0-9]%'");
            table.HasCheckConstraint("CK_PayrollEmployees_Salary", "[Salary] >= 0");
        });

        var createdAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            Seed(1, "01990000-0000-7000-8000-000000000001", "جمانه عبدالكريم بن حسن القحطاني", "1125236081", new DateOnly(2025, 9, 24), "SA6980000107608016495857", 1000m, createdAt),
            Seed(2, "01990000-0000-7000-8000-000000000002", "ندى علي سلمان غمقه", "1055695991", new DateOnly(2025, 10, 14), "SA6980000209608016472812", 1000m, createdAt),
            Seed(3, "01990000-0000-7000-8000-000000000003", "ريم محمد ابن حابي آل بسام", "1094893391", new DateOnly(2025, 10, 14), "SA7680000688608010011525", 1000m, createdAt),
            Seed(4, "01990000-0000-7000-8000-000000000004", "هتون سعد سالم آل بسام", "1109500338", new DateOnly(2025, 10, 14), "SA6380000209608016490962", 1000m, createdAt),
            Seed(5, "01990000-0000-7000-8000-000000000005", "هديل سعد سالم آل بسام", "1120249709", new DateOnly(2025, 10, 14), "SA7480000209608014899867", 1000m, createdAt),
            Seed(6, "01990000-0000-7000-8000-000000000006", "فيصل سعد سالم آل بسام", "1140492552", new DateOnly(2025, 11, 4), "SA8080000107608016555023", 1000m, createdAt),
            Seed(7, "01990000-0000-7000-8000-000000000007", "رغد عبدالله بن محمد آل هادي", "1124916642", new DateOnly(2025, 10, 14), "SA2380000437608016041454", 1000m, createdAt),
            Seed(8, "01990000-0000-7000-8000-000000000008", "بتلا يحي محمد القحطاني", "1012865497", new DateOnly(2025, 12, 22), "SA5880000347608010801019", 1000m, createdAt),
            Seed(10, "01990000-0000-7000-8000-000000000010", "شذي مشعل بن جبر السلمى", "1108386739", new DateOnly(2025, 12, 30), "SA3980000176608010913604", 1500m, createdAt));
    }

    private static PayrollEmployee Seed(
        int number,
        string id,
        string name,
        string nationalId,
        DateOnly joiningDate,
        string personalIban,
        decimal salary,
        DateTimeOffset createdAt) => new()
    {
        Id = Guid.Parse(id),
        Number = number,
        SponsorId = Sponsor.AlBawabaCommercialEstablishmentId,
        Name = name,
        NationalId = nationalId,
        Country = "السعودية",
        JoiningDate = joiningDate,
        PersonalIban = personalIban,
        Salary = salary,
        Status = string.Empty,
        CreatedAtUtc = createdAt
    };
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
