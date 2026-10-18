using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task AllRidersReportTranslatesRosterUnionAndBoundedMetricsForSqlServer()
    {
        var recorder = new ChefzReportSqlRecorder();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=diagnostic;Trusted_Connection=True")
            .AddInterceptors(new ChefzReportNoConnection(), recorder).Options);
        var service = new ChefzService(db, new CurrentUser(), new Checker(), new Clock(),
            new PlatformCredentialProtector(() => new byte[32]));
        var report = Value(await service.GetAllRidersReportAsync(2026, 10, 2, 2, TestContext.Current.CancellationToken));
        Assert.Empty(report.Riders);
        Assert.Equal(4, recorder.Commands.Count);
        var roster = Assert.Single(recorder.Commands, c => c.Contains("UNION", StringComparison.Ordinal));
        Assert.Contains("[EffectiveFrom] <=", roster);
        Assert.Contains("[EffectiveTo] >=", roster);
        Assert.Contains("[ReportDate] >=", roster);
        Assert.Contains("[ReportDate] <=", roster);
        Assert.Contains("[IsDeleted]", roster);
        Assert.Contains("CHEFZ", roster);
        var metrics = recorder.Commands[^1];
        Assert.Contains("[AcceptedOffers]", metrics);
        Assert.Contains("[TotalValidOffers]", metrics);
        Assert.Contains("[ReportDate] >=", metrics);
        Assert.Contains("[ReportDate] <=", metrics);
        Assert.DoesNotContain("[ReportIdNumber]", metrics);
    }

    private sealed class ChefzReportNoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class ChefzReportSqlRecorder : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            var table = new DataTable();
            if (command.CommandText.Contains("[PerformanceSettings]", StringComparison.Ordinal))
            {
                // Match the actual SQL projection order; provide a settings row without a database connection.
                var settings = new ChefzPerformanceSettings();
                var projection = command.CommandText.Split("FROM", 2, StringSplitOptions.None)[0];
                var values = new List<object>();
                foreach (Match match in Regex.Matches(projection, @"\[[^\]]+\]\.\[(?<name>[^\]]+)\]"))
                {
                    var name = match.Groups["name"].Value;
                    var property = typeof(ChefzPerformanceSettings).GetProperty(name)!;
                    table.Columns.Add(name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                    values.Add(property.GetValue(settings) ?? DBNull.Value);
                }
                table.Rows.Add(values.ToArray());
            }
            else if (command.CommandText.Contains("[ClientPlatforms]", StringComparison.Ordinal)
                && !command.CommandText.Contains("[PlatformRiderAccounts]", StringComparison.Ordinal))
            {
                table.Columns.Add("Id", typeof(Guid)); table.Rows.Add(Guid.NewGuid());
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}
