param([switch]$Apply, [switch]$ReuseServerBackup)

$ErrorActionPreference = 'Stop'
$treasuryWorkspace = Split-Path -Parent $PSScriptRoot
$treasuryConfig = Get-Content -LiteralPath (Join-Path $treasuryWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$treasuryConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $treasuryConfig.ConnectionStrings.LogisticsDatabase }
$treasuryConnection = [System.Data.SqlClient.SqlConnection]::new($treasuryConnectionText)
$treasuryTransaction = $null
$treasuryOutput = Join-Path $treasuryWorkspace 'outputs/jahez-treasury-hosted-deployment-20261019'
$treasuryScripts = @('database/scripts/jahez-treasury-v3-application.sql', 'database/scripts/jahez-treasury-v3-identity.sql')
$treasuryExpectedApplication = @('20261019075320_AddJahezTreasuryV3', '20261019114428_UseGranularJahezTreasuryPermissions')
$treasuryExpectedIdentity = @('20261019075332_GrantJahezTreasuryPermissions', '20261019114451_UseGranularJahezTreasuryPermissions')

function Read-TreasuryData([string]$Sql, [int]$Timeout = 120) {
    $command = $treasuryConnection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $Timeout
    $command.Transaction = $treasuryTransaction
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [System.Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

$treasuryPreflightSql = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261018144507_AddJahezSettlementCommissionStart')
 OR NOT EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261017095013_GrantNinjaPermissions')
 THROW 51000,'The required preceding migrations are missing.',1;
IF (SELECT COUNT(*) FROM app.Sponsors WHERE IsDeleted=0 AND Id IN
 ('019c18d5-62e1-7000-8000-000000000040','019c18d5-62e1-7000-8000-000000000041','019c18d5-62e1-7000-8000-000000000042'))<>3
 THROW 51000,'Required sponsor records are missing.',1;
IF NOT EXISTS(SELECT 1 FROM app.Sponsors WHERE Id='019c18d5-62e1-7000-8000-000000000040' AND EmployerIdentityNumber=N'7038745530')
 THROW 51000,'Trading sponsor identity does not match.',1;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261019075320_AddJahezTreasuryV3')
 AND (EXISTS(SELECT 1 FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3'))
 OR COL_LENGTH(N'jahez_v2.CashboxHandovers',N'TreasuryVersion') IS NOT NULL)
 THROW 51000,'Partial treasury schema found without migration history.',1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT MigrationId,ProductVersion FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId;
SELECT COUNT_BIG(*) AS Movements,COALESCE(SUM(Amount),0) AS MovementAmount FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerEntries,COALESCE(SUM(Amount),0) AS LedgerAmount FROM jahez_v2.AccountingEntries;
SELECT Status,COUNT_BIG(*) AS Handovers FROM jahez_v2.CashboxHandovers GROUP BY Status;
'@

$treasuryVerifySql = @'
IF (SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN
 (N'20261019075320_AddJahezTreasuryV3',N'20261019114428_UseGranularJahezTreasuryPermissions'))<>2
 OR (SELECT COUNT(*) FROM migration.__IdentityMigrationsHistory WHERE MigrationId IN
 (N'20261019075332_GrantJahezTreasuryPermissions',N'20261019114451_UseGranularJahezTreasuryPermissions'))<>2
 THROW 51000,'Treasury migrations are incomplete.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3'))<>8
 THROW 51000,'Expected eight treasury tables.',1;
IF (SELECT COUNT(*) FROM jahez_v3.TreasurySubAccounts)<>23
 THROW 51000,'Expected the 23 seeded treasury accounts.',1;
IF NOT EXISTS(SELECT 1 FROM jahez_v3.State WHERE Id='019d0000-0000-7000-8000-000000000001' AND IsActive=0 AND ActivatedAtUtc IS NULL AND NextHandoverNumber=1)
 THROW 51000,'Treasury must remain inactive after additive deployment.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.Postings) OR EXISTS(SELECT 1 FROM jahez_v3.Ownership)
 OR EXISTS(SELECT 1 FROM jahez_v3.HandoverLines) OR EXISTS(SELECT 1 FROM jahez_v3.HandoverPostings)
 OR EXISTS(SELECT 1 FROM jahez_v3.BalanceVerifications) OR EXISTS(SELECT 1 FROM jahez_v3.PetroEvidence)
 THROW 51000,'This deployment does not create historical or financial postings.',1;
IF (SELECT COUNT(*) FROM sys.triggers WHERE parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) AND is_disabled=0)<>8
 THROW 51000,'Treasury protection triggers are missing or disabled.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE schema_id=SCHEMA_ID(N'jahez_v3') AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>15
 THROW 51000,'Treasury restrictive foreign keys are not verified.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id=SCHEMA_ID(N'jahez_v3') AND is_disabled=0 AND is_not_trusted=0)<>4
 THROW 51000,'Treasury check constraints are not verified.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id IN(OBJECT_ID(N'jahez_v3.State'),OBJECT_ID(N'jahez_v3.TreasurySubAccounts')) AND name=N'RowVersion' AND system_type_id=189)<>2
 THROW 51000,'Treasury SQL rowversion columns are missing.',1;
