param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$migrationId = '20261011122817_AddJahezDebtFreezeAndClearance'
$config = Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)

function Read-DataSet([string]$Sql) {
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 90
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
        throw 'Unexpected hosted database; refusing migration.'
    }
    $preflight = Read-DataSet @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261003153822_PreserveJahezFinancialEvidence')
    THROW 51000, 'The Jahez migration baseline is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261011122817_AddJahezDebtFreezeAndClearance')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez.JahezAccountHandover')
       AND name IN ('DebtStatus','FrozenAtUtc','AccountClearedAtUtc','DebtPaidAtUtc','ExternalClearanceReference'))
    THROW 51000, 'Partial Jahez schema detected; reconcile before applying.', 1;
SELECT COUNT_BIG(*) HandoverCount FROM jahez.JahezAccountHandover;
SELECT COUNT_BIG(*) LedgerCount, COALESCE(SUM(Amount),0) SignedLedgerTotal FROM jahez.JahezLedgerEntry;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261011122817_AddJahezDebtFreezeAndClearance';
'@
    Write-Output ('Verified hosted target: ' + $connection.DataSource + ' / ' + $connection.Database)
    if (!$Apply) {
        $preflight.Tables[0] | Format-Table -AutoSize
        $preflight.Tables[1] | Format-Table -AutoSize
        Write-Output 'Preflight only. -Apply executes the authorized Jahez schema migration.'
        return
    }

    $runStamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $outputDirectory = Join-Path $workspace ('outputs/jahez-debt-migration-' + $runStamp)
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    $backup = Read-DataSet @'
SELECT * FROM jahez.JahezAccountHandover ORDER BY Id;
SELECT * FROM jahez.JahezLedgerEntry ORDER BY Id;
SELECT * FROM jahez.JahezRiderSettlement ORDER BY Id;
SELECT * FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
'@
    $names = @('handovers-before.csv','ledger-before.csv','settlements-before.csv','migration-history-before.csv')
    for ($i=0; $i -lt $names.Count; $i++) {
        $backup.Tables[$i] | Export-Csv (Join-Path $outputDirectory $names[$i]) -NoTypeInformation -Encoding UTF8
    }

    $columnData = Read-DataSet @'
SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez.JahezAccountHandover')
AND name NOT IN ('RowVersion','DebtStatus','FrozenAtUtc','AccountClearedAtUtc','DebtPaidAtUtc','ExternalClearanceReference') ORDER BY column_id;
'@
    $protectedColumns = ($columnData.Tables[0].Rows | ForEach-Object { '[' + $_.name.Replace(']',']]') + ']' }) -join ','
    $scriptPath = Join-Path $workspace 'database/scripts/jahez-debt-freeze-and-clearance.sql'
    $script = Get-Content $scriptPath -Raw
    $script = [regex]::Replace($script, '(?im)^\s*GO\s*\r?$', '')
    if ($script -notmatch '^BEGIN TRANSACTION;' -or $script -notmatch 'COMMIT;\s*$') {
        throw 'Unexpected migration script transaction structure.'
    }
    $body = $script -replace '^BEGIN TRANSACTION;','' -replace 'COMMIT;\s*$',''
    $sql = @"
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
BEGIN TRY
    BEGIN TRANSACTION;
    SELECT $protectedColumns INTO #JahezHandoverBefore FROM jahez.JahezAccountHandover WITH (TABLOCK,HOLDLOCK);
    SELECT * INTO #JahezLedgerBefore FROM jahez.JahezLedgerEntry WITH (TABLOCK,HOLDLOCK);
    $body
    IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId')
        THROW 51000, 'Migration history was not recorded.', 1;
    IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez.JahezAccountHandover')
        AND name IN ('DebtStatus','FrozenAtUtc','AccountClearedAtUtc','DebtPaidAtUtc','ExternalClearanceReference')) <> 5
        THROW 51000, 'New Jahez columns are incomplete.', 1;
    IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN ('CK_JahezLedger_StatusAmount','CK_JahezHandover_DebtStatus','CK_JahezHandover_FrozenRange')
        AND is_disabled=0 AND is_not_trusted=0) <> 3
        THROW 51000, 'New Jahez constraints are incomplete or untrusted.', 1;
    IF EXISTS (SELECT $protectedColumns FROM #JahezHandoverBefore EXCEPT SELECT $protectedColumns FROM jahez.JahezAccountHandover)
       OR EXISTS (SELECT $protectedColumns FROM jahez.JahezAccountHandover EXCEPT SELECT $protectedColumns FROM #JahezHandoverBefore)
        THROW 51000, 'Protected original handover values changed.', 1;
    IF EXISTS (SELECT * FROM #JahezLedgerBefore EXCEPT SELECT * FROM jahez.JahezLedgerEntry)
       OR EXISTS (SELECT * FROM jahez.JahezLedgerEntry EXCEPT SELECT * FROM #JahezLedgerBefore)
        THROW 51000, 'Existing financial ledger entries changed.', 1;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='$migrationId';
SELECT name,TYPE_NAME(user_type_id) SqlType,max_length,is_nullable FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez.JahezAccountHandover')
    AND name IN ('DebtStatus','FrozenAtUtc','AccountClearedAtUtc','DebtPaidAtUtc','ExternalClearanceReference') ORDER BY name;
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE name IN ('CK_JahezLedger_StatusAmount','CK_JahezHandover_DebtStatus','CK_JahezHandover_FrozenRange');
EXEC(N'SELECT DebtStatus,COUNT_BIG(*) PeriodCount FROM jahez.JahezAccountHandover GROUP BY DebtStatus ORDER BY DebtStatus;');
SELECT COUNT_BIG(*) LedgerCount,COALESCE(SUM(Amount),0) SignedLedgerTotal FROM jahez.JahezLedgerEntry;
SELECT SYSUTCDATETIME() VerifiedAtUtc;
"@
    $after = Read-DataSet $sql
    $results = @()
    for ($i=0; $i -lt $after.Tables.Count; $i++) {
        $rows = @()
        foreach ($row in $after.Tables[$i].Rows) {
            $item = [ordered]@{}
            foreach ($column in $after.Tables[$i].Columns) {
                $item[$column.ColumnName] = if ($row.IsNull($column)) { $null } else { $row[$column] }
            }
            $rows += [pscustomobject]$item
        }
        $results += [pscustomobject]@{ ResultSet=$i; Rows=$rows }
    }
    $report = [pscustomobject]@{
        Database=$connection.Database
        Server=$connection.DataSource
        Migration=$migrationId
        ScriptSha256=(Get-FileHash $scriptPath -Algorithm SHA256).Hash
        OriginalHandoverValuesPreserved=$true
        FinancialLedgerPreserved=$true
        Verification=$results
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $outputDirectory 'verification.json') -Encoding UTF8
    $report | ConvertTo-Json -Depth 8
    Write-Output ('Backup and verification: ' + $outputDirectory)
} finally { $connection.Dispose() }
