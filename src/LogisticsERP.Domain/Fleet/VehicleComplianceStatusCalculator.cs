using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Fleet;

public static class VehicleComplianceStatusCalculator
{
    public static VehicleComplianceDueStatus Calculate(DateOnly? expiryDate, DateOnly checkDate, int alertDays = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(alertDays);
        if (expiryDate is null) return VehicleComplianceDueStatus.Missing;

        var daysUntilExpiry = expiryDate.Value.DayNumber - checkDate.DayNumber;
        return daysUntilExpiry switch
        {
            < 0 => VehicleComplianceDueStatus.Expired,
            0 => VehicleComplianceDueStatus.DueToday,
            _ when daysUntilExpiry <= alertDays => VehicleComplianceDueStatus.Upcoming,
            _ => VehicleComplianceDueStatus.Valid
        };
    }
}
