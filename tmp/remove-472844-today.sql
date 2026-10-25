SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
IF @@TRANCOUNT=0 THROW 51000,'Explicit transaction required.',1;
DECLARE @a uniqueidentifier='49e3f36a-1bcc-41e8-b960-cea041725ffa',
 @h uniqueidentifier='01a163a8-04f9-7dbb-99cb-87bcbdfb4380',
 @r uniqueidentifier='01a163a8-04f9-7562-a177-4e80dacfaf55',
 @m uniqueidentifier='01a163a8-0561-7091-9ae5-57febf8c323c',
 @f uniqueidentifier='01a163a8-0554-7ea3-b888-0a7a5cc7d87d',
 @o uniqueidentifier='01a16443-4c02-7427-8c84-64d5293fc937',
 @now datetimeoffset=SYSUTCDATETIME(),
 @reason nvarchar(1000)=N'User explicitly requested fully removing account 472844 assignment dated 22 October 2026 and its 200 receipt, including deducting 200 from money totals. Exact removed records archived locally and in audit; earlier history preserved.';
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@Correlation) THROW 51000,'Already applied.',1;
IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK) WHERE Id=@a AND ExternalAccountId='472844' AND Status=1 AND IsDeleted=0)<>1 THROW 51000,'Account changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.State WITH(UPDLOCK,HOLDLOCK) WHERE IsActive=1 OR ActivatedAtUtc IS NOT NULL) THROW 51000,'Treasury activated; review required.',1;
IF (SELECT COUNT(*) FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK) WHERE PlatformRiderAccountId=@a AND EffectiveFrom='20261022' AND IsDeleted=0)<>1
 OR (SELECT COUNT(*) FROM app.RiderClientAssignments WHERE Id=@r AND Status=3 AND EffectiveTo='20261022' AND RowVersion=0x0000000000036F74)<>1 THROW 51000,'Assignment changed.',1;
IF (SELECT COUNT(*) FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id=@h AND RiderClientAssignmentId=@r AND RowVersion=0x0000000000036F6E)<>1 THROW 51000,'Session changed.',1;
IF (SELECT COUNT(*) FROM jahez_v2.MoneyMovements WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)<>1
 OR (SELECT COUNT(*) FROM jahez_v2.MoneyMovements WHERE Id=@m AND HandoverId=@h AND Amount=200 AND Fund=1 AND Direction=1 AND Purpose=3 AND OperationId IS NULL AND TransferredDebtId IS NULL AND CONVERT(date,SWITCHOFFSET(OccurredAtUtc,'+03:00'))='20261022')<>1 THROW 51000,'Receipt changed.',1;
IF (SELECT COUNT(*) FROM jahez_v2.Operations WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)<>1
 OR (SELECT COUNT(*) FROM jahez_v2.Operations WHERE Id=@o AND HandoverId=@h AND Net=0 AND Commission=0 AND Remaining=0 AND RowVersion=0x0000000000036F71)<>1 THROW 51000,'Operation changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandoverMovements WITH(UPDLOCK,HOLDLOCK) WHERE MoneyMovementId=@m)
 OR EXISTS(SELECT 1 FROM jahez_v3.Postings WITH(UPDLOCK,HOLDLOCK) WHERE MoneyMovementId=@m)
 OR EXISTS(SELECT 1 FROM jahez_v2.Requests WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h OR OperationId=@o)
 OR EXISTS(SELECT 1 FROM jahez_v2.TransferredDebts WITH(UPDLOCK,HOLDLOCK) WHERE OperationId=@o)
 OR EXISTS(SELECT 1 FROM jahez_v2.DailyOrders WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)
 OR EXISTS(SELECT 1 FROM jahez_v2.PlatformTransactions WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)
 OR EXISTS(SELECT 1 FROM jahez_v2.EarningsStatements WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)
 OR EXISTS(SELECT 1 FROM jahez_v2.CommissionPolicies WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)
 OR EXISTS(SELECT 1 FROM jahez_v2.SettlementReminders WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@h)
 THROW 51000,'Unexpected linked business data.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountDayUsages WITH(UPDLOCK,HOLDLOCK) WHERE RiderClientAssignmentId=@r AND (OrdersCount IS NOT NULL OR WorkingHours IS NOT NULL OR CollectedAmount IS NOT NULL OR DeductionsAmount IS NOT NULL OR TotalValidOffers IS NOT NULL OR DeclinedOffers IS NOT NULL OR MissedOffers IS NOT NULL)) THROW 51000,'Day contains work data.',1;

