param([switch]$Apply, [switch]$Validate)
$ErrorActionPreference = 'Stop'
if ($Apply -and $Validate) { throw 'Choose application or rolled-back validation.' }
$datesRoot = Split-Path -Parent $PSScriptRoot
$datesSourceRelative = 'outputs/jahez-financial-reset-20261019/applied/jahez_v2.AccountSessions-before.xml'
$datesSource = Join-Path $datesRoot $datesSourceRelative
$datesSourceHash = (Get-FileHash -LiteralPath $datesSource -Algorithm SHA256).Hash
if ($datesSourceHash -ne '48B90DCBD05F511074FB1D4BFF68771710CD64B720CC3480895CB30B3C60E772') {
    throw 'The reviewed pre-reset archive changed.'
}
$datesXml = [xml](Get-Content -LiteralPath $datesSource -Raw)
$datesPlanRows = @(foreach ($row in $datesXml.NewDataSet.Table) {
    [ordered]@{
        id = [string]$row.Id
        accountId = [string]$row.PlatformRiderAccountId
        riderId = [string]$row.RiderProfileId
        assignmentId = [string]$row.RiderClientAssignmentId
        startedAt = [string]$row.StartedAtUtc
        endedAt = if ($row.EndedAtUtc) { [string]$row.EndedAtUtc } else { $null }
        startsOn = ([string]$row.CommissionStartsOn).Substring(0, 10)
        postedThrough = if ($row.CommissionPostedThrough) { ([string]$row.CommissionPostedThrough).Substring(0, 10) } else { $null }
        lastPayment = if ($row.LastSettlementPaymentAtUtc) { [string]$row.LastSettlementPaymentAtUtc } else { $null }
    }
})
if ($datesPlanRows.Count -ne 313) { throw 'Expected all 313 archived sessions.' }
$datesPlan = ConvertTo-Json -InputObject $datesPlanRows -Depth 4 -Compress
$datesConfig = Get-Content -LiteralPath (Join-Path $datesRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$datesConnection = [System.Data.SqlClient.SqlConnection]::new($(if ($env:ConnectionStrings__LogisticsDatabase) {
    $env:ConnectionStrings__LogisticsDatabase
} else { $datesConfig.ConnectionStrings.LogisticsDatabase }))
$datesTransaction = $null
$datesCommitted = $false
$datesCorrelation = 'jahez-restore-pre-reset-dates-20261020'
$datesMode = if ($Apply) { 'applied' } elseif ($Validate) { 'validation' } else { 'review' }
$datesOutput = Join-Path $datesRoot ('outputs/jahez-date-restore-20261020/' + $datesMode + '-' + [guid]::NewGuid().ToString('N'))

function Read-DatesData([string]$Sql) {
    $command = $datesConnection.CreateCommand()
    $command.Transaction = $datesTransaction
    $command.CommandTimeout = 60
    $command.CommandText = $Sql
    [void]$command.Parameters.Add('@Plan', [System.Data.SqlDbType]::NVarChar, -1)
    $command.Parameters['@Plan'].Value = $datesPlan
    [void]$command.Parameters.AddWithValue('@Correlation', $datesCorrelation)
    [void]$command.Parameters.AddWithValue('@SourceHash', $datesSourceHash)
    [void]$command.Parameters.AddWithValue('@SourceFile', $datesSourceRelative)
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [System.Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

$datesArchiveSql = @'
SET NOCOUNT ON;
SELECT * INTO #DateArchive FROM OPENJSON(@Plan) WITH(
 id uniqueidentifier,accountId uniqueidentifier,riderId uniqueidentifier,assignmentId uniqueidentifier,
 startedAt datetimeoffset,endedAt datetimeoffset,startsOn date,postedThrough date,lastPayment datetimeoffset);
IF (SELECT COUNT(*) FROM #DateArchive)<>313 OR (SELECT COUNT(DISTINCT id) FROM #DateArchive)<>313
 THROW 51000,'Unexpected archive identities.',1;
'@
$datesReviewSql = @'
SELECT COUNT(*) ArchivedSessions,
 SUM(CASE WHEN a.lastPayment IS NOT NULL THEN 1 ELSE 0 END) ArchivedLastSettlements,
 SUM(CASE WHEN a.postedThrough IS NOT NULL THEN 1 ELSE 0 END) ArchivedLastCommissions,
 SUM(CASE WHEN s.Id IS NULL THEN 1 ELSE 0 END) MissingSessions,
 SUM(CASE WHEN s.Id IS NOT NULL AND (s.PlatformRiderAccountId<>a.accountId OR s.RiderProfileId<>a.riderId
 OR s.RiderClientAssignmentId<>a.assignmentId OR s.StartedAtUtc<>a.startedAt
 OR ISNULL(s.EndedAtUtc,'19000101')<>ISNULL(a.endedAt,'19000101') OR s.IsDeleted<>0) THEN 1 ELSE 0 END) IdentityMismatches,
 SUM(CASE WHEN s.CommissionStartsOn<>a.startsOn
 OR ISNULL(s.CommissionPostedThrough,'19000101')<>ISNULL(a.postedThrough,'19000101')
 OR ISNULL(s.LastSettlementPaymentAtUtc,'19000101')<>ISNULL(a.lastPayment,'19000101') THEN 1 ELSE 0 END) DateDifferences
 FROM #DateArchive a LEFT JOIN jahez_v2.AccountSessions s ON s.Id=a.id;
SELECT COUNT(*) NewSessions FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #DateArchive a WHERE a.id=s.Id);
SELECT COUNT(*) ExistingOperations FROM jahez_v2.Operations o JOIN #DateArchive a ON a.id=o.HandoverId;
SELECT a.id SessionId,p.ExternalAccountId Account,a.startsOn OriginalCommissionStartsOn,
 a.postedThrough OriginalCommissionPostedThrough,a.lastPayment OriginalLastSettlementPaymentAtUtc,
 s.CommissionStartsOn CurrentCommissionStartsOn,s.CommissionPostedThrough CurrentCommissionPostedThrough,
 s.LastSettlementPaymentAtUtc CurrentLastSettlementPaymentAtUtc,s.EndedAtUtc
 FROM #DateArchive a JOIN jahez_v2.AccountSessions s ON s.Id=a.id
 JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId ORDER BY p.ExternalAccountId,s.StartedAtUtc;
'@
$datesPrepareSql = @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'jahez-restore-pre-reset-dates-20261020',
 @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @lock<0 THROW 51000,'Could not acquire date restoration lock.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WITH(HOLDLOCK) WHERE CorrelationId=@Correlation)
 THROW 51000,'Date restoration already committed; do not replay.',1;
SELECT * INTO #DatesBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #DatesDatasetBefore FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey=N'jahez';
IF (SELECT COUNT(*) FROM #DatesDatasetBefore)<>1 THROW 51000,'Missing or duplicate Jahez dataset version.',1;
IF NOT EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019')
 THROW 51000,'The reviewed reset audit is missing.',1;
IF EXISTS(SELECT 1 FROM #DateArchive a LEFT JOIN #DatesBefore s ON s.Id=a.id
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=a.accountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR s.PlatformRiderAccountId<>a.accountId OR s.RiderProfileId<>a.riderId
 OR s.RiderClientAssignmentId<>a.assignmentId OR s.StartedAtUtc<>a.startedAt
 OR ISNULL(s.EndedAtUtc,'19000101')<>ISNULL(a.endedAt,'19000101') OR s.IsDeleted<>0
 OR p.Id IS NULL OR c.Code<>N'JAHEZ')
 THROW 51000,'An archived session identity or assignment changed.',1;
IF EXISTS(SELECT 1 FROM #DateArchive a JOIN #DatesBefore s ON s.Id=a.id
 WHERE s.CommissionStartsOn<>'20261020' OR s.CommissionPostedThrough IS NOT NULL OR s.LastSettlementPaymentAtUtc IS NOT NULL)
 THROW 51000,'A session no longer has the reviewed reset dates. Reinspect.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Operations o WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=o.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez_v2.AccountingEntries e WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=e.HandoverId WHERE e.Bucket=3)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements m WITH(HOLDLOCK) JOIN #DateArchive a ON a.id=m.HandoverId)
 THROW 51000,'New financial work exists for a target session. Reinspect.',1;
-- Four sessions have documented historical baseline corrections preceding their operational handover.
-- Restore those archive values exactly; do not reinterpret them as new handover dates.
IF EXISTS(SELECT 1 FROM #DateArchive WHERE startsOn IS NULL
 OR postedThrough>CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+03:00'))
 OR lastPayment>SYSDATETIMEOFFSET())
 THROW 51000,'Invalid archived financial dates.',1;
SELECT a.id INTO #DatesChanged FROM #DateArchive a JOIN #DatesBefore s ON s.Id=a.id
 WHERE EXISTS(SELECT a.startsOn,a.postedThrough,a.lastPayment
 EXCEPT SELECT s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc);
IF (SELECT COUNT(*) FROM #DatesChanged)<>309 THROW 51000,'Unexpected number of changed sessions.',1;
'@
$datesMutationSql = @'
DECLARE @now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
UPDATE s SET CommissionStartsOn=a.startsOn,CommissionPostedThrough=a.postedThrough,
 LastSettlementPaymentAtUtc=a.lastPayment,UpdatedAtUtc=@now,UpdatedByUserId=NULL
 FROM jahez_v2.AccountSessions s JOIN #DateArchive a ON a.id=s.Id JOIN #DatesChanged d ON d.id=s.Id;
IF @@ROWCOUNT<>309 THROW 51000,'Date restoration affected an unexpected number of sessions.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,
 CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #DatesBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,
 CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #DatesBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'Unrelated session fields changed.',1;
IF EXISTS(SELECT s.* FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #DatesChanged d WHERE d.id=s.Id)
 EXCEPT SELECT b.* FROM #DatesBefore b WHERE NOT EXISTS(SELECT 1 FROM #DatesChanged d WHERE d.id=b.Id))
 THROW 51000,'An untargeted session changed.',1;
IF EXISTS(SELECT a.id,a.startsOn,a.postedThrough,a.lastPayment FROM #DateArchive a
 EXCEPT SELECT s.Id,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc
 FROM jahez_v2.AccountSessions s JOIN #DateArchive a ON a.id=s.Id)
 THROW 51000,'Restored dates do not exactly match the archive.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,
 CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,N'System',N'JahezPreResetDatesRestored',N'jahez',N'JahezAccountSession',s.Id,@now,
 @Correlation,N'User explicitly requested restoration of all last settlement and commission dates cleared by the 19 October reset. Exact pre-reset values restored; no removed financial transactions restored and no payment invented.',
 (SELECT b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc,@SourceFile ArchiveSource,@SourceHash ArchiveSha256
 FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),N'CodexAuthorizedMaintenance',1,@now,NULL
 FROM jahez_v2.AccountSessions s JOIN #DatesBefore b ON b.Id=s.Id JOIN #DatesChanged d ON d.id=s.Id;
IF @@ROWCOUNT<>309 THROW 51000,'Restoration audit count mismatch.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey=N'jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Dataset invalidation failed.',1;
SELECT (SELECT COUNT(*) FROM #DateArchive) MatchedArchivedSessions,(SELECT COUNT(*) FROM #DatesChanged) UpdatedSessions,
 (SELECT COUNT(*) FROM #DateArchive WHERE lastPayment IS NOT NULL) RestoredLastSettlements,
 (SELECT COUNT(*) FROM #DateArchive WHERE postedThrough IS NOT NULL) RestoredLastCommissions,
 (SELECT COUNT(*) FROM #DatesBefore b WHERE NOT EXISTS(SELECT 1 FROM #DateArchive a WHERE a.id=b.Id)) PreservedNewSessions,
 (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation) AuditEntries;
'@

try {
    $datesConnection.Open()
    if ($datesConnection.Database -ne 'db67927' -or $datesConnection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected hosted database target.'
    }
    [void](New-Item -ItemType Directory -Path $datesOutput)
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $datesOutput 'executed.ps1')
    Copy-Item -LiteralPath $datesSource -Destination (Join-Path $datesOutput 'source-before-reset.xml')
    $datesPlan | Set-Content -LiteralPath (Join-Path $datesOutput 'plan.json') -Encoding utf8
    $review = Read-DatesData ($datesArchiveSql + $datesReviewSql)
    $review.WriteXml((Join-Path $datesOutput 'review.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $review.Tables[0] | Format-Table -AutoSize | Out-String -Width 180 | Write-Output
    if (!$Apply -and !$Validate) { Write-Output ('Read-only review: ' + $datesOutput); return }
    $datesTransaction = $datesConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $before = Read-DatesData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey=N'jahez';
'@
    $before.WriteXml((Join-Path $datesOutput 'before-restoration.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $protectedTables = (Read-DatesData @'
SELECT s.name SchemaName,t.name TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
 WHERE (s.name IN(N'jahez_v2',N'jahez_v3') AND NOT(s.name=N'jahez_v2' AND t.name=N'AccountSessions'))
 OR (s.name=N'app' AND t.name IN(N'PlatformRiderAccounts',N'RiderClientAssignments',N'Notifications'))
 ORDER BY s.name,t.name;
'@).Tables[0].Rows
    $prepareProtected = ''
    $verifyProtected = ''
    $tableNumber = 0
    foreach ($table in $protectedTables) {
        $qualified = '[' + $table.SchemaName + '].[' + $table.TableName + ']'
        $temp = '#DatesKeep' + $tableNumber++
        $prepareProtected += "SELECT * INTO $temp FROM $qualified WITH(HOLDLOCK);`n"
        $verifyProtected += "IF EXISTS(SELECT * FROM $temp EXCEPT SELECT * FROM $qualified) OR EXISTS(SELECT * FROM $qualified EXCEPT SELECT * FROM $temp) THROW 51000,'Protected table changed: $qualified',1;`n"
    }
    $prepareProtected += "SELECT * INTO #DatesPriorAudit FROM audit.AuditEntries WITH(HOLDLOCK);`n"
    $executedSql = $datesArchiveSql + $datesPrepareSql + $prepareProtected + $datesMutationSql + $verifyProtected
    $executedSql | Set-Content -LiteralPath (Join-Path $datesOutput 'executed.sql') -Encoding utf8
    $verified = Read-DatesData ($executedSql + @'
IF EXISTS(SELECT * FROM #DatesPriorAudit EXCEPT SELECT * FROM audit.AuditEntries)
 THROW 51000,'Prior audit history changed.',1;
'@)
    $verified.WriteXml((Join-Path $datesOutput 'transaction-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $verified.Tables[0] | Format-Table -AutoSize | Out-String -Width 180 | Write-Output
    if ($Apply) {
        $datesTransaction.Commit(); $datesCommitted = $true
    } else { $datesTransaction.Rollback() }
    $datesTransaction.Dispose(); $datesTransaction = $null
    $datesConnection.Close(); $datesConnection.Open()
    $after = Read-DatesData ($datesArchiveSql + $datesReviewSql + @'
SELECT COUNT(*) AuditEntries FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@)
    $after.WriteXml((Join-Path $datesOutput 'verification-after-reconnect.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    if ($Apply) {
        if ($after.Tables[0].Rows[0].DateDifferences -ne 0 -or $after.Tables[0].Rows[0].IdentityMismatches -ne 0 -or $after.Tables[0].Rows[0].MissingSessions -ne 0 -or $after.Tables[4].Rows[0].AuditEntries -ne 309) {
            throw 'Post-commit verification failed. Do not replay; inspect the committed restoration.'
        }
    } elseif ($after.Tables[0].Rows[0].DateDifferences -ne 309 -or $after.Tables[4].Rows[0].AuditEntries -ne 0) {
        throw 'Rolled-back validation did not return to the reviewed state.'
    }
    $report = [ordered]@{
        Applied = [bool]$Apply; ValidationRolledBack = [bool]$Validate
        VerifiedAtUtc = [datetime]::UtcNow.ToString('o'); CorrelationId = $datesCorrelation
        ArchiveSource = $datesSourceRelative; ArchiveSha256 = $datesSourceHash
        MatchedArchivedSessions = 313; ChangedSessions = 309; AlreadyMatchingArchivedSessions = 4
        RestoredLastSettlementDates = 307; RestoredLastCommissionDates = 307
        OriginallyWithoutSettlementDates = 6; PreservedNewSessions = [int]$after.Tables[1].Rows[0].NewSessions
        ProtectedTablesVerified = $protectedTables.Count; FinancialTransactionsRestored = 0
        PostReconnectDateDifferences = [int]$after.Tables[0].Rows[0].DateDifferences
    }
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $datesOutput 'report.json') -Encoding utf8
    $report | ConvertTo-Json -Depth 4 | Write-Output
    Write-Output ('Evidence: ' + $datesOutput)
} catch {
    if ($datesTransaction -and !$datesCommitted) { try { $datesTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($datesTransaction) { $datesTransaction.Dispose() }
    $datesConnection.Dispose()
}
