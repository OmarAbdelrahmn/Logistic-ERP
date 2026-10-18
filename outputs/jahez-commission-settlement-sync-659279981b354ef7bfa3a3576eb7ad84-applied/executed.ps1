param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCommitted = $false
$taskMode = if ($Apply) { 'applied' } else { 'review' }
$taskCorrelation = 'jahez-commission-settlement-sync-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + '-' + $taskMode)
[void](New-Item -ItemType Directory -Path $taskOutput)
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')

function Read-TaskData([string]$Sql) {
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandTimeout = 45
    $taskCommand.CommandText = $Sql
    [void]$taskCommand.Parameters.AddWithValue('@Correlation', $taskCorrelation)
    try {
        $taskData = [System.Data.DataSet]::new()
        $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
        try { [void]$taskAdapter.Fill($taskData) } finally { $taskAdapter.Dispose() }
        return ,$taskData
    } finally { $taskCommand.Dispose() }
}

try {
    $taskConnection.Open()
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') {
        throw 'Unexpected database target.'
    }
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $taskBefore = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT s.* FROM jahez_v2.AccountSessions s WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
'@
    $taskBefore.Tables[0].TableName = 'AccountSessions'
    $taskBefore.Tables[1].TableName = 'DatasetVersions'
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)

    $taskResult = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),
 @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(500)=N'User requested matching the last commission date to the recorded last settlement date for every Jahez account, so subsequent commission accrues after the settled day. Riyadh dates are used; sessions without a settlement date are preserved.';

SELECT * INTO #AllBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
SELECT s.Id,s.PlatformRiderAccountId,p.ExternalAccountId Account,
 CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')) SettlementDate
INTO #Targets
FROM jahez_v2.AccountSessions s
JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=s.PlatformRiderAccountId
JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
WHERE c.Code='JAHEZ' AND p.IsDeleted=0 AND s.IsDeleted=0 AND s.LastSettlementPaymentAtUtc IS NOT NULL
 AND (s.CommissionPostedThrough IS NULL OR s.CommissionPostedThrough<>CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')));

IF (SELECT COUNT(*) FROM #Targets)<>26
 THROW 51000,'Expected the 26 reviewed mismatching sessions. Reinspect before applying.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN #AllBefore s ON s.Id=t.Id
 WHERE s.CommissionPostedThrough IS NOT NULL OR t.SettlementDate>CONVERT(date,SWITCHOFFSET(@Now,'+03:00'))
 OR t.SettlementDate<CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))
 OR (s.EndedAtUtc IS NOT NULL AND t.SettlementDate>CONVERT(date,SWITCHOFFSET(s.EndedAtUtc,'+03:00'))))
 THROW 51000,'A reviewed session has changed or has an invalid settlement baseline.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountingEntries l WITH(HOLDLOCK) ON l.HandoverId=t.Id WHERE l.Bucket=3)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.CommissionPolicies cp WITH(HOLDLOCK) ON cp.HandoverId=t.Id)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.Operations o WITH(HOLDLOCK) ON o.HandoverId=t.Id
           WHERE o.ExecutedAtUtc IS NULL OR o.CalculatedCommission<>0 OR o.Commission<>0)
 THROW 51000,'Commission entries, policies or affected nonzero/pending operations require reinspection.',1;

UPDATE s SET CommissionPostedThrough=t.SettlementDate,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.Id=s.Id;
IF @@ROWCOUNT<>26 THROW 51000,'Expected exactly 26 updated sessions.',1;

-- Prove that every business field except the requested commission baseline is preserved.
IF EXISTS(
 SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionStartsOn,
 LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #AllBefore
 EXCEPT
 SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionStartsOn,
 LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #AllBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'Unrelated session fields changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions s JOIN #AllBefore b ON b.Id=s.Id
 WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.Id=s.Id)
 AND EXISTS(SELECT s.CommissionPostedThrough,s.UpdatedAtUtc,s.UpdatedByUserId EXCEPT SELECT b.CommissionPostedThrough,b.UpdatedAtUtc,b.UpdatedByUserId))
 THROW 51000,'An untargeted session changed.',1;

INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezCommissionSettlementDateSync','jahez','JahezAccountSession',t.Id,@Now,@Correlation,@Reason,
 (SELECT t.Account,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT t.Account,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Targets t JOIN #AllBefore b ON b.Id=t.Id JOIN jahez_v2.AccountSessions s ON s.Id=t.Id;
IF @@ROWCOUNT<>26 THROW 51000,'Audit count mismatch.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Jahez dataset version missing or duplicated.',1;

IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions s JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId
 JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
 WHERE c.Code='JAHEZ' AND p.IsDeleted=0 AND s.IsDeleted=0 AND s.LastSettlementPaymentAtUtc IS NOT NULL
 AND (s.CommissionPostedThrough IS NULL OR s.CommissionPostedThrough<>CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'))))
 THROW 51000,'A Jahez session still has mismatching settlement and commission dates.',1;

SELECT t.Account,t.Id SessionId,b.CommissionPostedThrough PreviousCommissionPostedThrough,s.CommissionPostedThrough,
 t.SettlementDate LastSettlementDate,DATEADD(day,1,s.CommissionPostedThrough) NextCommissionStartsOn,s.EndedAtUtc
FROM #Targets t JOIN #AllBefore b ON b.Id=t.Id JOIN jahez_v2.AccountSessions s ON s.Id=t.Id ORDER BY t.Account;
'@
    $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    if ($Apply) {
        $taskTransaction.Commit()
        $taskCommitted = $true
        $taskTransaction.Dispose()
        $taskTransaction = $null
        $taskConnection.Close()
        $taskConnection.Open()
        $taskVerified = Read-TaskData @'
SELECT COUNT(*) SessionsWithSettlement,
 SUM(CASE WHEN s.CommissionPostedThrough=CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')) THEN 0 ELSE 1 END) Mismatches
FROM jahez_v2.AccountSessions s JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId
JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
WHERE c.Code='JAHEZ' AND p.IsDeleted=0 AND s.IsDeleted=0 AND s.LastSettlementPaymentAtUtc IS NOT NULL;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
SELECT s.* FROM jahez_v2.AccountSessions s JOIN audit.AuditEntries a ON a.EntityId=s.Id WHERE a.CorrelationId=@Correlation;
SELECT ModuleKey,Version FROM app.DatasetVersions WHERE ModuleKey='jahez';
'@
        $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 26 -or $taskVerified.Tables[2].Rows.Count -ne 26) {
            throw 'Post-commit verification failed; do not replay.'
        }
        Write-Output ('COMMITTED AND VERIFIED: 26 corrected sessions; all ' + $taskVerified.Tables[0].Rows[0].SessionsWithSettlement + ' dated sessions match.')
    } else {
        $taskTransaction.Rollback()
        $taskTransaction.Dispose()
        $taskTransaction = $null
        Write-Output 'DRY RUN VERIFIED AND ROLLED BACK: 26 commission baselines match their settlement dates.'
    }
    $taskRows = @(foreach ($taskRow in $taskResult.Tables[0].Rows) {
        [pscustomobject]@{
            Account = $taskRow.Account
            SessionId = $taskRow.SessionId.ToString()
            LastCommissionDate = $taskRow.CommissionPostedThrough.ToString('yyyy-MM-dd')
            LastSettlementDate = $taskRow.LastSettlementDate.ToString('yyyy-MM-dd')
            NextCommissionStartsOn = $taskRow.NextCommissionStartsOn.ToString('yyyy-MM-dd')
            IsClosed = $taskRow.EndedAtUtc -isnot [DBNull]
        }
    })
    $taskRows | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $taskOutput 'report.json') -Encoding utf8
    $taskRows | Export-Csv (Join-Path $taskOutput 'report.csv') -NoTypeInformation -Encoding utf8
    Write-Output ('Evidence: ' + $taskOutput)
} catch {
    if ($taskTransaction -and -not $taskCommitted) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
