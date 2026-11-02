param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$settings = Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $settings.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($connectionText)
$script:transaction = $null
$migration = '20261031092701_AddDailyOdometerCorrections'
function Query([string]$sql, [int]$timeout = 120) {
    $command = $connection.CreateCommand(); $command.CommandText = $sql; $command.CommandTimeout = $timeout; $command.Transaction = $script:transaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data } finally { $adapter.Dispose(); $command.Dispose() }
}
$verification = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261031092701_AddDailyOdometerCorrections') THROW 51000,'Migration history missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns c JOIN sys.default_constraints d ON d.object_id=c.default_object_id WHERE c.object_id=OBJECT_ID(N'app.VehicleDailyDistances') AND c.name=N'IsOdometerCorrection' AND c.system_type_id=104 AND c.is_nullable=0 AND d.definition LIKE '%0%') THROW 51000,'Correction column invalid',1;
SELECT COUNT_BIG(*) AS TotalRows, COUNT_BIG(CASE WHEN IsOdometerCorrection=1 THEN 1 END) AS CorrectionRows FROM app.VehicleDailyDistances;
'@
$sql = @'
ALTER TABLE [app].[VehicleDailyDistances] ADD [IsOdometerCorrection] bit NOT NULL DEFAULT CAST(0 AS bit);
INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261031092701_AddDailyOdometerCorrections', N'10.0.11');
'@
try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $connection.Open()
    $history = Query 'SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;'
    $hosted = @($history.Tables[0].Rows | ForEach-Object MigrationId)
    $local = @(Get-ChildItem (Join-Path $root 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $missing = @($local | Where-Object { $_ -notin $hosted -and $_ -ne $migration }); $unknown = @($hosted | Where-Object { $_ -notin $local })
    if ($missing.Count -or $unknown.Count -or '20261029071925_MoveDayContributionsToAssignment' -notin $hosted) { throw "Unexpected migration history. Missing: $($missing -join ', '); unknown: $($unknown -join ', ')" }
    if ($migration -in $hosted) { [void](Query $verification); [pscustomobject]@{AlreadyApplied=$true;Verified=$true} | ConvertTo-Json; return }
    $pre = Query "IF OBJECT_ID(N'app.VehicleDailyDistances',N'U') IS NULL THROW 51000,'Daily distances table missing',1; SELECT COL_LENGTH(N'app.VehicleDailyDistances',N'IsOdometerCorrection') AS ColumnLength, COUNT_BIG(*) AS TotalRows FROM app.VehicleDailyDistances;"
    if ($pre.Tables[0].Rows[0].ColumnLength -ne [DBNull]::Value) { throw 'Correction column exists without migration history.' }
    if (!$Apply) { [pscustomobject]@{Database=$connection.Database;Pending=$migration;Preflight='OK';Rows=[long]$pre.Tables[0].Rows[0].TotalRows} | ConvertTo-Json; return }
    $output = Join-Path $root ('outputs/daily-odometer-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')); [void](New-Item -ItemType Directory -Path $output)
    $history.WriteXml((Join-Path $output 'history-before.xml'), [Data.XmlWriteMode]::WriteSchema)
    $backup = Query @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-odometer-corrections-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath, SYSUTCDATETIME() CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $output 'server-backup.xml'), [Data.XmlWriteMode]::WriteSchema)
    foreach ($commit in @($false, $true)) {
        $script:transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
        try {
            [void](Query "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'daily-odometer-corrections-deploy',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51000,'Deployment lock unavailable',1;")
            [void](Query $sql)
            $state = Query $verification
            if ([long]$state.Tables[0].Rows[0].TotalRows -ne [long]$pre.Tables[0].Rows[0].TotalRows -or [long]$state.Tables[0].Rows[0].CorrectionRows -ne 0) { throw 'Unexpected daily-distance row changes.' }
            if ($commit) { $script:transaction.Commit() } else { $script:transaction.Rollback() }
        } catch { try { $script:transaction.Rollback() } catch {}; throw } finally { $script:transaction.Dispose(); $script:transaction = $null }
    }
    $connection.Close(); $connection.Open(); $after = Query $verification
    $after.WriteXml((Join-Path $output 'postcommit-verification.xml'), [Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{Database=$connection.Database;Migration=$migration;BackupCompleted=$true;RehearsalRolledBack=$true;Applied=$true;PostCommitVerified=$true;Rows=[long]$after.Tables[0].Rows[0].TotalRows;ApiDeployed=$false;EvidenceDirectory=$output}
    $report | ConvertTo-Json | Set-Content (Join-Path $output 'report.json'); $report | ConvertTo-Json
} finally { if ($script:transaction) { $script:transaction.Dispose() }; $connection.Dispose() }
