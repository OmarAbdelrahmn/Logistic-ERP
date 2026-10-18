param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskCs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$taskConfig.ConnectionStrings.LogisticsDatabase}
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskCs)
$taskTransaction=$null
$taskCommitted=$false
$taskCorrelation='jahez-476832-rider-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation)
[void](New-Item -ItemType Directory -Path $taskOutput)
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
function Read-TaskData([string]$Sql) {
 $taskCommand=$taskConnection.CreateCommand()
 $taskCommand.Transaction=$taskTransaction
 $taskCommand.CommandTimeout=60
 $taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try {
  $taskData=[System.Data.DataSet]::new()
  $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
  try {[void]$taskAdapter.Fill($taskData)} finally {$taskAdapter.Dispose()}
  return ,$taskData
 } finally {$taskCommand.Dispose()}
}
try {
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskData=Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Account uniqueidentifier='196a2a75-de62-4757-80da-0a13beb12b85',
 @Assignment uniqueidentifier='b5378ecf-c2bd-4677-a0df-76db45014f4c',
 @Session uniqueidentifier='59cf1f02-2033-486e-a6c3-9ce3d8ad666f',
 @OldRider uniqueidentifier='01a0bf9d-e0a7-7a3f-8ea0-cda60ce6696d',
 @NewRider uniqueidentifier='01a14f01-aaaa-7343-9513-909f220ac619',
 @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Now datetimeoffset=SYSUTCDATETIME(),
 @Reason nvarchar(700)=N'User requested correcting the current rider of Jahez account 476832 from Iqama 2059179057 to Iqama 2493090191. Existing assignment/session identities, dates and financial baselines retained.';
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=0 AND parent_id IN(OBJECT_ID('app.RiderClientAssignments'),OBJECT_ID('jahez_v2.AccountSessions')))
 THROW 51000,'Review triggers before correction.',1;
IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK) JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE a.Id=@Account AND a.ExternalAccountId='476832' AND a.IsDeleted=0 AND a.Status=2 AND p.Code='JAHEZ')<>1
 THROW 51000,'Account no longer matches.',1;
IF (SELECT COUNT(*) FROM app.Employees e WITH(HOLDLOCK) JOIN app.RiderProfiles r WITH(HOLDLOCK) ON r.EmployeeId=e.Id WHERE e.IqamaNo='2493090191' AND e.IsDeleted=0 AND e.Status=3 AND r.Id=@NewRider AND r.IsDeleted=0)<>1
 THROW 51000,'Target rider no longer matches.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK) WHERE RiderProfileId=@NewRider AND EffectiveTo IS NULL AND IsDeleted=0)
 THROW 51000,'Target rider now has an active assignment; review first.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE RiderProfileId=@NewRider AND EndedAtUtc IS NULL AND IsDeleted=0)
 THROW 51000,'Target rider now has an active Jahez session.',1;
