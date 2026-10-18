param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskExpected=@('399990','404719','417748','458167','461497','485289')
$taskInspection=Get-Content (Join-Path $taskRoot 'tmp/jahez-all-commission-inspection.json') -Raw|ConvertFrom-Json
$taskTargets=@(foreach($taskRow in $taskInspection|Where-Object { $_.Category -eq 'NoRecordedSettlement' -and $_.Account -in $taskExpected }){
 $taskBaseline=[DateTimeOffset]$taskRow.StartedAtUtc
 $taskDate=$taskBaseline.ToOffset([TimeSpan]::FromHours(3)).Date
 [pscustomobject]@{account=$taskRow.Account;accountId=$taskRow.AccountId;sessionId=$taskRow.SessionId;assignmentId=$taskRow.AssignmentId;rowVersion=$taskRow.RowVersion;baselineAtUtc=$taskBaseline.ToString('o');baselineDate=$taskDate.ToString('yyyy-MM-dd');commissionStartsOn=$taskDate.AddDays(1).ToString('yyyy-MM-dd')}
})
if($taskTargets.Count -ne 6 -or (Compare-Object $taskExpected @($taskTargets.account))){throw 'Expected exactly the six accounts authorized by the user.'}
$taskPlan=$taskTargets|ConvertTo-Json -Depth 4
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null;$taskCommitted=$false
$taskCorrelation='jahez-six-handover-commission-baselines-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-review'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
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
'@
 $taskBefore.Tables[0].TableName='AccountSessions';$taskBefore.Tables[1].TableName='DatasetVersions'
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskProtected=@('AccountFees','AccountingEntries','CommandReceipts','CommissionPolicies','DailyOrders','EarningsStatements','ImportBatches','ImportFiles','ImportRows','MoneyMovements','Operations','PlatformTransactions','RequestDecisions','Requests','SettlementReminders','TransferredDebts')
 $taskPrepareSql="SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000; SELECT * INTO #AllBefore FROM jahez_v2.AccountSessions;`n"
 foreach($taskTable in $taskProtected){$taskPrepareSql+="SELECT * INTO #Keep$taskTable FROM jahez_v2.[$taskTable] WITH(HOLDLOCK);`n"}
 $taskMutationSql=@'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(700)=N'User explicitly approved using each handover date as the opening commission/settlement baseline for Jahez accounts 399990,404719,417748,458167,461497,485289, which have no recorded settlement. Initialize date anchors only; commission resumes after the baseline day. This is not a payment or executed settlement.';
SELECT * INTO #Targets FROM OPENJSON(@Plan) WITH(account varchar(100),accountId uniqueidentifier,sessionId uniqueidentifier,
 assignmentId uniqueidentifier,rowVersion varchar(32),baselineAtUtc datetimeoffset,baselineDate date,commissionStartsOn date);
