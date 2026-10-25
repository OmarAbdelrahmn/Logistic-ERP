$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root 'outputs/ninja-comparison-20261022'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$cfg=Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open()
 $q=$conn.CreateCommand();$q.CommandTimeout=90;$q.CommandText=@'
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRANSACTION;
SELECT a.Id,a.Code,a.ExternalAccountId,a.Status,a.IsDeleted,e.IqamaNo,e.FullNameAr,e.FullNameEn,e.Status OwnerStatus,e.IsDeleted OwnerDeleted,g.Code CityCode,g.NameAr CityAr,g.NameEn CityEn,s.EmployerIdentityNumber SponsorRegistry,s.RegistryNameAr SponsorName
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
LEFT JOIN app.OperatingCities c ON c.Id=a.OperatingCityId LEFT JOIN platform.GlobalCities g ON g.Id=c.GlobalCityId
LEFT JOIN app.Sponsors s ON s.Id=a.SponsorId
WHERE p.Code='NINJA';
SELECT * FROM ninja.NinjaAccountDetails;
SELECT SYSUTCDATETIME() SnapshotUtc,DB_NAME() DatabaseName;
COMMIT;
'@
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $result=[ordered]@{}
 $names=@('accounts','details','snapshot')
 for($i=0;$i -lt $names.Count;$i++){
 $result[$names[$i]]=@(foreach($r in $d.Tables[$i].Rows){$o=[ordered]@{};foreach($c in $d.Tables[$i].Columns){$v=$r[$c.ColumnName];$o[$c.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [byte[]]){[Convert]::ToBase64String($v)}elseif($v -is [DateTime]){$v.ToString('yyyy-MM-ddTHH:mm:ssZ')}else{$v}};[pscustomobject]$o})
 }
 $result | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $out 'database-snapshot.json') -Encoding utf8
 [pscustomobject]@{Accounts=$result.accounts.Count;Details=$result.details.Count;Snapshot=$result.snapshot[0]} | ConvertTo-Json -Compress
} finally {$conn.Dispose()}
