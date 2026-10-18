-- Parameters: @PlanJson, @CorrelationId. Runner owns the SERIALIZABLE transaction and backups.
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @Platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId'),
 @Contract uniqueidentifier=JSON_VALUE(@PlanJson,'$.contractId'),
 @Actor uniqueidentifier=JSON_VALUE(@PlanJson,'$.actorId'), @Now datetimeoffset=SYSUTCDATETIME();
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code='SHIFTZ' AND IsDeleted=0)
 THROW 51000,'Unexpected Chefz platform.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId)
 THROW 51000,'Import already committed; do not replay.',1;
IF NOT EXISTS(SELECT 1 FROM app.ClientContracts WHERE Id=@Contract AND ClientPlatformId=@Platform AND Status=2 AND IsDeleted=0)
 THROW 51000,'Chefz contract unavailable.',1;
SELECT * INTO #Accounts FROM OPENJSON(@PlanJson,'$.accounts') WITH
 (Id uniqueidentifier '$.id',ExternalId varchar(100) '$.externalAccountId',RowVersion varchar(32) '$.rowVersion',
 CityId uniqueidentifier '$.cityId',SponsorId uniqueidentifier '$.sponsorId',AccountType int '$.accountType',
 VehicleType nvarchar(20) '$.vehicleType',DetailsId uniqueidentifier '$.detailsId',DesiredStatus int '$.desiredStatus',
 DashboardStatus nvarchar(100) '$.dashboardStatus',AssignmentAction varchar(20) '$.assignmentAction',AssignmentId uniqueidentifier '$.assignmentId');
SELECT * INTO #Insert FROM OPENJSON(@PlanJson,'$.insertAssignments') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',EmployeeId uniqueidentifier '$.employeeId',
 UserIqama varchar(10) '$.userIqama',FromDate date '$.fromDate',ToDate date '$.toDate',Status int '$.status',Slot int '$.slot',
 PaymentModel int '$.paymentModel',Reason nvarchar(1000) '$.reason',Sheet nvarchar(100) '$.sheet',SheetRow int '$.sourceRow');
