param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$migrationId = '20261012093719_AddChefzOrderSettlementImports'
$scriptPath = Join-Path $workspace 'database/scripts/chefz-order-settlements.sql'
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
        throw 'Unexpected hosted database; refusing Chefz migration.'
    }
    $preflight = Read-Data @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261011191524_AddChefzWorkbookReportsAndStatistics')
    THROW 51000, 'Required Chefz workbook migration is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261012093719_AddChefzOrderSettlementImports')
   AND (OBJECT_ID(N'chefz.OrderSettlementImports',N'U') IS NOT NULL
       OR EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements')
           AND name IN ('Source','ImportId','DriverId','RiderName','SourceRows','DeliveryFees','VatAmount','Tips','Compensation','Bonuses','Deductions')))
    THROW 51000, 'Partial Chefz settlement schema detected; reconcile before applying.', 1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory
    WHERE MigrationId IN ('20261011191524_AddChefzWorkbookReportsAndStatistics','20261012093719_AddChefzOrderSettlementImports') ORDER BY MigrationId;
SELECT COUNT_BIG(*) SettlementCount,COALESCE(SUM(GrossEarnings),0) NetTotal,
    COALESCE(SUM(CompanyAmount),0) CompanyTotal,COALESCE(SUM(RiderAmount),0) RiderTotal FROM chefz.WalletSettlements;
SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name NOT IN ('Source','ImportId','DriverId','RiderName','SourceRows','DeliveryFees','VatAmount','Tips','Compensation','Bonuses','Deductions') ORDER BY column_id;
'@
    Write-Output ('Verified hosted target: ' + $connection.DataSource + ' / ' + $connection.Database)
    $preflight.Tables[0] | Format-Table -AutoSize
    $preflight.Tables[1] | Format-Table -AutoSize
    if (!$Apply) { Write-Output 'Preflight only. -Apply executes the authorized Chefz order-settlement migration.'; return }

    $script = Get-Content -LiteralPath $scriptPath -Raw
    if ($script -notmatch [regex]::Escape($migrationId) -or $script -notmatch 'COMMIT;\s*(GO\s*)?$') {
        throw 'Missing or unexpected Chefz migration script.'
    }
    $script = [regex]::Replace($script, '(?im)^\s*GO\s*\r?$', '')
    $body = $script -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', ''
    if ($body -match '(?im)^\s*(BEGIN TRANSACTION|COMMIT)\s*;') { throw 'Unexpected nested transaction in migration script.' }
    $originalColumns = ($preflight.Tables[2].Rows | ForEach-Object { '[' + $_.name.Replace(']',']]') + ']' }) -join ','
    if ([string]::IsNullOrWhiteSpace($originalColumns)) { throw 'Original settlement columns were not found.' }
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $outputDirectory = Join-Path $workspace ('outputs/chefz-order-settlement-migration-' + $stamp)
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-Data 'SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;')
    $before = Read-Data @"
SELECT * FROM chefz.WalletSettlements WITH (TABLOCK,UPDLOCK,HOLDLOCK) ORDER BY Id;
SELECT * FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT $originalColumns INTO #ChefzSettlementBefore FROM chefz.WalletSettlements;
IF OBJECT_ID(N'chefz.OrderSettlementImports',N'U') IS NOT NULL
    EXEC(N'SELECT * FROM chefz.OrderSettlementImports ORDER BY Id;');
