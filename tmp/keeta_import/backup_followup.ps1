$ErrorActionPreference = 'Stop'
$outputDirectory = 'outputs/keeta-followup-20261008-backup'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database' }
    $queries = @{
        accounts = @'
SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p
ON p.Id = a.ClientPlatformId WHERE p.Code = 'KEETA' FOR JSON PATH, INCLUDE_NULL_VALUES
'@
        assignments = @'
SELECT x.* FROM app.VehiclePlatformAccountAssignments x
JOIN app.PlatformRiderAccounts a ON a.Id = x.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id = a.ClientPlatformId
WHERE p.Code = 'KEETA' FOR JSON PATH, INCLUDE_NULL_VALUES
'@
    }
    foreach ($name in @('accounts','assignments')) {
        $command = $connection.CreateCommand()
        $command.CommandText = $queries[$name]
        $reader = $command.ExecuteReader()
        $builder = [System.Text.StringBuilder]::new()
        try { while ($reader.Read()) { [void]$builder.Append($reader.GetString(0)) } }
        finally { $reader.Dispose() }
        $path = Join-Path $outputDirectory ($name + '.json')
        [System.IO.File]::WriteAllText((Join-Path (Get-Location).Path $path),$builder.ToString(),[System.Text.UTF8Encoding]::new($false))
        Write-Output ("{0}: {1}" -f $name,@(ConvertFrom-Json $builder.ToString()).Count)
    }
    Get-FileHash -Algorithm SHA256 -LiteralPath 'C:\Users\omarf\Downloads\Keeta.xlsx' |
        Select-Object -ExpandProperty Hash | Set-Content (Join-Path $outputDirectory 'source-sha256.txt')
}
finally { $connection.Dispose() }