-- Snapshot every affected table and compare all unrelated rows after the deletion.
CREATE TABLE #Scope(TableName nvarchar(200),Predicate nvarchar(2000),Expected int);
INSERT #Scope VALUES
 ('app.RiderClientAssignments','Id=@r',1),
 ('app.RiderAssignmentEvents','RiderClientAssignmentId=@r',2),
 ('app.PlatformAccountDayUsages','RiderClientAssignmentId=@r',1),
 ('jahez_v2.AccountSessions','Id=@h',1),
 ('jahez_v2.AccountFees','HandoverId=@h',1),
 ('jahez_v2.AccountingEntries','HandoverId=@h',3),
 ('jahez_v2.MoneyMovements','Id=@m',1),
 ('jahez_v2.Operations','Id=@o',1),
 ('jahez_v3.Ownership','SourceId IN(@m,@f,@o)',3),
 ('jahez_v2.CommandReceipts','Id IN(''01a16443-4c4f-7b9f-be2b-3b2d554a708e'',''01a16443-5f28-7aa3-b313-a86c75c6b1e1'',''01a163a8-0865-778e-8cbd-ce6a407423da'')',3);
CREATE TABLE #Backup(TableName nvarchar(200),RowId uniqueidentifier,RowJson nvarchar(max),IsTarget bit);
DECLARE @table nvarchar(200),@predicate nvarchar(2000),@expected int,@sql nvarchar(max),@params nvarchar(1000)=N'@r uniqueidentifier,@h uniqueidentifier,@m uniqueidentifier,@f uniqueidentifier,@o uniqueidentifier,@expected int';
DECLARE scopeCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Predicate,Expected FROM #Scope;
OPEN scopeCursor; FETCH NEXT FROM scopeCursor INTO @table,@predicate,@expected;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'IF (SELECT COUNT(*) FROM '+@table+' WITH(UPDLOCK,HOLDLOCK) WHERE '+@predicate+')<>@expected THROW 51000,''Unexpected target count.'',1;
 INSERT #Backup SELECT '''+@table+''',t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),CASE WHEN '+@predicate+' THEN 1 ELSE 0 END FROM '+@table+' t WITH(UPDLOCK,HOLDLOCK);';
 EXEC sys.sp_executesql @sql,@params,@r,@h,@m,@f,@o,@expected;
 FETCH NEXT FROM scopeCursor INTO @table,@predicate,@expected;
