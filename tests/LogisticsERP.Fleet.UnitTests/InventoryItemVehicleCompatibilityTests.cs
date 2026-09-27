using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class InventoryItemVehicleCompatibilityTests
{
    [Fact]
    public void MultipleVehicleTypesAllowEitherTypeAndRejectOthers()
    {
        var mask = InventoryItemVehicleCompatibility.ToMask([VehicleType.Car, VehicleType.Motorcycle]);

        Assert.True(InventoryItemVehicleCompatibility.Allows(mask, VehicleType.Car));
        Assert.True(InventoryItemVehicleCompatibility.Allows(mask, VehicleType.Motorcycle));
        Assert.False(InventoryItemVehicleCompatibility.Allows(mask, VehicleType.Truck));
        Assert.False(InventoryItemVehicleCompatibility.Allows(mask, null));
        Assert.Equal([VehicleType.Motorcycle, VehicleType.Car], InventoryItemVehicleCompatibility.FromMask(mask));
    }

    [Fact]
    public void UnrestrictedItemsAllowAllKnownAndUnknownVehicleTypes()
    {
        Assert.True(InventoryItemVehicleCompatibility.Allows(InventoryItemVehicleCompatibility.AllVehicleTypesMask, VehicleType.Van));
        Assert.True(InventoryItemVehicleCompatibility.Allows(InventoryItemVehicleCompatibility.AllVehicleTypesMask, null));
    }

    [Fact]
    public void EmptyAndUnknownVehicleTypesAreInvalid()
    {
        Assert.False(InventoryItemVehicleCompatibility.IsValid([]));
        Assert.False(InventoryItemVehicleCompatibility.IsValid([(VehicleType)99]));
        Assert.True(InventoryItemVehicleCompatibility.IsValid([VehicleType.Car, VehicleType.Motorcycle]));
    }
}
