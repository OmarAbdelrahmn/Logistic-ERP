using LogisticsERP.Domain.Entities.Fleet;

namespace LogisticsERP.Domain.Fleet;

public static class VehicleMileageRules
{
    public static void ApplyVerifiedReading(Vehicle vehicle, long reading, DateTimeOffset recordedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(reading);

        vehicle.CurrentOdometer = reading;
        vehicle.TrackedDistanceKm = reading;
        vehicle.LastOdometerAtUtc = recordedAtUtc;
    }

    public static void ApplyEffectiveMileage(Vehicle vehicle, decimal effectiveMileageKm, DateTimeOffset recordedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(effectiveMileageKm);

        vehicle.TrackedDistanceKm = effectiveMileageKm;
        vehicle.CurrentOdometer = decimal.ToInt64(decimal.Floor(effectiveMileageKm));
        if (!vehicle.LastOdometerAtUtc.HasValue || recordedAtUtc >= vehicle.LastOdometerAtUtc.Value)
        {
            vehicle.LastOdometerAtUtc = recordedAtUtc;
        }
    }
}