SELECT * INTO #AssignmentsBefore FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #SessionsBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
IF (SELECT COUNT(*) FROM #AssignmentsBefore WHERE PlatformRiderAccountId=@Account AND EffectiveTo IS NULL AND IsDeleted=0)<>1
 OR (SELECT COUNT(*) FROM #AssignmentsBefore WHERE Id=@Assignment AND PlatformRiderAccountId=@Account AND RiderProfileId=@OldRider AND EffectiveTo IS NULL AND Status=2 AND IsDeleted=0)<>1
 THROW 51000,'Current assignment changed.',1;
IF (SELECT COUNT(*) FROM #SessionsBefore WHERE PlatformRiderAccountId=@Account AND EndedAtUtc IS NULL AND IsDeleted=0)<>1
 OR (SELECT COUNT(*) FROM #SessionsBefore WHERE Id=@Session AND RiderClientAssignmentId=@Assignment AND RiderProfileId=@OldRider AND EndedAtUtc IS NULL AND IsDeleted=0)<>1
 THROW 51000,'Current session changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.AccountingEntries WITH(HOLDLOCK) WHERE HandoverId=@Session)
 OR EXISTS(SELECT 1 FROM jahez_v2.Operations WITH(HOLDLOCK) WHERE HandoverId=@Session)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements WITH(HOLDLOCK) WHERE HandoverId=@Session)
 THROW 51000,'Financial activity now exists; review rider attribution before correction.',1;
SELECT * FROM #AssignmentsBefore WHERE Id=@Assignment;
SELECT * FROM #SessionsBefore WHERE Id=@Session;
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('jahez','platform-operations');
UPDATE app.RiderClientAssignments SET RiderProfileId=@NewRider,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE Id=@Assignment;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one assignment update.',1;
UPDATE jahez_v2.AccountSessions SET RiderProfileId=@NewRider,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE Id=@Session;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one session update.',1;
-- Verify every other persisted column and every unrelated assignment/session.
DECLARE @Table nvarchar(200),@Snapshot nvarchar(100),@Target uniqueidentifier,@Columns nvarchar(max),@AllColumns nvarchar(max),@Sql nvarchar(max);
DECLARE taskTables CURSOR LOCAL FAST_FORWARD FOR
 SELECT 'app.RiderClientAssignments','#AssignmentsBefore',@Assignment UNION ALL SELECT 'jahez_v2.AccountSessions','#SessionsBefore',@Session;
OPEN taskTables; FETCH NEXT FROM taskTables INTO @Table,@Snapshot,@Target;
WHILE @@FETCH_STATUS=0 BEGIN
 SELECT @Columns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP(ORDER BY column_id) FROM sys.columns WHERE object_id=OBJECT_ID(@Table) AND name NOT IN('RiderProfileId','UpdatedAtUtc','UpdatedByUserId','RowVersion');
 SELECT @AllColumns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP(ORDER BY column_id) FROM sys.columns WHERE object_id=OBJECT_ID(@Table);
 SET @Sql=N'IF EXISTS(SELECT '+@Columns+' FROM '+@Snapshot+' EXCEPT SELECT '+@Columns+' FROM '+@Table+') OR EXISTS(SELECT '+@Columns+' FROM '+@Table+' EXCEPT SELECT '+@Columns+' FROM '+@Snapshot+') THROW 51000,''Unrelated field changed.'',1;
 IF EXISTS(SELECT '+@AllColumns+' FROM '+@Snapshot+' WHERE Id<>@Target EXCEPT SELECT '+@AllColumns+' FROM '+@Table+' WHERE Id<>@Target) OR EXISTS(SELECT '+@AllColumns+' FROM '+@Table+' WHERE Id<>@Target EXCEPT SELECT '+@AllColumns+' FROM '+@Snapshot+' WHERE Id<>@Target) THROW 51000,''Unrelated row changed.'',1;';
 EXEC sys.sp_executesql @Sql,N'@Target uniqueidentifier',@Target;
 FETCH NEXT FROM taskTables INTO @Table,@Snapshot,@Target;
END;
CLOSE taskTables; DEALLOCATE taskTables;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','JahezCurrentRiderCorrection','jahez','PlatformRiderAccount',@Account,@Now,@Correlation,@Reason,
 (SELECT '476832' Account,'2059179057' IqamaNo,@OldRider RiderProfileId,@Assignment AssignmentId,@Session SessionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 (SELECT '476832' Account,'2493090191' IqamaNo,@NewRider RiderProfileId,@Assignment AssignmentId,@Session SessionId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('jahez','platform-operations');
IF @@ROWCOUNT<>2 THROW 51000,'Expected two dataset versions.',1;
SELECT a.ExternalAccountId,e.IqamaNo,e.FullNameAr,r.Id AssignmentId,h.Id SessionId,r.EffectiveFrom,h.CommissionStartsOn,h.LastSettlementPaymentAtUtc
FROM app.PlatformRiderAccounts a JOIN app.RiderClientAssignments r ON r.PlatformRiderAccountId=a.Id JOIN jahez_v2.AccountSessions h ON h.RiderClientAssignmentId=r.Id AND h.RiderProfileId=r.RiderProfileId
JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId JOIN app.Employees e ON e.Id=rp.EmployeeId WHERE a.Id=@Account AND r.Id=@Assignment AND h.Id=@Session AND e.IqamaNo='2493090191';
'@
 $taskData.WriteXml((Join-Path $taskOutput 'transaction-evidence.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($taskData.Tables[3].Rows.Count -ne 1){throw 'Transaction result mismatch.'}
 if($Apply){
  $taskTransaction.Commit();$taskCommitted=$true;$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT a.ExternalAccountId,e.IqamaNo,e.FullNameAr FROM app.PlatformRiderAccounts a
JOIN app.RiderClientAssignments r ON r.PlatformRiderAccountId=a.Id AND r.EffectiveTo IS NULL AND r.IsDeleted=0
JOIN jahez_v2.AccountSessions h ON h.RiderClientAssignmentId=r.Id AND h.RiderProfileId=r.RiderProfileId AND h.EndedAtUtc IS NULL AND h.IsDeleted=0
JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId JOIN app.Employees e ON e.Id=rp.EmployeeId WHERE a.ExternalAccountId='476832' AND a.IsDeleted=0;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation AND Action='JahezCurrentRiderCorrection';
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows.Count -ne 1 -or $taskVerified.Tables[0].Rows[0].IqamaNo -ne '2493090191' -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 1){throw 'Post-commit verification failed; do not replay.'}
  Write-Output 'COMMITTED AND VERIFIED: Jahez 476832 current rider is 2493090191.'
 }else{
  $taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null
  Write-Output 'DRY RUN VERIFIED AND ROLLED BACK: one assignment and session corrected; dates and other fields preserved.'
 }
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction -and -not $taskCommitted){$taskTransaction.Rollback()};throw}
finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
