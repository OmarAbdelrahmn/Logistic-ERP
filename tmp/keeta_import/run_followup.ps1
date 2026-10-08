param([switch]$Commit)
$ErrorActionPreference = 'Stop'
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database' }
    $command = $connection.CreateCommand()
    $command.CommandText = @'
CREATE TABLE #Followup (
  SheetRow int NOT NULL, ExternalId nvarchar(150) NOT NULL, OwnerIqama nvarchar(20) NOT NULL,
  PaymentModel int NOT NULL, SettlementMode nvarchar(100) NOT NULL,
  VehicleType nvarchar(100) NOT NULL, VehicleSerial nvarchar(100) NOT NULL,
  SourcePlate nvarchar(100) NOT NULL, VehicleId uniqueidentifier NULL,
  ExpectedVehiclePlate nvarchar(100) NULL, AlreadyAssigned bit NOT NULL,
  PlateConflict bit NOT NULL);
'@
    [void]$command.ExecuteNonQuery()
    $table = [System.Data.DataTable]::new()
    [void]$table.Columns.Add('SheetRow',[int])
    foreach ($name in @('ExternalId','OwnerIqama')) { [void]$table.Columns.Add($name,[string]) }
    [void]$table.Columns.Add('PaymentModel',[int])
    foreach ($name in @('SettlementMode','VehicleType','VehicleSerial','SourcePlate')) {
        [void]$table.Columns.Add($name,[string])
    }
    [void]$table.Columns.Add('VehicleId',[guid])
    [void]$table.Columns.Add('ExpectedVehiclePlate',[string])
    foreach ($name in @('AlreadyAssigned','PlateConflict')) { [void]$table.Columns.Add($name,[bool]) }
    foreach ($source in (Import-Csv 'tmp/keeta_import/followup.csv' -Encoding UTF8)) {
        $vehicleId = if ($source.VehicleId) { [guid]$source.VehicleId } else { [DBNull]::Value }
        $vehiclePlate = if ($source.ExpectedVehiclePlate) { $source.ExpectedVehiclePlate } else { [DBNull]::Value }
        [void]$table.Rows.Add([int]$source.SheetRow,$source.ExternalId,$source.OwnerIqama,
            [int]$source.PaymentModel,$source.SettlementMode,$source.VehicleType,
            $source.VehicleSerial,$source.SourcePlate,$vehicleId,$vehiclePlate,
            [bool]([int]$source.AlreadyAssigned),[bool]([int]$source.PlateConflict))
    }
    $bulk = [System.Data.SqlClient.SqlBulkCopy]::new($connection)
    try {
        $bulk.DestinationTableName = '#Followup'
        $bulk.WriteToServer($table)
    }
    finally { $bulk.Dispose() }
    $command.CommandText = Get-Content 'tmp/keeta_import/followup.sql' -Raw
    $command.CommandTimeout = 300
    [void]$command.Parameters.Add('@DryRun',[System.Data.SqlDbType]::Bit)
    $command.Parameters['@DryRun'].Value = if ($Commit) { 0 } else { 1 }
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $dataset = [System.Data.DataSet]::new()
    [void]$adapter.Fill($dataset)
    foreach ($result in $dataset.Tables) { $result | Format-Table -AutoSize | Out-String | Write-Output }
    if ($Commit) { Write-Output 'COMMITTED Keeta follow-up' }
    else { Write-Output 'DRY RUN rolled back all changes' }
}
finally { $connection.Dispose() }
