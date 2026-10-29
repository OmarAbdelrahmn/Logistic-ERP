SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @receipt uniqueidentifier='01a17896-4ec9-731b-aadb-ef96b1809e6d';
DECLARE @line uniqueidentifier='01a17896-4ed6-73f1-aa21-ce68dfd88d01';
DECLARE @layer uniqueidentifier='01a17896-4ed6-7b6b-9608-1d631129fb00';
DECLARE @movementLine uniqueidentifier='01a17896-4ed6-7119-8887-cc4cd27b878e';
DECLARE @movement uniqueidentifier='01a17896-4ed4-778b-8376-8318edbedd4e';
DECLARE @item uniqueidentifier='01a0e5bc-8289-75a3-8b33-c93a95e3b472';
DECLARE @location uniqueidentifier='019d77f0-0000-7000-8000-000000000004';
DECLARE @now datetimeoffset(7)=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');

SELECT * INTO #ReceiptBefore FROM maintenance.PurchaseReceipts WITH(UPDLOCK,HOLDLOCK) WHERE Id=@receipt;
SELECT * INTO #LinesBefore FROM maintenance.PurchaseReceiptLines WITH(UPDLOCK,HOLDLOCK) WHERE PurchaseReceiptId=@receipt;
SELECT * INTO #LayerBefore FROM maintenance.StockCostLayers WITH(UPDLOCK,HOLDLOCK) WHERE Id=@layer;
SELECT * INTO #MovementLinesBefore FROM maintenance.StockMovementLines WITH(UPDLOCK,HOLDLOCK) WHERE StockMovementId=@movement;
SELECT * INTO #MovementBefore FROM maintenance.StockMovements WITH(UPDLOCK,HOLDLOCK) WHERE Id=@movement;
SELECT * INTO #BarrelsBefore FROM maintenance.OilBarrels WITH(UPDLOCK,HOLDLOCK) WHERE PurchaseReceiptLineId=@line;
SELECT * INTO #BalanceBefore FROM maintenance.StockBalances WITH(UPDLOCK,HOLDLOCK) WHERE InventoryItemId=@item AND InventoryLocationId=@location AND IsDeleted=0;
SELECT * INTO #AttachmentsBefore FROM maintenance.PurchaseReceiptAttachments WITH(HOLDLOCK) WHERE PurchaseReceiptId=@receipt;

