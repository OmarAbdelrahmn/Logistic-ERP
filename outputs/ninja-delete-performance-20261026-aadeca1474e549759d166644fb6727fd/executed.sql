SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @date date='20261026';
IF (SELECT COUNT(*) FROM ninja.NinjaDailyPerformance WITH(UPDLOCK,HOLDLOCK) WHERE ReportDate=@date)<>61
 OR (SELECT COUNT(*) FROM ninja.NinjaDailyPerformanceSources WITH(UPDLOCK,HOLDLOCK) WHERE ReportDate=@date)<>61
 OR (SELECT COUNT(*) FROM ninja.NinjaDailyImports WITH(UPDLOCK,HOLDLOCK) WHERE ReportDate=@date AND IsDeductions=0)<>1
 THROW 51000,'Target counts changed; deletion stopped.',1;
IF NOT EXISTS(SELECT 1 FROM ninja.NinjaDailyImports WHERE Id='01a17815-f99f-7aff-a7f0-74a1a87a75e1' AND ReportDate=@date AND IsDeductions=0)
 THROW 51000,'Expected import missing.',1;
SELECT Id INTO #Imports FROM ninja.NinjaDailyImports WHERE ReportDate=@date AND IsDeductions=0;
SELECT Id INTO #Sources FROM ninja.NinjaDailyPerformanceSources WHERE ReportDate=@date;
IF EXISTS(SELECT 1 FROM ninja.NinjaDailyPerformance WHERE (ReportDate=@date AND (ImportId NOT IN(SELECT Id FROM #Imports) OR SourceId NOT IN(SELECT Id FROM #Sources)))
 OR (ReportDate<>@date AND (ImportId IN(SELECT Id FROM #Imports) OR SourceId IN(SELECT Id FROM #Sources))))
 OR EXISTS(SELECT 1 FROM ninja.NinjaDailyPerformanceSources WHERE (ReportDate=@date AND ImportId NOT IN(SELECT Id FROM #Imports)) OR (ReportDate<>@date AND ImportId IN(SELECT Id FROM #Imports)))
 OR EXISTS(SELECT 1 FROM ninja.NinjaDailyDeductions WHERE ImportId IN(SELECT Id FROM #Imports))
 OR EXISTS(SELECT 1 FROM ninja.NinjaDailyDeductionSources WHERE ImportId IN(SELECT Id FROM #Imports))
 THROW 51000,'Unexpected cross-date or deduction dependency.',1;
CREATE TABLE #Scope(TableName nvarchar(200),Predicate nvarchar(1000),DeleteOrder int);
INSERT #Scope VALUES
 ('ninja.AllocationProvenance','SourceId IN(SELECT Id FROM #Sources)',1),
 ('ninja.PerformanceImportArtifacts','ImportId IN(SELECT Id FROM #Imports)',2),
 ('ninja.NinjaDailyPerformance','ReportDate=CONVERT(date,''20261026'')',3),
 ('ninja.NinjaDailyPerformanceSources','Id IN(SELECT Id FROM #Sources)',4),
 ('ninja.NinjaDailyImports','Id IN(SELECT Id FROM #Imports)',5);
CREATE TABLE #Backup(TableName nvarchar(200),RowId uniqueidentifier,RowJson nvarchar(max),IsTarget bit);
DECLARE @table nvarchar(200),@predicate nvarchar(1000),@sql nvarchar(max);
DECLARE backupCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Predicate FROM #Scope;
OPEN backupCursor;FETCH NEXT FROM backupCursor INTO @table,@predicate;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'INSERT #Backup SELECT N'''+@table+N''',t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),CASE WHEN '+@predicate+N' THEN 1 ELSE 0 END FROM '+@table+N' t WITH(UPDLOCK,HOLDLOCK);';
 EXEC sys.sp_executesql @sql;
 FETCH NEXT FROM backupCursor INTO @table,@predicate;
END;CLOSE backupCursor;DEALLOCATE backupCursor;
SELECT TableName,RowId,RowJson FROM #Backup WHERE IsTarget=1 ORDER BY TableName,RowId;
SELECT TableName,COUNT(*) TargetRows FROM #Backup WHERE IsTarget=1 GROUP BY TableName;
-- EXECUTE_MUTATION
DECLARE @table nvarchar(200),@predicate nvarchar(1000),@sql nvarchar(max),@expected int;
DECLARE deleteCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Predicate FROM #Scope ORDER BY DeleteOrder;
OPEN deleteCursor;FETCH NEXT FROM deleteCursor INTO @table,@predicate;
WHILE @@FETCH_STATUS=0 BEGIN
 SELECT @expected=COUNT(*) FROM #Backup WHERE TableName=@table AND IsTarget=1;
 SET @sql=N'DELETE FROM '+@table+N' WHERE '+@predicate+N';IF @@ROWCOUNT<>@expected THROW 51000,''Deletion count mismatch.'',1;';
 EXEC sys.sp_executesql @sql,N'@expected int',@expected;
 FETCH NEXT FROM deleteCursor INTO @table,@predicate;
END;CLOSE deleteCursor;DEALLOCATE deleteCursor;
DECLARE verifyCursor CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,Predicate FROM #Scope;
OPEN verifyCursor;FETCH NEXT FROM verifyCursor INTO @table,@predicate;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'IF EXISTS(SELECT 1 FROM '+@table+N' WHERE '+@predicate+N') THROW 51000,''Target rows remain.'',1;
 IF EXISTS(SELECT RowId,RowJson FROM #Backup WHERE TableName=N'''+@table+N''' AND IsTarget=0
 EXCEPT SELECT t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) FROM '+@table+N' t)
 OR EXISTS(SELECT t.Id,(SELECT t.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES) FROM '+@table+N' t
 EXCEPT SELECT RowId,RowJson FROM #Backup WHERE TableName=N'''+@table+N''' AND IsTarget=0)
 THROW 51000,''Unrelated rows changed.'',1;';
 EXEC sys.sp_executesql @sql;
 FETCH NEXT FROM verifyCursor INTO @table,@predicate;
END;CLOSE verifyCursor;DEALLOCATE verifyCursor;
DECLARE @now datetimeoffset=SYSUTCDATETIME(),@before nvarchar(max),@after nvarchar(max);
SET @before=(SELECT TableName,RowId,JSON_QUERY(RowJson) Data FROM #Backup WHERE IsTarget=1 FOR JSON PATH);
SET @after=N'{"reportDate":"2026-10-26","remainingPerformanceRows":0,"remainingSources":0,"remainingImports":0,"unrelatedRowsVerified":true}';
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),NULL,'System','DeleteRequestedDailyPerformance','ninja','NinjaDailyImport','01a17815-f99f-7aff-a7f0-74a1a87a75e1',@now,@Correlation,
 N'User explicitly requested: delete all the performance for day 26-10. Scope: Ninja report date 2026-10-26.',@before,@after,'CodexAuthorizedMaintenance',1,@now,NULL);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL
WHERE ModuleKey IN('ninja','platform-operations') AND IsDeleted=0;
IF @@ROWCOUNT<>2 THROW 51000,'Dataset version count mismatch.',1;
SELECT TableName,COUNT(*) DeletedRows FROM #Backup WHERE IsTarget=1 GROUP BY TableName;
SELECT (SELECT COUNT(*) FROM ninja.NinjaDailyPerformance WHERE ReportDate='20261026') RemainingPerformance,
 (SELECT COUNT(*) FROM ninja.NinjaDailyPerformanceSources WHERE ReportDate='20261026') RemainingSources,
 (SELECT COUNT(*) FROM ninja.NinjaDailyImports WHERE ReportDate='20261026' AND IsDeductions=0) RemainingImports;
