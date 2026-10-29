param([switch]$Apply,[switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$syncRoot=Split-Path -Parent $PSScriptRoot
$syncBase=Join-Path $syncRoot 'outputs/ninja-workbook-sync-20261025'
$syncPlanPath=Join-Path $syncBase 'approved-plan.json'
$syncPlanText=Get-Content -LiteralPath $syncPlanPath -Raw
$syncPlan=$syncPlanText|ConvertFrom-Json
$syncSqlPath=Join-Path $syncBase 'approved-sync.sql'
$syncVerifyPath=Join-Path $syncBase 'approved-verify.sql'
$syncPlanHash=(Get-FileHash -LiteralPath $syncPlanPath -Algorithm SHA256).Hash
$syncSqlHash=(Get-FileHash -LiteralPath $syncSqlPath -Algorithm SHA256).Hash
$syncVerifyHash=(Get-FileHash -LiteralPath $syncVerifyPath -Algorithm SHA256).Hash
function Get-SyncSourceHash {
 $sourceStream=[IO.FileStream]::new($syncPlan.sourcePath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
 $sourceHasher=[Security.Cryptography.SHA256]::Create()
 try{return [Convert]::ToHexString($sourceHasher.ComputeHash($sourceStream)).ToLowerInvariant()}finally{$sourceHasher.Dispose();$sourceStream.Dispose()}
}
if ((Get-SyncSourceHash) -ne $syncPlan.sourceSha256) { throw 'Source workbook changed.' }
if ($syncPlan.expectedCurrent -ne 151 -or $syncPlan.mappings.Count -ne 229 -or $syncPlan.inserts.Count -ne 8) { throw 'Unexpected manifest.' }
$syncSettings=Get-Content -LiteralPath (Join-Path $syncRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$syncConnectionText=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$syncSettings.ConnectionStrings.LogisticsDatabase}
$syncConnection=[Data.SqlClient.SqlConnection]::new($syncConnectionText)
$syncTransaction=$null
$syncAccountIds=[Guid[]]@($syncPlan.mappings.accountId|Select-Object -Unique)
[Array]::Sort($syncAccountIds)
$syncLockOrder=ConvertTo-Json -InputObject @($syncAccountIds|ForEach-Object ToString) -Compress
$syncTables=[ordered]@{Employees='app.Employees';Accounts='app.PlatformRiderAccounts';Assignments='app.RiderClientAssignments';Details='ninja.NinjaAccountDetails';Usages='app.PlatformAccountDayUsages';Registrations='app.PlatformAccountRegistrations';Riders='app.RiderProfiles';Events='app.RiderAssignmentEvents';Credentials='app.PlatformAccountCredentialVersions';Versions='app.DatasetVersions'}

function Read-Sync([string]$Sql,[int]$Timeout=120,[string]$BackupPath){
 $command=$syncConnection.CreateCommand();$command.Transaction=$syncTransaction;$command.CommandTimeout=$Timeout;$command.CommandText=$Sql
 [void]$command.Parameters.Add('@Plan',[Data.SqlDbType]::NVarChar,-1);$command.Parameters['@Plan'].Value=$syncPlanText
 [void]$command.Parameters.Add('@LockOrder',[Data.SqlDbType]::NVarChar,-1);$command.Parameters['@LockOrder'].Value=$syncLockOrder
 if($BackupPath){[void]$command.Parameters.Add('@BackupPath',[Data.SqlDbType]::NVarChar,4000);$command.Parameters['@BackupPath'].Value=$BackupPath}
 $adapter=[Data.SqlClient.SqlDataAdapter]::new($command)
 try{$result=[Data.DataSet]::new();[void]$adapter.Fill($result);return ,$result}
 catch{foreach($sqlError in $_.Exception.GetBaseException().Errors){Write-Host "SQL diagnostic: number=$($sqlError.Number), line=$($sqlError.LineNumber), message=$($sqlError.Message)"};throw}
 finally{$adapter.Dispose();$command.Dispose()}
}

function Sync-Fingerprint([Data.DataTable]$Table){
 $hash=[Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
 try {
  foreach($row in @($Table.Rows|Sort-Object { [string]$_.Id })){
   $values=@(foreach($column in $Table.Columns){
    $value=$row[$column.ColumnName]
    if($value -is [DBNull]){'<DBNULL>'}elseif($value -is [byte[]]){[Convert]::ToBase64String($value)}
    elseif($value -is [DateTimeOffset]){$value.ToString('o')}elseif($value -is [DateTime]){$value.ToString('o')}
    elseif($value -is [IFormattable]){$value.ToString($null,[Globalization.CultureInfo]::InvariantCulture)}else{[string]$value}
   })
   $hash.AppendData([Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $values -Compress)))
  }
  return [Convert]::ToHexString($hash.GetHashAndReset())
 } finally {$hash.Dispose()}
}

try{
 if($syncConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $syncConnection.Database -ne 'db67927'){throw 'Unexpected hosted target.'}
 $syncConnection.Open()
 $already=Read-Sync "SELECT COUNT_BIG(*) Count FROM audit.AuditEntries WHERE CorrelationId=JSON_VALUE(@Plan,'`$.batchId');"
 if($VerifyOnly -or [long]$already.Tables[0].Rows[0].Count -gt 0){
  if([long]$already.Tables[0].Rows[0].Count -eq 0){throw 'This batch has not been applied.'}
  $verified=Read-Sync (Get-Content -LiteralPath $syncVerifyPath -Raw)
  $verified.WriteXml((Join-Path $syncBase 'postcommit-verification.xml'),[Data.XmlWriteMode]::WriteSchema)
  [pscustomobject]@{Mode='AlreadyAppliedVerified';BatchId=$syncPlan.batchId;DatabaseWrites=0;CurrentAssignments=151;MatchedSourceRows=229}|ConvertTo-Json
  return
 }
 $rehearsalPath=Join-Path $syncBase 'rehearsal.json'
 if($Apply){
  if(!(Test-Path -LiteralPath $rehearsalPath)){throw 'A successful rehearsal is required.'}
  $rehearsal=Get-Content -LiteralPath $rehearsalPath -Raw|ConvertFrom-Json
  if(!$rehearsal.Passed -or $rehearsal.PlanHash -ne $syncPlanHash -or $rehearsal.SqlHash -ne $syncSqlHash -or $rehearsal.VerifyHash -ne $syncVerifyHash){throw 'The rehearsal does not match the current manifest and SQL.'}
 }
 $mode=if($Apply){'apply'}else{'rehearsal'}
 $runDirectory=Join-Path $syncRoot ('artifacts/ninja-workbook-sync-'+$mode+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
 [void](New-Item -ItemType Directory -Path $runDirectory)
 Copy-Item -LiteralPath $syncPlanPath -Destination (Join-Path $runDirectory 'plan.json')
 if($Apply){
  Write-Output 'Creating and verifying a server COPY_ONLY checksum backup.'
  $serverBackup=Read-Sync @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup folder unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+CASE WHEN RIGHT(@folder,1) IN(N'\',N'/') THEN N'' ELSE N'\' END+N'db67927-before-ninja-workbook-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath,CAST(1 AS bit) BackupCommandCompleted,SYSUTCDATETIME() CompletedAtUtc;
'@ 180
  $serverBackup.WriteXml((Join-Path $runDirectory 'server-backup.xml'),[Data.XmlWriteMode]::WriteSchema)
  $serverBackupVerified=$false
  try{
   $serverVerify=Read-Sync 'RESTORE VERIFYONLY FROM DISK=@BackupPath WITH CHECKSUM; SELECT CAST(1 AS bit) Verified;' 180 ([string]$serverBackup.Tables[0].Rows[0].ServerBackupPath)
   $serverVerify.WriteXml((Join-Path $runDirectory 'server-backup-verification.xml'),[Data.XmlWriteMode]::WriteSchema)
   $serverBackupVerified=$true
  }catch{
   if(@($_.Exception.GetBaseException().Errors|Where-Object Number -eq 262).Count -eq 0){throw}
   Write-Output 'Host denies RESTORE VERIFYONLY. Exact row-backup read-back verification remains mandatory before commit.'
  }
 }
 $syncTransaction=$syncConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
 $execution=Read-Sync (Get-Content -LiteralPath $syncSqlPath -Raw) 180
 if($execution.Tables.Count -ne ($syncTables.Count+1)){throw 'Unexpected correction result shape.'}
 $index=0
 foreach($name in $syncTables.Keys){$execution.Tables[$index++].TableName=$name}
 $execution.Tables[$index].TableName='Verification'
 $backupPath=Join-Path $runDirectory 'before-and-verification.xml'
 $execution.WriteXml($backupPath,[Data.XmlWriteMode]::WriteSchema)
 $readBack=[Data.DataSet]::new();[void]$readBack.ReadXml($backupPath,[Data.XmlReadMode]::ReadSchema)
 foreach($name in $syncTables.Keys){
  if($readBack.Tables[$name].Rows.Count -ne $execution.Tables[$name].Rows.Count -or (Sync-Fingerprint $readBack.Tables[$name]) -ne (Sync-Fingerprint $execution.Tables[$name])){throw "Backup read-back failed for $name."}
 }
 $result=[ordered]@{Mode=$mode;BatchId=$syncPlan.batchId;Passed=$true;PlanHash=$syncPlanHash;SqlHash=$syncSqlHash;VerifyHash=$syncVerifyHash;BackupPath=$backupPath;BackupHash=(Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash;CurrentAssignments=151;MatchedSourceRows=229;HistoricalAssignments=78;CredentialsPreserved=$true;CompletedAtUtc=[DateTimeOffset]::UtcNow.ToString('o')}
 if($Apply){
  if((Get-SyncSourceHash) -ne $syncPlan.sourceSha256){throw 'Source workbook changed during the correction transaction.'}
  $syncTransaction.Commit();$syncTransaction.Dispose();$syncTransaction=$null
  $syncConnection.Close();$syncConnection.Open()
  $post=Read-Sync (Get-Content -LiteralPath $syncVerifyPath -Raw)
  $post.WriteXml((Join-Path $syncBase 'postcommit-verification.xml'),[Data.XmlWriteMode]::WriteSchema)
  $result.Mode='AppliedAndVerified'
  $result.ServerBackupCreated=$true
  $result.ServerRestoreVerificationSucceeded=$serverBackupVerified
  $result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $syncBase 'applied.json') -Encoding utf8
 }else{
  $syncTransaction.Rollback();$syncTransaction.Dispose();$syncTransaction=$null
  $rollbackSql=@($syncTables.Values|ForEach-Object {"SELECT * FROM $_;"}) -join [Environment]::NewLine
  $rolledBack=Read-Sync $rollbackSql
  $index=0
  foreach($name in $syncTables.Keys){
   if((Sync-Fingerprint $rolledBack.Tables[$index++]) -ne (Sync-Fingerprint $execution.Tables[$name])){throw "Rollback preservation failed for $name."}
  }
  $result.Mode='RehearsedVerifiedAndRolledBack'
  $result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $rehearsalPath -Encoding utf8
 }
 $result|ConvertTo-Json -Depth 5
}catch{
 if($syncTransaction){try{$syncTransaction.Rollback()}catch{};$syncTransaction.Dispose();$syncTransaction=$null}
 throw
}finally{$syncConnection.Dispose();$syncSettings=$null;$syncConnectionText=$null}
