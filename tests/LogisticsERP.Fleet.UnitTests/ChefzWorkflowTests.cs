using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Chefz;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using LogisticsERP.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public void PerformanceThresholdsAndMissingDatesUseOnlyObservedData()
    {
        var s = new ChefzPerformanceSettings();
        Assert.Equal("A", ChefzPerformanceRules.Classify(101, 80, false, s));
        Assert.Equal("B", ChefzPerformanceRules.Classify(80, 70, false, s));
        Assert.Equal("D", ChefzPerformanceRules.Classify(39, 100, false, s));
        Assert.Equal("A", ChefzPerformanceRules.Classify(7, 80, true, s));
        Assert.Null(ChefzPerformanceRules.Acceptance(0, 0));
        Assert.Equal(66.67m, ChefzPerformanceRules.Acceptance(2, 3));
        Assert.True(ChefzPerformanceRules.NeedsChange([(new(2026, 10, 1), 0), (new(2026, 10, 2), 0), (new(2026, 10, 3), 0)], 2));
        Assert.False(ChefzPerformanceRules.NeedsChange([(new(2026, 10, 1), 0), (new(2026, 10, 3), 0), (new(2026, 10, 4), 0)], 2));
        Assert.False(ChefzPerformanceRules.NeedsChange([(new(2026, 10, 1), 0), (new(2026, 10, 2), 0), (new(2026, 10, 3), 0), (new(2026, 10, 4), 1)], 2));
    }

    [Fact]
    public async Task HostedReadOnlyReportsTranslateAndReadSeededWorkbook()
    {
        var connection = Environment.GetEnvironmentVariable("CHEFZ_HOSTED_READ_CONNECTION");
        Assert.SkipUnless(connection is not null, "Set CHEFZ_HOSTED_READ_CONNECTION for read-only hosted SQL report verification.");
        var ct = TestContext.Current.CancellationToken;
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        var service = new ChefzService(db, new CurrentUser(), new Checker(), new Clock(), new PlatformCredentialProtector(() => new byte[32]));
        var settings = Value(await service.GetSettingsAsync(ct));
        Assert.Equal(80, settings.Settings.OverallAOrders);
        var retained = await db.PlatformRiderAccounts.SingleAsync(a => a.ExternalAccountId == "2507652275"
            && db.ClientPlatforms.Any(p => p.Id == a.ClientPlatformId && p.Code == "SHIFTZ"), ct);
        Assert.Equal("Unconfigured", Value(await service.GetAccountAsync(retained.Id, ct)).AccountType);
        var listed = new List<Guid>();
        for (var page = 1; page <= 5; page++) listed.AddRange(Value(await service.GetAccountsAsync(page, 100, ct)).Items.Select(a => a.AccountId));
        Assert.Equal(465, listed.Distinct().Count());
        Assert.Contains(retained.Id, listed);
        var account = await (from a in db.PlatformRiderAccounts join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
            where p.Code == "SHIFTZ" select a).FirstAsync(ct);
        var report = Value(await service.GetRiderReportAsync(account.Id, null, 2026, 10, 1, 4, ct));
        Assert.Equal(4, report.Halves.Count);
        Assert.Equal(4, report.Messages.Count);
        Assert.Null(report.AllTimeSummary.Rating);
        Assert.Null(report.AllTimeSummary.AverageOrdersPerHalf);
        var stats = Value(await service.GetStatisticsAsync(1, 50, ct));
        Assert.Equal(3, stats.Items.Count);
        Assert.Equal(399, stats.Items.Sum(p => p.Values.SponsorshipCount));
        Assert.All(stats.Items, p => Assert.Null(p.Values.WorkingWithoutPerformance));
        Assert.All(Value(await service.GetManagementReportAsync(new(2026, 10, 11), null, ct)), p => Assert.Equal(0, p.CycleOrders));
        Assert.True((await service.GetRemindersAsync(1, 50, ct)).IsSuccess);
    }

    [Fact]
    public async Task ReportsSettingsStatisticsAndOwnerRemindersFollowWorkbook()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Assert.Equal("Unconfigured", Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).AccountType);
        Assert.Equal(f.Account.Id, Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items).AccountId);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "driver.xlsx", Sheet("2522777933", "66.67%"), ct));
        Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 16), "driver.xlsx", Sheet("2522777933", "66.67%"), ct));
        var report = Value(await f.Service.GetRiderReportAsync(f.Account.Id, null, 2026, 10, 2, 4, ct));
        Assert.Equal(4, report.Halves.Count);
        Assert.Equal(2, report.Messages.Count);
        Assert.Equal(2, report.SelectedSummary.HalvesWithData);
        Assert.Equal(2m, report.SelectedSummary.AverageOrdersPerHalf);
        Assert.Equal(4, report.AllTimeSummary.Orders);
        Assert.Equal(2, report.Daily.Count);
        var rider = Value(await f.Service.GetRiderReportAsync(f.Account.Id, f.First.RiderProfileId, 2026, 10, 2, 4, ct));
        Assert.Equal(1, rider.SelectedSummary.HalvesWithData);
        Assert.Equal(2, rider.SelectedSummary.Orders);
        Assert.True((await f.Service.GetRiderReportAsync(f.Account.Id, null, 1, 1, 1, 4, ct)).IsFailure);
        Assert.Single(f.Db.ChefzPerformanceSettings).RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        var settings = Value(await f.Service.GetSettingsAsync(ct));
        Assert.True((await f.Service.SaveSettingsAsync(settings.Settings with { DailyCOrders = 9 }, ct)).IsFailure);
        Assert.True((await f.Service.SaveSettingsAsync(settings.Settings with { RowVersion = "bad" }, ct)).IsFailure);
        Value(await f.Service.SaveSettingsAsync(settings.Settings with { OverallCOrders = 2, OverallBOrders = 3, OverallAOrders = 4 }, ct));
        Assert.Equal("C", Value(await f.Service.GetRiderReportAsync(f.Account.Id, null, 2026, 10, 2, 4, ct)).SelectedSummary.Rating);

        var sponsor = new Sponsor { RegistryNameAr = "اختبار", EmployerIdentityNumber = "7000000000" };
        f.Db.Sponsors.Add(sponsor); f.Account.SponsorId = sponsor.Id;
        await f.Db.SaveChangesAsync(ct);
        var values = new ChefzStatisticsRequest(199, 89, 0, 2, 1, 1, 0, 17, 2, 0, 0, 87, 9, null);
        var created = Value(await f.Service.CreateStatisticsAsync(sponsor.Id, values, ct));
        Assert.Single(f.Db.ChefzSponsorStatistics).RowVersion = [1];
        await f.Db.SaveChangesAsync(ct);
        created = Value(await f.Service.GetStatisticsAsync(sponsor.Id, ct));
        Assert.True((await f.Service.CreateStatisticsAsync(sponsor.Id, values, ct)).IsFailure);
        Assert.True((await f.Service.UpdateStatisticsAsync(sponsor.Id, values with { Working = -1 }, ct)).IsFailure);
        Value(await f.Service.UpdateStatisticsAsync(sponsor.Id, values with { Working = 90, RowVersion = created.RowVersion }, ct));
        Assert.Equal(90, Value(await f.Service.GetStatisticsAsync(sponsor.Id, ct)).Values.Working);
        var management = Assert.Single(Value(await f.Service.GetManagementReportAsync(new(2026, 10, 16), sponsor.Id, ct)));
        Assert.Equal(new DateOnly(2026, 10, 15), management.PerformanceDate);
        Assert.Equal(2, management.CycleOrders);
        Assert.Equal(1, management.RidersWithOrdersYesterday);
        Assert.Equal(1, management.RidersWithThreeOrFewerOrdersYesterday);
        Assert.Equal(1, management.AccountsBelowThreeOrdersInCycle);
        Assert.True((await f.Service.DeleteStatisticsAsync(sponsor.Id, created.RowVersion, ct)).IsSuccess);
        Assert.True((await f.Service.GetStatisticsAsync(sponsor.Id, ct)).IsFailure);
        Assert.True(Assert.Single(f.Db.ChefzSponsorStatistics.IgnoreQueryFilters()).IsDeleted);
        Value(await f.Service.CreateStatisticsAsync(sponsor.Id, values, ct));

        var owner = await f.Db.Employees.SingleAsync(e => e.Id == f.Account.RegisteredEmployeeId, ct);
        owner.EngagementType = EmployeeRelationshipType.OutsideRider;
        await f.Db.SaveChangesAsync(ct);
        Assert.Equal("OutsideSponsorship", Assert.Single(Value(await f.Service.GetRemindersAsync(1, 50, ct)).Items).Condition);
        owner.Status = EmployeeStatus.Terminated; await f.Db.SaveChangesAsync(ct);
        Assert.Equal("FinalExit", Assert.Single(Value(await f.Service.GetRemindersAsync(1, 50, ct)).Items).Condition);
        f.Checker.Allow = false;
        Assert.True((await f.Service.GetSettingsAsync(ct)).IsFailure);
        Assert.True((await f.Service.GetRiderReportAsync(f.Account.Id, null, 2026, 10, 2, 4, ct)).IsFailure);
        Assert.True((await f.Service.GetRemindersAsync(1, 50, ct)).IsFailure);
    }

    [Fact]
    public void FreelancerTwoHalvesAndWalletResetUseIndependentStartAndEnd()
    {
        var first = ChefzRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1, new(2026, 10, 1), null, 0, 500);
        var second = ChefzRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 2, new(2026, 10, 1), null, 500, 1000);
        var reset = ChefzRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 2, new(2026, 10, 1), null, 0, 500);
        Assert.All(new[] { first, second, reset }, s => { Assert.Equal(500m, s.GrossEarnings); Assert.Equal(75m, s.CompanyAmount); Assert.Equal(425m, s.RiderAmount); });
    }

    [Theory]
    [InlineData(2026, 2, 28, 13)]
    [InlineData(2028, 2, 29, 14)]
    [InlineData(2026, 4, 30, 15)]
    [InlineData(2026, 10, 31, 16)]
    public void SecondHalfUsesActualMonthEnd(int year, int month, int last, int days)
    {
        var result = ChefzRules.Calculate(ChefzAccountType.FullTime, year, month, 2, new(year, month, 1), null, 0, 1000);
        Assert.Equal(new DateOnly(year, month, last), result.PeriodTo);
        Assert.Equal(days, result.AssignedDays);
        Assert.Equal(days * 25m, result.CompanyAmount);
    }

    [Fact]
    public void FullTimeIncludesHandoverDayAndStopsAtReturnDay()
    {
        var full = ChefzRules.Calculate(ChefzAccountType.FullTime, 2026, 10, 1, new(2026, 10, 1), null, 0, 500);
        Assert.Equal(375m, full.CompanyAmount);
        Assert.Equal(125m, full.RiderAmount);
        var partial = ChefzRules.Calculate(ChefzAccountType.FullTime, 2026, 10, 1, new(2026, 10, 10), new(2026, 10, 12), 500, 550);
        Assert.Equal(3, partial.AssignedDays);
        Assert.Equal(75m, partial.CompanyAmount);
        Assert.Equal(-25m, partial.RiderAmount);
        Assert.Throws<ArgumentException>(() => ChefzRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1, new(2026, 10, 1), null, 500, 0));
    }

    [Fact]
    public void SpreadsheetReadsPercentageTextAndNumericPercentAndRejectsDuplicateIds()
    {
        var text = ChefzSpreadsheetParser.Parse(Sheet("2522777933", "66.67%"));
        Assert.Empty(text.Issues);
        Assert.Equal(66.67m, Assert.Single(text.Rows).AcceptanceRate);
        var numeric = ChefzSpreadsheetParser.Parse(Sheet("2522777933", "numeric"));
        Assert.Equal(66.67m, Assert.Single(numeric.Rows).AcceptanceRate);
        var duplicate = ChefzSpreadsheetParser.Parse(Sheet("2522777933", "66.67%", "2522777933"));
        Assert.Single(duplicate.Issues);
        Assert.NotEmpty(ChefzSpreadsheetParser.Parse([1, 2, 3]).Issues);
    }

    [Fact]
    public void SuppliedSampleParsesWhenProvided()
    {
        var path = Environment.GetEnvironmentVariable("CHEFZ_SAMPLE_PATH");
        Assert.SkipUnless(path is not null, "Set CHEFZ_SAMPLE_PATH to verify a supplied Chefz workbook.");
        var result = ChefzSpreadsheetParser.Parse(File.ReadAllBytes(path!));
        Assert.Empty(result.Issues);
        Assert.Equal(182, result.Rows.Count);
        var first = result.Rows[0];
        Assert.Equal("2522777933", first.IdNumber);
        Assert.Equal(3, first.TotalValidOffers);
        Assert.Equal(2, first.AcceptedOffers);
        Assert.Equal(0, first.DeclinedOffers);
        Assert.Equal(1, first.MissedOffers);
        Assert.Equal(66.67m, first.AcceptanceRate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountPasswordSettlementsAndDailyPerformanceKeepHistoricalRider(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        var sponsor = await f.Db.Sponsors.FirstAsync(ct);
        f.Account.SponsorId = sponsor.Id;
        var saved = Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        if (!sql)
        {
            // InMemory does not generate SQL Server rowversions.
            Assert.Single(f.Db.ChefzAccountDetails).RowVersion = [1];
            await f.Db.SaveChangesAsync(ct);
            saved = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        }
        Assert.Equal("2522777933", saved.ReportIdNumber); // Automatically uses the registered owner's ID.
        Assert.False(saved.HasPassword);
        Assert.True((await f.Service.SetPasswordAsync(f.Account.Id, new("Chefz pass !$", "initial password"), ct)).IsSuccess);
        Assert.Equal("Chefz pass !$", Value(await f.Service.GetPasswordAsync(f.Account.Id, ct)));
        var encrypted = Assert.Single(f.Db.PlatformAccountCredentialVersions);
        Assert.NotEqual("Chefz pass !$", System.Text.Encoding.UTF8.GetString(encrypted.Ciphertext));
        var request = new ChefzSettlementRequest(f.First.Id, 2026, 10, 1, 0, 500);
        var first = Value(await f.Service.SaveSettlementAsync(request, ct));
        Assert.Equal(425m, first.RiderAmount);
        Assert.Equal(sponsor.RegistryNameAr, first.SponsorNameAr);
        Assert.Equal(sponsor.RegistryNameEn, first.SponsorNameEn);
        Assert.Equal(first.Id, Value(await f.Service.SaveSettlementAsync(request, ct)).Id);
        Assert.True((await f.Service.SaveSettlementAsync(request with { EndWalletAmount = 600 }, ct)).IsFailure);
        var second = Value(await f.Service.SaveSettlementAsync(new(f.Second.Id, 2026, 10, 2, 500, 1000), ct));
        Assert.Equal(425m, second.RiderAmount);
        Assert.NotEqual(first.RiderProfileId, second.RiderProfileId);
        var updated = Value(await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime", null, saved.RowVersion), ct));
        Assert.Equal(25m, updated.DailyFee);
        Assert.Equal("Freelancer", Assert.Single(Value(await f.Service.GetSettlementsAsync(f.Account.Id, f.First.RiderProfileId, 1, 50, ct)).Items).AccountType);
        var preview = Value(await f.Service.PreviewSettlementAsync(new(f.First.Id, 2026, 10, 1, 0, 500), ct));
        Assert.Equal(375m, preview.CompanyAmount);
        Assert.Equal(125m, preview.RiderAmount);
        Assert.Equal(sponsor.RegistryNameAr, preview.SponsorNameAr);
        Assert.Equal(sponsor.RegistryNameEn, preview.SponsorNameEn);
        var settlements = Value(await f.Service.GetSettlementsAsync(f.Account.Id, null, 1, 50, ct));
        Assert.Equal(2, settlements.Items.Count);
        Assert.All(settlements.Items, s =>
        {
            Assert.Equal(sponsor.RegistryNameAr, s.SponsorNameAr);
            Assert.Equal(sponsor.RegistryNameEn, s.SponsorNameEn);
        });
        f.Account.SponsorId = null;
        await f.Db.SaveChangesAsync(ct);
        var withoutSponsor = Value(await f.Service.GetSettlementsAsync(f.Account.Id, null, 1, 50, ct));
        Assert.Equal(2, withoutSponsor.Items.Count);
        Assert.All(withoutSponsor.Items, s => { Assert.Null(s.SponsorNameAr); Assert.Null(s.SponsorNameEn); });

        var data = Sheet("2522777933", "66.67%");
        var day15 = Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "driver.xlsx", data, ct));
        Assert.Empty(day15.Issues);
        Assert.Equal(1, day15.ImportedRows);
        Assert.Equal(0, Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "driver.xlsx", data, ct)).ImportedRows);
        Assert.Equal(1, Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 16), "driver.xlsx", data, ct)).ImportedRows);
        var performance = Value(await f.Service.GetPerformanceAsync(f.Account.Id, null, new(2026, 10, 1), new(2026, 10, 31), 1, 50, ct));
        Assert.Equal(f.Second.RiderProfileId, performance.Items[0].RiderProfileId);
        Assert.Equal(f.First.RiderProfileId, performance.Items[1].RiderProfileId);
        Assert.All(performance.Items, p => { Assert.Equal(3, p.TotalValidOffers); Assert.Equal(2, p.AcceptedOffers); Assert.Equal(0, p.DeclinedOffers); Assert.Equal(1, p.MissedOffers); Assert.Equal(66.67m, p.AcceptanceRate); });
        var changed = Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "changed.xlsx", Sheet("2522777933", "50%"), ct));
        Assert.Empty(changed.Issues);
        Assert.Equal(1, changed.ImportedRows);
        Assert.Equal(66.67m, (await f.Db.ChefzDailyPerformance.SingleAsync(p => p.ReportDate == new DateOnly(2026, 10, 15), ct)).AcceptanceRate);
        Assert.Equal(50m, (await f.Db.ChefzDailyPerformanceSources.SingleAsync(p => p.ReportDate == new DateOnly(2026, 10, 15), ct)).AcceptanceRate);
        Assert.Equal(2, await f.Db.ChefzDailyPerformance.CountAsync(ct));
        if (sql)
        {
            f.Db.ChefzDailyPerformance.Add(new() { PlatformRiderAccountId = f.Account.Id, RiderClientAssignmentId = f.First.Id,
                RiderProfileId = f.First.RiderProfileId, ImportId = day15.ImportId!.Value, ReportDate = new(2026, 10, 15), ReportIdNumber = "2522777933" });
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync(ct));
            f.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task PerformanceReuploadCorrectsInPlaceAndReappliesPreviouslyImportedFile()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var date = new DateOnly(2026, 10, 15);
        var original = Sheet("2522777933", "66.67%");
        var corrected = Sheet("2522777933", "50%", total: 10, accepted: 5, declined: 2, missed: 3);
        var first = Value(await f.Service.ImportPerformanceAsync(date, "original.xlsx", original, ct));
        Value(await f.Service.ImportPerformanceAsync(date.AddDays(1), "original.xlsx", original, ct));
        var saved = await f.Db.ChefzDailyPerformance.SingleAsync(p => p.ReportDate == date, ct);
        var id = saved.Id;
        var createdAt = saved.CreatedAtUtc;
        var createdBy = saved.CreatedByUserId;
        f.Db.ChangeTracker.Clear();

        var update = Value(await f.Service.ImportPerformanceAsync(date, "corrected.xlsx", corrected, ct));
        Assert.Empty(update.Issues);
        Assert.Equal(1, update.ImportedRows);
        Assert.Equal(0, update.SkippedRows);
        f.Db.ChangeTracker.Clear();
        saved = await f.Db.ChefzDailyPerformance.SingleAsync(p => p.ReportDate == date, ct);
        Assert.Equal(id, saved.Id);
        Assert.Equal(createdAt, saved.CreatedAtUtc);
        Assert.Equal(createdBy, saved.CreatedByUserId);
        Assert.Equal(update.ImportId, saved.ImportId);
        Assert.Equal(f.First.Id, saved.RiderClientAssignmentId);
        Assert.Equal(f.First.RiderProfileId, saved.RiderProfileId);
        Assert.Equal(10, saved.TotalValidOffers);
        Assert.Equal(5, saved.AcceptedOffers);
        Assert.Equal(2, saved.DeclinedOffers);
        Assert.Equal(3, saved.MissedOffers);
        Assert.Equal(50m, saved.AcceptanceRate);
        Assert.Equal(2, await f.Db.ChefzDailyPerformance.CountAsync(ct));
        Assert.Equal(66.67m, (await f.Db.ChefzDailyPerformance.SingleAsync(p => p.ReportDate == date.AddDays(1), ct)).AcceptanceRate);
        Assert.Contains(await f.Db.AuditEntries.ToArrayAsync(ct), a => a.EntityId == id && a.Action == "Updated"
            && a.BeforeJson!.Contains("AcceptedOffers", StringComparison.Ordinal)
            && a.AfterJson!.Contains("AcceptedOffers", StringComparison.Ordinal));

        var retry = Value(await f.Service.ImportPerformanceAsync(date, "renamed.xlsx", corrected, ct));
        Assert.Equal(update.ImportId, retry.ImportId);
        Assert.Equal(0, retry.ImportedRows);
        Assert.Equal(1, retry.SkippedRows);
        var restore = Value(await f.Service.ImportPerformanceAsync(date, "original.xlsx", original, ct));
        Assert.Empty(restore.Issues);
        Assert.Equal(first.ImportId, restore.ImportId);
        Assert.Equal(1, restore.ImportedRows);
        Assert.Equal(66.67m, (await f.Db.ChefzDailyPerformance.SingleAsync(p => p.ReportDate == date, ct)).AcceptanceRate);
        Assert.Equal(3, await f.Db.ChefzPerformanceImports.CountAsync(ct));
    }

    [Fact]
    public async Task PerformanceUploadCanInsertUpdateAndSkipRowsTogether()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        foreach (var reportId in new[] { "second", "third" })
        {
            var account = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
                OperatingCityId = f.Account.OperatingCityId, Code = reportId, ExternalAccountId = reportId };
            f.Db.AddRange(account,
                new ChefzAccountDetails { PlatformRiderAccountId = account.Id, ReportIdNumber = reportId, AccountType = ChefzAccountType.Freelancer },
                new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = f.First.ClientContractId,
                    RiderProfileId = f.First.RiderProfileId, EffectiveFrom = f.First.EffectiveFrom,
                    EffectiveTo = f.First.EffectiveTo, Status = f.First.Status, AssignedByUserId = f.First.AssignedByUserId });
        }
        await f.Db.SaveChangesAsync(ct);
        var date = new DateOnly(2026, 10, 15);
        Value(await f.Service.ImportPerformanceAsync(date, "original.xlsx", Sheet("2522777933", "66.67%", "second"), ct));
        using var source = new MemoryStream(Sheet("2522777933", "50%", "second", 10, 5, 2, 3));
        using var workbook = new XLWorkbook(source);
        var sheet = workbook.Worksheet(1);
        sheet.Row(2).CopyTo(sheet.Row(4));
        sheet.Cell(4, 2).Value = "third";
        sheet.Cell(3, 6).Value = 3; sheet.Cell(3, 7).Value = 2;
        sheet.Cell(3, 8).Value = 0; sheet.Cell(3, 9).Value = 1; sheet.Cell(3, 10).Value = "66.67%";
        using var content = new MemoryStream();
        workbook.SaveAs(content);
        f.Db.ChangeTracker.Clear();

        var result = Value(await f.Service.ImportPerformanceAsync(date, "mixed.xlsx", content.ToArray(), ct));
        Assert.Empty(result.Issues);
        Assert.Equal(2, result.ImportedRows);
        Assert.Equal(1, result.SkippedRows);
        f.Db.ChangeTracker.Clear();
        var rows = await f.Db.ChefzDailyPerformance.ToDictionaryAsync(p => p.ReportIdNumber, ct);
        Assert.Equal(3, rows.Count);
        Assert.Equal(5, rows["2522777933"].AcceptedOffers);
        Assert.Equal(5, rows["third"].AcceptedOffers);
        Assert.Equal(2, rows["second"].AcceptedOffers);
        Assert.Equal(result.ImportId, rows["2522777933"].ImportId);
        Assert.Equal(result.ImportId, rows["third"].ImportId);
        Assert.NotEqual(result.ImportId, rows["second"].ImportId);
    }

    [Fact]
    public async Task InvalidReuploadLeavesExistingPerformanceAndTrackingUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var date = new DateOnly(2026, 10, 15);
        var first = Value(await f.Service.ImportPerformanceAsync(date, "original.xlsx", Sheet("2522777933", "66.67%"), ct));
        f.Db.ChangeTracker.Clear();
        var rejected = Value(await f.Service.ImportPerformanceAsync(date, "invalid.xlsx",
            Sheet("2522777933", "50%", "unknown", 10, 5, 2, 3), ct));
        Assert.NotEmpty(rejected.Issues);
        Assert.Null(rejected.ImportId);
        Assert.Equal(0, rejected.ImportedRows);
        // A later save in the same unit of work must not commit any abandoned correction.
        await f.Db.SaveChangesAsync(ct);
        f.Db.ChangeTracker.Clear();
        var saved = Assert.Single(await f.Db.ChefzDailyPerformance.ToArrayAsync(ct));
        Assert.Equal(first.ImportId, saved.ImportId);
        Assert.Equal(3, saved.TotalValidOffers);
        Assert.Equal(2, saved.AcceptedOffers);
        Assert.Equal(66.67m, saved.AcceptanceRate);
        Assert.Single(await f.Db.ChefzPerformanceImports.ToArrayAsync(ct));

        saved.ReportDate = date.AddDays(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
        f.Db.ChangeTracker.Clear();
        f.Db.ChefzPerformanceImports.Single().ImportedRows = 9;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task InvalidRowsAndMissingOrAmbiguousAssignmentsSaveNothing()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var invalid = Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "driver.xlsx", Sheet("2522777933", "66.67%", "unknown"), ct));
        Assert.NotEmpty(invalid.Issues);
        Assert.Empty(f.Db.ChefzDailyPerformance);
        Assert.Empty(f.Db.ChefzPerformanceImports);
        var noAssignment = Value(await f.Service.ImportPerformanceAsync(new(2026, 9, 15), "driver.xlsx", Sheet("2522777933", "66.67%"), ct));
        Assert.NotEmpty(noAssignment.Issues);
        f.First.EffectiveTo = new(2026, 10, 16);
        await f.Db.SaveChangesAsync(ct);
        var ambiguous = Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 16), "driver.xlsx", Sheet("2522777933", "66.67%"), ct));
        Assert.NotEmpty(ambiguous.Issues);
        Assert.Empty(f.Db.ChefzDailyPerformance);
    }

    [Fact]
    public async Task ExistingHostedShiftzPlatformUsesChefzAccountDetails()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        Assert.Single(f.Db.ClientPlatforms).Code = "SHIFTZ";
        await f.Db.SaveChangesAsync(ct);
        var saved = Value(await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime"), ct));
        Assert.Equal(25m, saved.DailyFee);
        Assert.Equal(f.Account.Id, Value(await f.Service.GetAccountAsync(f.Account.Id, ct)).AccountId);
    }

    [Fact]
    public async Task UnauthorizedAndNonChefzAccountsCannotBeChanged()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        f.Checker.Allow = false;
        Assert.True((await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime"), ct)).IsFailure);
        Assert.True((await f.Service.SetPasswordAsync(f.Account.Id, new("secret", "initial"), ct)).IsFailure);
        Assert.True((await f.Service.ImportPerformanceAsync(new(2026, 10, 15), "driver.xlsx", Sheet("2522777933", "66.67%"), ct)).IsFailure);
        Assert.Empty(f.Db.ChefzAccountDetails);
        Assert.Empty(f.Db.PlatformAccountCredentialVersions);
        f.Checker.Allow = true;
        Assert.True((await f.Service.SaveAccountAsync(Guid.NewGuid(), new("FullTime"), ct)).IsFailure);
        Assert.True((await f.Service.GetPerformanceAsync(null, null, new(2026, 10, 1), new(2026, 10, 31), 1, 101, ct)).IsFailure);
    }

    [Fact]
    public void OrderCalculationTaxesOnlySummedFeesAndKeepsNegativeBalances()
    {
        var freelancer = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
            new(2026, 10, 1), null, 3, 1000, 10, 20, 30, 60);
        Assert.Equal(130.43m, freelancer.VatAmount);
        Assert.Equal(869.57m, freelancer.GrossEarnings);
        Assert.Equal(130.44m, freelancer.CompanyAmount);
        Assert.Equal(739.13m, freelancer.RiderAmount);
        var fullTime = ChefzOrderSettlementRules.Calculate(ChefzAccountType.FullTime, 2028, 2, 2,
            new(2028, 2, 20), new(2028, 2, 29), 1, 100, 0, 0, 0, 200);
        Assert.Equal(10, fullTime.AssignedDays);
        Assert.Equal(-113.04m, fullTime.GrossEarnings);
        Assert.Equal(250m, fullTime.CompanyAmount);
        Assert.Equal(-363.04m, fullTime.RiderAmount);
        var cents = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
            new(2026, 10, 1), null, 2, .10m, 0, 0, 0, 0);
        Assert.Equal(.01m, cents.VatAmount); // Sum first, round retained fees once.
        Assert.Equal(.09m, cents.GrossEarnings);
        Assert.Equal(cents.GrossEarnings, cents.CompanyAmount + cents.RiderAmount);
    }

    [Fact]
    public void RiderCycleRoundingReconcilesAcrossAccountsAndAssignments()
    {
        var rider = Guid.NewGuid();
        var rows = Enumerable.Range(0, 3).Select(_ => {
            var s = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
                new(2026, 10, 1), null, 1, .05m, 0, 0, 0, 0);
            s.RiderProfileId = rider; s.PlatformRiderAccountId = Guid.NewGuid(); return s;
        }).ToArray();
        ChefzOrderSettlementRules.ReconcileRiderCycles(rows);
        Assert.Equal(.13m, rows.Sum(s => s.GrossEarnings));
        Assert.Equal(.02m, rows.Sum(s => s.CompanyAmount));
        Assert.All(rows, s => Assert.Equal(s.GrossEarnings, s.CompanyAmount + s.RiderAmount));
        var first = rows.Select(s => (s.VatAmount, s.CompanyAmount, s.RiderAmount)).ToArray();
        ChefzOrderSettlementRules.ReconcileRiderCycles(rows);
        Assert.Equal(first, rows.Select(s => (s.VatAmount, s.CompanyAmount, s.RiderAmount)).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BulkOrdersDeriveCyclesKeepAssignedRidersAndSaveExactlyOnce(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1", "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        await using var f = await Fixture.Create(sql);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var sheet = OrderSheet((new(2026, 10, 1), 100, 1, 2, 3, 6),
            (new(2026, 10, 15), 100, 0, 0, 0, 0), (new(2026, 10, 16), 200, 0, 0, 0, 10),
            (new(2026, 10, 31), 100, 0, 0, 0, 0));
        var preview = Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, true, ct));
        Assert.True(preview.CanSave); Assert.Null(preview.ImportId);
        Assert.Equal(2, preview.Riders.Count);
        Assert.Equal(f.First.RiderProfileId, preview.Riders[0].RiderProfileId);
        Assert.Equal("المندوب الأول", preview.Riders[0].RiderName);
        Assert.Equal(f.Second.RiderProfileId, preview.Riders[1].RiderProfileId);
        Assert.Equal(new DateOnly(2026, 10, 16), preview.Riders[1].PeriodFrom);
        Assert.Equal(new DateOnly(2026, 10, 31), preview.Riders[1].PeriodTo);
        Assert.Equal(26.09m, preview.Riders[0].VatAmount);
        Assert.Equal(173.91m, preview.Riders[0].NetEarnings);
        Assert.Equal(26.09m, preview.Riders[0].CompanyAmount);
        Assert.Equal(147.82m, preview.Riders[0].RiderAmount);
        Assert.Equal(250.87m, preview.Riders[1].NetEarnings);
        Assert.Empty(f.Db.ChefzWalletSettlements); Assert.Empty(f.Db.ChefzOrderSettlementImports);
        var saved = Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, false, ct));
        Assert.NotNull(saved.ImportId); Assert.False(saved.CanSave);
        Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
        Assert.Equal(1, await f.Db.ChefzOrderSettlementImports.CountAsync(ct));
        Assert.Equal(preview.NetEarnings, saved.NetEarnings);
        Assert.Equal(saved.NetEarnings, saved.CompanyAmount + saved.RiderAmount);
        var replay = Value(await f.Service.ProcessOrderSettlementsAsync("renamed.xlsx", sheet, false, ct));
        Assert.True(replay.AlreadyImported); Assert.Equal(saved.ImportId, replay.ImportId);
        Assert.Equal(saved.RiderAmount, replay.RiderAmount);
        var read = Value(await f.Service.GetOrderSettlementImportAsync(saved.ImportId!.Value, ct));
        Assert.Equal(saved.CompanyAmount, read.CompanyAmount);
        Assert.All(Value(await f.Service.GetSettlementsAsync(null, null, 1, 50, ct)).Items, s => {
            Assert.Equal("Orders", s.Source); Assert.Equal(saved.ImportId, s.ImportId);
            Assert.Equal(0, s.EndWalletAmount); });
        Assert.True((await f.Service.SaveSettlementAsync(new(f.First.Id, 2026, 10, 1, 0, 0), ct)).IsFailure);
        var changed = Value(await f.Service.ProcessOrderSettlementsAsync("changed.xlsx",
            OrderSheet((new(2026, 10, 1), 101, 0, 0, 0, 0)), false, ct));
        Assert.NotEmpty(changed.Issues); Assert.Null(changed.ImportId);
        await f.Db.SaveChangesAsync(ct); // No abandoned writes can leak into a later save.
        Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
        Assert.Equal(1, await f.Db.ChefzOrderSettlementImports.CountAsync(ct));
        if (sql)
        {
            // The database also enforces the invariant under concurrent imports.
            var conflictingBatch = new ChefzOrderSettlementImport { FileName = "conflicting.xlsx", ContentHash = new string('D', 64), SourceRows = 2 };
            var duplicate = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 10, 1,
                f.First.EffectiveFrom, f.First.EffectiveTo, 1, 100, 0, 0, 0, 0);
            duplicate.PlatformRiderAccountId = f.Account.Id; duplicate.RiderClientAssignmentId = f.First.Id;
            duplicate.RiderProfileId = f.First.RiderProfileId; duplicate.ImportId = conflictingBatch.Id;
            var unrelated = ChefzOrderSettlementRules.Calculate(ChefzAccountType.Freelancer, 2026, 11, 1,
                f.Second.EffectiveFrom, null, 1, 100, 0, 0, 0, 0);
            unrelated.PlatformRiderAccountId = f.Account.Id; unrelated.RiderClientAssignmentId = f.Second.Id;
            unrelated.RiderProfileId = f.Second.RiderProfileId; unrelated.ImportId = conflictingBatch.Id;
            f.Db.AddRange(conflictingBatch, duplicate, unrelated);
            await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync(ct));
            f.Db.ChangeTracker.Clear();
            Assert.Equal(2, await f.Db.ChefzWalletSettlements.CountAsync(ct));
            Assert.Equal(1, await f.Db.ChefzOrderSettlementImports.CountAsync(ct));
        }
    }

    [Fact]
    public async Task FullTimeChargesAssignedCalendarDaysAndSplitsWithinACycle()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("FullTime"), ct));
        f.First.EffectiveFrom = new(2026, 10, 3); f.First.EffectiveTo = new(2026, 10, 7);
        f.Second.EffectiveFrom = new(2026, 10, 8);
        await f.Db.SaveChangesAsync(ct);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx",
            OrderSheet((new(2026, 10, 3), 1000, 0, 0, 0, 0), (new(2026, 10, 7), 1000, 0, 0, 0, 0),
                (new(2026, 10, 8), 1000, 0, 0, 0, 0), (new(2026, 10, 15), 1000, 0, 0, 0, 0)), true, ct));
        Assert.Empty(result.Issues); Assert.Equal(2, result.Riders.Count);
        var first = result.Riders.Single(r => r.RiderProfileId == f.First.RiderProfileId);
        var second = result.Riders.Single(r => r.RiderProfileId == f.Second.RiderProfileId);
        Assert.Equal(125m, first.CompanyAmount); Assert.Equal(200m, second.CompanyAmount);
        Assert.Equal(5, Assert.Single(first.Accounts).AssignedDays);
        Assert.Equal(8, Assert.Single(second.Accounts).AssignedDays);
        // Overlap on days without source orders must not double-charge full-time days.
        f.First.EffectiveTo = new(2026, 10, 9); await f.Db.SaveChangesAsync(ct);
        var overlap = Value(await f.Service.ProcessOrderSettlementsAsync("overlap.xlsx",
            OrderSheet((new(2026, 10, 3), 100, 0, 0, 0, 0), (new(2026, 10, 10), 100, 0, 0, 0, 0)), false, ct));
        Assert.NotEmpty(overlap.Issues); Assert.Null(overlap.ImportId);
        Assert.Empty(f.Db.ChefzWalletSettlements);
    }

    [Fact]
    public async Task BulkOrdersRejectUnknownAccountsOverlapInvalidDataAndUnauthorizedAccess()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        var sheet = OrderSheet((new(2026, 10, 15), 100, 0, 0, 0, 0));
        Assert.NotEmpty(Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, false, ct)).Issues);
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        Assert.NotEmpty(Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx",
            OrderSheet((new(2026, 9, 30), 100, 0, 0, 0, 0)), false, ct)).Issues);
        f.First.EffectiveTo = new(2026, 10, 16);
        await f.Db.SaveChangesAsync(ct);
        Assert.NotEmpty(Value(await f.Service.ProcessOrderSettlementsAsync("orders.xlsx",
            OrderSheet((new(2026, 10, 15), 100, 0, 0, 0, 0), (new(2026, 10, 16), 100, 0, 0, 0, 0)), false, ct)).Issues);
        Assert.NotEmpty(Value(await f.Service.ProcessOrderSettlementsAsync("broken.xlsx", [1, 2, 3], false, ct)).Issues);
        Assert.NotEmpty(Value(await f.Service.ProcessOrderSettlementsAsync("future.xlsx",
            OrderSheet((new(2026, 11, 1), 100, 0, 0, 0, 0)), false, ct)).Issues);
        await f.Db.SaveChangesAsync(ct);
        Assert.Empty(f.Db.ChefzWalletSettlements); Assert.Empty(f.Db.ChefzOrderSettlementImports);
        f.Checker.Allow = false;
        Assert.True((await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, true, ct)).IsFailure);
        Assert.True((await f.Service.ProcessOrderSettlementsAsync("orders.xlsx", sheet, false, ct)).IsFailure);
        Assert.True((await f.Service.GetOrderSettlementImportAsync(Guid.NewGuid(), ct)).IsFailure);
    }

    [Fact]
    public async Task BulkOrdersCombineMultipleAccountsForOneActualRider()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        var other = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
            RegisteredEmployeeId = f.Account.RegisteredEmployeeId, OperatingCityId = f.Account.OperatingCityId,
            Code = "CHEFZ-2", ExternalAccountId = "platform-2" };
        var assignment = new RiderClientAssignment { PlatformRiderAccountId = other.Id,
            RiderProfileId = f.First.RiderProfileId, ClientContractId = f.First.ClientContractId,
            EffectiveFrom = new(2026, 10, 1), EffectiveTo = new(2026, 10, 15), Status = RiderAssignmentStatus.Ended };
        f.Db.AddRange(other, assignment);
        await f.Db.SaveChangesAsync(ct);
        Value(await f.Service.SaveAccountAsync(other.Id, new("FullTime", "123456"), ct));
        using var source = new MemoryStream(OrderSheet((new(2026, 10, 1), 100, 1, 2, 3, 0), (new(2026, 10, 2), 1000, 0, 0, 0, 0)));
        using var book = new XLWorkbook(source);
        book.Worksheet(1).Cell(3, 2).Value = "123456";
        using var content = new MemoryStream(); book.SaveAs(content);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("mixed.xlsx", content.ToArray(), true, ct));
        Assert.Empty(result.Issues); var rider = Assert.Single(result.Riders);
        Assert.Equal(2, rider.Accounts.Count); Assert.Equal(f.First.RiderProfileId, rider.RiderProfileId);
        Assert.Equal(962.52m, rider.NetEarnings); Assert.Equal(388.94m, rider.CompanyAmount);
        Assert.Equal(573.58m, rider.RiderAmount);
        Assert.Equal(375m, rider.Accounts.Single(a => a.AccountId == other.Id).CompanyAmount);
    }

    [Fact]
    public async Task BulkOrdersKeepDifferentMonthsAndHalfBoundariesSeparate()
    {
        await using var f = await Fixture.Create(false);
        var ct = TestContext.Current.CancellationToken;
        Value(await f.Service.SaveAccountAsync(f.Account.Id, new("Freelancer"), ct));
        f.First.EffectiveFrom = new(2026, 9, 1); await f.Db.SaveChangesAsync(ct);
        var result = Value(await f.Service.ProcessOrderSettlementsAsync("months.xlsx",
            OrderSheet((new(2026, 9, 15), 100, 0, 0, 0, 0), (new(2026, 9, 16), 100, 0, 0, 0, 0),
                (new(2026, 9, 30), 100, 0, 0, 0, 0), (new(2026, 10, 1), 100, 0, 0, 0, 0),
                (new(2026, 10, 15), 100, 0, 0, 0, 0), (new(2026, 10, 16), 100, 0, 0, 0, 0),
                (new(2026, 10, 31), 100, 0, 0, 0, 0)), true, ct));
        Assert.Empty(result.Issues); Assert.Equal(4, result.Riders.Count);
        Assert.Equal(new DateOnly(2026, 9, 15), result.Riders[0].PeriodTo);
        Assert.Equal(new DateOnly(2026, 9, 30), result.Riders[1].PeriodTo);
        Assert.Equal(new DateOnly(2026, 10, 15), result.Riders[2].PeriodTo);
        Assert.Equal(new DateOnly(2026, 10, 31), result.Riders[3].PeriodTo);
        Assert.Equal(86.96m, result.Riders[0].NetEarnings);
        Assert.All(result.Riders.Skip(1), r => Assert.Equal(173.91m, r.NetEarnings));
    }

    [Fact]
    public void OrderParserHandlesBlanksSignedDeductionsAndValidatesDatesDuplicatesAndHeaders()
    {
        var ct = TestContext.Current.CancellationToken;
        var sheet = OrderSheet((new(2026, 10, 15), 100, 0, 10, 2, -20));
        var result = ChefzOrderSpreadsheetParser.Parse(sheet, ct);
        Assert.Empty(result.Issues); Assert.Equal(20, Assert.Single(result.Rows).Deductions);
        using var original = new MemoryStream();
        original.Write(sheet); original.Position = 0;
        using var book = new XLWorkbook(original);
        var s = book.Worksheet(1);
        s.Cell(2, 1).Value = "2026-10-15"; s.Cell(2, 4).Clear(); s.Cell(2, 2).Value = "٢٥٢٢٧٧٧٩٣٣";
        byte[] Bytes() { book.Save(); return original.ToArray(); }
        result = ChefzOrderSpreadsheetParser.Parse(Bytes(), ct);
        Assert.Empty(result.Issues); Assert.Equal("2522777933", Assert.Single(result.Rows).DriverId);
        Assert.Equal(0, Assert.Single(result.Rows).Deductions);
        s.Row(2).CopyTo(s.Row(3));
        Assert.Single(ChefzOrderSpreadsheetParser.Parse(Bytes(), ct).Issues);
        s.Row(3).Delete(); s.Cell(2, 1).Value = "invalid";
        Assert.Single(ChefzOrderSpreadsheetParser.Parse(Bytes(), ct).Issues);
        s.Cell(2, 1).Value = "2026-10-15"; s.Cell(2, 3).FormulaA1 = "1+1";
        Assert.Single(ChefzOrderSpreadsheetParser.Parse(Bytes(), ct).Issues);
        s.Cell(1, 9).Value = "Date";
        Assert.Single(ChefzOrderSpreadsheetParser.Parse(Bytes(), ct).Issues);
    }

    [Fact]
    public void SuppliedOrderSampleIsParsedReadOnlyWhenProvided()
    {
        var path = Environment.GetEnvironmentVariable("CHEFZ_ORDER_SAMPLE_PATH");
        Assert.SkipUnless(path is not null, "Set CHEFZ_ORDER_SAMPLE_PATH for read-only sample verification.");
        using var source = new FileStream(path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var content = new MemoryStream(); source.CopyTo(content);
        var result = ChefzOrderSpreadsheetParser.Parse(content.ToArray(), TestContext.Current.CancellationToken);
        Assert.Empty(result.Issues); Assert.Equal(5751, result.Rows.Count);
        Assert.All(result.Rows, r => Assert.InRange(r.Date, new(2026, 9, 16), new(2026, 9, 30)));
        Assert.Equal(540m, result.Rows.Sum(r => r.Bonuses));
    }

    private static byte[] OrderSheet(params (DateOnly Date, decimal Delivery, decimal Tip, decimal Compensation, decimal Bonus, decimal Deduction)[] rows)
    {
        using var book = new XLWorkbook(); var s = book.AddWorksheet("Orders");
        string[] headers = ["Date", "Driver ID", "Driver   Delivery Fees", "Deduction Amount", "Driver Compensate", "Driver Tip", "Bonus Amount", "Chef Order ID"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        for (var i = 0; i < rows.Length; i++)
        {
            var r = rows[i]; var n = i + 2;
            s.Cell(n, 1).Value = r.Date.ToDateTime(TimeOnly.MinValue); s.Cell(n, 2).Value = "2522777933";
            s.Cell(n, 3).Value = (double)r.Delivery; s.Cell(n, 4).Value = (double)r.Deduction;
            s.Cell(n, 5).Value = (double)r.Compensation; s.Cell(n, 6).Value = (double)r.Tip;
            s.Cell(n, 7).Value = (double)r.Bonus; s.Cell(n, 8).Value = $"order-{i}";
        }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    private static T Value<T>(Result<T> result) { Assert.True(result.IsSuccess, result.Error.Description); return result.Value!; }

    private static byte[] Sheet(string id, string rate, string? extraId = null,
        int total = 3, int accepted = 2, int declined = 0, int missed = 1)
    {
        using var book = new XLWorkbook();
        var s = book.AddWorksheet("Driver Report");
        string[] headers = ["Driver Name", "Id Number", "Phone Number", "Team", "Total Offers", "Total Valid Offers", "Accepted Offers", "Declined Offers", "Missed Offers", "Acceptance Rate"];
        for (var i = 0; i < headers.Length; i++) s.Cell(1, i + 1).Value = headers[i];
        s.Cell(2, 2).Value = id;
        s.Cell(2, 6).Value = total; s.Cell(2, 7).Value = accepted; s.Cell(2, 8).Value = declined; s.Cell(2, 9).Value = missed;
        if (rate == "numeric") { s.Cell(2, 10).Value = 2d / 3; s.Cell(2, 10).Style.NumberFormat.Format = "0.00%"; }
        else s.Cell(2, 10).Value = rate;
        if (extraId is not null) { s.Row(2).CopyTo(s.Row(3)); s.Cell(3, 2).Value = extraId; }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    private sealed class CurrentUser : ICurrentUser
    {
        public Guid? UserId { get; } = Guid.NewGuid(); public Guid? SessionId => null;
        public long? AuthorizationVersion => 1; public string? CorrelationId => null;
    }
    private sealed class Checker : IPermissionChecker
    {
        public bool Allow { get; set; } = true;
        public Task<bool> HasPermissionAsync(Guid userId, long authorizationVersion, string permissionKey, PermissionScope? scope = null, CancellationToken cancellationToken = default) => Task.FromResult(Allow);
        public void InvalidateUser(Guid userId, long authorizationVersion) { }
    }
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 10, 31, 20, 0, 0, TimeSpan.Zero); }

    private sealed class Fixture(ApplicationDbContext db, ChefzService service, Checker checker, PlatformRiderAccount account,
        RiderClientAssignment first, RiderClientAssignment second, bool sql) : IAsyncDisposable
    {
        public ApplicationDbContext Db => db; public ChefzService Service => service; public Checker Checker => checker;
        public PlatformRiderAccount Account => account; public RiderClientAssignment First => first; public RiderClientAssignment Second => second;
        public static async Task<Fixture> Create(bool sql)
        {
            var user = new CurrentUser(); var clock = new Clock(); var checker = new Checker();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (sql) options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database=ChefzTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True");
            else options.UseInMemoryDatabase($"ChefzTests_{Guid.NewGuid():N}", memory => memory.EnableNullChecks(false));
            options.AddInterceptors(new ApplicationPersistenceInterceptor(user, clock));
            var db = new ApplicationDbContext(options.Options);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var platform = new ClientPlatform { Code = "CHEFZ", NameAr = "شيفز", NameEn = "Chefz" };
            var owner = new Employee { FullNameAr = "صاحب الحساب", IqamaNo = "2522777933" };
            var firstEmployee = new Employee { FullNameAr = "المندوب الأول" }; var secondEmployee = new Employee { FullNameAr = "المندوب الثاني" };
            var rider1 = new RiderProfile { EmployeeId = firstEmployee.Id }; var rider2 = new RiderProfile { EmployeeId = secondEmployee.Id };
            var contract = new ClientContract { ClientPlatformId = platform.Id, Code = "CHEFZ-TEST", DisplayNameAr = "شيفز", DisplayNameEn = "Chefz" };
            var account = new PlatformRiderAccount { ClientPlatformId = platform.Id, RegisteredEmployeeId = owner.Id,
                OperatingCityId = await db.OperatingCities.Select(c => c.Id).FirstAsync(TestContext.Current.CancellationToken), Code = "CHEFZ-1", ExternalAccountId = "platform-1" };
            var first = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = contract.Id, RiderProfileId = rider1.Id,
                EffectiveFrom = new(2026, 10, 1), EffectiveTo = new(2026, 10, 15), Status = RiderAssignmentStatus.Ended, AssignedByUserId = user.UserId!.Value };
            var second = new RiderClientAssignment { PlatformRiderAccountId = account.Id, ClientContractId = contract.Id, RiderProfileId = rider2.Id,
                EffectiveFrom = new(2026, 10, 16), Status = RiderAssignmentStatus.Active, AssignedByUserId = user.UserId!.Value };
            db.AddRange(platform, owner, firstEmployee, secondEmployee, rider1, rider2, contract, account, first, second);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return new(db, new ChefzService(db, user, checker, clock, new PlatformCredentialProtector(() => new byte[32])), checker, account, first, second, sql);
        }
        public async ValueTask DisposeAsync()
        {
            if (sql) await db.Database.EnsureDeletedAsync();
            await db.DisposeAsync();
        }
    }
}
