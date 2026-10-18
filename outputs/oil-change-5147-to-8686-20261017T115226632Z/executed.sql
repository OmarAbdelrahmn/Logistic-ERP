SET XACT_ABORT ON;
DECLARE @operation uniqueidentifier='01A12F8A-10F7-7B3D-9AB6-E0D71901184C',
 @old uniqueidentifier='01A0AED7-0D9A-77EC-B1A3-AEBD6EE2EFD9',
 @target uniqueidentifier='01A0AED7-0D93-7D1A-B7E2-B7C2A245CACB',
 @usage uniqueidentifier,@filter uniqueidentifier,@performed datetimeoffset,@reading bigint,
 @assignment uniqueidentifier,@rider uniqueidentifier,@plan uniqueidentifier,
 @reminder bigint,@maximum bigint,@now datetimeoffset=SYSUTCDATETIME(),
 @audit uniqueidentifier=NEWID();
IF (SELECT COUNT(*) FROM app.Vehicles WHERE PlateDigits='8686' AND IsDeleted=0)<>1
 THROW 51000,'Target plate is missing or ambiguous',1;
IF NOT EXISTS(SELECT 1 FROM app.Vehicles WHERE Id=@old AND PlateDigits='5147' AND IsDeleted=0)
 THROW 51000,'Original vehicle mismatch',1;
IF NOT EXISTS(SELECT 1 FROM maintenance.OilChangeOperations o JOIN app.Vehicles v ON v.Id=@target
 WHERE o.Id=@operation AND o.VehicleId=@old AND o.MaintenanceWorkOrderId IS NULL
 AND v.PlateDigits='8686' AND v.IsDeleted=0 AND v.VehicleType=o.VehicleTypeSnapshot)
 THROW 51000,'Oil change source or target mismatch',1;
SELECT @usage=OilMaterialUsageId,@filter=OilFilterMaterialUsageId,@performed=PerformedAtUtc,@reading=OdometerAtChange
FROM maintenance.OilChangeOperations WHERE Id=@operation;
IF (SELECT COUNT(*) FROM maintenance.MaterialUsages WHERE Id IN(@usage,@filter) AND VehicleId=@old AND ReversalOfUsageId IS NULL) <> CASE WHEN @filter IS NULL THEN 1 ELSE 2 END
 OR EXISTS(SELECT 1 FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IN(@usage,@filter))
 THROW 51000,'Material usage is missing, reversed or attributed elsewhere',1;
IF (SELECT COUNT(*) FROM app.VehicleOdometerReadings WHERE SourceType=6 AND SourceEntityId=@operation AND VehicleId=@old)<>1
 THROW 51000,'Expected one original maintenance odometer reading',1;
-- The service only applies mileage when the service reading is at least the current reading.
-- This correction moves an earlier reading, so both current vehicle mileages must stay intact.
IF EXISTS(SELECT 1 FROM app.Vehicles WHERE Id=@target AND CurrentOdometer<=@reading)
 THROW 51000,'Target mileage changed; re-evaluate mileage update before proceeding',1;
SELECT TOP(1) @assignment=Id,@rider=RiderProfileId FROM app.RiderVehicleAssignments
WHERE VehicleId=@target AND IsDeleted=0 AND StartedAtUtc<=@performed AND (EndedAtUtc IS NULL OR EndedAtUtc>=@performed)
ORDER BY StartedAtUtc DESC;
SELECT TOP(1) @plan=Id,@reminder=ReminderAfterKilometers,@maximum=MaximumAfterKilometers FROM maintenance.Plans
WHERE Status=1 AND TriggerType=3 AND VehicleType=2 AND IsDeleted=0 ORDER BY Code;
IF @plan IS NULL THROW 51000,'Oil maintenance plan missing',1;
DECLARE @expenses TABLE(Id uniqueidentifier PRIMARY KEY);
INSERT @expenses SELECT Id FROM maintenance.VehicleExpenses WHERE VehicleId=@old AND
 ((SourceEntityType='MaintenanceMaterialUsage' AND SourceEntityId IN(@usage,@filter)) OR (SourceEntityType='OilChangeOperation' AND SourceEntityId=@operation));
