SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * INTO #BeforeAccounts FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeAssignments FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeRegistrations FROM app.PlatformAccountRegistrations WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeEvents FROM app.RiderAssignmentEvents WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeUsages FROM app.PlatformAccountDayUsages WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeEmployees FROM app.Employees WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM #BeforeAccounts WHERE ClientPlatformId=@platform;
SELECT * FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform) OR ClientContractId IN(SELECT Id FROM app.ClientContracts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeRegistrations WHERE ClientPlatformId=@platform OR PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeEvents WHERE RiderClientAssignmentId IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform));
SELECT * FROM #BeforeUsages WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeEmployees WHERE Id IN(SELECT OwnerId FROM OPENJSON(@PlanJson,'$.rows') WITH(OwnerId uniqueidentifier '$.ownerId'));
SELECT * FROM ninja.NinjaAccountDetails WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.VehiclePlatformAccountAssignments WITH(UPDLOCK,HOLDLOCK) WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM app.VehiclePlatformAccountSwitches WITH(UPDLOCK,HOLDLOCK) WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('ninja','platform-operations','employees','workforce');
SELECT * FROM platform.GlobalCities WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.OperatingCities WITH(UPDLOCK,HOLDLOCK);
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @p uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),@actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'),@now datetimeoffset=SYSUTCDATETIME();
IF JSON_VALUE(@PlanJson,'$.ready')<>'true' THROW 51000,'Corrected owner ID is still required.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@p AND Code='NINJA' AND IsDeleted=0) THROW 51000,'Unexpected Ninja platform.',1;
SELECT * INTO #Source FROM OPENJSON(@PlanJson,'$.rows') WITH(Id uniqueidentifier '$.id',DetailsId uniqueidentifier '$.detailsId',CaptainId nvarchar(150) '$.captainId',OwnerId uniqueidentifier '$.ownerId',Iqama nvarchar(10) '$.iqama',OwnerName nvarchar(200) '$.name',CityId uniqueidentifier '$.cityId',SponsorId uniqueidentifier '$.sponsorId',IsNew bit '$.isNew',NewOwner bit '$.newOwner',Metadata nvarchar(4000) '$.metadata');
IF (SELECT COUNT(*) FROM #Source)<>327 OR EXISTS(SELECT CaptainId FROM #Source GROUP BY CaptainId HAVING COUNT(*)>1) OR EXISTS(SELECT 1 FROM #Source WHERE LEN(Iqama)<>10 OR Iqama LIKE '%[^0-9]%' OR LEFT(Iqama,1) NOT IN('1','2')) THROW 51000,'Invalid source plan.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.Employees e ON e.Id=s.OwnerId WHERE s.NewOwner=0 AND (e.Id IS NULL OR e.IsDeleted=1 OR e.IqamaNo<>s.Iqama)) THROW 51000,'Owner references changed.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.Id WHERE s.IsNew=0 AND (a.Id IS NULL OR a.ClientPlatformId<>@p OR a.ExternalAccountId<>s.CaptainId OR a.IsDeleted=1)) THROW 51000,'Existing Ninja accounts changed.',1;
IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p AND IsDeleted=0)<>301 THROW 51000,'Unexpected Ninja account count; refresh plan.',1;
IF EXISTS(SELECT 1 FROM ninja.NinjaDailyPerformance) OR EXISTS(SELECT 1 FROM ninja.NinjaDailyDeductions) OR EXISTS(SELECT 1 FROM ninja.NinjaDailyPerformanceSources) OR EXISTS(SELECT 1 FROM ninja.NinjaDailyDeductionSources) OR EXISTS(SELECT 1 FROM ninja.NinjaDailyImports) THROW 51000,'New Ninja daily records require review.',1;
SELECT Id INTO #NinjaAccounts FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p;
SELECT r.Id INTO #NinjaAssignments FROM app.RiderClientAssignments r WHERE r.PlatformRiderAccountId IN(SELECT Id FROM #NinjaAccounts) OR r.ClientContractId IN(SELECT Id FROM app.ClientContracts WHERE ClientPlatformId=@p);
-- The runner saves these exact rows before this batch. Cross-platform rows are checked after.
DELETE u FROM app.PlatformAccountDayUsages u WHERE u.RiderClientAssignmentId IN(SELECT Id FROM #NinjaAssignments) OR u.PlatformRiderAccountId IN(SELECT Id FROM #NinjaAccounts);
DELETE e FROM app.RiderAssignmentEvents e WHERE e.RiderClientAssignmentId IN(SELECT Id FROM #NinjaAssignments);
DELETE FROM app.RiderClientAssignments WHERE Id IN(SELECT Id FROM #NinjaAssignments);
DELETE FROM app.PlatformAccountRegistrations WHERE ClientPlatformId=@p OR PlatformRiderAccountId IN(SELECT Id FROM #NinjaAccounts);
DELETE FROM app.VehiclePlatformAccountSwitches WHERE PlatformRiderAccountId IN(SELECT Id FROM #NinjaAccounts);
DELETE FROM app.VehiclePlatformAccountAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #NinjaAccounts);
-- Disable filtered ownership uniqueness only for the duration of the row updates by
-- placing the selected Ninja rows outside its active-status predicate first.
UPDATE app.PlatformRiderAccounts SET Status=3 WHERE ClientPlatformId=@p AND IsDeleted=0;
UPDATE a SET IsDeleted=1,DeletedAtUtc=@now,DeletedByUserId=@actor,DeletionReason=N'Removed from Ninja account list to match authorized workbook.',Status=5,UpdatedAtUtc=@now,UpdatedByUserId=@actor FROM app.PlatformRiderAccounts a WHERE a.ClientPlatformId=@p AND a.IsDeleted=0 AND NOT EXISTS(SELECT 1 FROM #Source s WHERE s.Id=a.Id);
INSERT platform.GlobalCities(Id,Code,NameAr,NameEn,RegionAr,RegionEn,CountryCode,DisplayOrder,Status,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT GlobalId,Code,NameAr,NameEn,RegionAr,RegionEn,'SA',10+ROW_NUMBER() OVER(ORDER BY Code),1,@now,@actor,0
FROM OPENJSON(@PlanJson,'$.cities') WITH(GlobalId uniqueidentifier '$.globalId',Code nvarchar(50) '$.code',NameAr nvarchar(200) '$.nameAr',NameEn nvarchar(200) '$.nameEn',RegionAr nvarchar(200) '$.regionAr',RegionEn nvarchar(200) '$.regionEn');
INSERT app.OperatingCities(Id,GlobalCityId,EnabledFrom,Status,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT Id,GlobalId,CONVERT(date,SWITCHOFFSET(@now,'+03:00')),1,@now,@actor,0 FROM OPENJSON(@PlanJson,'$.cities') WITH(Id uniqueidentifier '$.id',GlobalId uniqueidentifier '$.globalId');
INSERT app.Employees(Id,IqamaNo,FullNameAr,FullNameEn,Status,IsEmployee,EngagementType,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT DISTINCT OwnerId,Iqama,OwnerName,OwnerName,1,0,2,@now,@actor,0 FROM #Source WHERE NewOwner=1;
INSERT app.RiderProfiles(Id,EmployeeId,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT NEWID(),e.OwnerId,@now,@actor,0 FROM (SELECT DISTINCT OwnerId FROM #Source WHERE NewOwner=1) e;
UPDATE e SET FullNameEn=s.OwnerName,UpdatedAtUtc=@now,UpdatedByUserId=@actor FROM app.Employees e JOIN #Source s ON s.OwnerId=e.Id WHERE ISNULL(e.FullNameEn,'') COLLATE Latin1_General_100_BIN2<>s.OwnerName COLLATE Latin1_General_100_BIN2;
UPDATE a SET RegisteredEmployeeId=s.OwnerId,OperatingCityId=s.CityId,SponsorId=s.SponsorId,Status=1,StatusReason=NULL,OperationalNotes=s.Metadata,UpdatedAtUtc=@now,UpdatedByUserId=@actor FROM app.PlatformRiderAccounts a JOIN #Source s ON s.Id=a.Id WHERE s.IsNew=0;
INSERT app.PlatformRiderAccounts(Id,ClientPlatformId,RegisteredEmployeeId,OperatingCityId,SponsorId,Code,ExternalAccountId,PaymentModel,Status,OperationalNotes,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT Id,@p,OwnerId,CityId,SponsorId,CONCAT('NINJA-',CaptainId),CaptainId,1,1,Metadata,@now,@actor,0 FROM #Source WHERE IsNew=1;
INSERT ninja.NinjaAccountDetails(Id,PlatformRiderAccountId,CreatedAtUtc,CreatedByUserId,IsDeleted)
SELECT DetailsId,Id,@now,@actor,0 FROM #Source s WHERE NOT EXISTS(SELECT 1 FROM ninja.NinjaAccountDetails d WHERE d.PlatformRiderAccountId=s.Id);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=@actor WHERE ModuleKey IN('ninja','platform-operations','employees','workforce');
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@actor,'User','NinjaWorkbookSyncAndAssignmentReset','ninja','ClientPlatform',@p,@now,JSON_VALUE(@PlanJson,'$.correlationId'),N'User authorized clearing all Ninja assignments and history and synchronizing 327 spreadsheet accounts; Captain 66689 excluded at user request. All accounts Available; provider flags retained in OperationalNotes.',@PlanJson,'CodexAuthorizedMaintenance',1,@now,@actor);

DECLARE @platform2 uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
IF EXISTS(SELECT * FROM #BeforeAccounts WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM app.PlatformRiderAccounts WHERE ClientPlatformId<>@platform2) OR EXISTS(SELECT * FROM app.PlatformRiderAccounts WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM #BeforeAccounts WHERE ClientPlatformId<>@platform2) THROW 51000,'Other platform accounts changed.',1;
IF EXISTS(SELECT * FROM #BeforeAssignments WHERE PlatformRiderAccountId NOT IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2) EXCEPT SELECT * FROM app.RiderClientAssignments) OR EXISTS(SELECT * FROM app.RiderClientAssignments EXCEPT SELECT * FROM #BeforeAssignments) THROW 51000,'Other platform assignments changed.',1;
IF EXISTS(SELECT * FROM #BeforeRegistrations WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM app.PlatformAccountRegistrations) OR EXISTS(SELECT * FROM app.PlatformAccountRegistrations EXCEPT SELECT * FROM #BeforeRegistrations) THROW 51000,'Other platform registrations changed.',1;
IF EXISTS(SELECT * FROM #BeforeEvents WHERE RiderClientAssignmentId NOT IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2)) EXCEPT SELECT * FROM app.RiderAssignmentEvents) OR EXISTS(SELECT * FROM app.RiderAssignmentEvents EXCEPT SELECT * FROM #BeforeEvents) THROW 51000,'Other platform events changed.',1;
IF EXISTS(SELECT * FROM #BeforeUsages WHERE PlatformRiderAccountId NOT IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2) EXCEPT SELECT * FROM app.PlatformAccountDayUsages) OR EXISTS(SELECT * FROM app.PlatformAccountDayUsages EXCEPT SELECT * FROM #BeforeUsages) THROW 51000,'Other platform day allocations changed.',1;
IF EXISTS(SELECT * FROM #BeforeEmployees WHERE Id NOT IN(SELECT OwnerId FROM OPENJSON(@PlanJson,'$.rows') WITH(OwnerId uniqueidentifier '$.ownerId')) EXCEPT SELECT * FROM app.Employees) THROW 51000,'Unrelated employees changed.',1;
IF EXISTS(SELECT 1 FROM app.RiderAssignmentEvents WHERE RiderClientAssignmentId IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2))) THROW 51000,'Ninja assignment history remains.',1;
DECLARE @verifyPlatform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
WITH s AS(SELECT * FROM OPENJSON(@PlanJson,'$.rows') WITH(Id uniqueidentifier '$.id',CaptainId nvarchar(150) '$.captainId',OwnerId uniqueidentifier '$.ownerId',Iqama nvarchar(10) '$.iqama',OwnerName nvarchar(200) '$.name',CityId uniqueidentifier '$.cityId',SponsorId uniqueidentifier '$.sponsorId',Metadata nvarchar(4000) '$.metadata'))
SELECT 'Source field mismatches' CheckName,COUNT_BIG(*) Mismatches FROM s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.Id LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.ClientPlatformId<>@verifyPlatform OR a.ExternalAccountId<>s.CaptainId OR a.RegisteredEmployeeId<>s.OwnerId OR a.OperatingCityId<>s.CityId OR a.SponsorId<>s.SponsorId OR a.Status<>1 OR e.IqamaNo<>s.Iqama OR ISNULL(e.FullNameEn,'') COLLATE Latin1_General_100_BIN2<>s.OwnerName COLLATE Latin1_General_100_BIN2 OR ISNULL(a.OperationalNotes,'') COLLATE Latin1_General_100_BIN2<>s.Metadata COLLATE Latin1_General_100_BIN2
UNION ALL SELECT 'Account count',ABS(327-COUNT_BIG(*)) FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@verifyPlatform AND IsDeleted=0
UNION ALL SELECT 'Remaining rider assignments',COUNT_BIG(*) FROM app.RiderClientAssignments r WHERE r.PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@verifyPlatform) OR r.ClientContractId IN(SELECT Id FROM app.ClientContracts WHERE ClientPlatformId=@verifyPlatform)
UNION ALL SELECT 'Remaining registrations',COUNT_BIG(*) FROM app.PlatformAccountRegistrations WHERE ClientPlatformId=@verifyPlatform
UNION ALL SELECT 'Remaining day allocations',COUNT_BIG(*) FROM app.PlatformAccountDayUsages WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@verifyPlatform)
UNION ALL SELECT 'Remaining vehicle assignments',COUNT_BIG(*) FROM app.VehiclePlatformAccountAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@verifyPlatform)
UNION ALL SELECT 'Remaining vehicle assignment history',COUNT_BIG(*) FROM app.VehiclePlatformAccountSwitches WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@verifyPlatform);

