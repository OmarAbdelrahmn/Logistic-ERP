using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Fleet;

public static class VehiclePlatformAccountAssignmentPolicy
{
    public const string KeetaPlatformCode = "KEETA";
    public const int CarMaximumAccountsPerPlatformAndCity = 2;
    public const int MotorcycleMaximumAccountsPerPlatformAndCity = 3;

    public static int? GetMaximumAccounts(VehicleType vehicleType) => vehicleType switch
    {
        VehicleType.Car => CarMaximumAccountsPerPlatformAndCity,
        VehicleType.Motorcycle => MotorcycleMaximumAccountsPerPlatformAndCity,
        _ => null
    };

    public static bool IsCapacityExceeded(VehicleType vehicleType, int approvalOrdinal)
    {
        var maximum = GetMaximumAccounts(vehicleType);
        return maximum.HasValue && approvalOrdinal > maximum.Value;
    }

    public static bool IsOperationalStatusAllowed(VehicleOperationalStatus status) =>
        status is VehicleOperationalStatus.Available or VehicleOperationalStatus.Assigned;

    public static bool IsPlatformAccountStatusAllowed(PlatformRiderAccountStatus status) =>
        status is PlatformRiderAccountStatus.Available or PlatformRiderAccountStatus.Assigned;

    public static bool IsSponsorCompatible(
        Guid? vehicleSponsorId,
        Guid accountSponsorId,
        bool hasApplicableLeaseAgreement) =>
        vehicleSponsorId.HasValue
        && (vehicleSponsorId.Value == accountSponsorId || hasApplicableLeaseAgreement);
}
