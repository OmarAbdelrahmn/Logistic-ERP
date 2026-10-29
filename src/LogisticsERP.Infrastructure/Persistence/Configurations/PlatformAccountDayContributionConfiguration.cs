using LogisticsERP.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LogisticsERP.Infrastructure.Persistence.Configurations;
internal sealed class PlatformAccountDayContributionConfiguration : IEntityTypeConfiguration<PlatformAccountDayContribution>
{
    public void Configure(EntityTypeBuilder<PlatformAccountDayContribution> b)
    {
        b.ConfigureAuditable("PlatformAccountDayContributions", "app");
        b.HasOne<PlatformRiderAccount>().WithMany().HasForeignKey(x => x.PlatformRiderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RiderClientAssignment>().WithMany().HasForeignKey(x => x.RepresentativeAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PlatformRiderAccountId, x.BusinessDate, x.RepresentativeAssignmentId }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.AssignmentIdsJson).IsRequired();
    }
}
