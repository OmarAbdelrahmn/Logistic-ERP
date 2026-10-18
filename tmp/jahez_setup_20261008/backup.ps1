param([string]$BackupDirectory='outputs/jahez-setup-20261008/backup',[string]$PlanFile='plan.json')
$ErrorActionPreference='Stop'
$taskBase=Join-Path (Get-Location).Path $BackupDirectory
if(Test-Path -LiteralPath $taskBase){throw 'Backup already exists; preserve it'}
[void](New-Item -ItemType Directory -Path $taskBase)
$taskConfig=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927'){throw 'Unexpected database'}
 $taskTables=@('app.PlatformRiderAccounts','app.RiderClientAssignments','app.RiderAssignmentEvents','app.ClientContracts',
   'jahez.JahezAccountHandover','jahez.JahezAccountFee','jahez.JahezLedgerEntry','jahez.JahezRiderSettlement','jahez.JahezCashboxEntry','jahez.JahezReminderState')
 foreach($taskTable in $taskTables){
  $taskCommand=$taskConnection.CreateCommand();$taskCommand.CommandTimeout=90
  $taskWhere=if($taskTable -in @('app.PlatformRiderAccounts','app.ClientContracts')){" WHERE ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')"}else{''}
  $taskCommand.CommandText="SELECT * FROM $taskTable$taskWhere FOR JSON PATH,INCLUDE_NULL_VALUES"
  $taskReader=$taskCommand.ExecuteReader();$taskBuffer=[System.Text.StringBuilder]::new()
  while($taskReader.Read()){[void]$taskBuffer.Append($taskReader.GetString(0))};$taskReader.Dispose()
  $taskText=if($taskBuffer.Length){$taskBuffer.ToString()}else{'[]'}
  [System.IO.File]::WriteAllText((Join-Path $taskBase "$taskTable.json"),$taskText,[System.Text.UTF8Encoding]::new($false))
  Write-Output "Saved $taskTable"
 }
}finally{$taskConnection.Dispose()}
Copy-Item -LiteralPath (Join-Path 'tmp/jahez_setup_20261008' $PlanFile) -Destination (Join-Path $taskBase 'import-plan.json')
Copy-Item -LiteralPath 'tmp/jahez_setup_20261008/source.json' -Destination (Join-Path $taskBase 'source.json')
$taskSource=Get-Content 'tmp/jahez_setup_20261008/source.json' -Raw|ConvertFrom-Json
Copy-Item -LiteralPath $taskSource.file -Destination (Join-Path $taskBase 'source.xlsx')
$taskHashes=@(Get-ChildItem -LiteralPath $taskBase -File | Where-Object Name -ne 'sha256.csv' | Get-FileHash -Algorithm SHA256 | Select-Object Hash,Path)
$taskHashes | Export-Csv (Join-Path $taskBase 'sha256.csv') -NoTypeInformation -Encoding UTF8
