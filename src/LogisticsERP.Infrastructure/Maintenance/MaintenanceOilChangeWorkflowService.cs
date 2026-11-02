using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    private Task<bool> OilAllowed(string permission, CancellationToken ct) =>
        currentUser.UserId is Guid actor && currentUser.AuthorizationVersion is long version && oilPermissionChecker is not null
            ? oilPermissionChecker.HasPermissionAsync(actor, version, permission, null, ct) : Task.FromResult(false);

    public async Task<Result<OilChangeWorkflowDetails>> GetOilChangeDetailsAsync(Guid id, CancellationToken ct = default)
    {
        if (!await OilAllowed(PermissionKeys.Maintenance.OilRead, ct)
            || !await OilAllowed(PermissionKeys.Inventory.StockRead, ct)
            || !await OilAllowed(PermissionKeys.Inventory.CostLayersRead, ct))
            return Result.Failure<OilChangeWorkflowDetails>(MaintenanceErrors.Forbidden);
        var op = await dbContext.OilChangeOperations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (op is null) return Result.Failure<OilChangeWorkflowDetails>(MaintenanceErrors.NotFound);
        var materials = new List<MaintenanceMaterialUsageResponse>();
        foreach (var usageId in OilUsageIds(op))
        {
            var usage = await MapUsageAsync(usageId, ct);
            if (usage.IsFailure) return Result.Failure<OilChangeWorkflowDetails>(usage.Error);
            materials.Add(usage.Value!);
        }
        var workOrder = op.MaintenanceWorkOrderId is Guid workId
            ? await dbContext.MaintenanceWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == workId, ct) : null;
        var asset = op.VehicleId is Guid vehicleId
            ? await dbContext.Vehicles.AsNoTracking().Where(x => x.Id == vehicleId).Select(x => x.AssetNumber).SingleOrDefaultAsync(ct) : null;
        var report = new OilChangeReportResponse(op.Id, op.MaintenanceWorkOrderId, op.VehicleId, asset,
            op.PerformedAtUtc, op.OdometerAtChange, op.VehicleTypeSnapshot, op.OilInventoryItemId,
            op.OilQuantityLiters, op.OilCost, op.OilFilterChanged, op.OilFilterInventoryItemId, op.OilFilterCost,
            op.LaborCost, op.OtherCost, op.TotalCost, op.OilMaterialUsageId, op.OilFilterMaterialUsageId,
            op.PerformedByUserId, op.Notes, EncodeRowVersion(op.RowVersion), op.IsCancelled, op.RevisionNumber);
        return Result.Success(new OilChangeWorkflowDetails(report, workOrder is null ? null : EncodeRowVersion(workOrder.RowVersion),
            materials[0].InventoryLocationId, materials, op.CancelledAtUtc, op.CancelledByUserId, op.CancellationReason));
    }

    public async Task<Result<OilChangeRevisionPage>> GetOilChangeRevisionsAsync(Guid id, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await OilAllowed(PermissionKeys.Maintenance.OilRead, ct)
            || !await OilAllowed(PermissionKeys.Inventory.StockRead, ct)
            || !await OilAllowed(PermissionKeys.Inventory.CostLayersRead, ct))
            return Result.Failure<OilChangeRevisionPage>(MaintenanceErrors.Forbidden);
        if (!await dbContext.OilChangeOperations.AnyAsync(x => x.Id == id, ct))
            return Result.Failure<OilChangeRevisionPage>(MaintenanceErrors.NotFound);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        var offset = ((long)page - 1) * pageSize;
        var query = dbContext.OilChangeRevisions.AsNoTracking().Where(x => x.OilChangeOperationId == id);
        var count = await query.CountAsync(ct);
        var items = offset >= count ? [] : await query.OrderByDescending(x => x.RevisionNumber)
            .Skip((int)offset).Take(pageSize).Select(x => new OilChangeRevisionResponse(x.Id, x.RevisionNumber,
                x.Action, x.Reason, x.ActorUserId, x.PostedAtUtc, x.BeforeJson, x.AfterJson, x.EffectsJson)).ToArrayAsync(ct);
        return Result.Success(new OilChangeRevisionPage(items, page, pageSize, count));
    }

    public Task<Result<OilChangeWorkflowResponse>> EditOilChangeAsync(Guid id, OilChangeEditRequest request,
        string key, bool preview, CancellationToken ct = default) =>
        RunOilChangeWorkflowAsync(id, request, null, key, preview, ct);

    public Task<Result<OilChangeWorkflowResponse>> CancelOilChangeAsync(Guid id, OilChangeCancelRequest request,
        string key, bool preview, CancellationToken ct = default) =>
        RunOilChangeWorkflowAsync(id, null, request, key, preview, ct);

    private async Task<Result<OilChangeWorkflowResponse>> RunOilChangeWorkflowAsync(Guid id, OilChangeEditRequest? edit,
        OilChangeCancelRequest? cancel, string key, bool preview, CancellationToken ct)
    {
        var permission = cancel is null ? PermissionKeys.Maintenance.OilEdit : PermissionKeys.Maintenance.OilCancel;
        var stockPermission = cancel is null ? PermissionKeys.Inventory.StockMove : PermissionKeys.Inventory.StockAdjust;
        if (!await OilAllowed(permission, ct) || !await OilAllowed(stockPermission, ct)
            || !await OilAllowed(PermissionKeys.Inventory.CostLayersRead, ct))
            return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.Forbidden);
        var reason = edit?.Reason ?? cancel?.Reason;
        var version = edit?.RowVersion ?? cancel?.RowVersion;
        var expected = edit?.ExpectedEffectsHash ?? cancel?.ExpectedEffectsHash;
        var basis = edit?.MileageBaselineKm ?? cancel?.MileageBaselineKm;
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000
            || string.IsNullOrWhiteSpace(version) || basis < 0
            || !preview && (string.IsNullOrWhiteSpace(key) || key.Length > 200 || string.IsNullOrWhiteSpace(expected)))
            return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidRequest);
        if (edit is not null && (edit.PerformedAtUtc == default || edit.PerformedAtUtc > UtcNow
            || edit.OdometerAtChange < 0 || edit.OdometerAtChange > 9_999_999_999_999_999L
            || edit.LaborCost < 0 || edit.OtherCost < 0 || edit.LaborCost > 1_000_000_000m || edit.OtherCost > 1_000_000_000m
            || decimal.Round(edit.LaborCost, 2) != edit.LaborCost || decimal.Round(edit.OtherCost, 2) != edit.OtherCost
            || edit.InventoryLocationId == Guid.Empty || edit.OilInventoryItemId == Guid.Empty
            || edit.OilFilterChanged != edit.OilFilterInventoryItemId.HasValue || edit.Notes?.Length > 2000))
            return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidRequest);
        // This service owns its unit of work; never discard another operation's pending writes.
        if (dbContext.ChangeTracker.HasChanges()) return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidState);
        var requestHash = OilHash(new { id, Edit = edit is null ? null : edit with { ExpectedEffectsHash = null },
            Cancel = cancel is null ? null : cancel with { ExpectedEffectsHash = null } });
        try
        {
            return await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                dbContext.ChangeTracker.Clear();
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                try
                {
                    if (!preview)
                    {
                        var previous = await dbContext.OilChangeRevisions.AsNoTracking()
                            .SingleOrDefaultAsync(x => x.OilChangeOperationId == id && x.IdempotencyKey == key, ct);
                        if (previous is not null)
                        {
                            if (previous.Action == "EditDate" && !await OilAllowed(PermissionKeys.Maintenance.OilDateEdit, ct))
                                return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.Forbidden);
                            if (previous.RequestHash != requestHash)
                                return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.OilChangeIdempotencyConflict);
                            return Result.Success(JsonSerializer.Deserialize<OilChangeWorkflowResponse>(previous.ResultJson)!);
                        }
                    }
                    var op = await dbContext.OilChangeOperations.SingleOrDefaultAsync(x => x.Id == id, ct);
                    if (op is null) return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.NotFound);
                    if (op.IsCancelled) return Result.Failure<OilChangeWorkflowResponse>(OilChangeWorkflowErrors.Cancelled);
                    if (!MatchesRowVersion(op.RowVersion, version))
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.ConcurrencyConflict);
                    var dateChanged = edit is not null && edit.PerformedAtUtc != op.PerformedAtUtc;
                    if (dateChanged && !await OilAllowed(PermissionKeys.Maintenance.OilDateEdit, ct))
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.Forbidden);
                    var workOrder = op.MaintenanceWorkOrderId is Guid workId
                        ? await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == workId, ct) : null;
                    if (op.MaintenanceWorkOrderId.HasValue && (workOrder is null || workOrder.MaintenanceType != MaintenanceType.OilChange
                        || workOrder.Status is not (MaintenanceWorkOrderStatus.Completed or MaintenanceWorkOrderStatus.Closed)))
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidState);
                    if (workOrder is not null && !MatchesRowVersion(workOrder.RowVersion, edit?.WorkOrderRowVersion ?? cancel?.WorkOrderRowVersion))
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.ConcurrencyConflict);
                    if (edit is not null && (op.VehicleId.HasValue && edit.LaborCost != 0
                        || workOrder is null && edit.LaborCost != 0))
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.LaborCostExternalVehiclesOnly);
                    var originalVersion = EncodeRowVersion(op.RowVersion);
                    var workVersion = workOrder is null ? null : EncodeRowVersion(workOrder.RowVersion);
                    var oldOil = await dbContext.MaintenanceMaterialUsages.SingleOrDefaultAsync(x => x.Id == op.OilMaterialUsageId, ct);
                    if (oldOil is null) return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidState);
                    var quantity = edit is null ? op.OilQuantityLiters : MaintenanceBusinessRules.ResolveOilQuantityLiters(
                        op.VehicleTypeSnapshot, edit.OilFilterChanged, edit.ConfiguredOilQuantityLiters ?? op.OilQuantityLiters);
                    if (!quantity.HasValue || quantity <= 0 || decimal.Round(quantity.Value, 3) != quantity.Value)
                        return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidOilQuantity);
                    var replace = edit is not null && (op.OilInventoryItemId != edit.OilInventoryItemId
                        || op.OilFilterChanged != edit.OilFilterChanged || op.OilFilterInventoryItemId != edit.OilFilterInventoryItemId
                        || oldOil.InventoryLocationId != edit.InventoryLocationId || op.OilQuantityLiters != quantity.Value);
                    var oldIds = OilUsageIds(op);
                    var originalMaterials = await dbContext.MaintenanceMaterialUsages.AsNoTracking()
                        .Where(x => oldIds.Contains(x.Id)).ToArrayAsync(ct);
                    var revision = new OilChangeRevision { OilChangeOperationId = id, RevisionNumber = op.RevisionNumber + 1,
                        Action = cancel is not null ? "Cancel" : dateChanged ? "EditDate" : "Edit",
                        IdempotencyKey = preview ? Guid.NewGuid().ToString() : key, RequestHash = requestHash,
                        Reason = reason.Trim(), ActorUserId = currentUser.UserId!.Value, PostedAtUtc = UtcNow,
                        OldOilUsageId = op.OilMaterialUsageId, OldFilterUsageId = op.OilFilterMaterialUsageId,
                        BeforeJson = JsonSerializer.Serialize(new { Operation = op, Materials = originalMaterials, WorkOrder = workOrder }) };
                    dbContext.OilChangeRevisions.Add(revision);
                    dbContext.ActiveOilChangeRevisionId = revision.Id;
                    var oldLabor = op.LaborCost; var oldOther = op.OtherCost; var oldTotal = op.TotalCost;
                    var originalStatus = workOrder?.Status;
                    if (replace || cancel is not null)
                    {
                        foreach (var usageId in oldIds)
                        {
                            var validSource = await ValidateOilReversalSourceAsync(usageId, ct);
                            if (validSource.IsFailure) return Result.Failure<OilChangeWorkflowResponse>(validSource.Error);
                            var closedAllocations = await (from a in dbContext.OilBarrelUsageAllocations.AsNoTracking()
                                join b in dbContext.OilBarrels on a.OilBarrelId equals b.Id
                                where a.MaintenanceMaterialUsageId == usageId && a.Direction == MaintenanceUsageDirection.Issue
                                    && (b.Status == OilBarrelStatus.Depleted || b.Status == OilBarrelStatus.Returned)
                                select a).ToArrayAsync(ct);
                            if (closedAllocations.Length > 0 && !await OilAllowed(PermissionKeys.Inventory.StockAdjust, ct))
                                return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.Forbidden);
                            var reversed = await StageMaterialReversalAsync(usageId, Guid.CreateVersion7(),
                                new(revision.PostedAtUtc, reason.Trim()), revision.ActorUserId, true, ct, false, true);
                            if (reversed.IsFailure)
                                return Result.Failure<OilChangeWorkflowResponse>(reversed.Error == MaintenanceErrors.InvalidState
                                    ? OilChangeWorkflowErrors.BarrelRestore : reversed.Error);
                            foreach (var allocation in closedAllocations)
                                await StageClosedOilWasteAsync(allocation, revision, ct);
                        }
                        // Persist the restorations inside the enclosing transaction so FIFO queries
                        // see them. A later failure or preview rolls all these writes back.
                        await dbContext.SaveChangesAsync(ct);
                    }
                    if (replace && edit is not null)
                    {
                        if (workOrder is not null) workOrder.Status = MaintenanceWorkOrderStatus.InProgress;
                        var maintenanceLocation = workOrder?.MaintenanceLocationId ?? await dbContext.InventoryLocations
                            .Where(x => x.Id == edit.InventoryLocationId).Select(x => x.MaintenanceLocationId).SingleOrDefaultAsync(ct);
                        var oil = await PostUsageCoreAsync(workOrder, op.VehicleId, maintenanceLocation,
                            Guid.CreateVersion7(), edit.OilInventoryItemId, edit.InventoryLocationId, quantity.Value,
                            MaintenanceUsageType.Oil, revision.PostedAtUtc, edit.Notes, revision.ActorUserId,
                            StockMovementType.MaintenanceUsage, ct, edit.NextOilBarrelId);
                        if (oil.IsFailure) return Result.Failure<OilChangeWorkflowResponse>(oil.Error);
                        op.OilMaterialUsageId = oil.Value!.Usage.Id; op.OilCost = oil.Value.TotalCost;
                        op.OilFilterMaterialUsageId = null; op.OilFilterCost = 0;
                        if (edit.OilFilterChanged)
                        {
                            var filter = await PostUsageCoreAsync(workOrder, op.VehicleId, maintenanceLocation,
                                Guid.CreateVersion7(), edit.OilFilterInventoryItemId!.Value, edit.InventoryLocationId, 1,
                                MaintenanceUsageType.OilFilter, revision.PostedAtUtc, edit.Notes, revision.ActorUserId,
                                StockMovementType.MaintenanceUsage, ct);
                            if (filter.IsFailure) return Result.Failure<OilChangeWorkflowResponse>(filter.Error);
                            op.OilFilterMaterialUsageId = filter.Value!.Usage.Id; op.OilFilterCost = filter.Value.TotalCost;
                        }
                        op.OilInventoryItemId = edit.OilInventoryItemId; op.OilQuantityLiters = quantity.Value;
                        op.OilFilterChanged = edit.OilFilterChanged; op.OilFilterInventoryItemId = edit.OilFilterInventoryItemId;
                    }
                    if (cancel is not null)
                    {
                        op.IsCancelled = true; op.CancelledAtUtc = revision.PostedAtUtc;
                        op.CancelledByUserId = revision.ActorUserId; op.CancellationReason = revision.Reason;
                    }
                    else if (edit is not null)
                    {
                        op.PerformedAtUtc = edit.PerformedAtUtc; op.OdometerAtChange = edit.OdometerAtChange;
                        op.LaborCost = edit.LaborCost; op.OtherCost = edit.OtherCost; op.Notes = TrimOrNull(edit.Notes);
                        op.TotalCost = op.OilCost + op.OilFilterCost + op.LaborCost + op.OtherCost;
                    }
                    op.RevisionNumber = revision.RevisionNumber;
                    var usages = await CurrentOilUsagesAsync(op, ct);
                    await ReattributeOilServiceAsync(op, usages, workOrder, ct);
                    if (workOrder is not null)
                    {
                        workOrder.ActualLaborCost += (cancel is null ? op.LaborCost : 0) - oldLabor;
                        workOrder.ActualOtherCost += (cancel is null ? op.OtherCost : 0) - oldOther;
                        workOrder.ActualTotalCost = workOrder.ActualMaterialCost
                            + (workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle
                                ? workOrder.ActualLaborCost + workOrder.ActualOtherCost : 0);
                        var hasPayment = cancel is not null && await dbContext.ExternalCustomerPayments
                            .AnyAsync(x => x.MaintenanceWorkOrderId == workOrder.Id, ct);
                        workOrder.Status = cancel is null || workOrder.ActualTotalCost != 0 || hasPayment
                            ? originalStatus!.Value : MaintenanceWorkOrderStatus.Cancelled;
                        if (cancel is null) { workOrder.CompletedAtUtc = op.PerformedAtUtc; workOrder.OdometerAtCompletion = op.OdometerAtChange; }
                    }
                    var financials = await RebookOilFinancialsAsync(op, revision, usages[0], oldTotal, ct);
                    if (financials.IsFailure) return Result.Failure<OilChangeWorkflowResponse>(financials.Error);
                    var mileage = await ReconcileOilMileageAsync(op, revision, basis, ct);
                    if (mileage.IsFailure) return Result.Failure<OilChangeWorkflowResponse>(mileage.Error);
                    await dbContext.SaveChangesAsync(ct);
                    if (op.VehicleId is Guid vehicleId) await RefreshOilScheduleAfterCorrectionAsync(vehicleId, op.VehicleTypeSnapshot, ct);
                    await dbContext.SaveChangesAsync(ct);
                    var touchedIds = oldIds.Concat(OilUsageIds(op)).Distinct().ToArray();
                    var stagedReversals = await dbContext.MaintenanceMaterialUsages.Where(x => x.ReversalOfUsageId.HasValue
                        && oldIds.Contains(x.ReversalOfUsageId.Value)).Select(x => x.Id).ToArrayAsync(ct);
                    touchedIds = touchedIds.Concat(stagedReversals).Distinct().ToArray();
                    var barrelRows = await dbContext.OilBarrelUsageAllocations.AsNoTracking().Where(x => touchedIds.Contains(x.MaintenanceMaterialUsageId))
                        .GroupBy(x => x.OilBarrelId).Select(g => new { BarrelId = g.Key,
                            IssuedLiters = g.Sum(x => x.Direction == MaintenanceUsageDirection.Issue ? x.QuantityLiters : 0),
                            ReversedLiters = g.Sum(x => x.Direction == MaintenanceUsageDirection.Reversal ? x.QuantityLiters : 0) })
                        .OrderBy(x => x.BarrelId).ToArrayAsync(ct);
                    var barrelEffects = barrelRows.Select(x => new OilChangeBarrelEffect(x.BarrelId, x.IssuedLiters, x.ReversedLiters)).ToArray();
                    var effects = new OilChangeWorkflowEffects(op.IsCancelled, replace, op.OilQuantityLiters,
                        op.OilCost, op.OilFilterCost, op.LaborCost, op.OtherCost, op.IsCancelled ? 0 : op.TotalCost,
                        usages[0].RiderProfileId, mileage.Value, barrelEffects, workOrder?.Status,
                        await dbContext.OilBarrelLosses.Where(x => x.CorrectionRevisionId == revision.Id).SumAsync(x => x.QuantityLiters, ct));
                    var fingerprint = OilHash(new { originalVersion, workVersion, op.PerformedAtUtc, op.OdometerAtChange,
                        op.OilInventoryItemId, op.OilFilterInventoryItemId, op.Notes, Effects = effects,
                        PostingDate = revision.PostedAtUtc.ToOffset(TimeSpan.FromHours(3)).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) });
                    if (!preview && fingerprint != expected)
                        return Result.Failure<OilChangeWorkflowResponse>(OilChangeWorkflowErrors.PreviewChanged);
                    var response = new OilChangeWorkflowResponse(op.Id, preview ? null : revision.Id, preview,
                        preview ? originalVersion : EncodeRowVersion(op.RowVersion),
                        preview ? workVersion : workOrder is null ? null : EncodeRowVersion(workOrder.RowVersion),
                        revision.PostedAtUtc, fingerprint, effects);
                    if (preview) { await transaction.RollbackAsync(ct); return Result.Success(response); }
                    revision.AfterJson = JsonSerializer.Serialize(new { Operation = op, Materials = usages, WorkOrder = workOrder,
                        VehicleMileageKm = mileage.Value });
                    revision.EffectsJson = JsonSerializer.Serialize(effects);
                    revision.ResultJson = JsonSerializer.Serialize(response);
                    dbContext.AuditEntries.Add(new AuditEntry { Id = Guid.CreateVersion7(), ActorUserId = revision.ActorUserId,
                        ActorType = "User", SessionId = currentUser.SessionId, Action = revision.Action,
                        Category = "Maintenance", EntityType = nameof(OilChangeOperation), EntityId = id,
                        OccurredAtUtc = revision.PostedAtUtc, CorrelationId = currentUser.CorrelationId ?? revision.Id.ToString(),
                        Reason = revision.Reason, Source = nameof(MaintenanceService),
                        BeforeJson = revision.BeforeJson, AfterJson = revision.AfterJson, CreatedAtUtc = revision.PostedAtUtc });
                    await dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return Result.Success(response);
                }
                finally
                {
                    dbContext.ActiveOilChangeRevisionId = null;
                    dbContext.ChangeTracker.Clear();
                }
            });
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 1205 or 1222)
        { return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<OilChangeWorkflowResponse>(MaintenanceErrors.InvalidState); }
    }

    private static string OilHash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static Guid[] OilUsageIds(OilChangeOperation op) => op.OilFilterMaterialUsageId is Guid filter
        ? [op.OilMaterialUsageId, filter] : [op.OilMaterialUsageId];

    private async Task<Result> ValidateOilReversalSourceAsync(Guid usageId, CancellationToken ct)
    {
        var usage = await dbContext.MaintenanceMaterialUsages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == usageId, ct);
        if (usage is null || usage.Direction != MaintenanceUsageDirection.Issue)
            return Result.Failure(MaintenanceErrors.InvalidState);
        var allocations = await (from a in dbContext.StockCostAllocations.AsNoTracking()
            join l in dbContext.StockCostLayers.AsNoTracking() on a.StockCostLayerId equals l.Id
            where a.MaintenanceMaterialUsageId == usageId
            select new { a.AllocatedQuantity, a.AllocatedCost, l.InventoryItemId, l.InventoryLocationId }).ToArrayAsync(ct);
        if (allocations.Length == 0 || allocations.Sum(x => x.AllocatedQuantity) != usage.Quantity
            || allocations.Sum(x => x.AllocatedCost) != usage.TotalCost
            || allocations.Any(x => x.InventoryItemId != usage.InventoryItemId || x.InventoryLocationId != usage.InventoryLocationId))
            return Result.Failure(MaintenanceErrors.InvalidState);
        if (usage.UsageType == MaintenanceUsageType.Oil && await (from a in dbContext.OilBarrelUsageAllocations.AsNoTracking()
            join b in dbContext.OilBarrels.AsNoTracking() on a.OilBarrelId equals b.Id
            where a.MaintenanceMaterialUsageId == usageId && a.Direction == MaintenanceUsageDirection.Issue
                && (b.InventoryItemId != usage.InventoryItemId || b.InventoryLocationId != usage.InventoryLocationId)
            select a.Id).AnyAsync(ct)) return Result.Failure(OilChangeWorkflowErrors.BarrelRestore);
        return Result.Success();
    }

    private async Task<MaintenanceMaterialUsage[]> CurrentOilUsagesAsync(OilChangeOperation op, CancellationToken ct)
    {
        var ids = OilUsageIds(op);
        var existing = await dbContext.MaintenanceMaterialUsages.Where(x => ids.Contains(x.Id)).ToArrayAsync(ct);
        return existing.Concat(dbContext.MaintenanceMaterialUsages.Local.Where(x => ids.Contains(x.Id)))
            .DistinctBy(x => x.Id).OrderBy(x => x.Id == op.OilMaterialUsageId ? 0 : 1).ToArray();
    }

    private async Task ReattributeOilServiceAsync(OilChangeOperation op, MaintenanceMaterialUsage[] usages,
        MaintenanceWorkOrder? workOrder, CancellationToken ct)
    {
        if (op.IsCancelled) return;
        var assignment = op.VehicleId is Guid vehicleId ? await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.StartedAtUtc <= op.PerformedAtUtc
                && x.Status != RiderVehicleAssignmentStatus.Cancelled && x.Status != RiderVehicleAssignmentStatus.Corrected
                && (!x.EndedAtUtc.HasValue || x.EndedAtUtc > op.PerformedAtUtc))
            .OrderByDescending(x => x.StartedAtUtc).ThenBy(x => x.Id).FirstOrDefaultAsync(ct) : null;
        foreach (var usage in usages)
        {
            usage.EffectiveServiceAtUtc = op.PerformedAtUtc;
            usage.RiderVehicleAssignmentId = assignment?.Id; usage.RiderProfileId = assignment?.RiderProfileId;
            usage.AttributionStatus = op.VehicleId.HasValue
                ? assignment is null ? InventoryAttributionStatus.Unassigned : InventoryAttributionStatus.AssignedRider
                : InventoryAttributionStatus.ExternalVehicle;
        }
        if (workOrder is not null)
        { workOrder.RiderVehicleAssignmentId = assignment?.Id; workOrder.AttributedRiderProfileId = assignment?.RiderProfileId; }
    }
}
