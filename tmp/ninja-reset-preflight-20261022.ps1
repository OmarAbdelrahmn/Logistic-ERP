$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root 'outputs/ninja-reset-20261022'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$cfg=Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open();$q=$conn.CreateCommand();$q.CommandTimeout=90;$q.CommandText=@'
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='ninja' OR TABLE_NAME IN('PlatformRiderAccounts','Employees','RiderProfiles','PlatformAccountDayUsage','AuditEntries','DatasetVersions') ORDER BY TABLE_SCHEMA,TABLE_NAME,ORDINAL_POSITION;
SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) ChildSchema,OBJECT_NAME(f.parent_object_id) ChildTable,pc.name ChildColumn,OBJECT_SCHEMA_NAME(f.referenced_object_id) ParentSchema,OBJECT_NAME(f.referenced_object_id) ParentTable,rc.name ParentColumn,f.delete_referential_action_desc DeleteAction FROM sys.foreign_key_columns k JOIN sys.foreign_keys f ON f.object_id=k.constraint_object_id JOIN sys.columns pc ON pc.object_id=k.parent_object_id AND pc.column_id=k.parent_column_id JOIN sys.columns rc ON rc.object_id=k.referenced_object_id AND rc.column_id=k.referenced_column_id WHERE OBJECT_NAME(f.referenced_object_id) IN('RiderClientAssignments','RiderAssignmentEvents','PlatformAccountRegistrations','PlatformRiderAccounts','NinjaDailyImports','NinjaDailyPerformance','NinjaDailyDeductions','Employees','RiderProfiles') ORDER BY ParentTable,ChildSchema,ChildTable;
SELECT e.Id,e.IqamaNo,e.FullNameAr,e.FullNameEn,e.IsDeleted,e.IsEmployee,e.Status,e.OperatingCityId,e.SponsorId,e.OperationalWorkTypeId,r.Id RiderId,r.IsDeleted RiderDeleted,CONVERT(varchar(max),e.RowVersion,2) RowVersion FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IqamaNo IN(SELECT Iqama FROM OPENJSON(@j,'$.detail') WITH(Iqama nvarchar(20) '$[7]')) OR e.Id IN(SELECT RegisteredEmployeeId FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='NINJA');
SELECT s.Id,s.EmployerIdentityNumber,s.RegistryNameAr,s.IsDeleted,s.Status FROM app.Sponsors s;
SELECT c.Id,g.Code,g.NameAr,g.NameEn,c.Status,c.IsDeleted FROM app.OperatingCities c JOIN platform.GlobalCities g ON g.Id=c.GlobalCityId;
SELECT * FROM app.OperationalWorkTypes;
SELECT Id,UserName,IsDeleted FROM [identity].Users WHERE UserName='Omar';
SELECT OBJECT_SCHEMA_NAME(i.object_id) SchemaName,OBJECT_NAME(i.object_id) TableName,i.name,i.is_unique,i.filter_definition,c.name ColumnName,ic.key_ordinal FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE OBJECT_NAME(i.object_id) IN('Employees','PlatformRiderAccounts','RiderProfiles') AND i.is_unique=1 ORDER BY TableName,i.name,ic.key_ordinal;
DECLARE @p uniqueidentifier=(SELECT Id FROM platform.ClientPlatforms WHERE Code='NINJA');
SELECT * FROM app.RiderClientAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p) OR ClientContractId IN(SELECT Id FROM app.ClientContracts WHERE ClientPlatformId=@p);
SELECT * FROM app.PlatformAccountRegistrations WHERE ClientPlatformId=@p;
SELECT 'Performance' Kind,COUNT_BIG(*) Count FROM ninja.NinjaDailyPerformance UNION ALL SELECT 'Deductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions UNION ALL SELECT 'Imports',COUNT_BIG(*) FROM ninja.NinjaDailyImports;
SELECT * FROM platform.GlobalCities;
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN('GlobalCities','OperatingCities','VehiclePlatformAccountAssignments','VehiclePlatformAccountSwitches') ORDER BY TABLE_NAME,ORDINAL_POSITION;
SELECT * FROM app.VehiclePlatformAccountAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p);
SELECT * FROM app.VehiclePlatformAccountSwitches WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p);
SELECT * FROM app.PlatformAccountDayUsages WHERE PlatformRiderAccountId IN(SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId=@p);
'@
 [void]$q.Parameters.Add('@j',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@j'].Value=Get-Content (Join-Path $root 'outputs/ninja-comparison-20261022/comparison.json') -Raw
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $names=@('schema','foreignKeys','employees','sponsors','cities','workTypes','actors','indexes','assignments','registrations','dailyCounts','globalCities','extraSchema','vehicleAssignments','vehicleSwitches','dayUsages');$result=[ordered]@{}
 for($i=0;$i -lt $names.Count;$i++){$result[$names[$i]]=@(foreach($r in $d.Tables[$i].Rows){$o=[ordered]@{};foreach($c in $d.Tables[$i].Columns){$v=$r[$c.ColumnName];$o[$c.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [byte[]]){[Convert]::ToBase64String($v)}elseif($v -is [DateTime]){$v.ToString('o')}else{$v}};[pscustomobject]$o})}
 $result | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $out 'preflight.json') -Encoding utf8
 $names | ForEach-Object { [pscustomobject]@{Data=$_;Count=$result[$_].Count} } | Format-Table
} finally {$conn.Dispose()}
