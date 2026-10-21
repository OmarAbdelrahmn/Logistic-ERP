using System.Text.Json;
using LogisticsERP.Application.Features.Chefz;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    private const string FirstRiderIqama = "0123456789";
    private const string SecondRiderIqama = "2987654321";

    private static async Task SetReportRiderIqamas(Fixture f, CancellationToken ct)
    {
        var first = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.First.RiderProfileId, ct);
        var second = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.Second.RiderProfileId, ct);
        (await f.Db.Employees.SingleAsync(e => e.Id == first.EmployeeId, ct)).IqamaNo = FirstRiderIqama;
        (await f.Db.Employees.SingleAsync(e => e.Id == second.EmployeeId, ct)).IqamaNo = SecondRiderIqama;
        await f.Db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task AccountListReturnsCurrentWorkerIqamaAndPreservesLeadingZerosAfterHandover()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        await SetReportRiderIqamas(f, ct);

        var response = Value(await f.Service.GetAccountsAsync(1, 50, ct));
        Assert.Equal(SecondRiderIqama, Assert.Single(response.Items).RiderIqamaNo);

        f.Second.EffectiveTo = new(2026, 10, 31);
        f.First.EffectiveTo = null;
        await f.Db.SaveChangesAsync(ct);
        response = Value(await f.Service.GetAccountsAsync(1, 50, ct));
        Assert.Equal(FirstRiderIqama, Assert.Single(response.Items).RiderIqamaNo);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonSerializerOptions.Web));
        Assert.Equal(FirstRiderIqama, json.RootElement.GetProperty("items")[0].GetProperty("riderIqamaNo").GetString());
    }

    [Theory]
    [InlineData("ended")]
    [InlineData("deleted")]
    [InlineData("missing-rider")]
    [InlineData("missing-iqama")]
    public async Task AccountListReturnsNullWithoutCurrentWorkerIqama(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        if (scenario != "missing-iqama") await SetReportRiderIqamas(f, ct);
        if (scenario == "ended") f.Second.EffectiveTo = new(2026, 10, 31);
        if (scenario == "deleted") f.Second.IsDeleted = true;
        if (scenario == "missing-rider") f.Second.RiderProfileId = Guid.NewGuid();
        await f.Db.SaveChangesAsync(ct);

        var response = Value(await f.Service.GetAccountsAsync(1, 50, ct));
        Assert.Null(Assert.Single(response.Items).RiderIqamaNo);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task PerformanceReportsReturnActualAndHistoricalRiderIqamasInsteadOfAccountOwnerIdentity()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        await SetReportRiderIqamas(f, ct);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        foreach (var day in new[] { 15, 16 })
        {
            var imported = Value(await f.Service.ImportPerformanceAsync(new(2026, 10, day), "daily.xlsx",
                Sheet("2522777933", "66.67%"), ct));
            var rider = Assert.Single(imported.Riders);
            Assert.Equal(day == 15 ? FirstRiderIqama : SecondRiderIqama, rider.RiderIqamaNo);
            Assert.Equal(rider.RiderIqamaNo, Assert.Single(rider.Accounts).RiderIqamaNo);
        }
        var report = Value(await f.Service.GetRiderReportAsync(f.Account.Id, null, 2026, 10, 2, 2, ct));
        Assert.Null(report.RiderIqamaNo); // One account has two workers in this period.
        Assert.Equal(2, report.Accounts.Count);
        foreach (var account in report.Accounts)
        {
            var expected = account.RiderProfileId == f.First.RiderProfileId ? FirstRiderIqama : SecondRiderIqama;
            Assert.Equal(expected, account.RiderIqamaNo);
            Assert.All(account.Daily, d => Assert.Equal(expected, d.RiderIqamaNo));
        }
        var raw = Value(await f.Service.GetPerformanceAsync(null, f.First.RiderProfileId, new(2026, 10, 1), new(2026, 10, 31), 1, 50, ct));
        Assert.Equal(FirstRiderIqama, Assert.Single(raw.Items).RiderIqamaNo);
        var filtered = Value(await f.Service.GetRiderReportAsync(null, f.First.RiderProfileId, 2026, 10, 2, 2, ct));
        Assert.Equal(FirstRiderIqama, filtered.RiderIqamaNo);
        Assert.Equal("المندوب الأول", filtered.RiderName);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(filtered, JsonSerializerOptions.Web));
        Assert.Equal(FirstRiderIqama, json.RootElement.GetProperty("riderIqamaNo").GetString());
    }

    [Fact]
    public async Task ReportsReturnRiderIdentityWithoutPerformanceAndNullForMissingIqama()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        await SetReportRiderIqamas(f, ct);
        var missing = AddBulkReportRider(f, "Missing iqama");
        await f.Db.SaveChangesAsync(ct);
        var all = Value(await f.Service.GetAllRidersReportAsync(2026, 10, 2, 2, ct));
        Assert.Equal(FirstRiderIqama, Assert.Single(all.Riders, r => r.RiderProfileId == f.First.RiderProfileId).RiderIqamaNo);
        Assert.Equal(SecondRiderIqama, Assert.Single(all.Riders, r => r.RiderProfileId == f.Second.RiderProfileId).RiderIqamaNo);
        Assert.Null(Assert.Single(all.Riders, r => r.RiderProfileId == missing.Assignment.RiderProfileId).RiderIqamaNo);
        var empty = Value(await f.Service.GetRiderReportAsync(null, f.First.RiderProfileId, 2026, 10, 2, 1, ct));
        Assert.Empty(empty.Accounts);
        Assert.Equal(FirstRiderIqama, empty.RiderIqamaNo);
        var noIqama = Value(await f.Service.GetRiderReportAsync(null, missing.Assignment.RiderProfileId, 2026, 10, 2, 1, ct));
        Assert.Null(noIqama.RiderIqamaNo);
    }

    [Fact]
    public async Task WalletSettlementPreviewSaveReplayAndListReturnAssignedRiderIdentity()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        await SetReportRiderIqamas(f, ct);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var request = new ChefzSettlementRequest(f.First.Id, 2026, 10, 1, 0, 100);
        var preview = Value(await f.Service.PreviewSettlementAsync(request, ct));
        var saved = Value(await f.Service.SaveSettlementAsync(request, ct));
        var replay = Value(await f.Service.SaveSettlementAsync(request, ct));
        var listed = Assert.Single(Value(await f.Service.GetSettlementsAsync(null, null, 1, 50, ct)).Items);
        foreach (var response in new[] { preview, saved, replay, listed })
        {
            Assert.Equal(f.First.RiderProfileId, response.RiderProfileId);
            Assert.Equal(FirstRiderIqama, response.RiderIqamaNo);
            Assert.Equal("المندوب الأول", response.RiderName);
        }
    }

    [Fact]
    public async Task TwoAccountOrderSettlementPreviewImportReplayAndReadReturnWorkerIqamaOnBothLevels()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        await SetReportRiderIqamas(f, ct);
        await AddSecondAccountForReports(f, "Freelancer", "Freelancer", ct);
        var bytes = DualAccountMoneySheet();
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("money.xlsx", bytes, true, ct));
        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("money.xlsx", bytes, false, ct));
        var replay = Value(await f.Service.ProcessOrderSettlementsAsync("money.xlsx", bytes, false, ct));
        var stored = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        foreach (var response in new[] { preview, saved, replay, stored })
        {
            var rider = Assert.Single(response.Riders);
            Assert.Equal(SecondRiderIqama, rider.RiderIqamaNo);
            Assert.Equal(2, rider.Accounts.Count);
            Assert.All(rider.Accounts, a => Assert.Equal(SecondRiderIqama, a.RiderIqamaNo));
        }
        var settlements = Value(await f.Service.GetSettlementsAsync(null, f.Second.RiderProfileId, 1, 50, ct));
        Assert.Equal(2, settlements.Items.Count);
        Assert.All(settlements.Items, s => Assert.Equal(SecondRiderIqama, s.RiderIqamaNo));
    }
}
