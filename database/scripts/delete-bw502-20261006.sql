-- Explicitly authorized permanent deletion of fuel card BW502 and its direct links.
-- Backup: outputs/database-cleanup-bw502-20261006/before-delete.json.
-- @Commit=0 performs a transaction dry run; @Commit=1 commits.
-- Guarded against changes since inspection. Shared import history and rider records remain.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @card uniqueidentifier='01a10b3c-19d1-709b-9b10-20b779f5e71c';
DECLARE @assignment uniqueidentifier='01a10ca5-7a1c-7038-8cd5-b594bcc5e0a3';
DECLARE @rider uniqueidentifier='01a09667-03d5-7d3e-8a81-fe929033293f';
DECLARE @employee uniqueidentifier='01a09667-03d5-7b42-9a9f-2b64b16ba1fe';
DECLARE @deletedAssignments int=0,@deletedCards int=0,@deletedAuditRows int=0;
DECLARE @targets TABLE(Id uniqueidentifier PRIMARY KEY);
INSERT @targets VALUES(@card),(@assignment);
BEGIN TRY
 BEGIN TRANSACTION;
 IF DB_NAME()<>N'db67927' THROW 51600,'Unexpected database.',1;
 IF (SELECT COUNT(*) FROM app.FuelCards WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedCardNumber=N'BW502' OR CardNumber=N'BW502')<>1 THROW 51601,'BW502 card match changed.',1;
 IF NOT EXISTS(SELECT 1 FROM app.FuelCards WITH(UPDLOCK,HOLDLOCK) WHERE Id=@card AND CardNumber=N'BW502' AND NormalizedCardNumber=N'BW502' AND Provider=1 AND RowVersion=0x0000000000014AD5) THROW 51602,'Reviewed card changed.',1;
 IF (SELECT COUNT(*) FROM app.FuelCardRiderAssignments WITH(UPDLOCK,HOLDLOCK) WHERE FuelCardId=@card)<>1 THROW 51603,'Reviewed assignments changed.',1;
 IF NOT EXISTS(SELECT 1 FROM app.FuelCardRiderAssignments WITH(UPDLOCK,HOLDLOCK) WHERE Id=@assignment AND FuelCardId=@card AND RiderProfileId=@rider AND EmployeeId=@employee AND EffectiveTo IS NULL AND RowVersion=0x000000000001593D) THROW 51604,'Reviewed assignment changed.',1;
 IF EXISTS(SELECT 1 FROM app.FuelCardMonthlyUsages WITH(UPDLOCK,HOLDLOCK) WHERE FuelCardId=@card) THROW 51605,'New monthly usage exists; inspect before deleting.',1;
 IF (SELECT COUNT(*) FROM audit.AuditEntries WITH(UPDLOCK,HOLDLOCK) WHERE EntityId IN(SELECT Id FROM @targets))<>3 THROW 51606,'Direct audit entries changed.',1;
 IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE EntityId IN(SELECT Id FROM @targets) AND (CurrentHash IS NOT NULL OR PreviousHash IS NOT NULL)) THROW 51607,'Hash-linked audit entry requires separate handling.',1;
 IF EXISTS(SELECT 1 FROM app.FleetCommandReceipts WITH(UPDLOCK,HOLDLOCK) WHERE ResultEntityId IN(SELECT Id FROM @targets))
 OR EXISTS(SELECT 1 FROM app.Notifications WITH(UPDLOCK,HOLDLOCK) WHERE SourceEntityId IN(SELECT Id FROM @targets) OR DeepLink LIKE N'%01a10b3c-19d1-709b-9b10-20b779f5e71c%')
 OR EXISTS(SELECT 1 FROM app.VehicleOdometerReadings WHERE SourceEntityId IN(SELECT Id FROM @targets))
 OR EXISTS(SELECT 1 FROM app.VehicleOperationalStatusPeriods WHERE SourceEntityId IN(SELECT Id FROM @targets))
 OR EXISTS(SELECT 1 FROM maintenance.ExternalFinancialEntries WHERE SourceEntityId IN(SELECT Id FROM @targets))
 OR EXISTS(SELECT 1 FROM maintenance.VehicleExpenses WHERE SourceEntityId IN(SELECT Id FROM @targets))
 THROW 51608,'Unexpected additional references exist; inspect before deleting.',1;
 IF NOT EXISTS(SELECT 1 FROM app.RiderProfiles WHERE Id=@rider AND EmployeeId=@employee AND IsDeleted=0) THROW 51609,'Linked rider changed.',1;
 DELETE FROM audit.AuditEntries WHERE EntityId IN(SELECT Id FROM @targets);
 SET @deletedAuditRows=@@ROWCOUNT;
 DELETE FROM app.FuelCardRiderAssignments WHERE Id=@assignment AND FuelCardId=@card;
 SET @deletedAssignments=@@ROWCOUNT;
 DELETE FROM app.FuelCards WHERE Id=@card AND NormalizedCardNumber=N'BW502';
 SET @deletedCards=@@ROWCOUNT;
 IF @deletedCards<>1 OR @deletedAssignments<>1 OR @deletedAuditRows<>3 THROW 51610,'Unexpected deletion counts.',1;
 IF EXISTS(SELECT 1 FROM app.FuelCards WHERE Id=@card OR NormalizedCardNumber=N'BW502' OR CardNumber=N'BW502')
 OR EXISTS(SELECT 1 FROM app.FuelCardRiderAssignments WHERE FuelCardId=@card OR Id=@assignment)
 OR EXISTS(SELECT 1 FROM app.FuelCardMonthlyUsages WHERE FuelCardId=@card)
 OR EXISTS(SELECT 1 FROM audit.AuditEntries WHERE EntityId IN(SELECT Id FROM @targets))
 THROW 51611,'Deletion verification failed.',1;
 IF NOT EXISTS(SELECT 1 FROM app.RiderProfiles WHERE Id=@rider AND EmployeeId=@employee AND IsDeleted=0)
 OR NOT EXISTS(SELECT 1 FROM app.Employees WHERE Id=@employee AND IsDeleted=0)
 THROW 51612,'Rider preservation verification failed.',1;
 IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
 SELECT DB_NAME() AS DatabaseName,@Commit AS Committed,N'BW502' AS CardNumber,@card AS CardId,@deletedCards AS DeletedCards,@deletedAssignments AS DeletedRiderAssignments,0 AS DeletedMonthlyUsages,@deletedAuditRows AS DeletedDirectAuditEntries,1 AS RiderPreserved;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
