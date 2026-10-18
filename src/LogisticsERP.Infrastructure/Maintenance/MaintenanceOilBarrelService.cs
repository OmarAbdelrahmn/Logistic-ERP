using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<IReadOnlyList<OilBarrelResponse>>> GetOilBarrelsAsync(
        Guid? inventoryLocationId,
        Guid? inventoryItemId,
        string? status,
        VehicleType? vehicleType = null,
        CancellationToken cancellationToken = default)
    {
        if (vehicleType.HasValue && !IsOilBarrelVehicleType(vehicleType))
            return Result.Failure<IReadOnlyList<OilBarrelResponse>>(MaintenanceErrors.InvalidOilBarrelVehicleType);
        var query = dbContext.OilBarrels.AsNoTracking();
        if (vehicleType.HasValue) query = query.Where(x => x.AllowedVehicleType == vehicleType || x.AllowBothVehicleTypes);
        if (inventoryLocationId.HasValue) query = query.Where(x => x.InventoryLocationId == inventoryLocationId.Value);
        if (inventoryItemId.HasValue) query = query.Where(x => x.InventoryItemId == inventoryItemId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OilBarrelStatus>(status, true, out var parsed))
                return Result.Failure<IReadOnlyList<OilBarrelResponse>>(MaintenanceErrors.InvalidRequest);
            query = query.Where(x => x.Status == parsed);
        }

        var rows = await query
            .OrderBy(x => x.InventoryLocationId)
            .ThenBy(x => x.InventoryItemId)
            .ThenBy(x => x.Status == OilBarrelStatus.Open ? 0 : x.Status == OilBarrelStatus.Sealed ? 1 : 2)
            .ThenBy(x => x.OpenedAtUtc)
            .ThenBy(x => x.BarrelNumber)
            .Take(1000)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<OilBarrelResponse>>(rows.Select(MapOilBarrel).ToArray());
    }

    public async Task<Result<OpenOilBarrelResponse>> OpenOilBarrelAsync(
        Guid id,
        OpenOilBarrelRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OpenedAtUtc == default || !MatchesRequestedRowVersion(request.RowVersion))
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.InvalidRequest);
        if (!IsOilBarrelScope(request.AllowedVehicleType, request.AllowBothVehicleTypes))
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.InvalidOilBarrelVehicleType);

        var barrel = await dbContext.OilBarrels.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (barrel is null) return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.NotFound);
        if (!MatchesRowVersion(barrel.RowVersion, request.RowVersion))
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict);
        if (barrel.Status != OilBarrelStatus.Sealed || barrel.RemainingLiters <= 0)
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.InvalidOilBarrel);
        if ((barrel.AllowedVehicleType.HasValue || barrel.AllowBothVehicleTypes)
            && (barrel.AllowedVehicleType != request.AllowedVehicleType || barrel.AllowBothVehicleTypes != request.AllowBothVehicleTypes))
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.OilBarrelVehicleTypeMismatch);
        var item = await dbContext.InventoryItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == barrel.InventoryItemId, cancellationToken);
        if (item is null || !IsOilItemCompatible(item.CompatibleVehicleTypesMask, request.AllowedVehicleType, request.AllowBothVehicleTypes))
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.IncompatibleVehicleType);

        var previousRemaining = await dbContext.OilBarrels.AsNoTracking()
            .Where(x => x.Id != id
                && x.InventoryLocationId == barrel.InventoryLocationId
                && x.InventoryItemId == barrel.InventoryItemId
                && (request.AllowBothVehicleTypes || x.AllowBothVehicleTypes || x.AllowedVehicleType == request.AllowedVehicleType || x.AllowedVehicleType == null)
                && x.Status == OilBarrelStatus.Open
                && x.RemainingLiters > 0)
            .SumAsync(x => x.RemainingLiters, cancellationToken);

        if (!MaintenanceBusinessRules.CanOpenNextOilBarrel(previousRemaining))
        {
            return Result.Success(new OpenOilBarrelResponse(
                MapOilBarrel(barrel),
                Opened: false,
                HasPreviousBarrelWarning: true,
                PreviousOpenBarrelsRemainingLiters: previousRemaining,
                WarningCode: "oil.previous_barrel_remaining",
                WarningMessageAr: $"تنبيه: يوجد {previousRemaining:0.###} لتر متبقٍ في البرميل المفتوح. يجب استهلاكه أولاً قبل فتح البرميل المختار."));
        }

        var nextFifoLayerId = await GetNextOilLayerAsync(barrel.InventoryItemId,
            barrel.InventoryLocationId, request.AllowedVehicleType, cancellationToken, request.AllowBothVehicleTypes);
        if (!nextFifoLayerId.HasValue || nextFifoLayerId.Value != barrel.StockCostLayerId)
            return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.OilBarrelNotNextFifo);

        barrel.Status = OilBarrelStatus.Open;
        barrel.AllowedVehicleType = request.AllowedVehicleType;
        barrel.AllowBothVehicleTypes = request.AllowBothVehicleTypes;
        barrel.OpenedAtUtc = request.OpenedAtUtc;
        barrel.OpenedByUserId = actor.Value;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<OpenOilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict); }

        return Result.Success(new OpenOilBarrelResponse(
            MapOilBarrel(barrel),
            Opened: true,
            HasPreviousBarrelWarning: false,
            PreviousOpenBarrelsRemainingLiters: 0,
            WarningCode: null,
            WarningMessageAr: null));
    }

    private async Task<Result<RecordedOilLoss>> RecordOilLossCoreAsync(
        Guid id,
        RecordOilBarrelLossRequest request,
        OilBarrelMissingReason? missingReason,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<RecordedOilLoss>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OccurredAtUtc == default || request.QuantityLiters <= 0 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000
            || decimal.Round(request.QuantityLiters, 3) != request.QuantityLiters
            || !MatchesRequestedRowVersion(request.RowVersion))
            return Result.Failure<RecordedOilLoss>(MaintenanceErrors.InvalidRequest);

        var lossId = Guid.CreateVersion7();
        RecordedOilLoss? response = null;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                // Each execution attempt reloads state; a retry must not reuse staged deductions.
                dbContext.ChangeTracker.Clear();
                var committed = await dbContext.OilBarrelLosses.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == lossId, cancellationToken);
                if (committed is not null)
                {
                    var committedBarrel = await dbContext.OilBarrels.AsNoTracking().SingleAsync(x => x.Id == id, cancellationToken);
                    response = BuildRecordedOilLoss(committed, committedBarrel);
                    return Result.Success();
                }
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var barrel = await dbContext.OilBarrels.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (barrel is null) return Result.Failure(MaintenanceErrors.NotFound);
                if (!MatchesRowVersion(barrel.RowVersion, request.RowVersion)) return Result.Failure(MaintenanceErrors.ConcurrencyConflict);
                if (barrel.Status != OilBarrelStatus.Open || barrel.RemainingLiters < request.QuantityLiters)
                    return Result.Failure(MaintenanceErrors.InvalidOilBarrel);
                if (!missingReason.HasValue && barrel.RecordedLossLiters + request.QuantityLiters > barrel.MaximumAllowedLossLiters)
                    return Result.Failure(MaintenanceErrors.OilLossAllowanceExceeded);

                var layer = await dbContext.StockCostLayers.SingleAsync(x => x.Id == barrel.StockCostLayerId, cancellationToken);
                if (layer.RemainingQuantity < request.QuantityLiters) return Result.Failure(MaintenanceErrors.InsufficientStock);
                var balance = await GetOrCreateBalanceAsync(barrel.InventoryItemId, barrel.InventoryLocationId, cancellationToken);
                if (balance.QuantityOnHand < request.QuantityLiters) return Result.Failure(MaintenanceErrors.InsufficientStock);

                var cost = decimal.Round(request.QuantityLiters * layer.UnitCost, 2, MidpointRounding.AwayFromZero);
                var movementId = Guid.CreateVersion7();
                var movementLineId = Guid.CreateVersion7();
                dbContext.StockMovements.Add(NewMovement(movementId, StockMovementType.OilLoss, request.OccurredAtUtc, barrel.InventoryLocationId, null, nameof(OilBarrelLoss), lossId, request.Reason, actor.Value));
                dbContext.StockMovementLines.Add(new StockMovementLine
                {
                    Id = movementLineId,
                    StockMovementId = movementId,
                    InventoryItemId = barrel.InventoryItemId,
                    Quantity = request.QuantityLiters,
                    BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter,
                    UnitCost = layer.UnitCost,
                    TotalCost = cost
                });
                dbContext.StockCostAllocations.Add(new StockCostAllocation
                {
                    StockMovementLineId = movementLineId,
                    StockCostLayerId = layer.Id,
                    AllocatedQuantity = request.QuantityLiters,
                    UnitCost = layer.UnitCost,
                    AllocatedCost = cost
                });
                var entry = new OilBarrelLoss
                {
                    MissingReason = missingReason,
                    ResponsibleInventoryLocationId = missingReason.HasValue ? barrel.InventoryLocationId : null,
                    CreatedAtUtc = timeProvider.GetUtcNow(),
                    CreatedByUserId = actor.Value,
                    Id = lossId,
                    OilBarrelId = barrel.Id,
                    OccurredAtUtc = request.OccurredAtUtc,
                    QuantityLiters = request.QuantityLiters,
                    CostAmount = cost,
                    Reason = request.Reason.Trim(),
                    StockMovementId = movementId,
                    StockMovementLineId = movementLineId,
                    RecordedByUserId = actor.Value
                };
                dbContext.OilBarrelLosses.Add(entry);

                layer.RemainingQuantity -= request.QuantityLiters;
                RemoveFromBalance(balance, request.QuantityLiters, request.OccurredAtUtc);
                barrel.RemainingLiters -= request.QuantityLiters;
                if (missingReason.HasValue) barrel.RecordedMissingLiters += request.QuantityLiters;
                else barrel.RecordedLossLiters += request.QuantityLiters;
                if (barrel.RemainingLiters == 0)
                {
                    barrel.Status = OilBarrelStatus.Depleted;
                    barrel.DepletedAtUtc = request.OccurredAtUtc;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                response = BuildRecordedOilLoss(entry, barrel);
                return Result.Success();
            });
            if (result.IsFailure)
            {
                dbContext.ChangeTracker.Clear();
                return Result.Failure<RecordedOilLoss>(result.Error);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<RecordedOilLoss>(MaintenanceErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<RecordedOilLoss>(MaintenanceErrors.InvalidRequest);
        }
        return Result.Success(response!);
    }

    private async Task<Result<IReadOnlyList<TrackedAllocation>>> AllocateOilBarrelsAsync(
        Guid usageId,
        Guid inventoryItemId,
        Guid inventoryLocationId,
        decimal requestedQuantity,
        VehicleType? vehicleType,
        DateTimeOffset usedAtUtc,
        Guid actor,
        Guid? nextOilBarrelId,
        CancellationToken cancellationToken)
    {
        if (!IsOilBarrelVehicleType(vehicleType))
            return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InvalidOilBarrelVehicleType);
        var barrels = await dbContext.OilBarrels
            .Where(x => x.InventoryLocationId == inventoryLocationId
                && x.InventoryItemId == inventoryItemId
                && x.RemainingLiters > 0
                && (x.Status == OilBarrelStatus.Open && (x.AllowedVehicleType == vehicleType || x.AllowBothVehicleTypes)
                    || nextOilBarrelId.HasValue && x.Id == nextOilBarrelId.Value))
            .OrderBy(x => x.Status == OilBarrelStatus.Open ? 0 : 1)
            .ThenBy(x => x.OpenedAtUtc)
            .ThenBy(x => x.PackageSequence)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var openBarrels = barrels.Where(x => x.Status == OilBarrelStatus.Open).ToArray();
        if (openBarrels.Length != 1 || (!openBarrels[0].AllowBothVehicleTypes && openBarrels[0].AllowedVehicleType != vehicleType))
            return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.OpenOilBarrelRequired);
        OilBarrel? next = null;
        if (nextOilBarrelId.HasValue)
        {
            next = barrels.SingleOrDefault(x => x.Id == nextOilBarrelId.Value);
            if (next is null || next.Status != OilBarrelStatus.Sealed)
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InvalidOilBarrel);
            if (!next.AllowBothVehicleTypes && next.AllowedVehicleType.HasValue && next.AllowedVehicleType != vehicleType)
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.OilBarrelVehicleTypeMismatch);
            var nextLayer = await GetNextOilLayerAsync(inventoryItemId, inventoryLocationId,
                vehicleType!.Value, cancellationToken);
            if (nextLayer != next.StockCostLayerId)
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.OilBarrelNotNextFifo);
        }
        var planned = new List<(OilBarrel Barrel, decimal Quantity)>();
        var firstQuantity = Math.Min(requestedQuantity, openBarrels[0].RemainingLiters);
        planned.Add((openBarrels[0], firstQuantity));
        var remaining = requestedQuantity - firstQuantity;
        if (remaining > 0)
        {
            if (next is null || next.RemainingLiters < remaining)
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.OpenOilBarrelRequired);
            if (await dbContext.OilBarrels.AnyAsync(x => x.Id != openBarrels[0].Id
                && x.InventoryLocationId == inventoryLocationId && x.InventoryItemId == inventoryItemId
                && x.Status == OilBarrelStatus.Open
                && (next.AllowBothVehicleTypes || x.AllowBothVehicleTypes || x.AllowedVehicleType == null
                    || x.AllowedVehicleType == vehicleType), cancellationToken))
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InvalidOilBarrel);
            planned.Add((next, remaining));
        }
        var layerIds = planned.Select(x => x.Barrel.StockCostLayerId).Distinct().ToArray();
        var layers = await dbContext.StockCostLayers.Where(x => layerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var tracked = new List<TrackedAllocation>();
        foreach (var group in planned.GroupBy(x => x.Barrel.StockCostLayerId))
        {
            var quantity = group.Sum(x => x.Quantity);
            if (!layers.TryGetValue(group.Key, out var layer) || layer.RemainingQuantity < quantity)
                return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InsufficientStock);
            tracked.Add(new TrackedAllocation(layer, quantity,
                decimal.Round(quantity * layer.UnitCost, 2, MidpointRounding.AwayFromZero)));
        }
        var balance = await GetOrCreateBalanceAsync(inventoryItemId, inventoryLocationId, cancellationToken);
        if (balance.QuantityOnHand < requestedQuantity)
            return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InsufficientStock);
        foreach (var (barrel, quantity) in planned)
        {
            if (barrel.Status == OilBarrelStatus.Sealed)
            {
                if (!barrel.AllowBothVehicleTypes) barrel.AllowedVehicleType = vehicleType;
                barrel.Status = OilBarrelStatus.Open;
                barrel.OpenedAtUtc = usedAtUtc;
                barrel.OpenedByUserId = actor;
            }
            barrel.RemainingLiters -= quantity;
            if (barrel.RemainingLiters == 0)
            {
                barrel.Status = OilBarrelStatus.Depleted;
                barrel.DepletedAtUtc = usedAtUtc;
            }
            dbContext.OilBarrelUsageAllocations.Add(new OilBarrelUsageAllocation
            {
                MaintenanceMaterialUsageId = usageId, OilBarrelId = barrel.Id, QuantityLiters = quantity
            });
        }
        foreach (var allocation in tracked) allocation.Layer.RemainingQuantity -= allocation.Quantity;
        RemoveFromBalance(balance, requestedQuantity, usedAtUtc);
        return Result.Success<IReadOnlyList<TrackedAllocation>>(tracked);
    }

    private static bool IsOilBarrelVehicleType(VehicleType? vehicleType) =>
        vehicleType is VehicleType.Car or VehicleType.Motorcycle;

    private async Task<Guid?> GetNextOilLayerAsync(Guid itemId, Guid locationId,
        VehicleType? vehicleType, CancellationToken cancellationToken, bool allowBothVehicleTypes = false) =>
        await (from barrel in dbContext.OilBarrels.AsNoTracking()
               join layer in dbContext.StockCostLayers.AsNoTracking() on barrel.StockCostLayerId equals layer.Id
               where barrel.InventoryItemId == itemId && barrel.InventoryLocationId == locationId
                   && barrel.Status == OilBarrelStatus.Sealed && barrel.RemainingLiters > 0
                   && (barrel.AllowedVehicleType == null || !allowBothVehicleTypes && barrel.AllowedVehicleType == vehicleType)
                   && layer.RemainingQuantity > 0
               orderby layer.ReceivedAtUtc, layer.OriginalSequence, layer.Id
               select (Guid?)layer.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<Result> RestoreOilBarrelsAsync(
        Guid originalUsageId,
        Guid reversalUsageId,
        CancellationToken cancellationToken)
    {
        var allocations = await dbContext.OilBarrelUsageAllocations.AsNoTracking()
            .Where(x => x.MaintenanceMaterialUsageId == originalUsageId && x.Direction == MaintenanceUsageDirection.Issue)
            .ToArrayAsync(cancellationToken);
        if (allocations.Length == 0) return Result.Failure(MaintenanceErrors.InvalidState);
        var barrelIds = allocations.Select(x => x.OilBarrelId).Distinct().ToArray();
        var barrels = await dbContext.OilBarrels.Where(x => barrelIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (var allocation in allocations)
        {
            var barrel = barrels[allocation.OilBarrelId];
            if (barrel.Status == OilBarrelStatus.Returned || barrel.RemainingLiters + allocation.QuantityLiters > barrel.NominalCapacityLiters)
                return Result.Failure(MaintenanceErrors.InvalidState);
            if (await dbContext.OilBarrels.AnyAsync(x => x.Id != barrel.Id
                && x.InventoryLocationId == barrel.InventoryLocationId && x.InventoryItemId == barrel.InventoryItemId
                && (barrel.AllowBothVehicleTypes || x.AllowBothVehicleTypes || x.AllowedVehicleType == null
                    || barrel.AllowedVehicleType == null || x.AllowedVehicleType == barrel.AllowedVehicleType) && x.Status == OilBarrelStatus.Open,
                cancellationToken))
                return Result.Failure(MaintenanceErrors.InvalidState);
            barrel.RemainingLiters += allocation.QuantityLiters;
            barrel.Status = OilBarrelStatus.Open;
            barrel.DepletedAtUtc = null;
            dbContext.OilBarrelUsageAllocations.Add(new OilBarrelUsageAllocation
            {
                MaintenanceMaterialUsageId = reversalUsageId,
                OilBarrelId = barrel.Id,
                QuantityLiters = allocation.QuantityLiters,
                Direction = MaintenanceUsageDirection.Reversal,
                ReversalOfAllocationId = allocation.Id
            });
        }
        return Result.Success();
    }

    private async Task<Result> MoveWholeOilBarrelsAsync(
        Guid sourceLayerId,
        Guid sourceLocationId,
        Guid destinationLocationId,
        Guid destinationLayerId,
        decimal quantityLiters,
        CancellationToken cancellationToken)
    {
        var barrels = await dbContext.OilBarrels
            .Where(x => x.StockCostLayerId == sourceLayerId
                && x.InventoryLocationId == sourceLocationId
                && x.Status == OilBarrelStatus.Sealed
                && x.RemainingLiters > 0)
            .OrderBy(x => x.PackageSequence)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var selected = SelectWholeBarrels(barrels, quantityLiters);
        if (selected is null) return Result.Failure(MaintenanceErrors.OilTransferRequiresWholeBarrels);
        foreach (var barrel in selected)
        {
            barrel.InventoryLocationId = destinationLocationId;
            barrel.StockCostLayerId = destinationLayerId;
        }
        return Result.Success();
    }

    private async Task<Result> ReturnWholeOilBarrelsAsync(
        Guid stockCostLayerId,
        Guid inventoryLocationId,
        decimal quantityLiters,
        CancellationToken cancellationToken)
    {
        var barrels = await dbContext.OilBarrels
            .Where(x => x.StockCostLayerId == stockCostLayerId
                && x.InventoryLocationId == inventoryLocationId
                && x.Status == OilBarrelStatus.Sealed
                && x.RemainingLiters > 0)
            .OrderBy(x => x.PackageSequence)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var selected = SelectWholeBarrels(barrels, quantityLiters);
        if (selected is null) return Result.Failure(MaintenanceErrors.OilTransferRequiresWholeBarrels);
        foreach (var barrel in selected) barrel.Status = OilBarrelStatus.Returned;
        return Result.Success();
    }

    private static OilBarrel[]? SelectWholeBarrels(OilBarrel[] barrels, decimal targetQuantity)
    {
        var selected = new List<OilBarrel>();
        return TrySelect(0, targetQuantity) ? selected.ToArray() : null;

        bool TrySelect(int index, decimal remaining)
        {
            if (remaining == 0) return true;
            if (remaining < 0 || index >= barrels.Length) return false;
            var barrel = barrels[index];
            if (barrel.RemainingLiters <= remaining)
            {
                selected.Add(barrel);
                if (TrySelect(index + 1, remaining - barrel.RemainingLiters)) return true;
                selected.RemoveAt(selected.Count - 1);
            }
            return TrySelect(index + 1, remaining);
        }
    }

    private static OilBarrelResponse MapOilBarrel(OilBarrel barrel) => new(
        barrel.Id,
        barrel.BarrelNumber,
        barrel.PurchaseReceiptLineId,
        barrel.InventoryItemId,
        barrel.InventoryLocationId,
        barrel.StockCostLayerId,
        barrel.PackageSequence,
        barrel.NominalCapacityLiters,
        barrel.NominalCapacityLiters - barrel.RemainingLiters - barrel.RecordedLossLiters - barrel.RecordedMissingLiters,
        barrel.RemainingLiters,
        barrel.UnitCostPerLiter,
        barrel.Status == OilBarrelStatus.Returned ? 0 : decimal.Round(barrel.RemainingLiters * barrel.UnitCostPerLiter, 2, MidpointRounding.AwayFromZero),
        barrel.MaximumAllowedLossLiters,
        barrel.RecordedLossLiters,
        barrel.MaximumAllowedLossLiters - barrel.RecordedLossLiters,
        barrel.Status,
        barrel.OpenedAtUtc,
        barrel.DepletedAtUtc,
        EncodeRowVersion(barrel.RowVersion),
        barrel.AllowedVehicleType,
        barrel.AllowBothVehicleTypes,
        barrel.RecordedMissingLiters);
}
