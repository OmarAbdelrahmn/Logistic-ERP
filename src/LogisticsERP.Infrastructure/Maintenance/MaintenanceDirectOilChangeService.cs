using System.Security.Cryptography;
using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<IReadOnlyList<DirectOilInventoryLocationResponse>>> GetDirectOilInventoryLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await (from inventory in dbContext.InventoryLocations.AsNoTracking()
                               join site in dbContext.MaintenanceLocations.AsNoTracking()
                                   on inventory.MaintenanceLocationId equals site.Id
                               where inventory.Status == CatalogStatus.Active && site.Status == CatalogStatus.Active &&
                                   site.InventoryEnabled && site.AllowsCompanyVehicles
                               orderby site.NameAr, inventory.NameAr
                               select new DirectOilInventoryLocationResponse(inventory.Id, site.Id, inventory.NameAr, site.NameAr))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DirectOilInventoryLocationResponse>>(locations);
    }

    public async Task<Result<IReadOnlyList<DirectOilBarrelResponse>>> GetDirectOilBarrelsAsync(Guid inventoryLocationId, Guid inventoryItemId, CancellationToken cancellationToken = default)
    {
        if (inventoryLocationId == Guid.Empty || inventoryItemId == Guid.Empty)
            return Result.Failure<IReadOnlyList<DirectOilBarrelResponse>>(MaintenanceErrors.InvalidRequest);
        var barrels = await dbContext.OilBarrels.AsNoTracking()
            .Where(x => x.InventoryLocationId == inventoryLocationId && x.InventoryItemId == inventoryItemId &&
                (x.Status == OilBarrelStatus.Open || x.Status == OilBarrelStatus.Sealed))
            .OrderBy(x => x.PackageSequence)
            .Select(x => new DirectOilBarrelResponse(x.Id, x.BarrelNumber, x.InventoryLocationId,
                x.InventoryItemId, x.Status, x.RemainingLiters))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DirectOilBarrelResponse>>(barrels);
    }

    public async Task<Result<OilChangeResponse>> CompleteDirectOilChangeAsync(Guid vehicleId, DirectOilChangeRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<OilChangeResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            return Result.Failure<OilChangeResponse>(MaintenanceErrors.OilChangeIdempotencyRequired);
        if (request.PerformedAtUtc == default || request.OdometerAtChange < 0 || request.OtherCost < 0 ||
            string.IsNullOrWhiteSpace(request.VehicleRowVersion) || request.OilInventoryItemId == Guid.Empty ||
            request.InventoryLocationId == Guid.Empty || request.OilFilterChanged != request.OilFilterInventoryItemId.HasValue)
            return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest);

        var key = idempotencyKey.Trim();
        var hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { VehicleId = vehicleId, Request = request })));
        var previous = await dbContext.OilChangeOperations.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (previous is not null)
        {
            if (previous.RequestHash != hash) return Result.Failure<OilChangeResponse>(MaintenanceErrors.OilChangeIdempotencyConflict);
            return Result.Success(await MapOilChangeResponseAsync(previous, cancellationToken));
        }

        OilChangeResponse? response = null;
        var operationId = Guid.CreateVersion7();
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
                if (vehicle is null) return Result.Failure(MaintenanceErrors.NotFound);
                if (vehicle.VehicleType is not (VehicleType.Car or VehicleType.Motorcycle))
                    return Result.Failure(MaintenanceErrors.InvalidSubject);
                if (!MatchesRowVersion(vehicle.RowVersion, request.VehicleRowVersion))
                    return Result.Failure(MaintenanceErrors.ConcurrencyConflict);
                if (request.OdometerAtChange < vehicle.CurrentOdometer)
                    return Result.Failure(MaintenanceErrors.InvalidOdometer);
                var location = await (from inventory in dbContext.InventoryLocations.AsNoTracking()
                                      join site in dbContext.MaintenanceLocations.AsNoTracking() on inventory.MaintenanceLocationId equals site.Id
                                      where inventory.Id == request.InventoryLocationId && inventory.Status == CatalogStatus.Active &&
                                          site.InventoryEnabled && site.AllowsCompanyVehicles && site.Status == CatalogStatus.Active
                                      select site.Id).SingleOrDefaultAsync(cancellationToken);
                if (location == Guid.Empty) return Result.Failure(MaintenanceErrors.InvalidLocation);

                var configured = request.ConfiguredOilQuantityLiters;
                if (!configured.HasValue)
                    configured = await dbContext.MaintenancePlans.AsNoTracking()
                        .Where(x => x.Status == CatalogStatus.Active && x.DefaultOilQuantityLiters.HasValue &&
                            (x.VehicleModelId == vehicle.VehicleModelId || !x.VehicleModelId.HasValue && x.VehicleType == vehicle.VehicleType))
                        .OrderByDescending(x => x.VehicleModelId.HasValue)
                        .Select(x => x.DefaultOilQuantityLiters)
                        .FirstOrDefaultAsync(cancellationToken);
                var oilQuantity = MaintenanceBusinessRules.ResolveOilQuantityLiters(vehicle.VehicleType, request.OilFilterChanged, configured);
                if (!oilQuantity.HasValue) return Result.Failure(MaintenanceErrors.InvalidOilQuantity);

                var oilUsageId = Guid.CreateVersion7();
                var oil = await PostUsageCoreAsync(null, vehicle.Id, location, oilUsageId, request.OilInventoryItemId,
                    request.InventoryLocationId, oilQuantity.Value, MaintenanceUsageType.Oil, request.PerformedAtUtc,
                    request.Notes, actor.Value, StockMovementType.MaintenanceUsage, cancellationToken, request.NextOilBarrelId);
                if (oil.IsFailure) return Result.Failure(oil.Error);
                Guid? filterUsageId = null;
                UsagePosting? filter = null;
                if (request.OilFilterChanged)
                {
                    filterUsageId = Guid.CreateVersion7();
                    var posted = await PostUsageCoreAsync(null, vehicle.Id, location, filterUsageId.Value, request.OilFilterInventoryItemId!.Value,
                        request.InventoryLocationId, 1m, MaintenanceUsageType.OilFilter, request.PerformedAtUtc,
                        request.Notes, actor.Value, StockMovementType.MaintenanceUsage, cancellationToken);
                    if (posted.IsFailure) return Result.Failure(posted.Error);
                    filter = posted.Value;
                }

                VehicleMileageRules.ApplyVerifiedReading(vehicle, request.OdometerAtChange, request.PerformedAtUtc);
                dbContext.Entry(vehicle).Property(x => x.CurrentOdometer).IsModified = true;
                dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
                {
                    VehicleId = vehicle.Id, Reading = request.OdometerAtChange, RecordedAtUtc = request.PerformedAtUtc,
                    SourceType = VehicleOdometerSourceType.Maintenance, SourceEntityId = operationId, Notes = "Direct oil change"
                });
                if (request.OtherCost > 0)
                    AddDirectVehicleExpense(vehicle.Id, oil.Value!.Usage, operationId, request.PerformedAtUtc,
                        "MaintenanceOther", request.OtherCost, "Direct oil-change other expense");
                var operation = new OilChangeOperation
                {
                    Id = operationId, VehicleId = vehicle.Id, MaintenanceWorkOrderId = null,
                    IdempotencyKey = key, RequestHash = hash, PerformedAtUtc = request.PerformedAtUtc,
                    OdometerAtChange = request.OdometerAtChange, VehicleTypeSnapshot = vehicle.VehicleType,
                    OilInventoryItemId = request.OilInventoryItemId, OilQuantityLiters = oilQuantity.Value,
                    OilMaterialUsageId = oilUsageId, OilCost = oil.Value!.TotalCost,
                    OilFilterChanged = request.OilFilterChanged, OilFilterInventoryItemId = request.OilFilterInventoryItemId,
                    OilFilterMaterialUsageId = filterUsageId, OilFilterCost = filter?.TotalCost ?? 0,
                    LaborCost = 0, OtherCost = request.OtherCost,
                    TotalCost = oil.Value.TotalCost + (filter?.TotalCost ?? 0) + request.OtherCost,
                    PerformedByUserId = actor.Value, Notes = TrimOrNull(request.Notes)
                };
                dbContext.OilChangeOperations.Add(operation);
                await UpdateOilScheduleAsync(vehicle.Id, null, operation, vehicle.VehicleType, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                response = new OilChangeResponse(operation.Id, null, operation.PerformedAtUtc, operation.OdometerAtChange,
                    operation.VehicleTypeSnapshot, operation.OilQuantityLiters, operation.OilCost, operation.OilFilterChanged,
                    operation.OilFilterCost, 0, operation.OtherCost, operation.TotalCost, vehicle.Id, oil.Value.Usage.RiderProfileId);
                return Result.Success();
            });
            if (result.IsFailure) return Result.Failure<OilChangeResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException)
        {
            var completed = await dbContext.OilChangeOperations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (completed is not null)
                return completed.RequestHash == hash
                    ? Result.Success(await MapOilChangeResponseAsync(completed, cancellationToken))
                    : Result.Failure<OilChangeResponse>(MaintenanceErrors.OilChangeIdempotencyConflict);
            return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest);
        }
        return Result.Success(response!);
    }

    public async Task<Result<IReadOnlyList<OilChangeReportResponse>>> GetOilChangesAsync(Guid? vehicleId, CancellationToken cancellationToken = default)
    {
        if (vehicleId.HasValue && !await dbContext.Vehicles.AsNoTracking().AnyAsync(x => x.Id == vehicleId.Value, cancellationToken))
            return Result.Failure<IReadOnlyList<OilChangeReportResponse>>(MaintenanceErrors.NotFound);
        var query = dbContext.OilChangeOperations.AsNoTracking();
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        var operations = await query.OrderByDescending(x => x.PerformedAtUtc).ThenByDescending(x => x.Id).ToArrayAsync(cancellationToken);
        var vehicleIds = operations.Where(x => x.VehicleId.HasValue).Select(x => x.VehicleId!.Value).Distinct().ToArray();
        var assets = await dbContext.Vehicles.AsNoTracking().Where(x => vehicleIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.AssetNumber, cancellationToken);
        return Result.Success<IReadOnlyList<OilChangeReportResponse>>(operations.Select(x => new OilChangeReportResponse(
            x.Id, x.MaintenanceWorkOrderId, x.VehicleId, x.VehicleId.HasValue && assets.TryGetValue(x.VehicleId.Value, out var asset) ? asset : null,
            x.PerformedAtUtc, x.OdometerAtChange, x.VehicleTypeSnapshot, x.OilInventoryItemId,
            x.OilQuantityLiters, x.OilCost, x.OilFilterChanged, x.OilFilterInventoryItemId, x.OilFilterCost,
            x.LaborCost, x.OtherCost, x.TotalCost, x.OilMaterialUsageId, x.OilFilterMaterialUsageId,
            x.PerformedByUserId, x.Notes)).ToArray());
    }

    private async Task<OilChangeResponse> MapOilChangeResponseAsync(OilChangeOperation operation, CancellationToken cancellationToken)
    {
        var riderId = await dbContext.MaintenanceMaterialUsages.AsNoTracking()
            .Where(x => x.Id == operation.OilMaterialUsageId).Select(x => x.RiderProfileId).SingleOrDefaultAsync(cancellationToken);
        return new OilChangeResponse(operation.Id, operation.MaintenanceWorkOrderId, operation.PerformedAtUtc,
            operation.OdometerAtChange, operation.VehicleTypeSnapshot, operation.OilQuantityLiters, operation.OilCost,
            operation.OilFilterChanged, operation.OilFilterCost, operation.LaborCost, operation.OtherCost,
            operation.TotalCost, operation.VehicleId, riderId);
    }
}
