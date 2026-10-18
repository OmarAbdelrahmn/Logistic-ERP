param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
$plan=Get-Content (Join-Path $OutputDirectory 'plan.json') -Raw|ConvertFrom-Json
$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw|ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandTimeout=60
 $q.CommandText=@'
SELECT 'Employee dashboard owners without a rider profile' CheckName,COUNT(*) Mismatches
FROM OPENJSON(@PlanJson,'$.accounts') WITH(OwnerId uniqueidentifier '$.ownerId') s JOIN app.Employees e ON e.Id=s.OwnerId
WHERE e.IsEmployee=1 AND NOT EXISTS(SELECT 1 FROM app.RiderProfiles r WHERE r.EmployeeId=e.Id AND r.IsDeleted=0)
UNION ALL
SELECT 'Nine user-corrected dates',COUNT(*) FROM OPENJSON(@PlanJson,'$.dateCorrections') d
LEFT JOIN app.PlatformRiderAccounts a ON a.ExternalAccountId=d.[key] COLLATE DATABASE_DEFAULT AND a.ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId')
LEFT JOIN jahez_v2.AccountSessions h ON h.PlatformRiderAccountId=a.Id AND h.EndedAtUtc IS NULL AND h.IsDeleted=0
WHERE h.Id IS NULL OR CONVERT(date,SWITCHOFFSET(h.StartedAtUtc,'+03:00'))<>CONVERT(date,d.value)
 OR CONVERT(date,SWITCHOFFSET(h.LastSettlementPaymentAtUtc,'+03:00'))<>CONVERT(date,d.value) OR h.LastSettlementPaymentAtUtc IS NULL;
SELECT COUNT(*) TotalJahezAccounts,SUM(CASE WHEN Status=1 THEN 1 ELSE 0 END) Available,
 SUM(CASE WHEN Status=2 THEN 1 ELSE 0 END) Assigned,SUM(CASE WHEN Status=3 THEN 1 ELSE 0 END) Suspended,
 SUM(CASE WHEN Status=4 THEN 1 ELSE 0 END) Retired
FROM app.PlatformRiderAccounts WHERE ClientPlatformId=JSON_VALUE(@PlanJson,'$.platformId') AND IsDeleted=0;
'@
 [void]$q.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@PlanJson'].Value=$plan|ConvertTo-Json -Depth 30 -Compress
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $d.Tables[0].TableName='Checks';$d.Tables[1].TableName='Totals';$d.WriteXml((Join-Path $OutputDirectory 'final-owner-and-date-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if(@($d.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){throw 'Final owner/date verification failed.'}
 $d.Tables[0]|Format-Table -AutoSize;$d.Tables[1]|Format-Table -AutoSize
}finally{$c.Dispose()}
