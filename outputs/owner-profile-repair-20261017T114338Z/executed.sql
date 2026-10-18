SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
IF NOT EXISTS (SELECT 1 FROM app.PlatformRiderAccounts WITH (UPDLOCK,HOLDLOCK)
 WHERE Id=@AccountId AND RegisteredEmployeeId=@EmployeeId AND IsDeleted=0)
 THROW 51000,'Account ownership no longer matches the reviewed target.',1;
IF NOT EXISTS (SELECT 1 FROM app.Employees WITH (UPDLOCK,HOLDLOCK) WHERE Id=@EmployeeId AND IsDeleted=0)
 THROW 51001,'Registered owner is not available.',1;
DECLARE @EmployeeVersion binary(8)=(SELECT RowVersion FROM app.Employees WHERE Id=@EmployeeId);
DECLARE @AccountVersion binary(8)=(SELECT RowVersion FROM app.PlatformRiderAccounts WHERE Id=@AccountId);
DECLARE @ProfileId uniqueidentifier, @Deleted bit, @Created bit=0;
SELECT @ProfileId=Id,@Deleted=IsDeleted FROM app.RiderProfiles WITH (UPDLOCK,HOLDLOCK) WHERE EmployeeId=@EmployeeId;
IF @Deleted=1 THROW 51002,'An archived profile exists; automatic restoration is not authorized.',1;
IF @ProfileId IS NULL
BEGIN
 SET @ProfileId=NEWID();
 DECLARE @Now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
 INSERT app.RiderProfiles (Id,EmployeeId,CreatedAtUtc,IsDeleted)
 VALUES (@ProfileId,@EmployeeId,@Now,0);
 DECLARE @After nvarchar(max)=(SELECT Id,EmployeeId,CreatedAtUtc,IsDeleted FROM app.RiderProfiles WHERE Id=@ProfileId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 INSERT audit.AuditEntries (Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,
 OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES (NEWID(),NEWID(),NULL,'System','Created','Persistence','RiderProfile',@ProfileId,
 @Now,@CorrelationId,N'User authorized creating the missing registered-owner profile to resolve platform account assignment error 334f32f52c4bc150b6189d8bb187e1e6.',
 NULL,@After,'CodexAuthorizedMaintenance',1,@Now,NULL);
 SET @Created=1;
END;
IF (SELECT COUNT(*) FROM app.RiderProfiles WHERE EmployeeId=@EmployeeId AND IsDeleted=0)<>1
 THROW 51003,'Profile verification failed.',1;
IF NOT EXISTS (SELECT 1 FROM app.Employees WHERE Id=@EmployeeId AND RowVersion=@EmployeeVersion)
 OR NOT EXISTS (SELECT 1 FROM app.PlatformRiderAccounts WHERE Id=@AccountId AND RowVersion=@AccountVersion)
 THROW 51004,'Employee or account unexpectedly changed.',1;
SELECT @Created AS Created,@ProfileId AS RiderProfileId,@EmployeeId AS EmployeeId,@AccountId AS AccountId,
 @CorrelationId AS CorrelationId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
