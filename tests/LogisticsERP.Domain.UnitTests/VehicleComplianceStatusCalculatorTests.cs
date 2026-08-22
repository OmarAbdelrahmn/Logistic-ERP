using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class VehicleComplianceStatusCalculatorTests
{
    private static readonly DateOnly CheckDate = new(2026, 8, 22);

    [Fact]
    public void CalculateReturnsMissingWhenExpiryIsAbsent() =>
        Assert.Equal(VehicleComplianceDueStatus.Missing, VehicleComplianceStatusCalculator.Calculate(null, CheckDate));

    [Theory]
    [InlineData(-1, VehicleComplianceDueStatus.Expired)]
    [InlineData(0, VehicleComplianceDueStatus.DueToday)]
    [InlineData(1, VehicleComplianceDueStatus.Upcoming)]
    [InlineData(7, VehicleComplianceDueStatus.Upcoming)]
    [InlineData(30, VehicleComplianceDueStatus.Upcoming)]
    [InlineData(31, VehicleComplianceDueStatus.Valid)]
    public void CalculateUsesInclusiveBoundaryRules(int daysFromCheckDate, VehicleComplianceDueStatus expected)
    {
        var expiry = CheckDate.AddDays(daysFromCheckDate);

        Assert.Equal(expected, VehicleComplianceStatusCalculator.Calculate(expiry, CheckDate));
    }

    [Fact]
    public void CalculateUsesRequestedAlertWindow() =>
        Assert.Equal(
            VehicleComplianceDueStatus.Valid,
            VehicleComplianceStatusCalculator.Calculate(CheckDate.AddDays(8), CheckDate, alertDays: 7));

    [Fact]
    public void CalculateRejectsNegativeAlertWindow() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VehicleComplianceStatusCalculator.Calculate(CheckDate, CheckDate, alertDays: -1));
}
