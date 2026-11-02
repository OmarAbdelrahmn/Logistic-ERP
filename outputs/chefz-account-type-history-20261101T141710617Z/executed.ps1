param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$migrationId = '20261101140621_AddChefzAccountTypeHistory'
$scriptPath = Join-Path $taskRoot 'database/scripts/chefz-account-type-history.sql'
$config = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connectionString = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$transaction = $null
function Query([string]$Sql) {
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
    if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected database target.'
    }
    $connection.Open()
    $preflight = Query @'
SET NOCOUNT ON; SET LOCK_TIMEOUT 15000;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261031143205_AllowJahezPetroAppDebtReceipts')
    THROW 51000, 'Required predecessor migration is missing.', 1;
SELECT TOP(5) MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId DESC;
SELECT COUNT_BIG(*) SettlementCount,COALESCE(SUM(CompanyAmount),0) CompanyTotal,COALESCE(SUM(RiderAmount),0) RiderTotal FROM chefz.WalletSettlements;
SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements') ORDER BY column_id;
SELECT COUNT(*) AlreadyApplied FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261101140621_AddChefzAccountTypeHistory';
SELECT OBJECT_ID(N'chefz.AccountTypeChanges',N'U') TypeChangesTable,
 COL_LENGTH(N'chefz.WalletSettlements',N'FullTimeDays') FullTimeDays,
 COL_LENGTH(N'chefz.WalletSettlements',N'FreelancerCommissionBase') FreelancerCommissionBase;
'@
    Write-Output ('Verified hosted target: ' + $connection.DataSource + ' / ' + $connection.Database)
    $preflight.Tables[0] | Format-Table -AutoSize
    $preflight.Tables[1] | Format-Table -AutoSize
    $alreadyApplied = [int]$preflight.Tables[3].Rows[0].AlreadyApplied -eq 1
    if ($alreadyApplied) { throw 'Migration already recorded; inspect schema before repeating.' }
    $schema = $preflight.Tables[4].Rows[0]
    if ($schema.TypeChangesTable -isnot [DBNull] -or $schema.FullTimeDays -isnot [DBNull] -or $schema.FreelancerCommissionBase -isnot [DBNull]) {
        throw 'Partial or unrecorded migration schema detected.'
    }
    if (!$Apply) { Write-Output 'Preflight passed. -Apply executes this migration only.'; return }
    $script = Get-Content -LiteralPath $scriptPath -Raw
    if ($script -notmatch [regex]::Escape($migrationId) -or $script -notmatch 'COMMIT;\s*GO\s*$') { throw 'Unexpected migration SQL.' }
    $body = $script -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', '' -replace '(?im)^GO\s*$', ''
    if ($body -match '(?im)^\s*(BEGIN TRANSACTION|COMMIT)\s*;') { throw 'Unexpected nested transaction.' }
    $columns = ($preflight.Tables[2].Rows | ForEach-Object { '[' + $_.name.Replace(']',']]') + ']' }) -join ','
    if ([string]::IsNullOrWhiteSpace($columns)) { throw 'Existing settlement columns missing.' }
    $taskOutput = Join-Path $taskRoot ('outputs/chefz-account-type-history-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $taskOutput)
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
    Copy-Item -LiteralPath $scriptPath -Destination (Join-Path $taskOutput 'migration.sql')
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $before = Query @'
SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
IF EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WITH(UPDLOCK,HOLDLOCK) WHERE MigrationId='20261101140621_AddChefzAccountTypeHistory')
    THROW 51000, 'Migration concurrently applied.', 1;
SELECT * INTO #ChefzSettlementBefore FROM chefz.WalletSettlements WITH(TABLOCK,UPDLOCK,HOLDLOCK);
SELECT * INTO #ChefzAccountDetailsBefore FROM chefz.AccountDetails WITH(TABLOCK,UPDLOCK,HOLDLOCK);
SELECT * FROM #ChefzSettlementBefore ORDER BY Id;
SELECT * FROM #ChefzAccountDetailsBefore ORDER BY Id;
SELECT * FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
'@
    $before.Tables[0].TableName = 'WalletSettlementsBefore'
    $before.Tables[1].TableName = 'AccountDetailsBefore'
    $before.Tables[2].TableName = 'ApplicationMigrationsBefore'
    $before.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    [void](Query $body)
    $verified = Query @"
