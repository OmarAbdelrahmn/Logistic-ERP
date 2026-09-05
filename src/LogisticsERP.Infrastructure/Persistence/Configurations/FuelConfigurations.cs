using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Entities.Workforce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class FuelCardConfiguration : IEntityTypeConfiguration<FuelCard>
{
    public void Configure(EntityTypeBuilder<FuelCard> builder)
    {
        builder.ConfigureOperational("FuelCards");
        builder.Property(x => x.CardNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedCardNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PlateNumberText).HasMaxLength(100);
        builder.Property(x => x.NormalizedPlateNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasIndex(x => new { x.Provider, x.NormalizedCardNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Provider, x.IdentifierType });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FuelCards_Provider", "[Provider] BETWEEN 1 AND 2");
            table.HasCheckConstraint("CK_FuelCards_IdentifierType", "[IdentifierType] BETWEEN 1 AND 2");
        });
    }
}

internal sealed class FuelCardRiderAssignmentConfiguration : IEntityTypeConfiguration<FuelCardRiderAssignment>
{
    public void Configure(EntityTypeBuilder<FuelCardRiderAssignment> builder)
    {
        builder.ConfigureTemporal("FuelCardRiderAssignments");
        builder.Property(x => x.AssignmentReason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.EndReason).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasOne<FuelCard>().WithMany().HasForeignKey(x => x.FuelCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.FuelCardId).IsUnique().HasFilter("[EffectiveTo] IS NULL");
        builder.HasIndex(x => new { x.FuelCardId, x.EffectiveFrom });
        builder.HasIndex(x => new { x.RiderProfileId, x.EffectiveFrom });
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });
    }
}

internal sealed class FuelCardMonthlyUsageConfiguration : IEntityTypeConfiguration<FuelCardMonthlyUsage>
{
    public void Configure(EntityTypeBuilder<FuelCardMonthlyUsage> builder)
    {
        builder.ConfigureOperational("FuelCardMonthlyUsages");
        builder.Property(x => x.TotalLiters).HasPrecision(18, 3);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.AmountBeforeTax).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.FuelType).HasMaxLength(100);
        builder.Property(x => x.SourcePlateNumber).HasMaxLength(100);
        builder.Property(x => x.NormalizedSourcePlateNumber).HasMaxLength(100);
        builder.HasOne<FuelCard>().WithMany().HasForeignKey(x => x.FuelCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RiderProfile>().WithMany().HasForeignKey(x => x.RiderProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FuelCardImport>().WithMany().HasForeignKey(x => x.LastImportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FuelCardId, x.ReportMonth }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.ReportMonth, x.RiderProfileId });
        builder.HasIndex(x => new { x.ReportMonth, x.EmployeeId });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FuelCardMonthlyUsages_MonthStart", "DAY([ReportMonth]) = 1");
            table.HasCheckConstraint("CK_FuelCardMonthlyUsages_Amounts", "[TotalLiters] >= 0 AND [TotalAmount] >= 0 AND ([AmountBeforeTax] IS NULL OR [AmountBeforeTax] >= 0) AND ([VatAmount] IS NULL OR [VatAmount] >= 0)");
            table.HasCheckConstraint("CK_FuelCardMonthlyUsages_TransactionCount", "[TransactionCount] IS NULL OR [TransactionCount] >= 0");
        });
    }
}

internal sealed class FuelCardImportConfiguration : IEntityTypeConfiguration<FuelCardImport>
{
    public void Configure(EntityTypeBuilder<FuelCardImport> builder)
    {
        builder.ConfigureHistory("FuelCardImports");
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Sha256Checksum).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.RowErrorsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.ReportMonth, x.Provider });
        builder.HasIndex(x => x.Sha256Checksum);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FuelCardImports_Provider", "[Provider] BETWEEN 1 AND 2");
            table.HasCheckConstraint("CK_FuelCardImports_MonthStart", "DAY([ReportMonth]) = 1");
            table.HasCheckConstraint("CK_FuelCardImports_Counts", "[SourceRows] >= 0 AND [CardRows] >= 0 AND [CreatedCards] >= 0 AND [CreatedMonthlyRecords] >= 0 AND [UpdatedMonthlyRecords] >= 0 AND [UnassignedCards] >= 0 AND [InvalidRows] >= 0");
        });
    }
}
