$ErrorActionPreference = 'Stop'
$taskConfig = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
    $taskConnection.Open()
    if ($taskConnection.Database -ne 'db67927') { throw 'Unexpected target database' }
    $taskQueries = [ordered]@{
        accounts = @'
SELECT a.Id,a.ExternalAccountId,a.RegisteredEmployeeId,a.OperatingCityId,a.SponsorId,a.Status,a.PaymentModel,a.RowVersion
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE p.Code='JAHEZ' AND a.IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES
'@
        riders = @'
SELECT e.Id AS EmployeeId,e.IqamaNo,e.FullNameAr,e.Status,e.IsEmployee,r.Id AS RiderId,r.IsDeleted AS RiderIsDeleted
FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id
WHERE e.IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES
'@
        assignments = @'
SELECT ra.Id,ra.RiderProfileId,ra.PlatformRiderAccountId,ra.RiderAccountSlot,ra.EffectiveFrom,ra.EffectiveTo,ra.Status,p.Code AS PlatformCode
FROM app.RiderClientAssignments ra JOIN app.PlatformRiderAccounts a ON a.Id=ra.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE ra.IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES
'@
        handovers = 'SELECT * FROM jahez.JahezAccountHandover WHERE IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES'
        contracts = @'
SELECT c.Id,c.Code,c.Status,c.ClientPlatformId FROM app.ClientContracts c
JOIN platform.ClientPlatforms p ON p.Id=c.ClientPlatformId WHERE p.Code='JAHEZ' AND c.IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES
'@
        schema = @'
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS WHERE (TABLE_SCHEMA='jahez' AND TABLE_NAME IN ('JahezAccountHandover','JahezAccountFee','JahezLedgerEntry'))
OR (TABLE_SCHEMA='app' AND TABLE_NAME IN ('RiderClientAssignments','RiderAssignmentEvents','ClientContracts'))
ORDER BY TABLE_SCHEMA,TABLE_NAME,ORDINAL_POSITION FOR JSON PATH,INCLUDE_NULL_VALUES
'@
        platform = "SELECT Id,Code,Status FROM platform.ClientPlatforms WHERE Code='JAHEZ' FOR JSON PATH,INCLUDE_NULL_VALUES"
    }
    foreach ($taskName in $taskQueries.Keys) {
        $taskCommand = $taskConnection.CreateCommand()
        $taskCommand.CommandTimeout = 90
        $taskCommand.CommandText = $taskQueries[$taskName]
        $taskReader = $taskCommand.ExecuteReader()
        $taskBuffer = [System.Text.StringBuilder]::new()
        while ($taskReader.Read()) { [void]$taskBuffer.Append($taskReader.GetString(0)) }
        $taskReader.Dispose()
        $taskPath = Join-Path (Get-Location).Path "tmp/jahez_setup_20261008/live_$taskName.json"
        [System.IO.File]::WriteAllText($taskPath,$taskBuffer.ToString(),[System.Text.UTF8Encoding]::new($false))
        Write-Output ("{0}: {1} rows" -f $taskName,@(ConvertFrom-Json $taskBuffer.ToString()).Count)
    }
} finally { $taskConnection.Dispose() }
