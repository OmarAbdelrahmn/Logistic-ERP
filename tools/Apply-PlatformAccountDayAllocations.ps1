param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$dayWorkspace = Split-Path -Parent $PSScriptRoot
$dayMigration = '20261021071103_AddPlatformAccountDayAllocations'
$dayScriptPath = Join-Path $dayWorkspace 'database/scripts/platform-account-day-allocations.sql'
$dayReportPath = Join-Path $dayWorkspace 'database/scripts/platform-account-day-allocation-verification.sql'
$dayConfig = Get-Content -LiteralPath (Join-Path $dayWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$dayConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $dayConfig.ConnectionStrings.LogisticsDatabase }
$dayConnection = [System.Data.SqlClient.SqlConnection]::new($dayConnectionText)
$script:dayTransaction = $null
$dayOutput = Join-Path $dayWorkspace ('outputs/shared-day-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))

function Read-DayData([string]$Sql, [int]$Timeout = 120) {
    $command = $dayConnection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = $Timeout
    $command.Transaction = $script:dayTransaction
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [System.Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

$dayPreflight = @'
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261019114428_UseGranularJahezTreasuryPermissions')
 THROW 51000,'Required preceding application migration is missing.',1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261021071103_AddPlatformAccountDayAllocations')
 AND (OBJECT_ID(N'app.PlatformAccountDayUsages',N'U') IS NOT NULL
 OR OBJECT_ID(N'chefz.DailyPerformanceSources',N'U') IS NOT NULL OR OBJECT_ID(N'chefz.ImportedOrderRows',N'U') IS NOT NULL
 OR OBJECT_ID(N'ninja.NinjaDailyPerformanceSources',N'U') IS NOT NULL OR OBJECT_ID(N'ninja.NinjaDailyDeductionSources',N'U') IS NOT NULL
 OR COL_LENGTH(N'chefz.DailyPerformance',N'SourceId') IS NOT NULL OR COL_LENGTH(N'ninja.NinjaDailyPerformance',N'SourceId') IS NOT NULL
 OR COL_LENGTH(N'ninja.NinjaDailyDeductions',N'SourceId') IS NOT NULL OR COL_LENGTH(N'jahez_v2.Operations',N'DayUsageJson') IS NOT NULL
 OR COL_LENGTH(N'jahez_v2.DailyOrders',N'DayUsageId') IS NOT NULL)
 THROW 51000,'Partial shared-day schema without migration history; reconcile before deployment.',1;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT N'NinjaPerformance' Dataset,COUNT_BIG(*) Records FROM ninja.NinjaDailyPerformance
UNION ALL SELECT N'NinjaDeductions',COUNT_BIG(*) FROM ninja.NinjaDailyDeductions
UNION ALL SELECT N'ChefzPerformance',COUNT_BIG(*) FROM chefz.DailyPerformance
UNION ALL SELECT N'ChefzSettlements',COUNT_BIG(*) FROM chefz.WalletSettlements
UNION ALL SELECT N'AccountAssignments',COUNT_BIG(*) FROM app.RiderClientAssignments;
'@

$daySchemaVerify = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261021071103_AddPlatformAccountDayAllocations')
 THROW 51000,'Shared-day migration history is missing.',1;
DECLARE @new TABLE(Id int NOT NULL);
INSERT @new VALUES (OBJECT_ID(N'app.PlatformAccountDayUsages')),(OBJECT_ID(N'chefz.DailyPerformanceSources')),
 (OBJECT_ID(N'chefz.ImportedOrderRows')),(OBJECT_ID(N'ninja.NinjaDailyPerformanceSources')),(OBJECT_ID(N'ninja.NinjaDailyDeductionSources'));
IF (SELECT COUNT(*) FROM sys.tables WHERE object_id IN(SELECT Id FROM @new))<>5 THROW 51000,'Five shared-day tables are required.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN(SELECT Id FROM @new) AND is_disabled=0 AND is_not_trusted=0)<>5
 THROW 51000,'Shared-day amount constraints are missing or untrusted.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE (parent_object_id IN(SELECT Id FROM @new)
 OR name IN(N'FK_DailyOrders_PlatformAccountDayUsages_DayUsageId',N'FK_DailyPerformance_DailyPerformanceSources_SourceId',
 N'FK_NinjaDailyDeductions_NinjaDailyDeductionSources_SourceId',N'FK_NinjaDailyPerformance_NinjaDailyPerformanceSources_SourceId'))
 AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>15
 THROW 51000,'Shared-day restrictive foreign keys are missing or untrusted.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id IN(OBJECT_ID(N'app.PlatformAccountDayUsages'),OBJECT_ID(N'chefz.DailyPerformanceSources'))
 AND name=N'RowVersion' AND system_type_id=189)<>2 THROW 51000,'Shared-day rowversions are missing.',1;
IF COL_LENGTH(N'jahez_v2.Operations',N'DayUsageJson') IS NULL OR COL_LENGTH(N'jahez_v2.DailyOrders',N'DayUsageId') IS NULL
 THROW 51000,'Jahez usage evidence columns are missing.',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE is_unique=1 AND is_disabled=0 AND name IN(
 N'IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId',N'IX_NinjaDailyPerformance_SourceId_RiderClientAssignmentId',
 N'IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId',N'IX_NinjaDailyDeductions_SourceId_RiderClientAssignmentId',
 N'IX_DailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId',N'IX_DailyPerformance_SourceId_RiderClientAssignmentId',
 N'IX_DailyPerformanceSources_PlatformRiderAccountId_ReportDate',N'IX_NinjaDailyPerformanceSources_PlatformRiderAccountId_ReportDate',
 N'IX_NinjaDailyDeductionSources_PlatformRiderAccountId_ReportDate',N'IX_ImportedOrderRows_ImportId_SourceRowNumber',
 N'IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_RiderClientAssignmentId',N'IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_Sequence'))<>12
 THROW 51000,'Shared-day allocation uniqueness indexes are incomplete.',1;
