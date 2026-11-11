param(
    [ValidateSet('Preflight', 'Rehearse', 'Apply', 'Verify')][string]$Mode = 'Preflight',
    [string]$EvidenceDirectory = './outputs/hunger-cr005-hosted-20261111'
)
# Hunger CR-005: five application migrations and one identity migration, applied in one Serializable transaction.
# Preflight reads only. Rehearse applies everything and rolls back. Apply requires a passed rehearsal of the same script hashes.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
[void](New-Item -ItemType Directory -Force -Path $EvidenceDirectory)
$config = Get-Content -LiteralPath (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$text = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($text)
$transaction = $null
$baselineApp = '20261110105351_AddHungerJahezPerformanceDeletePermissions'
$baselineIdentity = '20261110105334_GrantHungerJahezPerformanceDeletePermissions'
$appIds = @('20261111071640_HungerCr005SealEvidenceExceptions', '20261111073327_HungerCr005InvoiceFedMonthClose',
    '20261111074551_HungerCr005AdjustmentApprovalDate', '20261111081325_HungerCr005WalletPendingDisposition', '20261111082612_HungerCr005FinanceReadViews')
$identityIds = @('20261111071648_GrantHungerCr005Permissions')
$views = @('vw_hunger_financial_events_v1', 'vw_hunger_wallet_reviews_v1', 'vw_hunger_month_close_v1', 'vw_hunger_adjustments_v1', 'vw_hunger_custody_v1')
$files = @('hunger-cr005-application.sql', 'hunger-cr005-identity.sql')
$hashes = [ordered]@{}; $batches = @()
foreach ($file in $files) {
    $path = Join-Path $root ('database/scripts/' + $file)
    $sql = (Get-Content -LiteralPath $path -Raw -Encoding UTF8).TrimStart([char]0xFEFF)
    foreach ($id in $(if ($file -like '*application.sql') { $appIds } else { $identityIds })) { if (!$sql.Contains($id)) { throw "Script $file is missing $id." } }
    if ($sql -match '(?im)^\s*(DROP\s+(TABLE|COLUMN|DATABASE)|TRUNCATE|DELETE)\b') { throw "Unexpected destructive statement in $file." }
    $hashes[$file] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $path -Destination (Join-Path $EvidenceDirectory $file) -Force
    $body = $sql -replace '(?im)^\s*BEGIN TRANSACTION;\s*$', '' -replace '(?im)^\s*COMMIT;\s*$', ''
    # EF emits one batch per migration. Run one statement at a time so statements compile after the ALTERs before them;
    # keep EF's DECLARE @var ... EXEC default-constraint helper together.
    foreach ($batch in [regex]::Split($body, '(?im)^[ \t]*GO[ \t]*\r?$')) {
        $pending = $null
        foreach ($piece in [regex]::Split($batch, ';[ \t]*\r?\n')) {
            if ($null -ne $pending) { $pending += ";`n" + $piece; if ($piece -match '(?im)^\s*IF @var') { $batches += $pending; $pending = $null }; continue }
            if ($piece -match '(?im)^\s*DECLARE @var') { $pending = $piece; continue }
            if (![string]::IsNullOrWhiteSpace($piece)) { $batches += $piece }
        }
        if ($null -ne $pending) { $batches += $pending }
    }
}
function Read-Sql([string]$Sql) {
    $command = $connection.CreateCommand(); $command.CommandText = $Sql; $command.CommandTimeout = 300; $command.Transaction = $transaction
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try { $data = [Data.DataSet]::new(); [void]$adapter.Fill($data); return , $data } finally { $adapter.Dispose(); $command.Dispose() }
}
$appList = ($appIds | ForEach-Object { "N'$_'" }) -join ','; $identityList = ($identityIds | ForEach-Object { "N'$_'" }) -join ','
function Get-State {
    Read-Sql @"
SELECT (SELECT COUNT(*) FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN ($appList)) AppApplied,
       (SELECT COUNT(*) FROM migration.__IdentityMigrationsHistory WHERE MigrationId IN ($identityList)) IdentityApplied,
       (SELECT TOP 1 MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId NOT IN ($appList) ORDER BY MigrationId DESC) AppLatestOther,
       (SELECT TOP 1 MigrationId FROM migration.__IdentityMigrationsHistory WHERE MigrationId NOT IN ($identityList) ORDER BY MigrationId DESC) IdentityLatestOther,
       (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('hunger.FinancialEvents') AND name = 'ApprovalStatus') SealColumn,
       (SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id WHERE s.name = 'hunger' AND t.name IN ('EvidenceFiles','ExceptionApprovals')) NewTables,
       (SELECT COUNT(*) FROM sys.views v JOIN sys.schemas s ON s.schema_id = v.schema_id WHERE s.name = 'finance_read') ReadViews,
       (SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE [Key] IN (N'hunger.finance.seal',N'hunger.finance.funding.approve',N'hunger.finance.exception.request',N'hunger.finance.exception.approve',N'hunger.finance.owner_collections.confirm')) NewDefinitions,
       (SELECT COUNT(*) FROM [identity].RolePermissions WHERE PermissionKey IN (N'hunger.finance.seal',N'hunger.finance.funding.approve',N'hunger.finance.exception.request',N'hunger.finance.exception.approve',N'hunger.finance.owner_collections.confirm')) NewGrants,
       (SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE Id IN ('019c18d5-62e1-7000-a000-000000000384','019c18d5-62e1-7000-a000-000000000385','019c18d5-62e1-7000-a000-000000000386','019c18d5-62e1-7000-a000-000000000387','019c18d5-62e1-7000-a000-000000000388')) DefinitionIdsTaken,
       (SELECT COUNT(*) FROM [identity].RolePermissions WHERE Id IN ('019c18d5-62e1-7000-b000-000000000321','019c18d5-62e1-7000-b000-000000000322','019c18d5-62e1-7000-b000-000000000323','019c18d5-62e1-7000-b000-000000000324','019c18d5-62e1-7000-b000-000000000325')) GrantIdsTaken,
       (SELECT COUNT(*) FROM hunger.FinanceConfigurations WHERE IsDeleted = 0) FinanceConfigurations;
SELECT Kind, Status, COUNT(*) Records, SUM(Amount) Amount FROM hunger.FinancialEvents GROUP BY Kind, Status ORDER BY Kind, Status;
SELECT OperationalDisposition, COUNT(*) Records FROM app.PlatformAccountBusinessDetails GROUP BY OperationalDisposition ORDER BY OperationalDisposition;
SELECT N'FinancialEvents' Dataset, COUNT_BIG(*) Records, CHECKSUM_AGG(BINARY_CHECKSUM(Id, Kind, Status, Amount, RiderBalanceChange, BusinessDate, IdempotencyKey)) Fingerprint FROM hunger.FinancialEvents
UNION ALL SELECT N'SettlementDocuments', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, Kind, Status, WalletBalance, IdempotencyKey)) FROM hunger.SettlementDocuments
UNION ALL SELECT N'WalletReviews', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, Status, PlatformRiderAccountId, RowVersion)) FROM hunger.WalletReviews
UNION ALL SELECT N'RemittanceLines', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, Amount, ConfirmedAmount, RowVersion)) FROM hunger.RemittanceLines
UNION ALL SELECT N'BusinessDetails', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, OperationalDisposition, RowVersion)) FROM app.PlatformAccountBusinessDetails
UNION ALL SELECT N'TreasuryPostings', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, SubAccountId, Amount)) FROM jahez_v3.Postings
UNION ALL SELECT N'Accounts', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, SponsorId, IsDeleted, RowVersion)) FROM app.PlatformRiderAccounts
UNION ALL SELECT N'Assignments', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, EffectiveFrom, EffectiveTo, IsDeleted, RowVersion)) FROM app.RiderClientAssignments
UNION ALL SELECT N'OtherDefinitions', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, [Key], IsDeleted, RowVersion)) FROM platform.PermissionDefinitions WHERE [Key] NOT LIKE N'hunger.finance.seal' AND [Key] NOT LIKE N'hunger.finance.funding.approve' AND [Key] NOT LIKE N'hunger.finance.exception.%' AND [Key] NOT LIKE N'hunger.finance.owner_collections.confirm'
UNION ALL SELECT N'OtherGrants', COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(Id, RoleId, PermissionKey, IsDeleted, RowVersion)) FROM [identity].RolePermissions WHERE PermissionKey NOT LIKE N'hunger.finance.seal' AND PermissionKey NOT LIKE N'hunger.finance.funding.approve' AND PermissionKey NOT LIKE N'hunger.finance.exception.%' AND PermissionKey NOT LIKE N'hunger.finance.owner_collections.confirm';
"@
}
function Assert-Baseline($State) {
    $s = $State.Tables[0].Rows[0]
    if ($s.AppLatestOther -cne $baselineApp -or $s.IdentityLatestOther -cne $baselineIdentity) { throw "Hosted baseline differs: $($s.AppLatestOther) / $($s.IdentityLatestOther)." }
    $complete = $s.AppApplied -eq $appIds.Count -and $s.IdentityApplied -eq $identityIds.Count
    $none = $s.AppApplied -eq 0 -and $s.IdentityApplied -eq 0
    if (!$complete -and !$none) { throw 'Partial CR-005 deployment detected; reconcile before continuing.' }
    if ($none -and ($s.SealColumn -ne 0 -or $s.NewTables -ne 0 -or $s.ReadViews -ne 0 -or $s.NewDefinitions -ne 0 -or $s.NewGrants -ne 0 -or $s.DefinitionIdsTaken -ne 0 -or $s.GrantIdsTaken -ne 0)) { throw 'CR-005 objects or seed ids already exist without migration history.' }
    return $complete
}
function Assert-Complete($State) {
    $s = $State.Tables[0].Rows[0]
    if ($s.AppApplied -ne $appIds.Count -or $s.IdentityApplied -ne $identityIds.Count -or $s.SealColumn -ne 1 -or $s.NewTables -ne 2 -or $s.ReadViews -ne $views.Count -or $s.NewDefinitions -ne 5 -or $s.NewGrants -ne 5) { throw 'CR-005 objects are incomplete after the migrations.' }
    # Every view must compile and run, and the backfill must have sealed exactly the pre-existing sealable rows.
    foreach ($view in $views) { [void](Read-Sql "SELECT COUNT_BIG(*) FROM finance_read.$view") }
    $check = (Read-Sql "SELECT (SELECT COUNT(*) FROM hunger.FinancialEvents WHERE ApprovalStatus NOT IN ('NotRequired','PendingSeal','Sealed','Rejected','Cancelled')) BadStatus, (SELECT COUNT(*) FROM finance_read.vw_hunger_financial_events_v1) ViewRows, (SELECT COUNT(*) FROM hunger.FinancialEvents) TableRows").Tables[0].Rows[0]
    if ($check.BadStatus -ne 0 -or $check.ViewRows -ne $check.TableRows) { throw 'Seal backfill or events view is inconsistent.' }
}
function Assert-Preserved($Before, $After) {
    foreach ($row in $Before.Tables[3].Rows) {
        $match = @($After.Tables[3].Rows | Where-Object Dataset -eq $row.Dataset)
        if ($match.Count -ne 1 -or $match[0].Records -ne $row.Records -or [string]$match[0].Fingerprint -ne [string]$row.Fingerprint) { throw ('Unrelated data changed: ' + $row.Dataset) }
    }
}
try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') { throw 'Unexpected hosted target.' }
    $connection.Open(); $before = Get-State; $already = Assert-Baseline $before
    $before.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-before.xml')), [Data.XmlWriteMode]::WriteSchema)
    $backupPath = $null
    if ($Mode -eq 'Verify') { Assert-Complete $before }
    if ($Mode -in @('Rehearse', 'Apply') -and !$already) {
        if ($Mode -eq 'Apply') {
            $rehearsal = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'rehearse.json') -Raw | ConvertFrom-Json
            if (!$rehearsal.Passed -or $rehearsal.Applied -or $rehearsal.Mode -ne 'Rehearse') { throw 'Successful rollback rehearsal required.' }
            foreach ($file in $files) { if ($rehearsal.ScriptSha256.$file -cne $hashes[$file]) { throw 'Migration script changed since rehearsal.' } }
            $backup = $connection.CreateCommand(); $backup.CommandTimeout = 900
            $backup.CommandText = "DECLARE @dir nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)); IF @dir IS NULL THROW 51000,'Backup directory unavailable',1; DECLARE @path nvarchar(4000)=@dir + CASE WHEN RIGHT(@dir,1) IN ('\','/') THEN '' ELSE '\' END + 'db67927_before_hunger_cr005_' + CONVERT(varchar(8),GETUTCDATE(),112) + '_' + REPLACE(CONVERT(varchar(8),GETUTCDATE(),108),':','') + '.bak'; BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM; SELECT @path;"
            try { $backupPath = $backup.ExecuteScalar() } finally { $backup.Dispose() }
        }
        $transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
        [void](Read-Sql "SET XACT_ABORT ON; SET LOCK_TIMEOUT 30000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'hunger-cr005-migrations',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=30000; IF @r<0 THROW 51000,'Cannot lock CR-005 migration.',1;")
        $locked = Get-State; if (Assert-Baseline $locked) { throw 'Applied concurrently by another session.' }
        foreach ($batch in $batches) { [void](Read-Sql $batch) }
        $inside = Get-State; Assert-Complete $inside; Assert-Preserved $locked $inside
        $inside.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-inside.xml')), [Data.XmlWriteMode]::WriteSchema)
        $sealed = Read-Sql "SELECT Kind, ApprovalStatus, Role, COUNT(*) Records FROM hunger.FinancialEvents GROUP BY Kind, ApprovalStatus, Role ORDER BY Kind"
        $sealed.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-seal-backfill.xml')), [Data.XmlWriteMode]::WriteSchema)
        if ($Mode -eq 'Apply') { $transaction.Commit() } else { $transaction.Rollback() }
        $transaction.Dispose(); $transaction = $null
    }
    $connection.Close(); $connection.Open(); $after = Get-State; $afterComplete = Assert-Baseline $after
    if ($Mode -in @('Apply', 'Verify') -or $already) { Assert-Complete $after }
    if ($Mode -eq 'Rehearse' -and !$already -and $afterComplete) { throw 'Rollback was not verified.' }
    if ($Mode -ne 'Apply' -or $already) { Assert-Preserved $before $after }
    $after.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-after.xml')), [Data.XmlWriteMode]::WriteSchema)
    $summary = $after.Tables[0].Rows[0]
    $report = [ordered]@{ Passed = $true; Mode = $Mode; Database = $connection.Database; Server = $connection.DataSource; VerifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        Applied = [bool]$afterComplete; AlreadyAppliedBeforeRun = [bool]$already; Batches = $batches.Count; ApplicationMigrations = $appIds; IdentityMigrations = $identityIds
        FinanceConfigurations = [int]$summary.FinanceConfigurations; FreshConnectionVerified = $true; ScriptSha256 = $hashes; BackupPath = $backupPath; ApiPublishedByThisRun = $false
        FinancialEvents = @($after.Tables[1].Rows | ForEach-Object { [ordered]@{ Kind = $_.Kind; Status = $_.Status; Records = $_.Records; Amount = $_.Amount } })
        Dispositions = @($after.Tables[2].Rows | ForEach-Object { [ordered]@{ Disposition = $_.OperationalDisposition; Records = $_.Records } }) }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '.json')) -Encoding utf8
    $report | ConvertTo-Json -Depth 6
} finally {
    if ($transaction) { try { $transaction.Rollback() } finally { $transaction.Dispose() } }
    $connection.Dispose()
}
