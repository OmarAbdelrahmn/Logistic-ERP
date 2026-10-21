SET NOCOUNT ON;
SELECT * INTO #DateArchive FROM OPENJSON(@Plan) WITH(
 id uniqueidentifier,accountId uniqueidentifier,riderId uniqueidentifier,assignmentId uniqueidentifier,
 startedAt datetimeoffset,endedAt datetimeoffset,startsOn date,postedThrough date,lastPayment datetimeoffset);
IF (SELECT COUNT(*) FROM #DateArchive)<>313 OR (SELECT COUNT(DISTINCT id) FROM #DateArchive)<>313
 THROW 51000,'Unexpected archive identities.',1;SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'jahez-restore-pre-reset-dates-20261020',
 @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @lock<0 THROW 51000,'Could not acquire date restoration lock.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WITH(HOLDLOCK) WHERE CorrelationId=@Correlation)
 THROW 51000,'Date restoration already committed; do not replay.',1;
SELECT * INTO #DatesBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #DatesDatasetBefore FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey=N'jahez';
IF (SELECT COUNT(*) FROM #DatesDatasetBefore)<>1 THROW 51000,'Missing or duplicate Jahez dataset version.',1;
IF NOT EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019')
 THROW 51000,'The reviewed reset audit is missing.',1;
IF EXISTS(SELECT 1 FROM #DateArchive a LEFT JOIN #DatesBefore s ON s.Id=a.id
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=a.accountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR s.PlatformRiderAccountId<>a.accountId OR s.RiderProfileId<>a.riderId
 OR s.RiderClientAssignmentId<>a.assignmentId OR s.StartedAtUtc<>a.startedAt
 OR ISNULL(s.EndedAtUtc,'19000101')<>ISNULL(a.endedAt,'19000101') OR s.IsDeleted<>0
 OR p.Id IS NULL OR c.Code<>N'JAHEZ')
 THROW 51000,'An archived session identity or assignment changed.',1;
IF EXISTS(SELECT 1 FROM #DateArchive a JOIN #DatesBefore s ON s.Id=a.id
 WHERE s.CommissionStartsOn<>'20261020' OR s.CommissionPostedThrough IS NOT NULL OR s.LastSettlementPaymentAtUtc IS NOT NULL)
 THROW 51000,'A session no longer has the reviewed reset dates. Reinspect.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Operations o WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=o.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez_v2.AccountingEntries e WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=e.HandoverId WHERE e.Bucket=3)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements m WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=m.HandoverId)
 THROW 51000,'New financial work exists for a target session. Reinspect.',1;
-- Four sessions have documented historical baseline corrections preceding their operational handover.
-- Restore those archive values exactly; do not reinterpret them as new handover dates.
IF EXISTS(SELECT 1 FROM #DateArchive WHERE startsOn IS NULL
 OR postedThrough>CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+03:00'))
 OR lastPayment>SYSDATETIMEOFFSET())
 THROW 51000,'Invalid archived financial dates.',1;
SELECT a.id INTO #DatesChanged FROM #DateArchive a JOIN #DatesBefore s ON s.Id=a.id
 WHERE EXISTS(SELECT a.startsOn,a.postedThrough,a.lastPayment
 EXCEPT SELECT s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc);
IF (SELECT COUNT(*) FROM #DatesChanged)<>309 THROW 51000,'Unexpected number of changed sessions.',1;SELECT * INTO #DatesKeep0 FROM [app].[Notifications] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep1 FROM [app].[PlatformRiderAccounts] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep2 FROM [app].[RiderClientAssignments] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep3 FROM [jahez_v2].[AccountFees] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep4 FROM [jahez_v2].[AccountingEntries] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep5 FROM [jahez_v2].[CashboxHandoverMovements] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep6 FROM [jahez_v2].[CashboxHandovers] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep7 FROM [jahez_v2].[CommandReceipts] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep8 FROM [jahez_v2].[CommissionPolicies] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep9 FROM [jahez_v2].[DailyOrders] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep10 FROM [jahez_v2].[EarningsStatements] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep11 FROM [jahez_v2].[ImportBatches] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep12 FROM [jahez_v2].[ImportFiles] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep13 FROM [jahez_v2].[ImportRows] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep14 FROM [jahez_v2].[MoneyMovements] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep15 FROM [jahez_v2].[Operations] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep16 FROM [jahez_v2].[PlatformTransactions] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep17 FROM [jahez_v2].[RequestDecisions] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep18 FROM [jahez_v2].[Requests] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep19 FROM [jahez_v2].[SettlementReminders] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep20 FROM [jahez_v2].[TransferredDebts] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep21 FROM [jahez_v3].[BalanceVerifications] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep22 FROM [jahez_v3].[HandoverLines] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep23 FROM [jahez_v3].[HandoverPostings] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep24 FROM [jahez_v3].[Ownership] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep25 FROM [jahez_v3].[PetroEvidence] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep26 FROM [jahez_v3].[Postings] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep27 FROM [jahez_v3].[State] WITH(HOLDLOCK);
SELECT * INTO #DatesKeep28 FROM [jahez_v3].[TreasurySubAccounts] WITH(HOLDLOCK);
SELECT * INTO #DatesPriorAudit FROM audit.AuditEntries WITH(HOLDLOCK);
DECLARE @now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
UPDATE s SET CommissionStartsOn=a.startsOn,CommissionPostedThrough=a.postedThrough,
 LastSettlementPaymentAtUtc=a.lastPayment,UpdatedAtUtc=@now,UpdatedByUserId=NULL
 FROM jahez_v2.AccountSessions s JOIN #DateArchive a ON a.id=s.Id JOIN #DatesChanged d ON d.id=s.Id;
IF @@ROWCOUNT<>309 THROW 51000,'Date restoration affected an unexpected number of sessions.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,
 CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #DatesBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,
 CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #DatesBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'Unrelated session fields changed.',1;
IF EXISTS(SELECT s.* FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #DatesChanged d WHERE d.id=s.Id)
 EXCEPT SELECT b.* FROM #DatesBefore b WHERE NOT EXISTS(SELECT 1 FROM #DatesChanged d WHERE d.id=b.Id))
 THROW 51000,'An untargeted session changed.',1;
IF EXISTS(SELECT a.id,a.startsOn,a.postedThrough,a.lastPayment FROM #DateArchive a
 EXCEPT SELECT s.Id,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc
 FROM jahez_v2.AccountSessions s JOIN #DateArchive a ON a.id=s.Id)
 THROW 51000,'Restored dates do not exactly match the archive.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,
 CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,N'System',N'JahezPreResetDatesRestored',N'jahez',N'JahezAccountSession',s.Id,@now,
 @Correlation,N'User explicitly requested restoration of all last settlement and commission dates cleared by the 19 October reset. Exact pre-reset values restored; no removed financial transactions restored and no payment invented.',
 (SELECT b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc,@SourceFile ArchiveSource,@SourceHash ArchiveSha256
 FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),N'CodexAuthorizedMaintenance',1,@now,NULL
 FROM jahez_v2.AccountSessions s JOIN #DatesBefore b ON b.Id=s.Id JOIN #DatesChanged d ON d.id=s.Id;
IF @@ROWCOUNT<>309 THROW 51000,'Restoration audit count mismatch.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey=N'jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Dataset invalidation failed.',1;
SELECT (SELECT COUNT(*) FROM #DateArchive) MatchedArchivedSessions,(SELECT COUNT(*) FROM #DatesChanged) UpdatedSessions,
 (SELECT COUNT(*) FROM #DateArchive WHERE lastPayment IS NOT NULL) RestoredLastSettlements,
 (SELECT COUNT(*) FROM #DateArchive WHERE postedThrough IS NOT NULL) RestoredLastCommissions,
 (SELECT COUNT(*) FROM #DatesBefore b WHERE NOT EXISTS(SELECT 1 FROM #DateArchive a WHERE a.id=b.Id)) PreservedNewSessions,
 (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation) AuditEntries;IF EXISTS(SELECT * FROM #DatesKeep0 EXCEPT SELECT * FROM [app].[Notifications]) OR EXISTS(SELECT * FROM [app].[Notifications] EXCEPT SELECT * FROM #DatesKeep0) THROW 51000,'Protected table changed: [app].[Notifications]',1;
IF EXISTS(SELECT * FROM #DatesKeep1 EXCEPT SELECT * FROM [app].[PlatformRiderAccounts]) OR EXISTS(SELECT * FROM [app].[PlatformRiderAccounts] EXCEPT SELECT * FROM #DatesKeep1) THROW 51000,'Protected table changed: [app].[PlatformRiderAccounts]',1;
IF EXISTS(SELECT * FROM #DatesKeep2 EXCEPT SELECT * FROM [app].[RiderClientAssignments]) OR EXISTS(SELECT * FROM [app].[RiderClientAssignments] EXCEPT SELECT * FROM #DatesKeep2) THROW 51000,'Protected table changed: [app].[RiderClientAssignments]',1;
IF EXISTS(SELECT * FROM #DatesKeep3 EXCEPT SELECT * FROM [jahez_v2].[AccountFees]) OR EXISTS(SELECT * FROM [jahez_v2].[AccountFees] EXCEPT SELECT * FROM #DatesKeep3) THROW 51000,'Protected table changed: [jahez_v2].[AccountFees]',1;
IF EXISTS(SELECT * FROM #DatesKeep4 EXCEPT SELECT * FROM [jahez_v2].[AccountingEntries]) OR EXISTS(SELECT * FROM [jahez_v2].[AccountingEntries] EXCEPT SELECT * FROM #DatesKeep4) THROW 51000,'Protected table changed: [jahez_v2].[AccountingEntries]',1;
IF EXISTS(SELECT * FROM #DatesKeep5 EXCEPT SELECT * FROM [jahez_v2].[CashboxHandoverMovements]) OR EXISTS(SELECT * FROM [jahez_v2].[CashboxHandoverMovements] EXCEPT SELECT * FROM #DatesKeep5) THROW 51000,'Protected table changed: [jahez_v2].[CashboxHandoverMovements]',1;
IF EXISTS(SELECT * FROM #DatesKeep6 EXCEPT SELECT * FROM [jahez_v2].[CashboxHandovers]) OR EXISTS(SELECT * FROM [jahez_v2].[CashboxHandovers] EXCEPT SELECT * FROM #DatesKeep6) THROW 51000,'Protected table changed: [jahez_v2].[CashboxHandovers]',1;
IF EXISTS(SELECT * FROM #DatesKeep7 EXCEPT SELECT * FROM [jahez_v2].[CommandReceipts]) OR EXISTS(SELECT * FROM [jahez_v2].[CommandReceipts] EXCEPT SELECT * FROM #DatesKeep7) THROW 51000,'Protected table changed: [jahez_v2].[CommandReceipts]',1;
IF EXISTS(SELECT * FROM #DatesKeep8 EXCEPT SELECT * FROM [jahez_v2].[CommissionPolicies]) OR EXISTS(SELECT * FROM [jahez_v2].[CommissionPolicies] EXCEPT SELECT * FROM #DatesKeep8) THROW 51000,'Protected table changed: [jahez_v2].[CommissionPolicies]',1;
IF EXISTS(SELECT * FROM #DatesKeep9 EXCEPT SELECT * FROM [jahez_v2].[DailyOrders]) OR EXISTS(SELECT * FROM [jahez_v2].[DailyOrders] EXCEPT SELECT * FROM #DatesKeep9) THROW 51000,'Protected table changed: [jahez_v2].[DailyOrders]',1;
IF EXISTS(SELECT * FROM #DatesKeep10 EXCEPT SELECT * FROM [jahez_v2].[EarningsStatements]) OR EXISTS(SELECT * FROM [jahez_v2].[EarningsStatements] EXCEPT SELECT * FROM #DatesKeep10) THROW 51000,'Protected table changed: [jahez_v2].[EarningsStatements]',1;
IF EXISTS(SELECT * FROM #DatesKeep11 EXCEPT SELECT * FROM [jahez_v2].[ImportBatches]) OR EXISTS(SELECT * FROM [jahez_v2].[ImportBatches] EXCEPT SELECT * FROM #DatesKeep11) THROW 51000,'Protected table changed: [jahez_v2].[ImportBatches]',1;
IF EXISTS(SELECT * FROM #DatesKeep12 EXCEPT SELECT * FROM [jahez_v2].[ImportFiles]) OR EXISTS(SELECT * FROM [jahez_v2].[ImportFiles] EXCEPT SELECT * FROM #DatesKeep12) THROW 51000,'Protected table changed: [jahez_v2].[ImportFiles]',1;
IF EXISTS(SELECT * FROM #DatesKeep13 EXCEPT SELECT * FROM [jahez_v2].[ImportRows]) OR EXISTS(SELECT * FROM [jahez_v2].[ImportRows] EXCEPT SELECT * FROM #DatesKeep13) THROW 51000,'Protected table changed: [jahez_v2].[ImportRows]',1;
IF EXISTS(SELECT * FROM #DatesKeep14 EXCEPT SELECT * FROM [jahez_v2].[MoneyMovements]) OR EXISTS(SELECT * FROM [jahez_v2].[MoneyMovements] EXCEPT SELECT * FROM #DatesKeep14) THROW 51000,'Protected table changed: [jahez_v2].[MoneyMovements]',1;
IF EXISTS(SELECT * FROM #DatesKeep15 EXCEPT SELECT * FROM [jahez_v2].[Operations]) OR EXISTS(SELECT * FROM [jahez_v2].[Operations] EXCEPT SELECT * FROM #DatesKeep15) THROW 51000,'Protected table changed: [jahez_v2].[Operations]',1;
IF EXISTS(SELECT * FROM #DatesKeep16 EXCEPT SELECT * FROM [jahez_v2].[PlatformTransactions]) OR EXISTS(SELECT * FROM [jahez_v2].[PlatformTransactions] EXCEPT SELECT * FROM #DatesKeep16) THROW 51000,'Protected table changed: [jahez_v2].[PlatformTransactions]',1;
IF EXISTS(SELECT * FROM #DatesKeep17 EXCEPT SELECT * FROM [jahez_v2].[RequestDecisions]) OR EXISTS(SELECT * FROM [jahez_v2].[RequestDecisions] EXCEPT SELECT * FROM #DatesKeep17) THROW 51000,'Protected table changed: [jahez_v2].[RequestDecisions]',1;
IF EXISTS(SELECT * FROM #DatesKeep18 EXCEPT SELECT * FROM [jahez_v2].[Requests]) OR EXISTS(SELECT * FROM [jahez_v2].[Requests] EXCEPT SELECT * FROM #DatesKeep18) THROW 51000,'Protected table changed: [jahez_v2].[Requests]',1;
IF EXISTS(SELECT * FROM #DatesKeep19 EXCEPT SELECT * FROM [jahez_v2].[SettlementReminders]) OR EXISTS(SELECT * FROM [jahez_v2].[SettlementReminders] EXCEPT SELECT * FROM #DatesKeep19) THROW 51000,'Protected table changed: [jahez_v2].[SettlementReminders]',1;
IF EXISTS(SELECT * FROM #DatesKeep20 EXCEPT SELECT * FROM [jahez_v2].[TransferredDebts]) OR EXISTS(SELECT * FROM [jahez_v2].[TransferredDebts] EXCEPT SELECT * FROM #DatesKeep20) THROW 51000,'Protected table changed: [jahez_v2].[TransferredDebts]',1;
IF EXISTS(SELECT * FROM #DatesKeep21 EXCEPT SELECT * FROM [jahez_v3].[BalanceVerifications]) OR EXISTS(SELECT * FROM [jahez_v3].[BalanceVerifications] EXCEPT SELECT * FROM #DatesKeep21) THROW 51000,'Protected table changed: [jahez_v3].[BalanceVerifications]',1;
IF EXISTS(SELECT * FROM #DatesKeep22 EXCEPT SELECT * FROM [jahez_v3].[HandoverLines]) OR EXISTS(SELECT * FROM [jahez_v3].[HandoverLines] EXCEPT SELECT * FROM #DatesKeep22) THROW 51000,'Protected table changed: [jahez_v3].[HandoverLines]',1;
IF EXISTS(SELECT * FROM #DatesKeep23 EXCEPT SELECT * FROM [jahez_v3].[HandoverPostings]) OR EXISTS(SELECT * FROM [jahez_v3].[HandoverPostings] EXCEPT SELECT * FROM #DatesKeep23) THROW 51000,'Protected table changed: [jahez_v3].[HandoverPostings]',1;
IF EXISTS(SELECT * FROM #DatesKeep24 EXCEPT SELECT * FROM [jahez_v3].[Ownership]) OR EXISTS(SELECT * FROM [jahez_v3].[Ownership] EXCEPT SELECT * FROM #DatesKeep24) THROW 51000,'Protected table changed: [jahez_v3].[Ownership]',1;
IF EXISTS(SELECT * FROM #DatesKeep25 EXCEPT SELECT * FROM [jahez_v3].[PetroEvidence]) OR EXISTS(SELECT * FROM [jahez_v3].[PetroEvidence] EXCEPT SELECT * FROM #DatesKeep25) THROW 51000,'Protected table changed: [jahez_v3].[PetroEvidence]',1;
IF EXISTS(SELECT * FROM #DatesKeep26 EXCEPT SELECT * FROM [jahez_v3].[Postings]) OR EXISTS(SELECT * FROM [jahez_v3].[Postings] EXCEPT SELECT * FROM #DatesKeep26) THROW 51000,'Protected table changed: [jahez_v3].[Postings]',1;
IF EXISTS(SELECT * FROM #DatesKeep27 EXCEPT SELECT * FROM [jahez_v3].[State]) OR EXISTS(SELECT * FROM [jahez_v3].[State] EXCEPT SELECT * FROM #DatesKeep27) THROW 51000,'Protected table changed: [jahez_v3].[State]',1;
IF EXISTS(SELECT * FROM #DatesKeep28 EXCEPT SELECT * FROM [jahez_v3].[TreasurySubAccounts]) OR EXISTS(SELECT * FROM [jahez_v3].[TreasurySubAccounts] EXCEPT SELECT * FROM #DatesKeep28) THROW 51000,'Protected table changed: [jahez_v3].[TreasurySubAccounts]',1;

