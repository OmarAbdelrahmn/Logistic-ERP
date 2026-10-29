using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Fact]
    public async Task ReportsCombineMultipleAccountsForOneRiderCountDistinctDaysAndExcludeOtherPlatforms()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var day = new DateOnly(2026, 10, 12);
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "NINJA-2",
            ExternalAccountId = "272579", OperatingCityId = f.Account.OperatingCityId, RegisteredEmployeeId = f.Rider.EmployeeId };
        var otherPlatform = new ClientPlatform { Code = "OTHER", NameAr = "Other" };
        var other = new PlatformRiderAccount { ClientPlatformId = otherPlatform.Id, Code = "OTHER-1", ExternalAccountId = "999" };
        f.Db.AddRange(second, otherPlatform, other); await f.Db.SaveChangesAsync(ct);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, day, "0501111111", "initial", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ImportAsync(false, day, "one.xlsx", Sheet(false, f.Account.ExternalAccountId, 10, 2), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(day, "0501111111", null, "Changed accounts", first.Assignment.RowVersion), ct));
        Value(await f.Service.AssignAsync(second.Id, new(f.Rider.Id, day, "0502222222", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ImportAsync(false, day, "two.xlsx", Sheet(false, second.ExternalAccountId, 5, 3), ct));
        f.Db.NinjaDailyPerformance.Add(new NinjaDailyPerformance { PlatformRiderAccountId = other.Id, RiderProfileId = f.Rider.Id,
            RiderClientAssignmentId = Guid.NewGuid(), ImportId = Guid.NewGuid(), ReportDate = day, CaptainId = "999", TotalOrders = 999 });
        await f.Db.SaveChangesAsync(ct);
        var summary = Value(await f.Service.GetPerformanceReportAsync(day, day, null, null, ct));
        Assert.Equal(15, summary.Metrics.TotalOrders); Assert.Equal(2, summary.Metrics.PerformanceRecords);
        Assert.Equal(1, summary.DaysWithPerformance); Assert.Equal(2, summary.AccountsWithData); Assert.Equal(1, summary.RidersWithData);
        var rider = Assert.Single(Value(await f.Service.GetRiderReportAsync(day, day, null, f.Rider.Id, 1, 50, ct)).Items);
        Assert.Equal(2, rider.AccountsWithData); Assert.Equal(1, rider.DaysWithPerformance); Assert.Equal(3, rider.Metrics.OrdersPerWorkingHour);
        Assert.Equal("ninja.not_found", (await f.Service.GetPerformanceReportAsync(day, day, other.Id, null, ct)).Error.Code);
        Assert.Equal("ninja.not_found", (await f.Service.GetAccountAsync(other.Id, ct)).Error.Code);
        Assert.Equal("ninja.not_found", (await f.Service.SetAccountStatusAsync(other.Id, new("Suspended", "AQ=="), ct)).Error.Code);
    }

    [Fact]
    public async Task ReportsAggregateHistoricalRidersAndIndependentDailySourcesWithoutMultiplyingTotals()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create();
        var day = new DateOnly(2026, 10, 12); f.Clock.Now = f.Clock.Now.AddDays(3);
        var assigned = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, day, "0501111111", "initial", WasBackdated: true, BackdatedReason: "History", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ImportAsync(false, day, "p.xlsx", Sheet(false, "272578", 18, 6), ct));
        Value(await f.Service.ImportAsync(true, day, "d.xlsx", Sheet(true, "272578", 100, -10), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(day, "0502222222", null, "Returned", assigned.Assignment.RowVersion), ct));
        Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, day.AddDays(1), "0503333333", WasBackdated: true, BackdatedReason: "History", RiderType: "InternalCar", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ImportAsync(false, day.AddDays(1), "p.xlsx", Sheet(false, "272578", 12, 4), ct));
        Value(await f.Service.ImportAsync(false, day.AddDays(2), "p.xlsx", Sheet(false, "272578", 0, 0), ct));
        Value(await f.Service.ImportAsync(true, day.AddDays(2), "d.xlsx", Sheet(true, "272578", 30, 2), ct));
        Value(await f.Service.ImportAsync(true, day.AddDays(3), "d.xlsx", Sheet(true, "272578", 0, -3), ct));
        f.Account.Status = PlatformRiderAccountStatus.Archived;
        await f.Db.SaveChangesAsync(ct);
        var summary = Value(await f.Service.GetPerformanceReportAsync(day, day.AddDays(3), null, null, ct));
        Assert.Equal(3, summary.DaysWithPerformance); Assert.Equal(3, summary.DaysWithDeductions);
        Assert.Equal(1, summary.AccountsWithData); Assert.Equal(2, summary.RidersWithData);
        Assert.Equal(30, summary.Metrics.TotalOrders); Assert.Equal(10, summary.Metrics.TotalWorkingHours);
        Assert.Equal(3, summary.Metrics.OrdersPerWorkingHour); Assert.Equal(130, summary.Metrics.TotalCollectedAmountAtDelivery);
        Assert.Equal(-11, summary.Metrics.TotalDeductions); Assert.Equal(3, summary.Metrics.PerformanceRecords);
        Assert.Equal(3, summary.Metrics.DeductionRecords);
        var riders = Value(await f.Service.GetRiderReportAsync(day, day.AddDays(3), null, null, 1, 50, ct)).Items;
        Assert.Equal(2, riders.Count); Assert.Equal(f.Rider.Id, riders[0].RiderProfileId); Assert.Equal(18, riders[0].Metrics.TotalOrders);
        Assert.Equal(f.Rider.EmployeeId, riders[0].Rider!.EmployeeId); Assert.Equal(-10, riders[0].Metrics.TotalDeductions);
        Assert.Equal(f.OwnerRider.Id, riders[1].RiderProfileId); Assert.Equal(1, riders[1].AccountsWithData);
        var filtered = Value(await f.Service.GetPerformanceReportAsync(day, day.AddDays(3), f.Account.Id, f.Rider.Id, ct));
        Assert.Equal(18, filtered.Metrics.TotalOrders); Assert.Equal(1, filtered.RidersWithData);
        var daily = Value(await f.Service.GetDailyReportAsync(day, day.AddDays(3), null, null, 1, 50, ct)).Items;
        Assert.Equal(4, daily.Count); Assert.Equal(day, daily[0].Date); Assert.Null(daily[2].Metrics.OrdersPerWorkingHour);
        Assert.Equal(0, daily[3].Metrics.PerformanceRecords); Assert.Equal(-3, daily[3].Metrics.TotalDeductions);
        Assert.Equivalent(daily[2], Assert.Single(Value(await f.Service.GetDailyReportAsync(day, day.AddDays(3), null, null, 3, 1, ct)).Items));
        f.Account.IsDeleted = true; await f.Db.SaveChangesAsync(ct);
        Assert.Equal(30, Value(await f.Service.GetPerformanceReportAsync(day, day.AddDays(3), f.Account.Id, null, ct)).Metrics.TotalOrders);
    }

    [Fact]
    public async Task ReportsDistinguishEmptyDataAndRejectBadRangesPagingUnknownFiltersAndUnauthorizedReads()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var f = await Fixture.Create(); var day = new DateOnly(2026, 10, 12);
        var empty = Value(await f.Service.GetPerformanceReportAsync(day, day, f.Account.Id, f.Rider.Id, ct));
        Assert.Equal(0, empty.Metrics.PerformanceRecords); Assert.Equal(0, empty.Metrics.TotalOrders);
        Assert.Null(empty.Metrics.OrdersPerWorkingHour); Assert.Equal(0, empty.DaysWithPerformance);
        Assert.Empty(Value(await f.Service.GetRiderReportAsync(day, day, null, null, 1, 50, ct)).Items);
        Assert.True((await f.Service.GetPerformanceReportAsync(default, day, null, null, ct)).IsFailure);
        Assert.True((await f.Service.GetPerformanceReportAsync(day, day.AddDays(1), null, null, ct)).IsFailure);
        Assert.True((await f.Service.GetPerformanceReportAsync(day, day.AddDays(-1), null, null, ct)).IsFailure);
        Assert.True((await f.Service.GetPerformanceReportAsync(day.AddDays(-366), day, null, null, ct)).IsFailure);
        Assert.True((await f.Service.GetDailyReportAsync(day, day, null, null, 0, 50, ct)).IsFailure);
        Assert.True((await f.Service.GetRiderReportAsync(day, day, null, null, 1, 101, ct)).IsFailure);
        Assert.Equal("ninja.not_found", (await f.Service.GetPerformanceReportAsync(day, day, Guid.NewGuid(), null, ct)).Error.Code);
        Assert.Equal("ninja.not_found", (await f.Service.GetPerformanceReportAsync(day, day, null, Guid.NewGuid(), ct)).Error.Code);
        f.Checker.Denied = PermissionKeys.Ninja.Read;
        Assert.Equal("ninja.forbidden", (await f.Service.GetPerformanceReportAsync(day, day, null, null, ct)).Error.Code);
        Assert.Equal("ninja.forbidden", (await f.Service.GetDailyReportAsync(day, day, null, null, 1, 50, ct)).Error.Code);
        Assert.Equal("ninja.forbidden", (await f.Service.GetRiderReportAsync(day, day, null, null, 1, 50, ct)).Error.Code);
        Assert.Empty(f.Db.NinjaDailyImports); Assert.Empty(f.Db.NinjaDailyPerformance); Assert.Empty(f.Db.NinjaDailyDeductions);
    }
}
