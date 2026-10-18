param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$config = Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database; refusing migration.' }
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 90
    $command.CommandText = @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId = '20261010112122_AddChefzAccountsSettlementsAndPerformance')
    THROW 51000, 'Database does not have the expected migration baseline.', 1;
IF EXISTS (SELECT 1 FROM maintenance.OilBarrels WHERE RemainingLiters + RecordedLossLiters > NominalCapacityLiters)
    THROW 51000, 'Existing barrel quantities violate the new accounting constraint.', 1;
IF EXISTS (
    SELECT 1 FROM maintenance.OilBarrels a JOIN maintenance.OilBarrels b
    ON a.InventoryLocationId=b.InventoryLocationId AND a.InventoryItemId=b.InventoryItemId AND a.Id<b.Id
    WHERE a.Status=2 AND b.Status=2 AND a.IsDeleted=0 AND b.IsDeleted=0
      AND (a.AllowedVehicleType IS NULL OR b.AllowedVehicleType IS NULL OR a.AllowedVehicleType=b.AllowedVehicleType))
    THROW 51000, 'Existing open barrels have overlapping scopes.', 1;
SELECT Id,BarrelNumber,InventoryLocationId,InventoryItemId,Status,RemainingLiters,RecordedLossLiters,AllowedVehicleType
FROM maintenance.OilBarrels ORDER BY BarrelNumber;
'@
    $before = [System.Data.DataTable]::new()
    [void]([System.Data.SqlClient.SqlDataAdapter]::new($command)).Fill($before)
    Write-Output ('Verified target database: ' + $connection.Database)
    if (!$Apply) {
        $before | Format-Table -AutoSize
        Write-Output 'Preflight only. Use -Apply to apply the reviewed migration script.'
        return
    }
    $outputDirectory = Join-Path $workspace 'outputs/oil-barrel-writeoffs-20261010'
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    $before | Export-Csv (Join-Path $outputDirectory 'barrels-before.csv') -NoTypeInformation -Encoding UTF8
    $script = Get-Content (Join-Path $workspace 'database/scripts/warehouse-oil-writeoffs-and-shared-barrels.sql') -Raw
    # Keep all EF-generated BEGIN/COMMIT batches on the same connection.
    foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command.CommandText = $batch
        [void]$command.ExecuteNonQuery()
    }
    $command.CommandText = @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels')
    THROW 51000, 'Migration history entry is missing.', 1;
SELECT Id,BarrelNumber,InventoryLocationId,InventoryItemId,Status,RemainingLiters,RecordedLossLiters,AllowedVehicleType,
       AllowBothVehicleTypes,RecordedMissingLiters,OpenCarSlot,OpenMotorcycleSlot
FROM maintenance.OilBarrels ORDER BY BarrelNumber;
'@
    $after = [System.Data.DataTable]::new()
    [void]([System.Data.SqlClient.SqlDataAdapter]::new($command)).Fill($after)
    $after | Export-Csv (Join-Path $outputDirectory 'barrels-after.csv') -NoTypeInformation -Encoding UTF8
    foreach ($old in $before.Rows) {
        $new = @($after.Rows | Where-Object { $_.Id -eq $old.Id })
        if ($new.Count -ne 1) { throw 'Barrel row changed during schema deployment.' }
        foreach ($name in @('Status','RemainingLiters','RecordedLossLiters','AllowedVehicleType')) {
            if ([string]$new[0][$name] -ne [string]$old[$name]) { throw "Unexpected barrel change in $name." }
        }
    }
    $after | Format-Table -AutoSize
    Write-Output 'Migration applied and verified. Barrel quantities and existing scopes are unchanged.'
} finally {
    $connection.Dispose()
}
