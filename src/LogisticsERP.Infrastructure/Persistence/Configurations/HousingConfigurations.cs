using LogisticsERP.Domain.Entities.Housing;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class HousingConfiguration : IEntityTypeConfiguration<Housing>
{
    public void Configure(EntityTypeBuilder<Housing> builder)
    {
        builder.ConfigureOperational("Housing");
        builder.Property(entity => entity.Code).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Latitude).HasPrecision(9, 6);
        builder.Property(entity => entity.Longitude).HasPrecision(9, 6);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(32);
        builder.Property(entity => entity.StatusReason).HasMaxLength(500);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.OwnsOne(entity => entity.Address, owned => owned.ConfigureAddress("Address"));
        builder.HasOne<GlobalCity>().WithMany().HasForeignKey(entity => entity.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.Status, entity.CityId });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Housing_TotalCapacity", "[TotalCapacity] > 0");
            table.HasCheckConstraint("CK_Housing_DateRange", "[ClosedDate] IS NULL OR [OpenedDate] IS NULL OR [ClosedDate] >= [OpenedDate]");
        });
    }
}

internal sealed class HousingSupervisorPeriodConfiguration : IEntityTypeConfiguration<HousingSupervisorPeriod>
{
    public void Configure(EntityTypeBuilder<HousingSupervisorPeriod> builder)
    {
        builder.ConfigureHistory("HousingSupervisorPeriods");
        builder.Property(entity => entity.AssignmentReason).HasMaxLength(1000);
        builder.Property(entity => entity.EndReason).HasMaxLength(1000);
        builder.HasOne<Housing>().WithMany().HasForeignKey(entity => entity.HousingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.SupervisorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.HousingId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.HousingId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_HousingSupervisorPeriods_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));
    }
}

internal sealed class HousingResidencePeriodConfiguration : IEntityTypeConfiguration<HousingResidencePeriod>
{
    public void Configure(EntityTypeBuilder<HousingResidencePeriod> builder)
    {
        builder.ConfigureHistory("HousingResidencePeriods");
        builder.Property(entity => entity.MoveInReason).HasMaxLength(1000);
        builder.Property(entity => entity.MoveOutReason).HasMaxLength(1000);
        builder.Property(entity => entity.SourceReference).HasMaxLength(200);
        builder.Property(entity => entity.DestinationReference).HasMaxLength(200);
        builder.Property(entity => entity.CapacityOverrideReason).HasMaxLength(1000);
        builder.HasOne<Employee>().WithMany().HasForeignKey(entity => entity.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Housing>().WithMany().HasForeignKey(entity => entity.HousingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.EmployeeId, entity.EffectiveFrom });
        builder.HasIndex(entity => new { entity.HousingId, entity.EffectiveFrom });
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_HousingResidencePeriods_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint("CK_HousingResidencePeriods_CapacityOverrideReason", "[CapacityOverrideUsed] = 0 OR [CapacityOverrideReason] IS NOT NULL");
        });
    }
}
