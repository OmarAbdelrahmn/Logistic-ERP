$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open()
 $cmd=$conn.CreateCommand(); $cmd.CommandTimeout=90
 $cmd.CommandText=@'
SELECT p.Id,p.Code,p.NameAr FROM platform.ClientPlatforms p WHERE p.Code IN ('SHIFTZ','CHEFZ') AND p.IsDeleted=0;
SELECT a.Id,a.ExternalAccountId,a.RegisteredEmployeeId,e.IqamaNo OwnerIqama,a.Status,a.PaymentModel,a.IsDeleted,d.ReportIdNumber FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0 WHERE p.Code IN ('SHIFTZ','CHEFZ') AND a.IsDeleted=0;
SELECT r.Id,r.RiderProfileId,r.PlatformRiderAccountId,r.EffectiveFrom,r.EffectiveTo,r.Status,r.IsDeleted,a.ExternalAccountId,p.Code,e.IqamaNo RiderIqama FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId LEFT JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId LEFT JOIN app.Employees e ON e.Id=rp.EmployeeId WHERE r.IsDeleted=0;
SELECT e.Id,e.IqamaNo,e.Status,e.IsEmployee,e.IsDeleted,r.Id RiderProfileId,r.IsDeleted RiderDeleted FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IqamaNo IS NOT NULL;
SELECT SYSUTCDATETIME() ReadAtUtc;
'@
 $ds=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$ad.Fill($ds)
 $names=@('platforms','accounts','assignments','employees','capture')
 $out=@{}
 for($i=0;$i -lt $names.Count;$i++) {
  $rows=@(foreach($row in $ds.Tables[$i].Rows){$obj=[ordered]@{};foreach($col in $ds.Tables[$i].Columns){$v=$row[$col.ColumnName];$obj[$col.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [DateTime]){$v.ToString('yyyy-MM-ddTHH:mm:ss')}else{$v}};[pscustomobject]$obj})
  $out[$names[$i]]=$rows
  Write-Output ($names[$i]+': '+$rows.Count)
 }
 $out | ConvertTo-Json -Depth 12 | Set-Content 'tmp/chefz-comparison-current-20261013.json' -Encoding utf8
}finally{$conn.Dispose()}
