using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task DeleteSettlementRequiresPermissionAndValidPeriod()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        f.Checker.Allow = false;
        Assert.True((await f.Service.DeleteSettlementsAsync(2026, 10, 1, ct)).IsFailure);
        f.Checker.Allow = true;
        Assert.True((await f.Service.DeleteSettlementsAsync(2026, 10, 3, ct)).IsFailure);
    }

    [Fact]
    public async Task DeletePeriodAllowsReimportAndRejectsMixedPeriodBatch()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        await using var f = await Fixture.Create(true);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var sheet = OrderSheet((new(2026, 10, 1), 100, 0, 0, 0, 0));
        var imported = Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, false, ct));
        var deleted = Value(await f.Service.DeleteSettlementsAsync(2026, 10, 1, ct));
        Assert.Equal(1, deleted.SettlementsDeleted); Assert.Equal(1, deleted.ImportsDeleted); Assert.Equal(1, deleted.OrderRowsDeleted);
        Assert.False((await f.Service.GetOrderSettlementImportAsync(imported.ImportId!.Value, ct)).IsSuccess);
        var reimport = Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, false, ct));
        Assert.False(reimport.AlreadyImported); Assert.NotNull(reimport.ImportId);
        Value(await f.Service.DeleteSettlementsAsync(2026, 10, 1, ct));
        var mixed = OrderSheet((new(2026, 10, 1), 100, 0, 0, 0, 0), (new(2026, 10, 16), 100, 0, 0, 0, 0));
        Value(await f.Service.ProcessOrderSettlementsAsync("mixed.xlsx", mixed, false, ct));
        Assert.True((await f.Service.DeleteSettlementsAsync(2026, 10, 1, ct)).IsFailure);
        Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
        Assert.Equal(2, await f.Db.ChefzImportedOrderRows.CountAsync(ct));
    }
}
