using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Maintenance;
using LogisticsERP.Domain.Entities.Maintenance;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Maintenance;

internal sealed partial class MaintenanceService
{
    public async Task<Result<IReadOnlyList<StockBalanceResponse>>> GetBalancesAsync(Guid? inventoryLocationId, Guid? inventoryItemId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockBalances.AsNoTracking();
        if (inventoryLocationId.HasValue) query = query.Where(x => x.InventoryLocationId == inventoryLocationId.Value);
        if (inventoryItemId.HasValue) query = query.Where(x => x.InventoryItemId == inventoryItemId.Value);
        var balances = await query.OrderBy(x => x.InventoryLocationId).ThenBy(x => x.InventoryItemId).ToArrayAsync(cancellationToken);
        var itemIds = balances.Select(x => x.InventoryItemId).Distinct().ToArray();
        var locationIds = balances.Select(x => x.InventoryLocationId).Distinct().ToArray();
        var items = await dbContext.InventoryItems.AsNoTracking().Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var locations = await dbContext.InventoryLocations.AsNoTracking().Where(x => locationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var layerValues = await dbContext.StockCostLayers.AsNoTracking()
            .Where(x => itemIds.Contains(x.InventoryItemId) && locationIds.Contains(x.InventoryLocationId) && x.RemainingQuantity > 0)
            .GroupBy(x => new { x.InventoryItemId, x.InventoryLocationId })
            .Select(group => new { group.Key.InventoryItemId, group.Key.InventoryLocationId, Value = group.Sum(x => x.RemainingQuantity * x.UnitCost) })
            .ToDictionaryAsync(x => (x.InventoryItemId, x.InventoryLocationId), x => x.Value, cancellationToken);

        return Result.Success<IReadOnlyList<StockBalanceResponse>>(balances.Select(balance => new StockBalanceResponse(
            balance.Id, balance.InventoryItemId, items[balance.InventoryItemId].Sku, items[balance.InventoryItemId].NameAr,
            balance.InventoryLocationId, locations[balance.InventoryLocationId].NameAr, balance.QuantityOnHand,
            balance.QuantityReserved, balance.ReportingAverageUnitCost,
            decimal.Round(layerValues.GetValueOrDefault((balance.InventoryItemId, balance.InventoryLocationId)), 2, MidpointRounding.AwayFromZero),
            balance.LastMovementAtUtc, EncodeRowVersion(balance.RowVersion))).ToArray());
    }

    public async Task<Result<IReadOnlyList<StockCostLayerResponse>>> GetCostLayersAsync(Guid? inventoryLocationId, Guid? inventoryItemId, bool availableOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockCostLayers.AsNoTracking();
        if (inventoryLocationId.HasValue) query = query.Where(x => x.InventoryLocationId == inventoryLocationId.Value);
        if (inventoryItemId.HasValue) query = query.Where(x => x.InventoryItemId == inventoryItemId.Value);
        if (availableOnly) query = query.Where(x => x.RemainingQuantity > 0);
        var items = await query.OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.OriginalSequence).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<StockCostLayerResponse>>(items.Select(x => new StockCostLayerResponse(
            x.Id, x.InventoryItemId, x.InventoryLocationId, x.ReceivedAtUtc, x.OriginalSequence, x.OriginalQuantity,
            x.RemainingQuantity, x.BaseUnitOfMeasure, x.UnitCost,
            decimal.Round(x.RemainingQuantity * x.UnitCost, 2, MidpointRounding.AwayFromZero), x.LotNumber,
            x.ExpiryDate, x.SourceReceiptLineId, x.SourceCostLayerId, EncodeRowVersion(x.RowVersion))).ToArray());
    }

