using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Infrastructure.Fleet;

internal static class FleetBusinessRules
{
    private static readonly VehicleFileKind[] PhotoSlots =
        [VehicleFileKind.FrontImage, VehicleFileKind.RearImage, VehicleFileKind.LeftImage, VehicleFileKind.RightImage];

    public static DateOnly RiyadhDate(DateTimeOffset timestamp) =>
        DateOnly.FromDateTime(timestamp.ToOffset(TimeSpan.FromHours(3)).DateTime);

    public static DateOnly PermitEnd(DateOnly start) => start.AddYears(1).AddDays(-1);

    public static bool IsCoreIdentityReady(Vehicle vehicle) =>
        !string.IsNullOrWhiteSpace(vehicle.SerialNumber)
        && !string.IsNullOrWhiteSpace(vehicle.NormalizedSerialNumber)
        && !string.IsNullOrWhiteSpace(vehicle.ChassisNumber)
        && !string.IsNullOrWhiteSpace(vehicle.NormalizedChassisNumber)
        && !string.IsNullOrWhiteSpace(vehicle.PlateNumberAr)
        && !string.IsNullOrWhiteSpace(vehicle.PlateNumberEn)
        && vehicle.SponsorId.HasValue
        && vehicle.OperatingCityId.HasValue
        && vehicle.RegistrationType.HasValue
        && (vehicle.OwnershipType != VehicleOwnershipType.Owned || vehicle.PurchasedFromSupplierId.HasValue);

    public static (VehicleFileKind[] MissingPhotos, VehicleFileKind[] MissingDocuments) MissingFiles(
        VehicleRegistrationType? registrationType,
        IReadOnlyCollection<VehicleFileKind> present)
    {
        var missingPhotos = PhotoSlots.Where(slot => !present.Contains(slot)).ToArray();
        var documents = registrationType == VehicleRegistrationType.PublicTransport
            ? new[] { VehicleFileKind.Istimara, VehicleFileKind.OperationCard }
            : new[] { VehicleFileKind.Istimara };
        return (missingPhotos, documents.Where(slot => !present.Contains(slot)).ToArray());
    }
}
