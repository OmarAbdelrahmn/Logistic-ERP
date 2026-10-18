-- One-time workbook reset. Run through Apply-ChefzWorkbookUpdate.ps1, which backs up
-- the affected rows before executing this transaction. @WorkbookJson is supplied as a parameter.
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @PlatformId uniqueidentifier, @CityId uniqueidentifier;
SELECT @PlatformId=Id FROM platform.ClientPlatforms WHERE Code='SHIFTZ' AND IsDeleted=0;
SELECT @CityId=o.Id FROM app.OperatingCities o JOIN platform.GlobalCities g ON g.Id=o.GlobalCityId WHERE g.NameAr=N'جدة' AND o.IsDeleted=0;
IF @PlatformId IS NULL OR @CityId IS NULL THROW 51000,'Chefz platform or Jeddah city is missing.',1;
CREATE TABLE #Source (IdNumber nvarchar(100) PRIMARY KEY, AccountType int, SponsorNumber nvarchar(100), VehicleType nvarchar(20), City nvarchar(100), DashboardStatus nvarchar(100));
INSERT #Source SELECT * FROM OPENJSON(@WorkbookJson,'$.accounts') WITH
    (IdNumber nvarchar(100) '$.id', AccountType int '$.accountType', SponsorNumber nvarchar(100) '$.sponsor', VehicleType nvarchar(20) '$.vehicleType', City nvarchar(100) '$.city', DashboardStatus nvarchar(100) '$.status');
