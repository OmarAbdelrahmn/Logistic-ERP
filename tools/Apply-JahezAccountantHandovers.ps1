param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$handoverWorkspace = Split-Path -Parent $PSScriptRoot
$handoverConfig = Get-Content -LiteralPath (Join-Path $handoverWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$handoverConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $handoverConfig.ConnectionStrings.LogisticsDatabase }
$handoverConnection = [System.Data.SqlClient.SqlConnection]::new($handoverConnectionText)
$handoverTransaction = $null
$handoverMigration = '20261017165534_AddJahezV2AccountantHandovers'
$handoverScriptPath = Join-Path $handoverWorkspace 'database/scripts/jahez-v2-accountant-handovers.sql'

function Read-HandoverMigrationData([string]$Sql) {
    $handoverCommand = $handoverConnection.CreateCommand()
    $handoverCommand.CommandText = $Sql
    $handoverCommand.CommandTimeout = 120
    $handoverCommand.Transaction = $handoverTransaction
    try {
        $handoverData = [System.Data.DataSet]::new()
        $handoverAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($handoverCommand)
        try { [void]$handoverAdapter.Fill($handoverData) } finally { $handoverAdapter.Dispose() }
        return ,$handoverData
    } finally { $handoverCommand.Dispose() }
}

$handoverPreflightSql = @'
IF OBJECT_ID(N'jahez_v2.MoneyMovements',N'U') IS NULL OR OBJECT_ID(N'jahez_v2.AccountingEntries',N'U') IS NULL
    THROW 51000,'Jahez V2 financial baseline is missing.',1;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017095007_AddNinjaPermissionCatalog')
    THROW 51000,'Expected preceding application migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017165534_AddJahezV2AccountantHandovers')
 AND (OBJECT_ID(N'jahez_v2.CashboxHandovers',N'U') IS NOT NULL OR OBJECT_ID(N'jahez_v2.CashboxHandoverMovements',N'U') IS NOT NULL)
    THROW 51000,'Partial accountant handover migration detected.',1;
IF (SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE IsDeleted=0 AND IsDeprecated=0 AND RequiresClientScope=1
 AND [Key] IN(N'jahez.cashbox.read',N'jahez.cashbox.submit',N'jahez.cashbox.confirm'))<>3
    THROW 51000,'Required existing Jahez cashbox permissions are missing.',1;
SELECT COUNT_BIG(*) AS MovementCount,COALESCE(SUM(Amount),0) AS MovementAmount FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerCount,COALESCE(SUM(Amount),0) AS LedgerAmount FROM jahez_v2.AccountingEntries;
SELECT COUNT_BIG(*) AS AccountCount FROM app.PlatformRiderAccounts;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017165534_AddJahezV2AccountantHandovers';
'@

$handoverVerifySql = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017165534_AddJahezV2AccountantHandovers')
    THROW 51000,'Accountant handover migration history was not recorded.',1;
IF OBJECT_ID(N'jahez_v2.CashboxHandovers',N'U') IS NULL OR OBJECT_ID(N'jahez_v2.CashboxHandoverMovements',N'U') IS NULL
    THROW 51000,'Accountant handover tables are missing.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'jahez_v2.CashboxHandovers') AND is_disabled=0 AND is_not_trusted=0)<>2
    THROW 51000,'Accountant handover constraints were not verified.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.CashboxHandovers') AND name=N'IX_CashboxHandovers_Status'
 AND is_unique=1 AND has_filter=1 AND is_disabled=0)
    THROW 51000,'Single-pending-handover unique index is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.CashboxHandoverMovements') AND name=N'IX_CashboxHandoverMovements_CashboxHandoverId_MoneyMovementId'
 AND is_unique=1 AND is_disabled=0)
    THROW 51000,'Handover membership unique index is missing.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'jahez_v2.CashboxHandoverMovements') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>2
    THROW 51000,'Handover financial foreign keys were not verified.',1;
SELECT COUNT_BIG(*) AS HandoverCount FROM jahez_v2.CashboxHandovers;
SELECT COUNT_BIG(*) AS HandoverMovementCount FROM jahez_v2.CashboxHandoverMovements;
SELECT COUNT_BIG(*) AS MovementCount,COALESCE(SUM(Amount),0) AS MovementAmount FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerCount,COALESCE(SUM(Amount),0) AS LedgerAmount FROM jahez_v2.AccountingEntries;
SELECT COUNT_BIG(*) AS AccountCount FROM app.PlatformRiderAccounts;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017165534_AddJahezV2AccountantHandovers';
'@

