param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$migrationId = '20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions'
$scriptPath = Join-Path $workspace 'database/scripts/ninja-application.sql'
$verificationPath = Join-Path $workspace 'database/scripts/verify-ninja-migration.sql'
$config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$configuredConnection = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [System.Data.SqlClient.SqlConnection]::new($configuredConnection)
$transaction = $null

function Read-Data([string]$Sql) {
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 90
    $command.Transaction = $transaction
    $command.CommandText = $Sql
    try {
        $data = [System.Data.DataSet]::new()
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        try { [void]$adapter.Fill($data) } finally { $adapter.Dispose() }
        return ,$data
    } finally { $command.Dispose() }
}

try {
    $connection.Open()
    if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected hosted database; refusing Ninja migration.'
    }
    $preflight = Read-Data @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261012093719_AddChefzOrderSettlementImports')
    THROW 51000, 'Required previous application migration is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE schema_id=SCHEMA_ID('ninja') AND name IN ('NinjaAccountDetails','NinjaDailyImports','NinjaDailyPerformance','NinjaDailyDeductions'))
    THROW 51000, 'Partial Ninja schema detected; reconcile before applying.', 1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory
    WHERE MigrationId IN ('20261012093719_AddChefzOrderSettlementImports','20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions') ORDER BY MigrationId;
SELECT COUNT_BIG(*) NinjaAccounts FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='NINJA';
SELECT name TableName FROM sys.tables WHERE schema_id=SCHEMA_ID('ninja') ORDER BY name;
'@
    Write-Output ('Verified hosted target: ' + $connection.DataSource + ' / ' + $connection.Database)
    $preflight.Tables[0] | Format-Table -AutoSize
    $preflight.Tables[1] | Format-Table -AutoSize
    if (!$Apply) { Write-Output 'Preflight only. -Apply executes the authorized Ninja migration.'; return }

    $script = Get-Content -LiteralPath $scriptPath -Raw
    if ($script -notmatch [regex]::Escape($migrationId) -or $script -notmatch 'COMMIT;\s*(GO\s*)?$' -or
        ([regex]::Matches($script,'(?i)CREATE TABLE \[ninja\]')).Count -ne 4 -or
        $script -match '(?im)^\s*(DROP|DELETE|UPDATE|ALTER)\b') {
        throw 'Unexpected Ninja migration script.'
    }
    $body = [regex]::Replace($script, '(?im)^\s*GO\s*\r?$', '')
    $body = $body -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', ''
    if ($body -match '(?im)^\s*(BEGIN TRANSACTION|COMMIT)\s*;') { throw 'Unexpected nested migration transaction.' }
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $outputDirectory = Join-Path $workspace ('outputs/ninja-migration-' + $stamp)
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-Data 'SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;')
    $before = Read-Data @'
SELECT a.* INTO #NinjaAccountsBefore FROM app.PlatformRiderAccounts a WITH (UPDLOCK,HOLDLOCK)
    JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='NINJA';
SELECT r.* INTO #NinjaAssignmentsBefore FROM app.RiderClientAssignments r WITH (UPDLOCK,HOLDLOCK)
    JOIN #NinjaAccountsBefore a ON r.PlatformRiderAccountId=a.Id;
SELECT v.* INTO #NinjaCredentialsBefore FROM app.PlatformAccountCredentialVersions v WITH (UPDLOCK,HOLDLOCK)
    JOIN #NinjaAccountsBefore a ON v.PlatformRiderAccountId=a.Id;
