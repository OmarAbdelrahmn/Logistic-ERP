using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Identity.Configurations;

internal static class IdentityConfigurationExtensions
{
    public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : IdentityAuditableEntity
    {
        builder.ToTable(table, "identity");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.DeletionReason).HasMaxLength(500);
        builder.HasIndex(entity => entity.IsDeleted);
    }
}
