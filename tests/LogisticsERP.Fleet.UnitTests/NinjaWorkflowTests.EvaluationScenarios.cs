using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Theory]
    [InlineData(1, "8", "Pass", 27)]
    [InlineData(5, "8", "Pass", 27)]
    [InlineData(5, "7", "Unknown", 26)]
    public async Task EvaluationCountsLaterAttendanceAndKeepsBreaksUnknown(
        int firstBreak, string lastDayHours, string status, int qualified)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        f.Clock.Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        int[] breaks = [firstBreak, 12, 19, 26];
        await ImportCsv(f, PerformanceCsv(Enumerable.Range(1, 31).Where(day => !breaks.Contains(day))
            .Select(day => ("272578", new DateOnly(2026, 10, day), day == 31 ? 0 : 15, day == 31 ? lastDayHours : "8")).ToArray()));
        var service = Evaluation(f);
        foreach (var view in new[] { "riders", "accounts" })
        {
            var row = Assert.Single(Value(await service.EvaluateAsync(new() { Year = 2026, Month = 10, View = view }, ct)).Items);
            Assert.Equal(390, row.Evaluated.Orders); Assert.Equal(26 * 8m + decimal.Parse(lastDayHours, System.Globalization.CultureInfo.InvariantCulture), row.Evaluated.Hours);
            Assert.Equal(new DateOnly(2026, 10, 31), row.LatestCompleteDate);
            Assert.Equal(450m, row.PeriodTarget); Assert.Equal(450m * 27 / 31, row.Evaluated.Target);
            Assert.Equal(qualified, row.Attendance.Qualified); Assert.Equal(4m, row.Attendance.Unknown);
            Assert.Equal(status, row.Attendance.Status); Assert.Equal(27 - qualified, row.Attendance.ShortHoursDays);
            Assert.NotNull(row.SuggestedMoney); Assert.False(row.SuggestedMoney.AttendanceReduction);
            Assert.All(row.Days.Where(d => breaks.Contains(d.Date.Day)), d =>
            {
                Assert.Null(d.Orders); Assert.Null(d.Hours); Assert.Equal("Unknown", d.HoursStatus);
            });
            Assert.Equal(lastDayHours == "8" ? "Qualified" : "ShortHours", row.Days.Single(d => d.Date.Day == 31).HoursStatus);
            Assert.Null(row.Forecast); Assert.NotNull(row.ContinuationScenario);
            Assert.Equal(status, row.ContinuationScenario.ProjectedAttendanceStatus);
        }
    }

    [Theory]
    [InlineData(null, null, "BIKE", 450, 5700)]
    [InlineData(NinjaRiderType.InternalCar, null, "CAR", 460, 6280)]
    [InlineData(null, "CAR", "CAR", 460, 6280)]
    [InlineData(NinjaRiderType.InternalCar, "BIKE", "BIKE", 450, 5700)]
    public async Task EvaluationDefaultsMissingPricingToBikeAndPreservesExplicitPricing(
        NinjaRiderType? riderType, string? contextType, string expectedType, decimal target, decimal amount)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        var assignment = await f.Db.RiderClientAssignments.SingleAsync(ct);
        assignment.NinjaRiderType = riderType;
        await f.Db.SaveChangesAsync(ct);
        await ImportCsv(f, PerformanceCsv(Enumerable.Range(1, 30)
            .Select(day => ("272578", new DateOnly(2026, 9, day), 15, "8")).ToArray()));
        var service = Evaluation(f);
        if (contextType is not null)
            Value(await service.SetContextAsync(f.Account.Id, new(null, null,
                [new(new(2026, 9, 1), new(2026, 9, 30), contextType)], "Explicit pricing"), ct));
        foreach (var view in new[] { "riders", "accounts" })
        {
            var query = new NinjaEvaluationQuery { Year = 2026, Month = 9, View = view, PricingType = expectedType };
            var row = Assert.Single(Value(await service.EvaluateAsync(query, ct)).Items);
            Assert.All(row.Days, d => Assert.Equal(expectedType, d.PricingType));
            Assert.Equal(target, row.PeriodTarget); Assert.Equal(target, row.Evaluated.Target);
            Assert.Equal(amount, row.SuggestedMoney!.SuggestedAmount);
            Assert.Equal(expectedType, Assert.Single(row.SuggestedMoney.Segments).PricingType);
            Assert.DoesNotContain("PricingTypeUnresolved", row.Reasons);
            Assert.Empty(Value(await service.EvaluateAsync(query with { PricingType = expectedType == "BIKE" ? "CAR" : "BIKE" }, ct)).Items);
        }
        var detail = Value(await service.DayAsync(f.Account.Id, new(2026, 9, 3), ct));
        Assert.Equal(expectedType, detail.Source!.PricingType);
        Assert.Equal(expectedType, Assert.Single(detail.Riders).PricingType);
        Assert.Equal(riderType, (await f.Db.RiderClientAssignments.AsNoTracking().SingleAsync(ct)).NinjaRiderType);
    }

    [Theory]
    [InlineData(26, "Fail", 4845)]
    [InlineData(27, "Pass", 5700)]
    public async Task EvaluationOctoberAttendanceRequiresTwentySevenDaysAndAdjustsOnlyOnce(int qualifyingDays, string status, decimal amount)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        f.Clock.Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
        var csv = PerformanceCsv(Enumerable.Range(1, 31).Select(day => ("272578", new DateOnly(2026, 10, day), day <= 30 ? 15 : 0, day <= qualifyingDays ? "8" : "7.99972222")).ToArray());
        await ImportCsv(f, csv);
        var row = Assert.Single(Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 10 }, ct)).Items);
        Assert.Equal(450m, row.PeriodTarget); Assert.Equal(450m, row.Evaluated.Target);
        Assert.Equal(27, row.Attendance.RequiredQualifyingDays); Assert.Equal(status, row.Attendance.Status);
        Assert.Equal(amount, row.SuggestedMoney!.SuggestedAmount); Assert.Equal(5700, row.SuggestedMoney.OrderAmount);
        Assert.Equal(amount, row.Forecast!.SuggestedMoney!.SuggestedAmount); Assert.Null(row.Forecast.RequiredDailyRate);
        Assert.Equal("PeriodEnded", row.Forecast.Assumption);
    }
    [Fact]
    public async Task EvaluationOctoberProjectionUsesCalendarDaysAndShowsSeparateTodayTarget()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        await ImportCsv(f, PerformanceCsv(Enumerable.Range(1, 24).Select(day => ("272578", new DateOnly(2026, 10, day), 15, "8")).ToArray()));
        var row = Assert.Single(Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 10 }, ct)).Items);
        Assert.Equal(new DateOnly(2026, 10, 24), row.LatestCompleteDate); Assert.Equal(2, row.DataDelayDays);
        Assert.Equal(450m * 24 / 31, row.Evaluated.Target); Assert.Equal(450m * 26 / 31, row.TargetDueToday);
        Assert.Equal(465m, row.Forecast!.ProjectedOrders); Assert.Equal(5865m, row.Forecast.SuggestedMoney!.SuggestedAmount);
        Assert.Equal(90m / 7, row.Forecast.RequiredDailyRate); Assert.True(row.Forecast.Reliable);
        Assert.Equal("ContinueCurrentAssignment", row.Forecast.Assumption);
    }
    [Fact]
    public async Task EvaluationSharedDayTargetUsesPlannedWeightsAndEligibilityStaysUnknown()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        var date = new DateOnly(2026, 10, 12);
        var first = Value(await f.Service.AssignAsync(f.Account.Id, new(f.Rider.Id, date, "0501111111", "initial", RiderType: "InternalMotorcycle", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ReturnAsync(f.Account.Id, new(date, "0501111111", null, "Returned", first.Assignment.RowVersion, DayUsage: new(8, 3)), ct));
        var second = Value(await f.Service.AssignAsync(f.Account.Id, new(f.OwnerRider.Id, date, "0502222222", RiderType: "InternalMotorcycle", PaymentModel: "PayPerOrder"), ct));
        Value(await f.Service.ImportAsync(false, date, "daily.xlsx", Sheet(false, "272578", 20, 8), ct));
        var service = Evaluation(f); var query = new NinjaEvaluationQuery { Year = 2026, Month = 10 };
        Assert.All(Value(await service.EvaluateAsync(query, ct)).Items, r => Assert.Contains("TargetWeightUnresolved", r.Reasons));
        Value(await service.SetResponsibilityAsync(f.Account.Id, new(2026, 10, null,
            [new(first.Assignment.Id, date, .5m), new(second.Assignment.Id, date, .5m)], "Planned half-day handover"), ct));
        var rows = Value(await service.EvaluateAsync(query, ct)).Items;
        var earlier = rows.Single(r => r.RiderProfileId == f.Rider.Id);
        var later = rows.Single(r => r.RiderProfileId == f.OwnerRider.Id);
        var earlierDay = earlier.Days.Single(d => d.Date == date); var laterDay = later.Days.Single(d => d.Date == date);
        Assert.Equal(8, earlierDay.Orders); Assert.Equal(12, laterDay.Orders);
        Assert.Equal(3, earlierDay.Hours); Assert.Equal(5, laterDay.Hours);
        Assert.Equal(450m * .5m / 31, earlierDay.Target); Assert.Equal(earlierDay.Target, laterDay.Target);
        Assert.Equal("PartialShiftUnknown", earlierDay.HoursStatus); Assert.Equal("PartialShiftUnknown", laterDay.HoursStatus);
        Assert.Equal("Declared", earlierDay.AllocationOrigin); Assert.Equal("Remainder", laterDay.AllocationOrigin);
        Assert.Equal("Unknown", earlier.Attendance.Status); Assert.False(earlier.SuggestedMoney!.AttendanceReduction);
    }
    [Fact]
    public async Task EvaluationMixedPricingIsProrated()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        await ImportCsv(f, PerformanceCsv(Enumerable.Range(1, 30).Select(day => ("272578", new DateOnly(2026, 9, day), 16, "8")).ToArray()));
        var service = Evaluation(f);
        Value(await service.SetContextAsync(f.Account.Id, new(null, new(2026, 9, 1),
            [new(new(2026, 9, 1), new(2026, 9, 10), "BIKE"), new(new(2026, 9, 11), new(2026, 9, 30), "CAR")], "Verified mixed pricing"), ct));
        var row = Assert.Single(Value(await service.EvaluateAsync(new() { Year = 2026, Month = 9 }, ct)).Items);
        Assert.Equal(2, row.SuggestedMoney!.Segments.Count);
        Assert.Equal(1900m + 6500m * 20 / 30, row.SuggestedMoney.Segments.Sum(s => s.Calculation.Base));
        Assert.True((await service.SetContextAsync(f.Account.Id, new(null, null, [], "Stale revision"), ct)).IsFailure);
        Assert.True((await service.SetContextAsync(f.Account.Id, new((await f.Db.NinjaEvaluationContexts.SingleAsync(ct)).Id, new(2026, 9, 2), [], "Wrong first delivery"), ct)).IsFailure);
    }
    [Fact]
    public async Task EvaluationOverlappingRiderResponsibilityWithholdsCombinedMoney()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        var first = await f.Db.RiderClientAssignments.SingleAsync(ct);
        var second = new PlatformRiderAccount { ClientPlatformId = f.Account.ClientPlatformId, Code = "NINJA-LEGACY-2", ExternalAccountId = "000123", OperatingCityId = f.Account.OperatingCityId };
        f.Db.Add(second); f.Db.Add(new RiderClientAssignment { PlatformRiderAccountId = second.Id, ClientContractId = first.ClientContractId,
            RiderProfileId = f.Rider.Id, EffectiveFrom = new(2026, 9, 1), Status = RiderAssignmentStatus.Active, NinjaRiderType = NinjaRiderType.InternalMotorcycle });
        await f.Db.SaveChangesAsync(ct);
        await ImportCsv(f, PerformanceCsv(("272578", new(2026, 9, 1), 15, "8"), ("000123", new(2026, 9, 1), 15, "8")));
        var row = Assert.Single(Value(await Evaluation(f).EvaluateAsync(new() { Year = 2026, Month = 9 }, ct)).Items);
        Assert.Equal(30, row.Observed.Orders); Assert.Equal(2, row.AccountIds.Count);
        Assert.Null(row.SuggestedMoney); Assert.Null(row.Forecast); Assert.Contains("OverlappingRiderResponsibility", row.Reasons);
    }
    [Fact]
    public async Task EvaluationPreviewExpiryAndFileMismatchDoNotWritePerformance()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken; await SeptemberAssignment(f);
        var csv = PerformanceCsv(("272578", new(2026, 9, 1), 15, "8")); var imports = PerformanceImports(f);
        var preview = Value(await imports.PreviewAsync("performance.csv", csv, new("MultiDay"), ct));
        Assert.True((await imports.ConfirmAsync(preview.PreviewId, "performance.csv", csv.Append((byte)10).ToArray(), ct)).IsFailure);
        f.Clock.Now = f.Clock.Now.AddMinutes(31);
        Assert.Equal("ninja.preview_expired", (await imports.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct)).Error.Code);
        f.Checker.Denied = PermissionKeys.Ninja.PerformanceImport;
        Assert.Equal("ninja.forbidden", (await imports.ConfirmAsync(preview.PreviewId, "performance.csv", csv, ct)).Error.Code);
        Assert.Empty(f.Db.NinjaDailyPerformance); Assert.Empty(f.Db.NinjaPerformanceArtifacts);
    }
}
