using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Maintenance;

public readonly record struct OilChangeWindow(long ReminderAfterKilometers, long MaximumAfterKilometers);
public readonly record struct FifoLayerSnapshot(Guid LayerId, DateTimeOffset ReceivedAtUtc, long OriginalSequence, decimal RemainingQuantity, decimal UnitCost);
public readonly record struct FifoAllocationResult(Guid LayerId, decimal Quantity, decimal UnitCost, decimal Cost);
public readonly record struct WorkshopProfit(
    decimal PartsGrossProfit,
    decimal LaborProfit,
    decimal NetProfitBeforeTax);

public static class MaintenanceBusinessRules
{
    public const decimal OilBarrelLossRate = 0.02m;

    public static OilChangeWindow? GetOilChangeWindow(VehicleType vehicleType) => vehicleType switch
    {
        VehicleType.Car => new OilChangeWindow(4_000, 5_000),
        VehicleType.Motorcycle => new OilChangeWindow(800, 1_000),
        _ => null
    };

    public static decimal? ResolveOilQuantityLiters(
        VehicleType vehicleType,
        bool oilFilterChanged,
        decimal? configuredQuantityLiters) => vehicleType switch
    {
        VehicleType.Car => oilFilterChanged ? 4.000m : 3.500m,
        _ when configuredQuantityLiters is > 0 => configuredQuantityLiters,
        _ => null
    };

    public static MaintenanceDueStatus GetOilDueStatus(
        VehicleType vehicleType,
        long currentOdometer,
        long? lastOilChangeOdometer)
    {
        if (!lastOilChangeOdometer.HasValue) return MaintenanceDueStatus.NeverDone;
        if (currentOdometer < lastOilChangeOdometer.Value) return MaintenanceDueStatus.OdometerMissing;

        var window = GetOilChangeWindow(vehicleType);
        if (!window.HasValue) return MaintenanceDueStatus.OdometerMissing;

        var distance = currentOdometer - lastOilChangeOdometer.Value;
        if (distance >= window.Value.MaximumAfterKilometers) return MaintenanceDueStatus.Overdue;
        return distance >= window.Value.ReminderAfterKilometers
            ? MaintenanceDueStatus.Due
            : MaintenanceDueStatus.Ok;
    }

    public static bool CanServe(
        bool allowsCompanyVehicles,
        bool allowsExternalVehicles,
        MaintenanceServiceSubjectType subjectType) => subjectType switch
    {
        MaintenanceServiceSubjectType.CompanyVehicle => allowsCompanyVehicles,
        MaintenanceServiceSubjectType.ExternalVehicle => allowsExternalVehicles,
        _ => false
    };

    public static decimal CalculateBaseUnitCost(decimal inventoryValuationAmount, decimal receivedBaseQuantity) =>
        receivedBaseQuantity <= 0
            ? throw new ArgumentOutOfRangeException(nameof(receivedBaseQuantity))
            : decimal.Round(inventoryValuationAmount / receivedBaseQuantity, 6, MidpointRounding.AwayFromZero);

    public static decimal CalculateOilBarrelLossAllowance(decimal nominalCapacityLiters) =>
        nominalCapacityLiters <= 0
            ? throw new ArgumentOutOfRangeException(nameof(nominalCapacityLiters))
            : decimal.Round(nominalCapacityLiters * OilBarrelLossRate, 3, MidpointRounding.AwayFromZero);

    public static bool CanOpenNextOilBarrel(decimal currentOpenBarrelRemainingLiters) =>
        currentOpenBarrelRemainingLiters == 0;

    public static IReadOnlyList<FifoAllocationResult>? AllocateFifo(
        IEnumerable<FifoLayerSnapshot> candidates,
        decimal requestedQuantity)
    {
        if (requestedQuantity <= 0) return null;

        var remaining = requestedQuantity;
        var allocations = new List<FifoAllocationResult>();
        foreach (var layer in candidates
                     .Where(candidate => candidate.RemainingQuantity > 0)
                     .OrderBy(candidate => candidate.ReceivedAtUtc)
                     .ThenBy(candidate => candidate.OriginalSequence)
                     .ThenBy(candidate => candidate.LayerId))
        {
            var quantity = Math.Min(layer.RemainingQuantity, remaining);
            allocations.Add(new FifoAllocationResult(
                layer.LayerId,
                quantity,
                layer.UnitCost,
                decimal.Round(quantity * layer.UnitCost, 2, MidpointRounding.AwayFromZero)));
            remaining -= quantity;
            if (remaining == 0) return allocations;
        }

        return null;
    }

    public static WorkshopProfit CalculateProfit(
        decimal partsRevenueBeforeTax,
        decimal customerLaborRevenueBeforeTax,
        decimal otherIncomeBeforeTax,
        decimal fifoInventoryCost,
        decimal mechanicLaborCost,
        decimal otherExpense)
    {
        var partsProfit = partsRevenueBeforeTax - fifoInventoryCost;
        var laborProfit = customerLaborRevenueBeforeTax - mechanicLaborCost;
        return new WorkshopProfit(
            partsProfit,
            laborProfit,
            partsProfit + laborProfit + otherIncomeBeforeTax - otherExpense);
    }
}