IF (SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId')<>1
    THROW 51000, 'Migration history missing.', 1;
IF OBJECT_ID(N'chefz.AccountTypeChanges',N'U') IS NULL
    THROW 51000, 'Type change table missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements') AND name='FullTimeDays' AND TYPE_NAME(user_type_id)='int' AND is_nullable=1)
 OR NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements') AND name='FreelancerCommissionBase' AND TYPE_NAME(user_type_id)='decimal' AND precision=18 AND scale=2 AND is_nullable=1)
    THROW 51000, 'Settlement column definitions are incorrect.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'chefz.AccountTypeChanges')
 AND name='IX_AccountTypeChanges_PlatformRiderAccountId_EffectiveFrom' AND is_unique=1 AND is_disabled=0)
    THROW 51000, 'Type change unique index missing.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'chefz.AccountTypeChanges')
 AND name='CK_ChefzTypeChange_Types' AND is_disabled=0 AND is_not_trusted=0)
    THROW 51000, 'Type change constraint missing or untrusted.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'chefz.AccountTypeChanges')
 AND referenced_object_id=OBJECT_ID(N'app.PlatformRiderAccounts') AND is_disabled=0 AND is_not_trusted=0)
    THROW 51000, 'Type change foreign key missing or untrusted.', 1;
IF EXISTS(SELECT $columns FROM #ChefzSettlementBefore EXCEPT SELECT $columns FROM chefz.WalletSettlements)
 OR EXISTS(SELECT $columns FROM chefz.WalletSettlements EXCEPT SELECT $columns FROM #ChefzSettlementBefore)
 OR (SELECT COUNT_BIG(*) FROM #ChefzSettlementBefore)<>(SELECT COUNT_BIG(*) FROM chefz.WalletSettlements)
    THROW 51000, 'Existing settlement values changed.', 1;
IF EXISTS(SELECT * FROM #ChefzAccountDetailsBefore EXCEPT SELECT * FROM chefz.AccountDetails)
 OR EXISTS(SELECT * FROM chefz.AccountDetails EXCEPT SELECT * FROM #ChefzAccountDetailsBefore)
    THROW 51000, 'Existing account details changed.', 1;
IF EXISTS(SELECT 1 FROM chefz.WalletSettlements WHERE FullTimeDays IS NOT NULL OR FreelancerCommissionBase IS NOT NULL)
 OR EXISTS(SELECT 1 FROM chefz.AccountTypeChanges)
    THROW 51000, 'Unexpected financial or conversion data inserted.', 1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId';
SELECT COUNT_BIG(*) SettlementCount,COALESCE(SUM(CompanyAmount),0) CompanyTotal,COALESCE(SUM(RiderAmount),0) RiderTotal FROM chefz.WalletSettlements;
SELECT COUNT_BIG(*) AccountDetailsCount FROM chefz.AccountDetails;
SELECT name,TYPE_NAME(user_type_id) SqlType,precision,scale,is_nullable FROM sys.columns
 WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements') AND name IN('FullTimeDays','FreelancerCommissionBase');
SELECT SYSUTCDATETIME() VerifiedAtUtc;
"@
    $verified.WriteXml((Join-Path $taskOutput 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $transaction.Commit()
    $transaction.Dispose()
    $transaction = $null
    $report = [ordered]@{
        Database = $connection.Database
        Migration = $migrationId
        Applied = $true
        ScriptSha256 = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
        OriginalSettlementValuesPreserved = $true
        OriginalAccountDetailsPreserved = $true
        SettlementCount = [long]$verified.Tables[1].Rows[0].SettlementCount
        AccountDetailsCount = [long]$verified.Tables[2].Rows[0].AccountDetailsCount
        VerifiedAtUtc = [DateTime]::SpecifyKind($verified.Tables[4].Rows[0].VerifiedAtUtc,[DateTimeKind]::Utc).ToString('o')
    }
    $report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'verification.json') -Encoding UTF8
    $report | ConvertTo-Json
    Write-Output ('Backup and verification: ' + $taskOutput)
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch {} }
    throw
} finally {
    if ($transaction) { $transaction.Dispose() }
    $connection.Dispose()
}
