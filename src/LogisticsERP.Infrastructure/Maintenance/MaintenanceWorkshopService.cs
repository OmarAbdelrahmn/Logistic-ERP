using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<MaintenanceMaterialUsageResponse>> PostMaterialUsageAsync(Guid workOrderId, PostMaterialUsageRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.Quantity <= 0 || request.UsageType is MaintenanceUsageType.Oil or MaintenanceUsageType.OilFilter or MaintenanceUsageType.ExternalPartSale)
            return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.InvalidRequest);
        var usageId = Guid.CreateVersion7();
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var workOrder = await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId, cancellationToken);
                if (workOrder is null) return Result.Failure<Guid>(MaintenanceErrors.NotFound);
                var posting = await PostUsageTrackedAsync(workOrder, usageId, request.InventoryItemId, request.InventoryLocationId, request.Quantity, request.UsageType, request.UsedAtUtc, TrimOrNull(request.Notes), actor.Value, StockMovementType.MaintenanceUsage, cancellationToken);
                if (posting.IsFailure) return Result.Failure<Guid>(posting.Error);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(usageId);
            });
            if (result.IsFailure) return Result.Failure<MaintenanceMaterialUsageResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.InvalidRequest); }
        return await MapUsageAsync(usageId, cancellationToken);
    }

    public async Task<Result<MaintenanceMaterialUsageResponse>> ReverseMaterialUsageAsync(Guid usageId, ReverseMaterialUsageRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.ReversedAtUtc == default || string.IsNullOrWhiteSpace(request.Reason)) return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.InvalidRequest);
        var reversalId = Guid.CreateVersion7();
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var original = await dbContext.MaintenanceMaterialUsages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == usageId && x.Direction == MaintenanceUsageDirection.Issue, cancellationToken);
                if (original is null) return Result.Failure<Guid>(MaintenanceErrors.NotFound);
                if (await dbContext.MaintenanceMaterialUsages.AsNoTracking().AnyAsync(x => x.ReversalOfUsageId == usageId, cancellationToken))
                    return Result.Failure<Guid>(MaintenanceErrors.AlreadyReversed);
                var workOrder = await dbContext.MaintenanceWorkOrders.SingleAsync(x => x.Id == original.MaintenanceWorkOrderId, cancellationToken);
                var allocations = await dbContext.StockCostAllocations.AsNoTracking().Where(x => x.MaintenanceMaterialUsageId == usageId).ToArrayAsync(cancellationToken);
                if (allocations.Length == 0) return Result.Failure<Guid>(MaintenanceErrors.InvalidState);
                var layerIds = allocations.Select(x => x.StockCostLayerId).ToArray();
                var layers = await dbContext.StockCostLayers.Where(x => layerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
                var movementId = Guid.CreateVersion7();
                var movementLineId = Guid.CreateVersion7();
                var movement = NewMovement(movementId, StockMovementType.Reversal, request.ReversedAtUtc, null, original.InventoryLocationId, nameof(MaintenanceMaterialUsage), reversalId, request.Reason, actor.Value);
                movement.ReversalOfMovementId = original.StockMovementId;
                dbContext.StockMovements.Add(movement);
                dbContext.StockMovementLines.Add(new StockMovementLine { Id = movementLineId, StockMovementId = movementId, InventoryItemId = original.InventoryItemId, Quantity = original.Quantity, BaseUnitOfMeasure = original.UnitOfMeasure, UnitCost = original.TotalCost / original.Quantity, TotalCost = original.TotalCost });
                dbContext.MaintenanceMaterialUsages.Add(new MaintenanceMaterialUsage
                {
                    Id = reversalId,
                    MaintenanceWorkOrderId = original.MaintenanceWorkOrderId,
                    InventoryItemId = original.InventoryItemId,
                    InventoryLocationId = original.InventoryLocationId,
                    UsageType = original.UsageType,
                    Direction = MaintenanceUsageDirection.Reversal,
                    Quantity = original.Quantity,
                    UnitOfMeasure = original.UnitOfMeasure,
                    TotalCost = original.TotalCost,
                    StockMovementId = movementId,
                    StockMovementLineId = movementLineId,
                    VehicleId = original.VehicleId,
                    RiderVehicleAssignmentId = original.RiderVehicleAssignmentId,
                    RiderProfileId = original.RiderProfileId,
                    AttributionStatus = original.AttributionStatus,
                    UsedAtUtc = request.ReversedAtUtc,
                    UsedByUserId = actor.Value,
                    Notes = request.Reason.Trim(),
                    ReversalOfUsageId = usageId
                });
                if (original.UsageType == MaintenanceUsageType.Oil)
                {
                    var barrelRestore = await RestoreOilBarrelsAsync(usageId, reversalId, cancellationToken);
                    if (barrelRestore.IsFailure) return Result.Failure<Guid>(barrelRestore.Error);
                }
                foreach (var allocation in allocations)
                {
                    var layer = layers[allocation.StockCostLayerId];
                    layer.RemainingQuantity += allocation.AllocatedQuantity;
                    dbContext.StockCostAllocations.Add(new StockCostAllocation { StockMovementLineId = movementLineId, MaintenanceMaterialUsageId = reversalId, StockCostLayerId = layer.Id, AllocatedQuantity = allocation.AllocatedQuantity, UnitCost = allocation.UnitCost, AllocatedCost = allocation.AllocatedCost });
                }
                var balance = await GetOrCreateBalanceAsync(original.InventoryItemId, original.InventoryLocationId, cancellationToken);
                AddToBalance(balance, original.Quantity, original.TotalCost, request.ReversedAtUtc);
                workOrder.ActualMaterialCost -= original.TotalCost;
                workOrder.ActualTotalCost = workOrder.ActualMaterialCost + workOrder.ActualLaborCost + workOrder.ActualOtherCost;
                if (workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle)
                {
                    var originalEntryId = await dbContext.ExternalMaintenanceFinancialEntries.AsNoTracking()
                        .Where(x => x.MaintenanceWorkOrderId == workOrder.Id
                            && x.SourceType == ExternalFinancialSourceType.InventoryCost
                            && x.SourceEntityId == usageId
                            && !x.ReversalOfEntryId.HasValue)
                        .Select(x => (Guid?)x.Id)
                        .SingleOrDefaultAsync(cancellationToken);
                    if (!originalEntryId.HasValue) return Result.Failure<Guid>(MaintenanceErrors.InvalidState);
                    AddExternalFinancialEntry(workOrder.Id, ExternalFinancialEntryType.Expense, ExternalFinancialSourceType.InventoryCost, reversalId, request.ReversedAtUtc, -original.TotalCost, 0, $"Reversal: {request.Reason.Trim()}", actor.Value, reversalOfEntryId: originalEntryId);
                }
                else if (original.VehicleId.HasValue)
                {
                    var originalExpenseId = await dbContext.VehicleExpenses.AsNoTracking()
                        .Where(x => x.SourceEntityType == nameof(MaintenanceMaterialUsage)
                            && x.SourceEntityId == usageId
                            && !x.ReversalOfExpenseId.HasValue)
                        .Select(x => (Guid?)x.Id)
                        .SingleOrDefaultAsync(cancellationToken);
                    if (!originalExpenseId.HasValue) return Result.Failure<Guid>(MaintenanceErrors.InvalidState);
                    AddVehicleExpense(original, reversalId, request.ReversedAtUtc, -original.TotalCost, $"Reversal: {request.Reason.Trim()}", originalExpenseId);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(reversalId);
            });
            if (result.IsFailure) return Result.Failure<MaintenanceMaterialUsageResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.ConcurrencyConflict); }
        return await MapUsageAsync(reversalId, cancellationToken);
    }

    public Task<Result<IReadOnlyList<MaintenanceMaterialUsageResponse>>> GetVehicleMaterialHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        GetMaterialHistoryAsync(vehicleId, null, cancellationToken);

    public Task<Result<IReadOnlyList<MaintenanceMaterialUsageResponse>>> GetRiderMaterialHistoryAsync(Guid riderProfileId, CancellationToken cancellationToken = default) =>
        GetMaterialHistoryAsync(null, riderProfileId, cancellationToken);

    public async Task<Result<OilChangeResponse>> CompleteOilChangeAsync(Guid workOrderId, CompleteOilChangeRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<OilChangeResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.OdometerAtChange < 0 || request.LaborCost < 0 || request.OtherCost < 0 || !MatchesRequestedRowVersion(request.WorkOrderRowVersion))
            return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest);
        var operationId = Guid.CreateVersion7();
        OilChangeResponse? response = null;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var workOrder = await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId, cancellationToken);
                if (workOrder is null) return Result.Failure(MaintenanceErrors.NotFound);
                if (!MatchesRowVersion(workOrder.RowVersion, request.WorkOrderRowVersion)) return Result.Failure(MaintenanceErrors.ConcurrencyConflict);
                if (workOrder.MaintenanceType != MaintenanceType.OilChange || workOrder.Status is MaintenanceWorkOrderStatus.Completed or MaintenanceWorkOrderStatus.Closed or MaintenanceWorkOrderStatus.Cancelled)
                    return Result.Failure(MaintenanceErrors.InvalidState);
                if (await dbContext.OilChangeOperations.AsNoTracking().AnyAsync(x => x.MaintenanceWorkOrderId == workOrderId, cancellationToken))
                    return Result.Failure(MaintenanceErrors.InvalidState);

                VehicleType vehicleType;
                if (workOrder.VehicleId.HasValue)
                {
                    var vehicle = await dbContext.Vehicles.SingleAsync(x => x.Id == workOrder.VehicleId.Value, cancellationToken);
                    vehicleType = vehicle.VehicleType;
                    if (request.OdometerAtChange < vehicle.CurrentOdometer) return Result.Failure(MaintenanceErrors.InvalidOdometer);
                    vehicle.CurrentOdometer = request.OdometerAtChange;
                    vehicle.LastOdometerAtUtc = request.PerformedAtUtc;
                    dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading { VehicleId = vehicle.Id, Reading = request.OdometerAtChange, RecordedAtUtc = request.PerformedAtUtc, SourceType = VehicleOdometerSourceType.Maintenance, SourceEntityId = operationId, Notes = "Oil change" });
                }
                else
                {
                    var snapshot = await dbContext.ExternalVehicleSnapshots.AsNoTracking().SingleAsync(x => x.MaintenanceWorkOrderId == workOrderId, cancellationToken);
                    if (!snapshot.VehicleType.HasValue) return Result.Failure(MaintenanceErrors.InvalidSubject);
                    vehicleType = snapshot.VehicleType.Value;
                }

                decimal? configured = request.ConfiguredOilQuantityLiters;
                if (!configured.HasValue && workOrder.VehicleId.HasValue)
                {
                    var vehicleModelId = await dbContext.Vehicles.AsNoTracking().Where(x => x.Id == workOrder.VehicleId.Value).Select(x => x.VehicleModelId).SingleAsync(cancellationToken);
                    configured = await dbContext.MaintenancePlans.AsNoTracking()
                        .Where(x => x.Status == CatalogStatus.Active && x.DefaultOilQuantityLiters.HasValue && (x.VehicleModelId == vehicleModelId || !x.VehicleModelId.HasValue && x.VehicleType == vehicleType))
                        .OrderByDescending(x => x.VehicleModelId.HasValue)
                        .Select(x => x.DefaultOilQuantityLiters)
                        .FirstOrDefaultAsync(cancellationToken);
                }
                var oilQuantity = MaintenanceBusinessRules.ResolveOilQuantityLiters(vehicleType, request.OilFilterChanged, configured);
                if (!oilQuantity.HasValue) return Result.Failure(MaintenanceErrors.InvalidOilQuantity);
                var oilUsageId = Guid.CreateVersion7();
                var oilPosting = await PostUsageTrackedAsync(workOrder, oilUsageId, request.OilInventoryItemId, request.InventoryLocationId, oilQuantity.Value, MaintenanceUsageType.Oil, request.PerformedAtUtc, request.Notes, actor.Value, StockMovementType.MaintenanceUsage, cancellationToken, request.NextOilBarrelId);
                if (oilPosting.IsFailure) return Result.Failure(oilPosting.Error);
                UsagePosting? filterPosting = null;
                Guid? filterUsageId = null;
                if (request.OilFilterChanged)
                {
                    if (!request.OilFilterInventoryItemId.HasValue) return Result.Failure(MaintenanceErrors.InvalidOilFilter);
                    filterUsageId = Guid.CreateVersion7();
                    var posted = await PostUsageTrackedAsync(workOrder, filterUsageId.Value, request.OilFilterInventoryItemId.Value, request.InventoryLocationId, 1m, MaintenanceUsageType.OilFilter, request.PerformedAtUtc, request.Notes, actor.Value, StockMovementType.MaintenanceUsage, cancellationToken);
                    if (posted.IsFailure) return Result.Failure(posted.Error);
                    filterPosting = posted.Value;
                }
                else if (request.OilFilterInventoryItemId.HasValue)
                {
                    return Result.Failure(MaintenanceErrors.InvalidOilFilter);
                }

                workOrder.ActualLaborCost += request.LaborCost;
                workOrder.ActualOtherCost += request.OtherCost;
                workOrder.ActualTotalCost = workOrder.ActualMaterialCost + workOrder.ActualLaborCost + workOrder.ActualOtherCost;
                workOrder.OdometerAtCompletion = request.OdometerAtChange;
                workOrder.StartedAtUtc ??= request.PerformedAtUtc;
                workOrder.CompletedAtUtc = request.PerformedAtUtc;
                workOrder.Status = MaintenanceWorkOrderStatus.Completed;
                workOrder.WorkPerformed = "Oil change";
                if (workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle)
                {
                    if (request.LaborCost > 0) AddExternalFinancialEntry(workOrder.Id, ExternalFinancialEntryType.Expense, ExternalFinancialSourceType.MechanicLaborPayment, operationId, request.PerformedAtUtc, request.LaborCost, 0, "Oil-change mechanic cost", actor.Value);
                    if (request.OtherCost > 0) AddExternalFinancialEntry(workOrder.Id, ExternalFinancialEntryType.Expense, ExternalFinancialSourceType.OtherExpense, operationId, request.PerformedAtUtc, request.OtherCost, 0, "Oil-change other cost", actor.Value);
                }
                else if (workOrder.VehicleId.HasValue)
                {
                    if (request.LaborCost > 0) AddDirectVehicleExpense(workOrder, oilPosting.Value!.Usage, operationId, request.PerformedAtUtc, "MaintenanceLabor", request.LaborCost, "Oil-change labor");
                    if (request.OtherCost > 0) AddDirectVehicleExpense(workOrder, oilPosting.Value!.Usage, operationId, request.PerformedAtUtc, "MaintenanceOther", request.OtherCost, "Oil-change other expense");
                }

                var operation = new OilChangeOperation
                {
                    Id = operationId,
                    MaintenanceWorkOrderId = workOrder.Id,
                    PerformedAtUtc = request.PerformedAtUtc,
                    OdometerAtChange = request.OdometerAtChange,
                    VehicleTypeSnapshot = vehicleType,
                    OilInventoryItemId = request.OilInventoryItemId,
                    OilQuantityLiters = oilQuantity.Value,
                    OilMaterialUsageId = oilUsageId,
                    OilCost = oilPosting.Value!.TotalCost,
                    OilFilterChanged = request.OilFilterChanged,
                    OilFilterInventoryItemId = request.OilFilterInventoryItemId,
                    OilFilterMaterialUsageId = filterUsageId,
                    OilFilterCost = filterPosting?.TotalCost ?? 0,
                    LaborCost = request.LaborCost,
                    OtherCost = request.OtherCost,
                    TotalCost = oilPosting.Value.TotalCost + (filterPosting?.TotalCost ?? 0) + request.LaborCost + request.OtherCost,
                    PerformedByUserId = actor.Value,
                    Notes = TrimOrNull(request.Notes)
                };
                dbContext.OilChangeOperations.Add(operation);
                if (workOrder.VehicleId.HasValue)
                    await UpdateOilScheduleAsync(workOrder, operation, vehicleType, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                response = new OilChangeResponse(operation.Id, operation.MaintenanceWorkOrderId, operation.PerformedAtUtc, operation.OdometerAtChange, operation.VehicleTypeSnapshot, operation.OilQuantityLiters, operation.OilCost, operation.OilFilterChanged, operation.OilFilterCost, operation.LaborCost, operation.OtherCost, operation.TotalCost, workOrder.VehicleId, oilPosting.Value!.Usage.RiderProfileId);
                return Result.Success();
            });
            if (result.IsFailure) return Result.Failure<OilChangeResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<OilChangeResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<OilChangeResponse>(MaintenanceErrors.InvalidRequest); }
        return Result.Success(response!);
    }

    public async Task<Result<IReadOnlyList<OilReminderResponse>>> GetOilRemindersAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await dbContext.Vehicles.AsNoTracking().Where(x => x.VehicleType == VehicleType.Car || x.VehicleType == VehicleType.Motorcycle).OrderBy(x => x.AssetNumber).ToArrayAsync(cancellationToken);
        var vehicleIds = vehicles.Select(x => x.Id).ToArray();
        var operations = await (from operation in dbContext.OilChangeOperations.AsNoTracking()
                                join workOrder in dbContext.MaintenanceWorkOrders.AsNoTracking() on operation.MaintenanceWorkOrderId equals workOrder.Id
                                where workOrder.VehicleId.HasValue && vehicleIds.Contains(workOrder.VehicleId.Value)
                                select new { VehicleId = workOrder.VehicleId.GetValueOrDefault(), Operation = operation }).ToArrayAsync(cancellationToken);
        var latest = operations.GroupBy(x => x.VehicleId).ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.Operation.OdometerAtChange).ThenByDescending(x => x.Operation.PerformedAtUtc).First().Operation);
        return Result.Success<IReadOnlyList<OilReminderResponse>>(vehicles.Select(vehicle =>
        {
            latest.TryGetValue(vehicle.Id, out var last);
            var window = MaintenanceBusinessRules.GetOilChangeWindow(vehicle.VehicleType)!.Value;
            var status = MaintenanceBusinessRules.GetOilDueStatus(vehicle.VehicleType, vehicle.CurrentOdometer, last?.OdometerAtChange);
            return new OilReminderResponse(vehicle.Id, vehicle.AssetNumber, vehicle.VehicleType, vehicle.CurrentOdometer, last?.PerformedAtUtc, last?.OdometerAtChange,
                last is null ? null : last.OdometerAtChange + window.ReminderAfterKilometers,
                last is null ? null : last.OdometerAtChange + window.MaximumAfterKilometers,
                last is null || vehicle.CurrentOdometer < last.OdometerAtChange ? null : vehicle.CurrentOdometer - last.OdometerAtChange,
                status);
        }).ToArray());
    }

    public async Task<Result<ExternalPartSaleResponse>> PostExternalPartSaleAsync(Guid workOrderId, ExternalPartSaleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<ExternalPartSaleResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.Quantity <= 0 || request.SellingUnitPriceBeforeTax < 0 || request.DiscountAmount < 0 || request.TaxAmount < 0)
            return Result.Failure<ExternalPartSaleResponse>(MaintenanceErrors.InvalidRequest);
        var gross = decimal.Round(request.Quantity * request.SellingUnitPriceBeforeTax, 2, MidpointRounding.AwayFromZero);
        if (request.DiscountAmount > gross) return Result.Failure<ExternalPartSaleResponse>(MaintenanceErrors.InvalidRequest);
        var saleId = Guid.CreateVersion7();
        ExternalPartSaleResponse? response = null;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var workOrderResult = await GetEligibleExternalWorkOrderTrackedAsync(workOrderId, requirePartSales: true, requirePaidRepair: false, cancellationToken);
                if (workOrderResult.IsFailure) return Result.Failure(workOrderResult.Error);
                var workOrder = workOrderResult.Value!;
                var usageId = Guid.CreateVersion7();
                var posting = await PostUsageTrackedAsync(workOrder, usageId, request.InventoryItemId, request.InventoryLocationId, request.Quantity, MaintenanceUsageType.ExternalPartSale, request.OccurredAtUtc, TrimOrNull(request.Notes), actor.Value, StockMovementType.ExternalPartSale, cancellationToken);
                if (posting.IsFailure) return Result.Failure(posting.Error);
                var revenue = gross - request.DiscountAmount;
                var lineTotal = revenue + request.TaxAmount;
                var partProfit = revenue - posting.Value!.TotalCost;
                var sale = new ExternalPartSaleLine { Id = saleId, MaintenanceWorkOrderId = workOrder.Id, InventoryItemId = request.InventoryItemId, Quantity = request.Quantity, SellingUnitPriceBeforeTax = request.SellingUnitPriceBeforeTax, DiscountAmount = request.DiscountAmount, TaxAmount = request.TaxAmount, LineTotal = lineTotal, MaintenanceMaterialUsageId = usageId, InventoryCost = posting.Value.TotalCost, PartsGrossProfit = partProfit };
                dbContext.ExternalPartSaleLines.Add(sale);
                AddExternalFinancialEntry(workOrder.Id, ExternalFinancialEntryType.Income, ExternalFinancialSourceType.PartSaleRevenue, saleId, request.OccurredAtUtc, revenue, request.TaxAmount, "Spare-part sale", actor.Value);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                response = new ExternalPartSaleResponse(saleId, workOrder.Id, request.InventoryItemId, request.Quantity, revenue, request.TaxAmount, lineTotal, usageId);
                return Result.Success();
            });
            if (result.IsFailure) return Result.Failure<ExternalPartSaleResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<ExternalPartSaleResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<ExternalPartSaleResponse>(MaintenanceErrors.InvalidRequest); }
        return Result.Success(response!);
    }

    public Task<Result<ExternalFinancialEntryResponse>> PostCustomerLaborChargeAsync(Guid workOrderId, ExternalFinancialEntryRequest request, CancellationToken cancellationToken = default) =>
        PostExternalFinancialAsync(workOrderId, ExternalFinancialEntryType.Income, ExternalFinancialSourceType.CustomerLaborCharge, request.AmountBeforeTax, request.TaxAmount, request.OccurredAtUtc, request.Description, null, null, cancellationToken);

    public Task<Result<ExternalFinancialEntryResponse>> PostMechanicLaborPaymentAsync(Guid workOrderId, MechanicLaborPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MechanicEmployeeId.HasValue == !string.IsNullOrWhiteSpace(request.ExternalMechanicName))
            return Task.FromResult(Result.Failure<ExternalFinancialEntryResponse>(MaintenanceErrors.InvalidRequest));
        return PostExternalFinancialAsync(workOrderId, ExternalFinancialEntryType.Expense, ExternalFinancialSourceType.MechanicLaborPayment, request.Amount, 0, request.PaidAtUtc, request.Description, request.MechanicEmployeeId, TrimOrNull(request.ExternalMechanicName), cancellationToken);
    }

    public Task<Result<ExternalFinancialEntryResponse>> PostOtherFinancialEntryAsync(Guid workOrderId, bool income, ExternalFinancialEntryRequest request, CancellationToken cancellationToken = default) =>
        PostExternalFinancialAsync(workOrderId, income ? ExternalFinancialEntryType.Income : ExternalFinancialEntryType.Expense, income ? ExternalFinancialSourceType.OtherIncome : ExternalFinancialSourceType.OtherExpense, request.AmountBeforeTax, request.TaxAmount, request.OccurredAtUtc, request.Description, null, null, cancellationToken);

    public async Task<Result<ExternalCustomerPaymentResponse>> PostCustomerPaymentAsync(Guid workOrderId, ExternalCustomerPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<ExternalCustomerPaymentResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.Amount <= 0 || request.PaidAtUtc == default || !Enum.IsDefined(request.PaymentMethod)) return Result.Failure<ExternalCustomerPaymentResponse>(MaintenanceErrors.InvalidRequest);
        var workOrderResult = await GetEligibleExternalWorkOrderTrackedAsync(workOrderId, false, false, cancellationToken);
        if (workOrderResult.IsFailure) return Result.Failure<ExternalCustomerPaymentResponse>(workOrderResult.Error);
        var payment = new ExternalCustomerPayment { MaintenanceWorkOrderId = workOrderId, PaidAtUtc = request.PaidAtUtc, Amount = request.Amount, PaymentMethod = request.PaymentMethod, Reference = TrimOrNull(request.Reference), RecordedByUserId = actor.Value };
        dbContext.ExternalCustomerPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(new ExternalCustomerPaymentResponse(payment.Id, payment.MaintenanceWorkOrderId, payment.Amount, payment.PaymentMethod, payment.PaidAtUtc, payment.Reference));
    }

    public async Task<Result<WorkshopProfitReportResponse>> GetWorkshopProfitAsync(Guid maintenanceLocationId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (endDate < startDate || endDate.DayNumber - startDate.DayNumber > 366) return Result.Failure<WorkshopProfitReportResponse>(MaintenanceErrors.InvalidRequest);
        var location = await dbContext.MaintenanceLocations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == maintenanceLocationId && x.AllowsExternalVehicles, cancellationToken);
        if (location is null) return Result.Failure<WorkshopProfitReportResponse>(MaintenanceErrors.InvalidLocation);
        var riyadhOffset = TimeSpan.FromHours(3);
        var fromUtc = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), riyadhOffset).ToUniversalTime();
        var toUtc = new DateTimeOffset(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), riyadhOffset).ToUniversalTime();
        var workOrders = await dbContext.MaintenanceWorkOrders.AsNoTracking().Where(x => x.MaintenanceLocationId == maintenanceLocationId && x.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle && x.OpenedAtUtc >= fromUtc && x.OpenedAtUtc < toUtc).OrderBy(x => x.OpenedAtUtc).ToArrayAsync(cancellationToken);
        var ids = workOrders.Select(x => x.Id).ToArray();
        var entries = await dbContext.ExternalMaintenanceFinancialEntries.AsNoTracking().Where(x => ids.Contains(x.MaintenanceWorkOrderId)).ToArrayAsync(cancellationToken);
        var payments = await dbContext.ExternalCustomerPayments.AsNoTracking().Where(x => ids.Contains(x.MaintenanceWorkOrderId)).ToArrayAsync(cancellationToken);
        var snapshots = await dbContext.ExternalVehicleSnapshots.AsNoTracking().Where(x => ids.Contains(x.MaintenanceWorkOrderId)).ToDictionaryAsync(x => x.MaintenanceWorkOrderId, cancellationToken);
        var rows = new List<WorkshopProfitWorkOrderResponse>();
        foreach (var workOrder in workOrders)
        {
            var ownEntries = entries.Where(x => x.MaintenanceWorkOrderId == workOrder.Id).ToArray();
            var partsRevenue = Sum(ownEntries, ExternalFinancialSourceType.PartSaleRevenue);
            var laborRevenue = Sum(ownEntries, ExternalFinancialSourceType.CustomerLaborCharge);
            var otherIncome = Sum(ownEntries, ExternalFinancialSourceType.OtherIncome);
            var inventoryCost = Sum(ownEntries, ExternalFinancialSourceType.InventoryCost);
            var mechanicCost = Sum(ownEntries, ExternalFinancialSourceType.MechanicLaborPayment);
            var otherExpense = Sum(ownEntries, ExternalFinancialSourceType.OtherExpense);
            var tax = ownEntries.Where(x => x.EntryType == ExternalFinancialEntryType.Income).Sum(x => x.TaxAmount);
            var invoice = partsRevenue + laborRevenue + otherIncome + tax;
            var paid = payments.Where(x => x.MaintenanceWorkOrderId == workOrder.Id).Sum(x => x.Amount);
            var outstanding = Math.Max(0, invoice - paid);
            var status = paid < 0 ? WorkshopPaymentStatus.Refunded : paid == 0 ? WorkshopPaymentStatus.Unpaid : paid < invoice ? WorkshopPaymentStatus.PartiallyPaid : WorkshopPaymentStatus.Paid;
            var profit = MaintenanceBusinessRules.CalculateProfit(partsRevenue, laborRevenue, otherIncome, inventoryCost, mechanicCost, otherExpense);
            rows.Add(new WorkshopProfitWorkOrderResponse(workOrder.Id, workOrder.WorkOrderNumber, snapshots.GetValueOrDefault(workOrder.Id)?.PlateOrReference, partsRevenue, laborRevenue, otherIncome, inventoryCost, mechanicCost, otherExpense, tax, invoice, paid, outstanding, status, profit.PartsGrossProfit, profit.LaborProfit, profit.NetProfitBeforeTax));
        }
        return Result.Success(new WorkshopProfitReportResponse(maintenanceLocationId, startDate, endDate, rows.Sum(x => x.PartsRevenueBeforeTax + x.CustomerLaborRevenueBeforeTax + x.OtherIncomeBeforeTax), rows.Sum(x => x.FifoInventoryCost + x.MechanicLaborCost + x.OtherExpense), rows.Sum(x => x.TaxCollected), rows.Sum(x => x.CustomerInvoiceTotal), rows.Sum(x => x.AmountPaid), rows.Sum(x => x.NetProfitBeforeTax), rows));
    }

    private async Task<Result<UsagePosting>> PostUsageTrackedAsync(MaintenanceWorkOrder workOrder, Guid usageId, Guid itemId, Guid inventoryLocationId, decimal quantity, MaintenanceUsageType usageType, DateTimeOffset usedAtUtc, string? notes, Guid actor, StockMovementType movementType, CancellationToken cancellationToken, Guid? nextOilBarrelId = null)
    {
        if (workOrder.Status is MaintenanceWorkOrderStatus.Completed or MaintenanceWorkOrderStatus.Closed or MaintenanceWorkOrderStatus.Cancelled)
            return Result.Failure<UsagePosting>(MaintenanceErrors.InvalidState);
        var locationMatch = await (from inventory in dbContext.InventoryLocations.AsNoTracking()
                                   join site in dbContext.MaintenanceLocations.AsNoTracking() on inventory.MaintenanceLocationId equals site.Id
                                   where inventory.Id == inventoryLocationId && inventory.MaintenanceLocationId == workOrder.MaintenanceLocationId && inventory.Status == CatalogStatus.Active && site.InventoryEnabled
                                   select inventory.Id).AnyAsync(cancellationToken);
        if (!locationMatch) return Result.Failure<UsagePosting>(MaintenanceErrors.InvalidLocation);
        var item = await dbContext.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == itemId && x.Status == CatalogStatus.Active, cancellationToken);
        if (item is null || !UsageMatchesItem(usageType, item)) return Result.Failure<UsagePosting>(MaintenanceErrors.InvalidInventoryItem);
        var allocationResult = await AllocateTrackedLayersAsync(item.Id, inventoryLocationId, quantity, usedAtUtc, cancellationToken);
        if (allocationResult.IsFailure) return Result.Failure<UsagePosting>(allocationResult.Error);
        var allocations = allocationResult.Value!;
        var total = allocations.Sum(x => x.Cost);
        var movementId = Guid.CreateVersion7();
        var movementLineId = Guid.CreateVersion7();
        dbContext.StockMovements.Add(NewMovement(movementId, movementType, usedAtUtc, inventoryLocationId, null, nameof(MaintenanceMaterialUsage), usageId, notes ?? "Maintenance material usage", actor));
        dbContext.StockMovementLines.Add(new StockMovementLine { Id = movementLineId, StockMovementId = movementId, InventoryItemId = item.Id, Quantity = quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, UnitCost = total / quantity, TotalCost = total });
        Guid? assignmentId = null;
        Guid? riderProfileId = null;
        if (workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.CompanyVehicle && workOrder.VehicleId.HasValue)
        {
            var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking()
                .Where(x => x.VehicleId == workOrder.VehicleId.Value
                    && x.StartedAtUtc <= usedAtUtc
                    && (!x.EndedAtUtc.HasValue || x.EndedAtUtc >= usedAtUtc))
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            assignmentId = assignment?.Id;
            riderProfileId = assignment?.RiderProfileId;
        }
        var attribution = workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle
            ? InventoryAttributionStatus.ExternalVehicle
            : riderProfileId.HasValue ? InventoryAttributionStatus.AssignedRider : InventoryAttributionStatus.Unassigned;
        var usage = new MaintenanceMaterialUsage { Id = usageId, MaintenanceWorkOrderId = workOrder.Id, InventoryItemId = item.Id, InventoryLocationId = inventoryLocationId, UsageType = usageType, Quantity = quantity, UnitOfMeasure = item.BaseUnitOfMeasure, TotalCost = total, StockMovementId = movementId, StockMovementLineId = movementLineId, VehicleId = workOrder.VehicleId, RiderVehicleAssignmentId = assignmentId, RiderProfileId = riderProfileId, AttributionStatus = attribution, UsedAtUtc = usedAtUtc, UsedByUserId = actor, Notes = notes };
        dbContext.MaintenanceMaterialUsages.Add(usage);
        if (item.ItemType == InventoryItemType.Oil)
        {
            var barrelAllocation = await AllocateOilBarrelsAsync(usageId, inventoryLocationId, allocations, usedAtUtc, actor, nextOilBarrelId, cancellationToken);
            if (barrelAllocation.IsFailure) return Result.Failure<UsagePosting>(barrelAllocation.Error);
        }
        foreach (var allocation in allocations)
            dbContext.StockCostAllocations.Add(new StockCostAllocation { StockMovementLineId = movementLineId, MaintenanceMaterialUsageId = usageId, StockCostLayerId = allocation.Layer.Id, AllocatedQuantity = allocation.Quantity, UnitCost = allocation.Layer.UnitCost, AllocatedCost = allocation.Cost });
        workOrder.Status = MaintenanceWorkOrderStatus.InProgress;
        workOrder.StartedAtUtc ??= usedAtUtc;
        workOrder.ActualMaterialCost += total;
        workOrder.ActualTotalCost = workOrder.ActualMaterialCost + workOrder.ActualLaborCost + workOrder.ActualOtherCost;
        if (workOrder.ServiceSubjectType == MaintenanceServiceSubjectType.ExternalVehicle)
            AddExternalFinancialEntry(workOrder.Id, ExternalFinancialEntryType.Expense, ExternalFinancialSourceType.InventoryCost, usageId, usedAtUtc, total, 0, $"FIFO inventory cost: {item.Sku}", actor);
        else if (workOrder.VehicleId.HasValue)
            AddVehicleExpense(usage, usageId, usedAtUtc, total, $"Maintenance material: {item.Sku}", null);
        return Result.Success(new UsagePosting(usage, total));
    }

    private async Task<Result<ExternalFinancialEntryResponse>> PostExternalFinancialAsync(Guid workOrderId, ExternalFinancialEntryType entryType, ExternalFinancialSourceType sourceType, decimal amountBeforeTax, decimal taxAmount, DateTimeOffset occurredAt, string description, Guid? mechanicEmployeeId, string? externalMechanicName, CancellationToken cancellationToken)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<ExternalFinancialEntryResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (amountBeforeTax < 0 || taxAmount < 0 || occurredAt == default || string.IsNullOrWhiteSpace(description)) return Result.Failure<ExternalFinancialEntryResponse>(MaintenanceErrors.InvalidRequest);
        if (mechanicEmployeeId.HasValue && !await dbContext.Employees.AsNoTracking().AnyAsync(x => x.Id == mechanicEmployeeId.Value, cancellationToken))
            return Result.Failure<ExternalFinancialEntryResponse>(MaintenanceErrors.NotFound);
        var eligible = await GetEligibleExternalWorkOrderTrackedAsync(workOrderId, false, sourceType == ExternalFinancialSourceType.MechanicLaborPayment, cancellationToken);
        if (eligible.IsFailure) return Result.Failure<ExternalFinancialEntryResponse>(eligible.Error);
        var workOrder = eligible.Value!;
        var entry = AddExternalFinancialEntry(workOrderId, entryType, sourceType, null, occurredAt, amountBeforeTax, taxAmount, description.Trim(), actor.Value, mechanicEmployeeId, externalMechanicName);
        if (sourceType == ExternalFinancialSourceType.MechanicLaborPayment) workOrder.ActualLaborCost += amountBeforeTax;
        if (sourceType == ExternalFinancialSourceType.OtherExpense) workOrder.ActualOtherCost += amountBeforeTax;
        workOrder.ActualTotalCost = workOrder.ActualMaterialCost + workOrder.ActualLaborCost + workOrder.ActualOtherCost;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(MapFinancialEntry(entry));
    }

    private async Task<Result<MaintenanceWorkOrder>> GetEligibleExternalWorkOrderTrackedAsync(Guid workOrderId, bool requirePartSales, bool requirePaidRepair, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.MaintenanceWorkOrders.SingleOrDefaultAsync(x => x.Id == workOrderId, cancellationToken);
        if (workOrder is null) return Result.Failure<MaintenanceWorkOrder>(MaintenanceErrors.NotFound);
        if (workOrder.ServiceSubjectType != MaintenanceServiceSubjectType.ExternalVehicle || workOrder.Status == MaintenanceWorkOrderStatus.Cancelled)
            return Result.Failure<MaintenanceWorkOrder>(MaintenanceErrors.InvalidSubject);
        var location = await dbContext.MaintenanceLocations.AsNoTracking().SingleAsync(x => x.Id == workOrder.MaintenanceLocationId, cancellationToken);
        if (requirePartSales && !location.AllowsSparePartSales || requirePaidRepair && !location.AllowsPaidExternalRepairs)
            return Result.Failure<MaintenanceWorkOrder>(MaintenanceErrors.InvalidLocation);
        return Result.Success(workOrder);
    }

    private ExternalMaintenanceFinancialEntry AddExternalFinancialEntry(Guid workOrderId, ExternalFinancialEntryType entryType, ExternalFinancialSourceType sourceType, Guid? sourceEntityId, DateTimeOffset occurredAt, decimal amountBeforeTax, decimal taxAmount, string description, Guid actor, Guid? mechanicEmployeeId = null, string? externalMechanicName = null, Guid? reversalOfEntryId = null)
    {
        var entry = new ExternalMaintenanceFinancialEntry { MaintenanceWorkOrderId = workOrderId, EntryType = entryType, SourceType = sourceType, SourceEntityId = sourceEntityId, OccurredAtUtc = occurredAt, AmountBeforeTax = amountBeforeTax, TaxAmount = taxAmount, TotalAmount = amountBeforeTax + taxAmount, Description = description, RecordedByUserId = actor, MechanicEmployeeId = mechanicEmployeeId, ExternalMechanicName = externalMechanicName, ReversalOfEntryId = reversalOfEntryId };
        dbContext.ExternalMaintenanceFinancialEntries.Add(entry);
        return entry;
    }

    private void AddVehicleExpense(MaintenanceMaterialUsage usage, Guid sourceId, DateTimeOffset occurredAt, decimal amount, string description, Guid? reversalOf)
    {
        dbContext.VehicleExpenses.Add(new VehicleExpense { VehicleId = usage.VehicleId!.Value, RiderVehicleAssignmentId = usage.RiderVehicleAssignmentId, RiderProfileId = usage.RiderProfileId, ExpenseType = "MaintenanceMaterial", SourceEntityType = nameof(MaintenanceMaterialUsage), SourceEntityId = sourceId, OccurredOn = DateOnly.FromDateTime(occurredAt.UtcDateTime), AmountBeforeTax = amount, TotalAmount = amount, Description = description, ReversalOfExpenseId = reversalOf });
    }

    private void AddDirectVehicleExpense(MaintenanceWorkOrder workOrder, MaintenanceMaterialUsage attributionSource, Guid sourceId, DateTimeOffset occurredAt, string expenseType, decimal amount, string description)
    {
        dbContext.VehicleExpenses.Add(new VehicleExpense { VehicleId = workOrder.VehicleId!.Value, RiderVehicleAssignmentId = attributionSource.RiderVehicleAssignmentId, RiderProfileId = attributionSource.RiderProfileId, ExpenseType = expenseType, SourceEntityType = nameof(OilChangeOperation), SourceEntityId = sourceId, OccurredOn = DateOnly.FromDateTime(occurredAt.UtcDateTime), AmountBeforeTax = amount, TotalAmount = amount, Description = description });
    }

    private async Task UpdateOilScheduleAsync(MaintenanceWorkOrder workOrder, OilChangeOperation operation, VehicleType vehicleType, CancellationToken cancellationToken)
    {
        var plan = await dbContext.MaintenancePlans.Where(x => x.Status == CatalogStatus.Active && x.TriggerType == MaintenanceTriggerType.OdometerWindow && x.VehicleType == vehicleType).OrderBy(x => x.Code).FirstOrDefaultAsync(cancellationToken);
        if (plan is null) return;
        var schedule = await dbContext.VehicleMaintenanceSchedules.SingleOrDefaultAsync(x => x.VehicleId == workOrder.VehicleId!.Value && x.MaintenancePlanId == plan.Id, cancellationToken)
            ?? new VehicleMaintenanceSchedule { VehicleId = workOrder.VehicleId!.Value, MaintenancePlanId = plan.Id };
        if (dbContext.Entry(schedule).State == EntityState.Detached) dbContext.VehicleMaintenanceSchedules.Add(schedule);
        schedule.LastCompletedWorkOrderId = workOrder.Id;
        schedule.LastCompletedAtUtc = operation.PerformedAtUtc;
        schedule.LastCompletedOdometer = operation.OdometerAtChange;
        schedule.ReminderFromOdometer = operation.OdometerAtChange + plan.ReminderAfterKilometers;
        schedule.MaximumDueOdometer = operation.OdometerAtChange + plan.MaximumAfterKilometers;
        schedule.ComputedStatus = MaintenanceDueStatus.Ok;
        schedule.ComputedAtUtc = UtcNow;
    }

    private async Task<Result<IReadOnlyList<MaintenanceMaterialUsageResponse>>> GetMaterialHistoryAsync(Guid? vehicleId, Guid? riderProfileId, CancellationToken cancellationToken)
    {
        var query = dbContext.MaintenanceMaterialUsages.AsNoTracking();
        query = vehicleId.HasValue ? query.Where(x => x.VehicleId == vehicleId.Value) : query.Where(x => x.RiderProfileId == riderProfileId!.Value);
        var ids = await query.OrderByDescending(x => x.UsedAtUtc).Select(x => x.Id).ToArrayAsync(cancellationToken);
        var result = new List<MaintenanceMaterialUsageResponse>(ids.Length);
        foreach (var id in ids)
        {
            var mapped = await MapUsageAsync(id, cancellationToken);
            if (mapped.IsSuccess) result.Add(mapped.Value!);
        }
        return Result.Success<IReadOnlyList<MaintenanceMaterialUsageResponse>>(result);
    }

    private async Task<Result<MaintenanceMaterialUsageResponse>> MapUsageAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (from material in dbContext.MaintenanceMaterialUsages.AsNoTracking()
                         join item in dbContext.InventoryItems.AsNoTracking() on material.InventoryItemId equals item.Id
                         where material.Id == id
                         select new { Usage = material, item.Sku, item.NameAr }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return Result.Failure<MaintenanceMaterialUsageResponse>(MaintenanceErrors.NotFound);
        var allocations = await dbContext.StockCostAllocations.AsNoTracking().Where(x => x.MaintenanceMaterialUsageId == id).OrderBy(x => x.CreatedAtUtc).Select(x => new StockCostAllocationResponse(x.StockCostLayerId, x.AllocatedQuantity, x.UnitCost, x.AllocatedCost)).ToArrayAsync(cancellationToken);
        var usage = row.Usage;
        return Result.Success(new MaintenanceMaterialUsageResponse(usage.Id, usage.MaintenanceWorkOrderId, usage.InventoryItemId, row.Sku, row.NameAr, usage.InventoryLocationId, usage.UsageType, usage.Direction, usage.Quantity, usage.UnitOfMeasure, usage.TotalCost, usage.VehicleId, usage.RiderVehicleAssignmentId, usage.RiderProfileId, usage.AttributionStatus, usage.UsedAtUtc, usage.ReversalOfUsageId, allocations));
    }

    private static ExternalFinancialEntryResponse MapFinancialEntry(ExternalMaintenanceFinancialEntry entry) => new(entry.Id, entry.MaintenanceWorkOrderId, entry.EntryType, entry.SourceType, entry.AmountBeforeTax, entry.TaxAmount, entry.TotalAmount, entry.OccurredAtUtc, entry.Description, entry.MechanicEmployeeId, entry.ExternalMechanicName);

    private static decimal Sum(IEnumerable<ExternalMaintenanceFinancialEntry> entries, ExternalFinancialSourceType type) => entries.Where(x => x.SourceType == type).Sum(x => x.AmountBeforeTax);

    private static bool UsageMatchesItem(MaintenanceUsageType usageType, InventoryItem item) => usageType switch
    {
        MaintenanceUsageType.Oil => item.ItemType == InventoryItemType.Oil && item.BaseUnitOfMeasure == InventoryUnitOfMeasure.Liter,
        MaintenanceUsageType.OilFilter => item.ItemType == InventoryItemType.SparePart && item.BaseUnitOfMeasure == InventoryUnitOfMeasure.Piece,
        MaintenanceUsageType.ExternalPartSale => item.ItemType is InventoryItemType.SparePart or InventoryItemType.Consumable,
        MaintenanceUsageType.SparePart => item.ItemType == InventoryItemType.SparePart,
        MaintenanceUsageType.Consumable => item.ItemType == InventoryItemType.Consumable,
        _ => false
    };

    private static bool MatchesRequestedRowVersion(string value) => !string.IsNullOrWhiteSpace(value);
    private sealed record UsagePosting(MaintenanceMaterialUsage Usage, decimal TotalCost);
}
