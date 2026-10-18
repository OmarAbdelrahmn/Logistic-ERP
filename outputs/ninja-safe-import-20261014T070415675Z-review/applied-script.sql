-- Parameters are supplied by Import-NinjaSafeAccounts20261014.ps1.
-- The runner owns a SERIALIZABLE transaction and captures before-state snapshots.
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @Platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),
 @Actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'),
 @Contract uniqueidentifier=JSON_VALUE(@PlanJson,'$.contractId'),
 @From date=JSON_VALUE(@PlanJson,'$.effectiveFrom'),@Now datetimeoffset=SYSUTCDATETIME();
IF @From<>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')) THROW 51000,'Assignment date must be today in Riyadh.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Platform AND Code='NINJA' AND IsDeleted=0 AND Status=1 AND (SupportedPaymentModels & 1)=1)
 THROW 51000,'Ninja platform is unavailable or payment model unsupported.',1;
IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName='Omar' AND IsDeleted=0)
 THROW 51000,'Import actor unavailable.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WITH(UPDLOCK,HOLDLOCK) WHERE CorrelationId=@CorrelationId AND Action='NinjaSafeAccountImport')
 THROW 51000,'This import already committed; inspect the saved receipt.',1;
IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK) WHERE ClientPlatformId=@Platform)
 OR EXISTS(SELECT 1 FROM app.ClientContracts WITH(UPDLOCK,HOLDLOCK) WHERE ClientPlatformId=@Platform)
 THROW 51000,'Ninja accounts or contracts appeared since preflight; reconcile again.',1;
SELECT * INTO #AllSource FROM OPENJSON(@PlanJson,'$.rows') WITH(
 SheetRow int '$.row',ExternalId nvarchar(150) '$.externalId',OwnerIqama varchar(10) '$.ownerIqama',UserIqama varchar(10) '$.userIqama',
 AccountAction varchar(12) '$.accountAction',AssignmentAction varchar(20) '$.assignmentAction',SourceStatus nvarchar(100) '$.sourceStatus',
 AccountId uniqueidentifier '$.accountId',DetailsId uniqueidentifier '$.detailsId',OwnerEmployeeId uniqueidentifier '$.ownerEmployeeId',OwnerRiderId uniqueidentifier '$.ownerRiderId',
 OperatingCityId uniqueidentifier '$.operatingCityId',SponsorId uniqueidentifier '$.sponsorId',Code nvarchar(32) '$.code',PaymentModel int '$.paymentModel',
 InitialStatus int '$.initialStatus',FinalStatus int '$.finalStatus',Phone nvarchar(32) '$.savedPhone',OwnershipNotes nvarchar(4000) '$.ownershipNotes',
 Notes nvarchar(4000) '$.notes',StatusReason nvarchar(500) '$.statusReason',UserEmployeeId uniqueidentifier '$.userEmployeeId',RiderId uniqueidentifier '$.riderId',
 AssignmentId uniqueidentifier '$.assignmentId',RegistrationId uniqueidentifier '$.registrationId',EventId uniqueidentifier '$.eventId');
