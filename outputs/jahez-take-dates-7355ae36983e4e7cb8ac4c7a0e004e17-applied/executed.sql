SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
IF @@TRANCOUNT=0 THROW 51000,'Explicit transaction required.',1;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(500)=N'User supplied corrected current take dates for 24 Jahez accounts; update handover and linked rider assignment dates.';
SELECT * INTO #Targets FROM OPENJSON(@Plan) WITH(account varchar(100),takeDate date,accountId uniqueidentifier,sessionId uniqueidentifier,assignmentId uniqueidentifier,expectedStartedAtUtc datetimeoffset,expectedEffectiveFrom date);
IF (SELECT COUNT(*) FROM #Targets)<>24 OR (SELECT COUNT(DISTINCT account) FROM #Targets)<>24 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>24
 THROW 51000,'Expected 24 unique requested accounts.',1;
IF EXISTS(SELECT 1 FROM #Targets WHERE takeDate IS NULL OR takeDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')))
 THROW 51000,'Invalid or future take date.',1;
SELECT s.* INTO #SessionsBefore FROM jahez_v2.AccountSessions s WITH(UPDLOCK,HOLDLOCK) WHERE s.Id IN(SELECT sessionId FROM #Targets);
SELECT a.* INTO #AssignmentsBefore FROM app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK) WHERE a.Id IN(SELECT assignmentId FROM #Targets);
IF EXISTS(SELECT 1 FROM #Targets t LEFT JOIN app.PlatformRiderAccounts p ON p.Id=t.accountId LEFT JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
 LEFT JOIN #SessionsBefore s ON s.Id=t.sessionId LEFT JOIN #AssignmentsBefore a ON a.Id=t.assignmentId
 WHERE p.Id IS NULL OR p.ExternalAccountId<>t.account OR p.IsDeleted=1 OR c.Code<>'JAHEZ' OR c.Id IS NULL
 OR s.Id IS NULL OR s.PlatformRiderAccountId<>p.Id OR s.IsDeleted=1 OR s.EndedAtUtc IS NOT NULL OR s.StartedAtUtc<>t.expectedStartedAtUtc
 OR s.RiderClientAssignmentId<>t.assignmentId OR a.Id IS NULL OR a.PlatformRiderAccountId<>p.Id OR a.RiderProfileId<>s.RiderProfileId
 OR a.EffectiveFrom<>t.expectedEffectiveFrom OR a.IsDeleted=1 OR a.Status<>2 OR a.EffectiveTo IS NOT NULL)
 THROW 51000,'Current accounts, sessions or rider assignments changed since review.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s WITH(UPDLOCK,HOLDLOCK) ON s.PlatformRiderAccountId=t.accountId AND s.Id<>t.sessionId
 WHERE s.IsDeleted=0 AND (s.EndedAtUtc IS NULL OR s.EndedAtUtc>TODATETIMEOFFSET(CONVERT(datetime2,t.takeDate),'+03:00')))
 OR EXISTS(SELECT 1 FROM #Targets t JOIN app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK) ON a.PlatformRiderAccountId=t.accountId AND a.Id<>t.assignmentId
 WHERE a.IsDeleted=0 AND a.Status<>4 AND (a.EffectiveTo IS NULL OR a.EffectiveTo>=t.takeDate))
 THROW 51000,'Requested take date overlaps an existing period.',1;
SELECT t.*,CAST(CASE WHEN CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))<>t.takeDate THEN 1 ELSE 0 END AS bit) ChangeSession,
 CAST(CASE WHEN a.EffectiveFrom<>t.takeDate THEN 1 ELSE 0 END AS bit) ChangeAssignment