IF (SELECT COUNT(*) FROM #Targets)<>6 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>6
 OR EXISTS(SELECT 1 FROM #Targets WHERE account NOT IN('399990','404719','417748','458167','461497','485289'))
 THROW 51000,'Unexpected target accounts.',1;
IF EXISTS(SELECT 1 FROM #Targets t LEFT JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=t.accountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR s.IsDeleted=1 OR s.EndedAtUtc IS NOT NULL OR s.PlatformRiderAccountId<>t.accountId OR s.RiderClientAssignmentId<>t.assignmentId
 OR s.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("t.rowVersion"))','varbinary(8)')
 OR s.StartedAtUtc<>t.baselineAtUtc OR s.LastSettlementPaymentAtUtc IS NOT NULL OR s.CommissionPostedThrough IS NOT NULL
 OR s.CommissionStartsOn<>'20261018' OR p.Id IS NULL OR p.IsDeleted=1 OR p.ExternalAccountId<>t.account OR c.Code<>'JAHEZ'
 OR t.baselineDate<>CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00')) OR t.commissionStartsOn<>DATEADD(day,1,t.baselineDate)
 OR t.baselineDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')))
 THROW 51000,'An authorized handover has changed. Reinspect before applying.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.Operations o ON o.HandoverId=t.sessionId)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountingEntries l ON l.HandoverId=t.sessionId WHERE l.Bucket=3)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.CommissionPolicies cp ON cp.HandoverId=t.sessionId)
 THROW 51000,'Affected handovers now have operations or commission records. Reinspect.',1;
UPDATE s SET LastSettlementPaymentAtUtc=t.baselineAtUtc,CommissionPostedThrough=t.baselineDate,
 CommissionStartsOn=t.commissionStartsOn,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id;
IF @@ROWCOUNT<>6 THROW 51000,'Expected exactly six initialized handovers.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #AllBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #AllBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'Unrelated session business fields changed.',1;
IF EXISTS(SELECT s.* FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=s.Id)
 EXCEPT SELECT b.* FROM #AllBefore b WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=b.Id))
 THROW 51000,'An untargeted session changed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezHandoverCommissionBaselineInitialization','jahez','JahezAccountSession',t.sessionId,@Now,@Correlation,@Reason,
 (SELECT t.account Account,b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT t.account Account,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc,N'Handover date approved as opening baseline; no payment created' BaselineSource FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Targets t JOIN #AllBefore b ON b.Id=t.sessionId JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
IF @@ROWCOUNT<>6 THROW 51000,'Audit count mismatch.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Jahez dataset version missing or duplicated.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId WHERE s.LastSettlementPaymentAtUtc<>t.baselineAtUtc
 OR s.CommissionStartsOn<>t.commissionStartsOn OR s.CommissionPostedThrough<>t.baselineDate)
 THROW 51000,'Initialized baseline dates do not match handover dates.',1;
SELECT t.account Account,t.sessionId SessionId,t.baselineDate BaselineDate,t.commissionStartsOn NextCommissionStartsOn FROM #Targets t ORDER BY t.account;
'@
 $taskCheckSql="`n"
 foreach($taskTable in $taskProtected){$taskCheckSql+="IF EXISTS(SELECT * FROM #Keep$taskTable EXCEPT SELECT * FROM jahez_v2.[$taskTable]) OR EXISTS(SELECT * FROM jahez_v2.[$taskTable] EXCEPT SELECT * FROM #Keep$taskTable) THROW 51000,'Protected Jahez table changed: $taskTable',1;`n"}
 $taskResult=Read-TaskData ($taskPrepareSql+$taskMutationSql+$taskCheckSql)
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $taskTransaction.Commit();$taskCommitted=$true;$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT COUNT(*) Verified,SUM(CASE WHEN s.LastSettlementPaymentAtUtc=t.baselineAtUtc AND s.CommissionPostedThrough=t.baselineDate AND s.CommissionStartsOn=t.commissionStartsOn THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier,baselineAtUtc datetimeoffset,baselineDate date,commissionStartsOn date) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
SELECT COUNT(*) AllSessions,SUM(CASE WHEN s.EndedAtUtc IS NULL THEN 1 ELSE 0 END) CurrentSessions,
 SUM(CASE WHEN s.CommissionPostedThrough=CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')) THEN 0 ELSE 1 END) Mismatches
FROM jahez_v2.AccountSessions s JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
WHERE c.Code='JAHEZ' AND s.IsDeleted=0 AND p.IsDeleted=0;
SELECT p.ExternalAccountId Account,s.Id SessionId,s.EndedAtUtc,CONVERT(varchar(10),SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'),23) SettlementBaselineDate,
 CONVERT(varchar(10),s.CommissionPostedThrough,23) LastCommissionDate,CONVERT(varchar(10),DATEADD(day,1,s.CommissionPostedThrough),23) NextCommissionStartsOn,
 CASE WHEN s.Id IS NULL THEN 'No handover; no commission' WHEN s.EndedAtUtc IS NOT NULL THEN 'Closed handover' ELSE 'Current handover' END Coverage
FROM app.PlatformRiderAccounts p JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
OUTER APPLY(SELECT TOP(1) h.* FROM jahez_v2.AccountSessions h WHERE h.PlatformRiderAccountId=p.Id AND h.IsDeleted=0
 ORDER BY CASE WHEN h.EndedAtUtc IS NULL THEN 0 ELSE 1 END,h.StartedAtUtc DESC,h.Id) s
WHERE c.Code='JAHEZ' AND p.IsDeleted=0 ORDER BY p.ExternalAccountId;
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows[0].Verified -ne 6 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 6 -or $taskVerified.Tables[2].Rows[0].Mismatches -ne 0){throw 'Post-commit verification failed; do not replay.'}
  $taskCoverage=@(foreach($taskRow in $taskVerified.Tables[3].Rows){[pscustomobject]@{Account=$taskRow.Account;SessionId=$taskRow.SessionId.ToString();SettlementBaselineDate=$taskRow.SettlementBaselineDate.ToString();LastCommissionDate=$taskRow.LastCommissionDate.ToString();NextCommissionStartsOn=$taskRow.NextCommissionStartsOn.ToString();Coverage=$taskRow.Coverage}})
  $taskCoverage|Export-Csv (Join-Path $taskOutput 'all-accounts-coverage.csv') -NoTypeInformation -Encoding utf8
  Write-Output ('COMMITTED AND VERIFIED: six approved baselines initialized. All '+$taskVerified.Tables[2].Rows[0].AllSessions+' sessions match; '+$taskVerified.Tables[2].Rows[0].CurrentSessions+' current sessions. Account coverage: '+$taskCoverage.Count+'.')
 }else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK: six approved handover baselines; all financial/history tables preserved.'}
 $taskTargets|Export-Csv (Join-Path $taskOutput 'six-accounts-report.csv') -NoTypeInformation -Encoding utf8
 $taskTargets|Select-Object account,baselineDate,commissionStartsOn|Format-Table -AutoSize
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction -and -not $taskCommitted){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
