using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Entities.Workforce;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task DailySponsorBreakdownReconcilesSharedSourcesLegacyRecordsFiltersAndDatePaging()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var day = new DateOnly(2026, 10, 12); f.Clock.Now = f.Clock.Now.AddDays(1);
        var sponsors = new[]
        {
            new Sponsor { RegistryNameAr = "الكفيل الأول" },
            new Sponsor { RegistryNameAr = "الكفيل الثاني", Status = CatalogStatus.Disabled },
            new Sponsor { RegistryNameAr = "الكفيل الثالث", IsDeleted = true }
        };
        f.Account.SponsorId = sponsors[0].Id;
        f.Account.IsDeleted = true; // Historical totals and sponsor details remain available.
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "NINJA-2",
            ExternalAccountId = "second", SponsorId = sponsors[1].Id };
        var third = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "NINJA-3",
            ExternalAccountId = "third", SponsorId = sponsors[2].Id };
        var unassigned = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "NINJA-4",
            ExternalAccountId = "unassigned" };
        var otherPlatform = new ClientPlatform { Code = "OTHER", NameAr = "Other" };
        var other = new PlatformRiderAccount { ClientPlatformId = otherPlatform.Id, Code = "OTHER-1",
            ExternalAccountId = "other", SponsorId = sponsors[0].Id };
        // The report uses the account's sponsor, even when the rider has another sponsor.
        (await f.Db.Employees.SingleAsync(e => e.Id == f.Rider.EmployeeId, ct)).SponsorId = sponsors[2].Id;
        f.Db.AddRange(sponsors); f.Db.AddRange(second, third, unassigned, otherPlatform, other);
        var performanceSource = Guid.NewGuid(); var deductionSource = Guid.NewGuid();
        f.Db.NinjaDailyPerformance.AddRange(
            Performance(f.Account.Id, f.Rider.Id, day, 2, 3, performanceSource),
            Performance(f.Account.Id, f.OwnerRider.Id, day, 4, 7, performanceSource),
            Performance(second.Id, f.Rider.Id, day, 8, 4, Guid.Empty),
            Performance(unassigned.Id, f.Rider.Id, day.AddDays(1), 0, 0, Guid.Empty),
            Performance(other.Id, f.Rider.Id, day, 999, 99, Guid.Empty));
        f.Db.NinjaDailyDeductions.AddRange(
            Deduction(f.Account.Id, f.Rider.Id, day, 30, -5, deductionSource),
            Deduction(f.Account.Id, f.OwnerRider.Id, day, 70, -2, deductionSource),
            Deduction(third.Id, f.OwnerRider.Id, day, 20, 1, Guid.Empty),
            Deduction(unassigned.Id, f.Rider.Id, day.AddDays(1), 0, -3, Guid.Empty));
        await f.Db.SaveChangesAsync(ct);

        var daily = Value(await f.Service.GetDailyReportAsync(day, day.AddDays(1), null, null, 1, 50, ct)).Items;
        Assert.Equal(2, daily.Count);
        var first = daily[0]; Assert.Equal(day, first.Date); Assert.Equal(3, first.Sponsors.Count);
        Assert.Equal(2, first.Metrics.PerformanceRecords); Assert.Equal(2, first.Metrics.DeductionRecords);
        Assert.Equal(14, first.Metrics.TotalOrders); Assert.Equal(14, first.Metrics.TotalWorkingHours);
        Assert.Equal(120, first.Metrics.TotalCollectedAmountAtDelivery); Assert.Equal(-6, first.Metrics.TotalDeductions);
        Assert.Equal(1, first.Metrics.OrdersPerWorkingHour);
        var one = Assert.Single(first.Sponsors, s => s.SponsorId == sponsors[0].Id);
        Assert.Equal(sponsors[0].RegistryNameAr, one.SponsorNameAr);
        Assert.Equal(new NinjaReportMetrics(1, 1, 6, 10, 0.6m, 100, -7), one.Metrics);
        var two = Assert.Single(first.Sponsors, s => s.SponsorId == sponsors[1].Id);
        Assert.Equal(sponsors[1].RegistryNameAr, two.SponsorNameAr);
        Assert.Equal(new NinjaReportMetrics(1, 0, 8, 4, 2, 0, 0), two.Metrics);
        var three = Assert.Single(first.Sponsors, s => s.SponsorId == sponsors[2].Id);
        Assert.Equal(sponsors[2].RegistryNameAr, three.SponsorNameAr);
        Assert.Equal(new NinjaReportMetrics(0, 1, 0, 0, null, 20, 1), three.Metrics);
        Assert.All(daily, AssertSponsorTotals);
        var noSponsor = Assert.Single(daily[1].Sponsors);
        Assert.Null(noSponsor.SponsorId); Assert.Null(noSponsor.SponsorNameAr);
        Assert.Equal(new NinjaReportMetrics(1, 1, 0, 0, null, 0, -3), noSponsor.Metrics);
        Assert.Equivalent(daily[1], Assert.Single(Value(await f.Service.GetDailyReportAsync(day, day.AddDays(1), null, null, 2, 1, ct)).Items));
        Assert.Empty(Value(await f.Service.GetDailyReportAsync(day, day.AddDays(1), null, null, 3, 1, ct)).Items);

        var accountFiltered = Assert.Single(Value(await f.Service.GetDailyReportAsync(day, day.AddDays(1), f.Account.Id, null, 1, 50, ct)).Items);
        Assert.Equal(one, Assert.Single(accountFiltered.Sponsors)); AssertSponsorTotals(accountFiltered);
        var riderFiltered = Value(await f.Service.GetDailyReportAsync(day, day.AddDays(1), null, f.Rider.Id, 1, 50, ct)).Items;
        Assert.Equal(2, riderFiltered[0].Sponsors.Count); Assert.Equal(10, riderFiltered[0].Metrics.TotalOrders);
        Assert.Equal(30, riderFiltered[0].Metrics.TotalCollectedAmountAtDelivery);
        Assert.All(riderFiltered, AssertSponsorTotals);
    }

    private static NinjaDailyPerformance Performance(Guid account, Guid rider, DateOnly date, int orders, decimal hours, Guid source) => new()
    {
        PlatformRiderAccountId = account, RiderProfileId = rider, ReportDate = date, SourceId = source,
        TotalOrders = orders, TotalWorkingHours = hours, RiderClientAssignmentId = Guid.NewGuid(), ImportId = Guid.NewGuid()
    };

    private static NinjaDailyDeduction Deduction(Guid account, Guid rider, DateOnly date, decimal collected, decimal deductions, Guid source) => new()
    {
        PlatformRiderAccountId = account, RiderProfileId = rider, ReportDate = date, SourceId = source,
        TotalCollectedAmountAtDelivery = collected, TotalDeductions = deductions,
        RiderClientAssignmentId = Guid.NewGuid(), ImportId = Guid.NewGuid()
    };

    private static void AssertSponsorTotals(NinjaDailyReport day)
    {
        var parts = day.Sponsors.Select(s => s.Metrics).ToArray();
        Assert.Equal(day.Metrics.PerformanceRecords, parts.Sum(s => s.PerformanceRecords));
        Assert.Equal(day.Metrics.DeductionRecords, parts.Sum(s => s.DeductionRecords));
        Assert.Equal(day.Metrics.TotalOrders, parts.Sum(s => s.TotalOrders));
        Assert.Equal(day.Metrics.TotalWorkingHours, parts.Sum(s => s.TotalWorkingHours));
        Assert.Equal(day.Metrics.TotalCollectedAmountAtDelivery, parts.Sum(s => s.TotalCollectedAmountAtDelivery));
        Assert.Equal(day.Metrics.TotalDeductions, parts.Sum(s => s.TotalDeductions));
    }
}
