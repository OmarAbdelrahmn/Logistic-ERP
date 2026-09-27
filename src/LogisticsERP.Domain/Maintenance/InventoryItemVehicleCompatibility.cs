using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Maintenance;

public static class InventoryItemVehicleCompatibility
{
    public const int AllVehicleTypesMask = 31;

    public static int For(VehicleType vehicleType) => 1 << ((int)vehicleType - 1);

    public static bool IsValid(IReadOnlyList<VehicleType>? vehicleTypes) =>
        vehicleTypes is { Count: > 0 } && vehicleTypes.All(vehicleType => Enum.IsDefined(vehicleType));

    public static int ToMask(IReadOnlyList<VehicleType> vehicleTypes) =>
        vehicleTypes.Aggregate(0, (mask, vehicleType) => mask | For(vehicleType));

    public static VehicleType[] FromMask(int mask) =>
        Enum.GetValues<VehicleType>().Where(vehicleType => (mask & For(vehicleType)) != 0).ToArray();

    public static bool Allows(int mask, VehicleType? vehicleType) =>
        mask == AllVehicleTypesMask || vehicleType.HasValue && (mask & For(vehicleType.Value)) != 0;
}