INTO #Changes FROM #Targets t JOIN #SessionsBefore s ON s.Id=t.sessionId JOIN #AssignmentsBefore a ON a.Id=t.assignmentId
WHERE CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))<>t.takeDate OR a.EffectiveFrom<>t.takeDate;
IF (SELECT COUNT(*) FROM #Changes)<>14 THROW 51000,'Expected 14 corrections and ten already matching dates.',1;
IF EXISTS(SELECT 1 FROM #Changes t JOIN #SessionsBefore s ON s.Id=t.sessionId WHERE s.CommissionPostedThrough IS NOT NULL)
 OR EXISTS(SELECT 1 FROM #Changes t JOIN jahez_v2.Operations o WITH(HOLDLOCK) ON o.HandoverId=t.sessionId WHERE o.ExecutedAtUtc IS NOT NULL)
 THROW 51000,'A corrected period contains posted commission or executed operations; re-evaluate.',1;
IF EXISTS(SELECT 1 FROM #Changes t JOIN jahez_v2.PlatformTransactions x WITH(HOLDLOCK) ON x.HandoverId=t.sessionId WHERE x.OccurredAtUtc<TODATETIMEOFFSET(CONVERT(datetime2,t.takeDate),'+03:00'))
 OR EXISTS(SELECT 1 FROM #Changes t JOIN jahez_v2.DailyOrders x WITH(HOLDLOCK) ON x.HandoverId=t.sessionId WHERE x.Date<t.takeDate)
 OR EXISTS(SELECT 1 FROM #Changes t JOIN jahez_v2.EarningsStatements x WITH(HOLDLOCK) ON x.HandoverId=t.sessionId WHERE x.FromDate<t.takeDate)
 OR EXISTS(SELECT 1 FROM #Changes t JOIN jahez_v2.Operations x WITH(HOLDLOCK) ON x.HandoverId=t.sessionId WHERE x.EffectiveAtUtc<TODATETIMEOFFSET(CONVERT(datetime2,t.takeDate),'+03:00') OR x.ThroughDate<t.takeDate)
 THROW 51000,'Requested take date excludes saved operational or financial records.',1;
UPDATE s SET StartedAtUtc=SWITCHOFFSET(TODATETIMEOFFSET(CONVERT(datetime2,t.takeDate),'+03:00'),'+00:00'),UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Changes t ON t.sessionId=s.Id WHERE t.ChangeSession=1;
DECLARE @SessionsUpdated int=@@ROWCOUNT;
UPDATE a SET EffectiveFrom=t.takeDate,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor FROM app.RiderClientAssignments a JOIN #Changes t ON t.assignmentId=a.Id WHERE t.ChangeAssignment=1;
DECLARE @AssignmentsUpdated int=@@ROWCOUNT;
IF @SessionsUpdated<>(SELECT COUNT(*) FROM #Changes WHERE ChangeSession=1) OR @AssignmentsUpdated<>(SELECT COUNT(*) FROM #Changes WHERE ChangeAssignment=1)
 THROW 51000,'Unexpected correction row counts.',1;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),a.Id,a.Status,a.Status,@Now,@Actor,@Reason,(SELECT t.account Account,a.EffectiveFrom PreviousEffectiveFrom,t.takeDate EffectiveFrom FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),@Now,@Actor
FROM #Changes t JOIN #AssignmentsBefore a ON a.Id=t.assignmentId WHERE t.ChangeAssignment=1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezTakeDateCorrected','jahez','JahezAccountSession',t.sessionId,@Now,@Correlation,@Reason,
(SELECT t.account Account,s.StartedAtUtc FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
(SELECT t.account Account,updated.StartedAtUtc FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Changes t JOIN #SessionsBefore s ON s.Id=t.sessionId JOIN jahez_v2.AccountSessions updated ON updated.Id=s.Id WHERE t.ChangeSession=1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezTakeDateCorrected','jahez','RiderClientAssignment',t.assignmentId,@Now,@Correlation,@Reason,
(SELECT t.account Account,a.EffectiveFrom FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
(SELECT t.account Account,updated.EffectiveFrom FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Changes t JOIN #AssignmentsBefore a ON a.Id=t.assignmentId JOIN app.RiderClientAssignments updated ON updated.Id=a.Id WHERE t.ChangeAssignment=1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('jahez','platform-operations');
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,EndedAtUtc,CommissionStartsOn,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #SessionsBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,EndedAtUtc,CommissionStartsOn,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions WHERE Id IN(SELECT sessionId FROM #Targets))
 OR EXISTS(SELECT Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,EffectiveTo,Status,StartReason,EndReason,OperationalAgreementReference,OperationalAgreementNotes,AssignedByUserId,EndedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #AssignmentsBefore
 EXCEPT SELECT Id,RiderProfileId,ClientContractId,PlatformRiderAccountId,PaymentModel,RiderAccountSlot,EffectiveTo,Status,StartReason,EndReason,OperationalAgreementReference,OperationalAgreementNotes,AssignedByUserId,EndedByUserId,WasBackdated,BackdatedReason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM app.RiderClientAssignments WHERE Id IN(SELECT assignmentId FROM #Targets))
 THROW 51000,'Unrelated session, commission or assignment fields changed.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId JOIN app.RiderClientAssignments a ON a.Id=t.assignmentId WHERE CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))<>t.takeDate OR a.EffectiveFrom<>t.takeDate)
 THROW 51000,'Saved take dates do not match requested dates.',1;
SELECT t.account Account,CONVERT(varchar(10),t.takeDate,23) TakeDate,CASE WHEN c.account IS NULL THEN 'Already matched' ELSE 'Updated' END Result
FROM #Targets t LEFT JOIN #Changes c ON c.account=t.account ORDER BY t.account;
SELECT @SessionsUpdated SessionsUpdated,@AssignmentsUpdated AssignmentsUpdated,24 AccountsVerified,10 AlreadyMatched,
(SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation) AuditEntries;
