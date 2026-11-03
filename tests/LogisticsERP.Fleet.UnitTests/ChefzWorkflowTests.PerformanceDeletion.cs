using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task DeletePerformanceRequiresPermissionAndReportDate()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        f.Checker.DeniedPermission = LogisticsERP.Application.Authorization.PermissionKeys.Chefz.PerformanceDelete;
        var forbidden = await f.Service.DeletePerformanceAsync(new(2026, 10, 15), ct);
        Assert.True(forbidden.IsFailure);
        Assert.Equal("Chefz.Forbidden", forbidden.Error.Code);
        f.Checker.DeniedPermission = null;
        Assert.True((await f.Service.DeletePerformanceAsync(default, ct)).IsFailure);
    }

    [Fact]
    public async Task DeletePerformanceDayPreservesOtherDatesAndAllowsReimport()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        await using var f = await Fixture.Create(true);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var date = new DateOnly(2026, 10, 15);
        var sheet = Sheet("2522777933", "66.67%");
        Value(await f.Service.ImportPerformanceAsync(date, "driver.xlsx", sheet, ct));
        Value(await f.Service.ImportPerformanceAsync(date.AddDays(1), "driver.xlsx", sheet, ct));
        var deleted = Value(await f.Service.DeletePerformanceAsync(date, ct));
        Assert.Equal(1, deleted.PerformanceRowsDeleted);
        Assert.Equal(1, deleted.SourcesDeleted);
        Assert.Equal(1, deleted.ImportsDeleted);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(date.AddDays(1), (await f.Db.ChefzDailyPerformance.SingleAsync(ct)).ReportDate);
        var repeated = Value(await f.Service.DeletePerformanceAsync(date, ct));
        Assert.Equal(0, repeated.PerformanceRowsDeleted);
        var reimport = Value(await f.Service.ImportPerformanceAsync(date, "driver.xlsx", sheet, ct));
        Assert.Empty(reimport.Issues);
        Assert.Equal(1, reimport.ImportedRows);
        Assert.Equal(2, await f.Db.ChefzDailyPerformance.CountAsync(ct));
        Assert.Equal(2, await f.Db.AuditEntries.CountAsync(a => a.EntityType == "ChefzPerformanceDay", ct));
    }
}
