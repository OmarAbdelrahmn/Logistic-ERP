using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class ChefzWorkflowTests
{
    [Fact]
    public async Task ManagementDailyBucketsReconcileAfterCombiningAccountsAndExcludingZeroAndMissingOrders()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var second = await AddSecondAccountForReports(f, "FullTime", "Freelancer", ct);
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(a => a.Id == second.AssignmentId, ct);
        var date = new DateOnly(2026, 10, 20);
        // Two low-order accounts belong to one rider whose combined total is above three.
        AddBulkPerformance(f, f.Account, f.Second, date, 2, 2);
        AddBulkPerformance(f, second.Account, assignment, date, 2, 2);
        foreach (var orders in new[] { 0, 1, 3, 4 })
        {
            var rider = AddBulkReportRider(f, $"DAILY-{orders}");
            rider.Account.SponsorId = f.Account.SponsorId;
            AddBulkPerformance(f, rider.Account, rider.Assignment, date, orders, 4);
        }
        var missing = AddBulkReportRider(f, "NO-DAILY-ROW");
        missing.Account.SponsorId = f.Account.SponsorId;
        AddBulkPerformance(f, missing.Account, missing.Assignment, date.AddDays(-1), 8, 8);
        AddBulkPerformance(f, missing.Account, missing.Assignment, date.AddDays(1), 9, 9);
        var outside = AddBulkReportRider(f, "OUTSIDE-SPONSOR");
        AddBulkPerformance(f, outside.Account, outside.Assignment, date, 3, 3);
        await f.Db.SaveChangesAsync(ct);

        var report = Assert.Single(Value(await f.Service.GetManagementReportAsync(date.AddDays(1), f.Account.SponsorId, ct)));
        Assert.Equal(date, report.PerformanceDate);
        Assert.Equal(20, report.CycleOrders);
        Assert.Equal(4, report.RidersWithOrdersYesterday);
        Assert.Equal(2, report.RidersWithThreeOrFewerOrdersYesterday);
        Assert.Equal(2, report.RidersWithMoreThanThreeOrdersYesterday);
        Assert.Equal(report.RidersWithOrdersYesterday,
            report.RidersWithThreeOrFewerOrdersYesterday + report.RidersWithMoreThanThreeOrdersYesterday);
    }

    [Theory]
    [InlineData(1, 9, 16)]
    [InlineData(16, 10, 1)]
    public async Task ManagementDailyBucketsAreZeroWithoutOrdersAtHalfBoundaries(int reportDay, int cycleMonth, int cycleDay)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(false);
        var sponsor = await f.Db.Sponsors.FirstAsync(ct);
        f.Account.SponsorId = sponsor.Id;
        var date = new DateOnly(2026, 10, reportDay);
        // An assigned rider with an explicit zero and another without a daily row are both excluded.
        AddBulkPerformance(f, f.Account, f.First, date.AddDays(-1), 0, 1);
        var missing = AddBulkReportRider(f, "MISSING");
        missing.Account.SponsorId = sponsor.Id;
        missing.Assignment.EffectiveFrom = new(2026, 9, 1);
        await f.Db.SaveChangesAsync(ct);

        var report = Assert.Single(Value(await f.Service.GetManagementReportAsync(date, sponsor.Id, ct)));
        Assert.Equal(new DateOnly(2026, cycleMonth, cycleDay), report.CycleFrom);
        Assert.Equal(0, report.RidersWithOrdersYesterday);
        Assert.Equal(0, report.RidersWithThreeOrFewerOrdersYesterday);
        Assert.Equal(0, report.RidersWithMoreThanThreeOrdersYesterday);
        Assert.Equal(2, report.AccountsBelowThreeOrdersInCycle);
    }
}
