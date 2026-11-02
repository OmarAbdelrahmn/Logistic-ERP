using LogisticsERP.Domain.Entities.Ninja;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed partial class NinjaWorkflowTests
{
    [Theory]
    [InlineData(3, "Valid", 17)]
    [InlineData(4, "Invalid", 16)]
    public async Task AccountAttendanceUses85PercentOfCompletedDaysAndEightHourMinimum(int missed, string status, int qualified)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        // Sep 6-9 are Sunday-Wednesday: below eight hours does not qualify.
        await SeedAccountAttendance(f, 1, 20, d => d >= 6 && d < 6 + missed ? 7.99m : 8m);
        f.Clock.Now = new(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        var attendance = account.Attendance!;
        Assert.Equal(status, account.OperationalValidity);
        Assert.Equal(status == "Valid", account.IsValidForOperation);
        Assert.Equal(20, attendance.EvaluatedDays);
        Assert.Equal(qualified, attendance.QualifiedDays);
        Assert.Equal(17, attendance.RequiredQualifiedDays);
        Assert.Equal((decimal)qualified / 20, attendance.AttendanceRate);
        Assert.Equal(missed, attendance.ShortHoursDays);
        Assert.Empty(attendance.ProtectedDayAbsences);
        Assert.Equivalent(attendance, Assert.Single(Value(await f.Service.GetAccountsAsync(1, 50, ct)).Items).Attendance);
        if (status == "Valid")
        {
            var suspended = Value(await f.Service.SetAccountStatusAsync(f.Account.Id, new("Suspended", account.RowVersion), ct));
            Assert.True(suspended.IsValidForOperation);
            Assert.True((suspended with { IsOperationallyBlocked = true }).IsValidForOperation);
        }
        else Assert.Contains("AttendanceBelow85Percent", attendance.Reasons);
    }

    [Theory]
    [InlineData(3)] // Thursday
    [InlineData(4)] // Friday
    [InlineData(5)] // Saturday
    public async Task AccountAttendanceMissingProtectedDayFailsEvenAbove85Percent(int absentDay)
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await SeedAccountAttendance(f, 1, 20, _ => 8m, absentDay);
        f.Clock.Now = new(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.False(account.IsValidForOperation);
        Assert.Equal("غير صالح", account.OperationalValidityAr);
        Assert.Equal(.95m, account.Attendance!.AttendanceRate);
        Assert.Equal(1, account.Attendance.AbsentDays);
        Assert.Equal(new DateOnly(2026, 9, absentDay), Assert.Single(account.Attendance.ProtectedDayAbsences));
        Assert.Equal("LeaveOnProtectedDay", Assert.Single(account.Attendance.Reasons));
    }

    [Fact]
    public async Task AccountAttendanceMissingOrdinaryDayIsAbsenceButZeroOrdersWithEightHoursIsPresent()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await SeedAccountAttendance(f, 1, 20, _ => 8m, missing: 6, zeroOrders: 4);
        f.Clock.Now = new(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.True(account.IsValidForOperation);
        Assert.Equal("صالح", account.OperationalValidityAr);
        Assert.Equal(19, account.Attendance!.QualifiedDays);
        Assert.Equal(1, account.Attendance.AbsentDays);
        Assert.Empty(account.Attendance.ProtectedDayAbsences);
    }

    [Fact]
    public async Task AccountAttendanceStartsAtFirstDeliveryAndResetsAtRiyadhMonthBoundaryWithoutDeletingHistory()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await SeedAccountAttendance(f, 10, 14, _ => 8m);
        f.Clock.Now = new(2026, 9, 14, 21, 0, 0, TimeSpan.Zero); // Sep 15 Riyadh
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.True(account.IsValidForOperation);
        Assert.Equal(new DateOnly(2026, 9, 10), account.Attendance!.EvaluatedFrom);
        Assert.Equal(new DateOnly(2026, 9, 14), account.Attendance.EvaluatedThrough);
        Assert.Equal(5, account.Attendance.EvaluatedDays);
        f.Clock.Now = new(2026, 9, 30, 21, 0, 0, TimeSpan.Zero); // Oct 1 Riyadh
        var reset = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.Null(reset.IsValidForOperation);
        Assert.Equal("Pending", reset.OperationalValidity);
        Assert.Equal("2026-10", reset.Attendance!.Month);
        Assert.Equal(0, reset.Attendance.EvaluatedDays);
        Assert.Equal(0, reset.Attendance.AbsentDays);
        Assert.Null(reset.Attendance.EvaluatedThrough);
        Assert.Equal(5, f.Db.Set<NinjaDailyPerformanceSource>().Count());
        f.Clock.Now = f.Clock.Now.AddDays(1);
        var missing = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.False(missing.IsValidForOperation);
        Assert.Equal(1, missing.Attendance!.AbsentDays);
        Assert.Equal(new DateOnly(2026, 10, 1), Assert.Single(missing.Attendance.ProtectedDayAbsences));
    }

    [Fact]
    public async Task AccountAttendanceUsesConfirmedFirstDeliveryAndLegacySharesWithoutDoubleCountingSources()
    {
        await using var f = await Fixture.Create(); var ct = TestContext.Current.CancellationToken;
        await SeedAccountAttendance(f, 10, 10, _ => 4m);
        f.Db.Set<NinjaEvaluationContextRevision>().Add(new() { PlatformRiderAccountId = f.Account.Id, Revision = 1,
            ConfirmedFirstDeliveryDate = new(2026, 9, 8) });
        // The source on Sep 10 takes precedence over its assignment shares.
        f.Db.NinjaDailyPerformance.AddRange(
            new NinjaDailyPerformance { PlatformRiderAccountId = f.Account.Id, ReportDate = new(2026, 9, 10), TotalOrders = 1, TotalWorkingHours = 8 },
            new NinjaDailyPerformance { PlatformRiderAccountId = f.Account.Id, ReportDate = new(2026, 9, 9), TotalOrders = 1, TotalWorkingHours = 4 },
            new NinjaDailyPerformance { PlatformRiderAccountId = f.Account.Id, ReportDate = new(2026, 9, 9), TotalOrders = 1, TotalWorkingHours = 4 });
        await f.Db.SaveChangesAsync(ct);
        f.Clock.Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
        var account = Value(await f.Service.GetAccountAsync(f.Account.Id, ct));
        Assert.False(account.IsValidForOperation);
        Assert.Equal(1, account.Attendance!.QualifiedDays);
        Assert.Equal(1, account.Attendance.ShortHoursDays);
        Assert.Equal(1, account.Attendance.AbsentDays);
        Assert.Equal(new DateOnly(2026, 9, 8), account.Attendance.FirstDeliveryDate);
    }

    private static async Task SeedAccountAttendance(Fixture f, int from, int to, Func<int, decimal> hours, int? missing = null, int? zeroOrders = null)
    {
        for (var day = from; day <= to; day++)
            if (day != missing) f.Db.Set<NinjaDailyPerformanceSource>().Add(new()
            {
                PlatformRiderAccountId = f.Account.Id, ReportDate = new(2026, 9, day),
                CaptainId = f.Account.ExternalAccountId, TotalOrders = day == zeroOrders ? 0 : 1, TotalWorkingHours = hours(day)
            });
        await f.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
