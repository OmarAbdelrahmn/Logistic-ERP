param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$configuredConnection = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [System.Data.SqlClient.SqlConnection]::new($configuredConnection)
$transaction = $null
$appMigration = '20261015072958_AddJahezWorkflowV2'
$identityMigration = '20261015072948_GrantJahezWorkflowPermissions'
function Read-Data([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    $command.Transaction = $transaction
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
        throw 'Unexpected hosted database; refusing Jahez V2 migration.'
    }
    $preflight = Read-Data @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261011122817_AddJahezDebtFreezeAndClearance')
    THROW 51000, 'Jahez debt migration baseline is missing.', 1;
IF NOT EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId='20261010112140_GrantChefzPermissions')
    THROW 51000, 'Identity baseline is missing; reconcile before applying.', 1;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261015072958_AddJahezWorkflowV2')
   AND (OBJECT_ID('jahez.JahezOperation','U') IS NOT NULL OR COL_LENGTH('jahez.JahezApprovalRequest','IsV2') IS NOT NULL)
    THROW 51000, 'Partial Jahez V2 schema detected.', 1;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT MigrationId FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId;
SELECT COUNT_BIG(*) Handovers FROM jahez.JahezAccountHandover;
SELECT COUNT_BIG(*) LedgerEntries,COALESCE(SUM(Amount),0) SignedLedgerTotal FROM jahez.JahezLedgerEntry;
SELECT COUNT_BIG(*) Receipts,COALESCE(SUM(Amount),0) ReceivedTotal FROM jahez.JahezCashboxEntry WHERE IsDeleted=0;
'@
    Write-Output ('Verified target: ' + $connection.DataSource + ' / ' + $connection.Database)
    $preflight.Tables[2] | Format-Table -AutoSize
    $preflight.Tables[3] | Format-Table -AutoSize
    $preflight.Tables[4] | Format-Table -AutoSize
    if (!$Apply) { Write-Output 'Preflight only; no schema or business data changed.'; return }

    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $outputDirectory = Join-Path $workspace ('outputs/jahez-v2-migration-' + $stamp)
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    $preflight.WriteXml((Join-Path $outputDirectory 'preflight.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $scriptNames = @('jahez-v2-application.sql','jahez-v2-identity.sql')
    $scripts = @()
    foreach ($name in $scriptNames) {
        $path = Join-Path $workspace ('database/scripts/' + $name)
        $script = Get-Content -LiteralPath $path -Raw
        if ($script -match '(?im)^\s*(DROP|DELETE|UPDATE|BEGIN TRANSACTION|COMMIT)\b') {
            throw ('Unexpected destructive statement or embedded transaction: ' + $name)
        }
        $scripts += $script
        Copy-Item -LiteralPath $path -Destination (Join-Path $outputDirectory $name)
    }
    if ($scripts[0] -notmatch [regex]::Escape($appMigration) -or $scripts[1] -notmatch [regex]::Escape($identityMigration)) {
        throw 'Unexpected migration script identity.'
    }
    $protected = @('JahezAccountHandover','JahezAccountFee','JahezApprovalRequest','JahezApprovalDecision',
        'JahezCommissionPolicyPeriod','JahezEarningsStatement','JahezLedgerEntry','JahezRiderSettlement','JahezCashboxEntry','JahezCashboxHandover')
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-Data 'SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;')
    $checks = @()
    foreach ($table in $protected) {
        $columns = Read-Data "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez.$table') AND name <> 'RowVersion' ORDER BY column_id;"
        $list = ($columns.Tables[0].Rows | ForEach-Object { '[' + $_.name.Replace(']',']]') + ']' }) -join ','
        if (!$list) { throw ('Missing protected table: ' + $table) }
        $before = Read-Data "SELECT $list INTO #Before$table FROM jahez.[$table] WITH (UPDLOCK,HOLDLOCK); SELECT * FROM #Before$table;"
        $before.WriteXml((Join-Path $outputDirectory ($table + '-before.xml')), [System.Data.XmlWriteMode]::WriteSchema)
        $checks += "IF EXISTS(SELECT $list FROM #Before$table EXCEPT SELECT $list FROM jahez.[$table]) OR EXISTS(SELECT $list FROM jahez.[$table] EXCEPT SELECT $list FROM #Before$table) THROW 51000,'Protected Jahez evidence changed: $table',1;"
    }
    foreach ($script in $scripts) {
        foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) {
            if ($batch.Trim()) { [void](Read-Data $batch) }
        }
    }
    foreach ($check in $checks) { [void](Read-Data $check) }
    $verification = Read-Data @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261015072958_AddJahezWorkflowV2')
    THROW 51000,'Application migration not recorded.',1;
