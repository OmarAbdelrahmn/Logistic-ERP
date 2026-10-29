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
        var evaluation = new NinjaEvaluationService(db, user, new Checker(), clock, EvaluationConfig);
        Value(await evaluation.EvaluateAsync(new() { Year = 2026, Month = 10 }, ct));
        Assert.Contains("LEFT JOIN", service.AccountUpdateSystemQuery(recorder.PlatformId).ToQueryString());
        var aggregates = recorder.Commands.Where(c => c.Contains("UNION ALL", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, aggregates.Length);
        Assert.All(aggregates, sql => { Assert.Contains("GROUP BY", sql); Assert.Contains("COUNT(DISTINCT", sql); });
        Assert.Equal(2, aggregates.Count(sql => sql.Contains("OFFSET", StringComparison.Ordinal)));
        Assert.Equal(2, recorder.UngroupedRiderParameterTypes.Count);
        Assert.All(recorder.UngroupedRiderParameterTypes, type => Assert.Equal(DbType.Guid, type));
        Assert.All(aggregates, sql => Assert.DoesNotContain("'00000000-0000-0000-0000-000000000000' AS [RiderId0]", sql));
        Assert.Contains(recorder.Commands, sql => sql.Contains("LEFT JOIN", StringComparison.Ordinal) && sql.Contains("OFFSET", StringComparison.Ordinal));
        Assert.Contains(recorder.Commands, sql => sql.Contains("NinjaAccountUpdateRows", StringComparison.Ordinal) && sql.Contains("GROUP BY", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DailySponsorQueryCompilesGroupingAndLimitsBreakdownToPagedDatesForSqlServer()
    {
        var ct = TestContext.Current.CancellationToken;
        var recorder = new NinjaSqlRecorder(Guid.NewGuid(), returnDailyRow: true);
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=invalid;Database=diagnostic;Trusted_Connection=True")
            .AddInterceptors(new NinjaNoConnection(), recorder).Options);
        var clock = new Clock(); var user = new CurrentUser(); var protector = new PlatformCredentialProtector(() => new byte[32]);
        var service = new NinjaService(db, user, new Checker(), clock, protector, new SimplePlatformService(db, user, clock, protector));
        var day = new DateOnly(2026, 10, 12);
        var report = Value(await service.GetDailyReportAsync(day, day, null, null, 1, 50, ct));
        Assert.Equal(day, Assert.Single(report.Items).Date);
        var aggregates = recorder.Commands.Where(c => c.Contains("UNION ALL", StringComparison.Ordinal)).ToArray();
        var sql = Assert.Single(aggregates);
        Assert.Contains("GROUP BY", sql); Assert.Contains("[SponsorId]", sql);
        Assert.Contains("COUNT(DISTINCT", sql); Assert.Contains("SELECT DISTINCT", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Matches(@"WHERE\s+\[\w+\]\.\[Date\]\s+IN", sql);
        Assert.All(recorder.UngroupedRiderParameterTypes, type => Assert.Equal(DbType.Guid, type));
    }

    private sealed class NinjaNoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default) => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class NinjaSqlRecorder(Guid platformId, bool returnDailyRow = false) : DbCommandInterceptor
    {
        public Guid PlatformId => platformId;
        public List<string> Commands { get; } = [];
        public List<DbType> UngroupedRiderParameterTypes { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            foreach (DbParameter parameter in command.Parameters)
                if (parameter.ParameterName.Contains("ungroupedRiderId", StringComparison.Ordinal))
                    UngroupedRiderParameterTypes.Add(parameter.DbType);
            var table = new DataTable();
            if (returnDailyRow && command.CommandText.Contains("UNION ALL", StringComparison.Ordinal)
                && command.CommandText.Contains("OFFSET", StringComparison.Ordinal))
            {
                table.Columns.Add("Date", typeof(DateOnly)); table.Columns.Add("RiderId", typeof(Guid));
                table.Columns.Add("SponsorId", typeof(Guid));
                table.Columns.Add("PerformanceRecords", typeof(int)); table.Columns.Add("DeductionRecords", typeof(int));
                table.Columns.Add("Orders", typeof(long)); table.Columns.Add("Hours", typeof(decimal));
                table.Columns.Add("Collected", typeof(decimal)); table.Columns.Add("Deductions", typeof(decimal));
                table.Columns.Add("PerformanceDays", typeof(int)); table.Columns.Add("DeductionDays", typeof(int));
                table.Columns.Add("Accounts", typeof(int)); table.Columns.Add("Riders", typeof(int));
                table.Rows.Add(new DateOnly(2026, 10, 12), Guid.Empty, DBNull.Value, 1, 0, 10L, 5m, 0m, 0m, 1, 0, 1, 1);
            }
            else if (command.CommandText.Contains("[ClientPlatforms]", StringComparison.Ordinal)
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
