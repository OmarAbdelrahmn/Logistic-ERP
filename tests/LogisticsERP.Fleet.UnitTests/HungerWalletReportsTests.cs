using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class HungerFinanceTests
{
    [Fact]
    public async Task HungerWalletReportPreservesSignedObservationAndUnknownMissingBalances()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var unknown = Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items);
        Assert.Null(unknown.MobileBalance); Assert.Null(unknown.CurrentPeriodBalance); Assert.Equal("None", unknown.ReceiptState);
        Value(await f.Service.ReviewWalletAsync(f.Account.Id, Delivery(f) with { MobileBalance = -100 }, ct));
        var row = Assert.Single(Value(await f.Service.WalletsAsync(new() { BalanceThrough = -1 }, ct: ct)).Items);
        Assert.Equal(-100, row.MobileBalance); Assert.Equal(-100, row.CurrentPeriodBalance); Assert.Equal(-100, row.AttributedBalance);
        Assert.Equal(new DateOnly(2026, 11, 1), row.ObservedOn); Assert.Equal("wallet snapshot", row.Evidence);
        Assert.Empty(Value(await f.Service.WalletsAsync(new() { BalanceFrom = 0 }, ct: ct)).Items);
        var history = Value(await f.Service.WalletHistoryAsync(f.Account.Id, ct: ct)); Assert.Single(history.Observations); Assert.Empty(history.Events);
        f.Permissions.Allowed = [PermissionKeys.Hunger.Read];
        Assert.True((await f.Service.WalletsAsync(new(), ct: ct)).IsFailure);
        Assert.True((await f.Service.WalletHistoryAsync(f.Account.Id, ct: ct)).IsFailure);
        Assert.True((await f.Service.ExportWalletsAsync(new(), ct: ct)).IsFailure);
    }

    [Fact]
    public async Task HungerWalletReceiptStateAndHistoryFollowPartialConfirmAndReversal()
    {
        await using var f = await Fixture.Create(intercept: true); var ct = TestContext.Current.CancellationToken;
        var source = await Collected(f); var batch = Value(await f.Service.SubmitRemittanceAsync(Batch(f, source.EventId), ct));
        var row = Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items);
        Assert.Equal("Pending", row.ReceiptState); Assert.Equal(100, row.PendingReceipts); Assert.Equal(0, row.ConfirmedReceipts);
        var request = new HungerRemittanceConfirmation(f.Bank.Id, [new(source.EventId, 60)], "partial", new(2026, 11, 2), "receipt");
        var confirm = Value(await f.Service.ConfirmRemittanceAsync(batch.EventId, request, ct));
        row = Assert.Single(Value(await f.Service.WalletsAsync(new() { ReceiptState = "PartiallyConfirmed" }, ct: ct)).Items);
        Assert.Equal(40, row.PendingReceipts); Assert.Equal(60, row.ConfirmedReceipts);
        var history = Value(await f.Service.WalletHistoryAsync(f.Account.Id, ct: ct));
        Assert.Contains(history.Events, e => e.Id == batch.EventId); Assert.Contains(history.Events, e => e.Id == confirm.EventId);
        Value(await f.Service.ReverseRemittanceConfirmationAsync(confirm.EventId, Action("reverse"), ct));
        row = Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items);
        Assert.Equal("Pending", row.ReceiptState); Assert.Equal(100, row.PendingReceipts); Assert.Equal(0, row.ConfirmedReceipts);
        Assert.Contains(Value(await f.Service.WalletHistoryAsync(f.Account.Id, ct: ct)).Events, e => e.Kind == "RemittanceReversal");
    }

    [Fact]
    public async Task HungerWalletWithdrawalArrivalIsDistinctFromSupervisorRemittance()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var request = await Withdrawal(f, "supervisor-withdraw");
        // The configured wallet subaccount is also a valid supervisor destination.
        var config = await f.Db.Set<HungerFinanceConfiguration>().SingleAsync(ct);
        (await f.Db.Set<TreasurySubAccount>().SingleAsync(a => a.Id == config.CollectionsSubAccountId, ct)).Location = TreasuryLocation.Supervisor;
        await f.Db.SaveChangesAsync(ct);
        var withdrawal = Value(await f.Service.PostMoneyAsync(request with { Kind = "WithdrawalSupervisor", DestinationType = "Supervisor",
            DestinationOwnerId = f.Supervisor.Id, CounterSubAccountId = config.CollectionsSubAccountId }, ct));
        var row = Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items);
        Assert.Equal(40, row.PendingArrival); Assert.Equal(0, row.ConfirmedReceipts);
        Value(await f.Service.ConfirmWithdrawalArrivalAsync(withdrawal.EventId, new("arrival", new(2026, 11, 2), "evidence"), ct));
        row = Assert.Single(Value(await f.Service.WalletsAsync(new(), ct: ct)).Items);
        Assert.Equal(0, row.PendingArrival); Assert.Equal(40, row.PendingReceipts); Assert.Equal(0, row.ConfirmedReceipts);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    public async Task HungerWalletExportIncludesAllFilteredAccountsAndNoCredentials(string format)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        for (var i = 0; i < 105; i++) f.Db.Add(new PlatformRiderAccount { ClientPlatformId = f.Platform.Id, ExternalAccountId = "export" + i, Code = "E" + i });
        f.Db.Add(new PlatformRiderAccount { ClientPlatformId = Guid.NewGuid(), ExternalAccountId = "export-forbidden", Code = "forbidden" });
        await f.Db.SaveChangesAsync(ct);
        var filter = new HungerWalletFilter { Accounts = new() { ExternalAccountId = "export" } };
        var page = Value(await f.Service.WalletsAsync(filter, 2, 10, ct)); Assert.Equal(105, page.TotalCount); Assert.Equal(10, page.Items.Count);
        var file = Value(await f.Service.ExportWalletsAsync(filter, format, ct));
        if (format == "xlsx")
        {
            using var book = new XLWorkbook(new MemoryStream(file.Content));
            Assert.Equal(106, book.Worksheet(1).RowsUsed().Count());
            Assert.DoesNotContain(book.Worksheet(1).Row(1).CellsUsed(), c => c.GetString().Contains("password", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var text = System.Text.Encoding.UTF8.GetString(file.Content); Assert.Equal(106, text.Trim().Split('\n').Length);
            Assert.DoesNotContain("export-forbidden", text); Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
