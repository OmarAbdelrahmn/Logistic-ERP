using LogisticsERP.Domain.Hunger;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerAccountUsageRulesTests
{
    [Fact]
    public void SourceReportedRiderMakesAccountUsedInObservedMonth()
    {
        var result = HungerAccountUsageRules.Evaluate(new(2026, 11, 4), false, "يعمل", "1234567890", new(2026, 11, 1), new(2026, 11, 4));
        Assert.Equal(("Used", "ImportedSource"), result);
    }
    [Fact]
    public void PreviousMonthSourceDoesNotProveCurrentMonthUsage()
    {
        var result = HungerAccountUsageRules.Evaluate(new(2026, 12, 1), false, "يعمل", "1234567890", new(2026, 11, 1), new(2026, 11, 4));
        Assert.Equal(("Unknown", "None"), result);
    }
    [Theory]
    [InlineData("الشهر الجديد")]
    [InlineData("مشاكل")]
    [InlineData("لايعمل")]
    public void ExceptionsAreNotMarkedUsedBySourceAlone(string sourceStatus)
    {
        var result = HungerAccountUsageRules.Evaluate(new(2026, 11, 4), false, sourceStatus, "1234567890", new(2026, 11, 1), new(2026, 11, 4));
        Assert.NotEqual("Used", result.Status);
    }
    [Fact]
    public void ActualAssignmentWinsOverSource()
    {
        var result = HungerAccountUsageRules.Evaluate(new(2026, 11, 4), true, null, null, null, null);
        Assert.Equal(("Used", "Assignment"), result);
    }
}
