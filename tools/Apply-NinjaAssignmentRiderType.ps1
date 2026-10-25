param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$ninjaWorkspace = Split-Path -Parent $PSScriptRoot
$ninjaConfig = Get-Content -LiteralPath (Join-Path $ninjaWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$ninjaConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $ninjaConfig.ConnectionStrings.LogisticsDatabase }
$ninjaConnection = [System.Data.SqlClient.SqlConnection]::new($ninjaConnectionText)
$ninjaTransaction = $null
$ninjaMigration = '20261024150306_AddNinjaAssignmentRiderType'

function Read-NinjaDeployment([string]$Sql, [int]$Timeout = 60) {
    $command = $ninjaConnection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $Timeout
    $command.Transaction = $ninjaTransaction
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [System.Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

function Get-NinjaMigrationState {
    $state = Read-NinjaDeployment @'
SELECT MigrationId FROM [migration].[__ApplicationMigrationsHistory] ORDER BY MigrationId;
SELECT MigrationId FROM [migration].[__IdentityMigrationsHistory] ORDER BY MigrationId;
'@
    foreach ($context in @(
        @{Path='src/LogisticsERP.Infrastructure/Persistence/Migrations/Application';Table=0;Allowed=@($ninjaMigration)},
        @{Path='src/LogisticsERP.Infrastructure/Identity/Migrations';Table=1;Allowed=@()}
    )) {
        $local = @(Get-ChildItem -LiteralPath (Join-Path $ninjaWorkspace $context.Path) -Filter '*.cs' |
            Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
        $hosted = @($state.Tables[$context.Table].Rows | ForEach-Object MigrationId)
        if (@($local | Where-Object { $_ -notin $hosted -and $_ -notin $context.Allowed }).Count -gt 0 -or
            @($hosted | Where-Object { $_ -notin $local }).Count -gt 0) {
            throw 'Unexpected migration history. No unrelated migrations may be applied.'
        }
    }
    return ,$state
}

$ninjaPreflightSql = @'
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024132700_AddJahezAccountFeeRefunds')
    THROW 51000,'Required preceding migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024150306_AddNinjaAssignmentRiderType')
    AND (COL_LENGTH(N'app.RiderClientAssignments',N'NinjaRiderType') IS NOT NULL
        OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_RiderClientAssignments_NinjaRiderType'))
    THROW 51000,'Rider type schema exists without the migration record.',1;
SELECT COUNT_BIG(*) AS Assignments FROM app.RiderClientAssignments;
SELECT name,system_type_id FROM sys.columns WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments') ORDER BY column_id;
'@

$ninjaVerifySql = @'
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024150306_AddNinjaAssignmentRiderType')
    THROW 51000,'Ninja migration record is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments') AND name=N'NinjaRiderType'
    AND TYPE_NAME(system_type_id)=N'nvarchar' AND max_length=64 AND is_nullable=1 AND default_object_id=0)
    THROW 51000,'Ninja rider type column does not match the model.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'app.RiderClientAssignments')
    AND name=N'CK_RiderClientAssignments_NinjaRiderType' AND is_disabled=0 AND is_not_trusted=0
    AND definition LIKE N'%InternalCar%' AND definition LIKE N'%ExternalCar%'
    AND definition LIKE N'%InternalMotorcycle%' AND definition LIKE N'%ExternalMotorcycle%')
    THROW 51000,'Ninja rider type check is missing, disabled or untrusted.',1;
SELECT MigrationId,ProductVersion FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024150306_AddNinjaAssignmentRiderType';
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'app.RiderClientAssignments') AND name=N'CK_RiderClientAssignments_NinjaRiderType';
SELECT COUNT_BIG(*) AS Assignments FROM app.RiderClientAssignments;
SELECT CONVERT(varchar(33),SYSUTCDATETIME(),126)+'Z' AS VerifiedAtUtc;
'@