try {
    $handoverConnection.Open()
    if ($handoverConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $handoverConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted database target; refusing accountant handover migration.'
    }
    $handoverBefore = Read-HandoverMigrationData $handoverPreflightSql
    Write-Output ('Verified target: ' + $handoverConnection.DataSource + ' / ' + $handoverConnection.Database)
    Write-Output ('Existing money movements: {0}; ledger entries: {1}; platform accounts: {2}; migration recorded: {3}' -f
        $handoverBefore.Tables[0].Rows[0].MovementCount,$handoverBefore.Tables[1].Rows[0].LedgerCount,
        $handoverBefore.Tables[2].Rows[0].AccountCount,($handoverBefore.Tables[3].Rows.Count -eq 1))
    if (!$Apply) { Write-Output 'Read-only preflight completed; no schema or data changed.'; return }

    $handoverScript = Get-Content -LiteralPath $handoverScriptPath -Raw
    if (([regex]::Matches($handoverScript,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($handoverScript,'(?im)^COMMIT;')).Count -ne 1 -or
        ([regex]::Matches($handoverScript,'(?im)^\s*CREATE TABLE')).Count -ne 2 -or
        $handoverScript -match '(?im)^\s*(DELETE|UPDATE|DROP|ALTER|TRUNCATE)\b' -or
        $handoverScript -notmatch [regex]::Escape($handoverMigration)) {
        throw 'Unexpected accountant handover migration script structure.'
    }
    $handoverBody = $handoverScript -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
    $handoverOutput = Join-Path $handoverWorkspace ('outputs/jahez-accountant-handovers-migration-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $handoverOutput)
    $handoverBefore.WriteXml((Join-Path $handoverOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $handoverScriptPath -Destination (Join-Path $handoverOutput 'migration.sql')
    $handoverHash = (Get-FileHash -LiteralPath $handoverScriptPath -Algorithm SHA256).Hash

    $handoverTransaction = $handoverConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-HandoverMigrationData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'jahez-v2-accountant-handovers-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire accountant handover migration lock.',1;
'@)
    [void](Read-HandoverMigrationData $handoverPreflightSql)
    [void](Read-HandoverMigrationData @'
SELECT * INTO #BeforeMoneyMovements FROM jahez_v2.MoneyMovements WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeLedgerEntries FROM jahez_v2.AccountingEntries WITH(UPDLOCK,HOLDLOCK);
SELECT Id,RowVersion INTO #BeforeAccountVersions FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK);
'@)
    [void](Read-HandoverMigrationData $handoverBody)
    [void](Read-HandoverMigrationData @'
IF EXISTS(SELECT * FROM #BeforeMoneyMovements EXCEPT SELECT * FROM jahez_v2.MoneyMovements)
 OR EXISTS(SELECT * FROM jahez_v2.MoneyMovements EXCEPT SELECT * FROM #BeforeMoneyMovements)
    THROW 51000,'Existing money movements changed.',1;
IF EXISTS(SELECT * FROM #BeforeLedgerEntries EXCEPT SELECT * FROM jahez_v2.AccountingEntries)
 OR EXISTS(SELECT * FROM jahez_v2.AccountingEntries EXCEPT SELECT * FROM #BeforeLedgerEntries)
    THROW 51000,'Existing ledger entries changed.',1;
IF EXISTS(SELECT Id,RowVersion FROM #BeforeAccountVersions EXCEPT SELECT Id,RowVersion FROM app.PlatformRiderAccounts)
 OR EXISTS(SELECT Id,RowVersion FROM app.PlatformRiderAccounts EXCEPT SELECT Id,RowVersion FROM #BeforeAccountVersions)
    THROW 51000,'Existing platform accounts changed.',1;
'@)
    $handoverVerified = Read-HandoverMigrationData $handoverVerifySql
    $handoverVerified.WriteXml((Join-Path $handoverOutput 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $handoverTransaction.Commit()
    $handoverTransaction.Dispose(); $handoverTransaction = $null
    $handoverConnection.Close(); $handoverConnection.Open()
    $handoverPostCommit = Read-HandoverMigrationData $handoverVerifySql
    $handoverPostCommit.WriteXml((Join-Path $handoverOutput 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $handoverReport = [ordered]@{
        Server=$handoverConnection.DataSource; Database=$handoverConnection.Database; VerifiedAtUtc=[DateTime]::UtcNow.ToString('o')
        Migration=$handoverMigration; PreviouslyApplied=($handoverBefore.Tables[3].Rows.Count -eq 1)
        PostCommitVerified=$true; ExistingFinancialRowsPreserved=$true; ExistingPlatformAccountsPreserved=$true
        MoneyMovements=[long]$handoverPostCommit.Tables[2].Rows[0].MovementCount
        LedgerEntries=[long]$handoverPostCommit.Tables[3].Rows[0].LedgerCount
        PlatformAccounts=[long]$handoverPostCommit.Tables[4].Rows[0].AccountCount
        CashboxHandovers=[long]$handoverPostCommit.Tables[0].Rows[0].HandoverCount
        CashboxHandoverMovements=[long]$handoverPostCommit.Tables[1].Rows[0].HandoverMovementCount
        ScriptSha256=$handoverHash; EvidenceDirectory=$handoverOutput
    }
    $handoverReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $handoverOutput 'report.json') -Encoding UTF8
    $handoverReport | ConvertTo-Json -Depth 5
} catch {
    if ($handoverTransaction) { try { $handoverTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($handoverTransaction) { $handoverTransaction.Dispose() }
    $handoverConnection.Dispose()
}
