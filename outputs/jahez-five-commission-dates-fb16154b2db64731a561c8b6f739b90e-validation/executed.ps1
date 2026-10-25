param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskPlan = @'
[
 {"account":"493593","date":"2026-10-01","previousDate":"2026-09-14","sessionId":"79e9dc82-ef4d-43aa-9be6-e15bca8c7571"},
 {"account":"483905","date":"2026-10-01","previousDate":"2026-09-28","sessionId":"d3dc1fc0-24df-4bf8-a5ca-af0dc75eb2f1"},
 {"account":"474572","date":"2026-10-22","previousDate":"2026-10-16","sessionId":"4679a728-59d3-4e35-be09-89c2e2910a51"},
 {"account":"408132","date":"2026-10-20","previousDate":"2026-09-23","sessionId":"e0e52bd1-cf85-4049-a45a-6d31b47724ec"},
 {"account":"405092","date":"2026-10-20","previousDate":"2026-09-13","sessionId":"b8783a1e-a8e4-4cad-970e-a160d1ed8b34"}
]
'@
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCommitted = $false
$taskCorrelation = 'jahez-five-commission-dates-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + $(if ($Apply) { '-applied' } else { '-validation' }))

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
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected hosted database target.' }
    [void](New-Item -ItemType Directory -Path $taskOutput)
    $taskPlan | Set-Content -LiteralPath (Join-Path $taskOutput 'plan.json') -Encoding utf8
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $taskBefore = Read-TaskData @'
SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT * INTO #Targets FROM OPENJSON(@Plan) WITH(account varchar(100),date date,previousDate date,sessionId uniqueidentifier);
IF (SELECT COUNT(*) FROM #Targets)<>5 OR (SELECT COUNT(DISTINCT account) FROM #Targets)<>5
 OR (SELECT COUNT(DISTINCT sessionId) FROM #Targets)<>5 THROW 51000,'Expected five unique requested accounts.',1;
SELECT * INTO #SessionsBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK);
IF EXISTS(SELECT 1 FROM #Targets t LEFT JOIN #SessionsBefore s ON s.Id=t.sessionId
 LEFT JOIN app.PlatformRiderAccounts p WITH(HOLDLOCK) ON p.Id=s.PlatformRiderAccountId
 LEFT JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE s.Id IS NULL OR p.Id IS NULL OR c.Id IS NULL OR c.Code<>'JAHEZ' OR p.ExternalAccountId<>t.account
 OR p.IsDeleted=1 OR s.IsDeleted=1 OR s.EndedAtUtc IS NOT NULL
 OR s.CommissionStartsOn<>t.previousDate OR s.CommissionPostedThrough IS NULL OR s.CommissionPostedThrough<>t.previousDate
 OR t.date IS NULL OR t.date<CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))
 OR t.date>CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+03:00'))
 OR (SELECT COUNT(*) FROM #SessionsBefore x WHERE x.PlatformRiderAccountId=p.Id AND x.IsDeleted=0 AND x.EndedAtUtc IS NULL)<>1)
 THROW 51000,'Reviewed session, account or baseline changed; reinspect before applying.',1;
SELECT l.* INTO #LedgerBefore FROM jahez_v2.AccountingEntries l WITH(HOLDLOCK) JOIN #Targets t ON t.sessionId=l.HandoverId;
IF EXISTS(SELECT 1 FROM #LedgerBefore WHERE Bucket=3)
 OR EXISTS(SELECT 1 FROM jahez_v2.Operations o WITH(HOLDLOCK) JOIN #Targets t ON t.sessionId=o.HandoverId)
 OR EXISTS(SELECT 1 FROM jahez_v2.CommissionPolicies p WITH(HOLDLOCK) JOIN #Targets t ON t.sessionId=p.HandoverId)
 THROW 51000,'Existing commission records or operations require reinspection.',1;
SELECT s.* FROM #SessionsBefore s JOIN #Targets t ON t.sessionId=s.Id;
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
SELECT * FROM #LedgerBefore;
'@
    $taskBefore.Tables[0].TableName = 'AccountSessions'
    $taskBefore.Tables[1].TableName = 'DatasetVersions'
    $taskBefore.Tables[2].TableName = 'AccountingEntries'
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult = Read-TaskData @'
SET NOCOUNT ON;
DECLARE @Now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
UPDATE s SET CommissionStartsOn=t.date,CommissionPostedThrough=t.date,UpdatedAtUtc=@Now,UpdatedByUserId=NULL
FROM jahez_v2.AccountSessions s JOIN #Targets t ON t.sessionId=s.Id;
IF @@ROWCOUNT<>5 THROW 51000,'Expected exactly five updated sessions.',1;
IF EXISTS(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,LastSettlementPaymentAtUtc,
 Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #SessionsBefore
 EXCEPT SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,LastSettlementPaymentAtUtc,
 Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions)
 OR (SELECT COUNT(*) FROM #SessionsBefore)<>(SELECT COUNT(*) FROM jahez_v2.AccountSessions)
 THROW 51000,'An unrelated session field changed.',1;
IF EXISTS(SELECT s.* FROM jahez_v2.AccountSessions s WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=s.Id)
 EXCEPT SELECT b.* FROM #SessionsBefore b WHERE NOT EXISTS(SELECT 1 FROM #Targets t WHERE t.sessionId=b.Id))
 THROW 51000,'An untargeted session changed.',1;
IF EXISTS(SELECT * FROM #LedgerBefore EXCEPT SELECT l.* FROM jahez_v2.AccountingEntries l JOIN #Targets t ON t.sessionId=l.HandoverId)
 OR EXISTS(SELECT l.* FROM jahez_v2.AccountingEntries l JOIN #Targets t ON t.sessionId=l.HandoverId EXCEPT SELECT * FROM #LedgerBefore)
 THROW 51000,'Accounting entries changed.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId
 WHERE s.CommissionStartsOn<>t.date OR s.CommissionPostedThrough IS NULL OR s.CommissionPostedThrough<>t.date)
 THROW 51000,'Requested dates were not saved exactly.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,
 CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','JahezCommissionDatesCorrection','jahez','JahezAccountSession',s.Id,@Now,@Correlation,
 N'User requested setting last commission date and calculation start date to the exact supplied date for each of five accounts. Historical date anchors only; existing last settlement payment and financial records preserved.',
 (SELECT t.account Account,b.CommissionStartsOn,b.CommissionPostedThrough,b.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT t.account Account,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,NULL
FROM #Targets t JOIN #SessionsBefore b ON b.Id=t.sessionId JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
IF @@ROWCOUNT<>5 THROW 51000,'Expected exactly five audit entries.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=NULL WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected exactly one Jahez dataset version.',1;
SELECT t.account Account,CONVERT(varchar(10),s.CommissionPostedThrough,23) LastCommissionDate,
 CONVERT(varchar(10),s.CommissionStartsOn,23) CalculationStartsOn,s.LastSettlementPaymentAtUtc
FROM #Targets t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId ORDER BY t.account;
SELECT (SELECT COUNT(*) FROM #Targets) UpdatedSessions,
 (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation) AuditEntries,
 (SELECT COUNT(*) FROM #SessionsBefore WHERE Id NOT IN(SELECT sessionId FROM #Targets)) PreservedOtherSessions,
 (SELECT COUNT(*) FROM #LedgerBefore) PreservedAccountingEntries;
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
SELECT t.account Account,CONVERT(varchar(10),s.CommissionPostedThrough,23) LastCommissionDate,
 CONVERT(varchar(10),s.CommissionStartsOn,23) CalculationStartsOn,s.LastSettlementPaymentAtUtc
FROM OPENJSON(@Plan) WITH(account varchar(100),date date,sessionId uniqueidentifier) t
JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId ORDER BY t.account;
SELECT COUNT(*) Verified,SUM(CASE WHEN s.CommissionStartsOn=t.date AND s.CommissionPostedThrough=t.date THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(date date,sessionId uniqueidentifier) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
        $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($taskVerified.Tables[1].Rows[0].Verified -ne 5 -or $taskVerified.Tables[1].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[2].Rows[0].AuditCount -ne 5) {
            throw 'Post-commit verification failed; do not replay.'
        }
        foreach ($taskRow in $taskVerified.Tables[0].Rows) {
            $taskOld = @($taskBefore.Tables[0].Rows | Where-Object { $_.Id -eq ($taskPlan | ConvertFrom-Json | Where-Object account -eq $taskRow.Account).sessionId })
            if ($taskOld.Count -ne 1 -or -not [object]::Equals($taskOld[0].LastSettlementPaymentAtUtc,$taskRow.LastSettlementPaymentAtUtc)) { throw 'Post-commit last payment preservation mismatch; do not replay.' }
        }
        Write-Output 'COMMITTED AND VERIFIED: all five accounts have both requested dates.'
    } else {
        $taskTransaction.Rollback()
        $taskTransaction.Dispose()
        $taskTransaction = $null
        Write-Output 'VALIDATED AND ROLLED BACK: all five date changes and preservation checks passed.'
    }
    $taskResult.Tables[0] | Select-Object Account,LastCommissionDate,CalculationStartsOn,LastSettlementPaymentAtUtc |
        Export-Csv (Join-Path $taskOutput 'report.csv') -NoTypeInformation -Encoding utf8
    $taskResult.Tables[0] | Format-Table Account,LastCommissionDate,CalculationStartsOn -AutoSize
    $taskResult.Tables[1] | Format-Table -AutoSize
    Write-Output ('Evidence: '+$taskOutput)
} catch {
    if ($taskTransaction -and -not $taskCommitted) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
