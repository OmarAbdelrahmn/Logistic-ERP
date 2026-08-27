using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Workforce;

public static class EmployeeExpiryComplianceStatusCalculator
{
    public static EmployeeExpiryComplianceDueStatus Calculate(DateOnly? expiryDate, DateOnly checkDate, int alertDays = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(alertDays);
        if (!expiryDate.HasValue) return EmployeeExpiryComplianceDueStatus.Missing;

        var daysRemaining = expiryDate.Value.DayNumber - checkDate.DayNumber;
        if (daysRemaining < 0) return EmployeeExpiryComplianceDueStatus.Expired;
        if (daysRemaining == 0) return EmployeeExpiryComplianceDueStatus.DueToday;
        return daysRemaining <= alertDays ? EmployeeExpiryComplianceDueStatus.Upcoming : EmployeeExpiryComplianceDueStatus.Valid;
    }
}