    public async Task<Result<PurchaseReceiptResponse>> PostPurchaseReceiptAsync(PostPurchaseReceiptRequest request, PrivateFileUpload billFile, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (!ValidReceiptRequest(request)) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.InvalidRequest);
        if (!IsBillDocument(billFile)) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.InvalidBillFile);
        if (!await dbContext.MaintenanceSuppliers.AsNoTracking().AnyAsync(x => x.Id == request.SupplierId && x.Status == CatalogStatus.Active, cancellationToken))
            return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.NotFound);
        var location = await GetActiveInventoryLocationAsync(request.InventoryLocationId, cancellationToken);
        if (location is null) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.InvalidLocation);
        if (!string.IsNullOrWhiteSpace(request.SupplierInvoiceNumber)
            && await dbContext.PurchaseReceipts.AsNoTracking().AnyAsync(x => x.SupplierId == request.SupplierId && x.SupplierInvoiceNumber == request.SupplierInvoiceNumber.Trim(), cancellationToken))
            return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.Duplicate);

        var itemIds = request.Lines.Select(x => x.InventoryItemId).Distinct().ToArray();
        var items = await dbContext.InventoryItems.AsNoTracking().Where(x => itemIds.Contains(x.Id) && x.Status == CatalogStatus.Active).ToDictionaryAsync(x => x.Id, cancellationToken);
        if (items.Count != itemIds.Length || request.Lines.Any(line => !ValidReceiptLine(line, items.GetValueOrDefault(line.InventoryItemId))))
            return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.InvalidInventoryItem);

        var receiptId = Guid.CreateVersion7();
        var stored = await fileStorage.StoreAsync($"maintenance/purchase-receipts/{receiptId:N}", billFile, MaximumBillFileSize, cancellationToken);
        if (stored.IsFailure) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.InvalidBillFile);

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var postResult = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var movementId = Guid.CreateVersion7();
                var movement = new StockMovement
                {
                    Id = movementId,
                    MovementNumber = NewNumber("RCV", request.ReceivedAtUtc, movementId),
                    MovementType = StockMovementType.PurchaseReceipt,
                    OccurredAtUtc = request.ReceivedAtUtc,
                    DestinationLocationId = request.InventoryLocationId,
                    SourceDocumentType = nameof(PurchaseReceipt),
                    SourceDocumentId = receiptId,
                    Reason = "Purchase receipt",
                    PostedByUserId = actor.Value
                };
                dbContext.StockMovements.Add(movement);

                var receipt = new PurchaseReceipt
                {
                    Id = receiptId,
                    ReceiptNumber = NewNumber("BILL", request.ReceivedAtUtc, receiptId),
                    SupplierId = request.SupplierId,
                    SupplierInvoiceNumber = TrimOrNull(request.SupplierInvoiceNumber),
                    InvoiceDate = request.InvoiceDate,
                    ReceivedAtUtc = request.ReceivedAtUtc,
                    InventoryLocationId = request.InventoryLocationId,
                    CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
                    PostedMovementId = movementId
                };

                for (var index = 0; index < request.Lines.Count; index++)
                {
                    var requestLine = request.Lines[index];
                    var item = items[requestLine.InventoryItemId];
                    var baseQuantity = decimal.Round(requestLine.PackageCount * requestLine.DeclaredQuantityPerPackage, 3, MidpointRounding.AwayFromZero);
                    var subtotal = decimal.Round(requestLine.PackageCount * requestLine.PackageUnitPrice, 2, MidpointRounding.AwayFromZero);
                    var valuation = subtotal - requestLine.DiscountAmount;
                    var baseUnitCost = MaintenanceBusinessRules.CalculateBaseUnitCost(valuation, baseQuantity);
                    var lineId = Guid.CreateVersion7();
                    var layerId = Guid.CreateVersion7();
                    var movementLineId = Guid.CreateVersion7();

                    dbContext.StockMovementLines.Add(new StockMovementLine
                    {
                        Id = movementLineId,
                        StockMovementId = movementId,
                        InventoryItemId = item.Id,
                        Quantity = baseQuantity,
                        BaseUnitOfMeasure = item.BaseUnitOfMeasure,
                        CostLayerId = layerId,
                        UnitCost = baseUnitCost,
                        TotalCost = valuation,
                        LotNumber = TrimOrNull(requestLine.LotNumber)
                    });
                    dbContext.StockCostLayers.Add(new StockCostLayer
                    {
                        Id = layerId,
                        InventoryItemId = item.Id,
                        InventoryLocationId = request.InventoryLocationId,
                        SourceReceiptLineId = lineId,
                        SourceMovementLineId = movementLineId,
                        ReceivedAtUtc = request.ReceivedAtUtc,
                        OriginalSequence = request.ReceivedAtUtc.UtcTicks + index,
                        OriginalQuantity = baseQuantity,
                        RemainingQuantity = baseQuantity,
                        BaseUnitOfMeasure = item.BaseUnitOfMeasure,
                        UnitCost = baseUnitCost,
                        OriginalTotalCost = valuation,
                        LotNumber = TrimOrNull(requestLine.LotNumber),
                        ExpiryDate = requestLine.ExpiryDate
                    });
                    dbContext.PurchaseReceiptLines.Add(new PurchaseReceiptLine
                    {
                        Id = lineId,
                        PurchaseReceiptId = receiptId,
                        InventoryItemId = item.Id,
                        PurchaseUnit = requestLine.PurchaseUnit,
                        PackageCount = requestLine.PackageCount,
                        DeclaredQuantityPerPackage = requestLine.DeclaredQuantityPerPackage,
                        ReceivedBaseQuantity = baseQuantity,
                        BaseUnitOfMeasure = item.BaseUnitOfMeasure,
                        GrossWeightKg = requestLine.GrossWeightKg,
                        NetWeightKg = requestLine.NetWeightKg,
                        PackageUnitPrice = requestLine.PackageUnitPrice,
                        LineSubtotal = subtotal,
                        DiscountAmount = requestLine.DiscountAmount,
                        TaxAmount = requestLine.TaxAmount,
                        InventoryValuationAmount = valuation,
                        BaseUnitCost = baseUnitCost,
                        LotNumber = TrimOrNull(requestLine.LotNumber),
                        ExpiryDate = requestLine.ExpiryDate,
                        StockMovementLineId = movementLineId,
                        StockCostLayerId = layerId
                    });
                    if (item.ItemType == InventoryItemType.Oil)
                    {
                        var barrelCount = decimal.ToInt32(requestLine.PackageCount);
                        for (var packageSequence = 1; packageSequence <= barrelCount; packageSequence++)
                        {
                            var barrelId = Guid.CreateVersion7();
                            dbContext.OilBarrels.Add(new OilBarrel
                            {
                                Id = barrelId,
                                BarrelNumber = NewNumber("OB", request.ReceivedAtUtc, barrelId),
                                PurchaseReceiptLineId = lineId,
                                InventoryItemId = item.Id,
                                InventoryLocationId = request.InventoryLocationId,
                                StockCostLayerId = layerId,
                                PackageSequence = packageSequence,
                                NominalCapacityLiters = requestLine.DeclaredQuantityPerPackage,
                                RemainingLiters = requestLine.DeclaredQuantityPerPackage,
                                UnitCostPerLiter = baseUnitCost,
                                MaximumAllowedLossLiters = MaintenanceBusinessRules.CalculateOilBarrelLossAllowance(requestLine.DeclaredQuantityPerPackage)
                            });
                        }
                    }
                    var balance = await GetOrCreateBalanceAsync(item.Id, request.InventoryLocationId, cancellationToken);
                    AddToBalance(balance, baseQuantity, valuation, request.ReceivedAtUtc);
                    receipt.Subtotal += subtotal;
                    receipt.DiscountAmount += requestLine.DiscountAmount;
                    receipt.TaxAmount += requestLine.TaxAmount;
                    receipt.InventoryValuationAmount += valuation;
                    receipt.TotalAmount += valuation + requestLine.TaxAmount;
                }

                dbContext.PurchaseReceipts.Add(receipt);
                dbContext.PurchaseReceiptAttachments.Add(new PurchaseReceiptAttachment
                {
                    PurchaseReceiptId = receiptId,
                    OriginalFileName = stored.Value!.OriginalFileName,
                    StoredFileName = stored.Value.StoredFileName,
                    ContentType = stored.Value.ContentType,
                    FileSizeBytes = stored.Value.Length,
                    Sha256Checksum = stored.Value.Sha256Checksum,
                    StoragePath = stored.Value.StoragePath,
                    UploadedByUserId = actor.Value,
                    UploadedAtUtc = UtcNow
                });
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(receiptId);
            });

            if (postResult.IsFailure)
            {
                fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
                return Result.Failure<PurchaseReceiptResponse>(postResult.Error);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.Duplicate);
        }
        catch
        {
            fileStorage.DeleteBestEffort(stored.Value!.StoragePath);
            throw;
        }

        return await GetPurchaseReceiptAsync(receiptId, cancellationToken);
    }

    public async Task<Result<PurchaseReceiptResponse>> GetPurchaseReceiptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await dbContext.PurchaseReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (receipt is null) return Result.Failure<PurchaseReceiptResponse>(MaintenanceErrors.NotFound);
        var supplierName = await dbContext.MaintenanceSuppliers.AsNoTracking().Where(x => x.Id == receipt.SupplierId).Select(x => x.LegalNameAr).SingleAsync(cancellationToken);
        var locationName = await dbContext.InventoryLocations.AsNoTracking().Where(x => x.Id == receipt.InventoryLocationId).Select(x => x.NameAr).SingleAsync(cancellationToken);
        var rows = await (from line in dbContext.PurchaseReceiptLines.AsNoTracking()
                          join item in dbContext.InventoryItems.AsNoTracking() on line.InventoryItemId equals item.Id
                          where line.PurchaseReceiptId == id
                          orderby line.CreatedAtUtc
                          select new { Line = line, item.Sku }).ToArrayAsync(cancellationToken);
        var attachment = await dbContext.PurchaseReceiptAttachments.AsNoTracking().SingleAsync(x => x.PurchaseReceiptId == id, cancellationToken);
        var lineIds = rows.Select(x => x.Line.Id).ToArray();
        var oilBarrels = await dbContext.OilBarrels.AsNoTracking()
            .Where(x => lineIds.Contains(x.PurchaseReceiptLineId))
            .OrderBy(x => x.PackageSequence)
            .ThenBy(x => x.BarrelNumber)
            .ToArrayAsync(cancellationToken);
        return Result.Success(new PurchaseReceiptResponse(
            receipt.Id, receipt.ReceiptNumber, receipt.SupplierId, supplierName, receipt.SupplierInvoiceNumber,
            receipt.InvoiceDate, receipt.ReceivedAtUtc, receipt.InventoryLocationId, locationName, receipt.Subtotal,
            receipt.DiscountAmount, receipt.TaxAmount, receipt.InventoryValuationAmount, receipt.TotalAmount,
            receipt.CurrencyCode, receipt.Status,
            rows.Select(row => new PurchaseReceiptLineResponse(row.Line.Id, row.Line.InventoryItemId, row.Sku,
                row.Line.PurchaseUnit, row.Line.PackageCount, row.Line.DeclaredQuantityPerPackage,
                row.Line.ReceivedBaseQuantity, row.Line.BaseUnitOfMeasure, row.Line.GrossWeightKg,
                row.Line.NetWeightKg, row.Line.PackageUnitPrice, row.Line.LineSubtotal, row.Line.DiscountAmount,
                row.Line.TaxAmount, row.Line.InventoryValuationAmount, row.Line.BaseUnitCost, row.Line.StockCostLayerId)).ToArray(),
            new PurchaseReceiptAttachmentResponse(attachment.Id, attachment.OriginalFileName, attachment.ContentType,
                attachment.FileSizeBytes, attachment.Sha256Checksum, attachment.UploadedAtUtc),
            oilBarrels.Select(MapOilBarrel).ToArray(),
            EncodeRowVersion(receipt.RowVersion)));
    }

    public async Task<Result<PrivateFileDownload>> DownloadPurchaseReceiptAttachmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.PurchaseReceiptAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.PurchaseReceiptId == id, cancellationToken);
        if (attachment is null) return Result.Failure<PrivateFileDownload>(MaintenanceErrors.FileMissing);
        var file = await fileStorage.OpenReadAsync(attachment.StoragePath, attachment.ContentType, attachment.OriginalFileName, attachment.FileSizeBytes, cancellationToken);
        return file.IsFailure ? Result.Failure<PrivateFileDownload>(MaintenanceErrors.FileMissing) : file;
    }

    public async Task<Result<StockTransferResponse>> PostTransferAsync(PostStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<StockTransferResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.SourceLocationId == request.DestinationLocationId || request.Lines.Count == 0 || request.Lines.Any(x => x.Quantity <= 0) || request.Lines.Select(x => x.InventoryItemId).Distinct().Count() != request.Lines.Count || string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<StockTransferResponse>(MaintenanceErrors.InvalidRequest);
        if (await GetActiveInventoryLocationAsync(request.SourceLocationId, cancellationToken) is null || await GetActiveInventoryLocationAsync(request.DestinationLocationId, cancellationToken) is null)
            return Result.Failure<StockTransferResponse>(MaintenanceErrors.InvalidLocation);

        var transferId = Guid.CreateVersion7();
        var totalCost = 0m;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var sourceMovementId = Guid.CreateVersion7();
                var destinationMovementId = Guid.CreateVersion7();
                var transfer = new StockTransfer
                {
                    Id = transferId,
                    TransferNumber = NewNumber("TRF", request.PostedAtUtc, transferId),
                    SourceLocationId = request.SourceLocationId,
                    DestinationLocationId = request.DestinationLocationId,
                    PostedAtUtc = request.PostedAtUtc,
                    PostedByUserId = actor.Value,
                    Reason = request.Reason.Trim(),
                    SourceMovementId = sourceMovementId,
                    DestinationMovementId = destinationMovementId
                };
                dbContext.StockTransfers.Add(transfer);
                dbContext.StockMovements.AddRange(
                    NewMovement(sourceMovementId, StockMovementType.TransferOut, request.PostedAtUtc, request.SourceLocationId, null, nameof(StockTransfer), transferId, request.Reason, actor.Value),
                    NewMovement(destinationMovementId, StockMovementType.TransferIn, request.PostedAtUtc, null, request.DestinationLocationId, nameof(StockTransfer), transferId, request.Reason, actor.Value));

                foreach (var requestLine in request.Lines)
                {
                    var item = await dbContext.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestLine.InventoryItemId && x.Status == CatalogStatus.Active, cancellationToken);
                    if (item is null) return Result.Failure<decimal>(MaintenanceErrors.InvalidInventoryItem);
                    var allocationResult = await AllocateTrackedLayersAsync(item.Id, request.SourceLocationId, requestLine.Quantity, request.PostedAtUtc, cancellationToken);
                    if (allocationResult.IsFailure) return Result.Failure<decimal>(allocationResult.Error);
                    var allocations = allocationResult.Value!;
                    var lineCost = allocations.Sum(x => x.Cost);
                    totalCost += lineCost;
                    var transferLineId = Guid.CreateVersion7();
                    dbContext.StockTransferLines.Add(new StockTransferLine { Id = transferLineId, StockTransferId = transferId, InventoryItemId = item.Id, Quantity = requestLine.Quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, TotalCost = lineCost });
                    var sourceLineId = Guid.CreateVersion7();
                    dbContext.StockMovementLines.Add(new StockMovementLine { Id = sourceLineId, StockMovementId = sourceMovementId, InventoryItemId = item.Id, Quantity = requestLine.Quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, UnitCost = lineCost / requestLine.Quantity, TotalCost = lineCost });
                    foreach (var allocation in allocations)
                    {
                        dbContext.StockCostAllocations.Add(new StockCostAllocation { StockMovementLineId = sourceLineId, StockCostLayerId = allocation.Layer.Id, AllocatedQuantity = allocation.Quantity, UnitCost = allocation.Layer.UnitCost, AllocatedCost = allocation.Cost });
                        var destinationLayerId = Guid.CreateVersion7();
                        var destinationLineId = Guid.CreateVersion7();
                        dbContext.StockMovementLines.Add(new StockMovementLine { Id = destinationLineId, StockMovementId = destinationMovementId, InventoryItemId = item.Id, Quantity = allocation.Quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, CostLayerId = destinationLayerId, UnitCost = allocation.Layer.UnitCost, TotalCost = allocation.Cost, LotNumber = allocation.Layer.LotNumber });
                        dbContext.StockCostLayers.Add(new StockCostLayer { Id = destinationLayerId, InventoryItemId = item.Id, InventoryLocationId = request.DestinationLocationId, SourceMovementLineId = destinationLineId, SourceCostLayerId = allocation.Layer.Id, ReceivedAtUtc = allocation.Layer.ReceivedAtUtc, OriginalSequence = allocation.Layer.OriginalSequence, OriginalQuantity = allocation.Quantity, RemainingQuantity = allocation.Quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, UnitCost = allocation.Layer.UnitCost, OriginalTotalCost = allocation.Cost, LotNumber = allocation.Layer.LotNumber, ExpiryDate = allocation.Layer.ExpiryDate });
                        if (item.ItemType == InventoryItemType.Oil)
                        {
                            var barrelTransfer = await MoveWholeOilBarrelsAsync(allocation.Layer.Id, request.SourceLocationId, request.DestinationLocationId, destinationLayerId, allocation.Quantity, cancellationToken);
                            if (barrelTransfer.IsFailure) return Result.Failure<decimal>(barrelTransfer.Error);
                        }
                    }
                    var destinationBalance = await GetOrCreateBalanceAsync(item.Id, request.DestinationLocationId, cancellationToken);
                    AddToBalance(destinationBalance, requestLine.Quantity, lineCost, request.PostedAtUtc);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(totalCost);
            });
            if (result.IsFailure) return Result.Failure<StockTransferResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<StockTransferResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<StockTransferResponse>(MaintenanceErrors.InvalidRequest); }

        var rowVersion = await dbContext.StockTransfers.AsNoTracking().Where(x => x.Id == transferId).Select(x => x.RowVersion).SingleAsync(cancellationToken);
        return Result.Success(new StockTransferResponse(transferId, NewNumber("TRF", request.PostedAtUtc, transferId), request.SourceLocationId, request.DestinationLocationId, request.PostedAtUtc, totalCost, InventoryDocumentStatus.Posted, EncodeRowVersion(rowVersion)));
    }

    public async Task<Result<SupplierReturnResponse>> PostSupplierReturnAsync(PostSupplierReturnRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<SupplierReturnResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.Lines.Count == 0 || request.Lines.Any(x => x.Quantity <= 0 || string.IsNullOrWhiteSpace(x.Reason)) || string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure<SupplierReturnResponse>(MaintenanceErrors.InvalidRequest);
        if (!await dbContext.MaintenanceSuppliers.AsNoTracking().AnyAsync(x => x.Id == request.SupplierId, cancellationToken) || await GetActiveInventoryLocationAsync(request.InventoryLocationId, cancellationToken) is null)
            return Result.Failure<SupplierReturnResponse>(MaintenanceErrors.NotFound);

        var returnId = Guid.CreateVersion7();
        var total = 0m;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var movementId = Guid.CreateVersion7();
                dbContext.StockMovements.Add(NewMovement(movementId, StockMovementType.SupplierReturn, request.ReturnedAtUtc, request.InventoryLocationId, null, nameof(SupplierReturn), returnId, request.Reason, actor.Value));
                var document = new SupplierReturn { Id = returnId, ReturnNumber = NewNumber("RET", request.ReturnedAtUtc, returnId), SupplierId = request.SupplierId, InventoryLocationId = request.InventoryLocationId, PurchaseReceiptId = request.PurchaseReceiptId, ReturnedAtUtc = request.ReturnedAtUtc, Reason = request.Reason.Trim(), PostedMovementId = movementId };
                dbContext.SupplierReturns.Add(document);
                foreach (var requestLine in request.Lines)
                {
                var layer = await dbContext.StockCostLayers.SingleOrDefaultAsync(x => x.Id == requestLine.StockCostLayerId && x.InventoryItemId == requestLine.InventoryItemId && x.InventoryLocationId == request.InventoryLocationId, cancellationToken);
                if (layer is null || layer.RemainingQuantity < requestLine.Quantity) return Result.Failure<decimal>(MaintenanceErrors.InsufficientStock);
                var itemType = await dbContext.InventoryItems.AsNoTracking().Where(x => x.Id == layer.InventoryItemId).Select(x => x.ItemType).SingleAsync(cancellationToken);
                if (itemType == InventoryItemType.Oil)
                {
                    var barrelReturn = await ReturnWholeOilBarrelsAsync(layer.Id, request.InventoryLocationId, requestLine.Quantity, cancellationToken);
                    if (barrelReturn.IsFailure) return Result.Failure<decimal>(barrelReturn.Error);
                }
                    var cost = decimal.Round(requestLine.Quantity * layer.UnitCost, 2, MidpointRounding.AwayFromZero);
                    total += cost;
                    layer.RemainingQuantity -= requestLine.Quantity;
                    var balance = await GetOrCreateBalanceAsync(layer.InventoryItemId, request.InventoryLocationId, cancellationToken);
                    RemoveFromBalance(balance, requestLine.Quantity, request.ReturnedAtUtc);
                    var movementLineId = Guid.CreateVersion7();
                    dbContext.StockMovementLines.Add(new StockMovementLine { Id = movementLineId, StockMovementId = movementId, InventoryItemId = layer.InventoryItemId, Quantity = requestLine.Quantity, BaseUnitOfMeasure = layer.BaseUnitOfMeasure, CostLayerId = layer.Id, UnitCost = layer.UnitCost, TotalCost = cost, LotNumber = layer.LotNumber });
                    dbContext.SupplierReturnLines.Add(new SupplierReturnLine { SupplierReturnId = returnId, InventoryItemId = layer.InventoryItemId, StockCostLayerId = layer.Id, Quantity = requestLine.Quantity, UnitCost = layer.UnitCost, TotalCost = cost, Reason = requestLine.Reason.Trim() });
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(total);
            });
            if (result.IsFailure) return Result.Failure<SupplierReturnResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<SupplierReturnResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<SupplierReturnResponse>(MaintenanceErrors.InvalidRequest); }
        var version = await dbContext.SupplierReturns.AsNoTracking().Where(x => x.Id == returnId).Select(x => x.RowVersion).SingleAsync(cancellationToken);
        return Result.Success(new SupplierReturnResponse(returnId, NewNumber("RET", request.ReturnedAtUtc, returnId), request.SupplierId, request.InventoryLocationId, request.ReturnedAtUtc, total, InventoryDocumentStatus.Posted, EncodeRowVersion(version)));
    }

    public async Task<Result<RiderInventoryIssueResponse>> PostRiderIssueAsync(PostRiderInventoryIssueRequest request, CancellationToken cancellationToken = default)
    {
        var actor = currentUser.UserId;
        if (!actor.HasValue) return Result.Failure<RiderInventoryIssueResponse>(MaintenanceErrors.CurrentUserUnavailable);
        if (request.Lines.Count == 0 || request.Lines.Any(x => x.Quantity <= 0) || request.Lines.Select(x => x.InventoryItemId).Distinct().Count() != request.Lines.Count)
            return Result.Failure<RiderInventoryIssueResponse>(MaintenanceErrors.InvalidRequest);
        if (!await dbContext.RiderProfiles.AsNoTracking().AnyAsync(x => x.Id == request.RiderProfileId, cancellationToken) || await GetActiveInventoryLocationAsync(request.InventoryLocationId, cancellationToken) is null)
            return Result.Failure<RiderInventoryIssueResponse>(MaintenanceErrors.NotFound);
        var assignment = await dbContext.RiderVehicleAssignments.AsNoTracking().Where(x => x.RiderProfileId == request.RiderProfileId && x.StartedAtUtc <= request.IssuedAtUtc && (!x.EndedAtUtc.HasValue || x.EndedAtUtc >= request.IssuedAtUtc)).OrderByDescending(x => x.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var issueId = Guid.CreateVersion7();
        var total = 0m;
        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                var movementId = Guid.CreateVersion7();
                dbContext.StockMovements.Add(NewMovement(movementId, StockMovementType.RiderIssue, request.IssuedAtUtc, request.InventoryLocationId, null, nameof(RiderInventoryIssue), issueId, "Rider inventory issue", actor.Value));
                dbContext.RiderInventoryIssues.Add(new RiderInventoryIssue { Id = issueId, IssueNumber = NewNumber("RDI", request.IssuedAtUtc, issueId), RiderProfileId = request.RiderProfileId, IssuedFromLocationId = request.InventoryLocationId, IssuedAtUtc = request.IssuedAtUtc, IssuedByUserId = actor.Value, RelatedAssignmentId = assignment?.Id, Notes = TrimOrNull(request.Notes), PostedMovementId = movementId });
                foreach (var requestLine in request.Lines)
                {
                    var item = await dbContext.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == requestLine.InventoryItemId && x.ItemType == InventoryItemType.RiderAccessory, cancellationToken);
                    if (item is null) return Result.Failure<decimal>(MaintenanceErrors.InvalidInventoryItem);
                    var allocationResult = await AllocateTrackedLayersAsync(item.Id, request.InventoryLocationId, requestLine.Quantity, request.IssuedAtUtc, cancellationToken);
                    if (allocationResult.IsFailure) return Result.Failure<decimal>(allocationResult.Error);
                    var lineCost = allocationResult.Value!.Sum(x => x.Cost);
                    total += lineCost;
                    var issueLineId = Guid.CreateVersion7();
                    var movementLineId = Guid.CreateVersion7();
                    dbContext.StockMovementLines.Add(new StockMovementLine { Id = movementLineId, StockMovementId = movementId, InventoryItemId = item.Id, Quantity = requestLine.Quantity, BaseUnitOfMeasure = item.BaseUnitOfMeasure, UnitCost = lineCost / requestLine.Quantity, TotalCost = lineCost });
                    dbContext.RiderInventoryIssueLines.Add(new RiderInventoryIssueLine { Id = issueLineId, RiderInventoryIssueId = issueId, InventoryItemId = item.Id, Quantity = requestLine.Quantity, TotalCost = lineCost, StockMovementLineId = movementLineId, ExpectedReturn = requestLine.ExpectedReturn });
                    foreach (var allocation in allocationResult.Value!)
                        dbContext.StockCostAllocations.Add(new StockCostAllocation { StockMovementLineId = movementLineId, RiderInventoryIssueLineId = issueLineId, StockCostLayerId = allocation.Layer.Id, AllocatedQuantity = allocation.Quantity, UnitCost = allocation.Layer.UnitCost, AllocatedCost = allocation.Cost });
                }
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(total);
            });
            if (result.IsFailure) return Result.Failure<RiderInventoryIssueResponse>(result.Error);
        }
        catch (DbUpdateConcurrencyException) { return Result.Failure<RiderInventoryIssueResponse>(MaintenanceErrors.ConcurrencyConflict); }
        catch (DbUpdateException) { return Result.Failure<RiderInventoryIssueResponse>(MaintenanceErrors.InvalidRequest); }
        var version = await dbContext.RiderInventoryIssues.AsNoTracking().Where(x => x.Id == issueId).Select(x => x.RowVersion).SingleAsync(cancellationToken);
        return Result.Success(new RiderInventoryIssueResponse(issueId, NewNumber("RDI", request.IssuedAtUtc, issueId), request.RiderProfileId, assignment?.Id, request.InventoryLocationId, request.IssuedAtUtc, total, InventoryDocumentStatus.Posted, EncodeRowVersion(version)));
    }

    private async Task<InventoryLocation?> GetActiveInventoryLocationAsync(Guid id, CancellationToken cancellationToken)
    {
        return await (from inventory in dbContext.InventoryLocations.AsNoTracking()
                      join maintenance in dbContext.MaintenanceLocations.AsNoTracking() on inventory.MaintenanceLocationId equals maintenance.Id
                      where inventory.Id == id && inventory.Status == CatalogStatus.Active && maintenance.Status == CatalogStatus.Active && maintenance.InventoryEnabled
                      select inventory).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<StockBalance> GetOrCreateBalanceAsync(Guid itemId, Guid locationId, CancellationToken cancellationToken)
    {
        var local = dbContext.StockBalances.Local.FirstOrDefault(x => x.InventoryItemId == itemId && x.InventoryLocationId == locationId);
        if (local is not null) return local;
        var balance = await dbContext.StockBalances.SingleOrDefaultAsync(x => x.InventoryItemId == itemId && x.InventoryLocationId == locationId, cancellationToken);
        if (balance is not null) return balance;
        balance = new StockBalance { InventoryItemId = itemId, InventoryLocationId = locationId };
        dbContext.StockBalances.Add(balance);
        return balance;
    }

    private async Task<Result<IReadOnlyList<TrackedAllocation>>> AllocateTrackedLayersAsync(Guid itemId, Guid locationId, decimal quantity, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        var layers = await dbContext.StockCostLayers
            .Where(x => x.InventoryItemId == itemId && x.InventoryLocationId == locationId && x.RemainingQuantity > 0)
            .OrderBy(x => x.ReceivedAtUtc).ThenBy(x => x.OriginalSequence).ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var allocations = MaintenanceBusinessRules.AllocateFifo(layers.Select(x => new FifoLayerSnapshot(x.Id, x.ReceivedAtUtc, x.OriginalSequence, x.RemainingQuantity, x.UnitCost)), quantity);
        if (allocations is null) return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InsufficientStock);
        var byId = layers.ToDictionary(x => x.Id);
        var tracked = allocations.Select(x => new TrackedAllocation(byId[x.LayerId], x.Quantity, x.Cost)).ToArray();
        foreach (var allocation in tracked) allocation.Layer.RemainingQuantity -= allocation.Quantity;
        var balance = await GetOrCreateBalanceAsync(itemId, locationId, cancellationToken);
        if (balance.QuantityOnHand < quantity) return Result.Failure<IReadOnlyList<TrackedAllocation>>(MaintenanceErrors.InsufficientStock);
        RemoveFromBalance(balance, quantity, occurredAt);
        return Result.Success<IReadOnlyList<TrackedAllocation>>(tracked);
    }

    private static void AddToBalance(StockBalance balance, decimal quantity, decimal value, DateTimeOffset occurredAt)
    {
        var existingValue = balance.QuantityOnHand * balance.ReportingAverageUnitCost;
        balance.QuantityOnHand += quantity;
        balance.ReportingAverageUnitCost = balance.QuantityOnHand == 0 ? 0 : decimal.Round((existingValue + value) / balance.QuantityOnHand, 6, MidpointRounding.AwayFromZero);
        balance.LastMovementAtUtc = occurredAt;
    }

    private static void RemoveFromBalance(StockBalance balance, decimal quantity, DateTimeOffset occurredAt)
    {
        balance.QuantityOnHand -= quantity;
        if (balance.QuantityOnHand == 0) balance.ReportingAverageUnitCost = 0;
        balance.LastMovementAtUtc = occurredAt;
    }

    private static StockMovement NewMovement(Guid id, StockMovementType type, DateTimeOffset occurredAt, Guid? source, Guid? destination, string documentType, Guid documentId, string reason, Guid actor) => new()
    {
        Id = id,
        MovementNumber = NewNumber(type switch { StockMovementType.TransferOut => "TOUT", StockMovementType.TransferIn => "TIN", StockMovementType.SupplierReturn => "SRET", StockMovementType.RiderIssue => "RISS", StockMovementType.MaintenanceUsage => "USE", StockMovementType.ExternalPartSale => "SALE", StockMovementType.Reversal => "REV", _ => "MOV" }, occurredAt, id),
        MovementType = type,
        OccurredAtUtc = occurredAt,
        SourceLocationId = source,
        DestinationLocationId = destination,
        SourceDocumentType = documentType,
        SourceDocumentId = documentId,
        Reason = reason.Trim(),
        PostedByUserId = actor
    };

    private static bool ValidReceiptRequest(PostPurchaseReceiptRequest request) =>
        request.InvoiceDate != default
        && request.ReceivedAtUtc != default
        && request.Lines is { Count: > 0 }
        && !string.IsNullOrWhiteSpace(request.CurrencyCode)
        && request.CurrencyCode.Trim().Length == 3;

    private static bool ValidReceiptLine(PurchaseReceiptLineRequest line, InventoryItem? item) =>
        item is not null && line.PackageCount > 0 && line.DeclaredQuantityPerPackage > 0 && line.PackageUnitPrice >= 0
        && line.DiscountAmount >= 0 && line.TaxAmount >= 0 && line.DiscountAmount <= decimal.Round(line.PackageCount * line.PackageUnitPrice, 2, MidpointRounding.AwayFromZero)
        && line.GrossWeightKg is null or > 0 && line.NetWeightKg is null or > 0
        && line.PurchaseUnit == item.PurchaseUnitOfMeasure
        && (item.ItemType != InventoryItemType.Oil
            || item.BaseUnitOfMeasure == InventoryUnitOfMeasure.Liter
            && line.PurchaseUnit == InventoryUnitOfMeasure.Barrel
            && line.PackageCount == decimal.Truncate(line.PackageCount)
            && line.GrossWeightKg is > 0
            && line.NetWeightKg is > 0
            && line.NetWeightKg <= line.GrossWeightKg);

    private static bool IsBillDocument(PrivateFileUpload file) => file.Length is > 0 and <= MaximumBillFileSize
        && (file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

    private sealed record TrackedAllocation(StockCostLayer Layer, decimal Quantity, decimal Cost);
}
