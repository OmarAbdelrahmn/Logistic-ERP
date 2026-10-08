$ErrorActionPreference = 'Stop'
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$conn = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
  $conn.Open()
  if ($conn.Database -ne 'db67927') { throw 'Unexpected database' }
  $q=$conn.CreateCommand()
  $q.CommandText=@'
CREATE TABLE #Owners(Iqama nvarchar(20) NOT NULL PRIMARY KEY);
CREATE TABLE #Accounts(Platform nvarchar(20), ExternalId nvarchar(150), OwnerIqama nvarchar(20), City nvarchar(100), Sponsor nvarchar(200), SourceStatus nvarchar(100));
'@
  [void]$q.ExecuteNonQuery()
  $csv=Import-Csv 'tmp/platform_upload/accounts.csv' -Encoding UTF8
  $owners=[System.Data.DataTable]::new(); [void]$owners.Columns.Add('Iqama',[string]); foreach($iqama in ($csv.owner_iqama | Select-Object -Unique)){ [void]$owners.Rows.Add($iqama) }
  $accounts=[System.Data.DataTable]::new(); foreach($name in @('Platform','ExternalId','OwnerIqama','City','Sponsor','SourceStatus')){[void]$accounts.Columns.Add($name,[string])}
  foreach($r in $csv){[void]$accounts.Rows.Add($r.platform,$r.external_id,$r.owner_iqama,$r.city,$r.sponsor,$r.status)}
  $bulk=[System.Data.SqlClient.SqlBulkCopy]::new($conn); $bulk.DestinationTableName='#Owners'; $bulk.WriteToServer($owners); $bulk.Dispose()
  $bulk=[System.Data.SqlClient.SqlBulkCopy]::new($conn); $bulk.DestinationTableName='#Accounts'; $bulk.WriteToServer($accounts); $bulk.Dispose()
  $sql=@'
