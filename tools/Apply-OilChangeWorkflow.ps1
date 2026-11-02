param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$settings = Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$cs = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $settings.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($cs)
$script:transaction = $null
$migrations = @('20261031112328_AddOilChangeConcurrency','20261031132700_AddOilChangeEditCancellationWorkflow')
function Query([string]$sql, [int]$timeout = 120) {
    $command = $connection.CreateCommand(); $command.CommandText = $sql; $command.CommandTimeout = $timeout; $command.Transaction = $script:transaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data }
    catch { $inner=$_.Exception.InnerException; if ($inner -is [Data.SqlClient.SqlException]) { throw "SQL error $($inner.Number) at line $($inner.LineNumber): $($inner.Message)" }; throw }
    finally { $adapter.Dispose(); $command.Dispose() }
}
$integrity = @'
SELECT N'operations' AS Dataset,COUNT_BIG(*) AS RecordCount,COALESCE(SUM(TotalCost),0) AS Amount FROM maintenance.OilChangeOperations
UNION ALL SELECT N'usages',COUNT_BIG(*),COALESCE(SUM(TotalCost),0) FROM maintenance.MaterialUsages
UNION ALL SELECT N'vehicle_expenses',COUNT_BIG(*),COALESCE(SUM(TotalAmount),0) FROM maintenance.VehicleExpenses
UNION ALL SELECT N'external_entries',COUNT_BIG(*),COALESCE(SUM(TotalAmount),0) FROM maintenance.ExternalFinancialEntries
UNION ALL SELECT N'barrel_losses',COUNT_BIG(*),COALESCE(SUM(QuantityLiters),0) FROM maintenance.OilBarrelLosses
UNION ALL SELECT N'barrels',COUNT_BIG(*),COALESCE(SUM(RemainingLiters),0) FROM maintenance.OilBarrels
UNION ALL SELECT N'balances',COUNT_BIG(*),COALESCE(SUM(QuantityOnHand),0) FROM maintenance.StockBalances
UNION ALL SELECT N'odometers',COUNT_BIG(*),COALESCE(SUM(CAST(Reading AS decimal(28,2))),0) FROM app.VehicleOdometerReadings;
'@
$verify = @'
IF (SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN (N'20261031112328_AddOilChangeConcurrency',N'20261031132700_AddOilChangeEditCancellationWorkflow'))<>2 THROW 51000,'Migration history incomplete',1;
IF OBJECT_ID(N'maintenance.OilChangeRevisions',N'U') IS NULL THROW 51000,'Revision table missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'maintenance.OilChangeOperations') AND name=N'RowVersion' AND system_type_id=189) THROW 51000,'Operation rowversion missing',1;
IF COL_LENGTH(N'maintenance.OilChangeOperations',N'IsCancelled') IS NULL OR COL_LENGTH(N'maintenance.OilChangeOperations',N'RevisionNumber') IS NULL OR COL_LENGTH(N'maintenance.MaterialUsages',N'EffectiveServiceAtUtc') IS NULL OR COL_LENGTH(N'maintenance.OilBarrelLosses',N'CorrectionRevisionId') IS NULL OR COL_LENGTH(N'app.VehicleOdometerReadings',N'VoidedByRevisionId') IS NULL THROW 51000,'Workflow columns missing',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN(N'CK_VehicleOdometerReadings_Void',N'CK_OilChangeOperations_Lifecycle',N'CK_OilChangeRevisions_Number') AND is_disabled=0 AND is_not_trusted=0)<>3 THROW 51000,'Workflow constraints invalid',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE name IN(N'IX_VehicleExpenses_ReversalOfExpenseId',N'IX_OilBarrelUsageAllocations_ReversalOfAllocationId',N'IX_MaterialUsages_ReversalOfUsageId',N'IX_ExternalFinancialEntries_ReversalOfEntryId') AND is_unique=1 AND has_filter=1 AND is_disabled=0)<>4 THROW 51000,'Reversal uniqueness missing',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'maintenance.OilChangeRevisions') AND is_unique=1 AND name IN(N'IX_OilChangeRevisions_OilChangeOperationId_RevisionNumber',N'IX_OilChangeRevisions_OilChangeOperationId_IdempotencyKey'))<>2 THROW 51000,'Revision uniqueness missing',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE name IN(N'FK_OilBarrelLosses_OilChangeRevisions_CorrectionRevisionId',N'FK_VehicleOdometerReadings_OilChangeRevisions_VoidedByRevisionId') AND is_disabled=0 AND is_not_trusted=0)<>2 THROW 51000,'Evidence foreign keys missing',1;
IF (SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE [Key] IN(N'maintenance.oil.edit',N'maintenance.oil.date_edit',N'maintenance.oil.cancel') AND IsDeleted=0)<>3 THROW 51000,'Workflow permissions missing',1;
SELECT COUNT_BIG(*) AS RevisionRows FROM maintenance.OilChangeRevisions;
'@
try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $connection.Open()
    $history = Query 'SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;'
    $hosted = @($history.Tables[0].Rows | ForEach-Object MigrationId)
    $local = @(Get-ChildItem (Join-Path $root 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $missing = @($local | Where-Object { $_ -notin $hosted -and $_ -notin $migrations }); $unknown = @($hosted | Where-Object { $_ -notin $local })
    if ($missing.Count -or $unknown.Count -or '20261031095204_AllowDailyOdometerCorrections' -notin $hosted) { throw "Unexpected migration history. Missing: $($missing -join ', '); unknown: $($unknown -join ', ')" }
    if (@($migrations | Where-Object { $_ -notin $hosted }).Count -eq 0) { [void](Query $verify); [pscustomobject]@{AlreadyApplied=$true;Verified=$true} | ConvertTo-Json; return }
    [void](Query @'
IF EXISTS(SELECT ReversalOfUsageId FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IS NOT NULL GROUP BY ReversalOfUsageId HAVING COUNT(*)>1) THROW 51000,'Duplicate material reversals',1;
IF EXISTS(SELECT ReversalOfAllocationId FROM maintenance.OilBarrelUsageAllocations WHERE ReversalOfAllocationId IS NOT NULL GROUP BY ReversalOfAllocationId HAVING COUNT(*)>1) THROW 51000,'Duplicate barrel reversals',1;
IF EXISTS(SELECT ReversalOfExpenseId FROM maintenance.VehicleExpenses WHERE ReversalOfExpenseId IS NOT NULL GROUP BY ReversalOfExpenseId HAVING COUNT(*)>1) THROW 51000,'Duplicate vehicle expense reversals',1;
IF EXISTS(SELECT ReversalOfEntryId FROM maintenance.ExternalFinancialEntries WHERE ReversalOfEntryId IS NOT NULL GROUP BY ReversalOfEntryId HAVING COUNT(*)>1) THROW 51000,'Duplicate external reversals',1;
'@)
    $pre = Query $integrity
    if (!$Apply) { [pscustomobject]@{Database=$connection.Database;Pending=@($migrations|Where-Object {$_ -notin $hosted});Preflight='OK';OilOperations=[long]$pre.Tables[0].Rows[0].RecordCount} | ConvertTo-Json; return }
    $output=Join-Path $root ('outputs/oil-workflow-hosted-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')); [void](New-Item -ItemType Directory -Path $output)
    $history.WriteXml((Join-Path $output 'history-before.xml'),[Data.XmlWriteMode]::WriteSchema)
    $sql=Get-Content (Join-Path $root 'database/scripts/oil-change-edit-cancel.sql') -Raw
    if ($sql -match '(?im)^\s*(DROP TABLE|DELETE FROM|TRUNCATE TABLE)\b' -or $sql -notmatch [regex]::Escape($migrations[1])) {throw 'Unexpected migration script scope.'}
    Copy-Item (Join-Path $root 'database/scripts/oil-change-edit-cancel.sql') (Join-Path $output 'migration.sql')
    $sql=$sql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$',''
    $backup=Query @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Backup location unavailable',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-oil-workflow-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path ServerBackupPath,SYSUTCDATETIME() CompletedAtUtc;
'@ 180
    $backup.WriteXml((Join-Path $output 'backup.xml'),[Data.XmlWriteMode]::WriteSchema)
    foreach($commit in @($false,$true)) {
        $script:transaction=$connection.BeginTransaction([Data.IsolationLevel]::Serializable)
        try {
            [void](Query "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'oil-workflow-schema-deploy',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51000,'Deployment lock unavailable',1;")
            $before=Query $integrity
            foreach($batch in [regex]::Split($sql,'(?im)^GO\s*\r?$')) {if (![string]::IsNullOrWhiteSpace($batch)) {[void](Query $batch)}}
            $state=Query $verify; $after=Query $integrity
            for($i=0;$i -lt $before.Tables[0].Rows.Count;$i++) {
                if($before.Tables[0].Rows[$i].RecordCount -ne $after.Tables[0].Rows[$i].RecordCount -or $before.Tables[0].Rows[$i].Amount -ne $after.Tables[0].Rows[$i].Amount){throw 'Business totals changed during migration.'}
            }
            if([long]$state.Tables[0].Rows[0].RevisionRows -ne 0){throw 'Unexpected correction history.'}
            $mode=if($commit){'apply'}else{'rehearsal'}
            $after.WriteXml((Join-Path $output "$mode-business-totals.xml"),[Data.XmlWriteMode]::WriteSchema)
            if($commit){$script:transaction.Commit()}else{$script:transaction.Rollback()}
        }catch{try{$script:transaction.Rollback()}catch{};throw}finally{$script:transaction.Dispose();$script:transaction=$null}
    }
    $connection.Close();$connection.Open();$verified=Query $verify
    $verified.WriteXml((Join-Path $output 'postcommit.xml'),[Data.XmlWriteMode]::WriteSchema)
    $report=[ordered]@{Database=$connection.Database;Migrations=$migrations;BackupCompleted=$true;RehearsalRolledBack=$true;Applied=$true;PostCommitVerified=$true;BusinessTotalsPreserved=$true;ApiDeployed=$false;EvidenceDirectory=$output}
    $report|ConvertTo-Json|Set-Content (Join-Path $output 'report.json');$report|ConvertTo-Json
}finally{if($script:transaction){$script:transaction.Dispose()};$connection.Dispose()}
