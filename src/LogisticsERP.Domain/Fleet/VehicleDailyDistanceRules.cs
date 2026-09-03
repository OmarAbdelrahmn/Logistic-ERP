using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Fleet;

public static class VehicleDailyDistanceRules
{
    public static (decimal DistanceKm, VehicleDailyDistanceSource Source) SelectAppliedDistance(
        decimal? gpsDistanceKm,
        decimal? manualDistanceKm) =>
        gpsDistanceKm.HasValue
            ? (gpsDistanceKm.Value, VehicleDailyDistanceSource.Gps)
            : manualDistanceKm.HasValue
                ? (manualDistanceKm.Value, VehicleDailyDistanceSource.Manual)
                : (0m, VehicleDailyDistanceSource.None);

    public static decimal CalculateManualDistance(long baselineOdometer, long currentOdometer)
    {
        if (baselineOdometer < 0 || currentOdometer < baselineOdometer)
        {
            throw new ArgumentOutOfRangeException(nameof(currentOdometer));
        }

        return currentOdometer - baselineOdometer;
    }

    public static decimal CalculateTotalAdjustment(decimal previousAppliedKm, decimal nextAppliedKm) =>
        nextAppliedKm - previousAppliedKm;
}
