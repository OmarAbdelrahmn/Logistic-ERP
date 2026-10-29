param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$settings = Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $settings.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($connectionText)
$script:transaction = $null
$migration = '20261029071925_MoveDayContributionsToAssignment'
function Query([string]$sql, [int]$timeout = 120) {
    $command = $connection.CreateCommand(); $command.CommandText = $sql; $command.CommandTimeout = $timeout; $command.Transaction = $script:transaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data } finally { $adapter.Dispose(); $command.Dispose() }
}
$verification = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261029071925_MoveDayContributionsToAssignment') THROW 51000,'Migration history missing',1;
IF OBJECT_ID(N'app.PlatformAccountDayContributions',N'U') IS NULL THROW 51000,'Contribution table missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'app.PlatformAccountDayContributions') AND name=N'RowVersion' AND system_type_id=189) THROW 51000,'Rowversion missing',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'app.PlatformAccountDayContributions') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>2 THROW 51000,'Foreign keys missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.PlatformAccountDayContributions') AND is_unique=1 AND has_filter=1 AND is_disabled=0) THROW 51000,'Unique index missing',1;
SELECT COUNT_BIG(*) ContributionRows FROM app.PlatformAccountDayContributions;
'@
try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $connection.Open()
    $history = Query 'SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;'
    $hosted = @($history.Tables[0].Rows | ForEach-Object MigrationId)
    $local = @(Get-ChildItem (Join-Path $root 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $missing = @($local | Where-Object { $_ -notin $hosted -and $_ -ne $migration }); $unknown = @($hosted | Where-Object { $_ -notin $local })
    if ($missing.Count -or $unknown.Count -or '20261028111233_AddNinjaAssignmentDatePermissionCatalog' -notin $hosted) { throw "Unexpected migration history. Missing: $($missing -join ', '); unknown: $($unknown -join ', ')" }
    if ($migration -in $hosted) { [void](Query $verification); [pscustomobject]@{AlreadyApplied=$true;Verified=$true} | ConvertTo-Json; return }
    if ((Query "SELECT OBJECT_ID(N'app.PlatformAccountDayContributions',N'U') AS TableId;").Tables[0].Rows[0].TableId -ne [DBNull]::Value) { throw 'Partial schema without migration history.' }
    if (!$Apply) { [pscustomobject]@{Database=$connection.Database;Pending=$migration;Preflight='OK'} | ConvertTo-Json; return }
    $output = Join-Path $root ('outputs/assignment-day-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')); [void](New-Item -ItemType Directory -Path $output)
    $history.WriteXml((Join-Path $output 'history-before.xml'), [Data.XmlWriteMode]::WriteSchema)
    $path = Join-Path $root 'database/scripts/assignment-day-contributions.sql'; $sql = Get-Content $path -Raw
    if ($sql -notmatch [regex]::Escape($migration) -or $sql -match '(?im)^\s*(DROP TABLE|TRUNCATE|DELETE FROM|ALTER TABLE)\b') { throw 'Unexpected script scope.' }
    $sql = $sql -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', ''
    Copy-Item $path (Join-Path $output 'migration.sql')
    $backup = Query @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-day-contributions-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath, SYSUTCDATETIME() CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $output 'server-backup.xml'), [Data.XmlWriteMode]::WriteSchema)
    foreach ($commit in @($false, $true)) {
        $script:transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
        try {
            [void](Query "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'assignment-day-contributions-deploy',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51000,'Deployment lock unavailable',1;")
            foreach ($batch in [regex]::Split($sql, '(?im)^GO\s*\r?$')) { if (![string]::IsNullOrWhiteSpace($batch)) { [void](Query $batch) } }
            $state = Query $verification; $mode = if ($commit) { 'apply' } else { 'rehearsal' }; $state.WriteXml((Join-Path $output "$mode-verification.xml"), [Data.XmlWriteMode]::WriteSchema)
            if ($commit) { $script:transaction.Commit() } else { $script:transaction.Rollback() }
        } catch { try { $script:transaction.Rollback() } catch {}; throw } finally { $script:transaction.Dispose(); $script:transaction = $null }
    }
    $connection.Close(); $connection.Open(); $after = Query $verification; $after.WriteXml((Join-Path $output 'postcommit-verification.xml'), [Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{Database=$connection.Database;Migration=$migration;BackupCompleted=$true;RehearsalRolledBack=$true;Applied=$true;PostCommitVerified=$true;ContributionRows=[long]$after.Tables[0].Rows[0].ContributionRows;ApiDeployed=$false;EvidenceDirectory=$output}
    $report | ConvertTo-Json | Set-Content (Join-Path $output 'report.json'); $report | ConvertTo-Json
} finally { if ($script:transaction) { $script:transaction.Dispose() }; $connection.Dispose() }