CREATE TABLE #NinjaReportCountsBefore(TableName nvarchar(100),RecordCount bigint);
IF OBJECT_ID('ninja.NinjaAccountDetails','U') IS NOT NULL EXEC(N'INSERT INTO #NinjaReportCountsBefore SELECT ''NinjaAccountDetails'',COUNT_BIG(*) FROM ninja.NinjaAccountDetails WITH(UPDLOCK,HOLDLOCK)');
ELSE INSERT INTO #NinjaReportCountsBefore VALUES('NinjaAccountDetails',0);
IF OBJECT_ID('ninja.NinjaDailyImports','U') IS NOT NULL EXEC(N'INSERT INTO #NinjaReportCountsBefore SELECT ''NinjaDailyImports'',COUNT_BIG(*) FROM ninja.NinjaDailyImports WITH(UPDLOCK,HOLDLOCK)');
ELSE INSERT INTO #NinjaReportCountsBefore VALUES('NinjaDailyImports',0);
IF OBJECT_ID('ninja.NinjaDailyPerformance','U') IS NOT NULL EXEC(N'INSERT INTO #NinjaReportCountsBefore SELECT ''NinjaDailyPerformance'',COUNT_BIG(*) FROM ninja.NinjaDailyPerformance WITH(UPDLOCK,HOLDLOCK)');
ELSE INSERT INTO #NinjaReportCountsBefore VALUES('NinjaDailyPerformance',0);
IF OBJECT_ID('ninja.NinjaDailyDeductions','U') IS NOT NULL EXEC(N'INSERT INTO #NinjaReportCountsBefore SELECT ''NinjaDailyDeductions'',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions WITH(UPDLOCK,HOLDLOCK)');
ELSE INSERT INTO #NinjaReportCountsBefore VALUES('NinjaDailyDeductions',0);
SELECT * FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT * FROM #NinjaAccountsBefore ORDER BY Id;
SELECT * FROM #NinjaAssignmentsBefore ORDER BY Id;
SELECT TableName,RecordCount FROM #NinjaReportCountsBefore ORDER BY TableName;
SELECT COUNT_BIG(*) CredentialVersionsBefore FROM #NinjaCredentialsBefore;
'@
    $before.Tables[0].TableName = 'ApplicationMigrationsBefore'
    $before.Tables[1].TableName = 'NinjaAccountsBefore'
    $before.Tables[2].TableName = 'NinjaAssignmentsBefore'
    $before.Tables[3].TableName = 'NinjaReportCountsBefore'
    $before.Tables[4].TableName = 'CredentialVersionCountBefore'
    $before.WriteXml((Join-Path $outputDirectory 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $scriptPath -Destination (Join-Path $outputDirectory 'applied-script.sql')
    [void](Read-Data $body)
    [void](Read-Data @'
IF EXISTS(SELECT * FROM #NinjaAccountsBefore EXCEPT SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='NINJA')
   OR EXISTS(SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='NINJA' EXCEPT SELECT * FROM #NinjaAccountsBefore)
    THROW 51000, 'Existing Ninja account values changed.', 1;
IF EXISTS(SELECT * FROM #NinjaAssignmentsBefore EXCEPT SELECT r.* FROM app.RiderClientAssignments r JOIN #NinjaAccountsBefore a ON r.PlatformRiderAccountId=a.Id)
   OR EXISTS(SELECT r.* FROM app.RiderClientAssignments r JOIN #NinjaAccountsBefore a ON r.PlatformRiderAccountId=a.Id EXCEPT SELECT * FROM #NinjaAssignmentsBefore)
    THROW 51000, 'Existing Ninja assignments changed.', 1;
IF EXISTS(SELECT * FROM #NinjaCredentialsBefore EXCEPT SELECT v.* FROM app.PlatformAccountCredentialVersions v JOIN #NinjaAccountsBefore a ON v.PlatformRiderAccountId=a.Id)
   OR EXISTS(SELECT v.* FROM app.PlatformAccountCredentialVersions v JOIN #NinjaAccountsBefore a ON v.PlatformRiderAccountId=a.Id EXCEPT SELECT * FROM #NinjaCredentialsBefore)
    THROW 51000, 'Existing Ninja credentials changed.', 1;
IF EXISTS(SELECT * FROM #NinjaReportCountsBefore EXCEPT SELECT * FROM (
    SELECT 'NinjaAccountDetails' TableName,COUNT_BIG(*) RecordCount FROM ninja.NinjaAccountDetails UNION ALL
    SELECT 'NinjaDailyImports',COUNT_BIG(*) FROM ninja.NinjaDailyImports UNION ALL
    SELECT 'NinjaDailyPerformance',COUNT_BIG(*) FROM ninja.NinjaDailyPerformance UNION ALL
    SELECT 'NinjaDailyDeductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions) counts)
    THROW 51000, 'Unexpected Ninja report rows were inserted.', 1;
'@)
    $verificationSql = Get-Content -LiteralPath $verificationPath -Raw
    $verification = Read-Data $verificationSql
    $transaction.Commit(); $transaction.Dispose(); $transaction = $null
    $verification.WriteXml((Join-Path $outputDirectory 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{
        Server = $connection.DataSource; Database = $connection.Database; Migration = $migrationId
        ScriptSha256 = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
        VerifiedAtUtc = [DateTime]::SpecifyKind($verification.Tables[6].Rows[0].VerifiedAtUtc,[DateTimeKind]::Utc).ToString('o')
        Tables = 4; TrustedForeignKeys = $verification.Tables[4].Rows.Count; TrustedCheckConstraints = $verification.Tables[2].Rows.Count
        UniqueIndexes = @($verification.Tables[3].Rows | Where-Object { $_.is_unique -and !($_.name -like 'PK_*') }).Count
        ExistingAccountsPreserved = $true; ExistingAssignmentsPreserved = $true; ExistingCredentialsPreserved = $true
        ReportRowsInserted = 0; NinjaAccounts = $before.Tables[1].Rows.Count
    }
    $report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDirectory 'verification.json') -Encoding UTF8
    # Verify committed state using a fresh, independent connection.
    $connection.Close(); $connection.Open()
    $postCommit = Read-Data $verificationSql
    $postCommit.WriteXml((Join-Path $outputDirectory 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $report | ConvertTo-Json
    Write-Output ('Deployment evidence: ' + $outputDirectory)
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch { } }
    throw
} finally {
    if ($transaction) { $transaction.Dispose() }
    $connection.Dispose()
}
