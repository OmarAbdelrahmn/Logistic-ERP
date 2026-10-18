param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$cfg = Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
$tx = $null
$correlation = 'vehicle-odometer-reset-20261013-five-requested-plates'
$targets = @(
    @{ Plate = 'ح ب 1031'; Normalized = 'حب1031' },
    @{ Plate = 'ك ع 851'; Normalized = 'كع851' },
    @{ Plate = 'ح ب 1034'; Normalized = 'حب1034' },
    @{ Plate = 'د ب 9966'; Normalized = 'دب9966' },
    @{ Plate = 'ه ا 3048'; Normalized = 'ها3048' }
) | ConvertTo-Json -Compress
function Read-Data([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.Transaction = $tx
    $cmd.CommandTimeout = 60
    $cmd.CommandText = $sql
    [void]$cmd.Parameters.Add('@Targets', [System.Data.SqlDbType]::NVarChar, -1)
    $cmd.Parameters['@Targets'].Value = $targets
    [void]$cmd.Parameters.Add('@Correlation', [System.Data.SqlDbType]::NVarChar, 200)
    $cmd.Parameters['@Correlation'].Value = $correlation
    try {
        $ds = [System.Data.DataSet]::new()
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($cmd)
        try { [void]$adapter.Fill($ds) } finally { $adapter.Dispose() }
        return ,$ds
    } finally { $cmd.Dispose() }
}
try {
    $conn.Open()
    if ($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected database; refusing changes.'
    }
    $suffix = if ($Apply) { '-applied' } else { '-review' }
    $out = Join-Path $workspace ('outputs/vehicle-odometer-reset-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + $suffix)
    [void](New-Item -ItemType Directory -Path $out -Force)
    $tx = $conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $before = Read-Data @'
SET XACT_ABORT ON;
SELECT t.Plate, t.Normalized INTO #Targets
FROM OPENJSON(@Targets) WITH (Plate nvarchar(32) '$.Plate', Normalized nvarchar(32) '$.Normalized') t;
IF (SELECT COUNT(*) FROM #Targets) <> 5 THROW 51000, 'Expected exactly five plates.', 1;
SELECT v.* INTO #BeforeVehicles FROM app.Vehicles v WITH (UPDLOCK, HOLDLOCK)
JOIN #Targets t ON t.Normalized = v.NormalizedPlateNumberAr AND t.Plate = v.PlateNumberAr
WHERE v.IsDeleted = 0;
IF (SELECT COUNT(*) FROM #BeforeVehicles) <> 5
 OR EXISTS (SELECT t.Normalized FROM #Targets t LEFT JOIN #BeforeVehicles v ON v.NormalizedPlateNumberAr=t.Normalized
 GROUP BY t.Normalized HAVING COUNT(v.Id)<>1)
 THROW 51000, 'Every requested full plate must match exactly one active vehicle.', 1;
IF EXISTS (SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=@Correlation)
 THROW 51000, 'This reset already has audit records; refusing a repeat.', 1;
SELECT * FROM #BeforeVehicles;
SELECT v.PlateNumberAr,
 (SELECT COUNT(*) FROM app.VehicleOdometerReadings h WHERE h.VehicleId=v.Id) OdometerHistoryRows,
 (SELECT COUNT(*) FROM app.VehicleDailyDistances h WHERE h.VehicleId=v.Id) DailyDistanceRows
FROM #BeforeVehicles v;
SELECT * FROM app.DatasetVersions WITH (UPDLOCK, HOLDLOCK) WHERE ModuleKey='fleet';
'@
    $before.Tables[0].TableName = 'Vehicles'
    $before.Tables[1].TableName = 'HistoryCounts'
    $before.Tables[2].TableName = 'DatasetVersion'
    $before.WriteXml((Join-Path $out 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $before.Tables[0] | Select-Object PlateNumberAr, CurrentOdometer, TrackedDistanceKm | Format-Table -AutoSize
    $after = Read-Data @'
SET XACT_ABORT ON;
SELECT v.* INTO #BeforeVehicles FROM app.Vehicles v WITH (UPDLOCK, HOLDLOCK)
JOIN OPENJSON(@Targets) WITH (Plate nvarchar(32) '$.Plate', Normalized nvarchar(32) '$.Normalized') t
 ON t.Normalized=v.NormalizedPlateNumberAr AND t.Plate=v.PlateNumberAr
WHERE v.IsDeleted=0;
IF (SELECT COUNT(*) FROM #BeforeVehicles)<>5 THROW 51000, 'Incorrect vehicle count.', 1;
DECLARE @Now datetimeoffset=SYSUTCDATETIME();
UPDATE v SET CurrentOdometer=0, TrackedDistanceKm=0, LastOdometerAtUtc=@Now,
 UpdatedAtUtc=@Now, UpdatedByUserId=NULL
FROM app.Vehicles v JOIN #BeforeVehicles b ON b.Id=v.Id;
IF @@ROWCOUNT<>5 THROW 51000, 'Incorrect update count.', 1;
INSERT audit.AuditEntries
 (Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,
 Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','VehicleOdometerReset','fleet','Vehicle',v.Id,@Now,@Correlation,
 N'User explicitly requested zero current odometer readings for these five full Arabic plates.',
 (SELECT b.PlateNumberAr,b.CurrentOdometer,b.TrackedDistanceKm,b.LastOdometerAtUtc FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 (SELECT v.PlateNumberAr,v.CurrentOdometer,v.TrackedDistanceKm,v.LastOdometerAtUtc FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 'CodexAuthorizedMaintenance',1,@Now,NULL
FROM app.Vehicles v JOIN #BeforeVehicles b ON b.Id=v.Id;
UPDATE app.DatasetVersions SET Version=Version+1, LastChangedAtUtc=@Now, UpdatedAtUtc=@Now, UpdatedByUserId=NULL
WHERE ModuleKey='fleet';
IF @@ROWCOUNT<>1 THROW 51000, 'Expected one fleet dataset version.', 1;
SELECT v.* FROM app.Vehicles v JOIN #BeforeVehicles b ON b.Id=v.Id;
SELECT v.PlateNumberAr,
 (SELECT COUNT(*) FROM app.VehicleOdometerReadings h WHERE h.VehicleId=v.Id) OdometerHistoryRows,
 (SELECT COUNT(*) FROM app.VehicleDailyDistances h WHERE h.VehicleId=v.Id) DailyDistanceRows
FROM #BeforeVehicles v;
SELECT COUNT(*) AuditRows FROM audit.AuditEntries WHERE CorrelationId=@Correlation AND Action='VehicleOdometerReset';
'@
    $after.Tables[0].TableName = 'Vehicles'
    $after.Tables[1].TableName = 'HistoryCounts'
    $after.Tables[2].TableName = 'AuditCount'
    $after.WriteXml((Join-Path $out 'after.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    if ($after.Tables[0].Rows.Count -ne 5 -or $after.Tables[2].Rows[0].AuditRows -ne 5) { throw 'Verification counts failed.' }
    $allowed = @('CurrentOdometer','TrackedDistanceKm','LastOdometerAtUtc','UpdatedAtUtc','UpdatedByUserId','RowVersion')
    foreach ($old in $before.Tables[0].Rows) {
        $new = @($after.Tables[0].Rows | Where-Object { $_.Id -eq $old.Id })
        if ($new.Count -ne 1 -or $new[0].CurrentOdometer -ne 0 -or $new[0].TrackedDistanceKm -ne 0) { throw 'Vehicle reset verification failed.' }
        foreach ($column in $before.Tables[0].Columns) {
            if ($column.ColumnName -notin $allowed -and [string]$old[$column.ColumnName] -cne [string]$new[0][$column.ColumnName]) {
                throw ('Unexpected vehicle field change: ' + $column.ColumnName)
            }
        }
    }
    foreach ($old in $before.Tables[1].Rows) {
        $new = @($after.Tables[1].Rows | Where-Object { $_.PlateNumberAr -eq $old.PlateNumberAr })
        if ($new.Count -ne 1 -or $new[0].OdometerHistoryRows -ne $old.OdometerHistoryRows -or $new[0].DailyDistanceRows -ne $old.DailyDistanceRows) {
            throw 'History count changed unexpectedly.'
        }
    }
    if (!$Apply) {
        $tx.Rollback(); $tx.Dispose(); $tx=$null
        'DRY RUN VERIFIED AND ROLLED BACK'
    } else {
        $tx.Commit(); $tx.Dispose(); $tx=$null
        'COMMITTED'
        $verified = Read-Data @'
SELECT v.Id,v.PlateNumberAr,v.CurrentOdometer,v.TrackedDistanceKm,v.LastOdometerAtUtc
FROM app.Vehicles v JOIN OPENJSON(@Targets) WITH (Plate nvarchar(32) '$.Plate', Normalized nvarchar(32) '$.Normalized') t
 ON t.Normalized=v.NormalizedPlateNumberAr AND t.Plate=v.PlateNumberAr
WHERE v.IsDeleted=0;
SELECT COUNT(*) AuditRows FROM audit.AuditEntries WHERE CorrelationId=@Correlation AND Action='VehicleOdometerReset';
'@
        $verified.WriteXml((Join-Path $out 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($verified.Tables[0].Rows.Count -ne 5 -or $verified.Tables[1].Rows[0].AuditRows -ne 5 -or @($verified.Tables[0].Rows | Where-Object { $_.CurrentOdometer -ne 0 -or $_.TrackedDistanceKm -ne 0 }).Count) {
            throw 'Post-commit verification failed; inspect the saved audit and backup.'
        }
        $verified.Tables[0] | Select-Object PlateNumberAr,CurrentOdometer,TrackedDistanceKm | Format-Table -AutoSize
        'POST-COMMIT VERIFICATION PASSED'
    }
    Write-Output ('Output: ' + $out)
} catch {
    if ($tx) { $tx.Rollback() }
    throw
} finally {
    if ($tx) { $tx.Dispose() }
    $conn.Dispose()
}
