using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace LogisticsERP.Fleet.UnitTests;

// SQL Server supplies the audit sequence and rowversion; emulate those for SQLite transaction tests.
internal sealed class HungerWalletSqliteGeneratedValues : SaveChangesInterceptor
{
    private long sequence;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Metadata.FindProperty("RowVersion") is not null) entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
            if (entry.Entity is LogisticsERP.Domain.Entities.System.AuditEntry audit && entry.State == EntityState.Added) audit.Sequence = ++sequence;
        }
        return ValueTask.FromResult(result);
    }
}

public sealed class HungerWalletTestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        foreach (var sequence in modelBuilder.Model.GetSequences().ToArray())
            modelBuilder.Model.RemoveSequence(sequence.Name, sequence.Schema);
        var tables = modelBuilder.Model.GetEntityTypes().Select(x => (Entity: x, Table: x.GetTableName(), Schema: x.GetSchema())).ToArray();
        foreach (var (entity, table, schema) in tables)
        {
            if (table is not null) { entity.SetTableName((schema ?? "app") + "_" + table); entity.SetSchema(null); }
            foreach (var constraint in entity.GetCheckConstraints().ToArray()) entity.RemoveCheckConstraint(constraint.Name!);
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnType(null); property.SetCollation(null);
                property.SetDefaultValueSql(null);
                if (entity.ClrType == typeof(LogisticsERP.Domain.Entities.System.AuditEntry) && property.Name == "Sequence")
                    property.ValueGenerated = ValueGenerated.Never;
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                    property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero)));
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(decimal))
                    property.SetValueConverter(new ValueConverter<decimal, double>(x => (double)x, x => (decimal)x));
                if (property.Name == "RowVersion")
                { property.IsNullable = true; property.SetDefaultValue(Array.Empty<byte>()); property.ValueGenerated = ValueGenerated.Never; property.SetBeforeSaveBehavior(PropertySaveBehavior.Save); property.SetAfterSaveBehavior(PropertySaveBehavior.Save); }
            }
        }
    }
}

