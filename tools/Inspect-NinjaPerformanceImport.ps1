param([string]$OutputDirectory = 'outputs/ninja-performance-20261026')
$ErrorActionPreference = 'Stop'
$performanceRoot = Split-Path -Parent $PSScriptRoot
$performanceOutput = Join-Path $performanceRoot $OutputDirectory
[void](New-Item -ItemType Directory -Force -Path $performanceOutput)
$performanceSettings = Get-Content -LiteralPath (Join-Path $performanceRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$performanceConnection = [Data.SqlClient.SqlConnection]::new($performanceSettings.ConnectionStrings.LogisticsDatabase)
try {
    if ($performanceConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $performanceConnection.Database -ne 'db67927') { throw 'Unexpected hosted database.' }
    $performanceConnection.Open()
    $performanceCommand = $performanceConnection.CreateCommand()
    $performanceCommand.CommandTimeout = 120
    $performanceCommand.CommandText = @'
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRANSACTION;
SELECT a.Id,a.ClientPlatformId,a.ExternalAccountId,a.SponsorId,a.OperatingCityId,a.IsDeleted,a.StartDate,a.RowVersion
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT r.Id,r.PlatformRiderAccountId,r.RiderProfileId,r.EffectiveFrom,r.EffectiveTo,r.Status,r.IsDeleted,r.NinjaRiderType,r.RowVersion
FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT u.* FROM app.PlatformAccountDayUsages u JOIN app.PlatformRiderAccounts a ON a.Id=u.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT s.* FROM ninja.NinjaDailyPerformanceSources s JOIN app.PlatformRiderAccounts a ON a.Id=s.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT f.* FROM ninja.NinjaDailyPerformance f JOIN app.PlatformRiderAccounts a ON a.Id=f.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT * FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT * FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId;
SELECT Id,UserName,AuthorizationVersion,IsDeleted FROM [identity].Users WHERE UserName=N'Omar';
SELECT s.name AS SchemaName,t.name AS TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
WHERE s.name=N'ninja';
SELECT Id,Code FROM platform.ClientPlatforms WHERE Code=N'NINJA';
COMMIT;
'@
    $performanceData = [Data.DataSet]::new()
    $performanceAdapter = [Data.SqlClient.SqlDataAdapter]::new($performanceCommand)
    [void]$performanceAdapter.Fill($performanceData)
    $performanceNames = @('accounts','assignments','dayUsages','sources','shares','applicationMigrations','identityMigrations','actors','schema','platforms')
    $performanceResult = [ordered]@{}
    for ($i = 0; $i -lt $performanceNames.Count; $i++) {
        $performanceResult[$performanceNames[$i]] = @(foreach ($row in $performanceData.Tables[$i].Rows) {
            $item = [ordered]@{}
            foreach ($column in $performanceData.Tables[$i].Columns) {
                $value = $row[$column.ColumnName]
                $item[$column.ColumnName] = if ($value -is [DBNull]) { $null } elseif ($value -is [byte[]]) { [Convert]::ToBase64String($value) }
                    elseif ($value -is [DateTimeOffset]) { $value.ToString('o') } elseif ($value -is [DateTime]) { $value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff') } else { $value }
            }
            [pscustomobject]$item
        })
    }
    $performanceResult | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $performanceOutput 'hosted-preflight.json') -Encoding utf8
    $performanceCounts = [ordered]@{}
    foreach ($name in $performanceNames) { $performanceCounts[$name] = $performanceResult[$name].Count }
    [pscustomobject]@{ Mode = 'ReadOnly'; Database = $performanceConnection.Database; Counts = $performanceCounts } | ConvertTo-Json -Depth 4
} finally { $performanceConnection.Dispose() }
