using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HrLegalCaseConfiguration : IEntityTypeConfiguration<HrLegalCase>
{
    public void Configure(EntityTypeBuilder<HrLegalCase> builder)
    {
        builder.ConfigureOperational("HrLegalCases");
        builder.Property(x => x.CaseNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PersonName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sponsor>().WithMany().HasForeignKey(x => x.SponsorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CaseNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Status, x.CaseDate, x.CaseTime });
        builder.HasIndex(x => x.ResponsibleUserId);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_HrLegalCases_PersonReference",
            "([PersonType] = 1 AND [EmployeeId] IS NOT NULL AND [RiderProfileId] IS NULL) OR " +
            "([PersonType] = 2 AND [EmployeeId] IS NULL AND [RiderProfileId] IS NOT NULL) OR " +
            "([PersonType] = 3 AND [EmployeeId] IS NULL AND [RiderProfileId] IS NULL)"));
    }
}

internal sealed class HrLegalCaseHearingConfiguration : IEntityTypeConfiguration<HrLegalCaseHearing>
{
    public void Configure(EntityTypeBuilder<HrLegalCaseHearing> builder)
    {
        builder.ConfigureOperational("HrLegalCaseHearings");
        builder.Property(x => x.Details).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Location).HasMaxLength(500);
        builder.HasOne<HrLegalCase>().WithMany().HasForeignKey(x => x.LegalCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LegalCaseId, x.HearingNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Status, x.HearingDate, x.HearingTime });
        builder.ToTable(table => table.HasCheckConstraint("CK_HrLegalCaseHearings_Number", "[HearingNumber] > 0"));
    }
}

internal sealed class HrLegalCaseHearingFileConfiguration : IEntityTypeConfiguration<HrLegalCaseHearingFile>
{
    public void Configure(EntityTypeBuilder<HrLegalCaseHearingFile> builder)
    {
        builder.ConfigureOperational("HrLegalCaseHearingFiles");
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasOne<HrLegalCaseHearing>().WithMany().HasForeignKey(x => x.HearingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.HearingId, x.UploadedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint("CK_HrLegalCaseHearingFiles_Size", "[FileSizeBytes] > 0"));
    }
}

internal sealed class HrLegalCaseHistoryConfiguration : IEntityTypeConfiguration<HrLegalCaseHistory>
{
    public void Configure(EntityTypeBuilder<HrLegalCaseHistory> builder)
    {
        builder.ConfigureHistory("HrLegalCaseHistory");
        builder.Property(x => x.ChangeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangedFieldsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AfterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ChangeReason).HasMaxLength(1000).IsRequired();
        builder.HasOne<HrLegalCase>().WithMany().HasForeignKey(x => x.LegalCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HrLegalCaseHearing>().WithMany().HasForeignKey(x => x.HearingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LegalCaseId, x.CreatedAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_HrLegalCaseHistory_ChangedFields", "ISJSON([ChangedFieldsJson]) = 1");
            table.HasCheckConstraint("CK_HrLegalCaseHistory_Before", "[BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1");
            table.HasCheckConstraint("CK_HrLegalCaseHistory_After", "ISJSON([AfterJson]) = 1");
        });
    }
}
