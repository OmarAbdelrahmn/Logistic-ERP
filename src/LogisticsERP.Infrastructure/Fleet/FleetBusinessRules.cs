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

    public static bool RequiresReturnConditionReport(VehicleCondition condition) =>
        condition != VehicleCondition.Good;

    public static bool IsValidReturnConditionReport(
        VehicleCondition condition,
        bool hasReport,
        VehicleIssueCategory? category,
        VehicleIssueSeverity? severity,
        string? problemDescription,
        decimal? estimatedRepairCost,
        int evidenceFileCount)
    {
        if (!Enum.IsDefined(condition)) return false;
        if (!RequiresReturnConditionReport(condition)) return !hasReport && evidenceFileCount == 0;

        return hasReport
            && category.HasValue
            && Enum.IsDefined(category.Value)
            && severity.HasValue
            && Enum.IsDefined(severity.Value)
            && !string.IsNullOrWhiteSpace(problemDescription)
            && problemDescription.Trim().Length <= 4000
            && estimatedRepairCost is >= 0 and <= 9999999999999999.99m
            && evidenceFileCount is >= 1 and <= 2;
    }

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
