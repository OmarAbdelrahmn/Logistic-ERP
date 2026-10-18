param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskPlan=Get-Content (Join-Path $taskRoot 'tmp/jahez-take-dates-plan-20261017.json') -Raw
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null
$taskCorrelation='jahez-commission-start-24-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-review'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
$taskPlan|Set-Content (Join-Path $taskOutput 'plan.json') -Encoding utf8
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction;$taskCommand.CommandTimeout=45;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1);$taskCommand.Parameters['@Plan'].Value=$taskPlan
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{$taskData=[System.Data.DataSet]::new();$taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
  try{[void]$taskAdapter.Fill($taskData)}finally{$taskAdapter.Dispose()};return ,$taskData
 }finally{$taskCommand.Dispose()}
}
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBefore=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT sessionId FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier));
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
'@
 $taskBefore.Tables[0].TableName='Sessions';$taskBefore.Tables[1].TableName='DatasetVersions'
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskResult=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(500)=N'User explicitly requested setting commission accrual starts for all 24 Jahez accounts to exactly the same supplied per-account dates as their take and last settlement dates, inclusive.';
SELECT * INTO #Targets FROM OPENJSON(@Plan) WITH(account varchar(100),takeDate date,accountId uniqueidentifier,sessionId uniqueidentifier,assignmentId uniqueidentifier);
IF (SELECT COUNT(*) FROM #Targets)<>24 OR (SELECT COUNT(DISTINCT account) FROM #Targets)<>24 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>24
 THROW 51000,'Expected 24 unique requested accounts and sessions.',1;
SELECT s.* INTO #Before FROM jahez_v2.AccountSessions s WITH(UPDLOCK,HOLDLOCK) WHERE s.Id IN(SELECT sessionId FROM #Targets);
IF (SELECT COUNT(*) FROM #Before)<>24 OR EXISTS(SELECT 1 FROM #Targets t LEFT JOIN app.PlatformRiderAccounts p ON p.Id=t.accountId
 LEFT JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId LEFT JOIN #Before s ON s.Id=t.sessionId
 LEFT JOIN app.RiderClientAssignments a ON a.Id=t.assignmentId
 WHERE p.Id IS NULL OR p.ExternalAccountId<>t.account OR p.IsDeleted=1 OR c.Id IS NULL OR c.Code<>'JAHEZ'
 OR s.Id IS NULL OR s.PlatformRiderAccountId<>t.accountId OR s.EndedAtUtc IS NOT NULL OR s.IsDeleted=1
 OR s.CommissionStartsOn<>'20261018' OR s.CommissionPostedThrough IS NOT NULL
 OR s.RiderClientAssignmentId<>t.assignmentId OR CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))<>t.takeDate
 OR s.LastSettlementPaymentAtUtc IS NULL OR CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'))<>t.takeDate
 OR a.Id IS NULL OR a.IsDeleted=1 OR a.EffectiveFrom<>t.takeDate OR a.EffectiveTo IS NOT NULL OR a.Status<>2
 OR t.takeDate IS NULL OR t.takeDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')))
 THROW 51000,'Current accounts or commission settings changed since review.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.Operations o WITH(HOLDLOCK) ON o.HandoverId=t.sessionId WHERE o.ExecutedAtUtc IS NOT NULL)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountingEntries l WITH(HOLDLOCK) ON l.HandoverId=t.sessionId WHERE l.Bucket=3)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.CommissionPolicies x WITH(HOLDLOCK) ON x.HandoverId=t.sessionId)
 THROW 51000,'Posted commission, executed operations or commission policies require re-evaluation.',1;
UPDATE s SET CommissionStartsOn=t.takeDate,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id WHERE s.CommissionStartsOn='20261018' AND s.CommissionPostedThrough IS NULL;
IF @@ROWCOUNT<>24 THROW 51000,'Expected exactly 24 updated commission start dates.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #Before
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions WHERE Id IN(SELECT sessionId FROM #Targets))
 THROW 51000,'Unrelated session fields changed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezCommissionStartDateCorrection','jahez','JahezAccountSession',t.sessionId,@Now,@Correlation,@Reason,
(SELECT t.account Account,b.CommissionStartsOn FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
(SELECT t.account Account,s.CommissionStartsOn FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Targets t JOIN #Before b ON b.Id=t.sessionId JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId WHERE s.CommissionStartsOn<>t.takeDate)
 THROW 51000,'Saved commission starts do not match requested dates.',1;
SELECT t.account Account,CONVERT(varchar(10),s.CommissionStartsOn,23) CommissionStartsOn
FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId ORDER BY t.account;
'@
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT COUNT(*) AccountsVerified,SUM(CASE WHEN s.CommissionStartsOn=t.takeDate AND CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))=t.takeDate AND CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'))=t.takeDate THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(takeDate date,sessionId uniqueidentifier) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows[0].AccountsVerified -ne 24 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 24){throw 'Post-commit verification failed; do not replay.'}
  Write-Output 'COMMITTED AND VERIFIED: all 24 commission start dates match the supplied dates, inclusive.'
 }else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK'}
 $taskRows=@(foreach($taskRow in $taskResult.Tables[0].Rows){[pscustomobject]@{Account=$taskRow.Account;CommissionStartsOn=$taskRow.CommissionStartsOn}})
 $taskRows|ConvertTo-Json -Depth 3|Set-Content (Join-Path $taskOutput 'report.json') -Encoding utf8
 $taskRows|Format-Table -AutoSize
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
