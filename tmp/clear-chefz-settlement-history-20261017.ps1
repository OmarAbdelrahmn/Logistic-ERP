param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null
$taskCorrelation='chefz-settlement-reset-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation)
[void](New-Item -ItemType Directory -Path $taskOutput)
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction
 $taskCommand.CommandTimeout=45;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{
  $data=[System.Data.DataSet]::new();$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
  try{[void]$adapter.Fill($data)}finally{$adapter.Dispose()}
  return ,$data
 }finally{$taskCommand.Dispose()}
}
$taskPreservedSql=@'
SELECT 'ChefzAccountDetails' Kind,COUNT_BIG(*) N FROM chefz.AccountDetails
UNION ALL SELECT 'ChefzDailyPerformance',COUNT_BIG(*) FROM chefz.DailyPerformance
UNION ALL SELECT 'ChefzPerformanceImports',COUNT_BIG(*) FROM chefz.PerformanceImports
UNION ALL SELECT 'PlatformRiderAccounts',COUNT_BIG(*) FROM app.PlatformRiderAccounts
UNION ALL SELECT 'RiderClientAssignments',COUNT_BIG(*) FROM app.RiderClientAssignments;
'@
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBefore=Read-TaskData (@'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT * FROM chefz.WalletSettlements WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM chefz.OrderSettlementImports WITH(UPDLOCK,HOLDLOCK);
'@ + "`n"+$taskPreservedSql)
 if($taskBefore.Tables[0].Rows.Count -ne 241 -or $taskBefore.Tables[1].Rows.Count -ne 3){throw 'Settlement/import counts changed; review before clearing.'}
 $taskBackup=Join-Path $taskOutput 'before.xml'
 $taskBefore.WriteXml($taskBackup,[System.Data.XmlWriteMode]::WriteSchema)
 $taskBackupCheck=[System.Data.DataSet]::new();[void]$taskBackupCheck.ReadXml($taskBackup,[System.Data.XmlReadMode]::ReadSchema)
 if($taskBackupCheck.Tables[0].Rows.Count -ne 241 -or $taskBackupCheck.Tables[1].Rows.Count -ne 3){throw 'Backup verification failed.'}
 (Get-FileHash -LiteralPath $taskBackup -Algorithm SHA256).Hash|Set-Content (Join-Path $taskOutput 'before.sha256') -Encoding utf8
 if(-not $Apply){
  $taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null
  Write-Output ('REVIEW ONLY: backed up 241 settlements and 3 order imports. '+$taskOutput)
  return
 }
 $taskMutation=@'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Platform uniqueidentifier='01a02a0d-845d-7535-ac25-1e1f0c8e5808',@Now datetimeoffset=SYSUTCDATETIME(),
 @Settlements int=(SELECT COUNT(*) FROM chefz.WalletSettlements),
 @Imports int=(SELECT COUNT(*) FROM chefz.OrderSettlementImports);
IF @Settlements<>241 OR @Imports<>3 THROW 51000,'Unexpected settlement/import counts.',1;
IF NOT EXISTS(SELECT 1 FROM [identity].Users WHERE Id=@Actor AND UserName='Omar' AND IsDeleted=0)
 THROW 51000,'Audit actor not found.',1;
IF NOT EXISTS(SELECT 1 FROM platform.ClientPlatforms WHERE Id=@Platform AND Code='SHIFTZ' AND IsDeleted=0)
 THROW 51000,'Expected Chefz platform not found.',1;
IF EXISTS(SELECT 1 FROM chefz.WalletSettlements s JOIN app.PlatformRiderAccounts a ON a.Id=s.PlatformRiderAccountId WHERE a.ClientPlatformId<>@Platform)
 THROW 51000,'Unexpected non-Chefz settlement reference.',1;
DECLARE @Before nvarchar(max)=(SELECT @Settlements SettlementCount,@Imports OrderImportCount,
 (SELECT SUM(GrossEarnings) FROM chefz.WalletSettlements) NetEarnings,
 (SELECT SUM(CompanyAmount) FROM chefz.WalletSettlements) CompanyAmount,
 (SELECT SUM(RiderAmount) FROM chefz.WalletSettlements) RiderAmount,
 JSON_QUERY((SELECT Id FROM chefz.WalletSettlements ORDER BY Id FOR JSON PATH)) SettlementIds,
 JSON_QUERY((SELECT Id,FileName,ContentHash,SourceRows FROM chefz.OrderSettlementImports ORDER BY Id FOR JSON PATH)) OrderImports
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
DELETE FROM chefz.WalletSettlements;
IF @@ROWCOUNT<>241 THROW 51000,'Unexpected settlement deletion count.',1;
DELETE FROM chefz.OrderSettlementImports;
IF @@ROWCOUNT<>3 THROW 51000,'Unexpected import deletion count.',1;
IF EXISTS(SELECT 1 FROM chefz.WalletSettlements) OR EXISTS(SELECT 1 FROM chefz.OrderSettlementImports)
 THROW 51000,'Settlement history was not completely cleared.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,
 OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','ChefzSettlementHistoryReset','chefz','ChefzSettlementHistory',@Platform,
 @Now,@Correlation,N'User requested clearing all old Chefz settlement data after confirming VAT-inclusive delivery fees. All settlement and order-import rows backed up before deletion; accounts, assignments and performance preserved.',
 @Before,N'{"SettlementCount":0,"OrderImportCount":0}','CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='chefz';
IF @@ROWCOUNT<>1 THROW 51000,'Chefz cache-version update failed.',1;
SELECT @Settlements DeletedSettlements,@Imports DeletedOrderImports,@Correlation CorrelationId;
'@
 $taskMutation|Set-Content (Join-Path $taskOutput 'executed.sql') -Encoding utf8
 $taskResult=Read-TaskData $taskMutation
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-result.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskInside=Read-TaskData $taskPreservedSql
 foreach($taskRow in $taskBefore.Tables[2].Rows){
  $taskMatch=@($taskInside.Tables[0].Rows|Where-Object {$_.Kind -eq $taskRow.Kind})
  if($taskMatch.Count -ne 1 -or $taskMatch[0].N -ne $taskRow.N){throw ('Preserved data count changed: '+$taskRow.Kind)}
 }
 $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
 $taskConnection.Close();$taskConnection.Open()
 $taskVerified=Read-TaskData (@'
SELECT COUNT(*) RemainingSettlements FROM chefz.WalletSettlements;
SELECT COUNT(*) RemainingOrderImports FROM chefz.OrderSettlementImports;
SELECT Action,BeforeJson,AfterJson,OccurredAtUtc FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@+"`n"+$taskPreservedSql)
 $taskVerified.WriteXml((Join-Path $taskOutput 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($taskVerified.Tables[0].Rows[0].RemainingSettlements -ne 0 -or $taskVerified.Tables[1].Rows[0].RemainingOrderImports -ne 0 -or $taskVerified.Tables[2].Rows.Count -ne 1){throw 'Post-commit verification failed. Inspect saved evidence; do not replay.'}
 $taskSummary=[ordered]@{Action='ChefzSettlementHistoryReset';CorrelationId=$taskCorrelation;DeletedSettlements=241;DeletedOrderImports=3;RemainingSettlements=0;RemainingOrderImports=0;Backup=$taskBackup;Preserved=@(foreach($taskRow in $taskVerified.Tables[3].Rows){@{Kind=$taskRow.Kind;Count=$taskRow.N}});VatCodeDeployment='Not performed by this reset'}
 $taskSummary|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskOutput 'summary.json') -Encoding utf8
 Write-Output 'COMMITTED AND VERIFIED: 241 Chefz settlements and 3 order-import records cleared. Accounts, assignments and performance counts preserved.'
 Write-Output ('Evidence: '+$taskOutput)
}catch{
 if($taskTransaction){$taskTransaction.Rollback()}
 throw
}finally{
 if($taskTransaction){$taskTransaction.Dispose()}
 $taskConnection.Dispose()
}
