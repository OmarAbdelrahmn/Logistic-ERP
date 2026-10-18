param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$taskWorkspace = Split-Path -Parent $PSScriptRoot
$taskMigrationId = '20261015134151_AddNinjaAccountSheetReviews'
$taskScriptPath = Join-Path $taskWorkspace 'database/scripts/ninja-account-sheet-reviews.sql'
$taskVerifyPath = Join-Path $taskWorkspace 'database/scripts/verify-ninja-account-sheet-reviews.sql'
$taskConfig = Get-Content -LiteralPath (Join-Path $taskWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $taskConfig.ConnectionStrings.LogisticsDatabase }
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConnectionText)
$taskTransaction = $null

function Read-TaskData([string]$Sql) {
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.CommandTimeout = 90
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandText = $Sql
    try {
        $taskData = [System.Data.DataSet]::new()
        $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
        try { [void]$taskAdapter.Fill($taskData) } finally { $taskAdapter.Dispose() }
        return ,$taskData
    } finally { $taskCommand.Dispose() }
}

$taskPreflightSql = @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions')
    THROW 51000, 'Required Ninja base migration is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261015080537_ResetJahezToAccountsOnlyV2')
    THROW 51000, 'Prepared script baseline migration is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261015134151_AddNinjaAccountSheetReviews')
    AND (OBJECT_ID(N'ninja.NinjaAccountUpdateUploads',N'U') IS NOT NULL OR OBJECT_ID(N'ninja.NinjaAccountUpdateRows',N'U') IS NOT NULL)
    THROW 51000, 'Partial account sheet-review schema detected.', 1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory
WHERE MigrationId IN (N'20261012130259_AddNinjaAccountsDailyPerformanceAndDeductions',N'20261015080537_ResetJahezToAccountsOnlyV2',N'20261015134151_AddNinjaAccountSheetReviews') ORDER BY MigrationId;
SELECT COUNT_BIG(*) AS NinjaAccounts FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT name AS TableName FROM sys.tables WHERE schema_id=SCHEMA_ID(N'ninja') ORDER BY name;
'@

try {
    $taskConnection.Open()
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected hosted target; refusing migration.'
    }
    $taskPreflight = Read-TaskData $taskPreflightSql
    Write-Output ('Verified hosted target: ' + $taskConnection.DataSource + ' / ' + $taskConnection.Database)
    $taskPreflight.Tables[0] | Format-Table -AutoSize
    $taskPreflight.Tables[1] | Format-Table -AutoSize
    $taskPreflight.Tables[2] | Format-Table -AutoSize
    if (!$Apply) { Write-Output 'Read-only preflight completed. -Apply runs the authorized migration.'; return }

    $taskScript = Get-Content -LiteralPath $taskScriptPath -Raw
    $taskSha = (Get-FileHash -LiteralPath $taskScriptPath -Algorithm SHA256).Hash
    if ($taskScript -notmatch [regex]::Escape($taskMigrationId) -or
        $taskScript -notmatch 'COMMIT;\s*(GO\s*)?$' -or
        ([regex]::Matches($taskScript, '(?i)CREATE TABLE \[ninja\]')).Count -ne 2 -or
        ([regex]::Matches($taskScript, '(?i)CREATE (UNIQUE )?INDEX')).Count -ne 5 -or
        $taskScript -match '(?im)^\s*(DROP|DELETE|UPDATE|ALTER|TRUNCATE|EXEC)\b' -or
        ([regex]::Matches($taskScript, '(?i)INSERT INTO')).Count -ne 1) {
        throw 'Migration script differs from the reviewed additive-only schema operation.'
    }
    $taskBody = [regex]::Replace($taskScript, '(?im)^\s*GO\s*\r?$', '')
    $taskBody = $taskBody -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', ''
    if ($taskBody -match '(?im)^\s*(BEGIN TRANSACTION|COMMIT)\s*;') { throw 'Unexpected nested transaction.' }
    $taskStamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $taskOutput = Join-Path $taskWorkspace ('outputs/ninja-account-sheet-reviews-migration-' + $taskStamp)
    [void](New-Item -ItemType Directory -Path $taskOutput -Force)
    Copy-Item -LiteralPath $taskScriptPath -Destination (Join-Path $taskOutput 'applied-script.sql')
    $taskPreflight.WriteXml((Join-Path $taskOutput 'preflight.xml'), [System.Data.XmlWriteMode]::WriteSchema)

    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-TaskData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @lockResult int;
