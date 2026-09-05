using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Xunit;

namespace LogisticsERP.Fleet.UnitTests;

public sealed class MaintenanceBusinessRulesTests
{
    [Theory]
    [InlineData(VehicleType.Car, 4000, 5000)]
    [InlineData(VehicleType.Motorcycle, 800, 1000)]
    public void OilChangeWindowsMatchFleetPolicy(VehicleType vehicleType, long reminder, long maximum)
    {
        var window = MaintenanceBusinessRules.GetOilChangeWindow(vehicleType);

        Assert.NotNull(window);
        Assert.Equal(reminder, window.Value.ReminderAfterKilometers);
        Assert.Equal(maximum, window.Value.MaximumAfterKilometers);
    }

    [Theory]
    [InlineData(3999, MaintenanceDueStatus.Ok)]
    [InlineData(4000, MaintenanceDueStatus.Due)]
    [InlineData(4999, MaintenanceDueStatus.Due)]
    [InlineData(5000, MaintenanceDueStatus.Overdue)]
    public void CarOilDueStatusUsesInclusiveThresholds(long distance, MaintenanceDueStatus expected) =>
        Assert.Equal(expected, MaintenanceBusinessRules.GetOilDueStatus(VehicleType.Car, 10_000 + distance, 10_000));

    [Theory]
    [InlineData(false, 3.5)]
    [InlineData(true, 4.0)]
    public void CarOilQuantityDependsOnFilterChange(bool filterChanged, decimal expectedLiters) =>
        Assert.Equal(expectedLiters, MaintenanceBusinessRules.ResolveOilQuantityLiters(VehicleType.Car, filterChanged, 99m));

    [Fact]
    public void BarrelCostIsConvertedToCostPerLiter()
    {
        Assert.Equal(5m, MaintenanceBusinessRules.CalculateBaseUnitCost(1040m, 208m));
    }

    [Fact]
    public void StandardBarrelHasTwoPointFivePercentLossAllowance()
    {
        Assert.Equal(0.025m, MaintenanceBusinessRules.OilBarrelLossRate);
        Assert.Equal(5.2m, MaintenanceBusinessRules.CalculateOilBarrelLossAllowance(208m));
    }

    [Fact]
    public void AnotherBarrelCannotOpenWhileEightLitersRemain()
    {
        Assert.False(MaintenanceBusinessRules.CanOpenNextOilBarrel(8m));
        Assert.True(MaintenanceBusinessRules.CanOpenNextOilBarrel(0m));
    }

    [Fact]
    public void DifferentBarrelSizesAndPricesKeepIndependentPerLiterCosts()
    {
        Assert.Equal(5m, MaintenanceBusinessRules.CalculateBaseUnitCost(1000m, 200m));
        Assert.Equal(6m, MaintenanceBusinessRules.CalculateBaseUnitCost(1248m, 208m));
    }

    [Fact]
    public void FifoFinishesOldPriceBeforeUsingNewPrice()
    {
        var oldLayer = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var newLayer = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var receivedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var allocations = MaintenanceBusinessRules.AllocateFifo(
        [
            new FifoLayerSnapshot(newLayer, receivedAt.AddDays(1), 2, 20m, 13m),
            new FifoLayerSnapshot(oldLayer, receivedAt, 1, 5m, 10m)
        ], 8m);

        Assert.NotNull(allocations);
        Assert.Collection(
            allocations,
            first =>
            {
                Assert.Equal(oldLayer, first.LayerId);
                Assert.Equal(5m, first.Quantity);
                Assert.Equal(10m, first.UnitCost);
                Assert.Equal(50m, first.Cost);
            },
            second =>
            {
                Assert.Equal(newLayer, second.LayerId);
                Assert.Equal(3m, second.Quantity);
                Assert.Equal(13m, second.UnitCost);
                Assert.Equal(39m, second.Cost);
            });
    }

    [Fact]
    public void FifoRejectsAnIssueLargerThanAvailableStock()
    {
        var result = MaintenanceBusinessRules.AllocateFifo(
            [new FifoLayerSnapshot(Guid.NewGuid(), DateTimeOffset.UtcNow, 1, 2m, 10m)],
            3m);

        Assert.Null(result);
    }

    [Fact]
    public void WorkshopProfitSeparatesSalesLaborAndActualCosts()
    {
        var result = MaintenanceBusinessRules.CalculateProfit(
            partsRevenueBeforeTax: 500m,
            customerLaborRevenueBeforeTax: 200m,
            otherIncomeBeforeTax: 20m,
            fifoInventoryCost: 300m,
            mechanicLaborCost: 80m,
            otherExpense: 10m);

        Assert.Equal(200m, result.PartsGrossProfit);
        Assert.Equal(120m, result.LaborProfit);
        Assert.Equal(330m, result.NetProfitBeforeTax);
    }

    [Fact]
    public void LocationScopeSupportsJeddahInternalAndRiyadhExternalRules()
    {
        Assert.True(MaintenanceBusinessRules.CanServe(true, false, MaintenanceServiceSubjectType.CompanyVehicle));
        Assert.False(MaintenanceBusinessRules.CanServe(true, false, MaintenanceServiceSubjectType.ExternalVehicle));
        Assert.True(MaintenanceBusinessRules.CanServe(true, true, MaintenanceServiceSubjectType.ExternalVehicle));
    }
}
