-- Run only inside the caller's transaction and roll that transaction back.
SET XACT_ABORT OFF;
SET ARITHABORT ON;
DECLARE @barrel uniqueidentifier, @bike uniqueidentifier, @next uniqueidentifier,
        @other uniqueidentifier, @item uniqueidentifier, @warehouse uniqueidentifier,
        @layer uniqueidentifier, @actor uniqueidentifier, @version binary(8),
        @quantity decimal(18,3), @unitCost decimal(18,6), @balanceBefore decimal(18,3),
        @layerBefore decimal(18,3), @now datetimeoffset = SYSUTCDATETIME(),
        @loss uniqueidentifier = NEWID(), @movement uniqueidentifier = NEWID(), @line uniqueidentifier = NEWID();

SELECT @barrel=Id,@item=InventoryItemId,@warehouse=InventoryLocationId,@layer=StockCostLayerId,
       @actor=OpenedByUserId,@version=RowVersion,@quantity=RemainingLiters
FROM maintenance.OilBarrels WHERE BarrelNumber='OB-20260928-2E748D48' AND Status=2 AND RemainingLiters=25 AND IsDeleted=0;
IF @barrel IS NULL THROW 51000, 'Expected 25-liter car barrel is not available for this rollback verification.', 1;
SELECT TOP 1 @bike=Id FROM maintenance.OilBarrels WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND Status=2 AND AllowedVehicleType=1 AND IsDeleted=0;
SELECT TOP 1 @next=Id FROM maintenance.OilBarrels WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND Status=1 AND IsDeleted=0 ORDER BY PackageSequence,Id;
SELECT TOP 1 @other=Id FROM maintenance.OilBarrels WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND Status=1 AND IsDeleted=0 AND Id<>@next ORDER BY PackageSequence,Id;
IF @bike IS NULL OR @next IS NULL OR @other IS NULL THROW 51000, 'Expected barrel fixtures are missing.', 1;
SELECT @unitCost=UnitCost,@layerBefore=RemainingQuantity FROM maintenance.StockCostLayers WHERE Id=@layer;
SELECT @balanceBefore=QuantityOnHand FROM maintenance.StockBalances WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND IsDeleted=0;

SAVE TRANSACTION VerifyScope;
BEGIN TRY
    UPDATE maintenance.OilBarrels SET Status=2,OpenedAtUtc=@now,AllowedVehicleType=NULL,AllowBothVehicleTypes=1 WHERE Id=@next;
    THROW 51000, 'Database allowed a shared barrel beside dedicated barrels.', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601,2627) THROW;
    ROLLBACK TRANSACTION VerifyScope;
END CATCH;

-- Validate the same inventory accounting records written by the service.
INSERT maintenance.StockMovements
    (Id,CreatedAtUtc,CreatedByUserId,MovementNumber,MovementType,OccurredAtUtc,SourceLocationId,SourceDocumentType,SourceDocumentId,Reason,PostedByUserId)
VALUES (@movement,@now,@actor,'VERIFY-OIL-'+CONVERT(varchar(36),@movement),9,@now,@warehouse,'OilBarrelLoss',@loss,'Rollback verification',@actor);
INSERT maintenance.StockMovementLines
    (Id,CreatedAtUtc,CreatedByUserId,StockMovementId,InventoryItemId,Quantity,BaseUnitOfMeasure,UnitCost,TotalCost)
VALUES (@line,@now,@actor,@movement,@item,@quantity,2,@unitCost,ROUND(@quantity*@unitCost,2));
INSERT maintenance.StockCostAllocations
    (Id,CreatedAtUtc,CreatedByUserId,StockMovementLineId,StockCostLayerId,AllocatedQuantity,UnitCost,AllocatedCost)
VALUES (NEWID(),@now,@actor,@line,@layer,@quantity,@unitCost,ROUND(@quantity*@unitCost,2));
INSERT maintenance.OilBarrelLosses
    (Id,CreatedAtUtc,CreatedByUserId,OilBarrelId,OccurredAtUtc,QuantityLiters,CostAmount,Reason,StockMovementId,StockMovementLineId,RecordedByUserId,MissingReason,ResponsibleInventoryLocationId)
VALUES (@loss,@now,@actor,@barrel,@now,@quantity,ROUND(@quantity*@unitCost,2),'Rollback verification',@movement,@line,@actor,1,@warehouse);
UPDATE maintenance.StockCostLayers SET RemainingQuantity=RemainingQuantity-@quantity WHERE Id=@layer;
UPDATE maintenance.StockBalances SET QuantityOnHand=QuantityOnHand-@quantity WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND IsDeleted=0;
UPDATE maintenance.OilBarrels SET RemainingLiters=0,RecordedMissingLiters=RecordedMissingLiters+@quantity,Status=3,DepletedAtUtc=@now WHERE Id=@barrel AND RowVersion=@version;
IF @@ROWCOUNT<>1 THROW 51000, 'Expected first rowversion update to succeed.', 1;
UPDATE maintenance.OilBarrels SET RemainingLiters=RemainingLiters WHERE Id=@barrel AND RowVersion=@version;
IF @@ROWCOUNT<>0 THROW 51000, 'Stale rowversion was not rejected.', 1;
IF NOT EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE Id=@barrel AND RemainingLiters=0 AND Status=3 AND RecordedMissingLiters=25 AND RecordedLossLiters=0)
    THROW 51000, 'Full write-off did not deplete the barrel correctly.', 1;
IF NOT EXISTS (SELECT 1 FROM maintenance.StockCostLayers WHERE Id=@layer AND RemainingQuantity=@layerBefore-@quantity)
    THROW 51000, 'Cost layer deduction was incorrect.', 1;
IF NOT EXISTS (SELECT 1 FROM maintenance.StockBalances WHERE InventoryItemId=@item AND InventoryLocationId=@warehouse AND QuantityOnHand=@balanceBefore-@quantity AND IsDeleted=0)
    THROW 51000, 'Warehouse deduction was incorrect.', 1;

-- A dedicated next car barrel can open while the motorcycle barrel remains open.
UPDATE maintenance.OilBarrels SET Status=2,OpenedAtUtc=@now,AllowedVehicleType=2,AllowBothVehicleTypes=0 WHERE Id=@next;
-- Deplete both scopes temporarily to verify a shared barrel can occupy both slots.
UPDATE maintenance.OilBarrels SET Status=3,OpenedAtUtc=COALESCE(OpenedAtUtc,@now),DepletedAtUtc=@now,
    RecordedMissingLiters=RecordedMissingLiters+RemainingLiters,RemainingLiters=0 WHERE Id IN (@next,@bike);
UPDATE maintenance.OilBarrels SET Status=2,OpenedAtUtc=@now,AllowedVehicleType=NULL,AllowBothVehicleTypes=1 WHERE Id=@other;
IF NOT EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE Id=@other AND OpenCarSlot=1 AND OpenMotorcycleSlot=1)
    THROW 51000, 'Shared barrel does not occupy both scopes.', 1;

SELECT 'PASS: schema, overlap rejection, full write-off records, stock/cost deductions, stale rowversion, next opening, shared slots' AS Verification;
