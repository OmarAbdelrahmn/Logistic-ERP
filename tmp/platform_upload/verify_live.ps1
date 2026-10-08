$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
  $conn.Open();if($conn.Database -ne 'db67927'){throw 'Unexpected database'}
  $cmd=$conn.CreateCommand();$cmd.CommandText='CREATE TABLE #Accounts(Platform nvarchar(20),ExternalId nvarchar(150),OwnerIqama nvarchar(20),City nvarchar(100),Sponsor nvarchar(200),SourceStatus nvarchar(100));CREATE TABLE #OldIds(Id uniqueidentifier);';[void]$cmd.ExecuteNonQuery()
  $data=[System.Data.DataTable]::new();foreach($name in @('Platform','ExternalId','OwnerIqama','City','Sponsor','SourceStatus')){[void]$data.Columns.Add($name,[string])}
  foreach($r in (Import-Csv 'tmp/platform_upload/accounts.csv' -Encoding UTF8)){[void]$data.Rows.Add($r.platform,$r.external_id,$r.owner_iqama,$r.city,$r.sponsor,$r.status)}
  $ids=[System.Data.DataTable]::new();[void]$ids.Columns.Add('Id',[guid]);foreach($r in (Get-Content 'outputs/platform-account-live-20261007-backup/app.PlatformRiderAccounts.json' -Raw|ConvertFrom-Json)){[void]$ids.Rows.Add([guid]$r.Id)}
  foreach($item in @(@('#Accounts',$data),@('#OldIds',$ids))){$bulk=[System.Data.SqlClient.SqlBulkCopy]::new($conn);try{$bulk.DestinationTableName=$item[0];$bulk.WriteToServer($item[1])}finally{$bulk.Dispose()}}
  $sql=@'
WITH M AS (
  SELECT x.*,a.Id AS AccountId,a.Status AS ErpStatus,a.StatusReason,e.IqamaNo AS LiveIqama,gc.NameAr AS LiveCity,s.RegistryNameAr AS LiveSponsor,a.RegisteredEmployeeId,a.SponsorId
  FROM #Accounts x LEFT JOIN platform.ClientPlatforms p ON p.Code=CASE x.Platform WHEN 'CHEFSZ' THEN 'SHIFTZ' ELSE x.Platform END
  LEFT JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=p.Id AND a.ExternalAccountId=x.ExternalId
  LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
  LEFT JOIN app.OperatingCities oc ON oc.Id=a.OperatingCityId LEFT JOIN platform.GlobalCities gc ON gc.Id=oc.GlobalCityId
  LEFT JOIN app.Sponsors s ON s.Id=a.SponsorId
)
SELECT 'staged' AS Metric,COUNT(*) AS N FROM #Accounts
UNION ALL SELECT 'live_target',COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ')
UNION ALL SELECT 'missing_accounts',COUNT(*) FROM M WHERE AccountId IS NULL
UNION ALL SELECT 'owner_mismatch',COUNT(*) FROM M WHERE LiveIqama<>OwnerIqama OR RegisteredEmployeeId IS NULL
UNION ALL SELECT 'city_mismatch',COUNT(*) FROM M WHERE LiveCity<>City
UNION ALL SELECT 'sponsor_mismatch',COUNT(*) FROM M WHERE (Sponsor=N'خارج الكفالة' AND SponsorId IS NOT NULL) OR (Sponsor<>N'خارج الكفالة' AND SponsorId IS NULL)
  OR (Sponsor=N'شركة اكسبرس جايت' AND SponsorId<>CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000042'))
  OR (Sponsor=N'شركة البوابا الموكبلا' AND SponsorId<>CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000041'))
  OR (Sponsor=N'مؤسسة البوابة التجارية' AND SponsorId<>CONVERT(uniqueidentifier,'019c18d5-62e1-7000-8000-000000000040'))
UNION ALL SELECT 'status_source_mismatch',COUNT(*) FROM M WHERE StatusReason<>SourceStatus
UNION ALL SELECT 'status_enum_mismatch',COUNT(*) FROM M WHERE ErpStatus<>CASE WHEN SourceStatus IN (N'نشط (Active)',N'يعمل (نشط)') THEN 1 WHEN SourceStatus IN (N'موقوف (مشاكل نظامية)',N'محظور من التطبيق',N'معلق من المشرف') THEN 3 WHEN SourceStatus=N'غير نشط (Inactive)' THEN 4 ELSE 5 END
UNION ALL SELECT 'target_assignments',COUNT(*) FROM app.RiderClientAssignments ra JOIN app.PlatformRiderAccounts a ON a.Id=ra.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ')
UNION ALL SELECT 'old_account_ids_remaining',COUNT(*) FROM app.PlatformRiderAccounts a JOIN #OldIds o ON o.Id=a.Id
UNION ALL SELECT 'old_handovers_remaining',COUNT(*) FROM jahez.JahezAccountHandover h JOIN #OldIds o ON o.Id=h.PlatformRiderAccountId
UNION ALL SELECT 'owner_stubs_created',COUNT(*) FROM app.Employees WHERE Notes LIKE N'Created as a registered platform-account owner from the 2026-10-07 directory.%'
UNION ALL SELECT 'keeta_accounts',COUNT(*) FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='KEETA'
UNION ALL SELECT 'outside_sponsorship',COUNT(*) FROM M WHERE SponsorId IS NULL
UNION ALL SELECT 'migration_history',COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261007120737_AllowPlatformAccountOutsideSponsorship';
SELECT p.Code,a.Status,COUNT(*) AS N FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code IN ('JAHEZ','HUNGER','SHIFTZ') GROUP BY p.Code,a.Status ORDER BY p.Code,a.Status;
'@
  $cmd.CommandText=$sql;$cmd.CommandTimeout=120;$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);$ds=[System.Data.DataSet]::new();[void]$adapter.Fill($ds)
  foreach($table in $ds.Tables){$table|Format-Table -AutoSize|Out-String|Write-Output}
}finally{$conn.Dispose()}
