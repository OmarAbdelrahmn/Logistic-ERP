$ErrorActionPreference = 'Stop'
$cfg = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927') { throw 'Unexpected database' }
    $sql = @'
SELECT Id, IqamaNo, FullNameAr, FullNameEn, SponsorId, OperatingCityId, Status, IsDeleted
FROM app.Employees WHERE IqamaNo IS NOT NULL;
SELECT Id, SerialNumber, PlateNumberAr, PlateNumberEn, SponsorId, OperatingCityId,
       VehicleType, CurrentOperationalStatus, IsDeleted
FROM app.Vehicles;
SELECT Id, Code, ExternalAccountId, RegisteredEmployeeId, OperatingCityId, SponsorId,
       PaymentModel, Status, IsDeleted
FROM app.PlatformRiderAccounts
WHERE ClientPlatformId = (SELECT Id FROM platform.ClientPlatforms WHERE Code = 'KEETA');
SELECT a.Id, a.VehicleId, a.PlatformRiderAccountId, a.Status, a.EndedAtUtc, a.IsDeleted
FROM app.VehiclePlatformAccountAssignments a;
SELECT Id, RegistryNameAr, RegistryNameEn, Status, IsDeleted FROM app.Sponsors;
SELECT oc.Id, gc.Code, gc.NameEn, gc.NameAr FROM app.OperatingCities oc
JOIN platform.GlobalCities gc ON gc.Id = oc.GlobalCityId WHERE oc.IsDeleted = 0;
'@
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $dataset = [System.Data.DataSet]::new()
    [void]$adapter.Fill($dataset)
    $names = @('employees','vehicles','keeta_accounts','vehicle_account_assignments','sponsors','cities')
    for ($index=0; $index -lt $names.Count; $index++) {
        $table = $dataset.Tables[$index]
        $path = Join-Path 'tmp/keeta_import' ($names[$index] + '.csv')
        $items = foreach ($row in $table.Rows) {
            $item = [ordered]@{}
            foreach ($column in $table.Columns) { $item[$column.ColumnName] = $row[$column.ColumnName] }
            [pscustomobject]$item
        }
        if ($items) { $items | Export-Csv -LiteralPath $path -NoTypeInformation -Encoding UTF8 }
        Write-Output ("{0}: {1}" -f $names[$index], $table.Rows.Count)
    }
}
finally { $connection.Dispose() }