IF COL_LENGTH(N'jahez_v2.Operations',N'ExpectedSponsorId') IS NULL
 OR COL_LENGTH(N'jahez_v2.CashboxHandovers',N'DocumentNumber') IS NULL
 OR COL_LENGTH(N'jahez_v2.CashboxHandovers',N'RequestedByNameAr') IS NULL
 OR COL_LENGTH(N'jahez_v2.CashboxHandovers',N'AccountantNameAr') IS NULL
 OR EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers WHERE TreasuryVersion<>2 OR DocumentNumber IS NOT NULL OR RequestedByNameAr IS NOT NULL OR AccountantNameAr IS NOT NULL)
 THROW 51000,'Legacy handover compatibility was not preserved.',1;
IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) AND is_disabled=1)
 THROW 51000,'A treasury index is disabled.',1;
DECLARE @keys TABLE([Key] nvarchar(200) NOT NULL);
INSERT @keys VALUES(N'jahez.treasury.read'),(N'jahez.treasury.accounts.create'),(N'jahez.treasury.accounts.update'),
 (N'jahez.treasury.transfers.create'),(N'jahez.treasury.entries.create'),(N'jahez.treasury.petroapp-evidence.create'),
 (N'jahez.treasury.adjust'),(N'jahez.financial-reports.read'),(N'jahez.financial-reports.export');
IF (SELECT COUNT(*) FROM platform.PermissionDefinitions d JOIN @keys k ON d.[Key]=k.[Key]
 WHERE d.IsDeleted=0 AND d.IsDeprecated=0 AND d.RequiresClientScope=1 AND d.IsHighTrust=1 AND d.IsSensitive=1)<>9
 THROW 51000,'Granular treasury permission definitions are incomplete.',1;
IF (SELECT COUNT(*) FROM [identity].[RolePermissions] p JOIN @keys k ON p.PermissionKey=k.[Key]
 WHERE p.IsDeleted=0 AND p.RoleId='019c18d5-62e1-7000-9000-000000000001')<>9
 THROW 51000,'System administrator treasury grants are incomplete.',1;
IF EXISTS(SELECT 1 FROM platform.PermissionDefinitions WHERE IsDeleted=0 AND [Key]=N'jahez.treasury.manage')
 OR EXISTS(SELECT 1 FROM [identity].[RolePermissions] WHERE IsDeleted=0 AND PermissionKey=N'jahez.treasury.manage')
 THROW 51000,'The deprecated broad treasury permission remains.',1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId LIKE N'20261019%' ORDER BY MigrationId;
SELECT MigrationId,ProductVersion FROM migration.__IdentityMigrationsHistory WHERE MigrationId LIKE N'20261019%' ORDER BY MigrationId;
SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3') ORDER BY name;
SELECT Code,NameAr,IsPosting,Location,Purpose FROM jahez_v3.TreasurySubAccounts ORDER BY Code;
SELECT IsActive,ActivatedAtUtc,NextHandoverNumber FROM jahez_v3.State;
SELECT d.[Key],d.NameAr,d.RequiresClientScope FROM platform.PermissionDefinitions d JOIN @keys k ON d.[Key]=k.[Key] ORDER BY d.[Key];
SELECT COUNT_BIG(*) AS Movements,COALESCE(SUM(Amount),0) AS MovementAmount FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) AS LedgerEntries,COALESCE(SUM(Amount),0) AS LedgerAmount FROM jahez_v2.AccountingEntries;
SELECT Status,COUNT_BIG(*) AS Handovers FROM jahez_v2.CashboxHandovers GROUP BY Status;
SELECT t.name AS TableName,tr.name AS TriggerName,tr.is_disabled FROM sys.triggers tr JOIN sys.tables t ON tr.parent_id=t.object_id WHERE t.schema_id=SCHEMA_ID(N'jahez_v3');
SELECT name,is_unique,has_filter,filter_definition FROM sys.indexes WHERE object_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) ORDER BY object_id,index_id;
'@

