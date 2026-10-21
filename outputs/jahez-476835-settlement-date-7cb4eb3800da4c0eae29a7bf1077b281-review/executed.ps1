param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCommitted = $false
$taskCorrelation = 'jahez-476835-settlement-date-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + $(if ($Apply) { '-applied' } else { '-review' }))
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
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database target.' }
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $taskBefore = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id='6a1bc8c1-729e-40fd-87d6-ccc82d679940';
SELECT * FROM jahez_v2.Operations WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId='6a1bc8c1-729e-40fd-87d6-ccc82d679940';
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
'@
    $taskBefore.Tables[0].TableName = 'AccountSession'
    $taskBefore.Tables[1].TableName = 'Operations'
    $taskBefore.Tables[2].TableName = 'DatasetVersion'
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Session uniqueidentifier='6a1bc8c1-729e-40fd-87d6-ccc82d679940',
 @Operation uniqueidentifier='01a14e30-a269-7c2b-8eeb-39775fa90150',
 @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',@Now datetimeoffset=SYSUTCDATETIME(),
 @Reason nvarchar(700)=N'User requested setting Jahez account 476835 last settlement date to 2026-10-14. Align historical commission baseline and refresh its unexecuted settlement calculation to accrue from 2026-10-15. No settlement execution or payment is performed.';
IF (SELECT COUNT(*) FROM app.PlatformRiderAccounts p WITH(HOLDLOCK) JOIN platform.ClientPlatforms c WITH(HOLDLOCK) ON c.Id=p.ClientPlatformId
 WHERE p.Id='a003d999-0913-4747-abcb-e13339023787' AND p.ExternalAccountId='476835' AND p.IsDeleted=0 AND c.Code='JAHEZ')<>1
 THROW 51000,'Reviewed account changed.',1;
SELECT * INTO #SessionBefore FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Session;
SELECT * INTO #OperationBefore FROM jahez_v2.Operations WITH(UPDLOCK,HOLDLOCK) WHERE HandoverId=@Session;
IF (SELECT COUNT(*) FROM #SessionBefore WHERE PlatformRiderAccountId='a003d999-0913-4747-abcb-e13339023787' AND IsDeleted=0
 AND EndedAtUtc IS NULL AND RowVersion=0x000000000002E49F AND CommissionStartsOn='20261009'
 AND CommissionPostedThrough='20261008' AND LastSettlementPaymentAtUtc='2026-10-08T00:00:00+03:00')<>1
 THROW 51000,'Reviewed session baseline changed.',1;
IF (SELECT COUNT(*) FROM jahez_v2.AccountSessions WITH(HOLDLOCK) WHERE PlatformRiderAccountId='a003d999-0913-4747-abcb-e13339023787' AND IsDeleted=0 AND EndedAtUtc IS NULL)<>1
 THROW 51000,'Expected one current session.',1;
IF (SELECT COUNT(*) FROM #OperationBefore)<>1 OR (SELECT COUNT(*) FROM #OperationBefore WHERE Id=@Operation AND IsDeleted=0 AND Status=1
 AND ExecutedAtUtc IS NULL AND ApprovedRequestId IS NULL AND Mode=1 AND PaymentStatus=1 AND ThroughDate='20261018'
 AND Net=75.94 AND Commission=150 AND CalculatedCommission=150 AND Remaining=225.94
 AND CalculationJson=N'{"Accrual":[{"Kind":"Daily","FromDate":"2026-10-09","ToDate":"2026-10-18","DailyRate":15,"Amount":150}],"UnpaidCommission":0.000000}')<>1
 THROW 51000,'Reviewed pending operation changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CommissionPolicies WITH(HOLDLOCK) WHERE HandoverId=@Session)
 OR EXISTS(SELECT 1 FROM jahez_v2.AccountingEntries WITH(HOLDLOCK) WHERE HandoverId=@Session AND Bucket=3)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements WITH(HOLDLOCK) WHERE OperationId=@Operation)
 THROW 51000,'Financial activity requires reinspection.',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=0 AND parent_id IN(OBJECT_ID('jahez_v2.AccountSessions'),OBJECT_ID('jahez_v2.Operations')))
 THROW 51000,'Review triggers before correction.',1;
UPDATE jahez_v2.AccountSessions SET LastSettlementPaymentAtUtc='2026-10-13T21:00:00+00:00',CommissionPostedThrough='20261014',
 CommissionStartsOn='20261015',UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE Id=@Session;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one updated session.',1;
UPDATE jahez_v2.Operations SET Commission=60,CalculatedCommission=60,Remaining=Net+60,
 CalculationJson=N'{"Accrual":[{"Kind":"Daily","FromDate":"2026-10-15","ToDate":"2026-10-18","DailyRate":15,"Amount":60}],"UnpaidCommission":0.000000}',
 UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE Id=@Operation;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one refreshed pending operation.',1;
