param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$planPath=Join-Path $workspace 'tmp/chefz-assignment-plan-20261013.json'
$planJson=Get-Content -LiteralPath $planPath -Raw
$plan=$planJson | ConvertFrom-Json
$sourceStream=[System.IO.File]::Open('C:/Users/omarf/Downloads/تقرير سيستم  داتا بدلاء .xlsx',[System.IO.FileMode]::Open,[System.IO.FileAccess]::Read,[System.IO.FileShare]::ReadWrite)
try{$sha=[System.Security.Cryptography.SHA256]::Create();try{$sourceHash=[Convert]::ToHexString($sha.ComputeHash($sourceStream)).ToLowerInvariant()}finally{$sha.Dispose()}}finally{$sourceStream.Dispose()}
if($sourceHash -ne $plan.sourceSha256){throw 'Workbook changed since extraction.'}
$correlation='chefz-rider-sheet-20261013-88db9f8f'
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
$tx=$null
function Read-Data([string]$sql) {
 $cmd=$conn.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandTimeout=90;$cmd.CommandText=$sql
 [void]$cmd.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$cmd.Parameters['@PlanJson'].Value=$planJson
 [void]$cmd.Parameters.Add('@CorrelationId',[System.Data.SqlDbType]::NVarChar,200);$cmd.Parameters['@CorrelationId'].Value=$correlation
 try{$ds=[System.Data.DataSet]::new();$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);try{[void]$adapter.Fill($ds)}finally{$adapter.Dispose()};return ,$ds}finally{$cmd.Dispose()}
}
try {
 $conn.Open()
 if($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database.'}
 $out=Join-Path $workspace ('outputs/chefz-rider-sheet-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')+$(if($Apply){'-applied'}else{'-review'}))
 [void](New-Item -ItemType Directory -Path $out -Force)
 $tx=$conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $backup=Read-Data @'
SELECT a.* FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK) WHERE a.ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId')
 OR a.Id IN(SELECT AccountId FROM OPENJSON(@PlanJson,'$.removeJahez') WITH(AccountId uniqueidentifier '$.accountId'))
 OR a.Id IN(SELECT AccountId FROM OPENJSON(@PlanJson,'$.moves') WITH(AccountId uniqueidentifier '$.accountId'));
SELECT e.* FROM app.Employees e WITH(UPDLOCK,HOLDLOCK) WHERE e.Id IN(SELECT EmployeeId FROM OPENJSON(@PlanJson,'$.ibanUpdates') WITH(EmployeeId uniqueidentifier '$.employeeId'));
SELECT a.* FROM app.RiderClientAssignments a WITH(UPDLOCK,HOLDLOCK) WHERE a.PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId'))
 OR a.Id IN(SELECT AssignmentId FROM OPENJSON(@PlanJson,'$.removeJahez') WITH(AssignmentId uniqueidentifier '$.assignmentId'))
 OR a.Id IN(SELECT AssignmentId FROM OPENJSON(@PlanJson,'$.moves') WITH(AssignmentId uniqueidentifier '$.assignmentId'));
SELECT e.* FROM app.RiderAssignmentEvents e WHERE e.RiderClientAssignmentId IN(SELECT AssignmentId FROM OPENJSON(@PlanJson,'$.removeJahez') WITH(AssignmentId uniqueidentifier '$.assignmentId'))
 OR e.RiderClientAssignmentId IN(SELECT AssignmentId FROM OPENJSON(@PlanJson,'$.moves') WITH(AssignmentId uniqueidentifier '$.assignmentId'));
SELECT h.* INTO #BackupHandovers FROM jahez.JahezAccountHandover h WITH(UPDLOCK,HOLDLOCK) WHERE h.Id IN(SELECT HandoverId FROM OPENJSON(@PlanJson,'$.removeJahez') WITH(HandoverId uniqueidentifier '$.handoverId'))
 OR h.Id IN(SELECT HandoverId FROM OPENJSON(@PlanJson,'$.moves') WITH(HandoverId uniqueidentifier '$.handoverId'));
SELECT * FROM #BackupHandovers;
SELECT l.* FROM jahez.JahezLedgerEntry l JOIN #BackupHandovers h ON h.Id=l.HandoverId;
SELECT f.* FROM jahez.JahezAccountFee f JOIN #BackupHandovers h ON h.Id=f.HandoverId;
SELECT * FROM app.DatasetVersions WHERE ModuleKey IN('chefz','jahez','platform-operations','workforce');
'@
 $names=@('Accounts','Employees','Assignments','AssignmentEvents','JahezHandovers','JahezLedger','JahezFees','DatasetVersions')
 for($i=0;$i -lt $names.Count;$i++){$backup.Tables[$i].TableName=$names[$i]}
 $backup.WriteXml((Join-Path $out 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 Copy-Item -LiteralPath $planPath -Destination (Join-Path $out 'plan.json')
 Copy-Item -LiteralPath (Join-Path $workspace 'tmp/chefz-assignment-workbook-20261013.json') -Destination (Join-Path $out 'source.json')
 $sql=Get-Content (Join-Path $workspace 'database/scripts/chefz-rider-sheet-import-20261013.sql') -Raw
 $result=Read-Data $sql
 $summary=$result.Tables[0].Rows[0].SummaryJson
 $summary | Set-Content (Join-Path $out 'summary.json') -Encoding utf8
 if($Apply){$tx.Commit();$tx.Dispose();$tx=$null;'COMMITTED'}else{$tx.Rollback();$tx.Dispose();$tx=$null;'DRY RUN VERIFIED AND ROLLED BACK'}
 $summary
 Write-Output ('Output: '+$out)
}catch{if($tx){$tx.Rollback()};throw}finally{if($tx){$tx.Dispose()};$conn.Dispose()}
