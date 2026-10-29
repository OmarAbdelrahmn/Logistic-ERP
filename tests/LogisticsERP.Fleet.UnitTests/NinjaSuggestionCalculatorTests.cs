using LogisticsERP.Domain.Ninja;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class NinjaSuggestionCalculatorTests
{
    [Theory]
    [InlineData("BIKE", 0, 0)]
    [InlineData("BIKE", 450, 5700)]
    [InlineData("BIKE", 405, 4980)]
    [InlineData("BIKE", 404, 4918)]
    [InlineData("BIKE", 360, 4170)]
    [InlineData("BIKE", 359, 4062)]
    [InlineData("BIKE", 315, 3270)]
    [InlineData("BIKE", 314, 2826)]
    [InlineData("BIKE", 290, 2610)]
    [InlineData("BIKE", 600, 7350)]
    [InlineData("CAR", 0, 0)]
    [InlineData("CAR", 460, 6500)]
    [InlineData("CAR", 414, 5488)]
    [InlineData("CAR", 413, 5442.5)]
    [InlineData("CAR", 368, 4430)]
    [InlineData("CAR", 367, 4361)]
    [InlineData("CAR", 322, 3326)]
    [InlineData("CAR", 321, 3210)]
    [InlineData("CAR", 290, 2900)]
    [InlineData("CAR", 600, 8180)]
    public void ContractBandsApplyOneRateToEntireShortage(string type, int orders, decimal expected)
    {
        var result = NinjaSuggestionCalculator.Orders(type, 30, 30, orders);
        Assert.NotNull(result); Assert.Equal(expected, NinjaSuggestionCalculator.Money(result.Amount));
    }

    [Theory]
    [InlineData(28, 24)] [InlineData(29, 25)] [InlineData(30, 26)] [InlineData(31, 27)]
    public void CalendarMonthUsesExactTargetAndAttendance(int days, int minimum)
    {
        Assert.Equal(450m, NinjaSuggestionCalculator.Orders("BIKE", days, days, 450)!.Target);
        Assert.Equal("Pass", NinjaSuggestionCalculator.Attendance(days, minimum, 0));
        Assert.Equal("Fail", NinjaSuggestionCalculator.Attendance(days, minimum - 1, 0));
        Assert.Equal("Unknown", NinjaSuggestionCalculator.Attendance(days, minimum - 1, 1));
    }
    [Fact]
    public void RiderPeriodsProduceIndependentSuggestionsAndSingleAttendanceAdjustment()
    {
        Assert.Equal(2010, NinjaSuggestionCalculator.Orders("BIKE", 30, 10, 160)!.Amount);
        Assert.Equal(3480, NinjaSuggestionCalculator.Orders("BIKE", 30, 20, 280)!.Amount);
        Assert.Equal(5540, NinjaSuggestionCalculator.Orders("BIKE", 30, 30, 440)!.Amount);
        Assert.Equal(4233, NinjaSuggestionCalculator.Adjust(4980, "Fail"));
        Assert.Equal(4980, NinjaSuggestionCalculator.Adjust(4980, "Unknown"));
        Assert.Null(NinjaSuggestionCalculator.Orders("BIKE", 31, 0, 0));
        Assert.Null(NinjaSuggestionCalculator.Orders(null, 31, 1, 1));
        Assert.Equal(1.01m, NinjaSuggestionCalculator.Money(1.005m));
    }
    [Theory]
    [InlineData("BIKE", 450)] [InlineData("CAR", 460)]
    public void TargetAdjacentOrdersAndFractionalForecastsHaveNoBandGaps(string type, int target)
    {
        Assert.Equal("Shortage10", NinjaSuggestionCalculator.Orders(type, 31, 31, target - 1)!.Band);
        Assert.Equal("TargetAchieved", NinjaSuggestionCalculator.Orders(type, 31, 31, target)!.Band);
        Assert.Equal("AboveTarget", NinjaSuggestionCalculator.Orders(type, 31, 31, target + 1)!.Band);
        Assert.Equal("Shortage20", NinjaSuggestionCalculator.Orders(type, 31, 31, target * .8999m)!.Band);
    }
}
