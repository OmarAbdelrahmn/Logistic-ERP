SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;

DECLARE @Actor uniqueidentifier=JSON_VALUE(@Plan,'$.actorId'),@Platform uniqueidentifier=JSON_VALUE(@Plan,'$.platformId'),
 @Batch nvarchar(100)=JSON_VALUE(@Plan,'$.batchId'),@Now datetimeoffset=SYSUTCDATETIME();


IF @@TRANCOUNT<>1 THROW 51000,'This correction requires one external transaction.',1;
DECLARE @LockResult int;
EXEC @LockResult=sys.sp_getapplock @Resource=N'ninja-workbook-reconciliation-20261025',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @LockResult<0 THROW 51000,'Could not acquire reconciliation lock.',1;
DECLARE @Resource nvarchar(255);
DECLARE accountLocks CURSOR LOCAL FAST_FORWARD FOR SELECT N'platform-account:'+value FROM OPENJSON(@LockOrder);
OPEN accountLocks; FETCH NEXT FROM accountLocks INTO @Resource;
WHILE @@FETCH_STATUS=0 BEGIN
 EXEC @LockResult=sys.sp_getapplock @Resource=@Resource,@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
 IF @LockResult<0 THROW 51000,'An account is being edited.',1;
 FETCH NEXT FROM accountLocks INTO @Resource;
END;
CLOSE accountLocks; DEALLOCATE accountLocks;
IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName=N'Omar' AND IsDeleted=0) THROW 51000,'Actor changed.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@Batch) THROW 51000,'Batch already applied; verify without reapplying.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code=N'NINJA' AND IsDeleted=0) THROW 51000,'Platform changed.',1;
SELECT * INTO #U FROM OPENJSON(@Plan,'$.updates') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex',JsonValues nvarchar(max) '$.values' AS JSON,AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #I FROM OPENJSON(@Plan,'$.inserts') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',JsonValues nvarchar(max) '$.values' AS JSON,AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #G FROM OPENJSON(@Plan,'$.guards') WITH(TableName nvarchar(150) '$.table',Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex');
SELECT * INTO #V FROM OPENJSON(@Plan,'$.expectedVersions') WITH(Id uniqueidentifier '$.id',RV varchar(16) '$.rowVersionHex',AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
SELECT * INTO #E FROM OPENJSON(@Plan,'$.assignmentEvents') WITH(Id uniqueidentifier '$.id',AssignmentId uniqueidentifier '$.assignmentId',FromStatus int '$.fromStatus',ToStatus int '$.toStatus',Snapshot nvarchar(max) '$.snapshot',AuditId uniqueidentifier '$.auditId',AuditEventId uniqueidentifier '$.auditEventId');
CREATE TABLE #Changes(TableName nvarchar(150),Id uniqueidentifier,AuditId uniqueidentifier,AuditEventId uniqueidentifier,BeforeJson nvarchar(max),AfterJson nvarchar(max));


SELECT * INTO #M FROM OPENJSON(@Plan,'$.mappings') WITH(
 accountId uniqueidentifier, captainId nvarchar(150),assignmentId uniqueidentifier,riderId uniqueidentifier,employeeId uniqueidentifier,
 fromDate date,toDate date,paymentModel int,cityId uniqueidentifier,phone nvarchar(32),nationality nvarchar(100),
 reference nvarchar(200),notes nvarchar(max));
SELECT * INTO #L FROM OPENJSON(@Plan,'$.latestAccounts') WITH(accountId uniqueidentifier,paymentModel int,cityId uniqueidentifier,status int);

