using LogisticsERP.Domain.Hunger;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class HungerAccountUsageRulesTests
{
    [Fact]
    public void UsageStaysUnknownWithoutObservedWorkingDays()
    {
        var result = HungerAccountUsageRules.Evaluate(false);
        Assert.Equal(("Unknown", "None"), result);
    }
    [Fact]
    public void ObservedWorkingDaysEstablishUsage()
    {
        var result = HungerAccountUsageRules.Evaluate(true);
        Assert.Equal(("Used", "ObservedWorkingHours"), result);
    }
}
