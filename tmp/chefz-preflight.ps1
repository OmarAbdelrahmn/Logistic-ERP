$ErrorActionPreference='Stop'
$config=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open()
 $q=$c.CreateCommand()
 $q.CommandText=@'
SELECT p.Id,p.Code,p.NameAr,COUNT(a.Id) Accounts FROM platform.ClientPlatforms p LEFT JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=p.Id WHERE p.Code IN ('CHEFZ','THECHEFZ','CHEFSZ','SHIFTZ') GROUP BY p.Id,p.Code,p.NameAr;
SELECT Id,EmployerIdentityNumber,UnifiedNationalNumber,RegistryNameAr FROM app.Sponsors;
SELECT name FROM sys.tables WHERE name LIKE '%Assignment%' OR name LIKE '%Performance%' OR name LIKE '%Account%' OR name LIKE '%Cities%';
'@
 $d=[System.Data.DataSet]::new(); $a=[System.Data.SqlClient.SqlDataAdapter]::new($q); [void]$a.Fill($d)
 foreach($t in $d.Tables){$t | Format-Table -AutoSize}
} finally {$c.Dispose()}