SELECT * INTO #BeforeEmployees FROM [app].[Employees] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeEmployees b ON b.Id=g.Id WHERE g.TableName=N'app.Employees' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.Employees.',1;
SELECT * INTO #BeforeAccounts FROM [app].[PlatformRiderAccounts] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeAccounts b ON b.Id=g.Id WHERE g.TableName=N'app.PlatformRiderAccounts' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.PlatformRiderAccounts.',1;
SELECT * INTO #BeforeAssignments FROM [app].[RiderClientAssignments] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeAssignments b ON b.Id=g.Id WHERE g.TableName=N'app.RiderClientAssignments' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.RiderClientAssignments.',1;
SELECT * INTO #BeforeDetails FROM [ninja].[NinjaAccountDetails] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeDetails b ON b.Id=g.Id WHERE g.TableName=N'ninja.NinjaAccountDetails' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: ninja.NinjaAccountDetails.',1;
SELECT * INTO #BeforeUsages FROM [app].[PlatformAccountDayUsages] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeUsages b ON b.Id=g.Id WHERE g.TableName=N'app.PlatformAccountDayUsages' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.PlatformAccountDayUsages.',1;
SELECT * INTO #BeforeRegistrations FROM [app].[PlatformAccountRegistrations] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeRegistrations b ON b.Id=g.Id WHERE g.TableName=N'app.PlatformAccountRegistrations' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.PlatformAccountRegistrations.',1;
SELECT * INTO #BeforeRiders FROM [app].[RiderProfiles] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeRiders b ON b.Id=g.Id WHERE g.TableName=N'app.RiderProfiles' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.RiderProfiles.',1;
SELECT * INTO #BeforeEvents FROM [app].[RiderAssignmentEvents] WITH(TABLOCKX,HOLDLOCK);

SELECT * INTO #BeforeCredentials FROM [app].[PlatformAccountCredentialVersions] WITH(TABLOCKX,HOLDLOCK);

SELECT * INTO #BeforeVersions FROM [app].[DatasetVersions] WITH(TABLOCKX,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #G g LEFT JOIN #BeforeVersions b ON b.Id=g.Id WHERE g.TableName=N'app.DatasetVersions' AND (b.Id IS NULL OR b.RowVersion<>CONVERT(binary(8),g.RV,2))) THROW 51000,'Preflight row changed: app.DatasetVersions.',1;

