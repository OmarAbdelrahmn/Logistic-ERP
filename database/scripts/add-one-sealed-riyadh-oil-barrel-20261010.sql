-- User-requested new stock: one sealed barrel matching the current oil barrels.
-- Fixed IDs make this operation repeatable without adding a second barrel.
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
    DECLARE @barrelId uniqueidentifier = '5694a585-22eb-475a-84b6-7776b889b0f9',
        @movementId uniqueidentifier = '79f87358-7cff-4498-ae0c-b6c6bb835b17',
        @movementLineId uniqueidentifier = '5cf4367b-fcf3-42af-a844-cfa45920abb4',
        @layerId uniqueidentifier = 'd5e85593-7717-4abc-b2ce-de05bfd93930',
        @locationId uniqueidentifier = '019d77f0-0000-7000-8000-000000000004',
        @itemId uniqueidentifier = '01a0e5bc-8289-75a3-8b33-c93a95e3b472',
        @systemActorId uniqueidentifier = '019c18d5-62e1-7000-d000-000000000002',
        @sourceBarrelId uniqueidentifier = '696b5735-f89f-46dc-9f4e-997cf2c9aa48';
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock
        @Resource='UserRequestedRiyadhOilBarrel20261010', @LockMode='Exclusive',
        @LockOwner='Transaction', @LockTimeout=15000;
    IF @lockResult < 0 THROW 51100, 'Could not lock this barrel addition.', 1;

    IF NOT EXISTS (SELECT 1 FROM maintenance.InventoryLocations il
        JOIN maintenance.MaintenanceLocations ml ON ml.Id=il.MaintenanceLocationId
        WHERE il.Id=@locationId AND il.Code='RIYADH_WORKSHOP_STOCK' AND il.IsDeleted=0 AND il.Status=1
          AND ml.IsDeleted=0 AND ml.Status=1 AND ml.InventoryEnabled=1)
        THROW 51101, 'Expected active Riyadh inventory location is unavailable.', 1;
    IF NOT EXISTS (SELECT 1 FROM maintenance.InventoryItems WHERE Id=@itemId AND Sku='OIL-10W41'
        AND ItemType=3 AND BaseUnitOfMeasure=2 AND Status=1 AND IsDeleted=0)
        THROW 51102, 'Expected oil inventory item is unavailable.', 1;

    IF EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE Id=@barrelId)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE Id=@barrelId
            AND InventoryItemId=@itemId AND InventoryLocationId=@locationId AND NominalCapacityLiters=208)
            THROW 51103, 'Operation ID belongs to an unexpected barrel.', 1;
        COMMIT TRANSACTION;
        SELECT 'Already added; no additional stock was created' AS Result,
            Id,BarrelNumber,Status,NominalCapacityLiters,RemainingLiters,InventoryLocationId
        FROM maintenance.OilBarrels WHERE Id=@barrelId;
        RETURN;
    END;

    DECLARE @capacity decimal(18,3), @unitCost decimal(18,6), @lossAllowance decimal(18,3);
    SELECT @capacity=NominalCapacityLiters,@unitCost=UnitCostPerLiter,@lossAllowance=MaximumAllowedLossLiters
    FROM maintenance.OilBarrels WHERE Id=@sourceBarrelId AND IsDeleted=0 AND InventoryItemId=@itemId;
    IF @capacity IS NULL OR @capacity<>208 OR @unitCost<>8.894231 OR @lossAllowance<>4.160
        THROW 51104, 'Source barrel capacity or cost no longer matches the reviewed details.', 1;
    DECLARE @now datetimeoffset(7)=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00'),
        @cost decimal(18,2)=ROUND(@capacity*@unitCost,2),
        @balanceId uniqueidentifier, @quantityBefore decimal(18,3), @averageBefore decimal(18,6);
    SELECT @balanceId=Id,@quantityBefore=QuantityOnHand,@averageBefore=ReportingAverageUnitCost
    FROM maintenance.StockBalances WITH (UPDLOCK,HOLDLOCK)
    WHERE InventoryItemId=@itemId AND InventoryLocationId=@locationId AND IsDeleted=0;
    SET @quantityBefore=COALESCE(@quantityBefore,0);
    SET @averageBefore=COALESCE(@averageBefore,0);

    INSERT maintenance.StockMovements
        (Id,MovementNumber,MovementType,OccurredAtUtc,DestinationLocationId,SourceDocumentType,
         SourceDocumentId,Reason,PostedByUserId,CreatedAtUtc,CreatedByUserId)
    VALUES (@movementId,'ADJ-20261010-79F87358',10,@now,@locationId,'ManualOilStockAddition',@movementId,
        'One new sealed oil barrel added to Riyadh stock via Codex at the user request on 2026-10-10; same oil, capacity and cost as existing barrels; no supplier invoice.',
        @systemActorId,@now,@systemActorId);
    INSERT maintenance.StockCostLayers
        (Id,InventoryItemId,InventoryLocationId,ReceivedAtUtc,OriginalSequence,OriginalQuantity,
         RemainingQuantity,BaseUnitOfMeasure,UnitCost,OriginalTotalCost,CreatedAtUtc,CreatedByUserId,IsDeleted)
    VALUES (@layerId,@itemId,@locationId,@now,
        DATEDIFF_BIG(MICROSECOND,'2000-01-01',CAST(@now AS datetime2(7))),
        @capacity,@capacity,2,@unitCost,@cost,@now,@systemActorId,0);
    INSERT maintenance.StockMovementLines
        (Id,StockMovementId,InventoryItemId,Quantity,BaseUnitOfMeasure,CostLayerId,UnitCost,TotalCost,CreatedAtUtc,CreatedByUserId)
    VALUES (@movementLineId,@movementId,@itemId,@capacity,2,@layerId,@unitCost,@cost,@now,@systemActorId);
    UPDATE maintenance.StockCostLayers SET SourceMovementLineId=@movementLineId WHERE Id=@layerId;
    INSERT maintenance.OilBarrels
        (Id,BarrelNumber,PurchaseReceiptLineId,InventoryItemId,InventoryLocationId,StockCostLayerId,
         PackageSequence,NominalCapacityLiters,RemainingLiters,UnitCostPerLiter,MaximumAllowedLossLiters,
         RecordedLossLiters,RecordedMissingLiters,Status,AllowedVehicleType,AllowBothVehicleTypes,
         CreatedAtUtc,CreatedByUserId,IsDeleted)
    VALUES (@barrelId,'OB-20261010-5694A585',NULL,@itemId,@locationId,@layerId,1,
        @capacity,@capacity,@unitCost,@lossAllowance,0,0,1,NULL,0,@now,@systemActorId,0);

    IF @balanceId IS NULL
    BEGIN
        SET @balanceId=NEWID();
        INSERT maintenance.StockBalances
            (Id,InventoryItemId,InventoryLocationId,QuantityOnHand,QuantityReserved,ReportingAverageUnitCost,
             LastMovementAtUtc,CreatedAtUtc,CreatedByUserId,IsDeleted)
        VALUES (@balanceId,@itemId,@locationId,@capacity,0,ROUND(@cost/@capacity,6),@now,@now,@systemActorId,0);
    END
    ELSE
    BEGIN
        UPDATE maintenance.StockBalances
        SET QuantityOnHand=QuantityOnHand+@capacity,
            ReportingAverageUnitCost=ROUND((@quantityBefore*@averageBefore+@cost)/(@quantityBefore+@capacity),6),
            LastMovementAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=@systemActorId
        WHERE Id=@balanceId;
    END;

    IF NOT EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE Id=@barrelId
        AND Status=1 AND RemainingLiters=208 AND OpenedAtUtc IS NULL AND AllowedVehicleType IS NULL AND AllowBothVehicleTypes=0)
        OR NOT EXISTS (SELECT 1 FROM maintenance.StockBalances WHERE Id=@balanceId AND QuantityOnHand=@quantityBefore+@capacity)
        OR NOT EXISTS (SELECT 1 FROM maintenance.StockCostLayers WHERE Id=@layerId AND RemainingQuantity=@capacity AND OriginalTotalCost=@cost)
        THROW 51105, 'New Riyadh barrel stock reconciliation failed.', 1;
    COMMIT TRANSACTION;

    SELECT b.Id,b.BarrelNumber,il.NameAr AS InventoryLocationNameAr,il.Code AS InventoryLocationCode,
        b.Status,b.NominalCapacityLiters,b.RemainingLiters,b.UnitCostPerLiter,@cost AS TotalCost,
        b.AllowedVehicleType,b.AllowBothVehicleTypes,b.StockCostLayerId,@movementId AS StockMovementId,
        sb.QuantityOnHand AS WarehouseOilQuantityOnHand
    FROM maintenance.OilBarrels b JOIN maintenance.InventoryLocations il ON il.Id=b.InventoryLocationId
    JOIN maintenance.StockBalances sb ON sb.InventoryItemId=b.InventoryItemId AND sb.InventoryLocationId=b.InventoryLocationId AND sb.IsDeleted=0
    WHERE b.Id=@barrelId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