IF NOT EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId='20261015072948_GrantJahezWorkflowPermissions')
    THROW 51000,'Identity migration not recorded.',1;
IF (SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE [Key] IN
 ('jahez.withdraw','jahez.settle','jahez.fees.record','jahez.accounts.status.update','jahez.money.receive','jahez.money.disburse',
  'jahez.administrative-debt.adjust','jahez.transferred-debt.repay','jahez.requests.fee.create','jahez.requests.fee.approve',
  'jahez.requests.switch.create','jahez.requests.switch.approve','jahez.requests.transfer.create','jahez.requests.transfer.approve',
  'jahez.requests.commission.create','jahez.requests.commission.approve','jahez.requests.adjustment.create','jahez.requests.adjustment.approve'))<>18
    THROW 51000,'Jahez V2 permission definitions incomplete.',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id IN
 (OBJECT_ID('jahez.JahezOperation'),OBJECT_ID('jahez.JahezMoneyMovement'),OBJECT_ID('jahez.JahezTransferredDebt'))
 AND (is_disabled=1 OR is_not_trusted=1)) THROW 51000,'Untrusted Jahez V2 constraints.',1;
IF EXISTS(SELECT 1 FROM jahez.JahezCashboxEntry e WHERE e.IsDeleted=0
 AND NOT EXISTS(SELECT 1 FROM jahez.JahezMoneyMovement m WHERE m.LegacyCashboxEntryId=e.Id AND m.Amount=e.Amount))
    THROW 51000,'Legacy receipt reconciliation failed.',1;
SELECT COUNT_BIG(*) LegacyReceipts,COALESCE(SUM(Amount),0) Amount FROM jahez.JahezCashboxEntry WHERE IsDeleted=0;
SELECT COUNT_BIG(*) MigratedReceipts,COALESCE(SUM(Amount),0) Amount FROM jahez.JahezMoneyMovement WHERE LegacyCashboxEntryId IS NOT NULL;
SELECT COUNT_BIG(*) Operations FROM jahez.JahezOperation;
SELECT COUNT_BIG(*) TransferredDebts FROM jahez.JahezTransferredDebt;
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id IN
 (OBJECT_ID('jahez.JahezOperation'),OBJECT_ID('jahez.JahezMoneyMovement'),OBJECT_ID('jahez.JahezTransferredDebt'));
SELECT SYSUTCDATETIME() VerifiedAtUtc;
'@
    $verification.WriteXml((Join-Path $outputDirectory 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $transaction.Commit()
    $transaction.Dispose()
    $transaction = $null
    $report = [ordered]@{ Server=$connection.DataSource; Database=$connection.Database; AppliedAtUtc=[DateTime]::UtcNow.ToString('o');
        ApplicationMigration=$appMigration; IdentityMigration=$identityMigration; ProtectedTablesPreserved=$protected;
        ScriptHashes=@($scriptNames | ForEach-Object { @{ Name=$_; Sha256=(Get-FileHash -LiteralPath (Join-Path $outputDirectory $_) -Algorithm SHA256).Hash } });
        LegacyReceipts=[long]$verification.Tables[0].Rows[0].LegacyReceipts; MigratedReceipts=[long]$verification.Tables[1].Rows[0].MigratedReceipts;
        LegacyAmount=[decimal]$verification.Tables[0].Rows[0].Amount; MigratedAmount=[decimal]$verification.Tables[1].Rows[0].Amount }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputDirectory 'report.json') -Encoding UTF8
    $report | ConvertTo-Json -Depth 6
    Write-Output ('Backup and verification: ' + $outputDirectory)
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch {} }
    throw
} finally {
    if ($transaction) { $transaction.Dispose() }
    $connection.Dispose()
}
