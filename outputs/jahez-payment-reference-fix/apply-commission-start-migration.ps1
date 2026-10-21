$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskBuilder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskBuilder['Connect Timeout'] = 15
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskBuilder.ConnectionString)
function Invoke-TaskScalar([string]$sql) {
    $taskCommand = $taskConnection.CreateCommand()
    try {
        $taskCommand.CommandTimeout = 30
        $taskCommand.CommandText = $sql
        return $taskCommand.ExecuteScalar()
    } finally { $taskCommand.Dispose() }
}
$taskSnapshotSql = @'
SELECT Id, HandoverId, Mode, Status, PaymentStatus, ThroughDate, EffectiveAtUtc,
Net, Commission, CalculatedCommission, Remaining, Fund, Purpose, TransferToRider,
PaymentReference, Reason, CalculationJson, ApprovedRequestId, ExecutedAtUtc, ExecutedByUserId,
(SELECT COUNT(*) FROM jahez_v2.MoneyMovements m WHERE m.OperationId=o.Id) AS MovementCount
FROM jahez_v2.Operations o WHERE Id='01a14e30-a269-7c2b-8eeb-39775fa90150'
FOR JSON PATH, INCLUDE_NULL_VALUES;
'@
try {
    $taskConnection.Open()
    if ($taskConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $taskConnection.Database -ne 'db67927') {
        throw 'Unexpected database target.'
    }
    $taskBefore = Invoke-TaskScalar $taskSnapshotSql
    if ($taskBefore -eq '[]') { throw 'Expected settlement was not found.' }
    $taskPrevious = Invoke-TaskScalar "SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261018111059_AllowJahezPetroAppSettlementReceipts'"
    if ($taskPrevious -ne 1) { throw 'Expected preceding migration is missing.' }
    $taskSql = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'commission-start-migration.sql') -Raw
    $taskSql = [regex]::Replace($taskSql, '(?im)^\s*GO\s*$', '')
    $taskCommand = $taskConnection.CreateCommand()
    try {
        $taskCommand.CommandTimeout = 90
        $taskCommand.CommandText = "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;`n" + $taskSql
        [void]$taskCommand.ExecuteNonQuery()
    } catch {
        try { [void](Invoke-TaskScalar 'IF @@TRANCOUNT > 0 ROLLBACK; SELECT 1;') } catch { }
        throw
    } finally { $taskCommand.Dispose() }
    $taskAfter = Invoke-TaskScalar $taskSnapshotSql
    if ($taskBefore -ne $taskAfter) { throw 'Settlement changed during verification; inspect concurrent activity.' }
    $taskVerified = Invoke-TaskScalar @'
SELECT CASE WHEN COL_LENGTH('jahez_v2.Operations','CommissionStartDate') IS NOT NULL
AND EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261018144507_AddJahezSettlementCommissionStart')
AND (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN('CK_JahezOperation_CommissionStart','CK_JahezMoney_Amount','CK_JahezMoney_Routing') AND is_disabled=0 AND is_not_trusted=0)=3
THEN 1 ELSE 0 END;
'@
    if ($taskVerified -ne 1) { throw 'Migration schema verification failed.' }
    $taskReport = [ordered]@{
        verifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        migration = '20261018144507_AddJahezSettlementCommissionStart'
        schemaVerified = $true
        settlementUnchanged = $true
        settlementExecuted = $false
        account = '476835'
    }
    $taskReport | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'migration-verification.json') -Encoding utf8
    $taskReport | ConvertTo-Json -Compress
} finally { $taskConnection.Dispose() }
