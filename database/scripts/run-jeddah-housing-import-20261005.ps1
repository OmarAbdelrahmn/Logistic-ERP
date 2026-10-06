param([switch]$Commit)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
    $taskConnection.Open()
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.CommandTimeout = 120
    $taskCommand.CommandText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'import-jeddah-housing-20261005.sql') -Raw
    [void]$taskCommand.Parameters.Add('@Commit', [System.Data.SqlDbType]::Bit)
    $taskCommand.Parameters['@Commit'].Value = $Commit.IsPresent
    $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
    $taskData = [System.Data.DataSet]::new()
    [void]$taskAdapter.Fill($taskData)
    $taskResults = @()
    foreach ($taskTable in $taskData.Tables) {
        $taskRows = @(foreach ($taskRow in $taskTable.Rows) {
            $taskObject = [ordered]@{}
            foreach ($taskColumn in $taskTable.Columns) {
                $taskObject[$taskColumn.ColumnName] = if ($taskRow[$taskColumn] -is [DBNull]) { $null } else { $taskRow[$taskColumn] }
            }
            [pscustomobject]$taskObject
        })
        $taskResults += ,$taskRows
    }
    ConvertTo-Json -InputObject $taskResults -Depth 8 -Compress
} finally { $taskConnection.Dispose() }
