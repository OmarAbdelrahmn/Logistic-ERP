$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$config = Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
$transaction = $null
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database; refusing verification.' }
    $transaction = $connection.BeginTransaction()
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandTimeout = 90
    # The migration's inner COMMIT leaves this outer transaction open for rollback.
    $script = Get-Content (Join-Path $workspace 'database/scripts/warehouse-oil-writeoffs-and-shared-barrels.sql') -Raw
    foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command.CommandText = $batch
        [void]$command.ExecuteNonQuery()
    }
    $command.CommandText = Get-Content (Join-Path $workspace 'database/scripts/verify-warehouse-oil-writeoffs-rollback.sql') -Raw
    $result = [System.Data.DataTable]::new()
    [void]([System.Data.SqlClient.SqlDataAdapter]::new($command)).Fill($result)
    $result | Format-Table -AutoSize
    $transaction.Rollback()
    $transaction.Dispose()
    $transaction = $null
    Write-Output 'Rolled back all verification writes and migration rehearsal.'
} finally {
    if ($transaction) {
        try { $transaction.Rollback() } catch { }
        $transaction.Dispose()
    }
    $connection.Dispose()
}
