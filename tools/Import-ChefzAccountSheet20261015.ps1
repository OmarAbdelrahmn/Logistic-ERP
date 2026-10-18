param([switch]$Apply,[switch]$SkipPasswords)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$planPath=Join-Path $workspace 'tmp/chefz-plan-20261015.json'
$plan=Get-Content -LiteralPath $planPath -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath $plan.sourcePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $plan.sourceSha256){throw 'Workbook changed since review.'}
if($SkipPasswords){$plan.credentials=@()}
if($Apply -and @($plan.credentials).Count -gt 0){
 $hostCfg=Get-Content (Join-Path $workspace 'artifacts/chefz-import-20261015/current-appsettings.json') -Raw|ConvertFrom-Json
 $key=Get-Content (Join-Path $workspace 'artifacts/chefz-import-20261015/encryption-key.txt') -Raw
 if(!$hostCfg.SensitiveData.EncryptionKey -or $hostCfg.SensitiveData.EncryptionKey.Trim() -cne $key.Trim()){throw 'The password encryption key must first be configured and re-read from the hosted API.'}
 $key=$null;$hostCfg=$null
}
$planJson=$plan|ConvertTo-Json -Depth 20
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase);$tx=$null
function Read-Data([string]$sql){
 $q=$conn.CreateCommand();$q.Transaction=$tx;$q.CommandTimeout=120;$q.CommandText=$sql
 [void]$q.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@PlanJson'].Value=$planJson
 [void]$q.Parameters.Add('@CorrelationId',[System.Data.SqlDbType]::NVarChar,200);$q.Parameters['@CorrelationId'].Value=$plan.correlationId
 try{$d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);try{[void]$ad.Fill($d)}finally{$ad.Dispose()};return ,$d}finally{$q.Dispose()}
}
try{
 $conn.Open();if($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $out=Join-Path $workspace ('outputs/chefz-account-sheet-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+$(if($Apply){'-applied'}else{'-review'}));[void](New-Item -ItemType Directory -Path $out)
 $planJson|Set-Content (Join-Path $out 'plan.json') -Encoding utf8
 $sqlPath=Join-Path $workspace 'database/scripts/chefz-account-sheet-20261015.sql'
 Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $out 'executed.sql')
 $tx=$conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $backupSql=@'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
SELECT * INTO #BeforeAccounts FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeAssignments FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeDetails FROM chefz.AccountDetails WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforePerformance FROM chefz.DailyPerformance WITH(HOLDLOCK);
SELECT * INTO #BeforeSettlements FROM chefz.WalletSettlements WITH(HOLDLOCK);
SELECT * INTO #BeforeEmployees FROM app.Employees WITH(HOLDLOCK);
SELECT * FROM #BeforeAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId');
SELECT a.* FROM #BeforeAssignments a WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId'));
SELECT * FROM #BeforeDetails;
SELECT e.* FROM app.RiderAssignmentEvents e JOIN #BeforeAssignments a ON a.Id=e.RiderClientAssignmentId JOIN #BeforeAccounts p ON p.Id=a.PlatformRiderAccountId WHERE p.ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * FROM #BeforePerformance;
SELECT * FROM #BeforeSettlements;
SELECT * FROM app.PlatformAccountCredentialVersions WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('chefz','platform-operations');
'@
 $result=Read-Data ($backupSql+[Environment]::NewLine+(Get-Content -LiteralPath $sqlPath -Raw))
 $names=@('AccountsBefore','AssignmentsBefore','DetailsBefore','AssignmentEventsBefore','PerformanceBefore','SettlementsBefore','CredentialsBefore','DatasetVersionsBefore')
 $backup=[System.Data.DataSet]::new()
 for($i=0;$i -lt $names.Count;$i++){$t=$result.Tables[$i].Copy();$t.TableName=$names[$i];[void]$backup.Tables.Add($t)}
 $backup.WriteXml((Join-Path $out 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $summary=$result.Tables[8].Rows[0].SummaryJson;$summary|Set-Content (Join-Path $out 'summary.json') -Encoding utf8
 $verifySql=Get-Content (Join-Path $workspace 'database/scripts/verify-chefz-account-sheet-20261015.sql') -Raw
 $v=Read-Data $verifySql
 if(@($v.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){$v.Tables[0]|Format-Table;throw 'Transaction verification failed.'}
 $v.Tables[0].TableName='Checks';$v.Tables[1].TableName='TwoAccountRiders';$v.Tables[2].TableName='EncryptedCredentials'
 $v.WriteXml((Join-Path $out 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $tx.Commit();$tx.Dispose();$tx=$null
  $conn.Close();$conn.Open();$v=Read-Data $verifySql
  $v.Tables[0].TableName='Checks';$v.Tables[1].TableName='TwoAccountRiders';$v.Tables[2].TableName='EncryptedCredentials'
  $v.WriteXml((Join-Path $out 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if(@($v.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){throw 'Post-commit verification failed. Do not replay.'}
  Write-Output 'COMMITTED AND VERIFIED ON A FRESH CONNECTION'
 }else{$tx.Rollback();$tx.Dispose();$tx=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK'}
 $v.Tables[0]|Format-Table -AutoSize
 Write-Output ('Riders with two Chefz accounts: '+$v.Tables[1].Rows.Count)
 Write-Output $summary
 Write-Output ('Output: '+$out)
}catch{if($tx){try{$tx.Rollback()}catch{}};throw}finally{if($tx){$tx.Dispose()};$conn.Dispose()}
