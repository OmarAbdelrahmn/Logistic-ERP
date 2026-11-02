using LogisticsERP.Domain.Entities.Chefz;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task DefaultConversionKeepsConversionDayInFirstTypeAndRejectsFutureOrDuplicateDates()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        var account = await ConfigureTimelineAccount(f);
        Assert.True((await f.Service.SaveAccountAsync(f.Account.Id,
            new("FullTime", RowVersion: account.RowVersion, AccountTypeChangeDate: new(2026, 11, 1)), ct)).IsFailure);
        account = Value(await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime", RowVersion: account.RowVersion), ct));
        Assert.True((await f.Service.SaveAccountAsync(f.Account.Id,
            new("Freelancer", RowVersion: account.RowVersion), ct)).IsFailure);
        var cycle = Value(await f.Service.PreviewSettlementAsync(new(f.Second.Id, 2026, 10, 2, 0, 1000), ct));
        Assert.Equal(150m, cycle.CompanyAmount);
        Assert.Equal("Freelancer", cycle.AccountType);
        var history = Assert.Single(Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).AccountTypeHistory);
        Assert.Equal(new DateOnly(2026, 10, 31), history.ChangeDate);
        Assert.Equal(new DateOnly(2026, 11, 1), history.EffectiveFrom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypeChangeSplitsOrdersAndPreservesHistory(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var account = await ConfigureTimelineAccount(f);
        account = Value(await f.Service.SaveAccountAsync(f.Account.Id,
            new("FullTime", RowVersion: account.RowVersion, AccountTypeChangeDate: new(2026, 10, 7)), ct));
        var sheet = OrderSheet((new(2026, 10, 7), 1150, 10, 0, 0, 0),
            (new(2026, 10, 8), 2300, 0, 0, 0, 0));
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("mixed.xlsx", sheet, true, ct));
        Assert.Empty(preview.Issues);
        var row = Assert.Single(Assert.Single(preview.Riders).Accounts);
        Assert.Equal(8, row.FullTimeDays);
        Assert.Equal(1000m, row.FreelancerCommissionBase);
        Assert.Equal(350m, row.CompanyAmount);
        Assert.Equal(2660m, row.RiderAmount);
        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("mixed.xlsx", sheet, false, ct));
        Assert.Empty(saved.Issues);
        var reread = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        Assert.Equal(350m, reread.CompanyAmount);
        Assert.Equal(8, Assert.Single(Assert.Single(reread.Riders).Accounts).FullTimeDays);
        var history = Assert.Single(Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).AccountTypeHistory);
        Assert.Equal(new DateOnly(2026, 10, 7), history.ChangeDate);
        Assert.Equal(new DateOnly(2026, 10, 8), history.EffectiveFrom);
    }

    [Fact]
    public async Task MixedWalletRequiresActualBoundaryBalancesAndSavesSnapshot()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        var account = await ConfigureTimelineAccount(f);
        Value(await f.Service.SaveAccountAsync(f.Account.Id,
            new("FullTime", RowVersion: account.RowVersion, AccountTypeChangeDate: new(2026, 10, 7)), ct));
        Assert.True((await f.Service.PreviewSettlementAsync(new(f.First.Id, 2026, 10, 1, 100, 3100), ct)).IsFailure);
        var request = new LogisticsERP.Application.Features.Chefz.ChefzSettlementRequest(f.First.Id, 2026, 10, 1,
            100, 3100, new Dictionary<DateOnly, decimal> { [new(2026, 10, 8)] = 1100 });
        var preview = Value(await f.Service.PreviewSettlementAsync(request, ct));
        Assert.Equal(350m, preview.CompanyAmount);
        Assert.Equal(2650m, preview.RiderAmount);
        var saved = Value(await f.Service.SaveSettlementAsync(request, ct));
        Assert.Equal(preview.CompanyAmount, saved.CompanyAmount);
        Assert.Equal(8, saved.FullTimeDays);
        Assert.Equal(1000m, saved.FreelancerCommissionBase);
        Assert.Single(await f.Db.Set<ChefzAccountTypeChange>().ToArrayAsync(ct));
        var current = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.True((await f.Service.SaveAccountAsync(f.Account.Id,
            new("Freelancer", RowVersion: current.RowVersion, AccountTypeChangeDate: new(2026, 10, 10)), ct)).IsFailure);
    }

    [Fact]
    public async Task RepeatedChangesKeepEarlierCyclesAndChargeOnlyFullTimeDays()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        var account = await ConfigureTimelineAccount(f);
        account = Value(await f.Service.SaveAccountAsync(f.Account.Id,
            new("FullTime", RowVersion: account.RowVersion, AccountTypeChangeDate: new(2026, 10, 19)), ct));
        Value(await f.Service.SaveAccountAsync(f.Account.Id,
            new("Freelancer", RowVersion: account.RowVersion, AccountTypeChangeDate: new(2026, 10, 24)), ct));
        var old = Value(await f.Service.PreviewSettlementAsync(new(f.First.Id, 2026, 10, 1, 0, 1000), ct));
        Assert.Equal(150m, old.CompanyAmount);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("changes.xlsx",
            OrderSheet((new(2026, 10, 16), 1150, 0, 0, 0, 0),
                (new(2026, 10, 20), 2300, 0, 0, 0, 0), (new(2026, 10, 25), 1150, 0, 0, 0, 0)), false, ct));
        Assert.Empty(result.Issues);
        Assert.Equal(425m, result.CompanyAmount);
        var saved = Assert.Single(await f.Db.ChefzWalletSettlements.ToArrayAsync(ct));
        Assert.Equal(5, saved.FullTimeDays);
        Assert.Equal(2000m, saved.FreelancerCommissionBase);
    }

    private static async Task<LogisticsERP.Application.Features.Chefz.ChefzAccountResponse> ConfigureTimelineAccount(Fixture f)
    {
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        if (!f.Db.Database.IsSqlServer())
        {
            Assert.Single(f.Db.ChefzAccountDetails).RowVersion = [1];
            await f.Db.SaveChangesAsync(ct);
        }
        return Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
    }
}


