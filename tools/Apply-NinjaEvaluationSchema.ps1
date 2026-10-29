param([ValidateSet('Rehearse','Apply','Verify')][string]$Mode = 'Rehearse')
$ErrorActionPreference = 'Stop'
$evaluationRoot = Split-Path -Parent $PSScriptRoot
$evaluationOutput = Join-Path $evaluationRoot 'outputs/ninja-performance-20261026'
[void](New-Item -ItemType Directory -Force -Path $evaluationOutput)
$evaluationConfig = Get-Content -LiteralPath (Join-Path $evaluationRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$evaluationConnection = [Data.SqlClient.SqlConnection]::new($evaluationConfig.ConnectionStrings.LogisticsDatabase)
$evaluationTransaction = $null
$evaluationAppId = '20261026180608_AddNinjaEvaluationSuggestions'
$evaluationIdentityId = '20261026180713_GrantNinjaEvaluationPermissions'
$evaluationFiles = @('database/scripts/ninja-evaluation-application.sql','database/scripts/ninja-evaluation-identity.sql')
$evaluationHashes = @($evaluationFiles | ForEach-Object { (Get-FileHash -LiteralPath (Join-Path $evaluationRoot $_) -Algorithm SHA256).Hash })

function Read-EvaluationSql([string]$Sql,[int]$Timeout=120) {
    $command = $evaluationConnection.CreateCommand(); $command.CommandText = $Sql; $command.CommandTimeout = $Timeout; $command.Transaction = $evaluationTransaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return ,$data } finally { $adapter.Dispose(); $command.Dispose() }
}
function Get-EvaluationState {
    return Read-EvaluationSql @'
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory ORDER BY MigrationId;
SELECT MigrationId FROM migration.__IdentityMigrationsHistory ORDER BY MigrationId;
SELECT N'PerformanceSources' Name,COUNT_BIG(*) Records,CHECKSUM_AGG(BINARY_CHECKSUM(Id,PlatformRiderAccountId,ReportDate,TotalOrders,TotalWorkingHours)) Fingerprint FROM ninja.NinjaDailyPerformanceSources
UNION ALL SELECT N'PerformanceShares',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SourceId,RiderClientAssignmentId,RiderProfileId,ReportDate,TotalOrders,TotalWorkingHours)) FROM ninja.NinjaDailyPerformance
UNION ALL SELECT N'Deductions',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SourceId,RiderClientAssignmentId,TotalDeductions,TotalCollectedAmountAtDelivery)) FROM ninja.NinjaDailyDeductions;
'@
}
try {
    if ($evaluationConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $evaluationConnection.Database -ne 'db67927') { throw 'Unexpected hosted database.' }
    $evaluationConnection.Open()
    $before = Get-EvaluationState
    $appIds = @($before.Tables[0].Rows | ForEach-Object MigrationId); $identityIds = @($before.Tables[1].Rows | ForEach-Object MigrationId)
    $localApp = @(Get-ChildItem -LiteralPath (Join-Path $evaluationRoot 'src/LogisticsERP.Infrastructure/Persistence/Migrations/Application') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $localIdentity = @(Get-ChildItem -LiteralPath (Join-Path $evaluationRoot 'src/LogisticsERP.Infrastructure/Identity/Migrations') -Filter '*.cs' | Where-Object { $_.BaseName -match '^\d{14}_' -and $_.Name -notlike '*.Designer.cs' } | ForEach-Object BaseName)
    $unexpectedApp = @($localApp | Where-Object { $_ -notin $appIds -and $_ -ne $evaluationAppId }).Count + @($appIds | Where-Object { $_ -notin $localApp }).Count
    $unexpectedIdentity = @($localIdentity | Where-Object { $_ -notin $identityIds -and $_ -ne $evaluationIdentityId }).Count + @($identityIds | Where-Object { $_ -notin $localIdentity }).Count
    if ($unexpectedApp -gt 0 -or $unexpectedIdentity -gt 0) { throw 'Unexpected migration history; refusing unrelated changes.' }
    if (($evaluationAppId -in $appIds) -and ($evaluationIdentityId -in $identityIds)) {
        $verify = Read-EvaluationSql "SELECT COUNT(*) TablesFound FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=N'ninja' AND t.name IN(N'SuggestionPolicies',N'EvaluationContexts',N'ResponsibilityRevisions',N'ResponsibilityLines',N'PerformancePreviews',N'PerformanceArtifacts',N'PerformanceImportArtifacts',N'AllocationProvenance',N'EvaluationSnapshots'); SELECT COUNT(*) PermissionsFound FROM platform.PermissionDefinitions WHERE [Key] IN(N'ninja.evaluation.manage',N'ninja.evaluation.export') AND IsDeleted=0; SELECT COUNT(*) Policies FROM ninja.SuggestionPolicies WHERE Version=N'NINJA-SUGGESTION-V1';"
        if ($verify.Tables[0].Rows[0].TablesFound -ne 9 -or $verify.Tables[1].Rows[0].PermissionsFound -ne 2 -or $verify.Tables[2].Rows[0].Policies -ne 1) { throw 'Post-deployment schema verification failed.' }
        [pscustomobject]@{ Mode='AlreadyAppliedVerified'; Database=$evaluationConnection.Database; Tables=9; Permissions=2; Writes=0 } | ConvertTo-Json
        return
    }
    if ($Mode -eq 'Verify') { throw 'Schema has not been applied.' }
    if (($evaluationAppId -in $appIds) -or ($evaluationIdentityId -in $identityIds)) { throw 'Partial deployment needs explicit review.' }
    $rehearsalPath = Join-Path $evaluationOutput 'schema-rehearsal.json'
    if ($Mode -eq 'Apply') {
        $rehearsal = Get-Content -LiteralPath $rehearsalPath -Raw | ConvertFrom-Json
        if (!$rehearsal.Passed -or ($rehearsal.ScriptHashes -join ',') -ne ($evaluationHashes -join ',')) { throw 'A successful matching schema rehearsal is required.' }
        Write-Output 'Creating hosted COPY_ONLY checksum backup before the authorized migration and import.'
        $backup = Read-EvaluationSql @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Backup path unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+CASE WHEN RIGHT(@folder,1) IN(N'\',N'/') THEN N'' ELSE N'\' END+N'db67927-before-ninja-performance-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path BackupPath,SYSUTCDATETIME() CompletedAtUtc;
'@ 240
        $backup.WriteXml((Join-Path $evaluationOutput 'server-backup.xml'),[Data.XmlWriteMode]::WriteSchema)
        $backupPath = [string]$backup.Tables[0].Rows[0].BackupPath
        $backupCommand = $evaluationConnection.CreateCommand(); $backupCommand.CommandTimeout = 240
        $backupCommand.CommandText = 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;'
        [void]$backupCommand.Parameters.Add('@path',[Data.SqlDbType]::NVarChar,4000); $backupCommand.Parameters['@path'].Value = $backupPath
        try { [void]$backupCommand.ExecuteNonQuery(); $backupVerified=$true }
        catch { if (@($_.Exception.GetBaseException().Errors | Where-Object Number -eq 262).Count -eq 0) { throw }; $backupVerified=$false }
        finally { $backupCommand.Dispose() }
    }
    $before.WriteXml((Join-Path $evaluationOutput ('schema-before-'+$Mode.ToLowerInvariant()+'.xml')),[Data.XmlWriteMode]::WriteSchema)
    $evaluationTransaction = $evaluationConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
    foreach ($file in $evaluationFiles) {
        $sql = Get-Content -LiteralPath (Join-Path $evaluationRoot $file) -Raw
        if ($sql -match '(?im)^\s*(DROP|TRUNCATE|DELETE|UPDATE)\b') { throw 'Unexpected destructive migration statement.' }
        $sql = [regex]::Replace($sql,'(?im)^\s*(BEGIN TRANSACTION;|COMMIT;)\s*$','')
        foreach ($batch in [regex]::Split($sql,'(?im)^\s*GO\s*$')) { if (![string]::IsNullOrWhiteSpace($batch)) { [void](Read-EvaluationSql $batch) } }
    }
    $after = Get-EvaluationState
    if ($evaluationAppId -notin @($after.Tables[0].Rows | ForEach-Object MigrationId) -or $evaluationIdentityId -notin @($after.Tables[1].Rows | ForEach-Object MigrationId)) { throw 'Migration markers missing.' }
    for ($i=0;$i -lt $before.Tables[2].Rows.Count;$i++) {
        if ($before.Tables[2].Rows[$i].Records -ne $after.Tables[2].Rows[$i].Records -or $before.Tables[2].Rows[$i].Fingerprint -ne $after.Tables[2].Rows[$i].Fingerprint) { throw 'Existing performance/deductions changed.' }
    }
    if ($Mode -eq 'Apply') { $evaluationTransaction.Commit() } else { $evaluationTransaction.Rollback() }
    $evaluationTransaction.Dispose(); $evaluationTransaction=$null
    $report = [ordered]@{ Passed=$true; Mode=$Mode; Database=$evaluationConnection.Database; ScriptHashes=$evaluationHashes; ExistingPerformancePreserved=$true; CompletedAtUtc=[DateTimeOffset]::UtcNow.ToString('o') }
    if ($Mode -eq 'Apply') { $report.ServerBackupCreated=$true; $report.RestoreVerifyOnlySucceeded=$backupVerified }
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evaluationOutput $(if($Mode -eq 'Apply'){'schema-applied.json'}else{'schema-rehearsal.json'})) -Encoding utf8
    $report | ConvertTo-Json -Depth 5
} finally {
    if ($evaluationTransaction) { try { $evaluationTransaction.Rollback() } finally { $evaluationTransaction.Dispose() } }
    $evaluationConnection.Dispose()
}