EXEC @lockResult=sys.sp_getapplock @Resource=N'ninja-account-sheet-reviews-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @lockResult < 0 THROW 51000, 'Could not acquire migration lock.', 1;
'@)
    [void](Read-TaskData $taskPreflightSql)
    $taskBefore = Read-TaskData @'
SELECT a.* INTO #NinjaAccountsBefore FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK)
    JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT r.* INTO #NinjaAssignmentsBefore FROM app.RiderClientAssignments r WITH(UPDLOCK,HOLDLOCK)
    JOIN #NinjaAccountsBefore a ON a.Id=r.PlatformRiderAccountId;
SELECT v.* INTO #NinjaCredentialsBefore FROM app.PlatformAccountCredentialVersions v WITH(UPDLOCK,HOLDLOCK)
    JOIN #NinjaAccountsBefore a ON a.Id=v.PlatformRiderAccountId;
SELECT N'NinjaAccountDetails' AS TableName,COUNT_BIG(*) AS Records INTO #NinjaReportCountsBefore FROM ninja.NinjaAccountDetails WITH(UPDLOCK,HOLDLOCK)
UNION ALL SELECT N'NinjaDailyImports',COUNT_BIG(*) FROM ninja.NinjaDailyImports WITH(UPDLOCK,HOLDLOCK)
UNION ALL SELECT N'NinjaDailyPerformance',COUNT_BIG(*) FROM ninja.NinjaDailyPerformance WITH(UPDLOCK,HOLDLOCK)
UNION ALL SELECT N'NinjaDailyDeductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions WITH(UPDLOCK,HOLDLOCK);
SELECT (SELECT COUNT_BIG(*) FROM #NinjaAccountsBefore) AS Accounts,
       (SELECT COUNT_BIG(*) FROM #NinjaAssignmentsBefore) AS Assignments,
       (SELECT COUNT_BIG(*) FROM #NinjaCredentialsBefore) AS CredentialVersions;
SELECT * FROM #NinjaReportCountsBefore ORDER BY TableName;
'@
    $taskBefore.WriteXml((Join-Path $taskOutput 'before-counts.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    [void](Read-TaskData $taskBody)
    [void](Read-TaskData @'
IF EXISTS(SELECT * FROM #NinjaAccountsBefore EXCEPT SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA')
 OR EXISTS(SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA' EXCEPT SELECT * FROM #NinjaAccountsBefore)
    THROW 51000, 'Existing Ninja account values or rowversions changed.', 1;
IF EXISTS(SELECT * FROM #NinjaAssignmentsBefore EXCEPT SELECT r.* FROM app.RiderClientAssignments r JOIN #NinjaAccountsBefore a ON a.Id=r.PlatformRiderAccountId)
 OR EXISTS(SELECT r.* FROM app.RiderClientAssignments r JOIN #NinjaAccountsBefore a ON a.Id=r.PlatformRiderAccountId EXCEPT SELECT * FROM #NinjaAssignmentsBefore)
    THROW 51000, 'Existing Ninja assignments changed.', 1;
IF EXISTS(SELECT * FROM #NinjaCredentialsBefore EXCEPT SELECT v.* FROM app.PlatformAccountCredentialVersions v JOIN #NinjaAccountsBefore a ON a.Id=v.PlatformRiderAccountId)
 OR EXISTS(SELECT v.* FROM app.PlatformAccountCredentialVersions v JOIN #NinjaAccountsBefore a ON a.Id=v.PlatformRiderAccountId EXCEPT SELECT * FROM #NinjaCredentialsBefore)
    THROW 51000, 'Existing Ninja credentials changed.', 1;
IF EXISTS(SELECT * FROM #NinjaReportCountsBefore EXCEPT SELECT * FROM (
    SELECT N'NinjaAccountDetails' AS TableName,COUNT_BIG(*) AS Records FROM ninja.NinjaAccountDetails
    UNION ALL SELECT N'NinjaDailyImports',COUNT_BIG(*) FROM ninja.NinjaDailyImports
    UNION ALL SELECT N'NinjaDailyPerformance',COUNT_BIG(*) FROM ninja.NinjaDailyPerformance
    UNION ALL SELECT N'NinjaDailyDeductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions) c)
    THROW 51000, 'Existing Ninja report row counts changed.', 1;
'@)
    $taskVerifySql = Get-Content -LiteralPath $taskVerifyPath -Raw
    $taskVerification = Read-TaskData $taskVerifySql
    $taskTransaction.Commit(); $taskTransaction.Dispose(); $taskTransaction = $null
    $taskVerification.WriteXml((Join-Path $taskOutput 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)

    # Independently re-open and verify the committed schema and migration history.
    $taskConnection.Close(); $taskConnection.Open()
    $taskPostCommit = Read-TaskData $taskVerifySql
    $taskPostCommit.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskSchema = Read-TaskData @'
SELECT t.name AS TableName,c.name AS ColumnName,TYPE_NAME(c.user_type_id) AS SqlType,c.max_length,c.is_nullable
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
WHERE t.schema_id=SCHEMA_ID(N'ninja') AND t.name IN(N'NinjaAccountUpdateUploads',N'NinjaAccountUpdateRows') ORDER BY t.name,c.column_id;
SELECT OBJECT_NAME(object_id) AS TableName,name,is_unique,is_disabled FROM sys.indexes
WHERE object_id IN(OBJECT_ID(N'ninja.NinjaAccountUpdateUploads'),OBJECT_ID(N'ninja.NinjaAccountUpdateRows')) AND is_primary_key=0 ORDER BY TableName,name;
SELECT name,is_disabled,is_not_trusted,delete_referential_action FROM sys.foreign_keys
WHERE parent_object_id IN(OBJECT_ID(N'ninja.NinjaAccountUpdateUploads'),OBJECT_ID(N'ninja.NinjaAccountUpdateRows')) ORDER BY name;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261015134151_AddNinjaAccountSheetReviews';
SELECT SYSUTCDATETIME() AS VerifiedAtUtc;
'@
    $taskSchema.WriteXml((Join-Path $taskOutput 'schema-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    if ($taskSchema.Tables[0].Rows.Count -ne 35 -or $taskSchema.Tables[1].Rows.Count -ne 5 -or
        @($taskSchema.Tables[1].Rows | Where-Object { $_.is_disabled }).Count -ne 0) {
        throw 'Post-commit schema column/index inventory differs from the reviewed model.'
    }
    $taskUtc = [DateTime]::SpecifyKind($taskSchema.Tables[4].Rows[0].VerifiedAtUtc,[DateTimeKind]::Utc)
    $taskReport = [ordered]@{
        Server=$taskConnection.DataSource; Database=$taskConnection.Database; Migration=$taskMigrationId
        ScriptSha256=$taskSha; VerifiedAtUtc=$taskUtc.ToString('o'); VerifiedAtRiyadh=$taskUtc.AddHours(3).ToString('yyyy-MM-dd HH:mm:ss') + ' UTC+03:00'
        Tables=2; Columns=$taskSchema.Tables[0].Rows.Count; NonPrimaryIndexes=$taskSchema.Tables[1].Rows.Count
        UniqueNonPrimaryIndexes=@($taskSchema.Tables[1].Rows | Where-Object { $_.is_unique }).Count
        TrustedRestrictiveForeignKeys=$taskSchema.Tables[2].Rows.Count; TrustedCheckConstraints=1
        ExistingNinjaAccounts=[long]$taskBefore.Tables[0].Rows[0].Accounts
        ExistingNinjaAssignments=[long]$taskBefore.Tables[0].Rows[0].Assignments
        ExistingNinjaCredentialVersions=[long]$taskBefore.Tables[0].Rows[0].CredentialVersions
        MainAccountValuesAndRowversionsPreserved=$true; AssignmentsPreserved=$true; CredentialsPreserved=$true; ExistingReportCountsPreserved=$true
        PostCommitVerified=$true; Uploads=[long]$taskPostCommit.Tables[0].Rows[0].Records; UploadedRows=[long]$taskPostCommit.Tables[0].Rows[1].Records
        EvidenceDirectory=$taskOutput
    }
    $taskReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'verification.json') -Encoding UTF8
    $taskReport | ConvertTo-Json
} catch {
    if ($taskTransaction) { try { $taskTransaction.Rollback() } catch { } }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
