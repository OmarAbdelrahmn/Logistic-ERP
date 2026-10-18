-- One-time authorized roster reconciliation. @PlanJson and @CorrelationId are supplied by the runner.
-- The runner owns the transaction, before-image backups and preservation checks.
-- Target account locks plus reviewed rowversions and unique rider-slot indexes protect the write set.
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @Platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),
 @Contract uniqueidentifier=JSON_VALUE(@PlanJson,'$.contractId'),
 @Actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'),@Now datetimeoffset=SYSUTCDATETIME(),
 @Reason nvarchar(1000)=N'October Jahez workbook reconciliation authorized by user; dates represent both handover and last clearance. No financial receipt or account fee imported.';
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code='JAHEZ' AND IsDeleted=0)
 THROW 51000,'Unexpected Jahez platform.',1;
IF NOT EXISTS(SELECT 1 FROM app.ClientContracts WHERE Id=@Contract AND ClientPlatformId=@Platform AND Status=2 AND IsDeleted=0)
 THROW 51000,'Jahez contract changed.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId)
 THROW 51000,'This import was already committed. Do not replay.',1;
SELECT * INTO #Source FROM OPENJSON(@PlanJson,'$.accounts') WITH
 (AccountId uniqueidentifier '$.accountId',ExternalId varchar(100) '$.account',NewAccount bit '$.newAccount',
 RowVersion varchar(32) '$.accountRowVersion',OwnerId uniqueidentifier '$.ownerId',OwnerIqama varchar(10) '$.ownerIqama',
 SponsorId uniqueidentifier '$.sponsorId',SponsorNumber varchar(20) '$.sponsorNumber',CityId uniqueidentifier '$.cityId',
 DesiredStatus int '$.desiredStatus',VehicleId uniqueidentifier '$.vehicleId',Serial varchar(100) '$.serial',
 SheetStatus varchar(20) '$.status',AssignmentResult varchar(30) '$.assignmentResult',SheetRow int '$.row');
SELECT * INTO #Profiles FROM OPENJSON(@PlanJson,'$.profiles') WITH
 (Id uniqueidentifier '$.id',EmployeeId uniqueidentifier '$.employeeId',Iqama varchar(10) '$.iqama');
SELECT * INTO #Assignments FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH
 (Id uniqueidentifier '$.id',SessionId uniqueidentifier '$.sessionId',AccountId uniqueidentifier '$.accountId',
 RiderId uniqueidentifier '$.riderId',EmployeeId uniqueidentifier '$.employeeId',RiderIqama varchar(10) '$.riderIqama',
 FromDate date '$.fromDate',Slot int '$.slot');
