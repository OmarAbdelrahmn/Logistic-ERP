using LogisticsERP.Domain.Entities.Telecom;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class PhoneSimCardConfiguration : IEntityTypeConfiguration<PhoneSimCard>
{
    public void Configure(EntityTypeBuilder<PhoneSimCard> builder)
    {
        builder.ConfigureOperational("PhoneSimCards");
        builder.Property(entity => entity.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NormalizedPhoneNumber).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(entity => entity.Iccid).HasMaxLength(22).IsUnicode(false);
        builder.Property(entity => entity.NormalizedIccid).HasMaxLength(22).IsUnicode(false);
        builder.Property(entity => entity.CarrierName).HasMaxLength(200);
        builder.Property(entity => entity.StatusReason).HasMaxLength(500);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.Property(entity => entity.ReceiptFormOriginalFileName).HasMaxLength(255);
        builder.Property(entity => entity.ReceiptFormStoredFileName).HasMaxLength(255).IsUnicode(false);
        builder.Property(entity => entity.ReceiptFormContentType).HasMaxLength(100).IsUnicode(false);
        builder.Property(entity => entity.ReceiptFormSha256Checksum).HasMaxLength(64).IsUnicode(false);
        builder.Property(entity => entity.ReceiptFormStoragePath).HasMaxLength(1000).IsUnicode(false);
        builder.HasOne<Employee>().WithMany()
            .HasForeignKey(entity => entity.ResponsibleEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.NormalizedPhoneNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => entity.NormalizedIccid)
            .IsUnique()
            .HasFilter("[NormalizedIccid] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.Status, entity.ResponsibleEmployeeId });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PhoneSimCards_Status",
            "[Status] BETWEEN 1 AND 5"));
    }
}

internal sealed class RiderPhoneSimAssignmentConfiguration : IEntityTypeConfiguration<RiderPhoneSimAssignment>
{
    public void Configure(EntityTypeBuilder<RiderPhoneSimAssignment> builder)
    {
        builder.ConfigureTemporal("RiderPhoneSimAssignments");
        builder.Property(entity => entity.AssignmentReason).HasMaxLength(1000);
        builder.Property(entity => entity.EndReason).HasMaxLength(1000);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.HasOne<PhoneSimCard>().WithMany()
            .HasForeignKey(entity => entity.PhoneSimCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany()
            .HasForeignKey(entity => entity.RiderProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PhoneSimCardId, entity.EffectiveFrom });
        builder.HasIndex(entity => new { entity.RiderProfileId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.PhoneSimCardId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
    }
}

internal sealed class PhoneSimResponsibilityChangeConfiguration : IEntityTypeConfiguration<PhoneSimResponsibilityChange>
{
    public void Configure(EntityTypeBuilder<PhoneSimResponsibilityChange> builder)
    {
        builder.ConfigureHistory("PhoneSimResponsibilityChanges");
        builder.Property(entity => entity.Reason).HasMaxLength(1000).IsRequired();
        builder.HasOne<PhoneSimCard>().WithMany()
            .HasForeignKey(entity => entity.PhoneSimCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany()
            .HasForeignKey(entity => entity.PreviousResponsibleEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany()
            .HasForeignKey(entity => entity.ResponsibleEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.PhoneSimCardId, entity.ChangedAtUtc });
        builder.HasIndex(entity => new { entity.ResponsibleEmployeeId, entity.ChangedAtUtc });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_PhoneSimResponsibilityChanges_ChangedResponsibleEmployee",
            "[PreviousResponsibleEmployeeId] IS NULL OR [PreviousResponsibleEmployeeId] <> [ResponsibleEmployeeId]"));
    }
}
