param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskBackupPath=Join-Path $taskRoot 'outputs/jahez-money-history-reset-20261017T124303649Z-applied/jahez_v2.AccountSessions-before.xml'
$taskBackup=[xml](Get-Content -LiteralPath $taskBackupPath -Raw)
$taskOriginal=@{};foreach($taskRow in $taskBackup.NewDataSet.Table){$taskOriginal[[string]$taskRow.Id]=$taskRow}
$taskInspection=Get-Content (Join-Path $taskRoot 'tmp/jahez-all-commission-inspection.json') -Raw|ConvertFrom-Json
$taskTargets=@(foreach($taskRow in $taskInspection|Where-Object Category -eq 'RecoverableSettlement'){
 $taskOld=$taskOriginal[$taskRow.SessionId]
 if(-not $taskOld -or [string]$taskOld.PlatformRiderAccountId -ne $taskRow.AccountId -or [string]$taskOld.RiderClientAssignmentId -ne $taskRow.AssignmentId -or ([DateTimeOffset]$taskOld.StartedAtUtc) -ne ([DateTimeOffset]$taskRow.StartedAtUtc)) {throw 'Backup identity does not match the reviewed session.'}
 $taskSettlement=[DateTimeOffset]$taskOld.LastSettlementPaymentAtUtc
 $taskDate=$taskSettlement.ToOffset([TimeSpan]::FromHours(3)).Date
 if($taskDate -ne ([DateTimeOffset]$taskRow.StartedAtUtc).ToOffset([TimeSpan]::FromHours(3)).Date -or ([DateTime]$taskOld.CommissionStartsOn).Date -ne $taskDate.AddDays(1)){throw 'Historical handover, settlement and commission dates require review.'}
 [pscustomobject]@{account=$taskRow.Account;accountId=$taskRow.AccountId;sessionId=$taskRow.SessionId;assignmentId=$taskRow.AssignmentId;rowVersion=$taskRow.RowVersion;startedAtUtc=$taskRow.StartedAtUtc;settlementAtUtc=$taskSettlement.ToString('o');settlementDate=$taskDate.ToString('yyyy-MM-dd');commissionStartsOn=$taskDate.AddDays(1).ToString('yyyy-MM-dd')}
})
if($taskTargets.Count -ne 268){throw 'Expected 268 reviewed sessions with recoverable historical settlement dates.'}
$taskPlan=$taskTargets|ConvertTo-Json -Depth 4
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null;$taskCommitted=$false
$taskCorrelation='jahez-all-commission-baselines-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-review'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
Copy-Item -LiteralPath $taskBackupPath -Destination (Join-Path $taskOutput 'historical-settlement-source.xml')
$taskPlan|Set-Content (Join-Path $taskOutput 'plan.json') -Encoding utf8
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction;$taskCommand.CommandTimeout=60;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1);$taskCommand.Parameters['@Plan'].Value=$taskPlan
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{$taskData=[System.Data.DataSet]::new();$taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand);try{[void]$taskAdapter.Fill($taskData)}finally{$taskAdapter.Dispose()};return ,$taskData}finally{$taskCommand.Dispose()}
}
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBefore=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
SELECT * INTO #AllBefore FROM jahez_v2.AccountSessions;
'@
 $taskBefore.Tables[0].TableName='AccountSessions';$taskBefore.Tables[1].TableName='DatasetVersions'
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskProtected=@('AccountFees','AccountingEntries','CommandReceipts','CommissionPolicies','DailyOrders','EarningsStatements','ImportBatches','ImportFiles','ImportRows','MoneyMovements','Operations','PlatformTransactions','RequestDecisions','Requests','SettlementReminders','TransferredDebts')
 foreach($taskTable in $taskProtected){[void](Read-TaskData "SELECT * INTO #Keep$taskTable FROM jahez_v2.[$taskTable] WITH(HOLDLOCK);")}
 $taskResult=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(700)=N'User clarified that every Jahez account must use its last settlement as the commission baseline. Restore documented historical settlement dates cleared by the prior money-history reset, using the matching session and assignment backup. Resume commission after that date. No payment or financial transaction is created.';
SELECT * INTO #Targets FROM OPENJSON(@Plan) WITH(account varchar(100),accountId uniqueidentifier,sessionId uniqueidentifier,
 assignmentId uniqueidentifier,rowVersion varchar(32),startedAtUtc datetimeoffset,settlementAtUtc datetimeoffset,settlementDate date,commissionStartsOn date);