SELECT Code,NameEn,NameAr,Status,SupportedPaymentModels FROM platform.ClientPlatforms ORDER BY Code;
SELECT 'owners_in_file' AS Metric,COUNT(*) AS N FROM #Owners
UNION ALL SELECT 'owners_matched',COUNT(*) FROM #Owners o WHERE EXISTS(SELECT 1 FROM app.Employees e WHERE e.IqamaNo=o.Iqama AND e.IsDeleted=0)
UNION ALL SELECT 'owners_with_rider_profile',COUNT(*) FROM #Owners o WHERE EXISTS(SELECT 1 FROM app.Employees e JOIN app.RiderProfiles rp ON rp.EmployeeId=e.Id WHERE e.IqamaNo=o.Iqama AND e.IsDeleted=0 AND rp.IsDeleted=0)
UNION ALL SELECT 'owners_ambiguous',COUNT(*) FROM #Owners o WHERE (SELECT COUNT(*) FROM app.Employees e WHERE e.IqamaNo=o.Iqama AND e.IsDeleted=0)>1;
SELECT a.Platform,COUNT(*) AS Rows,COUNT(DISTINCT a.OwnerIqama) AS Owners,
SUM(CASE WHEN e.Id IS NULL THEN 1 ELSE 0 END) AS RowsMissingOwner,
SUM(CASE WHEN e.Id IS NOT NULL AND rp.Id IS NULL THEN 1 ELSE 0 END) AS RowsWithoutRiderProfile
FROM #Accounts a LEFT JOIN app.Employees e ON e.IqamaNo=a.OwnerIqama AND e.IsDeleted=0
LEFT JOIN app.RiderProfiles rp ON rp.EmployeeId=e.Id AND rp.IsDeleted=0 GROUP BY a.Platform;
SELECT a.City,COUNT(*) AS N,oc.Id AS OperatingCityId FROM #Accounts a
LEFT JOIN platform.GlobalCities gc ON gc.NameAr=a.City AND gc.IsDeleted=0
LEFT JOIN app.OperatingCities oc ON oc.GlobalCityId=gc.Id AND oc.IsDeleted=0 GROUP BY a.City,oc.Id;
SELECT a.Sponsor,COUNT(*) AS N,s.Id AS SponsorId FROM #Accounts a
LEFT JOIN app.Sponsors s ON s.RegistryNameAr=REPLACE(REPLACE(a.Sponsor,N'شركة ',N''),N'مؤسسة ',N'') AND s.IsDeleted=0
GROUP BY a.Sponsor,s.Id;
SELECT a.SourceStatus,COUNT(*) AS N FROM #Accounts a GROUP BY a.SourceStatus;
SELECT a.Id,a.Code,a.ExternalAccountId,a.Status,a.IsDeleted,CASE WHEN e.Id IS NULL THEN 0 ELSE 1 END AS HasOwner
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
WHERE p.Code IN ('JAHEZ','HUNGER','CHEFSZ');
SELECT 'registrations' AS Dep,COUNT(*) AS N FROM app.PlatformAccountRegistrations d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','CHEFSZ')
UNION ALL SELECT 'assignments',COUNT(*) FROM app.RiderClientAssignments d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','CHEFSZ')
UNION ALL SELECT 'credentials',COUNT(*) FROM app.PlatformAccountCredentialVersions d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','CHEFSZ')
UNION ALL SELECT 'jahez_handovers',COUNT(*) FROM jahez.JahezAccountHandover d JOIN app.PlatformRiderAccounts a ON a.Id=d.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','CHEFSZ');
SELECT a.Platform,a.City,a.Sponsor,a.SourceStatus,COUNT(*) AS N,s.RegistryNameAr AS OwnerSponsor
FROM #Accounts a LEFT JOIN app.Employees e ON e.IqamaNo=a.OwnerIqama AND e.IsDeleted=0
LEFT JOIN app.Sponsors s ON s.Id=e.SponsorId
WHERE a.Sponsor=N'خارج الكفالة' GROUP BY a.Platform,a.City,a.Sponsor,a.SourceStatus,s.RegistryNameAr;
SELECT e.IsEmployee,e.Status,COUNT(DISTINCT e.Id) AS OwnersWithoutProfile FROM #Owners o
JOIN app.Employees e ON e.IqamaNo=o.Iqama AND e.IsDeleted=0
LEFT JOIN app.RiderProfiles rp ON rp.EmployeeId=e.Id AND rp.IsDeleted=0
WHERE rp.Id IS NULL GROUP BY e.IsEmployee,e.Status;
SELECT a.Platform,a.OwnerIqama,COUNT(*) AS N,MAX(CASE WHEN e.IsDeleted=1 THEN 1 ELSE 0 END) AS HasDeletedEmployee
FROM #Accounts a LEFT JOIN app.Employees e ON e.IqamaNo=a.OwnerIqama
WHERE NOT EXISTS(SELECT 1 FROM app.Employees live WHERE live.IqamaNo=a.OwnerIqama AND live.IsDeleted=0)
GROUP BY a.Platform,a.OwnerIqama;
SELECT a.Code,a.ExternalAccountId,ra.Status AS AssignmentStatus,ja.Id AS HandoverId
FROM app.PlatformRiderAccounts a LEFT JOIN app.RiderClientAssignments ra ON ra.PlatformRiderAccountId=a.Id
LEFT JOIN jahez.JahezAccountHandover ja ON ja.PlatformRiderAccountId=a.Id
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='JAHEZ';
SELECT 'handover_commission_policy' AS Dep,COUNT(*) AS N FROM jahez.JahezCommissionPolicyPeriod d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_cashbox',COUNT(*) FROM jahez.JahezCashboxEntry d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_dispatch',COUNT(*) FROM jahez.JahezDailyDispatch d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_transactions',COUNT(*) FROM jahez.JahezTransaction d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_fees',COUNT(*) FROM jahez.JahezAccountFee d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_approvals',COUNT(*) FROM jahez.JahezApprovalRequest d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_earnings',COUNT(*) FROM jahez.JahezEarningsStatement d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_ledger',COUNT(*) FROM jahez.JahezLedgerEntry d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_reminders',COUNT(*) FROM jahez.JahezReminderState d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'handover_settlements',COUNT(*) FROM jahez.JahezRiderSettlement d JOIN jahez.JahezAccountHandover h ON h.Id=d.HandoverId JOIN app.PlatformRiderAccounts a ON a.Id=h.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')
UNION ALL SELECT 'assignment_events',COUNT(*) FROM app.RiderAssignmentEvents d JOIN app.RiderClientAssignments ra ON ra.Id=d.RiderClientAssignmentId JOIN app.PlatformRiderAccounts a ON a.Id=ra.PlatformRiderAccountId WHERE a.ClientPlatformId=(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ');
'@
  $q.CommandText=$sql; $q.CommandTimeout=120
  $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($q); $ds=[System.Data.DataSet]::new(); [void]$adapter.Fill($ds)
  for($i=0;$i -lt $ds.Tables.Count;$i++){
    if($i -eq 10){$ds.Tables[$i] | Export-Csv 'tmp/platform_upload/missing_owners.csv' -NoTypeInformation -Encoding UTF8; Write-Output ("RESULT 10 saved {0} missing account-owner rows" -f $ds.Tables[$i].Rows.Count); continue}
    Write-Output ("RESULT {0}" -f $i); $ds.Tables[$i] | Format-Table -AutoSize | Out-String | Write-Output
  }
} finally {$conn.Dispose()}
