using LogisticsERP.Application.Features.Chefz;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllRidersReportReturnsEveryGradeHistoricalAssigneesAndMissingData(bool sql)
    {
        Assert.SkipUnless(!sql || Environment.GetEnvironmentVariable("CHEFZ_SQL_TESTS") == "1",
            "Set CHEFZ_SQL_TESTS=1 for disposable SQL Server tests.");
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(sql);
        AddBulkPerformance(f, f.Account, f.First, new(2026, 10, 15), 80, 100);
        AddBulkPerformance(f, f.Account, f.Second, new(2026, 10, 16), 60, 80);
        var c = AddBulkReportRider(f, "C");
        AddBulkPerformance(f, c.Account, c.Assignment, new(2026, 10, 20), 40, 60);
        var d = AddBulkReportRider(f, "D");
        AddBulkPerformance(f, d.Account, d.Assignment, new(2026, 10, 20), 1, 10);
        var change = AddBulkReportRider(f, "CHANGE");
        for (var day = 20; day <= 22; day++)
            AddBulkPerformance(f, change.Account, change.Assignment, new(2026, 10, day), 0, 1);
        var unrated = AddBulkReportRider(f, "UNRATED");
        var cancelled = AddBulkReportRider(f, "CANCELLED");
        cancelled.Assignment.Status = RiderAssignmentStatus.Cancelled;
        var outsidePeriod = AddBulkReportRider(f, "FUTURE");
        outsidePeriod.Assignment.EffectiveFrom = new(2026, 11, 1);
        var otherPlatform = new ClientPlatform { Code = "OTHER", NameAr = "أخرى", NameEn = "Other" };
        f.Db.Add(otherPlatform);
        var other = AddBulkReportRider(f, "OTHER");
        other.Account.ClientPlatformId = otherPlatform.Id;
        AddBulkPerformance(f, other.Account, other.Assignment, new(2026, 10, 20), 100, 100);
        // Data outside the selected period must not change either summary or grade.
        AddBulkPerformance(f, c.Account, c.Assignment, new(2026, 11, 1), 500, 500);
        await f.Db.SaveChangesAsync(ct);

        var report = Value(await f.Service.GetAllRidersReportAsync(2026, 10, 2, 2, ct));
        Assert.Equal(new DateOnly(2026, 10, 1), report.From);
        Assert.Equal(new DateOnly(2026, 10, 31), report.To);
        Assert.Equal(2, report.RequestedHalves);
        Assert.Equal(6, report.TotalRiders);
        Assert.Equal(new ChefzRatingCounts(1, 1, 1, 1, 1, 1), report.RatingCounts);
        Assert.Equal(6, report.Riders.Select(r => r.RiderProfileId).Distinct().Count());
        Assert.Equal("A", Assert.Single(report.Riders, r => r.RiderProfileId == f.First.RiderProfileId).SelectedSummary.Rating);
        Assert.Equal("B", Assert.Single(report.Riders, r => r.RiderProfileId == f.Second.RiderProfileId).SelectedSummary.Rating);
        var noData = Assert.Single(report.Riders, r => r.RiderProfileId == unrated.Assignment.RiderProfileId);
        Assert.Null(noData.SelectedSummary.Rating);
        Assert.Null(noData.SelectedSummary.AcceptanceRate);
        Assert.Null(noData.SelectedSummary.AverageOrdersPerHalf);
        Assert.All(noData.Halves, h => Assert.NotNull(h.Message));
        Assert.Null(Assert.Single(noData.Accounts).Summary.Rating);
        foreach (var row in report.Riders)
        {
            var individual = Value(await f.Service.GetRiderReportAsync(null, row.RiderProfileId, 2026, 10, 2, 2, ct));
            Assert.Equal(individual.SelectedSummary, row.SelectedSummary);
            Assert.Equal(individual.Halves, row.Halves);
        }
        var halfOnly = Value(await f.Service.GetAllRidersReportAsync(2026, 10, 2, 1, ct));
        Assert.Equal(5, halfOnly.TotalRiders);
        Assert.DoesNotContain(halfOnly.Riders, r => r.RiderProfileId == f.First.RiderProfileId);
    }

    [Fact]
    public async Task AllRidersReportCombinesTwoAccountsWithWeightedAcceptanceAndOneRiderCount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, "FullTime", "Freelancer", ct);
        Assert.Empty(Value(await f.Service.ImportPerformanceAsync(new(2026, 10, 20), "daily.xlsx", DualAccountDailySheet(4), ct)).Issues);
        var report = Value(await f.Service.GetAllRidersReportAsync(2026, 10, 2, 1, ct));
        Assert.Equal(1, report.TotalRiders);
        var rider = Assert.Single(report.Riders);
        Assert.Equal(f.Second.RiderProfileId, rider.RiderProfileId);
        Assert.Equal(6, rider.SelectedSummary.Orders);
        Assert.Equal(60m, rider.SelectedSummary.AcceptanceRate);
        Assert.Equal(1, rider.SelectedSummary.DaysWithData);
        Assert.Equal(2, rider.Accounts.Count);
        Assert.Equal(2, Assert.Single(rider.Accounts, a => a.AccountId == f.Account.Id).Summary.Orders);
        Assert.Equal(4, Assert.Single(rider.Accounts, a => a.AccountId == second.Account.Id).Summary.Orders);
        Assert.Equal(1, report.RatingCounts.D);
    }

    [Fact]
    public async Task AllRidersReportDoesNotTruncateTheRosterAtTheNormalPageLimit()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        for (var i = 0; i < 101; i++) AddBulkReportRider(f, $"RIDER-{i:D3}");
        await f.Db.SaveChangesAsync(ct);
        var report = Value(await f.Service.GetAllRidersReportAsync(2026, 10, 2, 1, ct));
        Assert.Equal(102, report.TotalRiders);
        Assert.Equal(102, report.Riders.Count);
        Assert.Equal(102, report.RatingCounts.Unrated);
        Assert.Equal(report.Riders.OrderBy(r => r.RiderName, StringComparer.Ordinal).ThenBy(r => r.RiderProfileId), report.Riders);
    }

    [Theory]
    [InlineData(2026, 0, 1, 1)]
    [InlineData(2026, 13, 1, 1)]
    [InlineData(0, 1, 1, 1)]
    [InlineData(10000, 1, 1, 1)]
    [InlineData(2026, 10, 3, 1)]
    [InlineData(2026, 10, 1, 0)]
    [InlineData(2026, 10, 1, 121)]
    [InlineData(1, 1, 1, 2)]
    public async Task AllRidersReportRejectsInvalidPeriods(int year, int month, int half, int halves)
    {
        await using var f = await Fixture.Create(false);
        var result = await f.Service.GetAllRidersReportAsync(year, month, half, halves, TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.Equal("Chefz.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task AllRidersReportHandlesEmptyPeriodsCalendarBoundariesAndDeniedAccess()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var empty = Value(await f.Service.GetAllRidersReportAsync(2026, 1, 1, 2, ct));
        Assert.Equal(new DateOnly(2025, 12, 16), empty.From);
        Assert.Empty(empty.Riders);
        Assert.Equal(new ChefzRatingCounts(0, 0, 0, 0, 0, 0), empty.RatingCounts);
        Assert.Equal(new DateOnly(9999, 12, 31), Value(await f.Service.GetAllRidersReportAsync(9999, 12, 2, 1, ct)).To);
        f.Checker.Allow = false;
        var denied = await f.Service.GetAllRidersReportAsync(2026, 10, 2, 1, ct);
        Assert.True(denied.IsFailure);
        Assert.Equal("Chefz.Forbidden", denied.Error.Code);
    }

    private static (PlatformRiderAccount Account, RiderClientAssignment Assignment) AddBulkReportRider(Fixture f, string name)
    {
        var employee = new Employee { FullNameAr = name };
        var rider = new RiderProfile { EmployeeId = employee.Id };
        var account = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId,
            RegisteredEmployeeId = employee.Id, OperatingCityId = f.Account.OperatingCityId,
            Code = $"BULK-{name}", ExternalAccountId = $"BULK-{name}" };
        var assignment = new RiderClientAssignment { PlatformRiderAccountId = account.Id, RiderProfileId = rider.Id,
            ClientContractId = f.Second.ClientContractId, AssignedByUserId = f.Second.AssignedByUserId,
            EffectiveFrom = new(2026, 10, 16), Status = RiderAssignmentStatus.Active };
        f.Db.AddRange(employee, rider, account, assignment);
        return (account, assignment);
    }

    private static void AddBulkPerformance(Fixture f, PlatformRiderAccount account, RiderClientAssignment assignment,
        DateOnly date, int orders, int valid)
    {
        var import = new ChefzPerformanceImport { ReportDate = date, FileName = $"{account.Code}-{date:yyyyMMdd}.xlsx",
            ContentHash = Guid.NewGuid().ToString("N"), ImportedRows = 1 };
        f.Db.AddRange(import, new ChefzDailyPerformance { PlatformRiderAccountId = account.Id,
            RiderClientAssignmentId = assignment.Id, RiderProfileId = assignment.RiderProfileId,
            ImportId = import.Id, ReportDate = date, ReportIdNumber = account.ExternalAccountId,
            AcceptedOffers = orders, TotalValidOffers = valid, MissedOffers = valid - orders,
            AcceptanceRate = valid == 0 ? 0 : decimal.Round(100m * orders / valid, 2) });
    }
}
