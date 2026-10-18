param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$migrationId = '20261015080537_ResetJahezToAccountsOnlyV2'
$config = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$configuredConnection = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [System.Data.SqlClient.SqlConnection]::new($configuredConnection)
$transaction = $null
function Read-Data([string]$sql) {
    $command = $connection.CreateCommand(); $command.CommandText = $sql; $command.CommandTimeout = 180; $command.Transaction = $transaction
    try {
        $data = [System.Data.DataSet]::new(); $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        try { [void]$adapter.Fill($data) } finally { $adapter.Dispose() }
        return ,$data
    } finally { $command.Dispose() }
}
function Columns([string]$schema, [string]$table, [string[]]$excluded = @('RowVersion')) {
    $metadata = Read-Data "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$schema.$table') ORDER BY column_id;"
    return ($metadata.Tables[0].Rows | Where-Object { $_.name -notin $excluded } | ForEach-Object { '['+$_.name.Replace(']',']]')+']' }) -join ','
}
try {
    $connection.Open()
    if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected hosted target.' }
    $preflight = Read-Data @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261015072958_AddJahezWorkflowV2')
    THROW 51000,'Jahez V2 baseline is missing.',1;
SELECT COUNT_BIG(*) AccountCount FROM app.PlatformRiderAccounts WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ');
SELECT COUNT_BIG(*) AssignmentCount FROM app.RiderClientAssignments WHERE PlatformRiderAccountId IN
 (SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ'));
SELECT COUNT_BIG(*) CredentialCount FROM app.PlatformAccountCredentialVersions WHERE PlatformRiderAccountId IN
 (SELECT Id FROM app.PlatformRiderAccounts WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ'));
SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez') ORDER BY name;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261015080537_ResetJahezToAccountsOnlyV2';
'@
    Write-Output ('Verified hosted target: '+$connection.DataSource+' / '+$connection.Database)
    $preflight.Tables[0] | Format-Table -AutoSize
    $preflight.Tables[1] | Format-Table -AutoSize
    $preflight.Tables[2] | Format-Table -AutoSize
    if ($preflight.Tables[4].Rows.Count -gt 0) { Write-Output 'Account-only reset already recorded; no data deleted again.'; return }
    if (!$Apply) { Write-Output 'Preflight only. -Apply erases Jahez history, credentials and assignments while retaining master accounts and owners.'; return }
    if ($preflight.Tables[3].Rows.Count -ne 20) { throw 'Unexpected old Jahez table inventory; refusing reset.' }
    $scriptPath = Join-Path $workspace 'database/scripts/jahez-v2-accounts-only-reset.sql'
    $script = Get-Content -LiteralPath $scriptPath -Raw
    if ($script -notmatch [regex]::Escape($migrationId) -or $script -match '(?im)^\s*(BEGIN TRANSACTION|COMMIT)\b') { throw 'Unexpected reset script.' }
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
    $outputDirectory = Join-Path $workspace ('outputs/jahez-v2-accounts-only-reset-'+$stamp)
    [void](New-Item -ItemType Directory -Path $outputDirectory -Force)
    Copy-Item -LiteralPath $scriptPath -Destination (Join-Path $outputDirectory 'executed.sql')
    $preflight.WriteXml((Join-Path $outputDirectory 'preflight.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-Data 'SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;')
    foreach ($row in $preflight.Tables[3].Rows) {
        $table = '['+$row.name.Replace(']',']]')+']'
        $backup = Read-Data "SELECT * FROM jahez.$table WITH (UPDLOCK,HOLDLOCK);"
        $backup.WriteXml((Join-Path $outputDirectory ('old-'+$row.name+'.xml')), [System.Data.XmlWriteMode]::WriteSchema)
    }
    $jahezAccounts = "ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')"
    $accountIds = "SELECT Id FROM app.PlatformRiderAccounts WHERE $jahezAccounts"
    $masterColumns = Columns 'app' 'PlatformRiderAccounts' @('RowVersion','UserName','Status','StatusReason','UpdatedAtUtc')
    $masters = Read-Data "SELECT $masterColumns INTO #KeepAccounts FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK) WHERE $jahezAccounts;
        SELECT Id,Status,StatusReason INTO #AccountStates FROM app.PlatformRiderAccounts WHERE $jahezAccounts;
        SELECT * FROM #KeepAccounts;"
    $masters.WriteXml((Join-Path $outputDirectory 'accounts-and-owners-before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $checks = @()
    $ownerCondition = "Id IN(SELECT RegisteredEmployeeId FROM app.PlatformRiderAccounts WHERE $jahezAccounts)"
    $ownerColumns = Columns 'app' 'Employees'
    $owners = Read-Data "SELECT $ownerColumns INTO #KeepOwners FROM app.Employees WITH(UPDLOCK,HOLDLOCK) WHERE $ownerCondition; SELECT * FROM #KeepOwners;"
    $owners.WriteXml((Join-Path $outputDirectory 'owners-before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $checks += "IF EXISTS(SELECT $ownerColumns FROM #KeepOwners EXCEPT SELECT $ownerColumns FROM app.Employees WHERE $ownerCondition)
        OR EXISTS(SELECT $ownerColumns FROM app.Employees WHERE $ownerCondition EXCEPT SELECT $ownerColumns FROM #KeepOwners)
        THROW 51000,'Account owner records changed.',1;"
    $notifications = Read-Data "SELECT * FROM app.Notifications WITH(UPDLOCK,HOLDLOCK) WHERE EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%';"
    $notifications.WriteXml((Join-Path $outputDirectory 'removed-notifications-before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $protected = @(
        @{ Table='PlatformRiderAccounts'; Condition="NOT ($jahezAccounts)" },
        @{ Table='RiderClientAssignments'; Condition="PlatformRiderAccountId NOT IN($accountIds)" },
        @{ Table='PlatformAccountCredentialVersions'; Condition="PlatformRiderAccountId NOT IN($accountIds)" },
        @{ Table='PlatformAccountRegistrations'; Condition="ClientPlatformId NOT IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ')" },
        @{ Table='RiderAssignmentEvents'; Condition="RiderClientAssignmentId IN(SELECT Id FROM app.RiderClientAssignments WHERE PlatformRiderAccountId NOT IN($accountIds))" }
    )
    foreach ($item in $protected) {
        $table=$item.Table; $where=$item.Condition; $list=Columns 'app' $table
        $snapshot=Read-Data "SELECT $list INTO #Keep$table FROM app.[$table] WITH(UPDLOCK,HOLDLOCK) WHERE $where;
            SELECT * FROM app.[$table] WHERE NOT ($where);"
        $snapshot.WriteXml((Join-Path $outputDirectory ('removed-'+$table+'-before.xml')), [System.Data.XmlWriteMode]::WriteSchema)
        $checks += "IF EXISTS(SELECT $list FROM #Keep$table EXCEPT SELECT $list FROM app.[$table] WHERE $where)
            OR EXISTS(SELECT $list FROM app.[$table] WHERE $where EXCEPT SELECT $list FROM #Keep$table)
            THROW 51000,'Other platform data changed: $table',1;"
    }
    foreach ($batch in [regex]::Split($script, '(?im)^\s*GO\s*\r?$')) { if ($batch.Trim()) { [void](Read-Data $batch) } }
    foreach ($check in $checks) { [void](Read-Data $check) }
    [void](Read-Data "IF EXISTS(SELECT $masterColumns FROM #KeepAccounts EXCEPT SELECT $masterColumns FROM app.PlatformRiderAccounts WHERE $jahezAccounts)
        OR EXISTS(SELECT $masterColumns FROM app.PlatformRiderAccounts WHERE $jahezAccounts EXCEPT SELECT $masterColumns FROM #KeepAccounts)
        THROW 51000,'Jahez master accounts or owners changed.',1;
        IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN #AccountStates b ON a.Id=b.Id WHERE a.Status<>CASE WHEN b.Status=2 THEN 1 ELSE b.Status END)
        THROW 51000,'Unexpected Jahez account status after assignment reset.',1;")
    $verification = Read-Data @'
IF EXISTS(SELECT 1 FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez')) THROW 51000,'V1 tables remain.',1;
IF SCHEMA_ID('jahez') IS NOT NULL THROW 51000,'Old Jahez schema remains.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez_v2'))<>17 THROW 51000,'V2 schema incomplete.',1;
CREATE TABLE #V2Rows(TableName nvarchar(128),RecordCount bigint);
DECLARE @countSql nvarchar(max)=N'';
SELECT @countSql+=N'INSERT INTO #V2Rows SELECT N'''+REPLACE(name,'''','''''')+N''',COUNT_BIG(*) FROM [jahez_v2].'+QUOTENAME(name)+N';'
FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez_v2');
EXEC sys.sp_executesql @countSql;
IF EXISTS(SELECT 1 FROM #V2Rows WHERE RecordCount<>0) THROW 51000,'V2 business data was not reset.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id
    JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='JAHEZ') THROW 51000,'Jahez assignments remain.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountCredentialVersions r JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id
    JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='JAHEZ') THROW 51000,'Jahez credentials remain.',1;
IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code='JAHEZ' AND a.UserName IS NOT NULL) THROW 51000,'Jahez login usernames remain.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountRegistrations r JOIN platform.ClientPlatforms p ON r.ClientPlatformId=p.Id WHERE p.Code='JAHEZ')
    THROW 51000,'Jahez registrations remain.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys fk WHERE fk.schema_id=SCHEMA_ID('jahez_v2') AND (is_disabled=1 OR is_not_trusted=1))
    THROW 51000,'V2 foreign keys are untrusted.',1;
SELECT COUNT_BIG(*) Accounts,COUNT_BIG(RegisteredEmployeeId) AccountsWithOwners FROM app.PlatformRiderAccounts
 WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='JAHEZ');
SELECT TableName,RecordCount FROM #V2Rows ORDER BY TableName;
SELECT SYSUTCDATETIME() VerifiedAtUtc;
'@
    $verification.WriteXml((Join-Path $outputDirectory 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $transaction.Commit(); $transaction.Dispose(); $transaction=$null
    $report=[ordered]@{ Database=$connection.Database; Server=$connection.DataSource; AppliedAtUtc=[DateTime]::UtcNow.ToString('o');
        Migration=$migrationId; AccountsPreserved=[long]$verification.Tables[0].Rows[0].Accounts;
        AccountsWithOwners=[long]$verification.Tables[0].Rows[0].AccountsWithOwners;
        RemovedAssignments=[long]$preflight.Tables[1].Rows[0].AssignmentCount; RemovedCredentials=[long]$preflight.Tables[2].Rows[0].CredentialCount;
        OldJahezSchemaRemoved=$true; EmptyV2Tables=17; OtherPlatformDataPreserved=$true; ScriptSha256=(Get-FileHash $scriptPath -Algorithm SHA256).Hash }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputDirectory 'report.json') -Encoding UTF8
    $report | ConvertTo-Json -Depth 6
    Write-Output ('Secured backup and verification: '+$outputDirectory)
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch {} }; throw
} finally { if ($transaction) { $transaction.Dispose() }; $connection.Dispose() }
