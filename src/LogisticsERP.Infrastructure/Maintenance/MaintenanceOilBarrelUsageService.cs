using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<OilBarrelResponse>> SetOilBarrelVehicleTypeAsync(Guid id,
        SetOilBarrelVehicleTypeRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (!IsOilBarrelScope(request.AllowedVehicleType, request.AllowBothVehicleTypes))
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.InvalidOilBarrelVehicleType);
        var barrel = await dbContext.OilBarrels.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (barrel is null) return Result.Failure<OilBarrelResponse>(MaintenanceErrors.NotFound);
        if (!MatchesRowVersion(barrel.RowVersion, request.RowVersion))
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict);
        // This endpoint classifies barrels that were already open before this feature.
        // New barrels choose their type in OpenOilBarrelAsync.
        if (barrel.Status != OilBarrelStatus.Open
            || (barrel.AllowedVehicleType.HasValue || barrel.AllowBothVehicleTypes)
                && (barrel.AllowedVehicleType != request.AllowedVehicleType || barrel.AllowBothVehicleTypes != request.AllowBothVehicleTypes))
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.OilBarrelVehicleTypeLocked);
        var item = await dbContext.InventoryItems.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == barrel.InventoryItemId, cancellationToken);
        if (item is null || !IsOilItemCompatible(item.CompatibleVehicleTypesMask, request.AllowedVehicleType, request.AllowBothVehicleTypes))
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.IncompatibleVehicleType);
        if (barrel.Status == OilBarrelStatus.Open && await dbContext.OilBarrels.AnyAsync(x =>
            x.Id != id && x.InventoryLocationId == barrel.InventoryLocationId
            && x.InventoryItemId == barrel.InventoryItemId && x.Status == OilBarrelStatus.Open
            && (request.AllowBothVehicleTypes || x.AllowBothVehicleTypes || x.AllowedVehicleType == null
                || x.AllowedVehicleType == request.AllowedVehicleType), cancellationToken))
            return Result.Failure<OilBarrelResponse>(MaintenanceErrors.InvalidOilBarrel);
        barrel.AllowedVehicleType = request.AllowedVehicleType;
        barrel.AllowBothVehicleTypes = request.AllowBothVehicleTypes;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<OilBarrelResponse>(MaintenanceErrors.ConcurrencyConflict); }
        return Result.Success(MapOilBarrel(barrel));
    }

    public async Task<Result<OilBarrelUsageResponse>> GetOilBarrelUsageAsync(Guid id,
        int page, int pageSize, int lossPage = 1, int lossPageSize = 50, CancellationToken cancellationToken = default)
    {
        var barrel = await dbContext.OilBarrels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (barrel is null) return Result.Failure<OilBarrelUsageResponse>(MaintenanceErrors.NotFound);
        var normalizedPage = Math.Max(1, page);
        var normalizedSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);
        var normalizedLossPage = Math.Max(1, lossPage);
        var normalizedLossSize = Math.Clamp(lossPageSize <= 0 ? 50 : lossPageSize, 1, 200);
        var vehicleOffset = ((long)normalizedPage - 1) * normalizedSize;
        var lossOffset = ((long)normalizedLossPage - 1) * normalizedLossSize;
        if (vehicleOffset > int.MaxValue || lossOffset > int.MaxValue)
            return Result.Failure<OilBarrelUsageResponse>(MaintenanceErrors.InvalidRequest);
        var query = from allocation in dbContext.OilBarrelUsageAllocations.AsNoTracking()
                    join usage in dbContext.MaintenanceMaterialUsages.AsNoTracking()
                        on allocation.MaintenanceMaterialUsageId equals usage.Id
                    join vehicle in dbContext.Vehicles.IgnoreQueryFilters().AsNoTracking()
                        on usage.VehicleId equals vehicle.Id into vehicles
                    from vehicle in vehicles.DefaultIfEmpty()
                    join external in dbContext.ExternalVehicleSnapshots.AsNoTracking()
                        on usage.MaintenanceWorkOrderId equals external.MaintenanceWorkOrderId into externalVehicles
                    from external in externalVehicles.DefaultIfEmpty()
                    where allocation.OilBarrelId == id
                    select new
                    {
                        usage.VehicleId,
                        ExternalWorkOrderId = usage.VehicleId == null ? usage.MaintenanceWorkOrderId : null,
                        AssetNumber = vehicle == null ? null : vehicle.AssetNumber,
                        PlateNumberAr = vehicle == null ? null : vehicle.PlateNumberAr,
                        PlateNumberEn = vehicle == null ? null : vehicle.PlateNumberEn,
                        ExternalPlateOrReference = external == null ? null : external.PlateOrReference,
                        VehicleType = vehicle == null ? external == null ? null : external.VehicleType : (VehicleType?)vehicle.VehicleType,
                        IssuedLiters = allocation.Direction == MaintenanceUsageDirection.Issue ? allocation.QuantityLiters : 0m,
                        ReversedLiters = allocation.Direction == MaintenanceUsageDirection.Reversal ? allocation.QuantityLiters : 0m,
                        UsedAtUtc = usage.EffectiveServiceAtUtc ?? usage.UsedAtUtc,
                        allocation.Direction
                    };
        var issued = await query.SumAsync(x => x.IssuedLiters, cancellationToken);
        var reversed = await query.SumAsync(x => x.ReversedLiters, cancellationToken);
        var grouped = query.GroupBy(x => new
        {
            x.VehicleId, x.ExternalWorkOrderId, x.AssetNumber, x.PlateNumberAr, x.PlateNumberEn,
            x.ExternalPlateOrReference, x.VehicleType
        }).Select(group => new
        {
            group.Key.VehicleId, group.Key.ExternalWorkOrderId, group.Key.AssetNumber,
            group.Key.PlateNumberAr, group.Key.PlateNumberEn, group.Key.ExternalPlateOrReference,
            group.Key.VehicleType,
            IssuedLiters = group.Sum(x => x.IssuedLiters),
            ReversedLiters = group.Sum(x => x.ReversedLiters),
            IssueCount = group.Count(x => x.Direction == MaintenanceUsageDirection.Issue),
            LastUsedAtUtc = group.Max(x => x.UsedAtUtc)
        });
        var count = await grouped.CountAsync(cancellationToken);
        var rows = await grouped.OrderByDescending(x => x.LastUsedAtUtc)
            .ThenBy(x => x.VehicleId).ThenBy(x => x.ExternalWorkOrderId)
            .Skip((int)vehicleOffset).Take(normalizedSize).ToArrayAsync(cancellationToken);
        var losses = dbContext.OilBarrelLosses.AsNoTracking().Where(x => x.OilBarrelId == id);
        var lossTotalCount = await losses.CountAsync(cancellationToken);
        var lossEntries = await losses.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((int)lossOffset).Take(normalizedLossSize)
            .Select(x => new OilBarrelUsageLossEntryResponse(x.Id, x.OccurredAtUtc, x.QuantityLiters,
                x.CostAmount, x.Reason, x.MissingReason, x.ResponsibleInventoryLocationId,
                x.RecordedByUserId, x.CreatedAtUtc, x.StockMovementId, x.StockMovementLineId, x.CorrectionRevisionId))
            .ToArrayAsync(cancellationToken);
        return Result.Success(new OilBarrelUsageResponse(MapOilBarrel(barrel), issued, reversed,
            issued - reversed, rows.Select(x => new OilBarrelVehicleUsageResponse(
                x.VehicleId, x.ExternalWorkOrderId, x.AssetNumber, x.PlateNumberAr, x.PlateNumberEn,
                x.ExternalPlateOrReference, x.VehicleType, x.IssuedLiters, x.ReversedLiters,
                x.IssuedLiters - x.ReversedLiters, x.IssueCount, x.LastUsedAtUtc)).ToArray(),
            normalizedPage, normalizedSize, count, lossEntries, normalizedLossPage, normalizedLossSize, lossTotalCount));
    }
}
