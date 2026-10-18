$ErrorActionPreference='Stop'
$taskConfig=Get-Content (Join-Path $PSScriptRoot '../src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskCs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$taskConfig.ConnectionStrings.LogisticsDatabase}
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskCs)
try {
 $taskConnection.Open()
 $taskCommand=$taskConnection.CreateCommand()
 $taskCommand.CommandText=@'
SELECT a.Id,a.ExternalAccountId,p.Code,a.Status FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE a.ExternalAccountId='476832' AND a.IsDeleted=0;
SELECT r.*,e.IqamaNo,e.FullNameAr,e.Status EmployeeStatus FROM app.RiderClientAssignments r JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId JOIN app.Employees e ON e.Id=rp.EmployeeId WHERE r.PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ExternalAccountId='476832') ORDER BY r.EffectiveFrom;
SELECT e.Id EmployeeId,e.IqamaNo,e.FullNameAr,e.Status,rp.Id RiderProfileId,rp.IsDeleted ProfileDeleted FROM app.Employees e LEFT JOIN app.RiderProfiles rp ON rp.EmployeeId=e.Id WHERE e.IqamaNo='2493090191' AND e.IsDeleted=0;
SELECT r.Id,r.PlatformRiderAccountId,a.ExternalAccountId,p.Code,r.Status,r.EffectiveFrom,r.EffectiveTo FROM app.RiderClientAssignments r JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId JOIN app.Employees e ON e.Id=rp.EmployeeId JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE e.IqamaNo='2493090191' AND r.IsDeleted=0 AND r.EffectiveTo IS NULL;
SELECT * FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId='196a2a75-de62-4757-80da-0a13beb12b85';
SELECT t.name TableName,c.name ColumnName FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE c.name IN('RiderProfileId','RiderClientAssignmentId','HandoverId') AND (t.schema_id=SCHEMA_ID('jahez_v2') OR t.name LIKE '%Assignment%');
SELECT 'Entries' Kind,COUNT(*) N FROM jahez_v2.AccountingEntries WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId='196a2a75-de62-4757-80da-0a13beb12b85')
UNION ALL SELECT 'Operations',COUNT(*) FROM jahez_v2.Operations WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId='196a2a75-de62-4757-80da-0a13beb12b85')
UNION ALL SELECT 'Movements',COUNT(*) FROM jahez_v2.MoneyMovements WHERE HandoverId IN(SELECT Id FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId='196a2a75-de62-4757-80da-0a13beb12b85');
'@
 $taskData=[System.Data.DataSet]::new()
 $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 [void]$taskAdapter.Fill($taskData)
 for($taskIndex=0;$taskIndex -lt $taskData.Tables.Count;$taskIndex++) {
  Write-Output ('Table '+$taskIndex)
  $taskData.Tables[$taskIndex] | Select-Object * -ExcludeProperty RowError,RowState,Table,ItemArray,HasErrors | ConvertTo-Json -Depth 4
 }
} finally {$taskConnection.Dispose()}