IF EXISTS(SELECT 1 FROM ninja.NinjaDailyPerformance p LEFT JOIN ninja.NinjaDailyPerformanceSources s ON s.Id=p.SourceId
 WHERE s.Id IS NULL OR p.Id<>p.SourceId OR p.TotalOrders<>s.TotalOrders OR p.TotalWorkingHours<>s.TotalWorkingHours
 OR p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId)
 OR EXISTS(SELECT 1 FROM ninja.NinjaDailyDeductions p LEFT JOIN ninja.NinjaDailyDeductionSources s ON s.Id=p.SourceId
 WHERE s.Id IS NULL OR p.Id<>p.SourceId OR p.TotalCollectedAmountAtDelivery<>s.TotalCollectedAmountAtDelivery OR p.TotalDeductions<>s.TotalDeductions
 OR p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId)
 OR EXISTS(SELECT 1 FROM chefz.DailyPerformance p LEFT JOIN chefz.DailyPerformanceSources s ON s.Id=p.SourceId
 WHERE s.Id IS NULL OR p.Id<>p.SourceId OR p.TotalValidOffers<>s.TotalValidOffers OR p.AcceptedOffers<>s.AcceptedOffers
 OR p.DeclinedOffers<>s.DeclinedOffers OR p.MissedOffers<>s.MissedOffers OR p.AcceptanceRate<>s.AcceptanceRate
 OR p.PlatformRiderAccountId<>s.PlatformRiderAccountId OR p.ReportDate<>s.ReportDate OR p.ImportId<>s.ImportId)
 THROW 51000,'Historical daily observations were not preserved exactly in sources.',1;
IF EXISTS(SELECT 1 FROM chefz.ImportedOrderRows) OR EXISTS(SELECT 1 FROM jahez_v2.Operations WHERE DayUsageJson IS NOT NULL)
 OR EXISTS(SELECT 1 FROM app.PlatformAccountDayUsages WHERE OrdersCount IS NOT NULL OR WorkingHours IS NOT NULL
 OR CollectedAmount IS NOT NULL OR DeductionsAmount IS NOT NULL OR TotalValidOffers IS NOT NULL OR DeclinedOffers IS NOT NULL OR MissedOffers IS NOT NULL)
 THROW 51000,'Migration must not invent usage figures or historical money rows.',1;
