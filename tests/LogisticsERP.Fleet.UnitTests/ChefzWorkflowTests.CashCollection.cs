using ClosedXML.Excel;
using LogisticsERP.Infrastructure.Chefz;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData("Freelancer", 115, 20, 25, 135, 15, 120)]
    [InlineData("FullTime", 115, 20, 25, 135, 375, -240)]
    [InlineData("Freelancer", 115, -20, 25, 135, 15, 120)]
    [InlineData("Freelancer", 115, 0, 5, 155, 15, 140)]
    [InlineData("Freelancer", 115, 200, 205, -45, 15, -60)]
    [InlineData("FullTime", 115, 200, 205, -45, 375, -420)]
    [InlineData("Freelancer", 0, 20, 25, 35, 0, 35)]
    public async Task OrderCashReducesRiderPayoutWithoutChangingFeeBasedCommissionAndSavesExactlyOnce(string type,
        decimal fees, decimal cash, decimal deductions, decimal net, decimal company, decimal riderAmount)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new(type), ct));
        var bytes = WithCashColumn(OrderSheet((new(2026, 10, 1), fees, 10, 20, 30, -5)), cash);
        var parsed = ChefzOrderSpreadsheetParser.Parse(bytes, ct);
        Assert.Empty(parsed.Issues);
        Assert.Equal(deductions, Assert.Single(parsed.Rows).Deductions);
        Assert.Equal(Math.Abs(cash), Assert.Single(parsed.Rows).CashCollected);

        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("cash.xlsx", bytes, true, ct));
        Assert.Empty(preview.Issues);
        Assert.True(preview.CanSave);
        var row = Assert.Single(preview.Riders);
        Assert.Equal(f.First.RiderProfileId, row.RiderProfileId);
        Assert.Equal(fees == 0 ? 0 : 15, row.VatAmount);
        Assert.Equal(fees == 0 ? 0 : 100, row.DeliveryFeesAfterVat);
        Assert.Equal(10, row.Tips);
        Assert.Equal(20, row.Compensation);
        Assert.Equal(30, row.Bonuses);
        Assert.Equal(deductions, row.Deductions);
        Assert.Equal(net, row.NetEarnings);
        Assert.Equal(company, row.CompanyAmount);
        Assert.Equal(riderAmount, row.RiderAmount);
        Assert.Equal(deductions, Assert.Single(row.Accounts).Deductions);
        Assert.Empty(f.Db.ChefzWalletSettlements);

        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("cash.xlsx", bytes, false, ct));
        Assert.NotNull(saved.ImportId);
        Assert.Equal(net, saved.NetEarnings);
        Assert.Equal(company, saved.CompanyAmount);
        Assert.Equal(riderAmount, saved.RiderAmount);
        var read = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        Assert.Equal(deductions, Assert.Single(read.Riders).Deductions);
        Assert.Equal(riderAmount, read.RiderAmount);
        var replay = Value(await f.Service.ProcessOrderSettlementsAsync("renamed.xlsx", bytes, false, ct));
        Assert.True(replay.AlreadyImported);
        Assert.Equal(saved.ImportId, replay.ImportId);
        Assert.Equal(riderAmount, replay.RiderAmount);
        var stored = await f.Db.ChefzWalletSettlements.SingleAsync(ct);
        Assert.Equal(deductions, stored.Deductions);
        Assert.Equal(net, stored.GrossEarnings);
        Assert.Equal(company, stored.CompanyAmount);
        Assert.Equal(riderAmount, stored.RiderAmount);
    }

    [Theory]
    [InlineData("Freelancer", "Freelancer", 13.04, 26.09)]
    [InlineData("FullTime", "FullTime", 400, 300)]
    [InlineData("FullTime", "Freelancer", 400, 26.09)]
    [InlineData("Freelancer", "FullTime", 13.04, 300)]
    public async Task CollectedCashIsDeductedPerAccountAndCombinedForTheActualRider(string firstType, string secondType,
        decimal firstCompany, decimal secondCompany)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, firstType, secondType, ct);
        using var input = new MemoryStream(WithCashColumn(DualAccountMoneySheet(), 10));
        using var book = new XLWorkbook(input);
        book.Worksheet(1).Cell(3, 9).Value = 20;
        using var output = new MemoryStream();
        book.SaveAs(output);
        var bytes = output.ToArray();
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("two-cash-accounts.xlsx", bytes, true, ct));
        Assert.Empty(preview.Issues);
        var row = Assert.Single(preview.Riders);
        Assert.Equal(f.Second.RiderProfileId, row.RiderProfileId);
        var first = Assert.Single(row.Accounts, a => a.AccountId == f.Account.Id);
        var other = Assert.Single(row.Accounts, a => a.AccountId == second.Account.Id);
        Assert.Equal(14, first.Deductions);
        Assert.Equal(28, other.Deductions);
        Assert.Equal(42, row.Deductions);
        Assert.Equal(87.96m, first.NetEarnings);
        Assert.Equal(175.91m, other.NetEarnings);
        Assert.Equal(263.87m, row.NetEarnings);
        Assert.Equal(39.13m, row.VatAmount);
        Assert.Equal(firstCompany, first.CompanyAmount);
        Assert.Equal(secondCompany, other.CompanyAmount);
        Assert.Equal(firstCompany + secondCompany, row.CompanyAmount);
        Assert.Equal(row.NetEarnings - row.CompanyAmount, row.RiderAmount);
        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("two-cash-accounts.xlsx", bytes, false, ct));
        Assert.NotNull(saved.ImportId);
        var stored = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        Assert.Equal(42, Assert.Single(stored.Riders).Deductions);
        Assert.Equal(row.RiderAmount, stored.RiderAmount);
    }

    [Theory]
    [InlineData("blank")]
    [InlineData("formula")]
    [InlineData("text")]
    [InlineData("precision")]
    [InlineData("range")]
    [InlineData("duplicate")]
    public async Task CashColumnUsesExistingFinancialValidationAndInvalidBatchSavesNothing(string variant)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        using var input = new MemoryStream(WithCashColumn(OrderSheet((new(2026, 10, 1), 115, 0, 0, 0, 5)), 20));
        using var book = new XLWorkbook(input);
        var sheet = book.Worksheet(1);
        var cell = sheet.Cell(2, 9);
        switch (variant)
        {
            case "blank": cell.Clear(); break;
            case "formula": cell.FormulaA1 = "10+10"; break;
            case "text": cell.Value = "invalid"; break;
            case "precision": cell.Value = 20.123; break;
            case "range": cell.Value = 1000000001; break;
            case "duplicate": sheet.Cell(1, 10).Value = "Cash Collected By Driver"; break;
        }
        using var output = new MemoryStream();
        book.SaveAs(output);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("cash-validation.xlsx", output.ToArray(), false, ct));
        if (variant == "blank")
        {
            Assert.Empty(result.Issues);
            Assert.Equal(5, Assert.Single(result.Riders).Deductions);
            Assert.Equal(95, result.NetEarnings);
        }
        else
        {
            Assert.NotEmpty(result.Issues);
            Assert.Null(result.ImportId);
            Assert.False(result.CanSave);
            Assert.Empty(result.Riders);
            Assert.Empty(f.Db.ChefzWalletSettlements);
            Assert.Empty(f.Db.ChefzOrderSettlementImports);
            Assert.False(f.Db.ChangeTracker.HasChanges());
        }
    }

    private static byte[] WithCashColumn(byte[] source, decimal cash)
    {
        using var input = new MemoryStream(source);
        using var book = new XLWorkbook(input);
        var sheet = book.Worksheet(1);
        sheet.Cell(1, 9).Value = "Cash   Collected By Driver";
        sheet.Cell(2, 9).Value = cash;
        using var output = new MemoryStream();
        book.SaveAs(output);
        return output.ToArray();
    }
}
