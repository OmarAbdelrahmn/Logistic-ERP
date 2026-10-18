param([string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$sqlPath = Join-Path $workspace 'database/scripts/verify-chefz-account-updates.sql'
$connection = $null
try {
    $config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
    $configuredConnection = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
    $connection = [System.Data.SqlClient.SqlConnection]::new($configuredConnection)
    $connection.Open()
    if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected target; refusing hosted Chefz verification.'
    }
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 90
    $command.CommandText = Get-Content -LiteralPath $sqlPath -Raw
    $data = [System.Data.DataSet]::new()
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try { [void]$adapter.Fill($data) } finally { $adapter.Dispose(); $command.Dispose() }
    $names = @('CompatibilityChecks','AssignmentIndexes','AssignmentConstraints','ChefzMigrations','IdentityMigrations',
        'Platforms','AccountStatuses','RiderAccountCounts','DataHealth','FinancialTotals','PerformanceTotals',
        'Permissions','SystemAdminGrants','VerifiedTime')
    if ($data.Tables.Count -ne $names.Count) { throw 'Unexpected verification result shape.' }
    if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
        $OutputDirectory = Join-Path $workspace ('outputs/chefz-account-updates-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
    }
    [void](New-Item -ItemType Directory -Path $OutputDirectory -Force)
    $tables = [ordered]@{}
    for ($index = 0; $index -lt $names.Count; $index++) {
        $table = $data.Tables[$index]
        $table.TableName = $names[$index]
        $tables[$names[$index]] = @($table.Rows | ForEach-Object {
            $record = [ordered]@{}
            foreach ($column in $table.Columns) {
                $value = $_[$column.ColumnName]
                $record[$column.ColumnName] = if ($value -is [System.DBNull]) { $null } else { $value }
            }
            [pscustomobject]$record
        })
    }
    $failed = @($tables.CompatibilityChecks | Where-Object Passed -ne 1)
    $report = [ordered]@{
        Server = $connection.DataSource
        Database = $connection.Database
        VerifiedAtUtc = [DateTime]::SpecifyKind($data.Tables[13].Rows[0].VerifiedAtUtc, [DateTimeKind]::Utc).ToString('o')
        VerificationOnly = $true
        DatabaseWritesPerformed = $false
        NewChefzMigrationRequired = $false
        Compatible = ($failed.Count -eq 0)
        SqlSha256 = (Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash
        Tables = $tables
    }
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verification.json') -Encoding utf8
    $data.WriteXml((Join-Path $OutputDirectory 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $OutputDirectory 'verification.sql')
    Write-Output ('Hosted target: ' + $connection.DataSource + ' / ' + $connection.Database)
    Write-Output ('Compatibility checks: ' + ($tables.CompatibilityChecks.Count - $failed.Count) + '/' + $tables.CompatibilityChecks.Count)
    $data.Tables[5] | Format-Table -AutoSize
    $data.Tables[6] | Format-Table -AutoSize
    $data.Tables[8] | Format-Table -AutoSize
    Write-Output ('Evidence: ' + $OutputDirectory)
    if ($failed.Count -gt 0) { throw ('Missing prerequisites: ' + (($failed | ForEach-Object CheckName) -join ', ')) }
    Write-Output 'HOSTED CHEFZ SCHEMA COMPATIBLE; NO DATABASE WRITES REQUIRED.'
} catch [System.Data.SqlClient.SqlException] {
    throw ('Hosted Chefz verification SQL failure; error number ' + $_.Exception.Number + ', line ' + $_.Exception.LineNumber + '. Connection credentials were not logged.')
} finally {
    if ($connection) { $connection.Dispose() }
}
