using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<OilBarrelLossResponse>> RecordOilBarrelLossAsync(Guid id,
        RecordOilBarrelLossRequest request, CancellationToken cancellationToken = default)
    {
        var result = await RecordOilLossCoreAsync(id, request, null, cancellationToken);
        return result.IsSuccess ? Result.Success(result.Value!.Loss)
            : Result.Failure<OilBarrelLossResponse>(result.Error);
    }

    public async Task<Result<OilBarrelMissingResponse>> RecordOilBarrelMissingAsync(Guid id,
        RecordOilBarrelMissingRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.MissingReason))
            return Result.Failure<OilBarrelMissingResponse>(MaintenanceErrors.InvalidRequest);
        var result = await RecordOilLossCoreAsync(id,
            new(request.OccurredAtUtc, request.QuantityLiters, request.Reason, request.RowVersion),
            request.MissingReason, cancellationToken);
        return result.IsSuccess
            ? Result.Success(new OilBarrelMissingResponse(MapMissingEntry(result.Value!.Entry), result.Value.Barrel))
            : Result.Failure<OilBarrelMissingResponse>(result.Error);
    }

    public async Task<Result<OilBarrelMissingHistoryResponse>> GetOilBarrelMissingHistoryAsync(Guid id,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.OilBarrels.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
            return Result.Failure<OilBarrelMissingHistoryResponse>(MaintenanceErrors.NotFound);
        var normalizedPage = Math.Max(1, page);
        var normalizedSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);
        if ((long)(normalizedPage - 1) * normalizedSize > int.MaxValue)
            return Result.Failure<OilBarrelMissingHistoryResponse>(MaintenanceErrors.InvalidRequest);
        var query = dbContext.OilBarrelLosses.AsNoTracking()
            .Where(x => x.OilBarrelId == id && x.MissingReason != null);
        var count = await query.CountAsync(cancellationToken);
        var quantity = await query.SumAsync(x => x.QuantityLiters, cancellationToken);
        var cost = await query.SumAsync(x => x.CostAmount, cancellationToken);
        var entries = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((normalizedPage - 1) * normalizedSize).Take(normalizedSize).ToArrayAsync(cancellationToken);
        return Result.Success(new OilBarrelMissingHistoryResponse(id, quantity, cost,
            entries.Select(MapMissingEntry).ToArray(), normalizedPage, normalizedSize, count));
    }

    private static OilBarrelMissingEntryResponse MapMissingEntry(OilBarrelLoss entry) => new(
        entry.Id, entry.OilBarrelId, entry.ResponsibleInventoryLocationId!.Value,
        entry.OccurredAtUtc, entry.QuantityLiters, entry.CostAmount, entry.MissingReason!.Value,
        entry.Reason, entry.RecordedByUserId, entry.CreatedAtUtc, entry.StockMovementId);

    private static RecordedOilLoss BuildRecordedOilLoss(OilBarrelLoss entry, OilBarrel barrel) => new(
        new OilBarrelLossResponse(entry.Id, barrel.Id, entry.OccurredAtUtc, entry.QuantityLiters,
            entry.CostAmount, barrel.RecordedLossLiters, barrel.RemainingLiters,
            barrel.MaximumAllowedLossLiters - barrel.RecordedLossLiters), MapOilBarrel(barrel), entry);

    private sealed record RecordedOilLoss(OilBarrelLossResponse Loss, OilBarrelResponse Barrel, OilBarrelLoss Entry);

    private static bool IsOilBarrelScope(VehicleType? type, bool allowBoth) =>
        allowBoth ? type is null : IsOilBarrelVehicleType(type);

    private static bool IsOilItemCompatible(int mask, VehicleType? type, bool allowBoth) => allowBoth
        ? InventoryItemVehicleCompatibility.Allows(mask, VehicleType.Car)
            && InventoryItemVehicleCompatibility.Allows(mask, VehicleType.Motorcycle)
        : InventoryItemVehicleCompatibility.Allows(mask, type);
}