IF (SELECT COUNT(*) FROM #ReceiptBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #ReceiptBefore WHERE ReceiptNumber='BILL-20261026-B1809E6D' AND SupplierInvoiceNumber=N'56143' AND IsDeleted=0 AND Status=1 AND Subtotal=3826.10 AND TaxAmount=573.92 AND DiscountAmount=0 AND InventoryValuationAmount=3826.10 AND TotalAmount=4400.02 AND InventoryLocationId=@location AND PostedMovementId=@movement AND RowVersion=0x000000000003D3EF)
 THROW 51000,'Receipt differs from the reviewed one-barrel invoice. Reinspect before applying.',1;
IF (SELECT COUNT(*) FROM #LinesBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #LinesBefore WHERE Id=@line AND InventoryItemId=@item AND PackageCount=1 AND PurchaseUnit=3 AND DeclaredQuantityPerPackage=208 AND ReceivedBaseQuantity=208 AND BaseUnitOfMeasure=2 AND PackageUnitPrice=3826.10 AND LineSubtotal=3826.10 AND DiscountAmount=0 AND TaxAmount=573.92 AND InventoryValuationAmount=3826.10 AND BaseUnitCost=18.394712 AND StockCostLayerId=@layer AND StockMovementLineId=@movementLine)
 THROW 51000,'Receipt line changed or does not match the reviewed barrel.',1;
IF (SELECT COUNT(*) FROM #LayerBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #LayerBefore WHERE IsDeleted=0 AND SourceReceiptLineId=@line AND SourceMovementLineId=@movementLine AND InventoryItemId=@item AND InventoryLocationId=@location AND OriginalQuantity=208 AND RemainingQuantity=208 AND OriginalTotalCost=3826.10 AND UnitCost=18.394712 AND RowVersion=0x000000000003D3F2)
 THROW 51000,'Cost layer changed or has been consumed.',1;
IF (SELECT COUNT(*) FROM #MovementLinesBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #MovementLinesBefore WHERE Id=@movementLine AND InventoryItemId=@item AND Quantity=208 AND TotalCost=3826.10 AND CostLayerId=@layer AND UnitCost=18.394712)
 OR (SELECT COUNT(*) FROM #MovementBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #MovementBefore WHERE MovementType=1 AND SourceDocumentId=@receipt AND DestinationLocationId=@location AND ReversalOfMovementId IS NULL)
 THROW 51000,'Original stock movement does not match the reviewed receipt.',1;
IF (SELECT COUNT(*) FROM #BarrelsBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #BarrelsBefore WHERE IsDeleted=0 AND PackageSequence=1 AND Status=1 AND RemainingLiters=208 AND NominalCapacityLiters=208 AND StockCostLayerId=@layer AND InventoryItemId=@item AND InventoryLocationId=@location AND RecordedLossLiters=0 AND RecordedMissingLiters=0 AND OpenedAtUtc IS NULL AND UnitCostPerLiter=18.394712)
 THROW 51000,'Original barrel changed or has been used.',1;
IF (SELECT COUNT(*) FROM #BalanceBefore)<>1 OR NOT EXISTS(SELECT 1 FROM #BalanceBefore WHERE QuantityOnHand=210 AND QuantityReserved=0 AND ReportingAverageUnitCost=18.304231 AND RowVersion=0x000000000003D3ED)
 THROW 51000,'Stock balance changed since inspection.',1;
IF EXISTS(SELECT 1 FROM maintenance.StockCostAllocations WITH(HOLDLOCK) WHERE StockCostLayerId=@layer)
 OR EXISTS(SELECT 1 FROM maintenance.StockCostLayers WITH(HOLDLOCK) WHERE SourceCostLayerId=@layer)
 OR EXISTS(SELECT 1 FROM maintenance.StockMovementLines WITH(HOLDLOCK) WHERE CostLayerId=@layer AND Id<>@movementLine)
 OR EXISTS(SELECT 1 FROM maintenance.StockMovements WITH(HOLDLOCK) WHERE ReversalOfMovementId=@movement)
 OR EXISTS(SELECT 1 FROM maintenance.SupplierReturns WITH(HOLDLOCK) WHERE PurchaseReceiptId=@receipt)
 OR EXISTS(SELECT 1 FROM maintenance.SupplierReturnLines WITH(HOLDLOCK) WHERE StockCostLayerId=@layer)
 OR EXISTS(SELECT 1 FROM maintenance.OilBarrelUsageAllocations a WITH(HOLDLOCK) JOIN #BarrelsBefore b ON b.Id=a.OilBarrelId)
 OR EXISTS(SELECT 1 FROM maintenance.OilBarrelLosses a WITH(HOLDLOCK) JOIN #BarrelsBefore b ON b.Id=a.OilBarrelId)
 THROW 51000,'Downstream inventory activity prevents this isolated correction.',1;
IF (SELECT SUM(RemainingQuantity) FROM maintenance.StockCostLayers WITH(HOLDLOCK) WHERE InventoryItemId=@item AND InventoryLocationId=@location AND IsDeleted=0)<>210
 THROW 51000,'Stock balance and cost layers do not reconcile before correction.',1;

DECLARE @before nvarchar(max)=(SELECT
 JSON_QUERY((SELECT * FROM #ReceiptBefore FOR JSON PATH)) Receipt,
 JSON_QUERY((SELECT * FROM #LinesBefore FOR JSON PATH)) Lines,
 JSON_QUERY((SELECT * FROM #LayerBefore FOR JSON PATH)) CostLayers,
 JSON_QUERY((SELECT * FROM #MovementBefore FOR JSON PATH)) Movements,
 JSON_QUERY((SELECT * FROM #MovementLinesBefore FOR JSON PATH)) MovementLines,
 JSON_QUERY((SELECT * FROM #BarrelsBefore FOR JSON PATH)) Barrels,
 JSON_QUERY((SELECT * FROM #BalanceBefore FOR JSON PATH)) Balances,
 JSON_QUERY((SELECT * FROM #AttachmentsBefore FOR JSON PATH)) Attachments
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
SELECT @before AS BeforeJson;

-- Preserve the original per-barrel price and per-barrel invoiced tax.
UPDATE maintenance.PurchaseReceipts SET Subtotal=7652.20,TaxAmount=1147.84,InventoryValuationAmount=7652.20,TotalAmount=8800.04,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE Id=@receipt;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated receipt.',1;
UPDATE maintenance.PurchaseReceiptLines SET PackageCount=2,ReceivedBaseQuantity=416,LineSubtotal=7652.20,TaxAmount=1147.84,InventoryValuationAmount=7652.20 WHERE Id=@line;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated receipt line.',1;
UPDATE maintenance.StockMovementLines SET Quantity=416,TotalCost=7652.20 WHERE Id=@movementLine;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated stock movement line.',1;
UPDATE maintenance.StockCostLayers SET OriginalQuantity=416,RemainingQuantity=416,OriginalTotalCost=7652.20,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE Id=@layer;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated cost layer.',1;
UPDATE maintenance.StockBalances SET ReportingAverageUnitCost=ROUND((QuantityOnHand*ReportingAverageUnitCost+3826.10)/(QuantityOnHand+208),6),QuantityOnHand=QuantityOnHand+208,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE Id=(SELECT Id FROM #BalanceBefore);
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated stock balance.',1;

INSERT maintenance.OilBarrels(Id,BarrelNumber,PurchaseReceiptLineId,InventoryItemId,InventoryLocationId,StockCostLayerId,PackageSequence,NominalCapacityLiters,RemainingLiters,UnitCostPerLiter,MaximumAllowedLossLiters,RecordedLossLiters,RecordedMissingLiters,Status,OpenedAtUtc,OpenedByUserId,DepletedAtUtc,CreatedAtUtc,CreatedByUserId,UpdatedAtUtc,UpdatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason,AllowedVehicleType,AllowBothVehicleTypes)
SELECT @BarrelId,'OB-20261026-'+UPPER(RIGHT(CONVERT(varchar(36),@BarrelId),8)),PurchaseReceiptLineId,InventoryItemId,InventoryLocationId,StockCostLayerId,2,NominalCapacityLiters,NominalCapacityLiters,UnitCostPerLiter,MaximumAllowedLossLiters,0,0,1,NULL,NULL,NULL,@now,NULL,NULL,NULL,0,NULL,NULL,NULL,AllowedVehicleType,AllowBothVehicleTypes FROM #BarrelsBefore;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one new sealed oil barrel.',1;

IF NOT EXISTS(SELECT 1 FROM maintenance.PurchaseReceipts p JOIN maintenance.PurchaseReceiptLines l ON l.PurchaseReceiptId=p.Id JOIN maintenance.StockCostLayers c ON c.Id=l.StockCostLayerId JOIN maintenance.StockMovementLines m ON m.Id=l.StockMovementLineId WHERE p.Id=@receipt AND l.PackageCount=2 AND l.ReceivedBaseQuantity=416 AND c.OriginalQuantity=416 AND c.RemainingQuantity=416 AND m.Quantity=416 AND p.Subtotal=7652.20 AND l.LineSubtotal=p.Subtotal AND p.InventoryValuationAmount=c.OriginalTotalCost AND c.OriginalTotalCost=m.TotalCost AND p.TaxAmount=1147.84 AND l.TaxAmount=p.TaxAmount AND p.TotalAmount=8800.04 AND p.TotalAmount=p.InventoryValuationAmount+p.TaxAmount)
 THROW 51000,'Corrected receipt, quantity or valuation failed verification.',1;
IF (SELECT COUNT(*) FROM maintenance.OilBarrels WHERE PurchaseReceiptLineId=@line AND IsDeleted=0)<>2
 OR (SELECT SUM(RemainingLiters) FROM maintenance.OilBarrels WHERE PurchaseReceiptLineId=@line AND IsDeleted=0)<>416
 OR EXISTS(SELECT * FROM #BarrelsBefore EXCEPT SELECT * FROM maintenance.OilBarrels WHERE PurchaseReceiptLineId=@line)
 THROW 51000,'Barrels failed verification or original barrel was changed.',1;
IF NOT EXISTS(SELECT 1 FROM maintenance.StockBalances WHERE InventoryItemId=@item AND InventoryLocationId=@location AND IsDeleted=0 AND QuantityOnHand=418 AND QuantityReserved=0)
 OR (SELECT SUM(RemainingQuantity) FROM maintenance.StockCostLayers WHERE InventoryItemId=@item AND InventoryLocationId=@location AND IsDeleted=0)<>418
 THROW 51000,'Corrected inventory balance does not reconcile.',1;
IF EXISTS(SELECT * FROM #AttachmentsBefore EXCEPT SELECT * FROM maintenance.PurchaseReceiptAttachments WHERE PurchaseReceiptId=@receipt)
 OR EXISTS(SELECT * FROM #MovementBefore EXCEPT SELECT * FROM maintenance.StockMovements WHERE Id=@movement)
 THROW 51000,'Receipt attachment or movement header changed unexpectedly.',1;

DECLARE @after nvarchar(max)=(SELECT
 JSON_QUERY((SELECT * FROM maintenance.PurchaseReceipts WHERE Id=@receipt FOR JSON PATH)) Receipt,
 JSON_QUERY((SELECT * FROM maintenance.PurchaseReceiptLines WHERE PurchaseReceiptId=@receipt FOR JSON PATH)) Lines,
 JSON_QUERY((SELECT * FROM maintenance.StockCostLayers WHERE Id=@layer FOR JSON PATH)) CostLayers,
 JSON_QUERY((SELECT * FROM maintenance.StockMovements WHERE Id=@movement FOR JSON PATH)) Movements,
 JSON_QUERY((SELECT * FROM maintenance.StockMovementLines WHERE StockMovementId=@movement FOR JSON PATH)) MovementLines,
 JSON_QUERY((SELECT * FROM maintenance.OilBarrels WHERE PurchaseReceiptLineId=@line FOR JSON PATH)) Barrels,
 JSON_QUERY((SELECT * FROM maintenance.StockBalances WHERE InventoryItemId=@item AND InventoryLocationId=@location AND IsDeleted=0 FOR JSON PATH)) Balances,
 JSON_QUERY((SELECT * FROM maintenance.PurchaseReceiptAttachments WHERE PurchaseReceiptId=@receipt FOR JSON PATH)) Attachments
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(@AuditId,@EventId,NULL,N'System',N'CorrectPurchaseReceiptQuantity',N'Maintenance',N'PurchaseReceipt',@receipt,@now,@Correlation,N'User requested changing this invoice from one barrel to two. Original barrel unit price and invoiced per-barrel tax preserved; added 208 liters and a second sealed barrel. Executed by Codex.',@before,@after,N'CodexUserRequestedCorrection',1,@now,NULL);
SELECT @after AS AfterJson;
