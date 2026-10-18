using ClosedXML.Excel;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    private const string SecondChefzReportId = "2522777944";

    private static async Task<(PlatformRiderAccount Account, Guid AssignmentId)> AddSecondAccountForReports(
        Fixture f, string firstType, string secondType, CancellationToken ct)
    {
        var rider = await f.Db.RiderProfiles.SingleAsync(r => r.Id == f.Second.RiderProfileId, ct);
        (await f.Db.Employees.SingleAsync(e => e.Id == rider.EmployeeId, ct)).Status = EmployeeStatus.Active;
        f.Account.Status = PlatformRiderAccountStatus.Assigned;
        var sponsor = await f.Db.Sponsors.FirstAsync(ct);
        f.Account.SponsorId = sponsor.Id;
        var owner = new Employee { FullNameAr = "صاحب الحساب الثاني", IqamaNo = SecondChefzReportId };
        var ownerRider = new RiderProfile { EmployeeId = owner.Id };
        var account = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, RegisteredEmployeeId = owner.Id,
            SponsorId = sponsor.Id, OperatingCityId = f.Account.OperatingCityId, Code = "CHEFZ-REPORT-2", ExternalAccountId = SecondChefzReportId };
        f.Db.AddRange(owner, ownerRider, account);
        await f.Db.SaveChangesAsync(ct);
        var simple = new SimplePlatformService(f.Db, new CurrentUser(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var assignment = Value(await simple.AssignAccountAsync(account.Id, new(rider.Id, new(2026, 10, 20), "Second Chefz account", false, null), ct));
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new(firstType), ct));
        Value(await f.Service.SaveAccountAsync(account.Id, new(secondType, SecondChefzReportId), ct));
        return (account, assignment.Id);
    }

    private static byte[] DualAccountDailySheet(int secondAccepted)
    {
        using var input = new MemoryStream(Sheet("2522777933", "50%", SecondChefzReportId, total: 4, accepted: 2, missed: 2));
        using var book = new XLWorkbook(input);
        var sheet = book.Worksheet(1);
        sheet.Cell(3, 6).Value = 6;
        sheet.Cell(3, 7).Value = secondAccepted;
        sheet.Cell(3, 9).Value = 6 - secondAccepted;
        sheet.Cell(3, 10).Value = secondAccepted == 4 ? "66.67%" : "50%";
        using var output = new MemoryStream(); book.SaveAs(output); return output.ToArray();
    }

    private static byte[] DualAccountMoneySheet(decimal secondDelivery = 200)
    {
        using var input = new MemoryStream(OrderSheet((new(2026, 10, 20), 100, 10, 2, 3, 4),
            (new(2026, 10, 20), secondDelivery, 20, 4, 6, 8)));
        using var book = new XLWorkbook(input);
        book.Worksheet(1).Cell(3, 2).Value = SecondChefzReportId;
        using var output = new MemoryStream(); book.SaveAs(output); return output.ToArray();
    }

    [Fact]
    public async Task TwoAccountDailyUploadsExposeEachAccountAndCombineRiderWithoutOverwritingOrDoubleCounting()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, "FullTime", "Freelancer", ct);
        var date = new DateOnly(2026, 10, 20);
        var imported = Value(await f.Service.ImportPerformanceAsync(date, "daily.xlsx", DualAccountDailySheet(4), ct));
        Assert.Empty(imported.Issues);
        Assert.Equal(2, imported.ImportedRows);
        var rider = Assert.Single(imported.Riders);
        Assert.Equal(f.Second.RiderProfileId, rider.RiderProfileId);
        Assert.Equal("المندوب الثاني", rider.RiderName);
        Assert.Equal(2, rider.Accounts.Count);
        Assert.Equal(2, Assert.Single(rider.Accounts, a => a.AccountId == f.Account.Id).AcceptedOffers);
        var accountB = Assert.Single(rider.Accounts, a => a.AccountId == second.Account.Id);
        Assert.Equal(4, accountB.AcceptedOffers);
        Assert.Equal(SecondChefzReportId, accountB.ExternalAccountId);
        Assert.Equal(second.AssignmentId, accountB.AssignmentId);
        var report = Value(await f.Service.GetRiderReportAsync(null, rider.RiderProfileId, 2026, 10, 2, 1, ct));
        Assert.Equal(6, report.SelectedSummary.Orders);
        Assert.Equal(60m, report.SelectedSummary.AcceptanceRate);
        Assert.Equal(1, report.SelectedSummary.DaysWithData);
        Assert.Equal(2, report.Accounts.Count);

        var correctedBytes = DualAccountDailySheet(3);
        var corrected = Value(await f.Service.ImportPerformanceAsync(date, "corrected.xlsx", correctedBytes, ct));
        Assert.Empty(corrected.Issues);
        Assert.Equal(1, corrected.ImportedRows);
        Assert.Equal(1, corrected.SkippedRows);
        Assert.Equal(2, Assert.Single(corrected.Riders).Accounts.Count);
        Assert.Equal(accountB.Id, Assert.Single(corrected.Riders[0].Accounts, a => a.AccountId == second.Account.Id).Id);
        var replay = Value(await f.Service.ImportPerformanceAsync(date, "renamed.xlsx", correctedBytes, ct));
        Assert.Equal(0, replay.ImportedRows);
        Assert.Equal(2, replay.SkippedRows);
        Assert.Equal(2, Assert.Single(replay.Riders).Accounts.Count);
        Assert.Equal(2, await f.Db.ChefzDailyPerformance.CountAsync(ct));

        report = Value(await f.Service.GetRiderReportAsync(null, rider.RiderProfileId, 2026, 10, 2, 1, ct));
        Assert.Equal(5, report.SelectedSummary.Orders);
        Assert.Equal(50m, report.SelectedSummary.AcceptanceRate);
        Assert.Equal(5, Assert.Single(report.Daily).Orders);
        Assert.Equal(2, Assert.Single(report.Accounts, a => a.AccountId == f.Account.Id).Summary.Orders);
        var breakdown = Assert.Single(report.Accounts, a => a.AccountId == second.Account.Id);
        Assert.Equal(3, breakdown.Summary.Orders);
        Assert.Equal(SecondChefzReportId, Assert.Single(breakdown.Daily).ReportIdNumber);
        Assert.All(report.Accounts, a => Assert.Equal(rider.RiderProfileId, a.RiderProfileId));
        var accountOnly = Value(await f.Service.GetRiderReportAsync(f.Account.Id, rider.RiderProfileId, 2026, 10, 2, 1, ct));
        Assert.Single(accountOnly.Accounts);
        Assert.Equal(2, accountOnly.SelectedSummary.Orders);
        var raw = Value(await f.Service.GetPerformanceAsync(null, rider.RiderProfileId, date, date, 1, 50, ct));
        Assert.Equal(2, raw.Items.Count);
        Assert.All(raw.Items, p => Assert.Equal("المندوب الثاني", p.RiderName));
        var management = Assert.Single(Value(await f.Service.GetManagementReportAsync(new(2026, 10, 21), f.Account.SponsorId, ct)));
        Assert.Equal(5, management.CycleOrders);
        Assert.Equal(1, management.RidersWithOrdersYesterday);
        Assert.Equal(1, management.RidersWithMoreThanThreeOrdersYesterday);
    }

    [Theory]
    [InlineData("Freelancer", "Freelancer", 14.69, 29.39)]
    [InlineData("FullTime", "FullTime", 400, 300)]
    [InlineData("FullTime", "Freelancer", 400, 29.39)]
    [InlineData("Freelancer", "FullTime", 14.69, 300)]
    public async Task TwoAccountMoneyUploadKeepsEachAccountAndItsFeeWithOneRiderPayout(string firstType, string secondType,
        double firstCompany, double secondCompany)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, firstType, secondType, ct);
        var bytes = DualAccountMoneySheet();
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("money.xlsx", bytes, true, ct));
        Assert.Empty(preview.Issues);
        Assert.True(preview.CanSave);
        Assert.Empty(f.Db.ChefzWalletSettlements);
        var rider = Assert.Single(preview.Riders);
        Assert.Equal(f.Second.RiderProfileId, rider.RiderProfileId);
        Assert.Equal("المندوب الثاني", rider.RiderName);
        Assert.Equal(2, rider.Accounts.Count);
        var a = Assert.Single(rider.Accounts, x => x.AccountId == f.Account.Id);
        var b = Assert.Single(rider.Accounts, x => x.AccountId == second.Account.Id);
        Assert.Equal("2522777933", a.DriverId);
        Assert.Equal(SecondChefzReportId, b.DriverId);
        Assert.Equal(f.Second.Id, a.AssignmentId);
        Assert.Equal(second.AssignmentId, b.AssignmentId);
        Assert.Equal(16, a.AssignedDays);
        Assert.Equal(12, b.AssignedDays);
        Assert.Equal(97.96m, a.NetEarnings);
        Assert.Equal(195.91m, b.NetEarnings);
        Assert.Equal((decimal)firstCompany, a.CompanyAmount);
        Assert.Equal((decimal)secondCompany, b.CompanyAmount);
        Assert.Equal(4m, a.Deductions);
        Assert.Equal(8m, b.Deductions);
        Assert.Equal(293.87m, rider.NetEarnings);
        Assert.Equal(39.13m, rider.VatAmount);
        Assert.Equal(12m, rider.Deductions);
        Assert.Equal(a.CompanyAmount + b.CompanyAmount, rider.CompanyAmount);
        Assert.Equal(a.RiderAmount + b.RiderAmount, rider.RiderAmount);
        Assert.All(rider.Accounts, x => Assert.Equal(x.NetEarnings, x.CompanyAmount + x.RiderAmount));
        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("money.xlsx", bytes, false, ct));
        Assert.Empty(saved.Issues);
        Assert.NotNull(saved.ImportId);
        Assert.Equal(preview.RiderAmount, saved.RiderAmount);
        Assert.Equal(2, Assert.Single(saved.Riders).Accounts.Count);
        var replay = Value(await f.Service.ProcessOrderSettlementsAsync("renamed.xlsx", bytes, false, ct));
        Assert.True(replay.AlreadyImported);
        Assert.Equal(saved.ImportId, replay.ImportId);
        Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
        Assert.Equal(1, await f.Db.ChefzOrderSettlementImports.CountAsync(ct));
        var stored = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        Assert.Equal(saved.CompanyAmount, stored.CompanyAmount);
        Assert.Equal(2, Assert.Single(stored.Riders).Accounts.Count);
        var conflict = Value(await f.Service.ProcessOrderSettlementsAsync("changed.xlsx", DualAccountMoneySheet(201), false, ct));
        Assert.NotEmpty(conflict.Issues);
        Assert.Null(conflict.ImportId);
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
        Assert.Equal(2, Value(await f.Service.GetSettlementsAsync(null, rider.RiderProfileId, 1, 50, ct)).Items.Count);
        Assert.Single(Value(await f.Service.GetSettlementsAsync(second.Account.Id, rider.RiderProfileId, 1, 50, ct)).Items);
    }

    [Theory]
    [InlineData("FullTime", 400, 300)]
    [InlineData("Freelancer", 15, 30)]
    public async Task TwoAccountWalletUpdatesSaveSeparatelyForTheSameRider(string type, int firstFee, int secondFee)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, type, type, ct);
        var firstRequest = new ChefzSettlementRequest(f.Second.Id, 2026, 10, 2, 0, 100);
        var secondRequest = new ChefzSettlementRequest(second.AssignmentId, 2026, 10, 2, 0, 200);
        var a = Value(await f.Service.SaveSettlementAsync(firstRequest, ct));
        var b = Value(await f.Service.SaveSettlementAsync(secondRequest, ct));
        Assert.NotEqual(a.Id, b.Id);
        Assert.NotEqual(a.AccountId, b.AccountId);
        Assert.Equal(a.RiderProfileId, b.RiderProfileId);
        Assert.Equal(firstFee, a.CompanyAmount);
        Assert.Equal(secondFee, b.CompanyAmount);
        Assert.Equal(a.Id, Value(await f.Service.SaveSettlementAsync(firstRequest, ct)).Id);
        Assert.Equal(b.Id, Value(await f.Service.SaveSettlementAsync(secondRequest, ct)).Id);
        Assert.True((await f.Service.SaveSettlementAsync(firstRequest with { EndWalletAmount = 101 }, ct)).IsFailure);
        Assert.Equal(2, Value(await f.Service.GetSettlementsAsync(null, a.RiderProfileId, 1, 50, ct)).Items.Count);
    }
}
