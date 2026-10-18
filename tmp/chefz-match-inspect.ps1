$ErrorActionPreference='Stop'
$config=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
WITH s AS (SELECT IdNumber FROM OPENJSON(@j,'$.accounts') WITH(IdNumber nvarchar(100) '$.id'))
SELECT s.IdNumber MissingExternal,a.Id AccountByOwner,a.ExternalAccountId,e.IqamaNo,e.IsDeleted OwnerDeleted FROM s
LEFT JOIN app.Employees e ON e.IqamaNo=s.IdNumber
LEFT JOIN app.PlatformRiderAccounts a ON a.RegisteredEmployeeId=e.Id AND a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='SHIFTZ')
WHERE NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ' AND a.IsDeleted=0 AND a.ExternalAccountId=s.IdNumber);
SELECT a.Id,a.ExternalAccountId,e.IqamaNo,a.IsDeleted,a.Status,d.ReportIdNumber FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id WHERE p.Code='SHIFTZ' AND NOT EXISTS(SELECT 1 FROM OPENJSON(@j,'$.accounts') WITH(IdNumber nvarchar(100) '$.id') s WHERE s.IdNumber=a.ExternalAccountId);
'@
 [void]$q.Parameters.Add('@j',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@j'].Value=Get-Content tmp/chefz-update-source.json -Raw
 $d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d);foreach($t in $d.Tables){$t | Format-Table -AutoSize}
}finally{$c.Dispose()}
