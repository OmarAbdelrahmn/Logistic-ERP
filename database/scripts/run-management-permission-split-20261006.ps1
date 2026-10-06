param([switch]$Commit, [string]$FixtureSqlPath)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskConnectionString = $env:ConnectionStrings__LogisticsDatabase
if ([string]::IsNullOrWhiteSpace($taskConnectionString)) {
    $taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
    $taskConnectionString = $taskConfig.ConnectionStrings.LogisticsDatabase
}
$taskSql = [System.Data.SqlClient.SqlConnection]::new($taskConnectionString)
$taskTransaction = $null
try {
    $taskSql.Open()
    $taskTransaction = $taskSql.BeginTransaction()
    $taskScripts = @()
    if ($FixtureSqlPath) {
        if ($Commit) { throw 'Test fixtures must only be used with rollback verification.' }
        $taskScripts += $FixtureSqlPath
    }
    $taskScripts += (Join-Path $PSScriptRoot 'management-permissions-application-20261006.sql')
    $taskScripts += (Join-Path $PSScriptRoot 'management-permissions-identity-20261006.sql')
    $taskScripts += (Join-Path $PSScriptRoot 'verify-management-permissions-20261006.sql')
    foreach ($taskScriptPath in $taskScripts) {
        $taskText = Get-Content -LiteralPath $taskScriptPath -Raw
        $taskBatches = [regex]::Split($taskText, '(?im)^\s*GO\s*(?:--[^\r\n]*)?$')
        foreach ($taskBatch in $taskBatches) {
            if ([string]::IsNullOrWhiteSpace($taskBatch)) { continue }
            $taskCmd = $taskSql.CreateCommand()
            $taskCmd.Transaction = $taskTransaction
            $taskCmd.CommandTimeout = 180
            $taskCmd.CommandText = $taskBatch
            $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCmd)
            $taskData = [System.Data.DataSet]::new()
            [void]$taskAdapter.Fill($taskData)
            foreach ($taskTable in $taskData.Tables) {
                if ($taskTable.Columns.Count -eq 1 -and $taskTable.Columns[0].ColumnName -eq "Column1") { continue }
                foreach ($taskRow in $taskTable.Rows) {
                    $taskResult = [ordered]@{}
                    foreach ($taskCol in $taskTable.Columns) {
                        $taskResult[$taskCol.ColumnName] = if ($taskRow[$taskCol] -is [DBNull]) { $null } else { $taskRow[$taskCol] }
                    }
                    [pscustomobject]$taskResult | ConvertTo-Json -Compress
                }
            }
            $taskCmd.Dispose()
        }
    }
    if ($Commit) { $taskTransaction.Commit(); 'Permission migrations committed.' }
    else { $taskTransaction.Rollback(); 'Verification succeeded; all changes rolled back.' }
} catch {
    if ($taskTransaction) { try { $taskTransaction.Rollback() } catch { } }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskSql.Dispose()
}
