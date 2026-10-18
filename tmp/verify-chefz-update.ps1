$ErrorActionPreference='Stop'
$config=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
DECLARE @p uniqueidentifier=(SELECT Id FROM platform.ClientPlatforms WHERE Code='SHIFTZ');
SELECT COUNT(*) Mismatches FROM OPENJSON(@j,'$.accounts') WITH(IdNumber nvarchar(100) '$.id',AccountType int '$.accountType',SponsorNumber nvarchar(100) '$.sponsor',VehicleType nvarchar(20) '$.vehicleType',DashboardStatus nvarchar(100) '$.status') s
LEFT JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=@p AND a.ExternalAccountId=s.IdNumber AND a.IsDeleted=0
LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0
LEFT JOIN app.Sponsors p ON p.Id=a.SponsorId
WHERE a.Id IS NULL OR d.Id IS NULL OR d.AccountType<>s.AccountType OR d.ReportIdNumber<>s.IdNumber OR d.VehicleType<>s.VehicleType OR d.DashboardStatus<>s.DashboardStatus OR p.EmployerIdentityNumber<>s.SponsorNumber OR a.OperatingCityId<>'019c18d5-62e1-7000-8000-000000000003' OR a.Status<>CASE WHEN s.DashboardStatus='active' THEN 1 ELSE 3 END;
SELECT a.ExternalAccountId,a.RegisteredEmployeeId,CASE WHEN d.Id IS NULL THEN 'Unconfigured' ELSE 'Configured' END Configuration FROM app.PlatformRiderAccounts a LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id WHERE a.ClientPlatformId=@p AND a.ExternalAccountId IN('2506752275','2507652275');
SELECT p.EmployerIdentityNumber,s.SponsorshipCount,s.Working,s.NotWorking,s.Blocked,s.PermanentlyBlocked,s.UpdateBlocked,s.Uploaded,s.PrivateLicense,s.PendingUpload,s.NoLicense,s.ManagementSuspended,s.Motorcycle,s.WorkingOutsideSponsorship,s.WorkingWithoutPerformance FROM chefz.SponsorStatistics s JOIN app.Sponsors p ON p.Id=s.SponsorId WHERE s.IsDeleted=0 ORDER BY p.EmployerIdentityNumber;
SELECT COUNT(*) RemainingAssignments FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId WHERE a.ClientPlatformId=@p;
SELECT COUNT(*) DailyPerformanceRecords FROM chefz.DailyPerformance;
SELECT COUNT(*) AuditEvents FROM audit.AuditEntries WHERE Action='ChefzWorkbookReset' AND CorrelationId='chefz-workbook-20261011';
'@
 [void]$q.Parameters.Add('@j',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@j'].Value=Get-Content tmp/chefz-update-source.json -Raw
 $d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d);foreach($t in $d.Tables){$t | Format-Table -AutoSize}
 if($d.Tables[0].Rows[0].Mismatches -ne 0){throw 'Workbook verification failed.'}
}finally{$c.Dispose()}