"@
    $before.Tables[0].TableName = 'WalletSettlementsBefore'
    $before.Tables[1].TableName = 'ApplicationMigrationsBefore'
    if ($before.Tables.Count -gt 2) { $before.Tables[2].TableName = 'OrderSettlementImportsBefore' }
    $before.WriteXml((Join-Path $outputDirectory 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $scriptPath -Destination (Join-Path $outputDirectory 'applied-script.sql')
    $batchCountBefore = if ($before.Tables.Count -gt 2) { $before.Tables[2].Rows.Count } else { 0 }
    [void](Read-Data $body)
    $verification = Read-Data @"
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId')
    THROW 51000, 'Chefz migration history was not recorded.', 1;
IF OBJECT_ID(N'chefz.OrderSettlementImports',N'U') IS NULL
    THROW 51000, 'Chefz order-import table is missing.', 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name IN ('Source','ImportId','DriverId','RiderName','SourceRows','DeliveryFees','VatAmount','Tips','Compensation','Bonuses','Deductions')) <> 11
    THROW 51000, 'Chefz settlement columns are incomplete.', 1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN ('CK_ChefzSettlement_Amounts','CK_ChefzOrderImport_Rows')
    AND is_disabled=0 AND is_not_trusted=0) <> 2
    THROW 51000, 'Chefz constraints are missing, disabled, or untrusted.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'chefz.OrderSettlementImports')
    AND name='IX_OrderSettlementImports_ContentHash' AND is_unique=1 AND is_disabled=0)
    THROW 51000, 'Chefz import hash uniqueness is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name='IX_WalletSettlements_RiderClientAssignmentId_PeriodFrom' AND is_unique=1 AND is_disabled=0)
    THROW 51000, 'Chefz assignment/cycle uniqueness is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name='FK_WalletSettlements_OrderSettlementImports_ImportId' AND is_disabled=0 AND is_not_trusted=0)
    THROW 51000, 'Chefz import foreign key is missing or untrusted.', 1;
IF EXISTS (SELECT $originalColumns FROM #ChefzSettlementBefore EXCEPT SELECT $originalColumns FROM chefz.WalletSettlements)
    OR EXISTS (SELECT $originalColumns FROM chefz.WalletSettlements EXCEPT SELECT $originalColumns FROM #ChefzSettlementBefore)
    THROW 51000, 'Existing settlement values changed.', 1;
IF (SELECT COUNT_BIG(*) FROM chefz.OrderSettlementImports) <> $batchCountBefore
    THROW 51000, 'Unexpected financial import rows were inserted.', 1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId';
SELECT name,TYPE_NAME(user_type_id) SqlType,max_length,precision,scale,is_nullable FROM sys.columns
    WHERE object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name IN ('Source','ImportId','DriverId','RiderName','SourceRows','DeliveryFees','VatAmount','Tips','Compensation','Bonuses','Deductions') ORDER BY name;
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE name IN ('CK_ChefzSettlement_Amounts','CK_ChefzOrderImport_Rows');
SELECT name,is_unique,is_disabled FROM sys.indexes WHERE (object_id=OBJECT_ID(N'chefz.WalletSettlements')
    AND name IN ('IX_WalletSettlements_RiderClientAssignmentId_PeriodFrom','IX_WalletSettlements_ImportId'))
    OR (object_id=OBJECT_ID(N'chefz.OrderSettlementImports') AND name='IX_OrderSettlementImports_ContentHash');
SELECT name,is_disabled,is_not_trusted FROM sys.foreign_keys WHERE name='FK_WalletSettlements_OrderSettlementImports_ImportId';
SELECT COUNT_BIG(*) SettlementCount,COALESCE(SUM(GrossEarnings),0) NetTotal,
    COALESCE(SUM(CompanyAmount),0) CompanyTotal,COALESCE(SUM(RiderAmount),0) RiderTotal FROM chefz.WalletSettlements;
SELECT COUNT_BIG(*) OrderImportCount FROM chefz.OrderSettlementImports;
SELECT SYSUTCDATETIME() VerifiedAtUtc;
"@
    $transaction.Commit()
    $transaction.Dispose()
    $transaction = $null
    $verification.WriteXml((Join-Path $outputDirectory 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{
        Database = $connection.Database
        Server = $connection.DataSource
        Migration = $migrationId
        ScriptSha256 = (Get-FileHash -LiteralPath $scriptPath -Algorithm SHA256).Hash
        VerifiedAtUtc = [DateTime]::SpecifyKind($verification.Tables[7].Rows[0].VerifiedAtUtc, [DateTimeKind]::Utc).ToString('o')
        OriginalSettlementValuesPreserved = $true
        OrderImportRowsInserted = 0
        NewSettlementColumns = $verification.Tables[1].Rows.Count
        TrustedCheckConstraints = $verification.Tables[2].Rows.Count
        VerifiedIndexes = $verification.Tables[3].Rows.Count
        TrustedImportForeignKeys = $verification.Tables[4].Rows.Count
    }
    $report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDirectory 'verification.json') -Encoding UTF8
    $report | ConvertTo-Json
    Write-Output ('Backup and verification: ' + $outputDirectory)
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch { } }
    throw
} finally {
    if ($transaction) { $transaction.Dispose() }
    $connection.Dispose()
}
