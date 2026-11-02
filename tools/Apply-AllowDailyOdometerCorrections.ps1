param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$settings = Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$cs = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $settings.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($cs)
$script:transaction = $null
$migration = '20261031095204_AllowDailyOdometerCorrections'
function Query([string]$sql, [int]$timeout = 120) {
    $command = $connection.CreateCommand(); $command.CommandText = $sql; $command.CommandTimeout = $timeout; $command.Transaction = $script:transaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data } catch { $inner=$_.Exception.InnerException; if ($inner -is [Data.SqlClient.SqlException]) { throw "SQL error $($inner.Number) at line $($inner.LineNumber): $($inner.Message)" }; throw } finally { $adapter.Dispose(); $command.Dispose() }
}
$verify = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261031095204_AllowDailyOdometerCorrections') THROW 51000,'Migration history missing',1;
DECLARE @definition nvarchar(max)=(SELECT definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'app.VehicleDailyDistances') AND name=N'CK_VehicleDailyDistances_ManualOdometer' AND is_disabled=0 AND is_not_trusted=0);
IF @definition IS NULL OR @definition NOT LIKE N'%IsOdometerCorrection%' THROW 51000,'Constraint invalid',1;
CREATE TABLE #CheckOdometer(ManualOdometerReading bigint NULL, ManualBaselineOdometerReading decimal(18,2) NULL, IsOdometerCorrection bit NOT NULL);
EXEC(N'ALTER TABLE #CheckOdometer ADD CHECK '+@definition);
INSERT INTO #CheckOdometer VALUES (20,100,1),(100,100,0),(NULL,NULL,0);
DECLARE @rejected int=0;
BEGIN TRY INSERT INTO #CheckOdometer VALUES(20,100,0); END TRY BEGIN CATCH IF ERROR_NUMBER()<>547 THROW; SET @rejected+=1; END CATCH;
BEGIN TRY INSERT INTO #CheckOdometer VALUES(-1,100,1); END TRY BEGIN CATCH IF ERROR_NUMBER()<>547 THROW; SET @rejected+=1; END CATCH;
BEGIN TRY INSERT INTO #CheckOdometer VALUES(20,NULL,1); END TRY BEGIN CATCH IF ERROR_NUMBER()<>547 THROW; SET @rejected+=1; END CATCH;
IF @rejected<>3 THROW 51000,'Constraint accepted invalid readings',1;
SELECT @rejected AS RejectedInvalidCases, COUNT_BIG(*) AS DailyRows FROM app.VehicleDailyDistances;
'@
$sql = @'
ALTER TABLE [app].[VehicleDailyDistances] DROP CONSTRAINT [CK_VehicleDailyDistances_ManualOdometer];
ALTER TABLE [app].[VehicleDailyDistances] WITH CHECK ADD CONSTRAINT [CK_VehicleDailyDistances_ManualOdometer] CHECK ([ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= 0 AND ([IsOdometerCorrection] = 1 OR [ManualOdometerReading] >= [ManualBaselineOdometerReading])));
INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261031095204_AllowDailyOdometerCorrections',N'10.0.11');
'@
try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $connection.Open()
    $history = Query 'SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;'
    $hosted = @($history.Tables[0].Rows | ForEach-Object MigrationId)
    $local = @(Get-ChildItem (Join-Path $root 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $missing = @($local | Where-Object { $_ -notin $hosted -and $_ -ne $migration }); $unknown = @($hosted | Where-Object { $_ -notin $local })
    if ($missing.Count -or $unknown.Count -or '20261031092701_AddDailyOdometerCorrections' -notin $hosted) { throw "Unexpected migration history. Missing: $($missing -join ', '); unknown: $($unknown -join ', ')" }
    if ($migration -in $hosted) { $result=Query $verify; [pscustomobject]@{AlreadyApplied=$true;Verified=$true;Rows=[long]$result.Tables[0].Rows[0].DailyRows} | ConvertTo-Json; return }
    if (!$Apply) { [pscustomobject]@{Database=$connection.Database;Pending=$migration;Preflight='OK'} | ConvertTo-Json; return }
    $output = Join-Path $root ('outputs/odometer-constraint-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')); [void](New-Item -ItemType Directory -Path $output)
    $history.WriteXml((Join-Path $output 'history-before.xml'), [Data.XmlWriteMode]::WriteSchema)
    $backup = Query @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-odometer-constraint-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath, SYSUTCDATETIME() CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $output 'server-backup.xml'), [Data.XmlWriteMode]::WriteSchema)
    foreach ($commit in @($false, $true)) {
        $script:transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
        try {
            [void](Query "SET XACT_ABORT OFF; SET LOCK_TIMEOUT 15000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'daily-odometer-constraint-deploy',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51000,'Deployment lock unavailable',1;")
            [void](Query $sql)
            $state=Query $verify
            if ($commit) { $script:transaction.Commit() } else { $script:transaction.Rollback() }
        } catch { try { $script:transaction.Rollback() } catch {}; throw } finally { $script:transaction.Dispose(); $script:transaction = $null }
    }
    $connection.Close(); $connection.Open(); $after=Query $verify
    $report=[ordered]@{Database=$connection.Database;Migration=$migration;BackupCompleted=$true;RehearsalRolledBack=$true;Applied=$true;PostCommitVerified=$true;RejectedInvalidCases=[int]$after.Tables[0].Rows[0].RejectedInvalidCases;Rows=[long]$after.Tables[0].Rows[0].DailyRows;ApiDeployed=$false;EvidenceDirectory=$output}
    $report | ConvertTo-Json | Set-Content (Join-Path $output 'report.json'); $report | ConvertTo-Json
} finally { if ($script:transaction) { $script:transaction.Dispose() }; $connection.Dispose() }