try {
    $treasuryConnection.Open()
    if ($treasuryConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $treasuryConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted database target; refusing treasury deployment.'
    }
    $before = Read-TreasuryData $treasuryPreflightSql
    # Require that the only pending migrations are this reviewed release, for both contexts.
    foreach ($context in @(
        @{ Path='src/LogisticsERP.Infrastructure/Persistence/Migrations/Application'; History=$before.Tables[0]; Allowed=$treasuryExpectedApplication },
        @{ Path='src/LogisticsERP.Infrastructure/Identity/Migrations'; History=$before.Tables[1]; Allowed=$treasuryExpectedIdentity }
    )) {
        $local = @(Get-ChildItem -LiteralPath (Join-Path $treasuryWorkspace $context.Path) -Filter '*.cs' |
            Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
        $applied = @($context.History.Rows | ForEach-Object MigrationId)
        if (@($local | Where-Object { $_ -notin $applied -and $_ -notin $context.Allowed }).Count -gt 0 -or
            @($applied | Where-Object { $_ -notin $local }).Count -gt 0) { throw 'Unexpected migration history or unreviewed pending migration.' }
    }
    Write-Output ('Verified hosted target. Money movements: {0}; amount: {1}; confirmed/pending handovers: {2} groups.' -f
        $before.Tables[2].Rows[0].Movements,$before.Tables[2].Rows[0].MovementAmount,$before.Tables[4].Rows.Count)
    if (!$Apply) { Write-Output 'Read-only preflight complete; no schema or financial data changed.'; return }

    [void](New-Item -ItemType Directory -Path $treasuryOutput -Force)
    $before.WriteXml((Join-Path $treasuryOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    if ($ReuseServerBackup) {
        $backupPath = Join-Path $treasuryOutput 'server-backup.xml'
        if (!(Test-Path -LiteralPath $backupPath)) { throw 'No completed backup evidence available for reuse.' }
        [xml]$backupEvidence = Get-Content -LiteralPath $backupPath -Raw
        if ($backupEvidence.NewDataSet.Table.BackupCommandCompleted -ne 'true' -or
            $backupEvidence.NewDataSet.Table.ServerBackupPath -notlike '*\db67927-before-jahez-treasury-*.bak') {
            throw 'Existing backup evidence does not match this database deployment.'
        }
        Write-Output 'Reusing the completed server checksum backup from the preceding rolled-back deployment attempt.'
    } else {
    $backup = Read-TreasuryData @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-jahez-treasury-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,CAST(0 AS bit) AS RestoreVerifyOnlyPerformed,TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00') AS CompletedAtUtc;
'@ 180
    # The hosted login cannot run RESTORE VERIFYONLY (CREATE DATABASE in master is denied).
    $backup.WriteXml((Join-Path $treasuryOutput 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Write-Output 'Server COPY_ONLY backup with checksums completed. Restore verification requires hosting administrator permissions.'
    }

    $scripts = @()
    foreach ($relative in $treasuryScripts) {
        $path = Join-Path $treasuryWorkspace $relative
        $sql = Get-Content -LiteralPath $path -Raw
        if (([regex]::Matches($sql,'(?im)^BEGIN TRANSACTION;')).Count -ne 2 -or
            ([regex]::Matches($sql,'(?im)^COMMIT;')).Count -ne 2 -or $sql -match '(?im)^\s*(DROP TABLE|TRUNCATE|DELETE FROM)\b') {
            throw 'Unexpected migration script structure.'
        }
        Copy-Item -LiteralPath $path -Destination (Join-Path $treasuryOutput (Split-Path -Leaf $path))
        $scripts += @{ Path=$path; Sql=($sql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$',''); Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    }

    $treasuryTransaction = $treasuryConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-TreasuryData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'jahez-treasury-v3-deployment',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Treasury deployment lock could not be acquired.',1;
'@)
    [void](Read-TreasuryData $treasuryPreflightSql)

    $tables = (Read-TreasuryData @'
SELECT s.name AS SchemaName,t.name AS TableName FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name=N'jahez_v2' OR (s.name=N'app' AND t.name IN(N'PlatformRiderAccounts',N'Sponsors',N'RiderClientAssignments'))
 OR (s.name=N'platform' AND t.name=N'PermissionDefinitions') OR (s.name=N'identity' AND t.name=N'RolePermissions')
ORDER BY s.name,t.name;
'@).Tables[0]
    $preservation = @()
    $index = 0
    foreach ($table in $tables.Rows) {
        $schema = ([string]$table.SchemaName).Replace(']',']]')
        $name = ([string]$table.TableName).Replace(']',']]')
        $qualified = "[$schema].[$name]"
        $columnsData = Read-TreasuryData "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$qualified') ORDER BY column_id;"
        $columns = @($columnsData.Tables[0].Rows | ForEach-Object { '[' + ([string]$_.name).Replace(']',']]') + ']' }) -join ','
        # Existing non-treasury catalog/grant rows must remain unchanged; release seeds are checked separately.
        $where = if ($name -eq 'PermissionDefinitions') { " WHERE [Id] NOT IN ('019c18d5-62e1-7000-a000-000000000750','019c18d5-62e1-7000-a000-000000000751','019c18d5-62e1-7000-a000-000000000752','019c18d5-62e1-7000-a000-000000000753','019c18d5-62e1-7000-a000-000000000754','019c18d5-62e1-7000-a000-000000000755','019c18d5-62e1-7000-a000-000000000756','019c18d5-62e1-7000-a000-000000000757','019c18d5-62e1-7000-a000-000000000758')" }
            elseif ($name -eq 'RolePermissions') { " WHERE [Id] NOT IN ('019c18d5-62e1-7000-b000-000000000272','019c18d5-62e1-7000-b000-000000000273','019c18d5-62e1-7000-b000-000000000274','019c18d5-62e1-7000-b000-000000000275','019c18d5-62e1-7000-b000-000000000276','019c18d5-62e1-7000-b000-000000000277','019c18d5-62e1-7000-b000-000000000278','019c18d5-62e1-7000-b000-000000000279','019c18d5-62e1-7000-b000-000000000280')" }
            else { '' }
        $temp = '#TreasuryPreserve' + $index++
        [void](Read-TreasuryData "SELECT $columns INTO $temp FROM $qualified WITH(TABLOCKX,HOLDLOCK)$where;")
        $preservation += @{ Table=$qualified; Columns=$columns; Temp=$temp; Where=$where }
    }

    foreach ($script in $scripts) {
        foreach ($batch in [regex]::Split($script.Sql,'(?im)^GO\s*$')) {
            if (![string]::IsNullOrWhiteSpace($batch)) { [void](Read-TreasuryData $batch) }
        }
    }
    foreach ($item in $preservation) {
        [void](Read-TreasuryData @"
IF EXISTS(SELECT $($item.Columns) FROM $($item.Temp) EXCEPT SELECT $($item.Columns) FROM $($item.Table)$($item.Where))
 OR EXISTS(SELECT $($item.Columns) FROM $($item.Table)$($item.Where) EXCEPT SELECT $($item.Columns) FROM $($item.Temp))
 THROW 51000,'Existing data changed during additive treasury deployment.',1;
"@)
    }
    $verified = Read-TreasuryData $treasuryVerifySql
    $verified.WriteXml((Join-Path $treasuryOutput 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $treasuryTransaction.Commit(); $treasuryTransaction.Dispose(); $treasuryTransaction = $null
    $treasuryConnection.Close(); $treasuryConnection.Open()
    $postCommit = Read-TreasuryData $treasuryVerifySql
    $postCommit.WriteXml((Join-Path $treasuryOutput 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{
        Server=$treasuryConnection.DataSource; Database=$treasuryConnection.Database; VerifiedAtUtc=[DateTime]::UtcNow.ToString('o')
        ApplicationMigrations=$treasuryExpectedApplication; IdentityMigrations=$treasuryExpectedIdentity
        PostCommitVerified=$true; PreservedBusinessTables=$preservation.Count; ExistingDataComparedInBothDirections=$true
        TreasuryActive=$false; HistoricalAttributionOrBackfillPerformed=$false; ApiBinaryDeployed=$false
        ServerBackupCompleted=$true; BackupIncludesChecksums=$true; RestoreVerifyOnlyPerformed=$false
        BackupVerificationLimitation='Hosted login lacks CREATE DATABASE permission in master required by RESTORE VERIFYONLY.'
        MoneyMovements=[long]$postCommit.Tables[6].Rows[0].Movements; MovementAmount=[decimal]$postCommit.Tables[6].Rows[0].MovementAmount
        LedgerEntries=[long]$postCommit.Tables[7].Rows[0].LedgerEntries; LedgerAmount=[decimal]$postCommit.Tables[7].Rows[0].LedgerAmount
        ScriptHashes=@($scripts | ForEach-Object { @{File=(Split-Path -Leaf $_.Path);Sha256=$_.Sha256} })
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $treasuryOutput 'report.json') -Encoding UTF8
    $report | ConvertTo-Json -Depth 6
} catch {
    if ($treasuryTransaction) { try { $treasuryTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($treasuryTransaction) { $treasuryTransaction.Dispose() }
    $treasuryConnection.Dispose()
}
