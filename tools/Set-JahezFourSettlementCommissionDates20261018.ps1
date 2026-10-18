param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskPlan = @'
[
 {"account":"491306","date":"2026-10-14","accountId":"226427a7-d441-4082-927f-6019a6d9651c","sessionId":"01a14e55-a93a-75e3-b4f9-9b4363512fbc","rowVersion":"AAAAAAAC7bk="},
 {"account":"476885","date":"2026-10-12","accountId":"9aab2a55-9e69-43eb-8f3d-dc5340315917","sessionId":"01a14e59-6c92-771f-ab02-aa9fcdfed811","rowVersion":"AAAAAAAC7iY="},
 {"account":"456420","date":"2026-10-12","accountId":"22e23827-8420-4714-89f1-8d4d251d8881","sessionId":"01a14e50-799a-7a6a-8017-198dc1384a31","rowVersion":"AAAAAAAC7Zc="},
 {"account":"420509","date":"2026-10-11","accountId":"a88090b0-c1b4-41ef-aec4-0fe70d69536d","sessionId":"01a14e51-f9d2-726b-b2e4-9e9ee8189a16","rowVersion":"AAAAAAAC7ac="}
]
'@
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCommitted = $false
$taskCorrelation = 'jahez-four-settlement-commission-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + $(if ($Apply) { '-applied' } else { '-review' }))
[void](New-Item -ItemType Directory -Path $taskOutput)
$taskPlan | Set-Content (Join-Path $taskOutput 'plan.json') -Encoding utf8
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')

function Read-TaskData([string]$Sql) {
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandTimeout = 45
    $taskCommand.CommandText = $Sql
    [void]$taskCommand.Parameters.Add('@Plan', [System.Data.SqlDbType]::NVarChar, -1)
    $taskCommand.Parameters['@Plan'].Value = $taskPlan
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
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK)
WHERE Id IN(SELECT sessionId FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier));
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
'@
    $taskBefore.Tables[0].TableName = 'AccountSessions'
    $taskBefore.Tables[1].TableName = 'DatasetVersions'
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(), @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(700)=N'User requested correcting last settlement and commission calculation dates for Jahez accounts 491306 (2026-10-14), 476885 (2026-10-12), 456420 (2026-10-12), 420509 (2026-10-11). Set historical date anchors; commission resumes after the supplied baseline day. No payment or ledger entry is created.';
SELECT * INTO #Targets FROM OPENJSON(@Plan)
 WITH(account varchar(100),date date,accountId uniqueidentifier,sessionId uniqueidentifier,rowVersion varchar(32));
IF (SELECT COUNT(*) FROM #Targets)<>4 OR (SELECT COUNT(DISTINCT account) FROM #Targets)<>4
 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>4
 THROW 51000,'Expected exactly four unique accounts and sessions.',1;
SELECT s.* INTO #Before FROM jahez_v2.AccountSessions s WITH(UPDLOCK,HOLDLOCK)
JOIN #Targets t ON t.sessionId=s.Id;
IF (SELECT COUNT(*) FROM #Before)<>4 OR EXISTS(
 SELECT 1 FROM #Targets t LEFT JOIN #Before s ON s.Id=t.sessionId
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=t.accountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR p.Id IS NULL OR c.Id IS NULL OR p.ExternalAccountId<>t.account OR p.IsDeleted=1 OR c.Code<>'JAHEZ'
 OR s.PlatformRiderAccountId<>t.accountId OR s.IsDeleted=1 OR s.EndedAtUtc IS NOT NULL
 OR s.RowVersion<>CAST(N'' AS xml).value('xs:base64Binary(sql:column("t.rowVersion"))','varbinary(8)')
 OR s.CommissionPostedThrough IS NOT NULL OR s.LastSettlementPaymentAtUtc IS NOT NULL OR s.CommissionStartsOn<>'20261019'
 OR t.date IS NULL OR t.date>CONVERT(date,SWITCHOFFSET(@Now,'+03:00')))
 THROW 51000,'A reviewed account or date baseline changed; reinspect before applying.',1;
IF EXISTS(SELECT 1 FROM #Targets t WHERE (SELECT COUNT(*) FROM jahez_v2.AccountSessions s WITH(HOLDLOCK)
 WHERE s.PlatformRiderAccountId=t.accountId AND s.IsDeleted=0 AND s.EndedAtUtc IS NULL)<>1)
 THROW 51000,'Expected exactly one current session per account.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountingEntries l WITH(HOLDLOCK) ON l.HandoverId=t.sessionId WHERE l.Bucket=3)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.CommissionPolicies cp WITH(HOLDLOCK) ON cp.HandoverId=t.sessionId)
 OR EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.Operations o WITH(HOLDLOCK) ON o.HandoverId=t.sessionId)
 THROW 51000,'Commission records or operations require reinspection.',1;

UPDATE s SET LastSettlementPaymentAtUtc=SWITCHOFFSET(TODATETIMEOFFSET(CONVERT(datetime2,t.date),'+03:00'),'+00:00'),
 CommissionPostedThrough=t.date,CommissionStartsOn=DATEADD(day,1,t.date),UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id;
IF @@ROWCOUNT<>4 THROW 51000,'Expected exactly four updated sessions.',1;
IF EXISTS(
 SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #Before
 EXCEPT SELECT s.Id,s.PlatformRiderAccountId,s.RiderProfileId,s.RiderClientAssignmentId,s.StartedAtUtc,s.EndedAtUtc,s.Reason,s.CreatedAtUtc,s.CreatedByUserId,s.IsDeleted,s.DeletedAtUtc,s.DeletedByUserId,s.DeletionReason
 FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id)
 THROW 51000,'An unrelated session field changed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezSettlementCommissionDateCorrection','jahez','JahezAccountSession',s.Id,@Now,@Correlation,@Reason,
 (SELECT t.account Account,b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT t.account Account,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM #Targets t JOIN #Before b ON b.Id=t.sessionId JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
IF @@ROWCOUNT<>4 THROW 51000,'Expected four audit entries.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected exactly one Jahez dataset version.',1;
SELECT t.account Account,CONVERT(varchar(10),SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'),23) LastSettlementDate,
 CONVERT(varchar(10),s.CommissionPostedThrough,23) LastCommissionDate,
 CONVERT(varchar(10),s.CommissionStartsOn,23) NextCommissionStartsOn
FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId ORDER BY t.account;
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
SELECT COUNT(*) Verified,SUM(CASE WHEN s.LastSettlementPaymentAtUtc IS NOT NULL
 AND CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'))=t.date
 AND s.CommissionPostedThrough=t.date AND s.CommissionStartsOn=DATEADD(day,1,t.date) THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier,date date) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
        $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($taskVerified.Tables[0].Rows[0].Verified -ne 4 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 4) {
            throw 'Post-commit verification failed; do not replay.'
        }
        Write-Output 'COMMITTED AND VERIFIED: four settlement and commission date baselines corrected.'
    } else {
        $taskTransaction.Rollback()
        $taskTransaction.Dispose()
        $taskTransaction = $null
        Write-Output 'DRY RUN VERIFIED AND ROLLED BACK.'
    }
    $taskResult.Tables[0] | Select-Object Account,LastSettlementDate,LastCommissionDate,NextCommissionStartsOn |
        Export-Csv (Join-Path $taskOutput 'report.csv') -NoTypeInformation -Encoding utf8
    $taskResult.Tables[0] | Format-Table -AutoSize
    Write-Output ('Evidence: ' + $taskOutput)
} catch {
    if ($taskTransaction -and -not $taskCommitted) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