DECLARE @before nvarchar(max)=(SELECT
 JSON_QUERY((SELECT * FROM maintenance.OilChangeOperations WHERE Id=@operation FOR JSON PATH)) oilChange,
 JSON_QUERY((SELECT * FROM maintenance.MaterialUsages WHERE Id IN(@usage,@filter) FOR JSON PATH)) materialUsages,
 JSON_QUERY((SELECT * FROM app.VehicleOdometerReadings WHERE SourceType=6 AND SourceEntityId=@operation FOR JSON PATH)) odometerReadings,
 JSON_QUERY((SELECT * FROM maintenance.VehicleExpenses WHERE Id IN(SELECT Id FROM @expenses) OR ReversalOfExpenseId IN(SELECT Id FROM @expenses) FOR JSON PATH)) expenses,
 JSON_QUERY((SELECT * FROM maintenance.VehicleSchedules WHERE VehicleId IN(@old,@target) AND MaintenancePlanId=@plan FOR JSON PATH)) schedules,
 JSON_QUERY((SELECT Id,PlateNumberAr,PlateNumberEn,CurrentOdometer,LastOdometerAtUtc FROM app.Vehicles WHERE Id IN(@old,@target) FOR JSON PATH)) vehicles,
 JSON_QUERY((SELECT * FROM app.DatasetVersions WHERE ModuleKey IN('maintenance','fleet') FOR JSON PATH)) datasetVersions
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
UPDATE maintenance.OilChangeOperations SET VehicleId=@target WHERE Id=@operation AND VehicleId=@old;
IF @@ROWCOUNT<>1 THROW 51000,'Oil change update count mismatch',1;
UPDATE maintenance.MaterialUsages SET VehicleId=@target,RiderVehicleAssignmentId=@assignment,RiderProfileId=@rider,
 AttributionStatus=CASE WHEN @assignment IS NULL THEN 2 ELSE 1 END WHERE Id IN(@usage,@filter) AND VehicleId=@old;
IF @@ROWCOUNT<>CASE WHEN @filter IS NULL THEN 1 ELSE 2 END THROW 51000,'Usage update count mismatch',1;
UPDATE app.VehicleOdometerReadings SET VehicleId=@target WHERE SourceType=6 AND SourceEntityId=@operation AND VehicleId=@old;
IF @@ROWCOUNT<>1 THROW 51000,'Odometer update count mismatch',1;
UPDATE maintenance.VehicleExpenses SET VehicleId=@target,RiderVehicleAssignmentId=@assignment,RiderProfileId=@rider
WHERE VehicleId=@old AND (Id IN(SELECT Id FROM @expenses) OR ReversalOfExpenseId IN(SELECT Id FROM @expenses));
DECLARE @affectedExpenses int=@@ROWCOUNT;
DECLARE @desired TABLE(VehicleId uniqueidentifier,LastCompletedWorkOrderId uniqueidentifier NULL,LastCompletedAtUtc datetimeoffset NULL,LastCompletedOdometer bigint NULL,ReminderFromOdometer bigint NULL,MaximumDueOdometer bigint NULL,ComputedStatus int);
INSERT @desired SELECT v.Id,l.MaintenanceWorkOrderId,l.PerformedAtUtc,l.OdometerAtChange,
 l.OdometerAtChange+@reminder,l.OdometerAtChange+@maximum,CASE WHEN l.Id IS NULL THEN 4 ELSE 1 END
FROM app.Vehicles v OUTER APPLY(SELECT TOP(1) * FROM maintenance.OilChangeOperations o WHERE o.VehicleId=v.Id ORDER BY PerformedAtUtc DESC,OdometerAtChange DESC) l
WHERE v.Id IN(@old,@target);
UPDATE s SET LastCompletedWorkOrderId=d.LastCompletedWorkOrderId,LastCompletedAtUtc=d.LastCompletedAtUtc,
 LastCompletedOdometer=d.LastCompletedOdometer,ReminderFromOdometer=d.ReminderFromOdometer,MaximumDueOdometer=d.MaximumDueOdometer,
 ComputedStatus=d.ComputedStatus,ComputedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL
FROM maintenance.VehicleSchedules s JOIN @desired d ON d.VehicleId=s.VehicleId WHERE s.MaintenancePlanId=@plan AND s.IsDeleted=0;
INSERT maintenance.VehicleSchedules(Id,VehicleId,MaintenancePlanId,LastCompletedWorkOrderId,LastCompletedAtUtc,LastCompletedOdometer,ReminderFromOdometer,MaximumDueOdometer,ComputedStatus,ComputedAtUtc,CreatedAtUtc,IsDeleted)
SELECT NEWID(),d.VehicleId,@plan,d.LastCompletedWorkOrderId,d.LastCompletedAtUtc,d.LastCompletedOdometer,d.ReminderFromOdometer,d.MaximumDueOdometer,d.ComputedStatus,@now,@now,0
FROM @desired d WHERE d.LastCompletedAtUtc IS NOT NULL AND NOT EXISTS(SELECT 1 FROM maintenance.VehicleSchedules s WHERE s.VehicleId=d.VehicleId AND s.MaintenancePlanId=@plan AND s.IsDeleted=0);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey IN('maintenance','fleet');
INSERT app.DatasetVersions(Id,ModuleKey,Version,LastChangedAtUtc,CreatedAtUtc,IsDeleted)
SELECT NEWID(),m.ModuleKey,1,@now,@now,0 FROM (VALUES('maintenance'),('fleet'))m(ModuleKey) WHERE NOT EXISTS(SELECT 1 FROM app.DatasetVersions d WHERE d.ModuleKey=m.ModuleKey);
DECLARE @after nvarchar(max)=(SELECT
 JSON_QUERY((SELECT * FROM maintenance.OilChangeOperations WHERE Id=@operation FOR JSON PATH)) oilChange,
 JSON_QUERY((SELECT * FROM maintenance.MaterialUsages WHERE Id IN(@usage,@filter) FOR JSON PATH)) materialUsages,
 JSON_QUERY((SELECT * FROM app.VehicleOdometerReadings WHERE SourceType=6 AND SourceEntityId=@operation FOR JSON PATH)) odometerReadings,
 JSON_QUERY((SELECT * FROM maintenance.VehicleExpenses WHERE Id IN(SELECT Id FROM @expenses) OR ReversalOfExpenseId IN(SELECT Id FROM @expenses) FOR JSON PATH)) expenses,
 JSON_QUERY((SELECT * FROM maintenance.VehicleSchedules WHERE VehicleId IN(@old,@target) AND MaintenancePlanId=@plan FOR JSON PATH)) schedules,
 JSON_QUERY((SELECT Id,PlateNumberAr,PlateNumberEn,CurrentOdometer,LastOdometerAtUtc FROM app.Vehicles WHERE Id IN(@old,@target) FOR JSON PATH)) vehicles,
 JSON_QUERY((SELECT * FROM app.DatasetVersions WHERE ModuleKey IN('maintenance','fleet') FOR JSON PATH)) datasetVersions
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc)
VALUES(@audit,NEWID(),'System','Corrected','Maintenance','OilChangeOperation',@operation,@now,CONVERT(nvarchar(36),@audit),
 N'User requested in Codex to correct oil-change vehicle from S B N 5147 to A T T 8686. Direct database correction following MaintenanceOilChangeCorrectionService.',@before,@after,'Codex.DatabaseCorrection',1,@now);
IF EXISTS(SELECT 1 FROM maintenance.MaterialUsages WHERE Id IN(@usage,@filter) AND (VehicleId<>@target OR VehicleId IS NULL))
 OR EXISTS(SELECT 1 FROM app.VehicleOdometerReadings WHERE SourceType=6 AND SourceEntityId=@operation AND VehicleId<>@target)
 OR EXISTS(SELECT 1 FROM maintenance.VehicleExpenses WHERE Id IN(SELECT Id FROM @expenses) AND VehicleId<>@target)
 THROW 51000,'Post-update attribution verification failed',1;
SELECT @before BeforeJson,@after AfterJson,@audit AuditId,@affectedExpenses ExpensesUpdated,@assignment TargetAssignmentId,@rider TargetRiderId;