try {
    if ($ninjaConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $ninjaConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted target. No query executed.'
    }
    $ninjaConnection.Open()
    $ninjaBeforeState = Get-NinjaMigrationState
    $ninjaPending = @($ninjaMigration | Where-Object { $_ -notin @($ninjaBeforeState.Tables[0].Rows | ForEach-Object MigrationId) })
    $ninjaBefore = Read-NinjaDeployment $ninjaPreflightSql
    if (!$Apply) {
        [pscustomobject]@{Database=$ninjaConnection.Database;Pending=$ninjaPending;Assignments=[long]$ninjaBefore.Tables[0].Rows[0].Assignments;Mode='ReadOnly'} | ConvertTo-Json
        return
    }
    if ($ninjaPending.Count -eq 0) {
        [void](Read-NinjaDeployment $ninjaVerifySql)
        [pscustomobject]@{Database=$ninjaConnection.Database;Pending=@();AlreadyApplied=$true;Verified=$true} | ConvertTo-Json
        return
    }
    $ninjaScriptPath = Join-Path $ninjaWorkspace 'database/scripts/ninja-assignment-rider-types.sql'
    $ninjaScript = Get-Content -LiteralPath $ninjaScriptPath -Raw
    if (([regex]::Matches($ninjaScript,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($ninjaScript,'(?im)^COMMIT;')).Count -ne 1 -or
        $ninjaScript -match '(?im)^\s*(DROP|TRUNCATE|DELETE|UPDATE)\b') { throw 'Unexpected migration script structure.' }
    $ninjaOutput = Join-Path $ninjaWorkspace ('outputs/ninja-rider-type-hosted-deployment-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $ninjaOutput)
    Copy-Item -LiteralPath $ninjaScriptPath -Destination (Join-Path $ninjaOutput 'migration.sql')
    $ninjaBeforeState.WriteXml((Join-Path $ninjaOutput 'migration-history-before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaBackup = Read-NinjaDeployment @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-ninja-rider-type-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,CONVERT(varchar(33),SYSUTCDATETIME(),126)+'Z' AS CompletedAtUtc;
'@ 180
    $ninjaBackup.WriteXml((Join-Path $ninjaOutput 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Write-Output 'Server COPY_ONLY checksum backup completed. Applying the Ninja rider type migration.'
    $ninjaTransaction = $ninjaConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-NinjaDeployment @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'ninja-assignment-rider-type-deployment',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire migration lock.',1;
'@)
    [void](Get-NinjaMigrationState)
    $ninjaLockedBefore = Read-NinjaDeployment $ninjaPreflightSql
    $ninjaColumns = @($ninjaLockedBefore.Tables[1].Rows | ForEach-Object {
        $name = '[' + $_.name.Replace(']',']]') + ']'
        if ($_.system_type_id -eq 189) { "CONVERT(binary(8),$name) AS $name" } else { $name }
    }) -join ','
    $ninjaCompareColumns = @($ninjaLockedBefore.Tables[1].Rows | ForEach-Object { '[' + $_.name.Replace(']',']]') + ']' }) -join ','
    [void](Read-NinjaDeployment "SELECT $ninjaColumns INTO #BeforeNinjaRiderType FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);")
    $ninjaSql = $ninjaScript -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
    [void](Read-NinjaDeployment $ninjaSql)
    [void](Read-NinjaDeployment "IF EXISTS(SELECT $ninjaCompareColumns FROM #BeforeNinjaRiderType EXCEPT SELECT $ninjaCompareColumns FROM app.RiderClientAssignments) OR EXISTS(SELECT $ninjaCompareColumns FROM app.RiderClientAssignments EXCEPT SELECT $ninjaCompareColumns FROM #BeforeNinjaRiderType) THROW 51000,'Existing assignments changed.',1;")
    [void](Read-NinjaDeployment "IF EXISTS(SELECT 1 FROM app.RiderClientAssignments WHERE NinjaRiderType IS NOT NULL) THROW 51000,'Existing assignments received an unexpected rider type.',1;")
    $ninjaVerified = Read-NinjaDeployment $ninjaVerifySql
    $ninjaVerified.WriteXml((Join-Path $ninjaOutput 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaTransaction.Commit(); $ninjaTransaction.Dispose(); $ninjaTransaction = $null
    $ninjaConnection.Close(); $ninjaConnection.Open()
    $ninjaAfter = Read-NinjaDeployment $ninjaVerifySql
    $ninjaAfterState = Get-NinjaMigrationState
    $ninjaAfter.WriteXml((Join-Path $ninjaOutput 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaAfterState.WriteXml((Join-Path $ninjaOutput 'migration-history-after.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaReport = [ordered]@{
        Database=$ninjaConnection.Database;VerifiedAtUtc=[string]$ninjaAfter.Tables[3].Rows[0].VerifiedAtUtc
        AppliedMigration=$ninjaMigration;ApplicationPending=@();IdentityPending=@();PostCommitVerified=$true
        ExistingAssignmentsComparedAndPreserved=$true;AssignmentsBefore=[long]$ninjaLockedBefore.Tables[0].Rows[0].Assignments
        AssignmentsAfter=[long]$ninjaVerified.Tables[2].Rows[0].Assignments;ExistingAssignmentsRemainUnclassified=$true
        ServerBackupCompleted=$true;RestoreVerificationPerformed=$false;ApiDeployed=$false
        ScriptSha256=(Get-FileHash -LiteralPath $ninjaScriptPath -Algorithm SHA256).Hash;EvidenceDirectory=$ninjaOutput
    }
    $ninjaReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ninjaOutput 'report.json') -Encoding UTF8
    $ninjaReport | ConvertTo-Json -Depth 5
} catch {
    if ($ninjaTransaction) { try { $ninjaTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($ninjaTransaction) { $ninjaTransaction.Dispose() }
    $ninjaConnection.Dispose()
}
