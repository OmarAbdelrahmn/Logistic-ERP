using System.Data;
using System.Data.Common;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task ReportQueriesCompileUnionAggregatesDistinctCoverageAndPagingForSqlServer()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = new NinjaSqlRecorder(Guid.NewGuid());
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=diagnostic;Trusted_Connection=True")
            .AddInterceptors(new NinjaNoConnection(), recorder).Options);
        var clock = new Clock(); var user = new CurrentUser(); var protector = new PlatformCredentialProtector(() => new byte[32]);
        var service = new NinjaService(db, user, new Checker(), clock, protector, new SimplePlatformService(db, user, clock, protector));
        var day = new DateOnly(2026, 10, 12);
        Value(await service.GetPerformanceReportAsync(day, day, null, null, ct));
        Value(await service.GetDailyReportAsync(day, day, null, null, 1, 50, ct));
        Value(await service.GetRiderReportAsync(day, day, null, null, 1, 50, ct));
        Value(await service.GetAccountsAsync(1, 50, ct));
        Value(await service.GetAccountSheetUploadsAsync(1, 50, ct));
        Assert.Contains("LEFT JOIN", service.AccountUpdateSystemQuery(recorder.PlatformId).ToQueryString());
        var aggregates = recorder.Commands.Where(c => c.Contains("UNION ALL", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, aggregates.Length);
        Assert.All(aggregates, sql => { Assert.Contains("GROUP BY", sql); Assert.Contains("COUNT(DISTINCT", sql); });
        Assert.Equal(2, aggregates.Count(sql => sql.Contains("OFFSET", StringComparison.Ordinal)));
        Assert.Contains(recorder.Commands, sql => sql.Contains("LEFT JOIN", StringComparison.Ordinal) && sql.Contains("OFFSET", StringComparison.Ordinal));
        Assert.Contains(recorder.Commands, sql => sql.Contains("NinjaAccountUpdateRows", StringComparison.Ordinal) && sql.Contains("GROUP BY", StringComparison.Ordinal));
    }

    private sealed class NinjaNoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class NinjaSqlRecorder(Guid platformId) : DbCommandInterceptor
    {
        public Guid PlatformId => platformId;
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            var table = new DataTable();
            if (command.CommandText.Contains("[ClientPlatforms]", StringComparison.Ordinal)
                && !command.CommandText.Contains("[PlatformRiderAccounts]", StringComparison.Ordinal))
            {
                table.Columns.Add("Id", typeof(Guid)); table.Rows.Add(platformId);
            }
            else if (command.CommandText.StartsWith("SELECT COUNT(*)", StringComparison.Ordinal)
                && !command.CommandText.Contains("GROUP BY", StringComparison.Ordinal))
            {
                table.Columns.Add("Value", typeof(int)); table.Rows.Add(0);
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}
