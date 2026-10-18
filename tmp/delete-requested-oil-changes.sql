SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
IF @@TRANCOUNT=0 THROW 51000,'Explicit transaction required.',1;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(500)=N'User requested deleting the latest oil change for eight specified plates and the latest two for ح ا 6162.';
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@Correlation) THROW 51000,'Already applied.',1;
SELECT Id,VehicleId,PlateNumberAr,OilMaterialUsageId INTO #Wanted
FROM OPENJSON(@Plan) WITH(Id uniqueidentifier,VehicleId uniqueidentifier,PlateNumberAr nvarchar(32),OilMaterialUsageId uniqueidentifier);
IF (SELECT COUNT(*) FROM #Wanted)<>10 OR (SELECT COUNT(DISTINCT VehicleId) FROM #Wanted)<>9
 THROW 51000,'Expected ten operations for nine vehicles.',1;
IF EXISTS(SELECT PlateNumberAr FROM #Wanted GROUP BY PlateNumberAr HAVING COUNT(*)<>CASE WHEN PlateNumberAr=N'ح ا 6162' THEN 2 ELSE 1 END)
 THROW 51000,'Incorrect requested deletion counts.',1;
SELECT v.* INTO #VehiclesBefore FROM app.Vehicles v WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT VehicleId FROM #Wanted);
IF (SELECT COUNT(*) FROM #VehiclesBefore)<>9 OR EXISTS(SELECT 1 FROM #Wanted w LEFT JOIN #VehiclesBefore v ON v.Id=w.VehicleId
 WHERE v.Id IS NULL OR v.PlateNumberAr<>w.PlateNumberAr OR v.IsDeleted=1 OR v.VehicleType<>1)
 THROW 51000,'Full plate matching failed.',1;
SELECT * INTO #AllOilBefore FROM maintenance.OilChangeOperations WITH(UPDLOCK,HOLDLOCK);
SELECT o.* INTO #Operations FROM #AllOilBefore o JOIN #Wanted w ON w.Id=o.Id AND w.VehicleId=o.VehicleId AND w.OilMaterialUsageId=o.OilMaterialUsageId;
IF (SELECT COUNT(*) FROM #Operations)<>10 OR EXISTS(SELECT 1 FROM #Operations WHERE MaintenanceWorkOrderId IS NOT NULL OR OilFilterChanged<>0 OR OilFilterMaterialUsageId IS NOT NULL
 OR OilQuantityLiters<>1 OR OilCost<>8.89 OR TotalCost<>8.89 OR OtherCost<>0 OR LaborCost<>0)
 THROW 51000,'Selected oil operations changed or have unsupported dependencies.',1;
;WITH Ranked AS(SELECT o.Id,o.VehicleId,ROW_NUMBER() OVER(PARTITION BY o.VehicleId ORDER BY o.PerformedAtUtc DESC,o.Id DESC) rn
 FROM #AllOilBefore o WHERE o.VehicleId IN(SELECT VehicleId FROM #Wanted)),Counts AS(SELECT VehicleId,COUNT(*) n FROM #Wanted GROUP BY VehicleId)
SELECT r.Id INTO #Expected FROM Ranked r JOIN Counts c ON c.VehicleId=r.VehicleId WHERE r.rn<=c.n;
IF EXISTS(SELECT Id FROM #Expected EXCEPT SELECT Id FROM #Wanted) OR EXISTS(SELECT Id FROM #Wanted EXCEPT SELECT Id FROM #Expected)
 THROW 51000,'Selected operations are no longer the latest by service date.',1;
SELECT u.* INTO #Usages FROM maintenance.MaterialUsages u WITH(UPDLOCK,HOLDLOCK) JOIN #Wanted w ON w.OilMaterialUsageId=u.Id AND w.VehicleId=u.VehicleId;
IF (SELECT COUNT(*) FROM #Usages)<>10 OR EXISTS(SELECT 1 FROM #Usages WHERE ReversalOfUsageId IS NOT NULL OR Direction<>1 OR UsageType<>2 OR Quantity<>1 OR TotalCost<>8.89 OR MaintenanceWorkOrderId IS NOT NULL)
 OR EXISTS(SELECT 1 FROM maintenance.MaterialUsages WITH(UPDLOCK,HOLDLOCK) WHERE ReversalOfUsageId IN(SELECT Id FROM #Usages))
 THROW 51000,'Material usage state invalid or already reversed.',1;
SELECT a.* INTO #CostAllocations FROM maintenance.StockCostAllocations a WITH(UPDLOCK,HOLDLOCK) WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM #Usages);
SELECT a.* INTO #BarrelAllocations FROM maintenance.OilBarrelUsageAllocations a WITH(UPDLOCK,HOLDLOCK) WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM #Usages);
IF EXISTS(SELECT 1 FROM #Usages u OUTER APPLY(SELECT SUM(AllocatedQuantity) Quantity,SUM(AllocatedCost) Cost FROM #CostAllocations WHERE MaintenanceMaterialUsageId=u.Id) c
 OUTER APPLY(SELECT SUM(QuantityLiters) Quantity FROM #BarrelAllocations WHERE MaintenanceMaterialUsageId=u.Id AND Direction=1) b
 WHERE ISNULL(c.Quantity,0)<>u.Quantity OR ISNULL(c.Cost,0)<>u.TotalCost OR ISNULL(b.Quantity,0)<>u.Quantity)
 OR EXISTS(SELECT 1 FROM #BarrelAllocations WHERE Direction<>1 OR ReversalOfAllocationId IS NOT NULL)
 OR EXISTS(SELECT 1 FROM maintenance.OilBarrelUsageAllocations WHERE ReversalOfAllocationId IN(SELECT Id FROM #BarrelAllocations))
 THROW 51000,'Oil stock or barrel allocations invalid.',1;
SELECT l.* INTO #LayersBefore FROM maintenance.StockCostLayers l WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT StockCostLayerId FROM #CostAllocations);
SELECT b.* INTO #BarrelsBefore FROM maintenance.OilBarrels b WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT OilBarrelId FROM #BarrelAllocations);
SELECT b.* INTO #BalancesBefore FROM maintenance.StockBalances b WITH(UPDLOCK,HOLDLOCK) WHERE EXISTS(SELECT 1 FROM #Usages u WHERE u.InventoryItemId=b.InventoryItemId AND u.InventoryLocationId=b.InventoryLocationId);
IF EXISTS(SELECT 1 FROM #LayersBefore l JOIN(SELECT StockCostLayerId,SUM(AllocatedQuantity) Quantity FROM #CostAllocations GROUP BY StockCostLayerId) a ON a.StockCostLayerId=l.Id WHERE l.IsDeleted=1 OR l.RemainingQuantity+a.Quantity>l.OriginalQuantity)
 OR EXISTS(SELECT 1 FROM #BarrelsBefore b JOIN(SELECT OilBarrelId,SUM(QuantityLiters) Quantity FROM #BarrelAllocations GROUP BY OilBarrelId) a ON a.OilBarrelId=b.Id WHERE b.IsDeleted=1 OR b.Status<>2 OR b.RemainingLiters+a.Quantity>b.NominalCapacityLiters)
 OR EXISTS(SELECT 1 FROM #Usages u WHERE NOT EXISTS(SELECT 1 FROM #BalancesBefore b WHERE b.InventoryItemId=u.InventoryItemId AND b.InventoryLocationId=u.InventoryLocationId AND b.IsDeleted=0))
 THROW 51000,'Inventory restoration is invalid.',1;
SELECT e.* INTO #Expenses FROM maintenance.VehicleExpenses e WITH(UPDLOCK,HOLDLOCK) WHERE e.SourceEntityType='MaintenanceMaterialUsage' AND e.SourceEntityId IN(SELECT Id FROM #Usages);
IF (SELECT COUNT(*) FROM #Expenses)<>10 OR EXISTS(SELECT 1 FROM #Expenses WHERE ReversalOfExpenseId IS NOT NULL OR TotalAmount<>8.89)
 OR EXISTS(SELECT 1 FROM maintenance.VehicleExpenses WHERE ReversalOfExpenseId IN(SELECT Id FROM #Expenses))
 OR EXISTS(SELECT 1 FROM maintenance.VehicleExpenses WHERE SourceEntityType='OilChangeOperation' AND SourceEntityId IN(SELECT Id FROM #Operations))
 THROW 51000,'Expense state unexpected.',1;
SELECT * INTO #Readings FROM app.VehicleOdometerReadings WITH(UPDLOCK,HOLDLOCK) WHERE SourceEntityId IN(SELECT Id FROM #Operations);
IF (SELECT COUNT(*) FROM #Readings)<>10 OR EXISTS(SELECT 1 FROM #Readings r LEFT JOIN #Operations o ON o.Id=r.SourceEntityId AND o.VehicleId=r.VehicleId WHERE o.Id IS NULL OR r.SourceType<>6)
 THROW 51000,'Expected exactly one maintenance odometer reading per deleted oil change.',1;
SELECT s.* INTO #SchedulesBefore FROM maintenance.VehicleSchedules s WITH(UPDLOCK,HOLDLOCK) WHERE s.VehicleId IN(SELECT VehicleId FROM #Wanted) AND s.IsDeleted=0;
-- Source histories remain intact; the standard reversal records undo stock consumption and vehicle expenses.
SELECT u.Id OriginalUsageId,NEWID() ReversalUsageId,NEWID() MovementId,NEWID() MovementLineId INTO #Reversals FROM #Usages u;
INSERT maintenance.StockMovements(Id,MovementNumber,MovementType,OccurredAtUtc,DestinationLocationId,SourceDocumentType,SourceDocumentId,Reason,PostedByUserId,ReversalOfMovementId,CreatedAtUtc,CreatedByUserId)
SELECT r.MovementId,'REV-'+REPLACE(CONVERT(varchar(36),r.MovementId),'-',''),7,@Now,u.InventoryLocationId,'MaintenanceMaterialUsage',r.ReversalUsageId,@Reason,@Actor,u.StockMovementId,@Now,@Actor FROM #Usages u JOIN #Reversals r ON r.OriginalUsageId=u.Id;
INSERT maintenance.StockMovementLines(Id,StockMovementId,InventoryItemId,Quantity,BaseUnitOfMeasure,UnitCost,TotalCost,CreatedAtUtc,CreatedByUserId)
SELECT r.MovementLineId,r.MovementId,u.InventoryItemId,u.Quantity,u.UnitOfMeasure,u.TotalCost/u.Quantity,u.TotalCost,@Now,@Actor FROM #Usages u JOIN #Reversals r ON r.OriginalUsageId=u.Id;
INSERT maintenance.MaterialUsages(Id,InventoryItemId,InventoryLocationId,UsageType,Direction,Quantity,UnitOfMeasure,TotalCost,StockMovementId,StockMovementLineId,VehicleId,RiderVehicleAssignmentId,RiderProfileId,AttributionStatus,UsedAtUtc,UsedByUserId,Notes,ReversalOfUsageId,CreatedAtUtc,CreatedByUserId)
SELECT r.ReversalUsageId,u.InventoryItemId,u.InventoryLocationId,u.UsageType,2,u.Quantity,u.UnitOfMeasure,u.TotalCost,r.MovementId,r.MovementLineId,u.VehicleId,u.RiderVehicleAssignmentId,u.RiderProfileId,u.AttributionStatus,@Now,@Actor,@Reason,u.Id,@Now,@Actor FROM #Usages u JOIN #Reversals r ON r.OriginalUsageId=u.Id;
INSERT maintenance.StockCostAllocations(Id,StockMovementLineId,MaintenanceMaterialUsageId,StockCostLayerId,AllocatedQuantity,UnitCost,AllocatedCost,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),r.MovementLineId,r.ReversalUsageId,a.StockCostLayerId,a.AllocatedQuantity,a.UnitCost,a.AllocatedCost,@Now,@Actor FROM #CostAllocations a JOIN #Reversals r ON r.OriginalUsageId=a.MaintenanceMaterialUsageId;
INSERT maintenance.OilBarrelUsageAllocations(Id,MaintenanceMaterialUsageId,OilBarrelId,QuantityLiters,Direction,ReversalOfAllocationId,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),r.ReversalUsageId,a.OilBarrelId,a.QuantityLiters,2,a.Id,@Now,@Actor FROM #BarrelAllocations a JOIN #Reversals r ON r.OriginalUsageId=a.MaintenanceMaterialUsageId;
UPDATE l SET RemainingQuantity=l.RemainingQuantity+a.Quantity,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM maintenance.StockCostLayers l JOIN(SELECT StockCostLayerId,SUM(AllocatedQuantity) Quantity FROM #CostAllocations GROUP BY StockCostLayerId) a ON a.StockCostLayerId=l.Id;
UPDATE b SET RemainingLiters=b.RemainingLiters+a.Quantity,DepletedAtUtc=NULL,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM maintenance.OilBarrels b JOIN(SELECT OilBarrelId,SUM(QuantityLiters) Quantity FROM #BarrelAllocations GROUP BY OilBarrelId) a ON a.OilBarrelId=b.Id;
UPDATE b SET ReportingAverageUnitCost=ROUND((b.QuantityOnHand*b.ReportingAverageUnitCost+a.Cost)/(b.QuantityOnHand+a.Quantity),6),QuantityOnHand=b.QuantityOnHand+a.Quantity,LastMovementAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM maintenance.StockBalances b JOIN(SELECT InventoryItemId,InventoryLocationId,SUM(Quantity) Quantity,SUM(TotalCost) Cost FROM #Usages GROUP BY InventoryItemId,InventoryLocationId) a ON a.InventoryItemId=b.InventoryItemId AND a.InventoryLocationId=b.InventoryLocationId;
INSERT maintenance.VehicleExpenses(Id,VehicleId,RiderVehicleAssignmentId,RiderProfileId,ExpenseType,SourceEntityType,SourceEntityId,OccurredOn,AmountBeforeTax,TaxAmount,TotalAmount,CurrencyCode,Description,ReversalOfExpenseId,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),e.VehicleId,e.RiderVehicleAssignmentId,e.RiderProfileId,e.ExpenseType,'MaintenanceMaterialUsage',r.ReversalUsageId,CONVERT(date,@Now),-e.AmountBeforeTax,-e.TaxAmount,-e.TotalAmount,e.CurrencyCode,@Reason,e.Id,@Now,@Actor FROM #Expenses e JOIN #Reversals r ON r.OriginalUsageId=e.SourceEntityId;
DELETE FROM app.VehicleOdometerReadings WHERE Id IN(SELECT Id FROM #Readings);
IF @@ROWCOUNT<>10 THROW 51000,'Odometer reading removal count mismatch.',1;
DELETE FROM maintenance.OilChangeOperations WHERE Id IN(SELECT Id FROM #Operations);
IF @@ROWCOUNT<>10 THROW 51000,'Oil change removal count mismatch.',1;
SELECT v.Id VehicleId,p.Id PlanId,l.MaintenanceWorkOrderId,l.PerformedAtUtc,l.OdometerAtChange,
 l.OdometerAtChange+p.ReminderAfterKilometers ReminderFromOdometer,l.OdometerAtChange+p.MaximumAfterKilometers MaximumDueOdometer,
 CASE WHEN l.Id IS NULL THEN 4 WHEN v.CurrentOdometer<l.OdometerAtChange THEN 5 WHEN v.CurrentOdometer-l.OdometerAtChange>=p.MaximumAfterKilometers THEN 3 WHEN v.CurrentOdometer-l.OdometerAtChange>=p.ReminderAfterKilometers THEN 2 ELSE 1 END ComputedStatus
INTO #Desired FROM #VehiclesBefore v
CROSS APPLY(SELECT TOP(1) * FROM maintenance.Plans WHERE Status=1 AND TriggerType=3 AND VehicleType=v.VehicleType AND IsDeleted=0 ORDER BY Code) p
OUTER APPLY(SELECT TOP(1) * FROM maintenance.OilChangeOperations o WHERE o.VehicleId=v.Id ORDER BY o.PerformedAtUtc DESC,o.OdometerAtChange DESC,o.Id DESC) l;
IF (SELECT COUNT(*) FROM #Desired)<>9 THROW 51000,'Oil plan missing for a requested vehicle.',1;
UPDATE s SET LastCompletedWorkOrderId=d.MaintenanceWorkOrderId,LastCompletedAtUtc=d.PerformedAtUtc,LastCompletedOdometer=d.OdometerAtChange,
ReminderFromOdometer=d.ReminderFromOdometer,MaximumDueOdometer=d.MaximumDueOdometer,ComputedStatus=d.ComputedStatus,ComputedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM maintenance.VehicleSchedules s JOIN #Desired d ON d.VehicleId=s.VehicleId AND d.PlanId=s.MaintenancePlanId WHERE s.IsDeleted=0;
IF @@ROWCOUNT<>9 THROW 51000,'Expected nine oil schedule updates.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','OilChangeDeletedAndUsageReversed','Maintenance','OilChangeOperation',o.Id,@Now,@Correlation,@Reason,
(SELECT o.*,w.PlateNumberAr FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
(SELECT CAST(1 AS bit) Deleted,r.ReversalUsageId,r.MovementId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Operations o JOIN #Wanted w ON w.Id=o.Id JOIN #Reversals r ON r.OriginalUsageId=o.OilMaterialUsageId;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('maintenance','fleet');
-- Verify preserved source histories, untouched vehicle mileages, exact removals and every reversal.
IF EXISTS(SELECT * FROM #VehiclesBefore EXCEPT SELECT * FROM app.Vehicles WHERE Id IN(SELECT Id FROM #VehiclesBefore))
 OR EXISTS(SELECT * FROM #Usages EXCEPT SELECT * FROM maintenance.MaterialUsages WHERE Id IN(SELECT Id FROM #Usages))
 OR EXISTS(SELECT * FROM #AllOilBefore WHERE Id NOT IN(SELECT Id FROM #Operations) EXCEPT SELECT * FROM maintenance.OilChangeOperations)
 OR (SELECT COUNT(*) FROM maintenance.OilChangeOperations)<>(SELECT COUNT(*)-10 FROM #AllOilBefore)
 THROW 51000,'Unrelated oil histories, source usage or vehicle mileage changed.',1;
IF EXISTS(SELECT 1 FROM #BarrelsBefore b JOIN maintenance.OilBarrels a ON a.Id=b.Id JOIN(SELECT OilBarrelId,SUM(QuantityLiters) Quantity FROM #BarrelAllocations GROUP BY OilBarrelId) q ON q.OilBarrelId=b.Id WHERE a.RemainingLiters<>b.RemainingLiters+q.Quantity OR a.Status<>b.Status)
 OR EXISTS(SELECT 1 FROM #LayersBefore b JOIN maintenance.StockCostLayers a ON a.Id=b.Id JOIN(SELECT StockCostLayerId,SUM(AllocatedQuantity) Quantity FROM #CostAllocations GROUP BY StockCostLayerId) q ON q.StockCostLayerId=b.Id WHERE a.RemainingQuantity<>b.RemainingQuantity+q.Quantity)
 OR EXISTS(SELECT 1 FROM #BalancesBefore b JOIN maintenance.StockBalances a ON a.Id=b.Id JOIN(SELECT InventoryItemId,InventoryLocationId,SUM(Quantity) Quantity FROM #Usages GROUP BY InventoryItemId,InventoryLocationId) q ON q.InventoryItemId=b.InventoryItemId AND q.InventoryLocationId=b.InventoryLocationId WHERE a.QuantityOnHand<>b.QuantityOnHand+q.Quantity)
 THROW 51000,'Inventory restoration verification failed.',1;
IF (SELECT COUNT(*) FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IN(SELECT Id FROM #Usages))<>10
 OR (SELECT COUNT(*) FROM maintenance.VehicleExpenses WHERE ReversalOfExpenseId IN(SELECT Id FROM #Expenses))<>10
 OR EXISTS(SELECT 1 FROM maintenance.OilChangeOperations WHERE Id IN(SELECT Id FROM #Operations))
 OR EXISTS(SELECT 1 FROM app.VehicleOdometerReadings WHERE SourceEntityId IN(SELECT Id FROM #Operations))
 THROW 51000,'Deletion or reversal verification failed.',1;
SELECT w.PlateNumberAr,w.VehicleId,COUNT(*) DeletedOilChanges,CONVERT(varchar(10),SWITCHOFFSET(MAX(d.PerformedAtUtc),'+03:00'),23) LatestRemainingOilDate,MAX(d.OdometerAtChange) LatestRemainingOilOdometer
FROM #Wanted w JOIN #Desired d ON d.VehicleId=w.VehicleId GROUP BY w.PlateNumberAr,w.VehicleId;
SELECT @Correlation CorrelationId,10 DeletedOilChanges,9 Vehicles,(SELECT SUM(Quantity) FROM #Usages) RestoredLiters,(SELECT SUM(TotalCost) FROM #Usages) ReversedExpenseSAR;
SELECT * FROM #Reversals;
