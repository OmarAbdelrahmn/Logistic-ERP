param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$planPath=Join-Path $workspace 'tmp/jahez-plan-20261017.json'
$planJson=Get-Content -LiteralPath $planPath -Raw
$plan=$planJson|ConvertFrom-Json
$planJson=$plan|ConvertTo-Json -Depth 30 -Compress
$sourceStream=[System.IO.File]::Open($plan.sourcePath,[System.IO.FileMode]::Open,[System.IO.FileAccess]::Read,[System.IO.FileShare]::ReadWrite)
try{$buffer=[System.IO.MemoryStream]::new();$sourceStream.CopyTo($buffer);$sourceBytes=$buffer.ToArray();$buffer.Dispose()}finally{$sourceStream.Dispose()}
$sha=[System.Security.Cryptography.SHA256]::Create()
try{$sourceHash=[Convert]::ToHexString($sha.ComputeHash($sourceBytes)).ToLowerInvariant()}finally{$sha.Dispose()}
if($sourceHash -ne $plan.sourceSha256){throw 'Source workbook changed since extraction.'}
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$cs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$cfg.ConnectionStrings.LogisticsDatabase}
$connection=[System.Data.SqlClient.SqlConnection]::new($cs);$transaction=$null
function Read-Data([string]$sql){
 $command=$connection.CreateCommand();$command.Transaction=$transaction;$command.CommandTimeout=180;$command.CommandText=$sql
 if($sql.Contains('@PlanJson')){[void]$command.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$command.Parameters['@PlanJson'].Value=$planJson}
 if($sql.Contains('@CorrelationId')){[void]$command.Parameters.Add('@CorrelationId',[System.Data.SqlDbType]::NVarChar,200);$command.Parameters['@CorrelationId'].Value=$plan.correlationId}
 try{$d=[System.Data.DataSet]::new();$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command);try{[void]$adapter.Fill($d)}finally{$adapter.Dispose()};return ,$d}finally{$command.Dispose()}
}
try{
 $connection.Open()
 if($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $already=Read-Data 'SELECT COUNT(*) N FROM audit.AuditEntries WHERE CorrelationId=@CorrelationId;'
 if($already.Tables[0].Rows[0].N -gt 0){throw 'Import already committed. Do not replay.'}
 $out=Join-Path $workspace ('outputs/jahez-account-sheet-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+$(if($Apply){'-applied'}else{'-review'}))
 [void](New-Item -ItemType Directory -Path $out)
 $planJson|Set-Content (Join-Path $out 'plan.json') -Encoding utf8
 [System.IO.File]::WriteAllBytes((Join-Path $out 'source.xlsx'),$sourceBytes)
 $sqlPath=Join-Path $workspace 'database/scripts/jahez-account-sheet-20261017.sql'
 $verifyPath=Join-Path $workspace 'database/scripts/verify-jahez-account-sheet-20261017.sql'
 Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $out 'executed.sql')
 Copy-Item -LiteralPath $verifyPath -Destination (Join-Path $out 'verify.sql')
 $transaction=$connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
 [void](Read-Data 'SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;')
 # All existing rows in these datasets must be preserved byte-for-byte, except the five explicitly edited account fields.
 $specs=@(
  @{schema='app';table='PlatformRiderAccounts';allow='accounts';idpath='accountId'},
  @{schema='app';table='RiderProfiles';allow='profiles';idpath='id'},
  @{schema='app';table='RiderClientAssignments';allow='insertAssignments';idpath='id'},
  @{schema='app';table='VehiclePlatformAccountAssignments';allow='vehicleLinks';idpath='id'},
  @{schema='app';table='Employees'},@{schema='app';table='Vehicles'},@{schema='app';table='Sponsors'},
  @{schema='app';table='RiderVehicleAssignments'},@{schema='app';table='PlatformAccountCredentialVersions'},
  @{schema='app';table='ClientContracts'},@{schema='platform';table='ClientPlatforms'}
 )
 $v2=Read-Data "SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez_v2') ORDER BY name;"
 foreach($row in $v2.Tables[0].Rows){
  $spec=@{schema='jahez_v2';table=[string]$row.name}
  if($row.name -eq 'AccountSessions'){$spec.allow='insertAssignments';$spec.idpath='sessionId'}
  $specs+=$spec
 }
 $checks=[System.Collections.Generic.List[string]]::new();$i=0
 foreach($spec in $specs){
  $schema=$spec.schema;$table=$spec.table;$qualified='['+$schema+'].['+$table.Replace(']',']]')+']';$temp='#Keep'+$i
  $metadata=Read-Data "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$schema.$table') ORDER BY column_id;"
  $cols=($metadata.Tables[0].Rows|ForEach-Object{'['+$_.name.Replace(']',']]')+']'}) -join ','
  $backup=Read-Data "SELECT * INTO $temp FROM $qualified;SELECT * FROM $temp;"
  $backup.WriteXml((Join-Path $out ($schema+'.'+$table+'-before.xml')),[System.Data.XmlWriteMode]::WriteSchema)
  if($table -eq 'PlatformRiderAccounts'){
   $unchanged=($metadata.Tables[0].Rows|Where-Object name -notin @('SponsorId','Status','StatusReason','UpdatedAtUtc','UpdatedByUserId','RowVersion')|ForEach-Object{'['+$_.name.Replace(']',']]')+']'}) -join ','
   $checks.Add("IF EXISTS(SELECT $unchanged FROM $temp EXCEPT SELECT $unchanged FROM $qualified)
    THROW 51000,'Existing account identity or unrelated fields changed.',1;")
   $outside="Id NOT IN(SELECT Id FROM OPENJSON(@PlanJson,'`$.accounts') WITH(Id uniqueidentifier '`$.accountId'))"
   $checks.Add("IF EXISTS(SELECT $cols FROM $temp WHERE $outside EXCEPT SELECT $cols FROM $qualified WHERE $outside)
    OR EXISTS(SELECT $cols FROM $qualified WHERE $outside EXCEPT SELECT $cols FROM $temp WHERE $outside)
    THROW 51000,'Accounts outside the source changed.',1;")
  }else{
   $exclude=if($spec.allow){" WHERE Id NOT IN(SELECT Id FROM OPENJSON(@PlanJson,'`$.$($spec.allow)') WITH(Id uniqueidentifier '`$.$($spec.idpath)'))"}else{''}
   $checks.Add("IF EXISTS(SELECT $cols FROM $temp EXCEPT SELECT $cols FROM $qualified)
    OR EXISTS(SELECT $cols FROM $qualified$exclude EXCEPT SELECT $cols FROM $temp)
    THROW 51000,'Existing/protected records changed: $schema.$table',1;")
  }
  $i++
 }
 $result=Read-Data (Get-Content -LiteralPath $sqlPath -Raw)
 $result.Tables[0].Rows[0].SummaryJson|Set-Content (Join-Path $out 'summary.json') -Encoding utf8
 [void](Read-Data ($checks -join [Environment]::NewLine))
 $verification=Read-Data (Get-Content -LiteralPath $verifyPath -Raw)
 $verification.Tables[0].TableName='Checks'
 $verification.WriteXml((Join-Path $out 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if(@($verification.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){$verification.Tables[0]|Format-Table;throw 'Import verification failed.'}
 if($Apply){
  $transaction.Commit();$transaction.Dispose();$transaction=$null
  $connection.Close();$connection.Open()
  $verification=Read-Data (Get-Content -LiteralPath $verifyPath -Raw)
  $verification.Tables[0].TableName='Checks'
  $verification.WriteXml((Join-Path $out 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if(@($verification.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){throw 'Post-commit verification failed. Do not replay.'}
  'COMMITTED AND VERIFIED ON A FRESH CONNECTION'
 }else{$transaction.Rollback();$transaction.Dispose();$transaction=$null;'DRY RUN VERIFIED AND ROLLED BACK'}
 $verification.Tables[0]|Format-Table -AutoSize
 $plan.summary|ConvertTo-Json -Depth 5
 Write-Output ('Output: '+$out)
}catch{if($transaction){try{$transaction.Rollback()}catch{}};throw}finally{if($transaction){$transaction.Dispose()};$connection.Dispose()}
