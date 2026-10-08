using System.Text.Json;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    private static readonly string[] OilChangeCorrectionModuleKeys = ["maintenance", "fleet"];

    public async Task<Result<OilChangeResponse>> CorrectCompletedOilChangeVehicleAsync(
        Guid oilChangeId, CorrectCompletedOilChangeVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<OilChangeResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (oilChangeId == Guid.Empty || request.VehicleId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest);

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);
                var operation = await dbContext.OilChangeOperations.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == oilChangeId, cancellationToken);
                if (operation is null) return Result.Failure<OilChangeResponse>(MaintenanceErrors.NotFound);
                if (operation.MaintenanceWorkOrderId.HasValue || !operation.VehicleId.HasValue)
                    return Result.Failure<OilChangeResponse>(MaintenanceErrors.OilChangeCorrectionWorkOrder);
                var oldVehicleId = operation.VehicleId.Value;
                if (oldVehicleId == request.VehicleId)
                    return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest);

                var target = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == request.VehicleId, cancellationToken);
                if (target is null) return Result.Failure<OilChangeResponse>(MaintenanceErrors.NotFound);
                if (target.VehicleType != operation.VehicleTypeSnapshot)
                    return Result.Failure<OilChangeResponse>(MaintenanceErrors.OilChangeCorrectionVehicleType);

                var usageIds = operation.OilFilterMaterialUsageId.HasValue
                    ? new[] { operation.OilMaterialUsageId, operation.OilFilterMaterialUsageId.Value }
                    : new[] { operation.OilMaterialUsageId };
                var usages = await dbContext.MaintenanceMaterialUsages.AsNoTracking()
                    .Where(x => usageIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
                if (usages.Length != usageIds.Length || usages.Any(x => x.VehicleId != oldVehicleId || x.ReversalOfUsageId.HasValue) ||
                    await dbContext.MaintenanceMaterialUsages.AsNoTracking()
                        .AnyAsync(x => x.ReversalOfUsageId.HasValue && usageIds.Contains(x.ReversalOfUsageId.Value), cancellationToken))
                    return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidState);

                var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking()
                    .Where(x => x.VehicleId == target.Id && x.StartedAtUtc <= operation.PerformedAtUtc &&
                        (!x.EndedAtUtc.HasValue || x.EndedAtUtc >= operation.PerformedAtUtc))
                    .OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);
                var attribution = assignment is null ? InventoryAttributionStatus.Unassigned : InventoryAttributionStatus.AssignedRider;
                var readingCount = await dbContext.VehicleOdometerReadings.AsNoTracking()
                    .CountAsync(x => x.SourceType == VehicleOdometerSourceType.Maintenance &&
                        x.SourceEntityId == oilChangeId && x.VehicleId == oldVehicleId, cancellationToken);
                if (readingCount != 1) return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidState);

                var changed = await dbContext.OilChangeOperations
                    .Where(x => x.Id == oilChangeId && x.VehicleId == oldVehicleId && x.MaintenanceWorkOrderId == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.VehicleId, request.VehicleId), cancellationToken);
                if (changed != 1) return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict);
                changed = await dbContext.MaintenanceMaterialUsages
                    .Where(x => usageIds.Contains(x.Id) && x.VehicleId == oldVehicleId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.VehicleId, request.VehicleId)
                        .SetProperty(x => x.RiderVehicleAssignmentId, assignment == null ? null : assignment.Id)
                        .SetProperty(x => x.RiderProfileId, assignment == null ? null : assignment.RiderProfileId)
                        .SetProperty(x => x.AttributionStatus, attribution), cancellationToken);
                if (changed != usageIds.Length) return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict);
                changed = await dbContext.VehicleOdometerReadings
                    .Where(x => x.SourceType == VehicleOdometerSourceType.Maintenance &&
                        x.SourceEntityId == oilChangeId && x.VehicleId == oldVehicleId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.VehicleId, request.VehicleId), cancellationToken);
                if (changed != 1) return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict);

                var expenseIds = await dbContext.VehicleExpenses.AsNoTracking()
                    .Where(x => x.VehicleId == oldVehicleId &&
                        (x.SourceEntityType == nameof(MaintenanceMaterialUsage) && usageIds.Contains(x.SourceEntityId) ||
                         x.SourceEntityType == nameof(OilChangeOperation) && x.SourceEntityId == oilChangeId))
                    .Select(x => x.Id).ToArrayAsync(cancellationToken);
                await dbContext.VehicleExpenses
                    .Where(x => x.VehicleId == oldVehicleId &&
                        (expenseIds.Contains(x.Id) || x.ReversalOfExpenseId.HasValue && expenseIds.Contains(x.ReversalOfExpenseId.Value)))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.VehicleId, request.VehicleId)
                        .SetProperty(x => x.RiderVehicleAssignmentId, assignment == null ? null : assignment.Id)
                        .SetProperty(x => x.RiderProfileId, assignment == null ? null : assignment.RiderProfileId), cancellationToken);

                ApplyOilChangeVehicleMileage(target, operation.OdometerAtChange, operation.PerformedAtUtc);
                await RefreshOilScheduleAfterCorrectionAsync(oldVehicleId, operation.VehicleTypeSnapshot, cancellationToken);
                await RefreshOilScheduleAfterCorrectionAsync(request.VehicleId, operation.VehicleTypeSnapshot, cancellationToken);
                dbContext.AuditEntries.Add(new AuditEntry
                {
                    Id = Guid.CreateVersion7(),
                    ActorUserId = actor, ActorType = "User", SessionId = currentUser.SessionId,
                    Action = "Corrected", Category = "Maintenance", EntityType = nameof(OilChangeOperation),
                    EntityId = oilChangeId, OccurredAtUtc = UtcNow,
                    CorrelationId = currentUser.CorrelationId ?? Guid.CreateVersion7().ToString(),
                    Reason = request.Reason.Trim(), Source = nameof(MaintenanceService),
                    BeforeJson = JsonSerializer.Serialize(new { VehicleId = oldVehicleId }),
                    AfterJson = JsonSerializer.Serialize(new { request.VehicleId }),
                    CreatedAtUtc = UtcNow, CreatedByUserId = actor
                });
                // ExecuteUpdate bypasses SaveChanges change tracking, including automatic dataset version updates.
                foreach (var moduleKey in OilChangeCorrectionModuleKeys)
                {
                    var version = await dbContext.DatasetVersions.IgnoreQueryFilters()
                        .SingleOrDefaultAsync(x => x.ModuleKey == moduleKey, cancellationToken);
                    if (version is null)
                        dbContext.DatasetVersions.Add(new DatasetVersion
                        {
                            ModuleKey = moduleKey, Version = 1, LastChangedAtUtc = UtcNow
                        });
                    else
                    {
                        version.Version++;
                        version.LastChangedAtUtc = UtcNow;
                    }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                operation.VehicleId = request.VehicleId;
                var response = await MapOilChangeResponseAsync(operation, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(response);
            });
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict); }
    }

    private async Task RefreshOilScheduleAfterCorrectionAsync(Guid vehicleId, VehicleType vehicleType, CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans.AsNoTracking()
            .Where(x => x.Status == CatalogStatus.Active && x.TriggerType == MaintenanceTriggerType.OdometerWindow && x.VehicleType == vehicleType)
            .OrderBy(x => x.Code).FirstOrDefaultAsync(cancellationToken);
        if (plan is null) return;
        var latest = await dbContext.OilChangeOperations.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.PerformedAtUtc).ThenByDescending(x => x.OdometerAtChange)
            .FirstOrDefaultAsync(cancellationToken);
        var schedule = await dbContext.VehicleMaintenanceSchedules
            .SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.MaintenancePlanId == plan.Id, cancellationToken);
        if (schedule is null)
        {
            if (latest is null) return;
            schedule = new VehicleMaintenanceSchedule { VehicleId = vehicleId, MaintenancePlanId = plan.Id };
            dbContext.VehicleMaintenanceSchedules.Add(schedule);
        }
        schedule.LastCompletedWorkOrderId = latest?.MaintenanceWorkOrderId;
        schedule.LastCompletedAtUtc = latest?.PerformedAtUtc;
        schedule.LastCompletedOdometer = latest?.OdometerAtChange;
        schedule.ReminderFromOdometer = latest is null ? null : latest.OdometerAtChange + plan.ReminderAfterKilometers;
        schedule.MaximumDueOdometer = latest is null ? null : latest.OdometerAtChange + plan.MaximumAfterKilometers;
        schedule.ComputedStatus = latest is null ? MaintenanceDueStatus.NeverDone : MaintenanceDueStatus.Ok;
        schedule.ComputedAtUtc = UtcNow;
    }
}
