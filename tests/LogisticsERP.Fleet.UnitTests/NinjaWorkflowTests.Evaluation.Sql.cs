using System.Data.Common;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Infrastructure.Ninja;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task SqlServerEvaluationFailureRollsBackAllDatesAndConcurrentContextWritesRejectStaleRevision()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NINJA_SQL_TEST_SERVER") is not null,
            "An isolated SQL Server is required; never use the hosted application database.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(true); await SeptemberAssignment(f);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(f.Connection!)
            .AddInterceptors(new ApplicationPersistenceInterceptor(f.User, f.Clock), new RejectProvenanceWrite()).Options;
        await using (var failing = new ApplicationDbContext(options, f.Clock))
        {
            var imports = new NinjaPerformanceImportService(failing, f.User, f.Checker, f.Clock, EvaluationConfig);
            var csv = PerformanceCsv(("272578", new(2026, 9, 1), 15, "8"), ("272578", new(2026, 9, 2), 15, "8"));
            var preview = Value(await imports.PreviewAsync("performance.csv", csv, new("MultiDay"), ct));
            Assert.Equal("ninja.conflict", (await imports.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct)).Error.Code);
        }
        Assert.Equal(0, await f.Db.NinjaDailyPerformance.CountAsync(ct));
        Assert.Equal(0, await f.Db.NinjaDailyImports.CountAsync(ct));
        Assert.Equal(0, await f.Db.NinjaPerformanceArtifacts.CountAsync(ct));
        await using var first = f.OtherContext(); await using var second = f.OtherContext();
        var one = new NinjaEvaluationService(first, f.User, f.Checker, f.Clock, EvaluationConfig);
        var two = new NinjaEvaluationService(second, f.User, f.Checker, f.Clock, EvaluationConfig);
        var request = new NinjaEvaluationContextRequest(null, new(2026, 9, 1), [], "Concurrent reviewed context");
        var writes = await Task.WhenAll(one.SetContextAsync(f.Account.Id, request, ct), two.SetContextAsync(f.Account.Id, request, ct));
        Assert.Single(writes, r => r.IsSuccess); Assert.Single(writes, r => r.IsFailure);
        Assert.Equal(1, await f.Db.NinjaEvaluationContexts.CountAsync(ct));
    }
    private sealed class RejectProvenanceWrite : DbCommandInterceptor
    {
        private static void Check(DbCommand command)
        {
            if (command.CommandText.Contains("[ninja].[AllocationProvenance]", StringComparison.Ordinal)
                && (command.CommandText.Contains("INSERT", StringComparison.Ordinal) || command.CommandText.Contains("MERGE", StringComparison.Ordinal)))
                throw new DbUpdateException("Injected failure after dependent source staging.");
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Check(command); return ValueTask.FromResult(result);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Check(command); return ValueTask.FromResult(result);
        }
    }
}
