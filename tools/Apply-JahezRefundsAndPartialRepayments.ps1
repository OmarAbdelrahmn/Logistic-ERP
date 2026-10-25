param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$refundWorkspace = Split-Path -Parent $PSScriptRoot
$refundConfig = Get-Content -LiteralPath (Join-Path $refundWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$refundConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $refundConfig.ConnectionStrings.LogisticsDatabase }
$refundConnection = [System.Data.SqlClient.SqlConnection]::new($refundConnectionText)
$refundTransaction = $null
$refundExpected = @('20261024115050_SupportJahezPartialDebtRepayments', '20261024132700_AddJahezAccountFeeRefunds')
$refundScriptPaths = @('database/scripts/jahez-partial-debt-repayments.sql', 'database/scripts/jahez-account-fee-refunds.sql')

function Read-RefundDeployment([string]$Sql, [int]$Timeout = 60) {
    $command = $refundConnection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $Timeout
    $command.Transaction = $refundTransaction
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [System.Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

function Get-RefundMigrationState {
    $state = Read-RefundDeployment @'
SELECT MigrationId FROM [migration].[__ApplicationMigrationsHistory] ORDER BY MigrationId;
SELECT MigrationId FROM [migration].[__IdentityMigrationsHistory] ORDER BY MigrationId;
'@
    foreach ($context in @(
        @{ Path='src/LogisticsERP.Infrastructure/Persistence/Migrations/Application'; Table=$state.Tables[0]; Allowed=$refundExpected },
        @{ Path='src/LogisticsERP.Infrastructure/Identity/Migrations'; Table=$state.Tables[1]; Allowed=@() }
    )) {
        $local = @(Get-ChildItem -LiteralPath (Join-Path $refundWorkspace $context.Path) -Filter '*.cs' |
            Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
        $hosted = @($context.Table.Rows | ForEach-Object MigrationId)
        if (@($local | Where-Object { $_ -notin $hosted -and $_ -notin $context.Allowed }).Count -gt 0 -or
            @($hosted | Where-Object { $_ -notin $local }).Count -gt 0) { throw 'Unexpected migration history; no unrelated migrations may be applied.' }
    }
    return ,$state
}

$refundPreflightSql = @'
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261021071103_AddPlatformAccountDayAllocations')
    THROW 51000,'Required preceding migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024115050_SupportJahezPartialDebtRepayments')
BEGIN
    IF COL_LENGTH(N'jahez_v2.TransferredDebts',N'PaidAmount') IS NOT NULL
        OR COL_LENGTH(N'jahez_v2.MoneyMovements',N'CommissionPaid') IS NOT NULL
        OR COL_LENGTH(N'jahez_v2.MoneyMovements',N'DashboardDebtPaid') IS NOT NULL
        THROW 51000,'Partial debt schema exists without its migration record.',1;
    IF EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements m JOIN jahez_v2.TransferredDebts d ON d.Id=m.TransferredDebtId
        WHERE m.Direction<>1 OR d.PaidAtUtc IS NULL OR m.Amount<>d.Amount)
        THROW 51000,'Legacy debt receipts require reconciliation first.',1;
END;
IF NOT EXISTS(SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId=N'20261024132700_AddJahezAccountFeeRefunds')
    AND OBJECT_ID(N'jahez_v2.AccountFeeRefunds',N'U') IS NOT NULL
    THROW 51000,'Refund table exists without its migration record.',1;
SELECT COUNT_BIG(*) AS MoneyMovements,COALESCE(SUM(Amount),0) AS MovementAmounts FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerEntries,COALESCE(SUM(Amount),0) AS LedgerAmounts FROM jahez_v2.AccountingEntries;
SELECT COUNT_BIG(*) AS Debts,COALESCE(SUM(Amount),0) AS DebtAmounts,SUM(CASE WHEN PaidAtUtc IS NOT NULL THEN 1 ELSE 0 END) AS PaidDebts FROM jahez_v2.TransferredDebts;
'@

$refundVerifySql = @'
IF (SELECT COUNT(*) FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId IN
    (N'20261024115050_SupportJahezPartialDebtRepayments',N'20261024132700_AddJahezAccountFeeRefunds'))<>2
    THROW 51000,'Migration history is incomplete.',1;
IF OBJECT_ID(N'jahez_v2.AccountFeeRefunds',N'U') IS NULL
    OR (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez_v2.AccountFeeRefunds'))<>17
    THROW 51000,'Refund table or columns are missing.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez_v2.TransferredDebts') AND name=N'PaidAmount'
    AND TYPE_NAME(system_type_id)=N'decimal' AND precision=18 AND scale=2 AND is_nullable=0)<>1
    OR (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez_v2.MoneyMovements') AND name IN(N'CommissionPaid',N'DashboardDebtPaid')
    AND TYPE_NAME(system_type_id)=N'decimal' AND precision=18 AND scale=2 AND is_nullable=1)<>2
    THROW 51000,'Partial-payment column definitions do not match.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN(N'CK_JahezFeeRefund_Amounts',N'CK_JahezTransferredDebt_Amount',N'CK_JahezMoney_DebtAllocation')
    AND is_disabled=0 AND is_not_trusted=0)<>3
    THROW 51000,'Financial checks are missing or untrusted.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'jahez_v2.AccountFeeRefunds')
    AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>3
    THROW 51000,'Refund foreign keys are missing or untrusted.',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.AccountFeeRefunds')
    AND name IN(N'IX_AccountFeeRefunds_AccountFeeId',N'IX_AccountFeeRefunds_HandoverId',N'IX_AccountFeeRefunds_MoneyMovementId') AND is_unique=1 AND is_disabled=0)<>3
    OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.AccountFeeRefunds') AND name=N'IX_AccountFeeRefunds_OccurredAtUtc' AND is_disabled=0)
    OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.MoneyMovements') AND name=N'IX_MoneyMovements_TransferredDebtId' AND is_unique=0 AND has_filter=1 AND is_disabled=0)
    THROW 51000,'Refund or partial-payment indexes are missing or incorrect.',1;