SELECT N'ChefzPerformanceSources' Dataset,COUNT_BIG(*) Records FROM chefz.DailyPerformanceSources
UNION ALL SELECT N'NinjaPerformanceSources',COUNT_BIG(*) FROM ninja.NinjaDailyPerformanceSources
UNION ALL SELECT N'NinjaDeductionSources',COUNT_BIG(*) FROM ninja.NinjaDailyDeductionSources
UNION ALL SELECT N'DayUsages',COUNT_BIG(*) FROM app.PlatformAccountDayUsages
UNION ALL SELECT N'OrderRows',COUNT_BIG(*) FROM chefz.ImportedOrderRows;
'@

function Invoke-DayUpgrade([bool]$Commit) {
    $script:dayTransaction = $dayConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    try {
        [void](Read-DayData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'platform-account-day-allocation-deployment',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire the shared-day schema deployment lock.',1;
'@)
        [void](Read-DayData $dayPreflight)
        $tables = (Read-DayData @'
SELECT s.name SchemaName,t.name TableName FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id
WHERE s.name IN(N'chefz',N'ninja',N'jahez_v2')
 OR (s.name=N'app' AND t.name IN(N'PlatformRiderAccounts',N'RiderClientAssignments'))
ORDER BY s.name,t.name;
'@).Tables[0]
        $preserve = @(); $index = 0
        foreach ($table in $tables.Rows) {
            $qualified = '[' + ([string]$table.SchemaName).Replace(']',']]') + '].[' + ([string]$table.TableName).Replace(']',']]') + ']'
            $columnData = Read-DayData "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$qualified') ORDER BY column_id;"
            $columns = @($columnData.Tables[0].Rows | ForEach-Object { '[' + ([string]$_.name).Replace(']',']]') + ']' }) -join ','
            $temp = '#DayPreserve' + $index++
            [void](Read-DayData "SELECT $columns INTO $temp FROM $qualified WITH(TABLOCKX,HOLDLOCK);")
            $preserve += @{Table=$qualified; Columns=$columns; Temp=$temp}
        }
        foreach ($batch in [regex]::Split($dayMigrationSql,'(?im)^GO\s*$')) {
            if (![string]::IsNullOrWhiteSpace($batch)) { [void](Read-DayData $batch) }
        }
        foreach ($item in $preserve) {
            [void](Read-DayData @"
IF EXISTS(SELECT $($item.Columns) FROM $($item.Temp) EXCEPT SELECT $($item.Columns) FROM $($item.Table))
 OR EXISTS(SELECT $($item.Columns) FROM $($item.Table) EXCEPT SELECT $($item.Columns) FROM $($item.Temp))
 THROW 51000,'An existing business row changed during shared-day deployment.',1;
"@)
        }
        $schema = Read-DayData $daySchemaVerify
        $reconciliation = Read-DayData $dayReconciliationSql
        # Sequence ambiguity and legacy FullTime overlaps are review lists, never automatic data corrections.
        for ($i=0; $i -lt $reconciliation.Tables.Count; $i++) {
            if ($i -notin @(6,7) -and $reconciliation.Tables[$i].Rows.Count -gt 0) { throw "Shared-day reconciliation dataset $i has mismatches." }
        }
        $mode = if ($Commit) { 'apply' } else { 'rehearsal' }
        $schema.WriteXml((Join-Path $dayOutput "$mode-schema.xml"),[System.Data.XmlWriteMode]::WriteSchema)
        $reconciliation.WriteXml((Join-Path $dayOutput "$mode-reconciliation.xml"),[System.Data.XmlWriteMode]::WriteSchema)
        if ($Commit) { $script:dayTransaction.Commit() } else { $script:dayTransaction.Rollback() }
        return [ordered]@{Mode=$mode; Committed=$Commit; PreservedBusinessTables=$preserve.Count;
            SourceRecords=@($schema.Tables[0].Rows | ForEach-Object { @{Dataset=[string]$_.Dataset; Records=[long]$_.Records} });
            UnresolvedLegacyDays=$reconciliation.Tables[6].Rows.Count; FullTimeOverlapPairs=$reconciliation.Tables[7].Rows.Count}
    } catch {
        try { $script:dayTransaction.Rollback() } catch {}
        throw
    } finally { $script:dayTransaction.Dispose(); $script:dayTransaction=$null }
}

try {
    $dayConnection.Open()
    if ($dayConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $dayConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted database target; refusing shared-day migration.'
    }
    $dayReconciliationSql = Get-Content -LiteralPath $dayReportPath -Raw
    $before = Read-DayData $dayPreflight
    $local = @(Get-ChildItem -LiteralPath (Join-Path $dayWorkspace 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' |
        Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $applied = @($before.Tables[0].Rows | ForEach-Object MigrationId)
    if (@($local | Where-Object { $_ -notin $applied -and $_ -ne $dayMigration }).Count -gt 0 -or
        @($applied | Where-Object { $_ -notin $local }).Count -gt 0) { throw 'Unexpected application migration history; refusing an unreviewed upgrade.' }
    if ($dayMigration -in $applied) {
        $existing = Read-DayData $dayReconciliationSql
        Write-Output 'Shared-day migration is already applied; no update was performed.'
        return
    }
    Write-Output ('Verified hosted database. Chefz performance records: {0}; settlements: {1}.' -f $before.Tables[1].Rows[2].Records,$before.Tables[1].Rows[3].Records)
    if (!$Apply) { Write-Output 'Read-only preflight complete.'; return }

    $dayMigrationSql = Get-Content -LiteralPath $dayScriptPath -Raw
    $dayReconciliationSql = Get-Content -LiteralPath $dayReportPath -Raw
    if (([regex]::Matches($dayMigrationSql,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($dayMigrationSql,'(?im)^COMMIT;')).Count -ne 1 -or
        $dayMigrationSql -match '(?im)^\s*(DROP TABLE|TRUNCATE|DELETE FROM)\b') { throw 'Unexpected migration script structure.' }
    $dayMigrationSql = $dayMigrationSql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$',''
    [void](New-Item -ItemType Directory -Path $dayOutput -Force)
    $before.WriteXml((Join-Path $dayOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $dayScriptPath -Destination (Join-Path $dayOutput 'migration.sql')
    $backup = Read-DayData @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup folder unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-shared-day-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath,CAST(1 AS bit) BackupCommandCompleted,SYSUTCDATETIME() CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $dayOutput 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Write-Output 'Server COPY_ONLY backup with checksums completed.'
    $rehearsal = Invoke-DayUpgrade $false
    $afterRehearsal = Read-DayData $dayPreflight
    if ($dayMigration -in @($afterRehearsal.Tables[0].Rows | ForEach-Object MigrationId)) { throw 'Rehearsal did not roll back migration history.' }
    Write-Output 'Rehearsal passed: original business rows and financial data preserved; schema rolled back.'
    $result = Invoke-DayUpgrade $true
    $dayConnection.Close(); $dayConnection.Open()
    $verified = Read-DayData $daySchemaVerify
    $verified.WriteXml((Join-Path $dayOutput 'postcommit-schema.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $postReconciliation = Read-DayData $dayReconciliationSql
    $postReconciliation.WriteXml((Join-Path $dayOutput 'postcommit-reconciliation.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    for ($i=0; $i -lt $postReconciliation.Tables.Count; $i++) {
        if ($i -notin @(6,7) -and $postReconciliation.Tables[$i].Rows.Count -gt 0) { throw "Post-commit reconciliation dataset $i has mismatches." }
    }
    $report = [ordered]@{Database=$dayConnection.Database; Server=$dayConnection.DataSource; Migration=$dayMigration;
        CompletedAtUtc=[DateTime]::UtcNow.ToString('o'); BackupWithChecksums=$true; RestoreVerifyOnlyPerformed=$false;
        BackupVerificationLimitation='Hosted login cannot perform RESTORE VERIFYONLY; checksum backup completed.';
        RehearsalRolledBack=$true; PostCommitVerified=$true; ApiDeployed=$false;
        PreservedBusinessTables=$result.PreservedBusinessTables; SourceRecords=$result.SourceRecords;
        UnresolvedLegacyDays=$postReconciliation.Tables[6].Rows.Count; FullTimeOverlapPairs=$postReconciliation.Tables[7].Rows.Count;
        ScriptSha256=(Get-FileHash -LiteralPath $dayScriptPath -Algorithm SHA256).Hash; OutputDirectory=$dayOutput}
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $dayOutput 'report.json') -Encoding UTF8
    $report | ConvertTo-Json -Depth 6
} finally { $dayConnection.Dispose() }