IF (SELECT COUNT(*) FROM #Source) <> 464 THROW 51000,'Expected exactly 464 unique account rows.',1;
IF EXISTS (SELECT 1 FROM #Source WHERE AccountType NOT IN (1,2) OR VehicleType NOT IN ('Car','Motorcycle') OR City<>N'جدة' OR DashboardStatus NOT IN ('active','Block By CHEFZ','The supervisor suspended'))
    THROW 51000,'Invalid workbook account values.',1;
IF EXISTS (SELECT 1 FROM #Source s WHERE (SELECT COUNT(*) FROM app.Sponsors p WHERE p.EmployerIdentityNumber=s.SponsorNumber AND p.IsDeleted=0)<>1)
    THROW 51000,'Workbook sponsor numbers must match exactly one sponsor.',1;
-- Explicit user resolution: 2507652275 and workbook ID 2506752275 are different people; keep both.
IF NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@PlatformId AND ExternalAccountId='2507652275' AND IsDeleted=0)
    THROW 51000,'The explicitly retained extra account is missing.',1;
IF EXISTS (SELECT 1 FROM #Source s WHERE s.IdNumber<>'2506752275' AND NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a WHERE a.ClientPlatformId=@PlatformId AND a.IsDeleted=0 AND a.ExternalAccountId=s.IdNumber))
    THROW 51000,'Unexpected unmatched workbook ID.',1;
INSERT app.PlatformRiderAccounts (Id,ClientPlatformId,OperatingCityId,SponsorId,Code,ExternalAccountId,PaymentModel,Status,CreatedAtUtc,IsDeleted)
SELECT NEWID(),@PlatformId,@CityId,p.Id,'SHIFTZ-WB-2506752275',s.IdNumber,1,CASE WHEN s.DashboardStatus='active' THEN 1 ELSE 3 END,SYSUTCDATETIME(),0
FROM #Source s JOIN app.Sponsors p ON p.EmployerIdentityNumber=s.SponsorNumber AND p.IsDeleted=0
WHERE s.IdNumber='2506752275' AND NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a WHERE a.ClientPlatformId=@PlatformId AND a.ExternalAccountId=s.IdNumber);
SELECT a.Id,a.ExternalAccountId INTO #Accounts FROM app.PlatformRiderAccounts a WITH (UPDLOCK,HOLDLOCK) WHERE a.ClientPlatformId=@PlatformId AND a.IsDeleted=0;
IF (SELECT COUNT(*) FROM #Accounts)<>465 OR EXISTS(SELECT 1 FROM #Accounts a WHERE a.ExternalAccountId<>'2507652275' AND NOT EXISTS(SELECT 1 FROM #Source s WHERE s.IdNumber=a.ExternalAccountId))
    THROW 51000,'Expected the 464 workbook accounts plus the explicitly retained account.',1;
IF EXISTS (SELECT 1 FROM #Accounts a JOIN app.PlatformRiderAccounts p ON p.Id=a.Id JOIN app.Employees e ON e.Id=p.RegisteredEmployeeId WHERE e.IqamaNo<>a.ExternalAccountId)
    THROW 51000,'Registered owner mismatch requires review.',1;
SELECT r.Id INTO #Assignments FROM app.RiderClientAssignments r WITH (UPDLOCK,HOLDLOCK) JOIN #Accounts a ON a.Id=r.PlatformRiderAccountId;
IF EXISTS(SELECT 1 FROM jahez.JahezAccountHandover h JOIN #Assignments r ON h.RiderClientAssignmentId=r.Id)
    THROW 51000,'A Chefz assignment is referenced by Jahez; refusing cross-platform deletion.',1;
DECLARE @AssignmentCount int=(SELECT COUNT(*) FROM #Assignments), @DailyCount int=(SELECT COUNT(*) FROM chefz.DailyPerformance d JOIN #Accounts a ON d.PlatformRiderAccountId=a.Id);
-- The user explicitly requested removal of Chefz assignment history, including dependent performance.
DELETE d FROM chefz.DailyPerformance d JOIN #Accounts a ON d.PlatformRiderAccountId=a.Id;
DELETE s FROM chefz.WalletSettlements s JOIN #Accounts a ON s.PlatformRiderAccountId=a.Id;
DELETE e FROM app.RiderAssignmentEvents e JOIN #Assignments r ON e.RiderClientAssignmentId=r.Id;
DELETE r FROM app.RiderClientAssignments r JOIN #Assignments a ON r.Id=a.Id;
DELETE i FROM chefz.PerformanceImports i WHERE NOT EXISTS(SELECT 1 FROM chefz.DailyPerformance d WHERE d.ImportId=i.Id);
UPDATE a SET SponsorId=p.Id,OperatingCityId=@CityId,Status=CASE WHEN s.DashboardStatus='active' THEN 1 ELSE 3 END,
    StatusReason=CASE WHEN s.DashboardStatus='active' THEN NULL ELSE s.DashboardStatus END,
    UpdatedAtUtc=SYSUTCDATETIME()
FROM app.PlatformRiderAccounts a JOIN #Source s ON s.IdNumber=a.ExternalAccountId JOIN app.Sponsors p ON p.EmployerIdentityNumber=s.SponsorNumber AND p.IsDeleted=0 WHERE a.ClientPlatformId=@PlatformId AND a.IsDeleted=0;
UPDATE d SET ReportIdNumber=s.IdNumber,AccountType=s.AccountType,VehicleType=s.VehicleType,DashboardStatus=s.DashboardStatus,
    IsDeleted=0,DeletedAtUtc=NULL,DeletedByUserId=NULL,DeletionReason=NULL,UpdatedAtUtc=SYSUTCDATETIME()
FROM chefz.AccountDetails d JOIN #Accounts a ON a.Id=d.PlatformRiderAccountId JOIN #Source s ON s.IdNumber=a.ExternalAccountId;
INSERT chefz.AccountDetails (Id,PlatformRiderAccountId,ReportIdNumber,AccountType,VehicleType,DashboardStatus,CreatedAtUtc,IsDeleted)
SELECT NEWID(),a.Id,s.IdNumber,s.AccountType,s.VehicleType,s.DashboardStatus,SYSUTCDATETIME(),0 FROM #Accounts a JOIN #Source s ON s.IdNumber=a.ExternalAccountId
WHERE NOT EXISTS(SELECT 1 FROM chefz.AccountDetails d WHERE d.PlatformRiderAccountId=a.Id);
IF EXISTS(SELECT 1 FROM chefz.SponsorStatistics WHERE IsDeleted=0) THROW 51000,'Sponsor statistics already exist; refusing to overwrite manual values.',1;
INSERT chefz.SponsorStatistics (Id,SponsorId,SponsorshipCount,Working,NotWorking,Blocked,PermanentlyBlocked,UpdateBlocked,Uploaded,PrivateLicense,PendingUpload,NoLicense,ManagementSuspended,Motorcycle,WorkingOutsideSponsorship,WorkingWithoutPerformance,CreatedAtUtc,IsDeleted)
SELECT NEWID(),p.Id,j.SponsorshipCount,j.Working,j.NotWorking,j.Blocked,j.PermanentlyBlocked,j.UpdateBlocked,j.Uploaded,j.PrivateLicense,j.PendingUpload,j.NoLicense,j.ManagementSuspended,j.Motorcycle,j.WorkingOutsideSponsorship,NULL,SYSUTCDATETIME(),0
FROM OPENJSON(@WorkbookJson,'$.statistics') WITH (SponsorNumber nvarchar(100) '$.sponsor', SponsorshipCount int,Working int,NotWorking int,Blocked int,PermanentlyBlocked int,UpdateBlocked int,Uploaded int,PrivateLicense int,PendingUpload int,NoLicense int,ManagementSuspended int,Motorcycle int,WorkingOutsideSponsorship int) j
JOIN app.Sponsors p ON p.EmployerIdentityNumber=j.SponsorNumber AND p.IsDeleted=0;
IF (SELECT COUNT(*) FROM chefz.SponsorStatistics WHERE IsDeleted=0)<>3 THROW 51000,'Expected three sponsor statistics rows.',1;
INSERT audit.AuditEntries (Id,EventId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc)
VALUES(NEWID(),NEWID(),'System','ChefzWorkbookReset','chefz','ClientPlatform',@PlatformId,SYSUTCDATETIME(),'chefz-workbook-20261011',N'User requested Chefz assignment/history removal and workbook account replacement.',
    CONCAT('{"assignments":',@AssignmentCount,',"dailyPerformance":',@DailyCount,'}'),'{"accounts":465,"workbookAccounts":464,"retainedAccount":"2507652275","assignments":0,"statistics":3}','CodexAuthorizedMaintenance',1,SYSUTCDATETIME());
-- Raw SQL bypasses the normal EF dataset-version hook; invalidate affected views.
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=SYSUTCDATETIME(),UpdatedAtUtc=SYSUTCDATETIME() WHERE ModuleKey IN ('chefz','platform-operations');
INSERT app.DatasetVersions (Id,ModuleKey,Version,LastChangedAtUtc,CreatedAtUtc,IsDeleted)
SELECT NEWID(),k.ModuleKey,1,SYSUTCDATETIME(),SYSUTCDATETIME(),0 FROM (VALUES('chefz'),('platform-operations')) k(ModuleKey)
WHERE NOT EXISTS(SELECT 1 FROM app.DatasetVersions d WHERE d.ModuleKey=k.ModuleKey);
SELECT @AssignmentCount RemovedAssignments,@DailyCount RemovedDailyRecords,COUNT(*) TotalChefzAccounts,
    (SELECT COUNT(*) FROM #Source) WorkbookAccounts FROM #Accounts;
