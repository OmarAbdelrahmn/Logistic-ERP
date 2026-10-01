SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @existingBarrels int;
    SELECT @existingBarrels = COUNT(*)
    FROM maintenance.OilBarrels WITH (TABLOCKX, HOLDLOCK)
    WHERE IsDeleted = 0;
    IF @existingBarrels <> 4
        THROW 51020, 'Expected exactly four existing oil barrels.', 1;

    DECLARE @itemId uniqueidentifier, @locationId uniqueidentifier,
        @capacity decimal(18, 3), @unitCost decimal(18, 6), @lossAllowance decimal(18, 3);
    SELECT TOP (1)
        @itemId = InventoryItemId,
        @locationId = InventoryLocationId,
        @capacity = NominalCapacityLiters,
        @unitCost = UnitCostPerLiter,
        @lossAllowance = MaximumAllowedLossLiters
    FROM maintenance.OilBarrels
    WHERE IsDeleted = 0;

    IF @capacity <> 208.000 OR @unitCost <> 8.894231
        THROW 51021, 'The source barrel capacity or unit cost changed.', 1;
    IF EXISTS (
        SELECT 1 FROM maintenance.OilBarrels
        WHERE IsDeleted = 0 AND (InventoryItemId <> @itemId OR InventoryLocationId <> @locationId
            OR NominalCapacityLiters <> @capacity OR RemainingLiters <> @capacity
            OR UnitCostPerLiter <> @unitCost OR MaximumAllowedLossLiters <> @lossAllowance
            OR RecordedLossLiters <> 0 OR Status NOT IN (1, 2)))
        THROW 51022, 'The source barrels no longer share the expected details.', 1;
    IF (SELECT COUNT(*) FROM maintenance.OilBarrels WHERE IsDeleted = 0 AND Status = 1) <> 3
        THROW 51023, 'Expected three sealed source barrels.', 1;
    IF (SELECT COUNT(*) FROM maintenance.OilBarrels WHERE IsDeleted = 0 AND Status = 2) <> 1
        THROW 51024, 'Expected one open source barrel.', 1;
    IF NOT EXISTS (SELECT 1 FROM maintenance.InventoryItems WHERE Id = @itemId AND Sku = 'OIL-10W41' AND IsDeleted = 0)
        THROW 51025, 'The source oil item changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM maintenance.InventoryLocations WHERE Id = @locationId AND Code = 'JEDDAH_WAREHOUSE_STOCK' AND IsDeleted = 0)
        THROW 51026, 'The source inventory location changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM maintenance.StockBalances WITH (UPDLOCK, HOLDLOCK)
                   WHERE InventoryItemId = @itemId AND InventoryLocationId = @locationId
                     AND IsDeleted = 0 AND QuantityOnHand = 832.000 AND QuantityReserved = 0)
        THROW 51027, 'The source stock balance changed.', 1;

    DECLARE @now datetimeoffset(7) = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');
    DECLARE @quantity decimal(18, 3) = @capacity * 2;
    DECLARE @totalCost decimal(18, 2) = ROUND(@quantity * @unitCost, 2);
    DECLARE @actorId uniqueidentifier = '019c18d5-62e1-7000-d000-000000000002';
    DECLARE @movementId uniqueidentifier = NEWID(), @movementLineId uniqueidentifier = NEWID(),
        @layerId uniqueidentifier = NEWID(), @barrelId1 uniqueidentifier = NEWID(),
        @barrelId2 uniqueidentifier = NEWID();
    DECLARE @dateToken char(8) = CONVERT(char(8), CAST(@now AS date), 112);

    INSERT maintenance.StockMovements
        (Id, MovementNumber, MovementType, OccurredAtUtc, DestinationLocationId,
         SourceDocumentType, SourceDocumentId, Reason, PostedByUserId, CreatedAtUtc, CreatedByUserId)
    VALUES
        (@movementId, CONCAT('ADJ-', @dateToken, '-', UPPER(LEFT(REPLACE(CONVERT(varchar(36), @movementId), '-', ''), 8))),
         10, @now, @locationId, 'ManualOilStockAddition', @movementId,
         'Two sealed oil barrels added to Jeddah stock without a supplier invoice.', @actorId, @now, @actorId);

    INSERT maintenance.StockCostLayers
        (Id, InventoryItemId, InventoryLocationId, ReceivedAtUtc, OriginalSequence,
         OriginalQuantity, RemainingQuantity, BaseUnitOfMeasure, UnitCost, OriginalTotalCost,
         CreatedAtUtc, CreatedByUserId, IsDeleted)
    VALUES
        (@layerId, @itemId, @locationId, @now,
         DATEDIFF_BIG(MICROSECOND, '2000-01-01', CAST(@now AS datetime2(7))),
         @quantity, @quantity, 2, @unitCost, @totalCost, @now, @actorId, 0);

    INSERT maintenance.StockMovementLines
        (Id, StockMovementId, InventoryItemId, Quantity, BaseUnitOfMeasure,
         CostLayerId, UnitCost, TotalCost, CreatedAtUtc, CreatedByUserId)
    VALUES
        (@movementLineId, @movementId, @itemId, @quantity, 2,
         @layerId, @unitCost, @totalCost, @now, @actorId);

    UPDATE maintenance.StockCostLayers
    SET SourceMovementLineId = @movementLineId
    WHERE Id = @layerId;

    INSERT maintenance.OilBarrels
        (Id, BarrelNumber, PurchaseReceiptLineId, InventoryItemId, InventoryLocationId,
         StockCostLayerId, PackageSequence, NominalCapacityLiters, RemainingLiters,
         UnitCostPerLiter, MaximumAllowedLossLiters, RecordedLossLiters, Status,
         CreatedAtUtc, CreatedByUserId, IsDeleted)
    VALUES
        (@barrelId1, CONCAT('OB-', @dateToken, '-', UPPER(LEFT(REPLACE(CONVERT(varchar(36), @barrelId1), '-', ''), 8))),
         NULL, @itemId, @locationId, @layerId, 1, @capacity, @capacity,
         @unitCost, @lossAllowance, 0, 1, @now, @actorId, 0),
        (@barrelId2, CONCAT('OB-', @dateToken, '-', UPPER(LEFT(REPLACE(CONVERT(varchar(36), @barrelId2), '-', ''), 8))),
         NULL, @itemId, @locationId, @layerId, 2, @capacity, @capacity,
         @unitCost, @lossAllowance, 0, 1, @now, @actorId, 0);

    UPDATE maintenance.StockBalances
    SET QuantityOnHand = QuantityOnHand + @quantity, LastMovementAtUtc = @now
    WHERE InventoryItemId = @itemId AND InventoryLocationId = @locationId AND IsDeleted = 0;
    IF @@ROWCOUNT <> 1
        THROW 51028, 'Expected one Jeddah oil stock balance.', 1;

    IF (SELECT COUNT(*) FROM maintenance.OilBarrels WHERE IsDeleted = 0) <> 6
        OR (SELECT SUM(RemainingLiters) FROM maintenance.OilBarrels WHERE IsDeleted = 0) <> 1248.000
        OR (SELECT QuantityOnHand FROM maintenance.StockBalances
            WHERE InventoryItemId = @itemId AND InventoryLocationId = @locationId AND IsDeleted = 0) <> 1248.000
        OR (SELECT SUM(RemainingQuantity) FROM maintenance.StockCostLayers
            WHERE InventoryItemId = @itemId AND InventoryLocationId = @locationId AND IsDeleted = 0) <> 1248.000
        THROW 51029, 'Oil stock reconciliation failed.', 1;

    COMMIT TRANSACTION;

    SELECT BarrelNumber, Status, NominalCapacityLiters, RemainingLiters, UnitCostPerLiter,
           InventoryLocationId, PurchaseReceiptLineId
    FROM maintenance.OilBarrels
    WHERE Id IN (@barrelId1, @barrelId2)
    ORDER BY BarrelNumber;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