SELECT MigrationId,ProductVersion FROM [migration].[__ApplicationMigrationsHistory] WHERE MigrationId IN
    (N'20261024115050_SupportJahezPartialDebtRepayments',N'20261024132700_AddJahezAccountFeeRefunds') ORDER BY MigrationId;
SELECT COUNT_BIG(*) AS Refunds FROM jahez_v2.AccountFeeRefunds;
SELECT COUNT_BIG(*) AS MoneyMovements,COALESCE(SUM(Amount),0) AS MovementAmounts FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerEntries,COALESCE(SUM(Amount),0) AS LedgerAmounts FROM jahez_v2.AccountingEntries;
SELECT COUNT_BIG(*) AS Debts,COALESCE(SUM(Amount),0) AS DebtAmounts,COALESCE(SUM(PaidAmount),0) AS PaidAmounts FROM jahez_v2.TransferredDebts;
'@

try {
    if ($refundConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $refundConnection.Database -ne 'db67927') { throw 'Unexpected hosted target; no query executed.' }
    $refundConnection.Open()
    $refundBeforeState = Get-RefundMigrationState
    $refundApplied = @($refundBeforeState.Tables[0].Rows | ForEach-Object MigrationId)
    $refundPending = @($refundExpected | Where-Object { $_ -notin $refundApplied })
    $refundBefore = Read-RefundDeployment $refundPreflightSql
    if (!$Apply) {
        [pscustomobject]@{ Database=$refundConnection.Database; Pending=$refundPending; Mode='ReadOnly'; LegacyDebtReceiptsConsistent=$true } | ConvertTo-Json
        return
    }
    if ($refundPending.Count -eq 0) {
        [void](Read-RefundDeployment $refundVerifySql)
        [pscustomobject]@{ Database=$refundConnection.Database; Pending=@(); AlreadyApplied=$true; Verified=$true } | ConvertTo-Json
        return
    }
    $refundOutput = Join-Path $refundWorkspace ('outputs/jahez-refunds-hosted-deployment-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $refundOutput)
    $refundBefore.WriteXml((Join-Path $refundOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $refundBeforeState.WriteXml((Join-Path $refundOutput 'migration-history-before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $refundScripts = @()
    foreach ($relative in $refundScriptPaths) {
        $path = Join-Path $refundWorkspace $relative
        $sql = Get-Content -LiteralPath $path -Raw
        if (([regex]::Matches($sql,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
            ([regex]::Matches($sql,'(?im)^COMMIT;')).Count -ne 1 -or $sql -match '(?im)^\s*(DROP TABLE|TRUNCATE|DELETE FROM)\b') {
            throw 'Unexpected migration script structure.'
        }
        Copy-Item -LiteralPath $path -Destination (Join-Path $refundOutput (Split-Path -Leaf $path))
        $refundScripts += @{ Name=(Split-Path -Leaf $path); Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;
            Sql=($sql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$','') }
    }
    $refundBackup = Read-RefundDeployment @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-jahez-refunds-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,CAST(0 AS bit) AS RestoreVerifyOnlyPerformed,SYSUTCDATETIME() AS CompletedAtUtc;
'@ 180
    $refundBackup.WriteXml((Join-Path $refundOutput 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Write-Output 'Server COPY_ONLY checksum backup completed. Applying the two reviewed migrations.'
    $refundTransaction = $refundConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-RefundDeployment @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'jahez-refunds-and-partial-repayments-deployment',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire migration lock.',1;
'@)
    [void](Get-RefundMigrationState)
    [void](Read-RefundDeployment $refundPreflightSql)
    $refundMetadata = Read-RefundDeployment @'
SELECT SCHEMA_NAME(t.schema_id) AS SchemaName,t.name AS TableName,c.name AS ColumnName,c.system_type_id AS TypeId
FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id
WHERE SCHEMA_NAME(t.schema_id) IN(N'jahez_v2',N'jahez_v3') OR (SCHEMA_NAME(t.schema_id)=N'app' AND t.name IN(N'PlatformRiderAccounts',N'RiderClientAssignments'))
ORDER BY SchemaName,TableName,c.column_id;
'@
    $refundSnapshots = @(); $refundNumber = 0
    foreach ($group in ($refundMetadata.Tables[0].Rows | Group-Object { $_.SchemaName + '.' + $_.TableName })) {
        $schema = $group.Group[0].SchemaName.Replace(']',']]'); $table = $group.Group[0].TableName.Replace(']',']]')
        $target = "[$schema].[$table]"; $temporary = '#BeforeRefundDeployment' + $refundNumber++
        $select = @($group.Group | ForEach-Object { $name='[' + $_.ColumnName.Replace(']',']]') + ']'; if ($_.TypeId -eq 189) { "CONVERT(binary(8),$name) AS $name" } else { $name } }) -join ','
        $compare = @($group.Group | Where-Object { !($target -eq '[jahez_v2].[TransferredDebts]' -and $_.ColumnName -eq 'RowVersion' -and $refundExpected[0] -in $refundPending) } |
            ForEach-Object { '[' + $_.ColumnName.Replace(']',']]') + ']' }) -join ','
        [void](Read-RefundDeployment "SELECT $select INTO $temporary FROM $target WITH(UPDLOCK,HOLDLOCK);")
        $refundSnapshots += @{ Target=$target; Temporary=$temporary; Columns=$compare }
    }
    foreach ($script in $refundScripts) { [void](Read-RefundDeployment $script.Sql) }
    foreach ($snapshot in $refundSnapshots) {
        $columns=$snapshot.Columns; $target=$snapshot.Target; $temporary=$snapshot.Temporary
        [void](Read-RefundDeployment "IF EXISTS(SELECT $columns FROM $temporary EXCEPT SELECT $columns FROM $target) OR EXISTS(SELECT $columns FROM $target EXCEPT SELECT $columns FROM $temporary) THROW 51000,'Existing records changed: $target',1;")
        if ($target -eq '[jahez_v2].[TransferredDebts]' -and $refundExpected[0] -in $refundPending) {
            [void](Read-RefundDeployment "IF EXISTS(SELECT Id,RowVersion FROM $temporary WHERE PaidAtUtc IS NULL EXCEPT SELECT Id,RowVersion FROM $target WHERE PaidAtUtc IS NULL) THROW 51000,'Unpaid debt rowversions changed.',1;")
        }
    }
    if ($refundExpected[0] -in $refundPending) {
        [void](Read-RefundDeployment @'
IF EXISTS(SELECT 1 FROM jahez_v2.TransferredDebts WHERE PaidAmount<>CASE WHEN PaidAtUtc IS NULL THEN 0 ELSE Amount END)
    OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements WHERE DashboardDebtPaid IS NOT NULL OR CommissionPaid IS NOT NULL)
    THROW 51000,'Legacy partial-payment backfill is inconsistent.',1;
'@)
    }
    if ($refundExpected[1] -in $refundPending) {
        [void](Read-RefundDeployment "IF EXISTS(SELECT 1 FROM jahez_v2.AccountFeeRefunds) THROW 51000,'Deployment unexpectedly created refunds.',1;")
    }
    $refundVerified = Read-RefundDeployment $refundVerifySql
    $refundVerified.WriteXml((Join-Path $refundOutput 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $refundTransaction.Commit(); $refundTransaction.Dispose(); $refundTransaction = $null
    $refundConnection.Close(); $refundConnection.Open()
    $refundPostCommit = Read-RefundDeployment $refundVerifySql
    $refundAfterState = Get-RefundMigrationState
    $refundPostCommit.WriteXml((Join-Path $refundOutput 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $refundAfterState.WriteXml((Join-Path $refundOutput 'migration-history-after.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $refundAfterApplication = @($refundAfterState.Tables[0].Rows | ForEach-Object MigrationId)
    if (@($refundExpected | Where-Object { $_ -notin $refundAfterApplication }).Count -ne 0) { throw 'Post-commit migration history is incomplete.' }
    $refundReport = [ordered]@{
        Server=$refundConnection.DataSource; Database=$refundConnection.Database; VerifiedAtUtc=[DateTime]::UtcNow.ToString('o')
        AppliedMigrations=$refundPending; ApplicationPending=@(); IdentityPending=@(); PostCommitVerified=$true
        ExistingRecordsComparedAndPreserved=$true; ProtectedTables=$refundSnapshots.Count
        MoneyMovements=[long]$refundPostCommit.Tables[2].Rows[0].MoneyMovements
        LedgerEntries=[long]$refundPostCommit.Tables[3].Rows[0].LedgerEntries
        TransferredDebts=[long]$refundPostCommit.Tables[4].Rows[0].Debts
        Refunds=[long]$refundPostCommit.Tables[1].Rows[0].Refunds
        ServerBackupCompleted=$true; RestoreVerifyOnlyPerformed=$false; ApiDeployed=$false
        Scripts=@($refundScripts | ForEach-Object { @{ Name=$_.Name; Sha256=$_.Sha256 } }); EvidenceDirectory=$refundOutput
    }
    $refundReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $refundOutput 'report.json') -Encoding UTF8
    $refundReport | ConvertTo-Json -Depth 5
} catch {
    if ($refundTransaction) { try { $refundTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($refundTransaction) { $refundTransaction.Dispose() }
    $refundConnection.Dispose()
}
