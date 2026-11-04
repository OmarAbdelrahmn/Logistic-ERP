param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
$transaction = $null
$applicationId = '20261104115133_AddHungerOperationsQualityAndSharedAccountContext'
$identityId = '20261104115200_GrantHungerAndPlatformFieldPermissions'
function Query([string]$Sql) {
    $command = $connection.CreateCommand(); $command.CommandTimeout = 180; $command.Transaction = $transaction; $command.CommandText = $Sql
    try { $data = [System.Data.DataSet]::new(); $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command); try { [void]$adapter.Fill($data) } finally { $adapter.Dispose() }; return ,$data } finally { $command.Dispose() }
}
try {
    $connection.Open()
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database.' }
    $preflight = Query @'
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN ('20261103120000_AddChefzPerformanceDeletePermission','20261104115133_AddHungerOperationsQualityAndSharedAccountContext');
SELECT MigrationId FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId DESC;
SELECT COUNT(*) ExistingHungerTables FROM sys.tables WHERE schema_id=SCHEMA_ID('hunger');
'@
    if ($preflight.Tables[0].Rows.MigrationId -notcontains '20261103120000_AddChefzPerformanceDeletePermission') { throw 'Application baseline is missing.' }
    if ($preflight.Tables[1].Rows.MigrationId -notcontains '20261028111320_GrantNinjaAssignmentDateCorrectionAccess') { throw 'Identity baseline is missing.' }
    $appDone = $preflight.Tables[0].Rows.MigrationId -contains $applicationId
    $identityDone = $preflight.Tables[1].Rows.MigrationId -contains $identityId
    if ($appDone -and $identityDone) { Write-Output 'Both Hunger migrations are already present.'; return }
    if ($appDone -or $identityDone -or $preflight.Tables[2].Rows[0].ExistingHungerTables -ne 0) { throw 'Partial Hunger deployment detected; reconcile before continuing.' }
    $scripts = @('hunger-application.sql','hunger-identity.sql') | ForEach-Object { Get-Content -LiteralPath (Join-Path $workspace "database/scripts/$_") -Raw }
    $body = ($scripts -join [Environment]::NewLine) -replace '(?im)^\s*BEGIN TRANSACTION;\s*$', '' -replace '(?im)^\s*COMMIT;\s*$', ''
    # These reviewed EF scripts contain one statement per terminator line. Execute separately so
    # SQL Server compiles indexes/checks only after the additive columns exist, in the same transaction.
    $batches = [regex]::Split($body, '(?im)^[ \t]*GO[ \t]*\r?$|;[ \t]*(?:\r?\n|$)') | Where-Object { ![string]::IsNullOrWhiteSpace($_) }
    if ($body -notmatch [regex]::Escape($applicationId) -or $body -notmatch [regex]::Escape($identityId) -or $body -match '(?im)^\s*(DROP|DELETE|UPDATE)\b') { throw 'Unexpected migration content.' }
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    foreach ($batch in $batches) { [void](Query $batch) }
    $checks = Query "SELECT COUNT(*) TablesCreated FROM sys.tables WHERE schema_id=SCHEMA_ID('hunger'); SELECT COUNT(*) PermissionsCreated FROM platform.PermissionDefinitions WHERE [Key] LIKE 'hunger.%';"
    if ($checks.Tables[0].Rows[0].TablesCreated -ne 10 -or $checks.Tables[1].Rows[0].PermissionsCreated -ne 11) { throw 'Migration verification count mismatch.' }
    $transaction.Rollback(); $transaction.Dispose(); $transaction = $null
    Write-Output 'Migration rehearsal succeeded and was rolled back.'
    if (!$Apply) { return }
    $backupCommand = $connection.CreateCommand(); $backupCommand.CommandTimeout = 180
    $backupCommand.CommandText = "DECLARE @dir nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)); IF @dir IS NULL THROW 51000,'Backup directory unavailable',1; DECLARE @path nvarchar(4000)=@dir + CASE WHEN RIGHT(@dir,1) IN ('\','/') THEN '' ELSE '\' END + 'db67927_before_hunger_' + CONVERT(varchar(8),GETUTCDATE(),112) + '_' + REPLACE(CONVERT(varchar(8),GETUTCDATE(),108),':','') + '.bak'; BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;"
    try { [void]$backupCommand.ExecuteNonQuery() } finally { $backupCommand.Dispose() }
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    foreach ($batch in $batches) { [void](Query $batch) }
    $transaction.Commit(); $transaction.Dispose(); $transaction = $null
    $connection.Close(); $connection.Open()
    $result = Query "SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$applicationId'; SELECT MigrationId FROM migration.__IdentityMigrationsHistory WHERE MigrationId='$identityId';"
    if ($result.Tables[0].Rows.Count -ne 1 -or $result.Tables[1].Rows.Count -ne 1) { throw 'Post-commit migration verification failed; do not replay.' }
    Write-Output 'Hosted application and identity migrations committed and verified on a fresh connection.'
} finally {
    if ($transaction) { try { $transaction.Rollback() } finally { $transaction.Dispose() } }
    $connection.Dispose()
}

