param([switch]$Commit,[string]$PlanFile='plan.json',[int]$ExpectedCount=199)
$ErrorActionPreference='Stop'
$taskRoot=Join-Path (Get-Location).Path 'tmp/jahez_setup_20261008'
$taskPlan=Get-Content (Join-Path $taskRoot $PlanFile) -Raw
$taskSource=Get-Content (Join-Path $taskRoot 'source.json') -Raw|ConvertFrom-Json
$taskSourceStream=[System.IO.FileStream]::new($taskSource.file,[System.IO.FileMode]::Open,[System.IO.FileAccess]::Read,[System.IO.FileShare]::ReadWrite)
$taskHasher=[System.Security.Cryptography.SHA256]::Create()
try{$taskSourceHash=[System.BitConverter]::ToString($taskHasher.ComputeHash($taskSourceStream)).Replace('-','').ToLowerInvariant()}
finally{$taskSourceStream.Dispose();$taskHasher.Dispose()}
if ($taskSourceHash -ne $taskSource.sha256.ToLowerInvariant()) {throw 'Source workbook changed since validation'}
$taskConfig=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927'){throw 'Unexpected database'}
 $taskCommand=$taskConnection.CreateCommand()
 $taskCommand.CommandTimeout=120
 $taskCommand.CommandText=Get-Content (Join-Path $taskRoot 'import.sql') -Raw
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1)
 $taskCommand.Parameters['@Plan'].Value=$taskPlan
 [void]$taskCommand.Parameters.Add('@Commit',[System.Data.SqlDbType]::Bit)
 $taskCommand.Parameters['@Commit'].Value=$Commit.IsPresent
 [void]$taskCommand.Parameters.Add('@ExpectedCount',[System.Data.SqlDbType]::Int)
 $taskCommand.Parameters['@ExpectedCount'].Value=$ExpectedCount
 $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 $taskResults=[System.Data.DataSet]::new()
 [void]$taskAdapter.Fill($taskResults)
 foreach($taskTable in $taskResults.Tables){$taskTable|Format-Table -AutoSize|Out-String|Write-Output}
 Write-Output $(if($Commit){'COMMITTED'}else{'DRY RUN rolled back'})
}finally{$taskConnection.Dispose()}