END; CLOSE scopeCursor; DEALLOCATE scopeCursor;
DECLARE @before nvarchar(max)=(SELECT TableName,RowId,JSON_QUERY(RowJson) Data FROM #Backup WHERE IsTarget=1 FOR JSON PATH);
SELECT TableName,RowId,RowJson FROM #Backup WHERE IsTarget=1 ORDER BY TableName,RowId;
DECLARE @cashBefore decimal(18,2)=(SELECT COALESCE(SUM(CASE Direction WHEN 1 THEN Amount ELSE -Amount END),0) FROM jahez_v2.MoneyMovements WHERE Fund=1),
 @allBefore decimal(18,2)=(SELECT COALESCE(SUM(CASE Direction WHEN 1 THEN Amount ELSE -Amount END),0) FROM jahez_v2.MoneyMovements),
 @outstandingBefore decimal(18,2)=(SELECT COALESCE(SUM(CASE m.Direction WHEN 1 THEN m.Amount ELSE -m.Amount END),0) FROM jahez_v2.MoneyMovements m WHERE Fund=1 AND NOT EXISTS(SELECT 1 FROM jahez_v2.CashboxHandoverMovements l JOIN jahez_v2.CashboxHandovers ch ON ch.Id=l.CashboxHandoverId AND ch.Status=2 AND ch.IsDeleted=0 WHERE l.MoneyMovementId=m.Id));

-- Preserve command keys as retired markers so stale retries cannot recreate removed data.
UPDATE jahez_v2.CommandReceipts SET PayloadHash='retired:472844:20261022',ResultJson='{}' WHERE Id IN(SELECT RowId FROM #Backup WHERE TableName='jahez_v2.CommandReceipts' AND IsTarget=1);
IF @@ROWCOUNT<>3 THROW 51000,'Receipt retirement mismatch.',1;
-- Only unposted ownership metadata is removed; protection is restored within this transaction.
IF NOT EXISTS(SELECT 1 FROM sys.triggers WHERE name='TR_Ownership_Immutable' AND parent_id=OBJECT_ID('jahez_v3.Ownership') AND is_disabled=0) THROW 51000,'Ownership protection changed.',1;
DISABLE TRIGGER jahez_v3.TR_Ownership_Immutable ON jahez_v3.Ownership;
DELETE jahez_v3.Ownership WHERE SourceId IN(@m,@f,@o);
IF @@ROWCOUNT<>3 THROW 51000,'Ownership count mismatch.',1;
ENABLE TRIGGER jahez_v3.TR_Ownership_Immutable ON jahez_v3.Ownership;
DELETE jahez_v2.AccountingEntries WHERE HandoverId=@h;
IF @@ROWCOUNT<>3 THROW 51000,'Entry count mismatch.',1;
DELETE jahez_v2.MoneyMovements WHERE Id=@m;
IF @@ROWCOUNT<>1 THROW 51000,'Movement count mismatch.',1;
DELETE jahez_v2.AccountFees WHERE HandoverId=@h;
IF @@ROWCOUNT<>1 THROW 51000,'Fee count mismatch.',1;
DELETE jahez_v2.Operations WHERE Id=@o;
IF @@ROWCOUNT<>1 THROW 51000,'Operation count mismatch.',1;
DELETE app.PlatformAccountDayUsages WHERE RiderClientAssignmentId=@r;
IF @@ROWCOUNT<>1 THROW 51000,'Usage count mismatch.',1;
DELETE app.RiderAssignmentEvents WHERE RiderClientAssignmentId=@r;
IF @@ROWCOUNT<>2 THROW 51000,'Event count mismatch.',1;
DELETE jahez_v2.AccountSessions WHERE Id=@h;
IF @@ROWCOUNT<>1 THROW 51000,'Session count mismatch.',1;
DELETE app.RiderClientAssignments WHERE Id=@r;
IF @@ROWCOUNT<>1 THROW 51000,'Assignment count mismatch.',1;

DECLARE verifyCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Predicate,Expected FROM #Scope;
OPEN verifyCursor; FETCH NEXT FROM verifyCursor INTO @table,@predicate,@expected;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'IF EXISTS(SELECT RowId,RowJson FROM #Backup WHERE TableName='''+@table+''' AND IsTarget=0 EXCEPT SELECT t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM '+@table+' t WHERE NOT('+@predicate+')) OR EXISTS(SELECT t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM '+@table+' t WHERE NOT('+@predicate+') EXCEPT SELECT RowId,RowJson FROM #Backup WHERE TableName='''+@table+''' AND IsTarget=0) THROW 51000,''Unrelated data changed.'',1;';
 IF @table<>'jahez_v2.CommandReceipts' SET @sql+=N'IF EXISTS(SELECT 1 FROM '+@table+' WHERE '+@predicate+') THROW 51000,''Target still exists.'',1;';
 EXEC sys.sp_executesql @sql,@params,@r,@h,@m,@f,@o,@expected;
 FETCH NEXT FROM verifyCursor INTO @table,@predicate,@expected;
END; CLOSE verifyCursor; DEALLOCATE verifyCursor;
DECLARE @cashAfter decimal(18,2)=(SELECT COALESCE(SUM(CASE Direction WHEN 1 THEN Amount ELSE -Amount END),0) FROM jahez_v2.MoneyMovements WHERE Fund=1),
 @allAfter decimal(18,2)=(SELECT COALESCE(SUM(CASE Direction WHEN 1 THEN Amount ELSE -Amount END),0) FROM jahez_v2.MoneyMovements),
 @outstandingAfter decimal(18,2)=(SELECT COALESCE(SUM(CASE m.Direction WHEN 1 THEN m.Amount ELSE -m.Amount END),0) FROM jahez_v2.MoneyMovements m WHERE Fund=1 AND NOT EXISTS(SELECT 1 FROM jahez_v2.CashboxHandoverMovements l JOIN jahez_v2.CashboxHandovers ch ON ch.Id=l.CashboxHandoverId AND ch.Status=2 AND ch.IsDeleted=0 WHERE l.MoneyMovementId=m.Id));
IF @cashBefore-@cashAfter<>200 OR @allBefore-@allAfter<>200 OR @outstandingBefore-@outstandingAfter<>200 THROW 51000,'Money total verification failed.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE name='TR_Ownership_Immutable' AND parent_id=OBJECT_ID('jahez_v3.Ownership') AND is_disabled=1) THROW 51000,'Protection not restored.',1;
DECLARE @after nvarchar(max)=(SELECT '472844' AccountId,@cashBefore CashBefore,@cashAfter CashAfter,@allBefore TotalBefore,@allAfter TotalAfter,@outstandingBefore OutstandingBefore,@outstandingAfter OutstandingAfter,CAST(1 AS bit) EarlierHistoryPreserved FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),NULL,'System','RemoveMistakenAssignmentAndReceipt','jahez','PlatformRiderAccount',@a,@now,@Correlation,@reason,@before,@after,'CodexAuthorizedMaintenance',1,@now,NULL);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey IN('jahez','platform-operations');
IF @@ROWCOUNT<>2 THROW 51000,'Dataset count mismatch.',1;
SELECT @cashBefore CashBefore,@cashAfter CashAfter,@allBefore TotalBefore,@allAfter TotalAfter,@outstandingBefore OutstandingBefore,@outstandingAfter OutstandingAfter;
