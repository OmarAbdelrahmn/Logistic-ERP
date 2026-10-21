using LogisticsERP.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal sealed class PlatformAccountDayUsageConfiguration : IEntityTypeConfiguration<PlatformAccountDayUsage>
{
    public void Configure(EntityTypeBuilder<PlatformAccountDayUsage> b)
    {
        b.ConfigureAuditable("PlatformAccountDayUsages", "app");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RiderClientAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate, x.RiderClientAssignmentId }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate, x.Sequence }).IsUnique().HasFilter("[Sequence] IS NOT NULL AND [IsDeleted] = 0");
        b.Property(x => x.WorkingHours).HasPrecision(18, 8);
        b.Property(x => x.CollectedAmount).HasPrecision(18, 2);
        b.Property(x => x.DeductionsAmount).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        b.ToTable(t => t.HasCheckConstraint("CK_PlatformAccountDayUsage_Values",
            "([Sequence] IS NULL OR [Sequence] > 0) AND ([OrdersCount] IS NULL OR [OrdersCount] >= 0) AND ([WorkingHours] IS NULL OR [WorkingHours] BETWEEN 0 AND 24) AND ([CollectedAmount] IS NULL OR [CollectedAmount] >= 0) AND ([TotalValidOffers] IS NULL OR [TotalValidOffers] >= 0) AND ([DeclinedOffers] IS NULL OR [DeclinedOffers] >= 0) AND ([MissedOffers] IS NULL OR [MissedOffers] >= 0)"));
    }
}
