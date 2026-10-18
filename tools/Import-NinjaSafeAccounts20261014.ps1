param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$planPath=Join-Path $workspace 'tmp/ninja-safe-plan-20261014.json'
$planJson=Get-Content -LiteralPath $planPath -Raw
$plan=$planJson | ConvertFrom-Json
if((Get-FileHash -LiteralPath $plan.sourcePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $plan.sourceSha256){throw 'Source workbook changed since preflight.'}
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
$tx=$null
function Read-Data([string]$sql){
 $q=$conn.CreateCommand();$q.Transaction=$tx;$q.CommandTimeout=90;$q.CommandText=$sql
 [void]$q.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@PlanJson'].Value=$planJson
 [void]$q.Parameters.Add('@CorrelationId',[System.Data.SqlDbType]::NVarChar,100);$q.Parameters['@CorrelationId'].Value=$plan.correlationId
 [void]$q.Parameters.Add('@SourceHash',[System.Data.SqlDbType]::VarChar,64);$q.Parameters['@SourceHash'].Value=$plan.sourceSha256
 try{$d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);try{[void]$ad.Fill($d)}finally{$ad.Dispose()};return ,$d}finally{$q.Dispose()}
}
try{
 $conn.Open()
 if($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $out=Join-Path $workspace ('outputs/ninja-safe-import-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+$(if($Apply){'-applied'}else{'-review'}))
 [void](New-Item -ItemType Directory -Path $out -Force)
 $tx=$conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $backupSql=@'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT * INTO #BeforeAccounts FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeAssignments FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeEmployees FROM app.Employees WITH(HOLDLOCK);
SELECT * INTO #BeforeRiderProfiles FROM app.RiderProfiles WITH(HOLDLOCK);
SELECT * INTO #BeforeRegistrations FROM app.PlatformAccountRegistrations WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeContracts FROM app.ClientContracts WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeDetails FROM ninja.NinjaAccountDetails WITH(UPDLOCK,HOLDLOCK);
SELECT (SELECT COUNT_BIG(*) FROM app.PlatformAccountCredentialVersions WITH(HOLDLOCK)) CredentialCount,
 (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyPerformance WITH(HOLDLOCK)) PerformanceCount,
 (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyDeductions WITH(HOLDLOCK)) DeductionCount,
 (SELECT COUNT_BIG(*) FROM ninja.NinjaDailyImports WITH(HOLDLOCK)) ImportCount INTO #BeforeCounts;
SELECT * FROM #BeforeAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId'))
 OR RiderProfileId IN(SELECT RiderId FROM OPENJSON(@PlanJson,'$.rows') WITH(RiderId uniqueidentifier '$.riderId'));
SELECT * FROM #BeforeRegistrations WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * FROM #BeforeContracts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * FROM #BeforeDetails;
SELECT * FROM #BeforeCounts;
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('ninja','platform-operations');
'@
 Copy-Item -LiteralPath $planPath -Destination (Join-Path $out 'plan.json')
 Copy-Item -LiteralPath (Join-Path $workspace 'tmp/ninja-source-20261013.json') -Destination (Join-Path $out 'source.json')
 $sqlPath=Join-Path $workspace 'database/scripts/ninja-safe-import-20261014.sql'
 Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $out 'applied-script.sql')
 # Keep temporary snapshots and the import in the same parameterized SQL batch.
 $result=Read-Data ($backupSql+[Environment]::NewLine+(Get-Content -LiteralPath $sqlPath -Raw))
 $names=@('NinjaAccountsBefore','SelectedRiderAssignmentsBefore','RegistrationsBefore','ContractsBefore','DetailsBefore','ProtectedCounts','DatasetVersionsBefore')
 $backup=[System.Data.DataSet]::new()
 for($i=0;$i -lt $names.Count;$i++){$table=$result.Tables[$i].Copy();$table.TableName=$names[$i];[void]$backup.Tables.Add($table)}
 $backup.WriteXml((Join-Path $out 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $summary=$result.Tables[7].Rows[0].SummaryJson
 $summary | Set-Content -LiteralPath (Join-Path $out 'summary.json') -Encoding utf8
 $verificationSql=Get-Content -LiteralPath (Join-Path $workspace 'database/scripts/verify-ninja-safe-import-20261014.sql') -Raw
 $verification=Read-Data $verificationSql
 if(@($verification.Tables[0].Rows | Where-Object Mismatches -ne 0).Count){throw 'Transaction verification failed.'}
 $verification.Tables[0].TableName='Checks';$verification.WriteXml((Join-Path $out 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
   $tx.Commit();$tx.Dispose();$tx=$null
   $conn.Close();$conn.Open()
   $post=Read-Data $verificationSql
   $post.Tables[0].TableName='Checks';$post.WriteXml((Join-Path $out 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
   if(@($post.Tables[0].Rows | Where-Object Mismatches -ne 0).Count){throw 'Committed data verification failed; do not rerun the import.'}
   $post.Tables[0] | Format-Table -AutoSize
   'COMMITTED AND VERIFIED USING A FRESH CONNECTION'
 }else{$tx.Rollback();$tx.Dispose();$tx=$null;'DRY RUN VERIFIED AND ROLLED BACK'}
 $summary
 Write-Output ('Output: '+$out)
}catch{if($tx){try{$tx.Rollback()}catch{}};throw}finally{if($tx){$tx.Dispose()};$conn.Dispose()}
