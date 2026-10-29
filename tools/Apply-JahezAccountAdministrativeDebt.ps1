param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$debtRoot = Split-Path -Parent $PSScriptRoot
$debtMigration = '20261026165219_MakeJahezAdministrativeDebtAccountBased'
$debtSettings = Get-Content -LiteralPath (Join-Path $debtRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$debtConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $debtSettings.ConnectionStrings.LogisticsDatabase }
$debtConnection = [Data.SqlClient.SqlConnection]::new($debtConnectionText)
$debtTransaction = $null
$debtSqlPath = Join-Path $debtRoot 'database/scripts/jahez-account-administrative-debt.sql'
$debtVerifySql = Get-Content -LiteralPath (Join-Path $debtRoot 'database/scripts/verify-jahez-account-administrative-debt.sql') -Raw

function Read-DebtDeployment([string]$Sql, [int]$Timeout = 90) {
    $command = $debtConnection.CreateCommand()
    $command.Transaction = $debtTransaction; $command.CommandText = $Sql; $command.CommandTimeout = $Timeout
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data }
    finally { $adapter.Dispose(); $command.Dispose() }
}

function Assert-DebtPreflight {
    $state = Read-DebtDeployment 'SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;'
    $hosted = @($state.Tables[0].Rows | ForEach-Object MigrationId)
    $local = @(Get-ChildItem -LiteralPath (Join-Path $debtRoot 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' |
        Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    if (@($local | Where-Object { $_ -notin $hosted -and $_ -ne $debtMigration }).Count -gt 0 -or
        @($hosted | Where-Object { $_ -notin $local }).Count -gt 0) { throw 'Unexpected migration history; refusing unrelated changes.' }
    return ,$state
}

try {
    if ($debtConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $debtConnection.Database -ne 'db67927') { throw 'Unexpected hosted target.' }
    $debtConnection.Open()
    $debtBeforeHistory = Assert-DebtPreflight
    if ($debtMigration -in @($debtBeforeHistory.Tables[0].Rows | ForEach-Object MigrationId)) {
        [void](Read-DebtDeployment $debtVerifySql)
        [pscustomobject]@{ Database=$debtConnection.Database; AlreadyApplied=$true; Verified=$true; DatabaseWrites=0 } | ConvertTo-Json
        return
    }
    [void](Read-DebtDeployment @'
IF COL_LENGTH(N'jahez_v2.Requests',N'AccountId') IS NOT NULL
    OR COL_LENGTH(N'jahez_v2.AccountingEntries',N'AccountId') IS NOT NULL
    OR COL_LENGTH(N'jahez_v2.PlatformTransactions',N'AccountId') IS NOT NULL
    THROW 51000,'Account debt schema exists without its migration history.',1;
'@)
    if (!$Apply) {
        [pscustomobject]@{ Database=$debtConnection.Database; PendingMigration=$debtMigration; Mode='ReadOnly'; Verified=$true } | ConvertTo-Json
        return
    }
    $debtSql = Get-Content -LiteralPath $debtSqlPath -Raw
    if ($debtSql -notmatch [regex]::Escape($debtMigration) -or $debtSql -match '(?im)^\s*(DROP TABLE|TRUNCATE|DELETE FROM|BEGIN TRANSACTION|COMMIT)\b' -or
        $debtSql -match '(?im)^\s*ALTER TABLE \[jahez_v2\]\.\[DailyOrders\]') { throw 'Unexpected migration SQL scope.' }
    $debtOutput = Join-Path $debtRoot ('outputs/jahez-account-debt-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $debtOutput)
    $debtBeforeHistory.WriteXml((Join-Path $debtOutput 'migration-history-before.xml'),[Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $debtSqlPath -Destination (Join-Path $debtOutput 'applied.sql')
    $backup = Read-DebtDeployment @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-jahez-account-debt-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,SYSUTCDATETIME() AS CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $debtOutput 'server-backup.xml'),[Data.XmlWriteMode]::WriteSchema)
    Write-Output 'Server COPY_ONLY checksum backup completed.'
    $debtTransaction = $debtConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
    [void](Read-DebtDeployment @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'jahez-account-debt-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire migration lock.',1;
'@)
    [void](Assert-DebtPreflight)
    $metadata = Read-DebtDeployment @'
SELECT SCHEMA_NAME(t.schema_id) SchemaName,t.name TableName,c.name ColumnName,c.system_type_id TypeId
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
WHERE SCHEMA_NAME(t.schema_id) IN(N'jahez_v2',N'jahez_v3')
    OR (SCHEMA_NAME(t.schema_id)=N'app' AND t.name IN(N'PlatformRiderAccounts',N'RiderClientAssignments'))
ORDER BY SchemaName,TableName,c.column_id;
'@
    $snapshots = @(); $number = 0
    foreach ($group in ($metadata.Tables[0].Rows | Group-Object { $_.SchemaName + '.' + $_.TableName })) {
        $target = '[' + $group.Group[0].SchemaName.Replace(']',']]') + '].[' + $group.Group[0].TableName.Replace(']',']]') + ']'
        $temporary = '#BeforeAccountDebt' + $number++
        $select = @($group.Group | ForEach-Object {
            $name = '[' + $_.ColumnName.Replace(']',']]') + ']'
            if ($_.TypeId -eq 189) { "CONVERT(binary(8),$name) AS $name" } else { $name }
        }) -join ','
        $columns = @($group.Group | Where-Object { !($target -eq '[jahez_v2].[Requests]' -and $_.ColumnName -eq 'RowVersion') } |
            ForEach-Object { '[' + $_.ColumnName.Replace(']',']]') + ']' }) -join ','
        [void](Read-DebtDeployment "SELECT $select INTO $temporary FROM $target WITH(UPDLOCK,HOLDLOCK);")
        $snapshots += @{ Target=$target; Temporary=$temporary; Columns=$columns }
    }
    foreach ($batch in [regex]::Split($debtSql,'(?im)^GO\s*\r?$')) {
        if (![string]::IsNullOrWhiteSpace($batch)) { [void](Read-DebtDeployment $batch) }
    }
    foreach ($snapshot in $snapshots) {
        $columns=$snapshot.Columns; $target=$snapshot.Target; $temporary=$snapshot.Temporary
        [void](Read-DebtDeployment "IF EXISTS(SELECT $columns FROM $temporary EXCEPT SELECT $columns FROM $target) OR EXISTS(SELECT $columns FROM $target EXCEPT SELECT $columns FROM $temporary) THROW 51000,'Existing records changed: $target',1;")
    }
    $verified = Read-DebtDeployment $debtVerifySql
    $verified.WriteXml((Join-Path $debtOutput 'verification-before-commit.xml'),[Data.XmlWriteMode]::WriteSchema)
    $debtTransaction.Commit(); $debtTransaction.Dispose(); $debtTransaction=$null
    $debtConnection.Close(); $debtConnection.Open()
    $after = Read-DebtDeployment $debtVerifySql
    $after.WriteXml((Join-Path $debtOutput 'verification-after-commit.xml'),[Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{
        Server=$debtConnection.DataSource; Database=$debtConnection.Database; AppliedMigration=$debtMigration
        VerifiedAtUtc=[DateTimeOffset]::UtcNow.ToString('o'); PostCommitVerified=$true
        ServerBackupCompleted=$true; RestoreVerifyOnlyPerformed=$false; ProtectedTables=$snapshots.Count
        OriginalValuesPreserved=$true; RequestRowVersionsRefreshed=$true; DailyOrdersUnchanged=$true
        Requests=[long]$after.Tables[1].Rows[0].Requests; LedgerEntries=[long]$after.Tables[2].Rows[0].LedgerEntries
        Transactions=[long]$after.Tables[3].Rows[0].Transactions; DailyOrders=[long]$after.Tables[4].Rows[0].DailyOrders
        MoneyMovements=[long]$after.Tables[5].Rows[0].MoneyMovements
        SqlSha256=(Get-FileHash -LiteralPath $debtSqlPath -Algorithm SHA256).Hash
        ApiDeployed=$false; EvidenceDirectory=$debtOutput
    }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $debtOutput 'report.json') -Encoding utf8
    $report | ConvertTo-Json -Depth 5
} catch [Data.SqlClient.SqlException] {
    if ($debtTransaction -and $debtTransaction.Connection) { $debtTransaction.Rollback() }
    foreach ($sqlError in $_.Exception.Errors) { Write-Output ('SQL ' + $sqlError.Number + ' line ' + $sqlError.LineNumber + ': ' + $sqlError.Message) }
    throw 'Hosted account debt deployment SQL failed; credentials omitted.'
} catch {
    if ($debtTransaction -and $debtTransaction.Connection) { $debtTransaction.Rollback() }
    throw
} finally {
    if ($debtTransaction) { $debtTransaction.Dispose() }
    $debtConnection.Dispose()
}