-- Compare every unaffected business column with the captured records.
DECLARE @Table nvarchar(200),@Snapshot nvarchar(100),@Excluded nvarchar(500),@Columns nvarchar(max),@Sql nvarchar(max);
DECLARE taskTables CURSOR LOCAL FAST_FORWARD FOR
 SELECT 'jahez_v2.AccountSessions','#SessionBefore',',LastSettlementPaymentAtUtc,CommissionPostedThrough,CommissionStartsOn,'
 UNION ALL SELECT 'jahez_v2.Operations','#OperationBefore',',Commission,CalculatedCommission,Remaining,CalculationJson,';
OPEN taskTables; FETCH NEXT FROM taskTables INTO @Table,@Snapshot,@Excluded;
WHILE @@FETCH_STATUS=0 BEGIN
 SELECT @Columns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP(ORDER BY column_id)
 FROM sys.columns WHERE object_id=OBJECT_ID(@Table) AND name NOT IN('UpdatedAtUtc','UpdatedByUserId','RowVersion') AND CHARINDEX(','+name+',',@Excluded)=0;
 SET @Sql=N'IF EXISTS(SELECT '+@Columns+' FROM '+@Snapshot+' EXCEPT SELECT '+@Columns+' FROM '+@Table+' WHERE Id IN(SELECT Id FROM '+@Snapshot+')) THROW 51000,''Unrelated business field changed.'',1;';
 EXEC sys.sp_executesql @Sql;
 FETCH NEXT FROM taskTables INTO @Table,@Snapshot,@Excluded;
END;
CLOSE taskTables; DEALLOCATE taskTables;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','JahezSettlementCommissionDateCorrection','jahez','JahezAccountSession',@Session,@Now,@Correlation,@Reason,
 (SELECT CommissionStartsOn,CommissionPostedThrough,LastSettlementPaymentAtUtc FROM #SessionBefore FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 (SELECT CommissionStartsOn,CommissionPostedThrough,LastSettlementPaymentAtUtc FROM jahez_v2.AccountSessions WHERE Id=@Session FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor),
 (NEWID(),NEWID(),@Actor,'User','JahezPendingSettlementCalculationRefresh','jahez','JahezOperation',@Operation,@Now,@Correlation,@Reason,
 (SELECT Commission,CalculatedCommission,Remaining,CalculationJson FROM #OperationBefore FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 (SELECT Commission,CalculatedCommission,Remaining,CalculationJson FROM jahez_v2.Operations WHERE Id=@Operation FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected one Jahez dataset version.',1;
SELECT '476835' Account,CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00')) LastSettlementDate,
 s.CommissionPostedThrough,s.CommissionStartsOn,o.Commission,o.Remaining,o.ExecutedAtUtc
FROM jahez_v2.AccountSessions s JOIN jahez_v2.Operations o ON o.HandoverId=s.Id WHERE s.Id=@Session;
'@
    $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    if ($Apply) {
        $taskTransaction.Commit(); $taskCommitted = $true
        $taskTransaction.Dispose(); $taskTransaction = $null
        $taskConnection.Close(); $taskConnection.Open()
        $taskVerified = Read-TaskData @'
SELECT COUNT(*) Verified FROM jahez_v2.AccountSessions s JOIN jahez_v2.Operations o ON o.HandoverId=s.Id
WHERE s.Id='6a1bc8c1-729e-40fd-87d6-ccc82d679940' AND CONVERT(date,SWITCHOFFSET(s.LastSettlementPaymentAtUtc,'+03:00'))='20261014'
 AND s.CommissionPostedThrough='20261014' AND s.CommissionStartsOn='20261015'
 AND o.Id='01a14e30-a269-7c2b-8eeb-39775fa90150' AND o.Commission=60 AND o.CalculatedCommission=60 AND o.Remaining=135.94 AND o.ExecutedAtUtc IS NULL AND o.Status=1;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
        $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($taskVerified.Tables[0].Rows[0].Verified -ne 1 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 2) { throw 'Post-commit verification failed; do not replay.' }
        Write-Output 'COMMITTED AND VERIFIED.'
    } else {
        $taskTransaction.Rollback(); $taskTransaction.Dispose(); $taskTransaction = $null
        Write-Output 'DRY RUN VERIFIED AND ROLLED BACK.'
    }
    $taskResult.Tables[0] | Format-Table -AutoSize
    Write-Output ('Evidence: ' + $taskOutput)
} catch {
    if ($taskTransaction -and -not $taskCommitted) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
