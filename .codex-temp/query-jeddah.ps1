param([string]$SqlPath, [string]$OutputPath, [switch]$Import, [switch]$Commit)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskSql = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
    $taskSql.Open()
    $taskCmd = $taskSql.CreateCommand()
    $taskCmd.CommandTimeout = 120
    $taskCmd.CommandText = Get-Content -LiteralPath $SqlPath -Raw
    if ($Import) {
        [void]$taskCmd.Parameters.Add('@Commit', [System.Data.SqlDbType]::Bit)
        $taskCmd.Parameters['@Commit'].Value = $Commit.IsPresent
    }
    $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCmd)
    $taskData = [System.Data.DataSet]::new()
    [void]$taskAdapter.Fill($taskData)
    $taskResults = @()
    foreach ($taskTable in $taskData.Tables) {
        $taskRows = @(foreach ($taskRow in $taskTable.Rows) {
            $taskObj = [ordered]@{}
            foreach ($taskCol in $taskTable.Columns) {
                $taskObj[$taskCol.ColumnName] = if ($taskRow[$taskCol] -is [DBNull]) { $null } else { $taskRow[$taskCol] }
            }
            [pscustomobject]$taskObj
        })
        $taskResults += ,$taskRows
    }
    $taskJson = ConvertTo-Json -InputObject $taskResults -Depth 8 -Compress
    if ($OutputPath) { Set-Content -LiteralPath $OutputPath -Value $taskJson -Encoding utf8 }
    $taskJson
} finally { $taskSql.Dispose() }
