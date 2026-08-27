using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Workforce;
using Xunit;

namespace LogisticsERP.Domain.UnitTests;

public sealed class EmployeeExpiryComplianceStatusCalculatorTests
{
    private static readonly DateOnly CheckDate = new(2026, 8, 27);

    [Fact]
    public void CalculateReturnsMissingWhenARequiredExpiryIsAbsent() =>
        Assert.Equal(EmployeeExpiryComplianceDueStatus.Missing, EmployeeExpiryComplianceStatusCalculator.Calculate(null, CheckDate));

    [Theory]
    [InlineData(-1, EmployeeExpiryComplianceDueStatus.Expired)]
    [InlineData(0, EmployeeExpiryComplianceDueStatus.DueToday)]
    [InlineData(1, EmployeeExpiryComplianceDueStatus.Upcoming)]
    [InlineData(7, EmployeeExpiryComplianceDueStatus.Upcoming)]
    [InlineData(30, EmployeeExpiryComplianceDueStatus.Upcoming)]
    [InlineData(31, EmployeeExpiryComplianceDueStatus.Valid)]
    public void CalculateUsesTheEmployeeExpiryBoundaries(int daysFromCheckDate, EmployeeExpiryComplianceDueStatus expected) =>
        Assert.Equal(expected, EmployeeExpiryComplianceStatusCalculator.Calculate(CheckDate.AddDays(daysFromCheckDate), CheckDate));

    [Fact]
    public void CalculateHonorsCustomReminderWindows() =>
        Assert.Equal(EmployeeExpiryComplianceDueStatus.Valid, EmployeeExpiryComplianceStatusCalculator.Calculate(CheckDate.AddDays(8), CheckDate, 7));
}