SELECT * INTO #Close FROM OPENJSON(@PlanJson,'$.closeAssignments') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',EndDate date '$.endDate',Status int '$.status',Reason nvarchar(1000) '$.reason');
SELECT * INTO #Credentials FROM OPENJSON(@PlanJson,'$.credentials') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',Ciphertext nvarchar(max) '$.ciphertext',Nonce nvarchar(100) '$.nonce',Tag nvarchar(100) '$.tag');
SELECT * INTO #Reopen FROM OPENJSON(@PlanJson,'$.reopenAssignments') WITH
 (Id uniqueidentifier '$.id',AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',EmployeeId uniqueidentifier '$.employeeId',UserIqama varchar(10) '$.userIqama',FromDate date '$.fromDate',Slot int '$.slot');
IF (SELECT COUNT(*) FROM #Accounts)<>464 OR (SELECT COUNT(*) FROM #Insert WHERE Status=3)<>3
 OR (SELECT COUNT(*) FROM #Insert WHERE Status=2)<>66 OR (SELECT COUNT(*) FROM #Close)<>3 OR (SELECT COUNT(*) FROM #Reopen)<>1
 THROW 51000,'Unexpected reviewed import counts.',1;
IF EXISTS(SELECT 1 FROM #Accounts s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.Id
 WHERE a.Id IS NULL OR a.ClientPlatformId<>@Platform OR a.ExternalAccountId<>s.ExternalId OR a.IsDeleted=1
 OR a.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.RowVersion"))','varbinary(8)'))
 THROW 51000,'An account changed after review; rebuild the plan.',1;
IF EXISTS(SELECT 1 FROM OPENJSON(@PlanJson,'$.assignmentChecks') WITH(Id uniqueidentifier '$.id',RowVersion varchar(32) '$.rowVersion') s
 LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id
 WHERE a.Id IS NULL OR a.IsDeleted=1 OR a.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("s.RowVersion"))','varbinary(8)'))
 OR (SELECT COUNT(*) FROM app.RiderClientAssignments WHERE IsDeleted=0)<>(SELECT COUNT(*) FROM OPENJSON(@PlanJson,'$.assignmentChecks'))
 THROW 51000,'An assignment changed after review; rebuild the plan.',1;
IF EXISTS(SELECT 1 FROM #Insert s LEFT JOIN app.PlatformRiderAccounts a ON a.Id=s.AccountId
 LEFT JOIN app.RiderProfiles r ON r.Id=s.RiderId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.Id IS NULL OR a.ClientPlatformId<>@Platform OR a.IsDeleted=1 OR a.PaymentModel<>s.PaymentModel
 OR r.Id IS NULL OR r.IsDeleted=1 OR e.Id<>s.EmployeeId OR e.IqamaNo<>s.UserIqama OR e.IsDeleted=1
 OR s.FromDate IS NULL OR s.FromDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00'))
 OR (s.Status=2 AND (e.IsEmployee=1 OR e.Status<>3 OR s.ToDate IS NOT NULL))
 OR (s.Status=3 AND (s.ToDate IS NULL OR s.ToDate<s.FromDate)))
 THROW 51000,'Rider/account match is no longer valid.',1;
IF EXISTS(SELECT 1 FROM #Insert s JOIN jahez_v2.TransferredDebts d ON d.RiderProfileId=s.RiderId
 WHERE s.Status=2 AND d.PaidAtUtc IS NULL AND d.IsDeleted=0)
 THROW 51000,'A rider has unpaid transferred debt.',1;
IF EXISTS(SELECT 1 FROM #Close s JOIN app.RiderClientAssignments a ON a.Id=s.Id
 WHERE a.EffectiveTo IS NOT NULL OR a.PlatformRiderAccountId<>s.AccountId OR a.RiderProfileId<>s.RiderId OR s.EndDate<a.EffectiveFrom)
 THROW 51000,'Replacement dates conflict with existing history.',1;
IF EXISTS(SELECT 1 FROM #Close s JOIN chefz.DailyPerformance p ON p.RiderClientAssignmentId=s.Id WHERE s.Status=4 OR p.ReportDate>s.EndDate)
 OR EXISTS(SELECT 1 FROM #Close s JOIN chefz.WalletSettlements p ON p.RiderClientAssignmentId=s.Id WHERE s.Status=4 OR p.ChargedTo>s.EndDate)
 OR EXISTS(SELECT 1 FROM #Close s JOIN jahez_v2.AccountSessions p ON p.RiderClientAssignmentId=s.Id)
 OR EXISTS(SELECT 1 FROM #Close s JOIN ninja.NinjaDailyPerformance p ON p.RiderClientAssignmentId=s.Id)
 OR EXISTS(SELECT 1 FROM #Close s JOIN ninja.NinjaDailyDeductions p ON p.RiderClientAssignmentId=s.Id)
 THROW 51000,'A replacement now affects financial or performance records.',1;
IF EXISTS(SELECT AccountId FROM #Insert WHERE Status=2 GROUP BY AccountId HAVING COUNT(*)>1)
 OR EXISTS(SELECT Id FROM #Accounts GROUP BY Id HAVING COUNT(*)>1)
 THROW 51000,'Duplicate account selection.',1;
IF (SELECT COUNT(*) FROM #Credentials) NOT IN(0,233)
 OR EXISTS(SELECT AccountId FROM #Credentials GROUP BY AccountId HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM #Credentials c LEFT JOIN #Accounts a ON a.Id=c.AccountId WHERE a.Id IS NULL)
 OR EXISTS(SELECT 1 FROM #Credentials c JOIN app.PlatformAccountCredentialVersions v ON v.PlatformRiderAccountId=c.AccountId)
 THROW 51000,'Credential source or version state changed.',1;

-- Retain mistaken rows as Cancelled, and retain the real previous rider through 13 October.
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),a.Id,a.Status,s.Status,@Now,@Actor,s.Reason,
 (SELECT a.RiderProfileId,a.EffectiveFrom,s.EndDate NewEffectiveTo,s.Status NewStatus FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor
 FROM app.RiderClientAssignments a JOIN #Close s ON s.Id=a.Id;
UPDATE a SET EffectiveTo=s.EndDate,Status=s.Status,EndReason=s.Reason,EndedByUserId=@Actor,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.RiderClientAssignments a JOIN #Close s ON s.Id=a.Id;
IF EXISTS(SELECT 1 FROM #Reopen s LEFT JOIN app.RiderClientAssignments a ON a.Id=s.Id
 LEFT JOIN app.RiderProfiles r ON r.Id=s.RiderId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.Id IS NULL OR a.Status<>3 OR a.EffectiveTo IS NULL OR a.RiderProfileId<>s.RiderId OR a.PlatformRiderAccountId<>s.AccountId
 OR a.EffectiveFrom<>s.FromDate OR e.IqamaNo<>s.UserIqama OR e.IsEmployee=1 OR e.Status<>3 OR e.IsDeleted=1 OR r.IsDeleted=1)
 THROW 51000,'The original rider assignment cannot be restored.',1;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),a.Id,a.Status,2,@Now,@Actor,N'Restored original workbook rider after cancelling the mistaken replacement.',
 (SELECT a.EffectiveTo PreviousEffectiveTo,a.EndReason PreviousEndReason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor
 FROM app.RiderClientAssignments a JOIN #Reopen s ON s.Id=a.Id;
UPDATE a SET EffectiveTo=NULL,Status=2,EndReason=NULL,EndedByUserId=NULL,RiderAccountSlot=s.Slot,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.RiderClientAssignments a JOIN #Reopen s ON s.Id=a.Id;
-- Insert completed histories first, then current handovers. FullTime/Freelancer is independent of PaymentModel.
INSERT app.RiderClientAssignments(Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,
 EffectiveFrom,EffectiveTo,Status,StartReason,EndReason,AssignedByUserId,EndedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT Id,RiderId,@Contract,AccountId,PaymentModel,Slot,FromDate,ToDate,Status,Reason,
 CASE WHEN Status=3 THEN N'Completed assignment from بدلاء الدوره; October return dates confirmed by user.' END,
 @Actor,CASE WHEN Status=3 THEN @Actor END,1,N'Workbook handover date supplied and confirmed by user.',@Now,@Actor,0
 FROM #Insert WHERE Status=3;
INSERT app.RiderClientAssignments(Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,
 EffectiveFrom,Status,StartReason,AssignedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted)
 SELECT Id,RiderId,@Contract,AccountId,PaymentModel,Slot,FromDate,Status,Reason,@Actor,1,
 N'Workbook handover date supplied and confirmed by user.',@Now,@Actor,0 FROM #Insert WHERE Status=2;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),Id,1,2,@Now,@Actor,Reason,
 (SELECT s.Sheet,s.SheetRow,s.UserIqama,s.FromDate,s.ToDate FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor FROM #Insert s;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,CreatedAtUtc,CreatedByUserId)
 SELECT NEWID(),Id,2,3,@Now,@Actor,N'Historical assignment completed on the confirmed return date.',@Now,@Actor FROM #Insert WHERE Status=3;
UPDATE a SET OperatingCityId=s.CityId,SponsorId=s.SponsorId,Status=s.DesiredStatus,
 StatusReason=CASE WHEN s.DesiredStatus IN(1,2) THEN NULL ELSE a.StatusReason END,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM app.PlatformRiderAccounts a JOIN #Accounts s ON s.Id=a.Id
 WHERE a.OperatingCityId<>s.CityId OR ISNULL(a.SponsorId,'00000000-0000-0000-0000-000000000000')<>s.SponsorId OR a.Status<>s.DesiredStatus;
DECLARE @AccountsUpdated int=@@ROWCOUNT;
UPDATE d SET AccountType=s.AccountType,VehicleType=s.VehicleType,DashboardStatus=s.DashboardStatus,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
 FROM chefz.AccountDetails d JOIN #Accounts s ON s.Id=d.PlatformRiderAccountId
 WHERE d.AccountType<>s.AccountType OR d.VehicleType<>s.VehicleType OR d.DashboardStatus<>s.DashboardStatus;
DECLARE @DetailsUpdated int=@@ROWCOUNT;
INSERT app.PlatformAccountCredentialVersions(Id,PlatformRiderAccountId,Ciphertext,Nonce,AuthenticationTag,KeyVersion,
 RotatedAtUtc,RotatedByUserId,RotationReason,CreatedAtUtc,CreatedByUserId)
 SELECT c.Id,c.AccountId,
 CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Ciphertext"))','varbinary(max)'),
 CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Nonce"))','varbinary(12)'),
 CAST(N'' AS xml).value('xs:base64Binary(sql:column("c.Tag"))','varbinary(16)'),1,
 @Now,@Actor,N'Password supplied in Chefz dashboard workbook dated 14 October 2026.',@Now,@Actor FROM #Credentials c;

IF EXISTS(SELECT s.Id FROM #Insert s EXCEPT SELECT a.Id FROM app.RiderClientAssignments a JOIN #Insert s ON s.Id=a.Id
 WHERE a.RiderProfileId=s.RiderId AND a.PlatformRiderAccountId=s.AccountId AND a.EffectiveFrom=s.FromDate
 AND ISNULL(a.EffectiveTo,'9999-12-31')=ISNULL(s.ToDate,'9999-12-31') AND a.Status=s.Status AND a.RiderAccountSlot=s.Slot)
 OR EXISTS(SELECT 1 FROM #Accounts s JOIN app.PlatformRiderAccounts a ON a.Id=s.Id JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id
 WHERE a.Status<>s.DesiredStatus OR a.OperatingCityId<>s.CityId OR a.SponsorId<>s.SponsorId OR d.AccountType<>s.AccountType OR d.VehicleType<>s.VehicleType OR d.DashboardStatus<>s.DashboardStatus)
 THROW 51000,'Import values did not reconcile.',1;
IF EXISTS(SELECT 1 FROM #Insert s JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId=s.AccountId AND a.Id<>s.Id
 WHERE a.IsDeleted=0 AND a.Status IN(2,3) AND a.EffectiveFrom<=ISNULL(s.ToDate,'9999-12-31') AND ISNULL(a.EffectiveTo,'9999-12-31')>=s.FromDate)
 THROW 51000,'Account history overlaps after import.',1;
IF EXISTS(SELECT a.RiderProfileId FROM app.RiderClientAssignments a WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL
 AND a.RiderProfileId IN(SELECT RiderId FROM #Insert WHERE Status=2) GROUP BY a.RiderProfileId HAVING COUNT(*)>2)
 OR EXISTS(SELECT a.RiderProfileId,a.RiderAccountSlot FROM app.RiderClientAssignments a WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL
 AND a.RiderProfileId IN(SELECT RiderId FROM #Insert WHERE Status=2) GROUP BY a.RiderProfileId,a.RiderAccountSlot HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM app.RiderClientAssignments a JOIN app.PlatformRiderAccounts p ON p.Id=a.PlatformRiderAccountId
 WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL AND a.RiderProfileId IN(SELECT RiderId FROM #Insert WHERE Status=2) AND p.ClientPlatformId<>@Platform)
 THROW 51000,'Rider capacity or platform policy failed.',1;
IF EXISTS(SELECT * FROM #BeforePerformance EXCEPT SELECT * FROM chefz.DailyPerformance)
 OR EXISTS(SELECT * FROM chefz.DailyPerformance EXCEPT SELECT * FROM #BeforePerformance)
 OR EXISTS(SELECT * FROM #BeforeSettlements EXCEPT SELECT * FROM chefz.WalletSettlements)
 OR EXISTS(SELECT * FROM chefz.WalletSettlements EXCEPT SELECT * FROM #BeforeSettlements)
 OR EXISTS(SELECT b.* FROM #BeforeAssignments b WHERE b.Id NOT IN(SELECT Id FROM #Close) AND b.Id NOT IN(SELECT Id FROM #Reopen) EXCEPT SELECT a.* FROM app.RiderClientAssignments a WHERE a.Id NOT IN(SELECT Id FROM #Close) AND a.Id NOT IN(SELECT Id FROM #Reopen) AND a.Id NOT IN(SELECT Id FROM #Insert))
 OR EXISTS(SELECT b.* FROM #BeforeAccounts b WHERE b.Id NOT IN(SELECT Id FROM #Accounts) EXCEPT SELECT a.* FROM app.PlatformRiderAccounts a WHERE a.Id NOT IN(SELECT Id FROM #Accounts))
 OR EXISTS(SELECT * FROM #BeforeEmployees EXCEPT SELECT * FROM app.Employees)
 OR EXISTS(SELECT * FROM app.Employees EXCEPT SELECT * FROM #BeforeEmployees)
 THROW 51000,'An unrelated or financial record changed.',1;
DECLARE @Summary nvarchar(max)=(SELECT @AccountsUpdated AccountsUpdated,@DetailsUpdated DashboardDetailsUpdated,
 (SELECT COUNT(*) FROM #Insert WHERE Status=2) ActiveAssignmentsCreated,
 (SELECT COUNT(*) FROM #Reopen) OriginalAssignmentsRestored,
 (SELECT COUNT(*) FROM #Insert WHERE Status=3) CompletedHistoriesCreated,
 (SELECT COUNT(*) FROM #Close WHERE Status=4) MistakenAssignmentsCancelled,
 (SELECT COUNT(*) FROM #Close WHERE Status=3) PreviousAssignmentsEnded,
 (SELECT COUNT(*) FROM #Credentials) PasswordsSaved FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,
 Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),@Actor,'User','ChefzAccountSheetImport','chefz','ClientPlatform',@Platform,@Now,@CorrelationId,
 N'User authorized completed histories, existing-account reconciliation, workbook rider replacements and two-account handovers.',
 (SELECT JSON_QUERY(@PlanJson,'$.summary') ReviewSummary,JSON_QUERY(@PlanJson,'$.closeAssignments') Replacements FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 @Summary,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('chefz','platform-operations');
SELECT @Summary SummaryJson;
