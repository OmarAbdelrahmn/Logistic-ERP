using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Entities.Fleet;

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

    public static decimal CalculateManualDistance(decimal baselineOdometer, long currentOdometer)
    {
        if (baselineOdometer < 0 || currentOdometer < baselineOdometer)
        {
            throw new ArgumentOutOfRangeException(nameof(currentOdometer));
        }

        return currentOdometer - baselineOdometer;
    }

    public static decimal CalculateTotalAdjustment(decimal previousAppliedKm, decimal nextAppliedKm) =>
        nextAppliedKm - previousAppliedKm;

    public static bool TryRecalculate(
        IEnumerable<VehicleDailyDistance> orderedDistances,
        decimal baselineOdometerKm,
        out decimal effectiveOdometerKm)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baselineOdometerKm);
        effectiveOdometerKm = baselineOdometerKm;

        foreach (var distance in orderedDistances)
        {
            if (distance.ManualOdometerReading.HasValue)
            {
                if (distance.ManualOdometerReading.Value < effectiveOdometerKm)
                {
                    return false;
                }

                distance.ManualBaselineOdometerReading = effectiveOdometerKm;
                distance.ManualDistanceKm = CalculateManualDistance(
                    effectiveOdometerKm,
                    distance.ManualOdometerReading.Value);
            }

            var selected = SelectAppliedDistance(distance.GpsDistanceKm, distance.ManualDistanceKm);
            distance.AppliedDistanceKm = selected.DistanceKm;
            distance.AppliedSource = selected.Source;
            effectiveOdometerKm += selected.DistanceKm;
            distance.EffectiveOdometerAfterKm = effectiveOdometerKm;
        }

        return true;
    }
}