IF (SELECT COUNT(*) FROM #AllSource)<>329 OR (SELECT COUNT(*) FROM #AllSource WHERE AccountAction='Add')<>301
 OR (SELECT COUNT(*) FROM #AllSource WHERE AssignmentAction='Assign')<>107
 OR (SELECT COUNT(*) FROM #AllSource WHERE AssignmentAction='Pending')<>25
 THROW 51000,'Unexpected plan counts.',1;
SELECT * INTO #Source FROM #AllSource WHERE AccountAction='Add';
IF EXISTS(SELECT ExternalId FROM #AllSource GROUP BY ExternalId HAVING COUNT(*)>1)
 OR EXISTS(SELECT Code FROM #Source GROUP BY Code HAVING COUNT(*)>1)
 OR EXISTS(SELECT RiderId FROM #Source WHERE AssignmentAction='Assign' GROUP BY RiderId HAVING COUNT(*)>1)
 OR EXISTS(SELECT OwnerEmployeeId,OperatingCityId,SponsorId FROM #Source WHERE FinalStatus IN(1,2) GROUP BY OwnerEmployeeId,OperatingCityId,SponsorId HAVING COUNT(*)>1)
 THROW 51000,'Ambiguous source identity or ownership.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.Employees e WITH(UPDLOCK,HOLDLOCK) ON e.Id=s.OwnerEmployeeId
 LEFT JOIN app.RiderProfiles p WITH(UPDLOCK,HOLDLOCK) ON p.Id=s.OwnerRiderId
 LEFT JOIN app.Sponsors sp ON sp.Id=s.SponsorId LEFT JOIN app.OperatingCities c ON c.Id=s.OperatingCityId
 WHERE e.Id IS NULL OR e.IsDeleted=1 OR e.IsEmployee=1 OR e.IqamaNo<>s.OwnerIqama
 OR p.Id IS NULL OR p.EmployeeId<>e.Id OR p.IsDeleted=1
 OR sp.Id IS NULL OR sp.IsDeleted=1 OR sp.Status<>1 OR c.Id IS NULL OR c.IsDeleted=1 OR c.Status<>1
 OR s.AccountId IS NULL OR s.DetailsId IS NULL OR s.Code<>CONCAT('NINJA-',s.ExternalId) OR s.PaymentModel<>1
 OR s.InitialStatus NOT IN(1,3) OR s.FinalStatus NOT IN(1,2,3) OR s.Notes IS NULL OR s.OwnershipNotes IS NULL)
 THROW 51000,'Account references changed or account plan is invalid.',1;
IF EXISTS(SELECT 1 FROM #Source s JOIN app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK)
 ON a.Code=s.Code OR (a.ClientPlatformId=@Platform AND a.ExternalAccountId=s.ExternalId))
 THROW 51000,'Account code or external ID already exists.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.Employees e WITH(UPDLOCK,HOLDLOCK) ON e.Id=s.UserEmployeeId
 LEFT JOIN app.RiderProfiles p WITH(UPDLOCK,HOLDLOCK) ON p.Id=s.RiderId
 WHERE s.AssignmentAction='Assign' AND (e.Id IS NULL OR e.IqamaNo<>s.UserIqama OR e.IsDeleted=1 OR e.IsEmployee=1 OR e.Status<>3
 OR p.Id IS NULL OR p.EmployeeId<>e.Id OR p.IsDeleted=1 OR s.SourceStatus<>N'يعمل'
 OR s.Phone IS NULL OR LEN(s.Phone)<>10 OR s.Phone NOT LIKE '05%' OR s.Phone LIKE '%[^0-9]%'
 OR s.AssignmentId IS NULL OR s.RegistrationId IS NULL OR s.EventId IS NULL OR s.FinalStatus<>2 OR s.InitialStatus<>1))
 THROW 51000,'Assignment references or phone changed since preflight.',1;
IF EXISTS(SELECT 1 FROM #Source s JOIN app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK) ON a.RiderProfileId=s.RiderId
 WHERE s.AssignmentAction='Assign' AND a.IsDeleted=0 AND a.EffectiveTo IS NULL)
 THROW 51000,'A selected rider already has an active platform account.',1;

INSERT app.ClientContracts(Id,ClientPlatformId,Code,DisplayNameAr,DisplayNameEn,Status,Notes,CreatedAtUtc,CreatedByUserId,IsDeleted)
 VALUES(@Contract,@Platform,JSON_VALUE(@PlanJson,'$.contractCode'),N'تشغيل نينجا','Ninja operations',2,
 N'System-managed contract used by the simplified platform API.',@Now,@Actor,0);
-- Create every eligible account before assigning any of them.
INSERT app.PlatformRiderAccounts(Id,ClientPlatformId,RegisteredEmployeeId,OperatingCityId,SponsorId,Code,ExternalAccountId,
 PaymentModel,Status,StatusReason,OwnershipNotes,OperationalNotes,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT AccountId,@Platform,OwnerEmployeeId,OperatingCityId,SponsorId,Code,ExternalId,PaymentModel,InitialStatus,StatusReason,
 OwnershipNotes,Notes,@Now,@Actor,0 FROM #Source;
INSERT ninja.NinjaAccountDetails(Id,PlatformRiderAccountId,PhoneNumber,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT DetailsId,AccountId,Phone,@Now,@Actor,0 FROM #Source;
INSERT app.PlatformAccountRegistrations(Id,RegisteredEmployeeId,RiderProfileId,ClientPlatformId,ClientContractId,SponsorId,OperatingCityId,
 RegistrationType,Status,RequestedAtUtc,ActivatedAtUtc,PlatformRiderAccountId,Notes,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT RegistrationId,OwnerEmployeeId,OwnerRiderId,@Platform,@Contract,SponsorId,OperatingCityId,1,4,@Now,@Now,AccountId,
 N'Managed by the simplified platform API; owner matched by iqama during authorized Ninja import.',@Now,@Actor,0
 FROM #Source WHERE AssignmentAction='Assign';
INSERT app.RiderClientAssignments(Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,
 EffectiveFrom,Status,StartReason,AssignedByUserId,WasBackdated,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT AssignmentId,RiderId,@Contract,AccountId,PaymentModel,1,@From,2,N'تعيين حساب نينجا حسب إقامة المستخدم الفعلي في ملف البيانات.',@Actor,0,@Now,@Actor,0
 FROM #Source WHERE AssignmentAction='Assign';
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT EventId,AssignmentId,1,2,@Now,@Actor,N'تعيين نينجا من ملف البيانات بمطابقة رقم إقامة المستخدم.',
 (SELECT s.SheetRow,s.ExternalId,s.OwnerIqama,s.UserIqama,s.OwnerRiderId,s.RiderId ActualRiderProfileId,@From EffectiveFrom FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor
 FROM #Source s WHERE AssignmentAction='Assign';
UPDATE a SET Status=2,StatusReason=NULL,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.PlatformRiderAccounts a JOIN #Source s ON s.AccountId=a.Id WHERE s.AssignmentAction='Assign';

-- Independently compare persisted values against the full prepared source.
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.AccountId
 LEFT JOIN ninja.NinjaAccountDetails d ON d.Id=s.DetailsId AND d.PlatformRiderAccountId=a.Id
 WHERE a.Id IS NULL OR a.ClientPlatformId<>@Platform OR a.ExternalAccountId<>s.ExternalId OR a.Code<>s.Code
 OR a.RegisteredEmployeeId<>s.OwnerEmployeeId OR a.OperatingCityId<>s.OperatingCityId OR a.SponsorId<>s.SponsorId
 OR a.Status<>s.FinalStatus OR a.PaymentModel<>s.PaymentModel OR a.IsDeleted<>0
 OR ISNULL(a.OwnershipNotes,'')<>ISNULL(s.OwnershipNotes,'') OR ISNULL(a.OperationalNotes,'')<>ISNULL(s.Notes,'')
 OR ISNULL(a.StatusReason,'')<>ISNULL(CASE WHEN s.AssignmentAction='Assign' THEN NULL ELSE s.StatusReason END,'')
 OR d.Id IS NULL OR d.IsDeleted<>0 OR ISNULL(d.PhoneNumber,'')<>ISNULL(s.Phone,''))
 THROW 51000,'Persisted account values do not match the source plan.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.AssignmentId
 LEFT JOIN app.RiderAssignmentEvents ev ON ev.Id=s.EventId AND ev.RiderClientAssignmentId=a.Id
 LEFT JOIN app.PlatformAccountRegistrations reg ON reg.Id=s.RegistrationId AND reg.PlatformRiderAccountId=s.AccountId
 WHERE s.AssignmentAction='Assign' AND (a.Id IS NULL OR a.PlatformRiderAccountId<>s.AccountId OR a.RiderProfileId<>s.RiderId
 OR a.ClientContractId<>@Contract OR a.EffectiveFrom<>@From OR a.EffectiveTo IS NOT NULL OR a.Status<>2 OR a.IsDeleted<>0
 OR a.PaymentModel<>1 OR a.RiderAccountSlot<>1 OR a.WasBackdated<>0 OR ev.Id IS NULL OR ev.ToStatus<>2
 OR reg.Id IS NULL OR reg.RegisteredEmployeeId<>s.OwnerEmployeeId OR reg.RiderProfileId<>s.OwnerRiderId
 OR reg.ClientPlatformId<>@Platform OR reg.ClientContractId<>@Contract OR reg.SponsorId<>s.SponsorId OR reg.OperatingCityId<>s.OperatingCityId
 OR reg.Status<>4 OR reg.RegistrationType<>1 OR reg.IsDeleted<>0))
 THROW 51000,'Persisted assignments or ownership registrations differ from the plan.',1;
IF EXISTS(SELECT 1 FROM #AllSource s JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=s.ExternalId WHERE s.AccountAction='Skip')
 OR EXISTS(SELECT 1 FROM #Source s JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId=s.AccountId AND a.IsDeleted=0 AND a.EffectiveTo IS NULL WHERE s.AssignmentAction<>'Assign')
 THROW 51000,'A skipped account or pending assignment was saved.',1;
IF EXISTS(SELECT a.RiderProfileId FROM app.RiderClientAssignments a JOIN #Source s ON s.RiderId=a.RiderProfileId WHERE s.AssignmentAction='Assign' AND a.EffectiveTo IS NULL AND a.IsDeleted=0 GROUP BY a.RiderProfileId HAVING COUNT(*)<>1)
 THROW 51000,'A selected rider has an unexpected active-account count.',1;
IF EXISTS(SELECT * FROM #BeforeAccounts EXCEPT SELECT * FROM app.PlatformRiderAccounts)
 OR EXISTS(SELECT * FROM #BeforeAssignments EXCEPT SELECT * FROM app.RiderClientAssignments)
 OR EXISTS(SELECT * FROM #BeforeEmployees EXCEPT SELECT * FROM app.Employees)
 OR EXISTS(SELECT * FROM app.Employees EXCEPT SELECT * FROM #BeforeEmployees)
 OR EXISTS(SELECT * FROM #BeforeRiderProfiles EXCEPT SELECT * FROM app.RiderProfiles)
 OR EXISTS(SELECT * FROM app.RiderProfiles EXCEPT SELECT * FROM #BeforeRiderProfiles)
 OR EXISTS(SELECT * FROM #BeforeRegistrations EXCEPT SELECT * FROM app.PlatformAccountRegistrations)
 OR EXISTS(SELECT * FROM #BeforeContracts EXCEPT SELECT * FROM app.ClientContracts)
 OR EXISTS(SELECT * FROM #BeforeDetails EXCEPT SELECT * FROM ninja.NinjaAccountDetails)
 THROW 51000,'An existing account, assignment, employee, profile, registration, contract, or phone changed.',1;
IF (SELECT COUNT_BIG(*) FROM app.PlatformAccountCredentialVersions)<>(SELECT CredentialCount FROM #BeforeCounts)
 OR (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyPerformance)<>(SELECT PerformanceCount FROM #BeforeCounts)
 OR (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyDeductions)<>(SELECT DeductionCount FROM #BeforeCounts)
 OR (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyImports)<>(SELECT ImportCount FROM #BeforeCounts)
 THROW 51000,'Credentials or daily-report counts changed.',1;

-- Append audit evidence for each created account and each assignment.
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),NEWID(),@Actor,'User','Created','ninja','PlatformRiderAccount',s.AccountId,@Now,@CorrelationId,
 N'User authorized importing only eligible Ninja accounts.',
 (SELECT s.ExternalId,s.OwnerIqama,s.OwnerEmployeeId,s.OwnerRiderId,s.OperatingCityId,s.SponsorId,s.FinalStatus,s.Notes,s.Phone,@SourceHash SourceSha256,s.SheetRow FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor FROM #Source s;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),NEWID(),@Actor,'User','Created','ninja','RiderClientAssignment',s.AssignmentId,@Now,@CorrelationId,
 N'User authorized iqama-matched assignments without conflicts.',
 (SELECT s.ExternalId,s.AccountId,s.UserIqama,s.RiderId,s.RegistrationId,@Contract ClientContractId,@From EffectiveFrom,s.EventId,s.SheetRow FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor FROM #Source s WHERE AssignmentAction='Assign';
DECLARE @Summary nvarchar(max)=(SELECT 329 SourceAccounts,301 AccountsAdded,28 AccountsSkipped,107 AssignmentsCreated,25 WorkingAccountsPending,
 93 NonWorkingAccountsUnassigned,76 ProblemAccountsSuspended,@From EffectiveFrom,@SourceHash SourceSha256 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),@Actor,'User','NinjaSafeAccountImport','ninja','ClientPlatform',@Platform,@Now,@CorrelationId,
 N'User requested eligible accounts and eligible assignments only. Account vehicle types and source statuses retained in notes. No staff, profiles, prior assignments, or passwords changed.',
 @Summary,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('ninja','platform-operations');
INSERT app.DatasetVersions(Id,ModuleKey,Version,LastChangedAtUtc,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT NEWID(),k.ModuleKey,1,@Now,@Now,@Actor,0 FROM(VALUES('ninja'),('platform-operations')) k(ModuleKey)
 WHERE NOT EXISTS(SELECT 1 FROM app.DatasetVersions WHERE ModuleKey=k.ModuleKey);
SELECT @Summary SummaryJson;
