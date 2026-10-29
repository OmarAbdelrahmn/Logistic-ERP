param([string]$SnapshotFileName='database-snapshot.json')
$ErrorActionPreference = 'Stop'
if([IO.Path]::GetFileName($SnapshotFileName) -ne $SnapshotFileName -or !$SnapshotFileName.EndsWith('.json')){throw 'Use a JSON filename without directories.'}
$syncRoot = Split-Path -Parent $PSScriptRoot
$syncOutput = Join-Path $syncRoot 'outputs/ninja-workbook-sync-20261025'
$syncConfig = Get-Content -LiteralPath (Join-Path $syncRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$syncText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $syncConfig.ConnectionStrings.LogisticsDatabase }
$syncConnection = [System.Data.SqlClient.SqlConnection]::new($syncText)
try {
    if ($syncConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $syncConnection.Database -ne 'db67927') { throw 'Unexpected database target.' }
    $syncConnection.Open()
    $syncCommand = $syncConnection.CreateCommand()
    $syncCommand.CommandTimeout = 90
    $syncCommand.CommandText = @'
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRANSACTION;
SELECT a.*, e.IqamaNo OwnerIqama, e.FullNameAr OwnerNameAr,e.FullNameEn OwnerNameEn,e.IsDeleted OwnerDeleted,
g.Code CityCode,g.NameEn CityNameEn,g.NameAr CityNameAr
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId
LEFT JOIN app.OperatingCities c ON c.Id=a.OperatingCityId LEFT JOIN platform.GlobalCities g ON g.Id=c.GlobalCityId
WHERE p.Code=N'NINJA';
SELECT x.*,a.ExternalAccountId,e.IqamaNo ActualIqama,e.FullNameAr ActualNameAr,e.FullNameEn ActualNameEn,e.IsDeleted RiderDeleted
FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
LEFT JOIN app.RiderProfiles r ON r.Id=x.RiderProfileId LEFT JOIN app.Employees e ON e.Id=r.EmployeeId WHERE p.Code=N'NINJA';
SELECT * FROM app.Employees;
SELECT * FROM app.RiderProfiles;
SELECT * FROM ninja.NinjaAccountDetails;
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA IN (N'app',N'ninja',N'platform') ORDER BY TABLE_SCHEMA,TABLE_NAME,ORDINAL_POSITION;
SELECT * FROM platform.ClientPlatforms WHERE Code=N'NINJA';
SELECT * FROM app.ClientContracts WHERE ClientPlatformId IN (SELECT Id FROM platform.ClientPlatforms WHERE Code=N'NINJA');
SELECT * FROM app.PlatformAccountRegistrations WHERE ClientPlatformId IN (SELECT Id FROM platform.ClientPlatforms WHERE Code=N'NINJA');
SELECT * FROM ninja.NinjaDailyPerformance;
SELECT * FROM ninja.NinjaDailyDeductions;
SELECT SYSUTCDATETIME() SnapshotUtc,DB_NAME() DatabaseName;
SELECT x.*,p.Code PlatformCode,e.IqamaNo ActualIqama
FROM app.RiderClientAssignments x JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
JOIN app.RiderProfiles r ON r.Id=x.RiderProfileId JOIN app.Employees e ON e.Id=r.EmployeeId
WHERE x.IsDeleted=0 AND p.Code<>N'NINJA' AND x.RiderProfileId IN (
 SELECT n.RiderProfileId FROM app.RiderClientAssignments n JOIN app.PlatformRiderAccounts na ON na.Id=n.PlatformRiderAccountId
 JOIN platform.ClientPlatforms np ON np.Id=na.ClientPlatformId WHERE np.Code=N'NINJA');
SELECT u.* FROM app.PlatformAccountDayUsages u JOIN app.PlatformRiderAccounts a ON a.Id=u.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT a.ExternalAccountId,COUNT(v.Id) SavedVersionCount,MAX(v.RotatedAtUtc) LatestSavedAtUtc
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
LEFT JOIN app.PlatformAccountCredentialVersions v ON v.PlatformRiderAccountId=a.Id
WHERE p.Code=N'NINJA' GROUP BY a.ExternalAccountId;
SELECT OBJECT_SCHEMA_NAME(f.parent_object_id) ChildSchema,OBJECT_NAME(f.parent_object_id) ChildTable,pc.name ChildColumn,
OBJECT_NAME(f.referenced_object_id) ParentTable
FROM sys.foreign_key_columns k JOIN sys.foreign_keys f ON f.object_id=k.constraint_object_id
JOIN sys.columns pc ON pc.object_id=k.parent_object_id AND pc.column_id=k.parent_column_id
WHERE OBJECT_NAME(f.referenced_object_id) IN(N'RiderClientAssignments',N'Employees',N'RiderProfiles');
SELECT c.*,g.Code CityCode,g.NameAr CityAr,g.NameEn CityEn FROM app.OperatingCities c JOIN platform.GlobalCities g ON g.Id=c.GlobalCityId;
SELECT Id,UserName,IsDeleted FROM [identity].Users WHERE UserName=N'Omar';
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN(N'AuditEntries',N'EmployeeWorkHistories') ORDER BY TABLE_NAME,ORDINAL_POSITION;
SELECT OBJECT_SCHEMA_NAME(t.parent_id) SchemaName,OBJECT_NAME(t.parent_id) TableName,t.name,t.is_disabled
FROM sys.triggers t WHERE OBJECT_NAME(t.parent_id) IN(N'Employees',N'PlatformRiderAccounts',N'RiderClientAssignments',N'NinjaAccountDetails',N'PlatformAccountDayUsages',N'AuditEntries',N'RiderAssignmentEvents');
SELECT e.*,a.ExternalAccountId FROM app.RiderAssignmentEvents e JOIN app.RiderClientAssignments x ON x.Id=e.RiderClientAssignmentId
JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT * FROM app.DatasetVersions;
SELECT x.Id,x.RiderProfileId,x.PlatformRiderAccountId,x.EffectiveFrom,x.EffectiveTo,x.Status,e.IqamaNo,p.Code PlatformCode
FROM app.RiderClientAssignments x JOIN app.RiderProfiles r ON r.Id=x.RiderProfileId JOIN app.Employees e ON e.Id=r.EmployeeId
JOIN app.PlatformRiderAccounts a ON a.Id=x.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE x.IsDeleted=0 AND x.EffectiveTo IS NULL AND e.IqamaNo IN(N'2536722909',N'2514822994');
SELECT OBJECT_SCHEMA_NAME(i.object_id) SchemaName,OBJECT_NAME(i.object_id) TableName,i.name,i.is_unique,i.filter_definition,c.name ColumnName,ic.key_ordinal
FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
WHERE i.is_unique=1 AND OBJECT_NAME(i.object_id) IN(N'PlatformAccountDayUsages',N'Employees',N'RiderClientAssignments')
ORDER BY TableName,i.name,ic.key_ordinal;
COMMIT;
'@
    $syncData = [System.Data.DataSet]::new()
    $syncAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($syncCommand)
    [void]$syncAdapter.Fill($syncData)
    $syncNames = @('accounts','assignments','employees','riders','details','schema','platform','contracts','registrations','performance','deductions','snapshot','otherPlatformAssignments','dayUsages','credentialMetadata','foreignKeys','cities','actors','auditSchema','triggers','events','versions','newRiderOtherAssignments','uniqueIndexes')
    $syncResult = [ordered]@{}
    for ($i=0; $i -lt $syncNames.Count; $i++) {
        $syncResult[$syncNames[$i]] = @(foreach ($row in $syncData.Tables[$i].Rows) {
            $entry = [ordered]@{}
            foreach ($column in $syncData.Tables[$i].Columns) {
                $value = $row[$column.ColumnName]
                $entry[$column.ColumnName] = if ($value -is [DBNull]) { $null } elseif ($value -is [byte[]]) { [Convert]::ToBase64String($value) } elseif ($value -is [DateTime]) { $value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff') } else { $value }
            }
            [pscustomobject]$entry
        })
    }
    $syncResult | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $syncOutput $SnapshotFileName) -Encoding utf8
    $syncCounts = [ordered]@{}
    foreach ($name in $syncNames) { $syncCounts[$name] = $syncResult[$name].Count }
    [pscustomobject]@{Mode='ReadOnly';Counts=$syncCounts;Snapshot=$syncResult.snapshot} | ConvertTo-Json -Depth 5
} finally { $syncConnection.Dispose() }
