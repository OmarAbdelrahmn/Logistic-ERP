using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    private async Task StageClosedOilWasteAsync(OilBarrelUsageAllocation allocation, OilChangeRevision revision, CancellationToken ct)
    {
        var barrel = await dbContext.OilBarrels.SingleAsync(x => x.Id == allocation.OilBarrelId, ct);
        var layer = await dbContext.StockCostLayers.SingleAsync(x => x.Id == barrel.StockCostLayerId, ct);
        var balance = await GetOrCreateBalanceAsync(barrel.InventoryItemId, barrel.InventoryLocationId, ct);
        var quantity = allocation.QuantityLiters;
        var cost = decimal.Round(quantity * layer.UnitCost, 2, MidpointRounding.AwayFromZero);
        var lossId = Guid.CreateVersion7(); var movementId = Guid.CreateVersion7(); var lineId = Guid.CreateVersion7();
        dbContext.StockMovements.Add(NewMovement(movementId, StockMovementType.OilLoss, revision.PostedAtUtc,
            barrel.InventoryLocationId, null, nameof(OilBarrelLoss), lossId, revision.Reason, revision.ActorUserId));
        dbContext.StockMovementLines.Add(new StockMovementLine { Id = lineId, StockMovementId = movementId,
            InventoryItemId = barrel.InventoryItemId, Quantity = quantity, BaseUnitOfMeasure = InventoryUnitOfMeasure.Liter,
            UnitCost = layer.UnitCost, TotalCost = cost });
        dbContext.StockCostAllocations.Add(new StockCostAllocation { StockMovementLineId = lineId,
            StockCostLayerId = layer.Id, AllocatedQuantity = quantity, UnitCost = layer.UnitCost, AllocatedCost = cost });
        dbContext.OilBarrelLosses.Add(new OilBarrelLoss { Id = lossId, OilBarrelId = barrel.Id,
            CorrectionRevisionId = revision.Id, OccurredAtUtc = revision.PostedAtUtc,
            QuantityLiters = quantity, CostAmount = cost, MissingReason = OilBarrelMissingReason.Wasted,
            ResponsibleInventoryLocationId = barrel.InventoryLocationId, Reason = revision.Reason,
            StockMovementId = movementId, StockMovementLineId = lineId, RecordedByUserId = revision.ActorUserId });
        layer.RemainingQuantity -= quantity;
        RemoveFromBalance(balance, quantity, revision.PostedAtUtc);
        barrel.RecordedMissingLiters += quantity;
    }

    private async Task<Result> RebookOilFinancialsAsync(OilChangeOperation op, OilChangeRevision revision,
        MaintenanceMaterialUsage attribution, decimal previousTotal, CancellationToken ct)
    {
        var revisions = await dbContext.OilChangeRevisions.AsNoTracking()
            .Where(x => x.OilChangeOperationId == op.Id)
            .Select(x => new { x.Id, x.OldOilUsageId, x.OldFilterUsageId }).ToArrayAsync(ct);
        var revisionIds = revisions.Select(x => x.Id).Append(revision.Id).Distinct().ToArray();
        var usageIds = revisions.Select(x => x.OldOilUsageId)
            .Concat(revisions.Where(x => x.OldFilterUsageId.HasValue).Select(x => x.OldFilterUsageId!.Value))
            .Concat(OilUsageIds(op)).Concat([revision.OldOilUsageId])
            .Concat(revision.OldFilterUsageId is Guid f ? [f] : []).Distinct().ToArray();
        // Posting helpers create expenses for new usages. Replace only their unsaved
        // financial rows with the revision's final financial projection below.
        foreach (var entry in dbContext.ChangeTracker.Entries<VehicleExpense>().Where(x => x.State == EntityState.Added
            && x.Entity.SourceEntityType == nameof(MaintenanceMaterialUsage) && usageIds.Contains(x.Entity.SourceEntityId)).ToArray())
            entry.State = EntityState.Detached;
        foreach (var entry in dbContext.ChangeTracker.Entries<ExternalMaintenanceFinancialEntry>().Where(x => x.State == EntityState.Added
            && x.Entity.SourceType == ExternalFinancialSourceType.InventoryCost && x.Entity.SourceEntityId.HasValue
            && usageIds.Contains(x.Entity.SourceEntityId.Value)).ToArray()) entry.State = EntityState.Detached;

        if (op.VehicleId is Guid vehicleId)
        {
            var active = await dbContext.VehicleExpenses.AsNoTracking().Where(x => x.VehicleId == vehicleId
                && !x.ReversalOfExpenseId.HasValue
                && (x.SourceEntityType == nameof(MaintenanceMaterialUsage) && usageIds.Contains(x.SourceEntityId)
                    || x.SourceEntityType == nameof(OilChangeOperation) && x.SourceEntityId == op.Id
                    || x.SourceEntityType == nameof(OilChangeRevision) && revisionIds.Contains(x.SourceEntityId))
                && !dbContext.VehicleExpenses.Any(r => r.ReversalOfExpenseId == x.Id)).ToArrayAsync(ct);
            if (active.Sum(x => x.TotalAmount) != previousTotal) return Result.Failure(MaintenanceErrors.InvalidState);
            var postingDate = DateOnly.FromDateTime(revision.PostedAtUtc.ToOffset(TimeSpan.FromHours(3)).DateTime);
            foreach (var original in active)
                dbContext.VehicleExpenses.Add(new VehicleExpense { VehicleId = vehicleId,
                    RiderVehicleAssignmentId = original.RiderVehicleAssignmentId, RiderProfileId = original.RiderProfileId,
                    ExpenseType = original.ExpenseType, SourceEntityType = nameof(OilChangeRevision), SourceEntityId = revision.Id,
                    OccurredOn = postingDate, AmountBeforeTax = -original.AmountBeforeTax, TaxAmount = -original.TaxAmount,
                    TotalAmount = -original.TotalAmount, CurrencyCode = original.CurrencyCode,
                    Description = revision.Reason, ReversalOfExpenseId = original.Id });
            if (!op.IsCancelled)
            {
                foreach (var (type, amount) in new[] { ("MaintenanceMaterial", op.OilCost + op.OilFilterCost),
                    ("MaintenanceLabor", op.LaborCost), ("MaintenanceOther", op.OtherCost) })
                    if (amount != 0) dbContext.VehicleExpenses.Add(new VehicleExpense { VehicleId = vehicleId,
                        RiderVehicleAssignmentId = attribution.RiderVehicleAssignmentId, RiderProfileId = attribution.RiderProfileId,
                        ExpenseType = type, SourceEntityType = nameof(OilChangeRevision), SourceEntityId = revision.Id,
                        OccurredOn = postingDate, AmountBeforeTax = amount, TotalAmount = amount, Description = revision.Reason });
            }
        }
        else if (op.MaintenanceWorkOrderId is Guid workId)
        {
            var active = await dbContext.ExternalMaintenanceFinancialEntries.AsNoTracking().Where(x =>
                x.MaintenanceWorkOrderId == workId && x.EntryType == ExternalFinancialEntryType.Expense
                && !x.ReversalOfEntryId.HasValue && x.SourceEntityId.HasValue
                && (usageIds.Contains(x.SourceEntityId.Value) || x.SourceEntityId == op.Id || revisionIds.Contains(x.SourceEntityId.Value))
                && !dbContext.ExternalMaintenanceFinancialEntries.Any(r => r.ReversalOfEntryId == x.Id)).ToArrayAsync(ct);
            if (active.Sum(x => x.TotalAmount) != previousTotal) return Result.Failure(MaintenanceErrors.InvalidState);
            foreach (var original in active)
                AddExternalFinancialEntry(workId, original.EntryType, original.SourceType, revision.Id, revision.PostedAtUtc,
                    -original.AmountBeforeTax, -original.TaxAmount, revision.Reason, revision.ActorUserId,
                    reversalOfEntryId: original.Id);
            if (!op.IsCancelled)
                foreach (var (type, amount) in new[] { (ExternalFinancialSourceType.InventoryCost, op.OilCost + op.OilFilterCost),
                    (ExternalFinancialSourceType.MechanicLaborPayment, op.LaborCost), (ExternalFinancialSourceType.OtherExpense, op.OtherCost) })
                    if (amount != 0) AddExternalFinancialEntry(workId, ExternalFinancialEntryType.Expense, type,
                        revision.Id, revision.PostedAtUtc, amount, 0, revision.Reason, revision.ActorUserId);
        }
        return Result.Success();
    }

    private async Task<Result<decimal?>> ReconcileOilMileageAsync(OilChangeOperation op, OilChangeRevision revision,
        decimal? suppliedBasis, CancellationToken ct)
    {
        if (op.VehicleId is not Guid vehicleId) return Result.Success<decimal?>(null);
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, ct);
        if (vehicle is null) return Result.Failure<decimal?>(MaintenanceErrors.NotFound);
        var source = await dbContext.VehicleOdometerReadings.Where(x => x.VehicleId == vehicleId
            && x.SourceType == VehicleOdometerSourceType.Maintenance && x.SourceEntityId == op.Id).ToArrayAsync(ct);
        // Legacy operations may have missing or duplicate mileage evidence. Neither
        // condition should prevent correcting or cancelling the oil service.
        if (!op.IsCancelled && source.Length == 1
            && source[0].RecordedAtUtc == op.PerformedAtUtc && source[0].Reading == op.OdometerAtChange)
            return Result.Success<decimal?>(vehicle.TrackedDistanceKm);
        foreach (var reading in source) { reading.IsVoided = true; reading.VoidedByRevisionId = revision.Id; }
        if (!op.IsCancelled) dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
        {
            VehicleId = vehicleId, Reading = op.OdometerAtChange, RecordedAtUtc = op.PerformedAtUtc,
            SourceType = VehicleOdometerSourceType.Maintenance, SourceEntityId = op.Id,
            IsCorrection = true, CorrectionReason = revision.Reason, Notes = $"Oil correction {revision.Id}"
        });
        await dbContext.SaveChangesAsync(ct);
        var readings = await dbContext.VehicleOdometerReadings.AsNoTracking().Where(x => x.VehicleId == vehicleId).ToArrayAsync(ct);
        var days = await dbContext.VehicleDailyDistances.Where(x => x.VehicleId == vehicleId).OrderBy(x => x.WorkDate).ToArrayAsync(ct);
        var dailyIds = days.Select(x => x.Id).ToHashSet();
        var evidence = readings.Where(x => !x.SourceEntityId.HasValue || !dailyIds.Contains(x.SourceEntityId.Value))
            .Select(x => new MileageEvent(x.RecordedAtUtc, x.Id, x, null))
            .Concat(days.Select(x => new MileageEvent(new DateTimeOffset(x.WorkDate.ToDateTime(TimeOnly.MaxValue),
                TimeSpan.FromHours(3)).ToUniversalTime(), x.Id, null, x)))
            .OrderBy(x => x.At).ThenBy(x => x.Id).ToArray();
        // Prefer remaining recorded evidence. With no evidence, retain the cached
        // vehicle mileage rather than inventing a zero baseline or rejecting the request.
        var first = evidence.FirstOrDefault();
        var inferredBasis = first?.Reading?.Reading
            ?? (first?.Day is { } firstDay
                ? firstDay.ManualBaselineOdometerReading
                    ?? firstDay.EffectiveOdometerAfterKm - firstDay.AppliedDistanceKm
                : vehicle.TrackedDistanceKm);
        var running = suppliedBasis is >= 0 and <= 9_999_999_999_999_999m
            ? suppliedBasis.Value : Math.Max(0m, inferredBasis);
        foreach (var e in evidence)
        {
            if (e.Day is { } day)
            {
                // A lower later manual reading becomes a correction, using the
                // existing correction rules instead of blocking the oil operation.
                VehicleDailyDistanceRules.TryRecalculate([day], running, out running,
                    day.ManualOdometerReading < running ? day : null);
            }
            else if (e.Reading is { } reading)
            {
                running = reading.Reading;
            }
        }
        vehicle.CurrentOdometer = decimal.ToInt64(decimal.Floor(running)); vehicle.TrackedDistanceKm = running;
        vehicle.LastOdometerAtUtc = evidence.LastOrDefault()?.At ?? vehicle.LastOdometerAtUtc;
        return Result.Success<decimal?>(running);
    }

    private sealed record MileageEvent(DateTimeOffset At, Guid Id, VehicleOdometerReading? Reading,
        LogisticsERP.Domain.Entities.Fleet.VehicleDailyDistance? Day);
}
