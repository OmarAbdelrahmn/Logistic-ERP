param([switch]$Commit, [switch]$IncludeUnavailableVehicles)
$ErrorActionPreference = 'Stop'
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database' }
    $command = $connection.CreateCommand()
    $command.CommandText = @'
CREATE TABLE #KeetaImport (
  SheetRow int NOT NULL, ExternalId nvarchar(150) NOT NULL, OwnerIqama nvarchar(20) NOT NULL,
  OwnerNameEn nvarchar(150) NOT NULL, OperatingCityId uniqueidentifier NOT NULL,
  SponsorId uniqueidentifier NOT NULL, ReviewStatus nvarchar(100) NOT NULL,
  ServiceStatus nvarchar(100) NOT NULL, SettlementMode nvarchar(100) NOT NULL,
  VehicleType nvarchar(100) NOT NULL, VehicleSerial nvarchar(100) NOT NULL,
  VehiclePlate nvarchar(100) NOT NULL, VehicleId uniqueidentifier NULL,
  ExpectedVehiclePlate nvarchar(100) NULL, AssignmentHoldReason nvarchar(100) NOT NULL);
'@
    [void]$command.ExecuteNonQuery()
    $table = [System.Data.DataTable]::new()
    [void]$table.Columns.Add('SheetRow', [int])
    foreach ($name in @('ExternalId','OwnerIqama','OwnerNameEn')) { [void]$table.Columns.Add($name,[string]) }
    foreach ($name in @('OperatingCityId','SponsorId')) { [void]$table.Columns.Add($name,[guid]) }
    foreach ($name in @('ReviewStatus','ServiceStatus','SettlementMode','VehicleType','VehicleSerial','VehiclePlate')) {
        [void]$table.Columns.Add($name,[string])
    }
    [void]$table.Columns.Add('VehicleId',[guid])
    foreach ($name in @('ExpectedVehiclePlate','AssignmentHoldReason')) { [void]$table.Columns.Add($name,[string]) }
    foreach ($source in (Import-Csv 'tmp/keeta_import/staging.csv' -Encoding UTF8)) {
        $vehicleId = if ($source.VehicleId) { [guid]$source.VehicleId } else { [DBNull]::Value }
        $vehiclePlate = if ($source.ExpectedVehiclePlate) { $source.ExpectedVehiclePlate } else { [DBNull]::Value }
        [void]$table.Rows.Add([int]$source.SheetRow,$source.ExternalId,$source.OwnerIqama,
            $source.OwnerNameEn,[guid]$source.OperatingCityId,[guid]$source.SponsorId,
            $source.ReviewStatus,$source.ServiceStatus,$source.SettlementMode,
            $source.VehicleType,$source.VehicleSerial,$source.VehiclePlate,
            $vehicleId,$vehiclePlate,$source.AssignmentHoldReason)
    }
    $bulk = [System.Data.SqlClient.SqlBulkCopy]::new($connection)
    try {
        $bulk.DestinationTableName = '#KeetaImport'
        $bulk.WriteToServer($table)
    }
    finally { $bulk.Dispose() }
    $command.CommandText = Get-Content 'tmp/keeta_import/import.sql' -Raw
    $command.CommandTimeout = 300
    [void]$command.Parameters.Add('@DryRun',[System.Data.SqlDbType]::Bit)
    [void]$command.Parameters.Add('@IncludeUnavailableVehicles',[System.Data.SqlDbType]::Bit)
    $command.Parameters['@DryRun'].Value = if ($Commit) { 0 } else { 1 }
    $command.Parameters['@IncludeUnavailableVehicles'].Value = if ($IncludeUnavailableVehicles) { 1 } else { 0 }
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $dataset = [System.Data.DataSet]::new()
    [void]$adapter.Fill($dataset)
    foreach ($result in $dataset.Tables) { $result | Format-Table -AutoSize | Out-String | Write-Output }
    if ($Commit) { Write-Output 'COMMITTED Keeta import' }
    else { Write-Output 'DRY RUN rolled back all changes' }
}
finally { $connection.Dispose() }