IF EXISTS(SELECT 1 FROM #V v LEFT JOIN #BeforeVersions b ON b.Id=v.Id WHERE b.Id IS NULL OR b.IsDeleted=1 OR b.ModuleKey NOT IN(N'ninja',N'platform-operations',N'workforce')) THROW 51000,'Dataset version identity changed.',1;
-- Version counters may advance for unrelated live work. Increment the locked current value;
-- source entity rowversions in #G remain strict and are never refreshed here.
IF (SELECT COUNT(*) FROM #BeforeAssignments x JOIN #BeforeAccounts a ON a.Id=x.PlatformRiderAccountId WHERE a.ClientPlatformId=@Platform)<>227 THROW 51000,'Assignment set changed since preflight.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=0 AND parent_id IN(OBJECT_ID(N'app.Employees'),OBJECT_ID(N'app.PlatformRiderAccounts'),OBJECT_ID(N'app.RiderClientAssignments'),OBJECT_ID(N'ninja.NinjaAccountDetails'),OBJECT_ID(N'app.PlatformAccountDayUsages'),OBJECT_ID(N'app.RiderProfiles'),OBJECT_ID(N'app.PlatformAccountRegistrations'),OBJECT_ID(N'audit.AuditEntries'))) THROW 51000,'Unexpected write trigger.',1;

IF EXISTS(SELECT 1 FROM [chefz].[WalletSettlements] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
IF EXISTS(SELECT 1 FROM [jahez_v2].[AccountSessions] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
IF EXISTS(SELECT 1 FROM [chefz].[DailyPerformance] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
IF EXISTS(SELECT 1 FROM [chefz].[ImportedOrderRows] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
IF EXISTS(SELECT 1 FROM [ninja].[NinjaDailyDeductions] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
IF EXISTS(SELECT 1 FROM [ninja].[NinjaDailyPerformance] WHERE [RiderClientAssignmentId]='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate has an unexpected operational/financial reference.',1;
UPDATE t SET [IqamaNo]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'IqamaNo') THEN JSON_VALUE(p.JsonValues,'$.IqamaNo') ELSE t.[IqamaNo] END,[Nationality]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'Nationality') THEN JSON_VALUE(p.JsonValues,'$.Nationality') ELSE t.[Nationality] END,[PrimaryPhone]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'PrimaryPhone') THEN JSON_VALUE(p.JsonValues,'$.PrimaryPhone') ELSE t.[PrimaryPhone] END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM [app].[Employees] t JOIN #U p ON p.Id=t.Id AND p.TableName=N'app.Employees' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>152 THROW 51000,'Update count mismatch: app.Employees.',1;
UPDATE t SET [OperatingCityId]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'OperatingCityId') THEN TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.OperatingCityId')) ELSE t.[OperatingCityId] END,[PaymentModel]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'PaymentModel') THEN TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.PaymentModel')) ELSE t.[PaymentModel] END,[Status]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'Status') THEN TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.Status')) ELSE t.[Status] END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM [app].[PlatformRiderAccounts] t JOIN #U p ON p.Id=t.Id AND p.TableName=N'app.PlatformRiderAccounts' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>37 THROW 51000,'Update count mismatch: app.PlatformRiderAccounts.',1;
UPDATE t SET [DeletionReason]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'DeletionReason') THEN JSON_VALUE(p.JsonValues,'$.DeletionReason') ELSE t.[DeletionReason] END,[IsDeleted]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'IsDeleted') THEN CASE JSON_VALUE(p.JsonValues,'$.IsDeleted') WHEN 'true' THEN CAST(1 AS bit) WHEN 'false' THEN CAST(0 AS bit) ELSE TRY_CONVERT(bit,JSON_VALUE(p.JsonValues,'$.IsDeleted')) END ELSE t.[IsDeleted] END,[OperationalAgreementNotes]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'OperationalAgreementNotes') THEN JSON_VALUE(p.JsonValues,'$.OperationalAgreementNotes') ELSE t.[OperationalAgreementNotes] END,[OperationalAgreementReference]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'OperationalAgreementReference') THEN JSON_VALUE(p.JsonValues,'$.OperationalAgreementReference') ELSE t.[OperationalAgreementReference] END,[PaymentModel]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'PaymentModel') THEN TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.PaymentModel')) ELSE t.[PaymentModel] END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor,DeletedAtUtc=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Now ELSE t.DeletedAtUtc END,DeletedByUserId=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Actor ELSE t.DeletedByUserId END FROM [app].[RiderClientAssignments] t JOIN #U p ON p.Id=t.Id AND p.TableName=N'app.RiderClientAssignments' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>227 THROW 51000,'Update count mismatch: app.RiderClientAssignments.',1;
UPDATE t SET [PhoneNumber]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'PhoneNumber') THEN JSON_VALUE(p.JsonValues,'$.PhoneNumber') ELSE t.[PhoneNumber] END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM [ninja].[NinjaAccountDetails] t JOIN #U p ON p.Id=t.Id AND p.TableName=N'ninja.NinjaAccountDetails' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>10 THROW 51000,'Update count mismatch: ninja.NinjaAccountDetails.',1;
UPDATE t SET [DeletionReason]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'DeletionReason') THEN JSON_VALUE(p.JsonValues,'$.DeletionReason') ELSE t.[DeletionReason] END,[IsDeleted]=CASE WHEN EXISTS(SELECT 1 FROM OPENJSON(p.JsonValues) WHERE [key]=N'IsDeleted') THEN CASE JSON_VALUE(p.JsonValues,'$.IsDeleted') WHEN 'true' THEN CAST(1 AS bit) WHEN 'false' THEN CAST(0 AS bit) ELSE TRY_CONVERT(bit,JSON_VALUE(p.JsonValues,'$.IsDeleted')) END ELSE t.[IsDeleted] END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor,DeletedAtUtc=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Now ELSE t.DeletedAtUtc END,DeletedByUserId=CASE WHEN JSON_VALUE(p.JsonValues,'$.IsDeleted')='true' THEN @Actor ELSE t.DeletedByUserId END FROM [app].[PlatformAccountDayUsages] t JOIN #U p ON p.Id=t.Id AND p.TableName=N'app.PlatformAccountDayUsages' AND t.RowVersion=CONVERT(binary(8),p.RV,2); IF @@ROWCOUNT<>1 THROW 51000,'Update count mismatch: app.PlatformAccountDayUsages.',1;
INSERT [app].[RiderProfiles] ([Id],[EmployeeId],[TShirtSize],[OperationalNotes],CreatedAtUtc,CreatedByUserId,IsDeleted) SELECT TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.Id')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.EmployeeId')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.TShirtSize')),JSON_VALUE(p.JsonValues,'$.OperationalNotes'),@Now,@Actor,0 FROM #I p WHERE TableName=N'app.RiderProfiles'; IF @@ROWCOUNT<>2 THROW 51000,'Insert count mismatch: app.RiderProfiles.',1;
INSERT [app].[PlatformAccountRegistrations] ([Id],[RegisteredEmployeeId],[RiderProfileId],[ClientPlatformId],[ClientContractId],[SponsorId],[OperatingCityId],[RegistrationType],[Status],[StatusReason],[RequestedAtUtc],[ActivatedAtUtc],[PlatformRiderAccountId],[Notes],CreatedAtUtc,CreatedByUserId,IsDeleted) SELECT TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.Id')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.RegisteredEmployeeId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.RiderProfileId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.ClientPlatformId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.ClientContractId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.SponsorId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.OperatingCityId')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.RegistrationType')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.Status')),JSON_VALUE(p.JsonValues,'$.StatusReason'),TRY_CONVERT(datetimeoffset,JSON_VALUE(p.JsonValues,'$.RequestedAtUtc')),TRY_CONVERT(datetimeoffset,JSON_VALUE(p.JsonValues,'$.ActivatedAtUtc')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.PlatformRiderAccountId')),JSON_VALUE(p.JsonValues,'$.Notes'),@Now,@Actor,0 FROM #I p WHERE TableName=N'app.PlatformAccountRegistrations'; IF @@ROWCOUNT<>3 THROW 51000,'Insert count mismatch: app.PlatformAccountRegistrations.',1;
INSERT [app].[RiderClientAssignments] ([Id],[RiderProfileId],[ClientContractId],[PlatformRiderAccountId],[PaymentModel],[RiderAccountSlot],[NinjaRiderType],[EffectiveFrom],[EffectiveTo],[Status],[StartReason],[EndReason],[OperationalAgreementReference],[OperationalAgreementNotes],[AssignedByUserId],[EndedByUserId],[WasBackdated],[BackdatedReason],CreatedAtUtc,CreatedByUserId,IsDeleted) SELECT TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.Id')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.RiderProfileId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.ClientContractId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.PlatformRiderAccountId')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.PaymentModel')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.RiderAccountSlot')),JSON_VALUE(p.JsonValues,'$.NinjaRiderType'),TRY_CONVERT(date,JSON_VALUE(p.JsonValues,'$.EffectiveFrom')),TRY_CONVERT(date,JSON_VALUE(p.JsonValues,'$.EffectiveTo')),TRY_CONVERT(int,JSON_VALUE(p.JsonValues,'$.Status')),JSON_VALUE(p.JsonValues,'$.StartReason'),JSON_VALUE(p.JsonValues,'$.EndReason'),JSON_VALUE(p.JsonValues,'$.OperationalAgreementReference'),JSON_VALUE(p.JsonValues,'$.OperationalAgreementNotes'),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.AssignedByUserId')),TRY_CONVERT(uniqueidentifier,JSON_VALUE(p.JsonValues,'$.EndedByUserId')),CASE JSON_VALUE(p.JsonValues,'$.WasBackdated') WHEN 'true' THEN CAST(1 AS bit) WHEN 'false' THEN CAST(0 AS bit) ELSE TRY_CONVERT(bit,JSON_VALUE(p.JsonValues,'$.WasBackdated')) END,JSON_VALUE(p.JsonValues,'$.BackdatedReason'),@Now,@Actor,0 FROM #I p WHERE TableName=N'app.RiderClientAssignments'; IF @@ROWCOUNT<>3 THROW 51000,'Insert count mismatch: app.RiderClientAssignments.',1;

INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
SELECT Id,AssignmentId,FromStatus,ToStatus,@Now,@Actor,N'User-approved Ninja workbook reconciliation; original source labels and corrections retained.',Snapshot,@Now,@Actor FROM #E;
UPDATE t SET Version=t.Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.DatasetVersions t JOIN #V v ON v.Id=t.Id JOIN #BeforeVersions b ON b.Id=t.Id AND b.RowVersion=t.RowVersion;
IF @@ROWCOUNT<>3 THROW 51000,'Dataset version update mismatch.',1;


IF EXISTS(SELECT b.* FROM #BeforeEmployees b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.Employees' ) EXCEPT SELECT * FROM [app].[Employees])
 OR EXISTS(SELECT t.* FROM [app].[Employees] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.Employees' UNION SELECT Id FROM #I WHERE TableName=N'app.Employees' ) EXCEPT SELECT * FROM #BeforeEmployees)
 THROW 51000,'Unrelated rows changed: app.Employees.',1;
IF EXISTS(SELECT Id FROM #BeforeEmployees EXCEPT SELECT Id FROM [app].[Employees]) THROW 51000,'A row was physically removed: app.Employees.',1;

INSERT #Changes SELECT N'app.Employees',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeEmployees b ON b.Id=p.Id JOIN [app].[Employees] t ON t.Id=p.Id WHERE p.TableName=N'app.Employees';
INSERT #Changes SELECT N'app.Employees',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[Employees] t ON t.Id=p.Id WHERE p.TableName=N'app.Employees';

IF EXISTS(SELECT b.* FROM #BeforeAccounts b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformRiderAccounts' ) EXCEPT SELECT * FROM [app].[PlatformRiderAccounts])
 OR EXISTS(SELECT t.* FROM [app].[PlatformRiderAccounts] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformRiderAccounts' UNION SELECT Id FROM #I WHERE TableName=N'app.PlatformRiderAccounts' ) EXCEPT SELECT * FROM #BeforeAccounts)
 THROW 51000,'Unrelated rows changed: app.PlatformRiderAccounts.',1;
IF EXISTS(SELECT Id FROM #BeforeAccounts EXCEPT SELECT Id FROM [app].[PlatformRiderAccounts]) THROW 51000,'A row was physically removed: app.PlatformRiderAccounts.',1;

INSERT #Changes SELECT N'app.PlatformRiderAccounts',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeAccounts b ON b.Id=p.Id JOIN [app].[PlatformRiderAccounts] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformRiderAccounts';
INSERT #Changes SELECT N'app.PlatformRiderAccounts',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[PlatformRiderAccounts] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformRiderAccounts';

IF EXISTS(SELECT b.* FROM #BeforeAssignments b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderClientAssignments' ) EXCEPT SELECT * FROM [app].[RiderClientAssignments])
 OR EXISTS(SELECT t.* FROM [app].[RiderClientAssignments] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderClientAssignments' UNION SELECT Id FROM #I WHERE TableName=N'app.RiderClientAssignments' ) EXCEPT SELECT * FROM #BeforeAssignments)
 THROW 51000,'Unrelated rows changed: app.RiderClientAssignments.',1;
IF EXISTS(SELECT Id FROM #BeforeAssignments EXCEPT SELECT Id FROM [app].[RiderClientAssignments]) THROW 51000,'A row was physically removed: app.RiderClientAssignments.',1;

INSERT #Changes SELECT N'app.RiderClientAssignments',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeAssignments b ON b.Id=p.Id JOIN [app].[RiderClientAssignments] t ON t.Id=p.Id WHERE p.TableName=N'app.RiderClientAssignments';
INSERT #Changes SELECT N'app.RiderClientAssignments',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[RiderClientAssignments] t ON t.Id=p.Id WHERE p.TableName=N'app.RiderClientAssignments';

IF EXISTS(SELECT b.* FROM #BeforeDetails b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'ninja.NinjaAccountDetails' ) EXCEPT SELECT * FROM [ninja].[NinjaAccountDetails])
 OR EXISTS(SELECT t.* FROM [ninja].[NinjaAccountDetails] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'ninja.NinjaAccountDetails' UNION SELECT Id FROM #I WHERE TableName=N'ninja.NinjaAccountDetails' ) EXCEPT SELECT * FROM #BeforeDetails)
 THROW 51000,'Unrelated rows changed: ninja.NinjaAccountDetails.',1;
IF EXISTS(SELECT Id FROM #BeforeDetails EXCEPT SELECT Id FROM [ninja].[NinjaAccountDetails]) THROW 51000,'A row was physically removed: ninja.NinjaAccountDetails.',1;

INSERT #Changes SELECT N'ninja.NinjaAccountDetails',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeDetails b ON b.Id=p.Id JOIN [ninja].[NinjaAccountDetails] t ON t.Id=p.Id WHERE p.TableName=N'ninja.NinjaAccountDetails';
INSERT #Changes SELECT N'ninja.NinjaAccountDetails',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [ninja].[NinjaAccountDetails] t ON t.Id=p.Id WHERE p.TableName=N'ninja.NinjaAccountDetails';

IF EXISTS(SELECT b.* FROM #BeforeUsages b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformAccountDayUsages' ) EXCEPT SELECT * FROM [app].[PlatformAccountDayUsages])
 OR EXISTS(SELECT t.* FROM [app].[PlatformAccountDayUsages] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformAccountDayUsages' UNION SELECT Id FROM #I WHERE TableName=N'app.PlatformAccountDayUsages' ) EXCEPT SELECT * FROM #BeforeUsages)
 THROW 51000,'Unrelated rows changed: app.PlatformAccountDayUsages.',1;
IF EXISTS(SELECT Id FROM #BeforeUsages EXCEPT SELECT Id FROM [app].[PlatformAccountDayUsages]) THROW 51000,'A row was physically removed: app.PlatformAccountDayUsages.',1;

INSERT #Changes SELECT N'app.PlatformAccountDayUsages',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeUsages b ON b.Id=p.Id JOIN [app].[PlatformAccountDayUsages] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformAccountDayUsages';
INSERT #Changes SELECT N'app.PlatformAccountDayUsages',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[PlatformAccountDayUsages] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformAccountDayUsages';

IF EXISTS(SELECT b.* FROM #BeforeRegistrations b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformAccountRegistrations' ) EXCEPT SELECT * FROM [app].[PlatformAccountRegistrations])
 OR EXISTS(SELECT t.* FROM [app].[PlatformAccountRegistrations] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.PlatformAccountRegistrations' UNION SELECT Id FROM #I WHERE TableName=N'app.PlatformAccountRegistrations' ) EXCEPT SELECT * FROM #BeforeRegistrations)
 THROW 51000,'Unrelated rows changed: app.PlatformAccountRegistrations.',1;
IF EXISTS(SELECT Id FROM #BeforeRegistrations EXCEPT SELECT Id FROM [app].[PlatformAccountRegistrations]) THROW 51000,'A row was physically removed: app.PlatformAccountRegistrations.',1;

INSERT #Changes SELECT N'app.PlatformAccountRegistrations',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeRegistrations b ON b.Id=p.Id JOIN [app].[PlatformAccountRegistrations] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformAccountRegistrations';
INSERT #Changes SELECT N'app.PlatformAccountRegistrations',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[PlatformAccountRegistrations] t ON t.Id=p.Id WHERE p.TableName=N'app.PlatformAccountRegistrations';

IF EXISTS(SELECT b.* FROM #BeforeRiders b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderProfiles' ) EXCEPT SELECT * FROM [app].[RiderProfiles])
 OR EXISTS(SELECT t.* FROM [app].[RiderProfiles] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderProfiles' UNION SELECT Id FROM #I WHERE TableName=N'app.RiderProfiles' ) EXCEPT SELECT * FROM #BeforeRiders)
 THROW 51000,'Unrelated rows changed: app.RiderProfiles.',1;
IF EXISTS(SELECT Id FROM #BeforeRiders EXCEPT SELECT Id FROM [app].[RiderProfiles]) THROW 51000,'A row was physically removed: app.RiderProfiles.',1;

INSERT #Changes SELECT N'app.RiderProfiles',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #U p JOIN #BeforeRiders b ON b.Id=p.Id JOIN [app].[RiderProfiles] t ON t.Id=p.Id WHERE p.TableName=N'app.RiderProfiles';
INSERT #Changes SELECT N'app.RiderProfiles',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #I p JOIN [app].[RiderProfiles] t ON t.Id=p.Id WHERE p.TableName=N'app.RiderProfiles';

IF EXISTS(SELECT b.* FROM #BeforeEvents b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderAssignmentEvents' UNION SELECT Id FROM #E) EXCEPT SELECT * FROM [app].[RiderAssignmentEvents])
 OR EXISTS(SELECT t.* FROM [app].[RiderAssignmentEvents] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.RiderAssignmentEvents' UNION SELECT Id FROM #I WHERE TableName=N'app.RiderAssignmentEvents' UNION SELECT Id FROM #E) EXCEPT SELECT * FROM #BeforeEvents)
 THROW 51000,'Unrelated rows changed: app.RiderAssignmentEvents.',1;
IF EXISTS(SELECT Id FROM #BeforeEvents EXCEPT SELECT Id FROM [app].[RiderAssignmentEvents]) THROW 51000,'A row was physically removed: app.RiderAssignmentEvents.',1;

INSERT #Changes SELECT N'app.RiderAssignmentEvents',p.Id,p.AuditId,p.AuditEventId,NULL,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #E p JOIN app.RiderAssignmentEvents t ON t.Id=p.Id;
IF EXISTS(SELECT * FROM #BeforeCredentials EXCEPT SELECT * FROM [app].[PlatformAccountCredentialVersions]) OR EXISTS(SELECT * FROM [app].[PlatformAccountCredentialVersions] EXCEPT SELECT * FROM #BeforeCredentials) THROW 51000,'Credentials changed.',1;

IF EXISTS(SELECT b.* FROM #BeforeVersions b WHERE b.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.DatasetVersions' UNION SELECT Id FROM #V) EXCEPT SELECT * FROM [app].[DatasetVersions])
 OR EXISTS(SELECT t.* FROM [app].[DatasetVersions] t WHERE t.Id NOT IN(SELECT Id FROM #U WHERE TableName=N'app.DatasetVersions' UNION SELECT Id FROM #I WHERE TableName=N'app.DatasetVersions' UNION SELECT Id FROM #V) EXCEPT SELECT * FROM #BeforeVersions)
 THROW 51000,'Unrelated rows changed: app.DatasetVersions.',1;
IF EXISTS(SELECT Id FROM #BeforeVersions EXCEPT SELECT Id FROM [app].[DatasetVersions]) THROW 51000,'A row was physically removed: app.DatasetVersions.',1;

INSERT #Changes SELECT N'app.DatasetVersions',p.Id,p.AuditId,p.AuditEventId,(SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #V p JOIN #BeforeVersions b ON b.Id=p.Id JOIN app.DatasetVersions t ON t.Id=p.Id;

IF (SELECT COUNT(*) FROM #M)<>229 OR (SELECT COUNT(*) FROM #M WHERE toDate IS NULL)<>151 THROW 51000,'Invalid source mapping.',1;
IF (SELECT COUNT(*) FROM #L)<>207 OR EXISTS(SELECT 1 FROM #L l JOIN app.PlatformRiderAccounts a ON a.Id=l.accountId
 WHERE a.IsDeleted=1 OR a.PaymentModel<>l.paymentModel OR a.OperatingCityId<>l.cityId OR a.Status<>l.status)
 THROW 51000,'Latest account payment/city/status does not match the source.',1;
IF EXISTS(SELECT 1 FROM #M m LEFT JOIN app.RiderClientAssignments x ON x.Id=m.assignmentId
 WHERE x.Id IS NULL OR x.IsDeleted=1 OR x.PlatformRiderAccountId<>m.accountId OR x.RiderProfileId<>m.riderId
 OR x.EffectiveFrom<>m.fromDate OR ISNULL(x.EffectiveTo,'99991231')<>ISNULL(m.toDate,'99991231')
 OR x.PaymentModel<>m.paymentModel OR x.Status<>CASE WHEN m.toDate IS NULL THEN 2 ELSE 3 END
 OR ISNULL(x.OperationalAgreementReference,'')<>m.reference OR ISNULL(x.OperationalAgreementNotes,'')<>m.notes)
 THROW 51000,'Assignment/source reconciliation failed.',1;
IF EXISTS(SELECT 1 FROM #M m JOIN app.PlatformRiderAccounts a ON a.Id=m.accountId
 LEFT JOIN ninja.NinjaAccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0
 JOIN app.Employees e ON e.Id=m.employeeId WHERE m.toDate IS NULL AND
 (a.IsDeleted=1 OR a.Status<>2 OR a.PaymentModel<>m.paymentModel OR a.OperatingCityId<>m.cityId
 OR ISNULL(d.PhoneNumber,'')<>m.phone OR ISNULL(e.PrimaryPhone,'')<>m.phone OR ISNULL(e.Nationality,'')<>m.nationality))
 THROW 51000,'Current account/contact/city reconciliation failed.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND x.IsDeleted=0)<>229 THROW 51000,'Visible assignment count is not 229.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND x.IsDeleted=0 AND x.EffectiveTo IS NULL)<>151 THROW 51000,'Current assignment count is not 151.',1;
IF EXISTS(SELECT x.RiderProfileId FROM app.RiderClientAssignments x WHERE x.EffectiveTo IS NULL AND x.IsDeleted=0
 AND x.RiderProfileId IN(SELECT riderId FROM #M WHERE toDate IS NULL) GROUP BY x.RiderProfileId HAVING COUNT(*)>1)
 THROW 51000,'A source current rider has another active account.',1;
IF NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'255527' AND e.IqamaNo='2629274197')
 THROW 51000,'Owner identity correction is missing.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'286371' AND x.EffectiveTo IS NULL AND x.IsDeleted=0)
 THROW 51000,'Historical-only account has a current assignment.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'215459' AND x.EffectiveFrom='20261006'
 AND x.EffectiveTo='20261006' AND x.IsDeleted=0)<>1 THROW 51000,'Duplicate historical assignment remains.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountDayUsages u JOIN app.RiderClientAssignments x ON x.Id=u.RiderClientAssignmentId
 WHERE u.IsDeleted=0 AND x.Id='01a16df8-6809-71c6-b939-d53d664fe930') THROW 51000,'Duplicate daily allocation remains active.',1;
IF NOT EXISTS(SELECT 1 FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
 WHERE a.ClientPlatformId=@Platform AND a.ExternalAccountId=N'267570' AND x.EffectiveFrom='20261023' AND x.EffectiveTo IS NULL AND x.IsDeleted=0)
 THROW 51000,'Confirmed current start date is missing.',1;
IF EXISTS(SELECT 1 FROM #M m WHERE m.toDate IS NULL AND NOT EXISTS(SELECT 1 FROM app.PlatformAccountRegistrations r
 WHERE r.PlatformRiderAccountId=m.accountId AND r.IsDeleted=0 AND r.Status=4)) THROW 51000,'Current registration missing.',1;


INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT AuditId,AuditEventId,@Actor,N'User',CASE WHEN BeforeJson IS NULL THEN N'Created' ELSE N'WorkbookReconciled' END,N'Ninja',TableName,Id,@Now,@Batch,
 N'Explicit user instructions: match worksheet payment, nationality, phone and work city; apply confirmed placements and duplicate consolidation.',BeforeJson,AfterJson,N'NinjaWorkbookSync20261025',1,@Now,@Actor FROM #Changes;
IF @@ROWCOUNT<>(SELECT COUNT(*) FROM #Changes) THROW 51000,'Audit count mismatch.',1;

SELECT * FROM #BeforeEmployees;
SELECT * FROM #BeforeAccounts;
SELECT * FROM #BeforeAssignments;
SELECT * FROM #BeforeDetails;
SELECT * FROM #BeforeUsages;
SELECT * FROM #BeforeRegistrations;
SELECT * FROM #BeforeRiders;
SELECT * FROM #BeforeEvents;
SELECT * FROM #BeforeCredentials;
SELECT * FROM #BeforeVersions;
SELECT @Batch BatchId,@Now VerifiedAtUtc,(SELECT COUNT(*) FROM #Changes) AuditedChanges,229 MatchedSourceRows,151 CurrentAssignments,78 HistoricalAssignments,CAST(1 AS bit) CredentialsPreserved,CAST(1 AS bit) UnrelatedRowsPreserved;