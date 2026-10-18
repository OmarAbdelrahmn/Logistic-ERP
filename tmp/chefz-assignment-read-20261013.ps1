$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open()
 $cmd=$conn.CreateCommand(); $cmd.CommandTimeout=90
 $cmd.CommandText=@'
SELECT DB_NAME() DatabaseName, @@SERVERNAME ServerName;
SELECT Id,Code,NameAr FROM platform.ClientPlatforms WHERE Code IN ('SHIFTZ','CHEFZ') AND IsDeleted=0;
SELECT a.Id,a.ExternalAccountId,a.RegisteredEmployeeId,e.IqamaNo OwnerIqama,a.Status,a.PaymentModel,a.OperatingCityId,a.SponsorId,a.IsDeleted,d.ReportIdNumber,d.AccountType,d.DashboardStatus FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0 WHERE p.Code='SHIFTZ';
SELECT e.Id,e.IqamaNo,e.Iban,e.Status,e.IsEmployee,e.IsDeleted,r.Id RiderProfileId,r.IsDeleted RiderDeleted FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IqamaNo IS NOT NULL;
SELECT r.Id,r.RiderProfileId,r.PlatformRiderAccountId,r.PaymentModel,r.RiderAccountSlot,r.EffectiveFrom,r.EffectiveTo,r.Status,r.IsDeleted,a.ExternalAccountId,p.Code FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId;
SELECT c.* FROM app.ClientContracts c JOIN platform.ClientPlatforms p ON p.Id=c.ClientPlatformId WHERE p.Code='SHIFTZ';
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE (TABLE_SCHEMA='app' AND TABLE_NAME IN ('RiderClientAssignments','RiderAssignmentEvents','Employees','PlatformRiderAccounts','ClientContracts','DatasetVersions')) OR (TABLE_SCHEMA='audit' AND TABLE_NAME='AuditEntries');
SELECT Id,UserName,IsDeleted FROM [identity].Users WHERE UserName='Omar';
SELECT 'daily' Kind,COUNT(*) N FROM chefz.DailyPerformance UNION ALL SELECT 'settlements',COUNT(*) FROM chefz.WalletSettlements;
SELECT h.* FROM jahez.JahezAccountHandover h WHERE h.EndedAtUtc IS NULL AND h.IsDeleted=0;
SELECT h.Id HandoverId,h.RiderClientAssignmentId,l.Bucket,SUM(l.Amount) Balance FROM jahez.JahezAccountHandover h JOIN jahez.JahezLedgerEntry l ON l.HandoverId=h.Id WHERE h.EndedAtUtc IS NULL AND h.IsDeleted=0 GROUP BY h.Id,h.RiderClientAssignmentId,l.Bucket;
SELECT h.Id HandoverId,s.ThroughDate,s.CountsAsSettlement FROM jahez.JahezAccountHandover h JOIN jahez.JahezRiderSettlement s ON s.HandoverId=h.Id WHERE h.EndedAtUtc IS NULL AND h.IsDeleted=0;
'@
 $ds=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$ad.Fill($ds)
 $names=@('target','platforms','accounts','employees','assignments','contracts','columns','actors','financialCounts','handovers','ledgerBalances','settlements')
 $out=@{}
 for($i=0;$i -lt $names.Count;$i++) {
  $rows=@(foreach($row in $ds.Tables[$i].Rows){$obj=[ordered]@{};foreach($col in $ds.Tables[$i].Columns){$v=$row[$col.ColumnName];$obj[$col.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [DateTime]){$v.ToString('yyyy-MM-ddTHH:mm:ss')}else{$v}};[pscustomobject]$obj})
  $out[$names[$i]]=$rows
  Write-Output ($names[$i]+': '+$rows.Count)
 }
 $out | ConvertTo-Json -Depth 12 | Set-Content 'tmp/chefz-assignment-db-20261013.json' -Encoding utf8
 $out.target | Format-Table; $out.platforms | Format-Table; $out.contracts | Select-Object Id,Code,Status,IsDeleted | Format-Table; $out.financialCounts | Format-Table
}finally{$conn.Dispose()}
