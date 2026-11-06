param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
$transaction = $null
$baselineApp = '20261104115133_AddHungerOperationsQualityAndSharedAccountContext'
$baselineIdentity = '20261104115200_GrantHungerAndPlatformFieldPermissions'
$appIds = @('20261105145423_HungerWalletV21ReconciliationAndWithdrawalArrival','20261106083232_HungerReceiptLifecycleAndFinancePermissions',
    '20261106090616_HungerWalletReviewIntegrityAndRiderSwap','20261106093923_GrantFleetVehicleIdentityFieldPermissions',
    '20261106131847_HungerPerformanceCatalogAndPotentialFines','20261106143740_HungerSourceCaptainClassification')
$identityIds = @('20261106083738_GrantHungerFinanceActionPermissions','20261106094011_GrantFleetVehicleIdentityFieldPermissions')
function Query([string]$Sql) {
    $command = $connection.CreateCommand(); $command.CommandTimeout = 300; $command.Transaction = $transaction; $command.CommandText = $Sql
    try { $data = [System.Data.DataSet]::new(); $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command); try { [void]$adapter.Fill($data) } finally { $adapter.Dispose() }; return ,$data } finally { $command.Dispose() }
}
function Count-Applied {
    $list = ($appIds | ForEach-Object { "'$_'" }) -join ','; $ilist = ($identityIds | ForEach-Object { "'$_'" }) -join ','
    $r = Query "SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN ($list); SELECT COUNT(*) FROM migration.__IdentityMigrationsHistory WHERE MigrationId IN ($ilist);"
    return @($r.Tables[0].Rows[0][0], $r.Tables[1].Rows[0][0])
}
try {
    $connection.Open()
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database.' }
    $pre = Query "SELECT TOP 1 MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId DESC; SELECT TOP 1 MigrationId FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId DESC;"
    $applied = Count-Applied
    if ($applied[0] -eq $appIds.Count -and $applied[1] -eq $identityIds.Count) { Write-Output 'All Hunger v2.1 migrations are already present.'; return }
    if ($applied[0] -ne 0 -or $applied[1] -ne 0) { throw 'Partial Hunger v2.1 deployment detected; reconcile before continuing.' }
    if ($pre.Tables[0].Rows[0][0] -ne $baselineApp -or $pre.Tables[1].Rows[0][0] -ne $baselineIdentity) { throw 'Hosted migration baseline differs from expected; aborting.' }
    $body = @('hunger-v21-application.sql','hunger-v21-identity.sql') | ForEach-Object { Get-Content -LiteralPath (Join-Path $workspace "database/scripts/$_") -Raw -Encoding UTF8 }
    $body = ($body -join "`nGO`n") -replace '(?im)^\s*BEGIN TRANSACTION;\s*$', '' -replace '(?im)^\s*COMMIT;\s*$', ''
    foreach ($id in $appIds + $identityIds) { if ($body -notmatch [regex]::Escape($id)) { throw "Script is missing $id." } }
    # Split only on GO: the receipt migration contains a multi-statement cursor block that must stay in one batch.
    # Other batches run one statement at a time so later statements compile after earlier ALTERs.
    $batches = [regex]::Split($body, '(?im)^[ \t]*GO[ \t]*\r?$') | Where-Object { ![string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        if ($_ -match '(?i)\bCURSOR\b') { $_; return }
        # EF's default-constraint helper (DECLARE @var ... IF @var ... EXEC) must stay in one batch.
        $pending = $null
        foreach ($piece in [regex]::Split($_, ';[ \t]*\r?\n')) {
            if ($pending -ne $null) { $pending += ";`n" + $piece; if ($piece -match '(?im)^\s*IF @var') { $pending; $pending = $null }; continue }
            if ($piece -match '(?im)^\s*DECLARE @var') { $pending = $piece; continue }
            $piece
        }
        if ($pending -ne $null) { $pending }
    } | Where-Object { ![string]::IsNullOrWhiteSpace($_) }
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    foreach ($batch in $batches) { [void](Query $batch) }
    $applied = Count-Applied
    if ($applied[0] -ne $appIds.Count -or $applied[1] -ne $identityIds.Count) { throw 'Rehearsal history count mismatch.' }
    $transaction.Rollback(); $transaction.Dispose(); $transaction = $null
    Write-Output "Rehearsal of $($batches.Count) batches succeeded and was rolled back."
    if (!$Apply) { return }
    $backupCommand = $connection.CreateCommand(); $backupCommand.CommandTimeout = 600
    $backupCommand.CommandText = "DECLARE @dir nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)); IF @dir IS NULL THROW 51000,'Backup directory unavailable',1; DECLARE @path nvarchar(4000)=@dir + CASE WHEN RIGHT(@dir,1) IN ('\','/') THEN '' ELSE '\' END + 'db67927_before_hunger_v21_' + CONVERT(varchar(8),GETUTCDATE(),112) + '_' + REPLACE(CONVERT(varchar(8),GETUTCDATE(),108),':','') + '.bak'; BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM; SELECT @path;"
    try { $backupPath = $backupCommand.ExecuteScalar() } finally { $backupCommand.Dispose() }
    Write-Output "Backup written: $backupPath"
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    foreach ($batch in $batches) { [void](Query $batch) }
    $transaction.Commit(); $transaction.Dispose(); $transaction = $null
    $connection.Close(); $connection.Open()
    $applied = Count-Applied
    if ($applied[0] -ne $appIds.Count -or $applied[1] -ne $identityIds.Count) { throw 'Post-commit verification failed; do not replay.' }
    Write-Output 'Hunger v2.1 application and identity migrations committed and verified on a fresh connection.'
} finally {
    if ($transaction) { try { $transaction.Rollback() } finally { $transaction.Dispose() } }
    $connection.Dispose()
}
