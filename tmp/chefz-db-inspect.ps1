$ErrorActionPreference='Stop'
$config=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open(); $q=$c.CreateCommand(); $q.CommandText=@'
SELECT COUNT(*) Accounts,COUNT(d.Id) Details,COUNT(e.Id) Owners FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId WHERE p.Code='SHIFTZ';
SELECT TOP 4 a.Id,a.ExternalAccountId,e.IqamaNo,a.Status,d.ReportIdNumber FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id WHERE p.Code='SHIFTZ';
SELECT COUNT(*) Assignments FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ';
SELECT SCHEMA_NAME(t.schema_id) S,t.name TableName,c.name ColumnName,OBJECT_NAME(f.referenced_object_id) Target FROM sys.foreign_key_columns f JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id WHERE f.referenced_object_id=OBJECT_ID('app.RiderClientAssignments');
SELECT o.Id,g.NameAr FROM app.OperatingCities o JOIN platform.GlobalCities g ON g.Id=o.GlobalCityId;
SELECT 'daily' T,COUNT(*) C FROM chefz.DailyPerformance UNION ALL SELECT 'settlement',COUNT(*) FROM chefz.WalletSettlements;
'@
 $d=[System.Data.DataSet]::new(); $a=[System.Data.SqlClient.SqlDataAdapter]::new($q); [void]$a.Fill($d); foreach($t in $d.Tables){$t | Format-Table -AutoSize}
} finally {$c.Dispose()}
