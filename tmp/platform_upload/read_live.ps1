$ErrorActionPreference = 'Stop'
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$conn = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
    $conn.Open()
    Write-Output ("TARGET {0} / {1}" -f $conn.DataSource, $conn.Database)
    if ($conn.Database -ne 'db67927') { throw 'Unexpected database; refusing further work.' }
    $sql = @'
SELECT 'platforms' AS Section, CONVERT(nvarchar(36),Id) AS Id, Code AS Name, NULL AS Extra
FROM platform.ClientPlatforms
UNION ALL
SELECT 'cities', CONVERT(nvarchar(36),oc.Id), gc.Code + N' / ' + gc.NameAr, CONVERT(nvarchar(20),oc.Status)
FROM app.OperatingCities oc JOIN platform.GlobalCities gc ON gc.Id=oc.GlobalCityId
UNION ALL
SELECT 'sponsors', CONVERT(nvarchar(36),Id), RegistryNameAr, CONVERT(nvarchar(20),Status)
FROM app.Sponsors
ORDER BY Section, Name;
SELECT p.Code, a.Status, COUNT(*) AS Accounts
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
GROUP BY p.Code,a.Status ORDER BY p.Code,a.Status;
SELECT OBJECT_SCHEMA_NAME(fk.parent_object_id) AS SchemaName,OBJECT_NAME(fk.parent_object_id) AS TableName,c.name AS ColumnName,fk.delete_referential_action_desc AS OnDelete
FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id JOIN sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
WHERE fk.referenced_object_id=OBJECT_ID('app.PlatformRiderAccounts') ORDER BY SchemaName,TableName;
SELECT OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS RefSchema,OBJECT_NAME(fk.referenced_object_id) AS RefTable,
OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,OBJECT_NAME(fk.parent_object_id) AS ChildTable,c.name AS ChildColumn
FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
JOIN sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
WHERE fk.referenced_object_id IN (OBJECT_ID('app.RiderClientAssignments'),OBJECT_ID('jahez.JahezAccountHandover'));
'@
    $cmd = $conn.CreateCommand(); $cmd.CommandText=$sql; $cmd.CommandTimeout=90
    $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd)
    $ds=[System.Data.DataSet]::new(); [void]$adapter.Fill($ds)
    for($i=0;$i -lt $ds.Tables.Count;$i++) {
      Write-Output ("RESULT {0}" -f $i)
      $ds.Tables[$i] | Format-Table -AutoSize | Out-String | Write-Output
    }
} finally { $conn.Dispose() }