SELECT * INTO #VehicleLinks FROM OPENJSON(@PlanJson,'$.vehicleLinks') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',VehicleId uniqueidentifier '$.vehicleId',Serial varchar(100) '$.serial');
IF (SELECT COUNT(*) FROM #Source)<>380 OR (SELECT COUNT(*) FROM #Source WHERE NewAccount=1)<>2
 OR (SELECT COUNT(*) FROM #Profiles)<>CONVERT(int,JSON_VALUE(@PlanJson,'$.summary.ownerProfilesCreated'))
 OR (SELECT COUNT(*) FROM #Assignments)<>CONVERT(int,JSON_VALUE(@PlanJson,'$.summary.newRiderAssignments'))
 OR (SELECT COUNT(*) FROM #VehicleLinks)<>374
 THROW 51000,'Unexpected reviewed plan counts.',1;
IF EXISTS(SELECT AccountId FROM #Source GROUP BY AccountId HAVING COUNT(*)>1)
 OR EXISTS(SELECT ExternalId FROM #Source GROUP BY ExternalId HAVING COUNT(*)>1)
 THROW 51000,'Duplicate source account.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.PlatformRiderAccounts p WITH(UPDLOCK,HOLDLOCK) ON p.Id=s.AccountId
 WHERE s.NewAccount=0 AND (p.Id IS NULL OR p.ClientPlatformId<>@Platform OR p.ExternalAccountId<>s.ExternalId
 OR p.RegisteredEmployeeId<>s.OwnerId OR p.RegisteredEmployeeId IS NULL OR p.IsDeleted=1
 OR p.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.RowVersion"))','varbinary(8)')))
 THROW 51000,'An existing account changed since review.',1;
IF EXISTS(SELECT 1 FROM #Source s JOIN app.PlatformRiderAccounts p ON p.ClientPlatformId=@Platform AND p.ExternalAccountId=s.ExternalId WHERE s.NewAccount=1)
 THROW 51000,'A new account identifier already exists.',1;
IF EXISTS(SELECT 1 FROM #Source s LEFT JOIN app.Employees e ON e.Id=s.OwnerId
 LEFT JOIN app.Sponsors p ON p.Id=s.SponsorId
 WHERE e.Id IS NULL OR e.IqamaNo<>s.OwnerIqama OR e.IsDeleted=1 OR p.Id IS NULL OR p.IsDeleted=1 OR p.EmployerIdentityNumber<>s.SponsorNumber)
 THROW 51000,'Owner Iqama or sponsor 70-number no longer matches.',1;
IF EXISTS(SELECT 1 FROM #Profiles p LEFT JOIN app.Employees e ON e.Id=p.EmployeeId
 WHERE e.Id IS NULL OR e.IqamaNo<>p.Iqama OR e.IsDeleted=1 OR e.IsEmployee<>1)
 OR EXISTS(SELECT 1 FROM #Profiles p JOIN app.RiderProfiles r ON r.EmployeeId=p.EmployeeId)
 THROW 51000,'Owner profile creation conflicts with current employee records.',1;
IF EXISTS(SELECT 1 FROM OPENJSON(@PlanJson,'$.assignmentChecks') WITH(Id uniqueidentifier '$.id',RowVersion varchar(32) '$.rowVersion') s
 LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id WHERE a.Id IS NULL OR a.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.RowVersion"))','varbinary(8)'))
 OR (SELECT COUNT(*) FROM app.RiderClientAssignments)<>(SELECT COUNT(*) FROM OPENJSON(@PlanJson,'$.assignmentChecks'))
 THROW 51000,'Rider assignments changed since review.',1;
IF EXISTS(SELECT 1 FROM OPENJSON(@PlanJson,'$.sessionChecks') WITH(Id uniqueidentifier '$.id',RowVersion varchar(32) '$.rowVersion') s
 LEFT JOIN jahez_v2.AccountSessions a ON a.Id=s.Id WHERE a.Id IS NULL OR a.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.RowVersion"))','varbinary(8)'))
 OR (SELECT COUNT(*) FROM jahez_v2.AccountSessions)<>(SELECT COUNT(*) FROM OPENJSON(@PlanJson,'$.sessionChecks'))
 THROW 51000,'Jahez sessions changed since review.',1;
IF EXISTS(SELECT 1 FROM #Assignments s JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId=s.AccountId WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL)
 OR EXISTS(SELECT 1 FROM #VehicleLinks s JOIN app.VehiclePlatformAccountAssignments a ON a.PlatformRiderAccountId=s.AccountId WHERE a.IsDeleted=0 AND a.EndedAtUtc IS NULL)
 THROW 51000,'A target account is already assigned.',1;
IF EXISTS(SELECT 1 FROM #VehicleLinks s LEFT JOIN app.Vehicles v ON v.Id=s.VehicleId
 WHERE v.Id IS NULL OR v.SerialNumber<>s.Serial OR v.IsDeleted=1)
 THROW 51000,'A vehicle serial no longer matches.',1;
IF EXISTS(SELECT 1 FROM #Assignments s LEFT JOIN app.RiderProfiles r ON r.Id=s.RiderId
 LEFT JOIN app.Employees e ON e.Id=r.EmployeeId JOIN #Source a ON a.AccountId=s.AccountId
 WHERE r.Id IS NULL OR r.IsDeleted=1 OR e.Id<>s.EmployeeId OR e.IqamaNo<>s.RiderIqama OR e.IsDeleted=1 OR e.Status<>3
 OR s.FromDate IS NULL OR s.FromDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')) OR a.SheetStatus<>'Active'
 OR EXISTS(SELECT 1 FROM jahez_v2.TransferredDebts d WHERE d.RiderProfileId=s.RiderId AND d.IsDeleted=0 AND d.PaidAtUtc IS NULL))
 THROW 51000,'An actual rider or handover date is invalid.',1;
IF EXISTS(SELECT 1 FROM #Assignments s JOIN app.RiderClientAssignments a ON a.RiderProfileId=s.RiderId AND a.RiderAccountSlot=s.Slot
 WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL)
 OR EXISTS(SELECT RiderId,Slot FROM #Assignments GROUP BY RiderId,Slot HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM #Assignments s JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId=s.AccountId
 WHERE a.IsDeleted=0 AND a.Status IN(1,2,3) AND (a.EffectiveTo IS NULL OR a.EffectiveTo>=s.FromDate))
 THROW 51000,'Rider capacity or assignment period conflicts.',1;

INSERT app.RiderProfiles(Id,EmployeeId,OperationalNotes,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT Id,EmployeeId,N'Rider profile for Jahez dashboard owner, created from the October owner-Iqama reconciliation.',@Now,@Actor,0 FROM #Profiles;
INSERT app.PlatformRiderAccounts(Id,ClientPlatformId,RegisteredEmployeeId,OperatingCityId,SponsorId,Code,ExternalAccountId,
 PaymentModel,Status,StatusReason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT AccountId,@Platform,OwnerId,CityId,SponsorId,'JAHEZ-'+ExternalId,ExternalId,1,DesiredStatus,@Reason,@Now,@Actor,0 FROM #Source WHERE NewAccount=1;
UPDATE a SET SponsorId=s.SponsorId,Status=s.DesiredStatus,
 StatusReason=CASE WHEN s.SheetStatus='Inactive' THEN N'October source dashboard is Inactive; suspended for possible future reactivation.'
 WHEN s.AssignmentResult='Pending' THEN N'October roster reconciled; rider import pending a documented date, capacity, status or existing-payment conflict.'
 ELSE @Reason END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.PlatformRiderAccounts a JOIN #Source s ON s.AccountId=a.Id;
INSERT app.RiderClientAssignments(Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,
 EffectiveFrom,Status,StartReason,AssignedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT Id,RiderId,@Contract,AccountId,1,Slot,FromDate,2,@Reason,@Actor,
 CASE WHEN FromDate<CONVERT(date,SWITCHOFFSET(@Now,'+03:00')) THEN 1 ELSE 0 END,
 CASE WHEN FromDate<CONVERT(date,SWITCHOFFSET(@Now,'+03:00')) THEN @Reason ELSE NULL END,@Now,@Actor,0 FROM #Assignments;
INSERT jahez_v2.AccountSessions(Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,CommissionStartsOn,
 LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT SessionId,AccountId,RiderId,Id,TODATETIMEOFFSET(CONVERT(datetime2,FromDate),'+03:00'),DATEADD(day,1,FromDate),
 TODATETIMEOFFSET(CONVERT(datetime2,FromDate),'+03:00'),@Reason,@Now,@Actor,0 FROM #Assignments;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),Id,1,2,@Now,@Actor,@Reason,
 (SELECT s.AccountId,s.RiderId,s.FromDate,s.SessionId,s.Slot FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor FROM #Assignments s;
-- Vehicle links are a current roster snapshot. No historical vehicle handover date was supplied.
INSERT app.VehiclePlatformAccountAssignments(Id,VehicleId,PlatformRiderAccountId,AssignedAtUtc,AssignmentReason,
 ApprovalStatus,ApprovedAtUtc,ApprovedByUserId,Status,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT Id,VehicleId,AccountId,@Now,N'Current October Jahez roster matched by exact vehicle serial number.',1,@Now,@Actor,1,@Now,@Actor,0 FROM #VehicleLinks;
DECLARE @Summary nvarchar(max)=JSON_QUERY(@PlanJson,'$.summary');
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,
 Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),@Actor,'User','JahezAccountRosterImport','jahez','ClientPlatform',@Platform,@Now,@CorrelationId,@Reason,
 (SELECT JSON_VALUE(@PlanJson,'$.sourceSha256') SourceSha256,JSON_QUERY(@PlanJson,'$.dateCorrections') DateCorrections,
 JSON_QUERY(@PlanJson,'$.pendingAssignments') PendingAssignments FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Summary,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 WHERE ModuleKey IN('jahez','platform-operations','workforce','fleet');
SELECT @Summary SummaryJson;
