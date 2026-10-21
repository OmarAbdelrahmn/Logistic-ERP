using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Migrations.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class PlatformAccountDayMigrationTests
{
    [Fact]
    public void MigrationBackfillsBeforeConstraintsAndProtectsNewEvidenceOnDowngrade()
    {
        var migration = new AddPlatformAccountDayAllocations();
        var operations = migration.UpOperations;
        Assert.DoesNotContain(operations, o => o is DropTableOperation or DropColumnOperation);
        var backfill = Assert.Single(operations.OfType<SqlOperation>());
        Assert.Contains("EXEC(N'", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("UPDATE ninja.NinjaDailyPerformance SET SourceId = Id", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("UPDATE ninja.NinjaDailyDeductions SET SourceId = Id", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("UPDATE chefz.DailyPerformance SET SourceId = Id", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("ELSE NULL END", backfill.Sql, StringComparison.Ordinal);
        Assert.True(operations.ToList().IndexOf(backfill) < operations.ToList().FindIndex(o => o is AddForeignKeyOperation));
        var guard = Assert.IsType<SqlOperation>(migration.DownOperations[0]);
        Assert.Contains("THROW 51000", guard.Sql, StringComparison.Ordinal);
        Assert.Contains("chefz.ImportedOrderRows", guard.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServerModelAndMigrationScriptIncludeSourcesAllocationKeysAndRowversions()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DayAllocationScriptOnly;Trusted_Connection=True").Options);
        var usage = db.Model.FindEntityType(typeof(PlatformAccountDayUsage))!;
        Assert.True(usage.FindProperty(nameof(PlatformAccountDayUsage.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(usage.GetIndexes(), i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(PlatformAccountDayUsage.Sequence))
            && i.GetFilter() == "[Sequence] IS NOT NULL AND [IsDeleted] = 0");
        foreach (var type in new[] { typeof(NinjaDailyPerformance), typeof(NinjaDailyDeduction), typeof(ChefzDailyPerformance) })
        {
            var entity = db.Model.FindEntityType(type)!;
            Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Count == 3);
            Assert.Contains(entity.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == "SourceId"));
        }
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.FindIndex(migrations, m => m.EndsWith("_AddPlatformAccountDayAllocations", StringComparison.Ordinal));
        Assert.True(index > 0);
        var script = db.GetService<IMigrator>().GenerateScript(migrations[index - 1], migrations[index], MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE [app].[PlatformAccountDayUsages]", script, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO ninja.NinjaDailyPerformanceSources", script, StringComparison.Ordinal);
        Assert.Contains("FK_NinjaDailyPerformance_NinjaDailyPerformanceSources_SourceId", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", script, StringComparison.OrdinalIgnoreCase);
    }
}
