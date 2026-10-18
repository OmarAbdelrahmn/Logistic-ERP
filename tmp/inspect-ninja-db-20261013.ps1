$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$cfg=Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open()
 $q=$conn.CreateCommand();$q.CommandTimeout=90;$q.CommandText=@'
SELECT Id,Code,NameAr,Status,IsDeleted FROM platform.ClientPlatforms WHERE Code='NINJA';
SELECT a.Id,a.Code,a.ExternalAccountId,a.RegisteredEmployeeId,a.OperatingCityId,a.SponsorId,a.Status,a.IsDeleted,a.OperationalNotes,a.OwnershipNotes,CONVERT(varchar(max),a.RowVersion,2) RowVersion FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='NINJA';
SELECT e.Id,e.IqamaNo,e.FullNameAr,e.Status,e.EngagementType,e.IsEmployee,e.OperatingCityId,e.OperationalWorkTypeId,e.SponsorId,e.IsDeleted,r.Id RiderId,r.IsDeleted RiderDeleted FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IqamaNo IN(SELECT OwnerIqama FROM OPENJSON(@j,'$.rows') WITH(OwnerIqama nvarchar(10) '$.ownerIqama') UNION SELECT UserIqama FROM OPENJSON(@j,'$.rows') WITH(UserIqama nvarchar(10) '$.userIqama'));
SELECT r.Id,r.RiderProfileId,e.IqamaNo,p.Code PlatformCode,a.ExternalAccountId,r.PlatformRiderAccountId,r.EffectiveFrom,r.EffectiveTo,r.Status,r.ClientContractId,r.PaymentModel,r.RiderAccountSlot,CONVERT(varchar(max),r.RowVersion,2) RowVersion FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId JOIN app.RiderProfiles rp ON rp.Id=r.RiderProfileId JOIN app.Employees e ON e.Id=rp.EmployeeId WHERE r.IsDeleted=0 AND (r.EffectiveTo IS NULL OR p.Code='NINJA') AND (p.Code='NINJA' OR e.IqamaNo IN(SELECT UserIqama FROM OPENJSON(@j,'$.rows') WITH(UserIqama nvarchar(10) '$.userIqama')));
SELECT Id,EmployerIdentityNumber,RegistryNameAr,RegistryNameEn,Status,IsDeleted FROM app.Sponsors;
SELECT c.Id,c.GlobalCityId,c.Status,c.IsDeleted,g.Code,g.NameAr,g.NameEn FROM app.OperatingCities c JOIN platform.GlobalCities g ON g.Id=c.GlobalCityId;
SELECT Id,Code,NameAr,NameEn,Status,IsDeleted FROM app.OperationalWorkTypes;
SELECT c.* FROM app.ClientContracts c JOIN platform.ClientPlatforms p ON p.Id=c.ClientPlatformId WHERE p.Code='NINJA';
SELECT Id,UserName,IsDeleted FROM [identity].Users WHERE UserName='Omar';
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN('PlatformRiderAccounts','RiderClientAssignments','RiderAssignmentEvents','PlatformAccountRegistrations','ClientContracts','NinjaAccountDetails','AuditEntries','DatasetVersions') ORDER BY TABLE_NAME,ORDINAL_POSITION;
SELECT (SELECT COUNT(*) FROM ninja.NinjaDailyPerformance) PerformanceCount,(SELECT COUNT(*) FROM ninja.NinjaDailyDeductions) DeductionCount;
'@
 [void]$q.Parameters.Add('@j',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@j'].Value=Get-Content (Join-Path $root 'tmp/ninja-source-20261013.json') -Raw
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $names=@('platforms','accounts','employees','assignments','sponsors','cities','workTypes','contracts','actors','schema','reportCounts')
 $result=[ordered]@{}
 for($i=0;$i -lt $names.Count;$i++){
   $items=@(foreach($r in $d.Tables[$i].Rows){$o=[ordered]@{};foreach($c in $d.Tables[$i].Columns){$v=$r[$c.ColumnName];$o[$c.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [byte[]]){[Convert]::ToBase64String($v)}elseif($v -is [DateTime]){$v.ToString('yyyy-MM-ddTHH:mm:ss')}else{$v}};[pscustomobject]$o})
   $result[$names[$i]]=$items
 }
 $result | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $root 'tmp/ninja-db-20261013.json') -Encoding utf8
 $names | ForEach-Object { [pscustomobject]@{Data=$_;Count=$result[$_].Count} } | Format-Table
} finally {$conn.Dispose()}
