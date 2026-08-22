using LogisticsERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureAuditable<TEntity>(this EntityTypeBuilder<TEntity> builder, string table, string schema)
        where TEntity : AuditableEntity
    {
        builder.ToTable(table, schema);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.DeletionReason).HasMaxLength(500);
        builder.HasIndex(entity => entity.IsDeleted);
    }

    public static void ConfigureOperational<TEntity>(this EntityTypeBuilder<TEntity> builder, string table, string schema = "app")
        where TEntity : AuditableEntity
    {
        builder.ConfigureAuditable(table, schema);
    }

    public static void ConfigureHistory<TEntity>(this EntityTypeBuilder<TEntity> builder, string table, string schema = "app")
        where TEntity : HistoryEntity
    {
        builder.ToTable(table, schema);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAtUtc).IsRequired();
    }

    public static void ConfigureAddress<TOwner>(this OwnedNavigationBuilder<TOwner, Address> builder, string prefix)
        where TOwner : class
    {
        builder.Property(address => address.BuildingNumber).HasColumnName($"{prefix}BuildingNumber").HasMaxLength(32);
        builder.Property(address => address.Street).HasColumnName($"{prefix}Street").HasMaxLength(200);
        builder.Property(address => address.District).HasColumnName($"{prefix}District").HasMaxLength(200);
        builder.Property(address => address.City).HasColumnName($"{prefix}City").HasMaxLength(200);
        builder.Property(address => address.PostalCode).HasColumnName($"{prefix}PostalCode").HasMaxLength(32);
        builder.Property(address => address.AdditionalNumber).HasColumnName($"{prefix}AdditionalNumber").HasMaxLength(32);
    }
}
