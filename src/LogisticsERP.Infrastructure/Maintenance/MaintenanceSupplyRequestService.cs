using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Entities.System;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<InventorySupplyRequestResponse>> CreateRiderSupplyRequestAsync(
        CreateRiderSupplyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actor)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.RequestedAtUtc == default || !ValidSupplyLines(request.Lines))
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.SupplyRequestRequired);

        var location = await GetActiveInventoryLocationAsync(request.InventoryLocationId, cancellationToken);
        if (location is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidLocation);
        if (!await dbContext.RiderProfiles.AsNoTracking().AnyAsync(x => x.Id == request.RiderProfileId, cancellationToken))
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.NotFound);

        var validation = await ValidateSupplyItemsAsync(request.Lines, InventorySupplyRequestSubjectType.Rider, cancellationToken);
        if (validation.IsFailure)
            return Result.Failure<InventorySupplyRequestResponse>(validation.Error);

        var entity = NewSupplyRequest(
            InventorySupplyRequestSubjectType.Rider,
            request.InventoryLocationId,
            request.RequestedAtUtc,
            actor,
            request.Notes,
            maintenanceWorkOrderId: null,
            vehicleId: null,
            riderProfileId: request.RiderProfileId);
        AddSupplyLines(entity.Id, request.Lines, validation.Value!);

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest); }
        return await GetSupplyRequestAsync(entity.Id, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<InventorySupplyRequestResponse>>> GetSupplyRequestsAsync(
        Guid? inventoryLocationId,
        Guid? vehicleId,
        Guid? riderProfileId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InventorySupplyRequests.AsNoTracking();
        if (inventoryLocationId.HasValue) query = query.Where(x => x.InventoryLocationId == inventoryLocationId.Value);
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        if (riderProfileId.HasValue) query = query.Where(x => x.RiderProfileId == riderProfileId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseSupplyStatus(status, out var parsed))
                return Result.Failure<IReadOnlyList<InventorySupplyRequestResponse>>(MaintenanceErrors.InvalidRequest);
            query = query.Where(x => x.Status == parsed);
        }

        var entities = await query.OrderByDescending(x => x.RequestedAtUtc).Take(500).ToArrayAsync(cancellationToken);
        var responses = new List<InventorySupplyRequestResponse>(entities.Length);
        foreach (var entity in entities)
            responses.Add(await MapSupplyRequestAsync(entity, cancellationToken));
        return Result.Success<IReadOnlyList<InventorySupplyRequestResponse>>(responses);
    }

    public async Task<Result<InventorySupplyRequestResponse>> GetOwnSupplyRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actor)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.CurrentUserUnavailable);
        var entity = await dbContext.InventorySupplyRequests.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.RequestedByUserId == actor, cancellationToken);
        if (entity is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.NotFound);
        return Result.Success(await MapSupplyRequestAsync(entity, cancellationToken));
    }

    public async Task<Result<InventorySupplyRequestResponse>> GetSupplyRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.InventorySupplyRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.NotFound);
        return Result.Success(await MapSupplyRequestAsync(entity, cancellationToken));
    }

    public async Task<Result<InventorySupplyRequestResponse>> ApproveAndIssueSupplyRequestAsync(
        Guid id,
        InventorySupplyDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actor)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OccurredAtUtc == default)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest);

        var entity = await dbContext.InventorySupplyRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.NotFound);
        if (!MatchesRowVersion(entity.RowVersion, request.RowVersion))
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.ConcurrencyConflict);
        if (entity.Status != InventorySupplyRequestStatus.PendingWarehouseApproval)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.SupplyRequestNotPending);
        if (request.OccurredAtUtc < entity.RequestedAtUtc)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest);
        if (await GetActiveInventoryLocationAsync(entity.InventoryLocationId, cancellationToken) is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidLocation);

        var lines = await dbContext.InventorySupplyRequestLines
            .Where(x => x.InventorySupplyRequestId == entity.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        if (lines.Length == 0)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.SupplyRequestRequired);

        try
        {
            Result<decimal> issueResult = entity.SubjectType switch
            {
                InventorySupplyRequestSubjectType.VehicleMaintenance =>
                    await IssueVehicleSupplyRequestAsync(entity, lines, actor, request.OccurredAtUtc, request.NextOilBarrelId, cancellationToken),
                InventorySupplyRequestSubjectType.Rider =>
                    await IssueRiderSupplyRequestAsync(entity, lines, actor, request.OccurredAtUtc, cancellationToken),
                _ => Result.Failure<decimal>(MaintenanceErrors.InvalidSubject)
            };
            if (issueResult.IsFailure)
            {
                dbContext.ChangeTracker.Clear();
                return Result.Failure<InventorySupplyRequestResponse>(issueResult.Error);
            }

            entity.Status = InventorySupplyRequestStatus.ApprovedAndIssued;
            entity.DecidedAtUtc = request.OccurredAtUtc;
            entity.DecidedByUserId = actor;
            entity.IssuedAtUtc = request.OccurredAtUtc;
            entity.IssuedByUserId = actor;
            entity.DecisionNotes = TrimOrNull(request.Notes);
            entity.TotalIssuedCost = issueResult.Value;
            AddSupplyDecisionNotification(entity, approved: true);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest);
        }

        return await GetSupplyRequestAsync(entity.Id, cancellationToken);
    }

    public Task<Result<InventorySupplyRequestResponse>> RejectSupplyRequestAsync(
        Guid id,
        InventorySupplyDecisionRequest request,
        CancellationToken cancellationToken = default) =>
        DecideUnissuedSupplyRequestAsync(id, request, InventorySupplyRequestStatus.Rejected, requireOwner: false, cancellationToken);

    public Task<Result<InventorySupplyRequestResponse>> CancelSupplyRequestAsync(
        Guid id,
        InventorySupplyDecisionRequest request,
        CancellationToken cancellationToken = default) =>
        DecideUnissuedSupplyRequestAsync(id, request, InventorySupplyRequestStatus.Cancelled, requireOwner: true, cancellationToken);

    private async Task<Result<InventorySupplyRequestResponse>> DecideUnissuedSupplyRequestAsync(
        Guid id,
        InventorySupplyDecisionRequest request,
        InventorySupplyRequestStatus status,
        bool requireOwner,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actor)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OccurredAtUtc == default || status == InventorySupplyRequestStatus.Rejected && string.IsNullOrWhiteSpace(request.Notes))
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest);

        var entity = await dbContext.InventorySupplyRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.NotFound);
        if (requireOwner && entity.RequestedByUserId != actor)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.SupplyRequestOwnership);
        if (!MatchesRowVersion(entity.RowVersion, request.RowVersion))
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.ConcurrencyConflict);
        if (entity.Status != InventorySupplyRequestStatus.PendingWarehouseApproval)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.SupplyRequestNotPending);
        if (request.OccurredAtUtc < entity.RequestedAtUtc)
            return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.InvalidRequest);

        entity.Status = status;
        entity.DecidedAtUtc = request.OccurredAtUtc;
        entity.DecidedByUserId = actor;
        entity.DecisionNotes = TrimOrNull(request.Notes);
        if (status == InventorySupplyRequestStatus.Rejected)
            AddSupplyDecisionNotification(entity, approved: false);

        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<InventorySupplyRequestResponse>(MaintenanceErrors.ConcurrencyConflict); }
        return await GetSupplyRequestAsync(entity.Id, cancellationToken);
    }

    private async Task<Result<decimal>> IssueVehicleSupplyRequestAsync(
        InventorySupplyRequest request,
        IReadOnlyList<InventorySupplyRequestLine> lines,
        Guid actor,
        DateTimeOffset issuedAtUtc,
        Guid? nextOilBarrelId,
        CancellationToken cancellationToken)
    {
        if (!request.MaintenanceWorkOrderId.HasValue)
            return Result.Failure<decimal>(MaintenanceErrors.InvalidSubject);
        var workOrder = await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == request.MaintenanceWorkOrderId.Value, cancellationToken);
        if (workOrder is null)
            return Result.Failure<decimal>(MaintenanceErrors.NotFound);
        if (workOrder.Status != MaintenanceWorkOrderStatus.Open || workOrder.VehicleId != request.VehicleId)
            return Result.Failure<decimal>(MaintenanceErrors.InvalidState);

        if (workOrder.MaintenanceType == MaintenanceType.OilChange)
            return await IssueOilChangeSupplyRequestAsync(
                workOrder, request, lines, actor, issuedAtUtc, nextOilBarrelId, cancellationToken);

        var total = 0m;
        foreach (var line in lines)
        {
            if (!line.MaintenanceUsageType.HasValue)
                return Result.Failure<decimal>(MaintenanceErrors.InvalidInventoryItem);
            var usageId = Guid.CreateVersion7();
            var result = await PostUsageTrackedAsync(workOrder, usageId, line.InventoryItemId, request.InventoryLocationId,
                line.RequestedQuantity, line.MaintenanceUsageType.Value, issuedAtUtc, line.Notes, actor,
                StockMovementType.MaintenanceUsage, cancellationToken);
            if (result.IsFailure)
                return Result.Failure<decimal>(result.Error);
            line.IssuedQuantity = line.RequestedQuantity;
            line.IssuedCost = result.Value!.TotalCost;
            line.MaintenanceMaterialUsageId = usageId;
            total += line.IssuedCost;
        }
        workOrder.Status = MaintenanceWorkOrderStatus.Completed;
        workOrder.CompletedAtUtc = issuedAtUtc;
        workOrder.WorkPerformed = "Maintenance supplies approved and issued by warehouse";
        return Result.Success(total);
    }

    private async Task<Result<decimal>> IssueOilChangeSupplyRequestAsync(
        MaintenanceWorkOrder workOrder,
        InventorySupplyRequest request,
        IReadOnlyList<InventorySupplyRequestLine> lines,
        Guid actor,
        DateTimeOffset issuedAtUtc,
        Guid? nextOilBarrelId,
        CancellationToken cancellationToken)
    {
        if (!workOrder.VehicleId.HasValue || !workOrder.OdometerAtOpen.HasValue)
            return Result.Failure<decimal>(MaintenanceErrors.OilChangeRequestRequired);

        var oilLine = lines.SingleOrDefault(x => x.MaintenanceUsageType == MaintenanceUsageType.Oil);
        var filterLines = lines.Where(x => x.MaintenanceUsageType == MaintenanceUsageType.OilFilter).ToArray();
        if (oilLine is null || filterLines.Length > 1 || lines.Count != 1 + filterLines.Length)
            return Result.Failure<decimal>(MaintenanceErrors.InvalidOilFilter);

        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == workOrder.VehicleId.Value, cancellationToken);
        if (vehicle is null)
            return Result.Failure<decimal>(MaintenanceErrors.NotFound);
        if (workOrder.OdometerAtOpen.Value < vehicle.CurrentOdometer)
            return Result.Failure<decimal>(MaintenanceErrors.InvalidOdometer);

        var operationId = Guid.CreateVersion7();
        var oilUsageId = Guid.CreateVersion7();
        var oilPosting = await PostUsageTrackedAsync(workOrder, oilUsageId, oilLine.InventoryItemId,
            request.InventoryLocationId, oilLine.RequestedQuantity, MaintenanceUsageType.Oil, issuedAtUtc,
            oilLine.Notes, actor, StockMovementType.MaintenanceUsage, cancellationToken, nextOilBarrelId);
        if (oilPosting.IsFailure)
            return Result.Failure<decimal>(oilPosting.Error);
        oilLine.IssuedQuantity = oilLine.RequestedQuantity;
        oilLine.IssuedCost = oilPosting.Value!.TotalCost;
        oilLine.MaintenanceMaterialUsageId = oilUsageId;

        UsagePosting? filterPosting = null;
        Guid? filterUsageId = null;
        if (filterLines.Length == 1)
        {
            filterUsageId = Guid.CreateVersion7();
            var filterLine = filterLines[0];
            var posted = await PostUsageTrackedAsync(workOrder, filterUsageId.Value, filterLine.InventoryItemId,
                request.InventoryLocationId, filterLine.RequestedQuantity, MaintenanceUsageType.OilFilter, issuedAtUtc,
                filterLine.Notes, actor, StockMovementType.MaintenanceUsage, cancellationToken);
            if (posted.IsFailure)
                return Result.Failure<decimal>(posted.Error);
            filterPosting = posted.Value;
            filterLine.IssuedQuantity = filterLine.RequestedQuantity;
            filterLine.IssuedCost = filterPosting!.TotalCost;
            filterLine.MaintenanceMaterialUsageId = filterUsageId;
        }

        VehicleMileageRules.ApplyVerifiedReading(vehicle, workOrder.OdometerAtOpen.Value, issuedAtUtc);
        dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
        {
            VehicleId = vehicle.Id,
            Reading = workOrder.OdometerAtOpen.Value,
            RecordedAtUtc = issuedAtUtc,
            SourceType = VehicleOdometerSourceType.Maintenance,
            SourceEntityId = operationId,
            Notes = "Oil change approved by warehouse"
        });

        var operation = new OilChangeOperation
        {
            Id = operationId,
            MaintenanceWorkOrderId = workOrder.Id,
            VehicleId = vehicle.Id,
            PerformedAtUtc = issuedAtUtc,
            OdometerAtChange = workOrder.OdometerAtOpen.Value,
            VehicleTypeSnapshot = vehicle.VehicleType,
            OilInventoryItemId = oilLine.InventoryItemId,
            OilQuantityLiters = oilLine.RequestedQuantity,
            OilMaterialUsageId = oilUsageId,
            OilCost = oilLine.IssuedCost,
            OilFilterChanged = filterLines.Length == 1,
            OilFilterInventoryItemId = filterLines.SingleOrDefault()?.InventoryItemId,
            OilFilterMaterialUsageId = filterUsageId,
            OilFilterCost = filterPosting?.TotalCost ?? 0,
            LaborCost = 0,
            OtherCost = 0,
            TotalCost = oilLine.IssuedCost + (filterPosting?.TotalCost ?? 0),
            PerformedByUserId = actor,
            Notes = request.Notes
        };
        dbContext.OilChangeOperations.Add(operation);
        workOrder.OdometerAtCompletion = workOrder.OdometerAtOpen;
        workOrder.CompletedAtUtc = issuedAtUtc;
        workOrder.Status = MaintenanceWorkOrderStatus.Completed;
        workOrder.WorkPerformed = "Oil change approved and issued by warehouse";
        await UpdateOilScheduleAsync(vehicle.Id, workOrder.Id, operation, vehicle.VehicleType, cancellationToken);
        return Result.Success(operation.TotalCost);
    }

    private async Task<Result<decimal>> IssueRiderSupplyRequestAsync(
        InventorySupplyRequest request,
        IReadOnlyList<InventorySupplyRequestLine> lines,
        Guid actor,
        DateTimeOffset issuedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!request.RiderProfileId.HasValue)
            return Result.Failure<decimal>(MaintenanceErrors.InvalidSubject);

        var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking()
            .Where(x => x.RiderProfileId == request.RiderProfileId.Value && x.StartedAtUtc <= issuedAtUtc
                && (!x.EndedAtUtc.HasValue || x.EndedAtUtc >= issuedAtUtc))
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var issueId = Guid.CreateVersion7();
        var movementId = Guid.CreateVersion7();
        dbContext.StockMovements.Add(NewMovement(movementId, StockMovementType.RiderIssue, issuedAtUtc,
            request.InventoryLocationId, null, nameof(RiderInventoryIssue), issueId, "Approved rider supply request", actor));
        dbContext.RiderInventoryIssues.Add(new RiderInventoryIssue
        {
            Id = issueId,
            IssueNumber = NewNumber("RDI", issuedAtUtc, issueId),
            RiderProfileId = request.RiderProfileId.Value,
            IssuedFromLocationId = request.InventoryLocationId,
            IssuedAtUtc = issuedAtUtc,
            IssuedByUserId = actor,
            RelatedAssignmentId = assignment?.Id,
            Notes = request.Notes,
            PostedMovementId = movementId
        });

        var total = 0m;
        foreach (var line in lines)
        {
            var item = await dbContext.InventoryItems.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == line.InventoryItemId && x.Status == CatalogStatus.Active
                    && x.ItemType == InventoryItemType.RiderAccessory, cancellationToken);
            if (item is null)
                return Result.Failure<decimal>(MaintenanceErrors.InvalidInventoryItem);
            var allocationResult = await AllocateTrackedLayersAsync(item.Id, request.InventoryLocationId, line.RequestedQuantity, issuedAtUtc, cancellationToken);
            if (allocationResult.IsFailure)
                return Result.Failure<decimal>(allocationResult.Error);

            var lineCost = allocationResult.Value!.Sum(x => x.Cost);
            var issueLineId = Guid.CreateVersion7();
            var movementLineId = Guid.CreateVersion7();
            dbContext.StockMovementLines.Add(new StockMovementLine
            {
                Id = movementLineId,
                StockMovementId = movementId,
                InventoryItemId = item.Id,
                Quantity = line.RequestedQuantity,
                BaseUnitOfMeasure = item.BaseUnitOfMeasure,
                UnitCost = lineCost / line.RequestedQuantity,
                TotalCost = lineCost
            });
            dbContext.RiderInventoryIssueLines.Add(new RiderInventoryIssueLine
            {
                Id = issueLineId,
                RiderInventoryIssueId = issueId,
                InventoryItemId = item.Id,
                Quantity = line.RequestedQuantity,
                TotalCost = lineCost,
                StockMovementLineId = movementLineId,
                ExpectedReturn = line.ExpectedReturn
            });
            foreach (var allocation in allocationResult.Value!)
                dbContext.StockCostAllocations.Add(new StockCostAllocation
                {
                    StockMovementLineId = movementLineId,
                    RiderInventoryIssueLineId = issueLineId,
                    StockCostLayerId = allocation.Layer.Id,
                    AllocatedQuantity = allocation.Quantity,
                    UnitCost = allocation.Layer.UnitCost,
                    AllocatedCost = allocation.Cost
                });

            line.IssuedQuantity = line.RequestedQuantity;
            line.IssuedCost = lineCost;
            line.RiderInventoryIssueLineId = issueLineId;
            total += lineCost;
        }
        request.RiderInventoryIssueId = issueId;
        return Result.Success(total);
    }

    private async Task<Result<InventorySupplyRequest>> BuildMaintenanceSupplyRequestAsync(
        MaintenanceWorkOrder workOrder,
        MaintenanceSupplyRequestInput input,
        Guid actor,
        CancellationToken cancellationToken)
    {
        if (workOrder.ServiceSubjectType != MaintenanceServiceSubjectType.CompanyVehicle || !workOrder.VehicleId.HasValue)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.InvalidSubject);
        if (workOrder.MaintenanceType == MaintenanceType.OilChange)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.InvalidRequest);
        if (!ValidSupplyLines(input.Lines))
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.SupplyRequestRequired);

        var inventoryLocation = await GetActiveInventoryLocationAsync(input.InventoryLocationId, cancellationToken);
        if (inventoryLocation is null || inventoryLocation.MaintenanceLocationId != workOrder.MaintenanceLocationId)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.SupplyRequestLocationMismatch(input.InventoryLocationId, workOrder.MaintenanceLocationId));
        var validation = await ValidateSupplyItemsAsync(input.Lines, InventorySupplyRequestSubjectType.VehicleMaintenance, cancellationToken);
        if (validation.IsFailure)
            return Result.Failure<InventorySupplyRequest>(validation.Error);

        var entity = NewSupplyRequest(InventorySupplyRequestSubjectType.VehicleMaintenance, input.InventoryLocationId,
            workOrder.OpenedAtUtc, actor, input.Notes, workOrder.Id, workOrder.VehicleId, null);
        AddSupplyLines(entity.Id, input.Lines, validation.Value!);
        workOrder.EstimatedCost = await EstimateSupplyRequestCostAsync(input.InventoryLocationId, input.Lines, cancellationToken);
        return Result.Success(entity);
    }

    private async Task<Result<InventorySupplyRequest>> BuildOilChangeSupplyRequestAsync(
        MaintenanceWorkOrder workOrder,
        OilChangeSupplyRequestInput input,
        Guid actor,
        CancellationToken cancellationToken)
    {
        if (workOrder.ServiceSubjectType != MaintenanceServiceSubjectType.CompanyVehicle
            || !workOrder.VehicleId.HasValue
            || workOrder.MaintenanceType != MaintenanceType.OilChange
            || !workOrder.OdometerAtOpen.HasValue)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.OilChangeRequestRequired);
        if (input.OilFilterChanged != input.OilFilterInventoryItemId.HasValue)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.InvalidOilFilter);

        var inventoryLocation = await GetActiveInventoryLocationAsync(input.InventoryLocationId, cancellationToken);
        if (inventoryLocation is null || inventoryLocation.MaintenanceLocationId != workOrder.MaintenanceLocationId)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.SupplyRequestLocationMismatch(input.InventoryLocationId, workOrder.MaintenanceLocationId));

        var vehicle = await dbContext.Vehicles.AsNoTracking()
            .Where(x => x.Id == workOrder.VehicleId.Value)
            .Select(x => new { x.VehicleType, x.VehicleModelId, x.CurrentOdometer })
            .SingleOrDefaultAsync(cancellationToken);
        if (vehicle is null)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.NotFound);
        if (workOrder.OdometerAtOpen.Value < vehicle.CurrentOdometer)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.InvalidOdometer);

        var configuredQuantity = await dbContext.MaintenancePlans.AsNoTracking()
            .Where(x => x.Status == CatalogStatus.Active && x.DefaultOilQuantityLiters.HasValue
                && (x.VehicleModelId == vehicle.VehicleModelId || !x.VehicleModelId.HasValue && x.VehicleType == vehicle.VehicleType))
            .OrderByDescending(x => x.VehicleModelId.HasValue)
            .Select(x => x.DefaultOilQuantityLiters)
            .FirstOrDefaultAsync(cancellationToken);
        var oilQuantity = LogisticsERP.Domain.Maintenance.MaintenanceBusinessRules.ResolveOilQuantityLiters(
            vehicle.VehicleType, input.OilFilterChanged, configuredQuantity);
        if (!oilQuantity.HasValue)
            return Result.Failure<InventorySupplyRequest>(MaintenanceErrors.InvalidOilQuantity);

        var lines = new List<InventorySupplyRequestLineInput>
        {
            new(input.OilInventoryItemId, oilQuantity.Value, MaintenanceUsageType.Oil, Notes: input.Notes)
        };
        if (input.OilFilterChanged)
            lines.Add(new InventorySupplyRequestLineInput(input.OilFilterInventoryItemId!.Value, 1,
                MaintenanceUsageType.OilFilter, Notes: input.Notes));

        var validation = await ValidateSupplyItemsAsync(lines, InventorySupplyRequestSubjectType.VehicleMaintenance, cancellationToken, allowOil: true);
        if (validation.IsFailure)
            return Result.Failure<InventorySupplyRequest>(validation.Error);

        var entity = NewSupplyRequest(InventorySupplyRequestSubjectType.VehicleMaintenance, input.InventoryLocationId,
            workOrder.OpenedAtUtc, actor, input.Notes, workOrder.Id, workOrder.VehicleId, null);
        AddSupplyLines(entity.Id, lines, validation.Value!);
        workOrder.EstimatedCost = await EstimateSupplyRequestCostAsync(input.InventoryLocationId, lines, cancellationToken);
        return Result.Success(entity);
    }

    private async Task<decimal> EstimateSupplyRequestCostAsync(
        Guid inventoryLocationId,
        IReadOnlyList<InventorySupplyRequestLineInput> lines,
        CancellationToken cancellationToken)
    {
        var itemIds = lines.Select(x => x.InventoryItemId).Distinct().ToArray();
        var averageCosts = await dbContext.StockBalances.AsNoTracking()
            .Where(x => x.InventoryLocationId == inventoryLocationId && itemIds.Contains(x.InventoryItemId))
            .ToDictionaryAsync(x => x.InventoryItemId, x => x.ReportingAverageUnitCost, cancellationToken);
        return decimal.Round(lines.Sum(x => x.Quantity * averageCosts.GetValueOrDefault(x.InventoryItemId)),
            2, MidpointRounding.AwayFromZero);
    }

    private InventorySupplyRequest NewSupplyRequest(
        InventorySupplyRequestSubjectType subjectType,
        Guid inventoryLocationId,
        DateTimeOffset requestedAtUtc,
        Guid actor,
        string? notes,
        Guid? maintenanceWorkOrderId,
        Guid? vehicleId,
        Guid? riderProfileId)
    {
        var id = Guid.CreateVersion7();
        var entity = new InventorySupplyRequest
        {
            Id = id,
            RequestNumber = NewNumber("ISR", requestedAtUtc, id),
            SubjectType = subjectType,
            InventoryLocationId = inventoryLocationId,
            MaintenanceWorkOrderId = maintenanceWorkOrderId,
            VehicleId = vehicleId,
            RiderProfileId = riderProfileId,
            RequestedAtUtc = requestedAtUtc,
            RequestedByUserId = actor,
            Notes = TrimOrNull(notes)
        };
        dbContext.InventorySupplyRequests.Add(entity);
        return entity;
    }

    private void AddSupplyLines(
        Guid requestId,
        IReadOnlyList<InventorySupplyRequestLineInput> lines,
        IReadOnlyDictionary<Guid, InventoryItem> items)
    {
        foreach (var input in lines)
        {
            var item = items[input.InventoryItemId];
            dbContext.InventorySupplyRequestLines.Add(new InventorySupplyRequestLine
            {
                Id = Guid.CreateVersion7(),
                InventorySupplyRequestId = requestId,
                InventoryItemId = item.Id,
                RequestedQuantity = input.Quantity,
                MaintenanceUsageType = input.MaintenanceUsageType ?? InferMaintenanceUsageType(item.ItemType),
                ExpectedReturn = input.ExpectedReturn,
                Notes = TrimOrNull(input.Notes)
            });
        }
    }

    private async Task<Result<IReadOnlyDictionary<Guid, InventoryItem>>> ValidateSupplyItemsAsync(
        IReadOnlyList<InventorySupplyRequestLineInput> lines,
        InventorySupplyRequestSubjectType subjectType,
        CancellationToken cancellationToken,
        bool allowOil = false)
    {
        var ids = lines.Select(x => x.InventoryItemId).Distinct().ToArray();
        var items = await dbContext.InventoryItems.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.Status == CatalogStatus.Active)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (items.Count != ids.Length)
            return Result.Failure<IReadOnlyDictionary<Guid, InventoryItem>>(MaintenanceErrors.InvalidInventoryItem);

        foreach (var line in lines)
        {
            var item = items[line.InventoryItemId];
            if (subjectType == InventorySupplyRequestSubjectType.Rider)
            {
                if (item.ItemType != InventoryItemType.RiderAccessory || line.MaintenanceUsageType.HasValue)
                    return Result.Failure<IReadOnlyDictionary<Guid, InventoryItem>>(MaintenanceErrors.InvalidInventoryItem);
                continue;
            }

            var usageType = line.MaintenanceUsageType ?? InferMaintenanceUsageType(item.ItemType);
            var valid = allowOil && item.ItemType == InventoryItemType.Oil
                    && item.BaseUnitOfMeasure == InventoryUnitOfMeasure.Liter
                    && usageType == MaintenanceUsageType.Oil
                || item.ItemType == InventoryItemType.SparePart
                    && (usageType == MaintenanceUsageType.SparePart
                        || usageType == MaintenanceUsageType.OilFilter && item.BaseUnitOfMeasure == InventoryUnitOfMeasure.Piece)
                || item.ItemType == InventoryItemType.Consumable && usageType == MaintenanceUsageType.Consumable;
            if (!valid || line.ExpectedReturn)
                return Result.Failure<IReadOnlyDictionary<Guid, InventoryItem>>(MaintenanceErrors.InvalidInventoryItem);
        }
        return Result.Success<IReadOnlyDictionary<Guid, InventoryItem>>(items);
    }

    private async Task<InventorySupplyRequestResponse> MapSupplyRequestAsync(
        InventorySupplyRequest entity,
        CancellationToken cancellationToken)
    {
        var locationName = await dbContext.InventoryLocations.AsNoTracking()
            .Where(x => x.Id == entity.InventoryLocationId).Select(x => x.NameAr).SingleAsync(cancellationToken);
        string? workOrderNumber = null;
        if (entity.MaintenanceWorkOrderId.HasValue)
            workOrderNumber = await dbContext.MaintenanceWorkOrders.AsNoTracking().Where(x => x.Id == entity.MaintenanceWorkOrderId.Value)
                .Select(x => x.WorkOrderNumber).SingleOrDefaultAsync(cancellationToken);
        string? assetNumber = null;
        string? plateNumber = null;
        if (entity.VehicleId.HasValue)
        {
            var vehicle = await dbContext.Vehicles.AsNoTracking().Where(x => x.Id == entity.VehicleId.Value)
                .Select(x => new { x.AssetNumber, x.PlateNumberAr, x.PlateNumberEn }).SingleOrDefaultAsync(cancellationToken);
            assetNumber = vehicle?.AssetNumber;
            plateNumber = vehicle?.PlateNumberAr ?? vehicle?.PlateNumberEn;
        }
        string? riderName = null;
        if (entity.RiderProfileId.HasValue)
            riderName = await (from rider in dbContext.RiderProfiles.AsNoTracking()
                               join employee in dbContext.Employees.AsNoTracking() on rider.EmployeeId equals employee.Id
                               where rider.Id == entity.RiderProfileId.Value
                               select employee.FullNameAr).SingleOrDefaultAsync(cancellationToken);

        var lines = await (from line in dbContext.InventorySupplyRequestLines.AsNoTracking()
                           join item in dbContext.InventoryItems.AsNoTracking() on line.InventoryItemId equals item.Id
                           where line.InventorySupplyRequestId == entity.Id
                           orderby line.CreatedAtUtc, line.Id
                           select new InventorySupplyRequestLineResponse(line.Id, line.InventoryItemId, item.Sku, item.NameAr,
                               item.ItemType, item.BaseUnitOfMeasure, line.RequestedQuantity, line.IssuedQuantity,
                               line.MaintenanceUsageType, line.ExpectedReturn, line.IssuedCost, line.Notes))
            .ToArrayAsync(cancellationToken);

        return new InventorySupplyRequestResponse(entity.Id, entity.RequestNumber, entity.SubjectType, entity.Status,
            entity.InventoryLocationId, locationName, entity.MaintenanceWorkOrderId, workOrderNumber, entity.VehicleId,
            assetNumber, plateNumber, entity.RiderProfileId, riderName, entity.RiderInventoryIssueId, entity.RequestedAtUtc,
            entity.RequestedByUserId, entity.DecidedAtUtc, entity.DecidedByUserId, entity.IssuedAtUtc, entity.IssuedByUserId,
            entity.DecisionNotes, entity.Notes, entity.TotalIssuedCost, lines, EncodeRowVersion(entity.RowVersion));
    }

    private void AddSupplyDecisionNotification(InventorySupplyRequest entity, bool approved)
    {
        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.CreateVersion7(),
            RecipientUserId = entity.RequestedByUserId,
            EventType = approved ? "inventory.supply_request.approved" : "inventory.supply_request.rejected",
            Severity = approved ? NotificationSeverity.Success : NotificationSeverity.Warning,
            TitleAr = approved ? "تم اعتماد وتسليم طلب الصرف" : "تم رفض طلب الصرف",
            TitleEn = approved ? "Supply request approved and issued" : "Supply request rejected",
            BodyAr = approved
                ? $"تم اعتماد الطلب {entity.RequestNumber} وتسليم الأصناف من المستودع."
                : $"تم رفض الطلب {entity.RequestNumber}. راجع سبب الرفض.",
            BodyEn = approved
                ? $"Request {entity.RequestNumber} was approved and the items were issued by the warehouse."
                : $"Request {entity.RequestNumber} was rejected. Review the rejection reason.",
            SourceEntityType = "inventory-supply-request",
            SourceEntityId = entity.Id,
            DeepLink = entity.MaintenanceWorkOrderId.HasValue
                ? $"/maintenance/work-orders/{entity.MaintenanceWorkOrderId.Value}"
                : $"/maintenance/inventory/my-supply-requests/{entity.Id}",
            DeduplicationKey = $"inventory-supply-request:{entity.Id:N}:{(approved ? "approved" : "rejected")}",
            VisibleAtUtc = UtcNow
        });
    }

    private static bool ValidSupplyLines(IReadOnlyList<InventorySupplyRequestLineInput>? lines) =>
        lines is { Count: > 0 and <= 100 }
        && lines.All(x => x.InventoryItemId != Guid.Empty && x.Quantity > 0)
        && lines.Select(x => x.InventoryItemId).Distinct().Count() == lines.Count;

    private static MaintenanceUsageType? InferMaintenanceUsageType(InventoryItemType itemType) => itemType switch
    {
        InventoryItemType.SparePart => MaintenanceUsageType.SparePart,
        InventoryItemType.Oil => MaintenanceUsageType.Oil,
        InventoryItemType.Consumable => MaintenanceUsageType.Consumable,
        _ => null
    };

    private static bool TryParseSupplyStatus(string value, out InventorySupplyRequestStatus status)
    {
        status = value.Trim().ToLowerInvariant() switch
        {
            "pending" => InventorySupplyRequestStatus.PendingWarehouseApproval,
            "approved" or "issued" or "approvedandissued" => InventorySupplyRequestStatus.ApprovedAndIssued,
            "rejected" => InventorySupplyRequestStatus.Rejected,
            "cancelled" or "canceled" => InventorySupplyRequestStatus.Cancelled,
            _ => default
        };
        return status != default || Enum.TryParse(value, true, out status) && Enum.IsDefined(status);
    }
}