IF (SELECT COUNT(*) FROM #Targets)<>268 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>268
 THROW 51000,'Unexpected target count.',1;
IF EXISTS(SELECT 1 FROM #Targets t LEFT JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=t.accountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR s.IsDeleted=1 OR s.EndedAtUtc IS NOT NULL OR s.PlatformRiderAccountId<>t.accountId OR s.RiderClientAssignmentId<>t.assignmentId
 OR s.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("t.rowVersion"))','varbinary(8)')
 OR s.StartedAtUtc<>t.startedAtUtc OR s.LastSettlementPaymentAtUtc IS NOT NULL OR s.CommissionPostedThrough IS NOT NULL
 OR s.CommissionStartsOn<>'20261018' OR p.Id IS NULL OR p.IsDeleted=1 OR p.ExternalAccountId<>t.account OR c.Code<>'JAHEZ'
 OR t.settlementDate<>CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00')) OR t.commissionStartsOn<>DATEADD(day,1,t.settlementDate)
 OR t.settlementDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')))
 THROW 51000,'A reviewed session changed or a historical date is invalid. Reinspect.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.Operations o ON o.HandoverId=t.sessionId)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountingEntries l ON l.HandoverId=t.sessionId WHERE l.Bucket=3)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.CommissionPolicies cp ON cp.HandoverId=t.sessionId)
 THROW 51000,'Affected sessions now have operations or commission records. Reinspect.',1;

UPDATE s SET LastSettlementPaymentAtUtc=t.settlementAtUtc,CommissionPostedThrough=t.settlementDate,
 CommissionStartsOn=t.commissionStartsOn,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id;
IF @@ROWCOUNT<>268 THROW 51000,'Expected exactly 268 restored session baselines.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #AllBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #AllBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'Unrelated session business fields changed.',1;
IF EXISTS(SELECT s.* FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=s.Id)
 EXCEPT SELECT b.* FROM #AllBefore b WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=b.Id))
 THROW 51000,'An untargeted session changed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezHistoricalCommissionBaselineRestore','jahez','JahezAccountSession',t.sessionId,@Now,@Correlation,@Reason,
 (SELECT t.account Account,b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT t.account Account,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Targets t JOIN #AllBefore b ON b.Id=t.sessionId JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
IF @@ROWCOUNT<>268 THROW 51000,'Audit count mismatch.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Jahez dataset version missing or duplicated.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId WHERE s.LastSettlementPaymentAtUtc<>t.settlementAtUtc
 OR s.CommissionStartsOn<>t.commissionStartsOn OR s.CommissionPostedThrough<>t.settlementDate)
 THROW 51000,'Restored dates do not match the historical source.',1;
SELECT t.account Account,t.sessionId SessionId,t.settlementDate LastSettlementDate,t.settlementDate LastCommissionDate,t.commissionStartsOn NextCommissionStartsOn
FROM #Targets t ORDER BY t.account;
'@
 foreach($taskTable in $taskProtected){[void](Read-TaskData "IF EXISTS(SELECT * FROM #Keep$taskTable EXCEPT SELECT * FROM jahez_v2.[$taskTable]) OR EXISTS(SELECT * FROM jahez_v2.[$taskTable] EXCEPT SELECT * FROM #Keep$taskTable) THROW 51000,'Protected Jahez table changed: $taskTable',1;")}
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $taskTransaction.Commit();$taskCommitted=$true;$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT COUNT(*) Verified,SUM(CASE WHEN s.LastSettlementPaymentAtUtc=t.settlementAtUtc AND s.CommissionPostedThrough=t.settlementDate AND s.CommissionStartsOn=t.commissionStartsOn THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier,settlementAtUtc datetimeoffset,settlementDate date,commissionStartsOn date) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
SELECT COUNT(*) SessionsWithSettlement,SUM(CASE WHEN s.CommissionPostedThrough=CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')) THEN 0 ELSE 1 END) Mismatches
FROM jahez_v2.AccountSessions s JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
WHERE c.Code='JAHEZ' AND s.IsDeleted=0 AND p.IsDeleted=0 AND s.LastSettlementPaymentAtUtc IS NOT NULL;
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows[0].Verified -ne 268 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 268 -or $taskVerified.Tables[2].Rows[0].Mismatches -ne 0){throw 'Post-commit verification failed; do not replay.'}
  Write-Output ('COMMITTED AND VERIFIED: 268 restored baselines; all '+$taskVerified.Tables[2].Rows[0].SessionsWithSettlement+' dated sessions match.')
 }else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK: 268 historical settlement/commission baselines; all financial/history tables preserved.'}
 $taskTargets|Export-Csv (Join-Path $taskOutput 'report.csv') -NoTypeInformation -Encoding utf8
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction -and -not $taskCommitted){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
