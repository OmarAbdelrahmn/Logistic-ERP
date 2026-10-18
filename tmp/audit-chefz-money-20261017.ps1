param([string]$OutputDirectory='outputs/chefz-money-audit-20261017')
$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.LogisticsDatabase)
if($builder.DataSource -ne 'db67927.public.databaseasp.net' -or $builder.InitialCatalog -ne 'db67927'){throw 'Unexpected database target'}
$builder['Application Name']='ChefzReadOnlyMoneyAudit20261017'
$connection=[System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$outDir=$OutputDirectory
[void](New-Item -ItemType Directory -Path $outDir -Force)
$sql=@'
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRANSACTION;
SELECT DB_NAME() DatabaseName,SYSUTCDATETIME() ReadAtUtc;
SELECT Id,FileName,ContentHash,SourceRows,CreatedAtUtc FROM chefz.OrderSettlementImports;
SELECT s.* FROM chefz.WalletSettlements s WHERE s.PeriodFrom >= '20261001' AND s.PeriodFrom < '20261101';
SELECT d.PlatformRiderAccountId,d.ReportIdNumber,d.AccountType,d.IsDeleted,a.ExternalAccountId,a.IsDeleted AccountDeleted
 FROM chefz.AccountDetails d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId;
SELECT a.Id,a.PlatformRiderAccountId,a.RiderProfileId,a.EffectiveFrom,a.EffectiveTo,a.Status,a.IsDeleted,e.FullNameAr RiderName
 FROM app.RiderClientAssignments a JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.PlatformRiderAccountId
 LEFT JOIN app.RiderProfiles r ON r.Id=a.RiderProfileId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId
 WHERE a.EffectiveFrom <= '20261031' AND (a.EffectiveTo IS NULL OR a.EffectiveTo >= '20261001');
SELECT name,definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('chefz.WalletSettlements');
SELECT COUNT(*) InvalidArithmetic FROM chefz.WalletSettlements WHERE CompanyAmount+RiderAmount<>GrossEarnings
 OR (Source='Orders' AND GrossEarnings<>DeliveryFees-VatAmount+Tips+Compensation+Bonuses-Deductions);
SELECT RiderClientAssignmentId,PeriodFrom,COUNT(*) N FROM chefz.WalletSettlements GROUP BY RiderClientAssignmentId,PeriodFrom HAVING COUNT(*)>1;
SELECT Id,RiderClientAssignmentId,OccurredAtUtc,Reason,ChangeSnapshotJson FROM app.RiderAssignmentEvents
 WHERE RiderClientAssignmentId='ce52e580-07b7-4476-9711-8a48db7039bb' ORDER BY OccurredAtUtc;
SELECT Id,Action,OccurredAtUtc,Reason,BeforeJson,AfterJson FROM audit.AuditEntries
 WHERE EntityId='ce52e580-07b7-4476-9711-8a48db7039bb' ORDER BY OccurredAtUtc;
COMMIT TRANSACTION;
'@
try {
 $connection.Open();$command=$connection.CreateCommand();$command.CommandText=$sql;$command.CommandTimeout=90
 $dataset=[System.Data.DataSet]::new();$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command);[void]$adapter.Fill($dataset)
 $names=@('target','imports','settlements','accounts','assignments','constraints','invalidArithmetic','duplicateSettlements','assignmentEvents','assignmentAudit');$result=[ordered]@{}
 for($i=0;$i -lt $names.Count;$i++){
  $result[$names[$i]]=@(foreach($row in $dataset.Tables[$i].Rows){$obj=[ordered]@{};foreach($col in $dataset.Tables[$i].Columns){$value=$row[$col.ColumnName];$obj[$col.ColumnName]=if($value -is [DBNull]){$null}elseif($value -is [DateTime]){$value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff')}elseif($value -is [byte[]]){[Convert]::ToBase64String($value)}else{$value}};[pscustomobject]$obj})
  Write-Output ($names[$i]+': '+$result[$names[$i]].Count)
 }
 $result|ConvertTo-Json -Depth 10|Set-Content (Join-Path $outDir 'database-snapshot.json') -Encoding utf8
 $sql|Set-Content (Join-Path $outDir 'read-only-queries.sql') -Encoding utf8
 $result.target|ConvertTo-Json -Compress|Write-Output
 $result.imports|Select-Object Id,FileName,SourceRows,CreatedAtUtc|ConvertTo-Json -Compress|Write-Output
}finally{$connection.Dispose()}
