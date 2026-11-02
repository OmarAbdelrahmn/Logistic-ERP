param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCorrelation = 'jahez-debt-method-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + $(if ($Apply) { '-applied' } else { '-validation' }))
function Query([string]$Sql) {
    $command = $taskConnection.CreateCommand()
    $command.Transaction = $taskTransaction
    $command.CommandTimeout = 90
    $command.CommandText = $Sql
    [void]$command.Parameters.AddWithValue('@Correlation', $taskCorrelation)
    try {
        $data = [System.Data.DataSet]::new()
        $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
        try { [void]$adapter.Fill($data) } finally { $adapter.Dispose() }
        return ,$data
    } finally { $command.Dispose() }
}
try {
    if ($taskConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $taskConnection.Database -ne 'db67927') { throw 'Unexpected database target.' }
    $taskConnection.Open()
    [void](New-Item -ItemType Directory -Path $taskOutput)
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $before = Query @'
SET NOCOUNT ON; SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261031132700_AddOilChangeEditCancellationWorkflow')
 THROW 51000,'Required predecessor missing.',1;
IF EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261031143205_AllowJahezPetroAppDebtReceipts')
 THROW 51000,'Already applied; inspect before repeating.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('jahez_v2.MoneyMovements')
 AND name='CK_JahezMoney_Routing' AND is_disabled=0 AND is_not_trusted=0) THROW 51000,'Routing check unavailable.',1;
SELECT * INTO #MovementBefore FROM jahez_v2.MoneyMovements WITH(UPDLOCK,HOLDLOCK) WHERE Id='01a19250-7397-7943-828b-7e919ff916b3';
IF (SELECT COUNT(*) FROM #MovementBefore WHERE Fund=1 AND Direction=1 AND Purpose=4 AND Amount=40.66
 AND CommissionPaid=40.66 AND DashboardDebtPaid=0 AND Reference=N'JHZ/2026/10/00821'
 AND TransferredDebtId='01a186be-0eaa-78d5-a22f-2a4ceee00b0d' AND OperationId IS NULL)<>1
 THROW 51000,'Reviewed movement changed.',1;
IF NOT EXISTS(SELECT 1 FROM #MovementBefore m JOIN jahez_v2.AccountSessions s WITH(HOLDLOCK) ON s.Id=m.HandoverId
 JOIN app.PlatformRiderAccounts a WITH(HOLDLOCK) ON a.Id=s.PlatformRiderAccountId
 JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE a.ExternalAccountId='456433' AND p.Code='JAHEZ' AND a.IsDeleted=0)
 THROW 51000,'Account mismatch.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandoverMovements l WITH(HOLDLOCK) JOIN jahez_v2.CashboxHandovers h WITH(HOLDLOCK)
 ON h.Id=l.CashboxHandoverId WHERE l.MoneyMovementId='01a19250-7397-7943-828b-7e919ff916b3' AND h.Status<>3)
 THROW 51000,'Movement is in a handover.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.State WITH(UPDLOCK,HOLDLOCK) WHERE IsActive=1)
 OR EXISTS(SELECT 1 FROM jahez_v3.Postings WITH(HOLDLOCK) WHERE MoneyMovementId='01a19250-7397-7943-828b-7e919ff916b3')
 THROW 51000,'Treasury requires posting correction; reinspect.',1;
SELECT * INTO #DebtBefore FROM jahez_v2.TransferredDebts WITH(HOLDLOCK) WHERE Id='01a186be-0eaa-78d5-a22f-2a4ceee00b0d';
IF (SELECT COUNT(*) FROM #DebtBefore WHERE Amount=40.66 AND PaidAmount=40.66 AND PaidAtUtc IS NOT NULL)<>1 THROW 51000,'Debt changed.',1;
SELECT * FROM #MovementBefore;
SELECT * FROM #DebtBefore;
SELECT * FROM jahez_v3.Ownership WHERE Source=4 AND SourceId='01a19250-7397-7943-828b-7e919ff916b3';
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE name='CK_JahezMoney_Routing';
SELECT COUNT_BIG(*) MovementCount,SUM(Amount) MovementTotal FROM jahez_v2.MoneyMovements WITH(HOLDLOCK);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='jahez';
'@
    $before.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $taskSql = Get-Content (Join-Path $taskRoot 'database/scripts/jahez-petroapp-debt-receipts.sql') -Raw
    Copy-Item (Join-Path $taskRoot 'database/scripts/jahez-petroapp-debt-receipts.sql') (Join-Path $taskOutput 'migration.sql')
    $taskSql = $taskSql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
    [void](Query $taskSql)
    $result = Query @'
DECLARE @Definition nvarchar(max)=(SELECT definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('jahez_v2.MoneyMovements') AND name='CK_JahezMoney_Routing' AND is_disabled=0 AND is_not_trusted=0);
IF @Definition IS NULL THROW 51000,'Routing constraint is not trusted.',1;
CREATE TABLE #Routes(Fund int,Direction int,Purpose int,Amount decimal(18,2),OperationId uniqueidentifier,Expected bit);
INSERT #Routes SELECT f.v,d.v,p.v,10,NULL,
 CASE WHEN d.v=1 AND (f.v=1 OR (f.v=2 AND p.v IN(1,2,4))) THEN 1
 WHEN d.v=2 AND f.v IN(1,3) AND p.v<>2 THEN 1 ELSE 0 END
 FROM (VALUES(1),(2),(3),(99)) f(v) CROSS JOIN (VALUES(1),(2)) d(v) CROSS JOIN (VALUES(1),(2),(3),(4),(5)) p(v);
DECLARE @Sql nvarchar(max)=N'IF EXISTS(SELECT 1 FROM #Routes WHERE CASE WHEN '+@Definition+N' THEN 1 ELSE 0 END<>Expected) THROW 51000,''Routing matrix mismatch.'',1;';
EXEC sp_executesql @Sql;
DECLARE @Now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
UPDATE jahez_v2.MoneyMovements SET Fund=2 WHERE Id='01a19250-7397-7943-828b-7e919ff916b3' AND Fund=1;
IF @@ROWCOUNT<>1 THROW 51000,'Expected one corrected receipt.',1;
IF EXISTS(SELECT Id,HandoverId,RiderProfileId,OperationId,TransferredDebtId,Direction,Purpose,Amount,ActorUserId,OccurredAtUtc,Reference,Reason,CreatedAtUtc,CreatedByUserId,CommissionPaid,DashboardDebtPaid FROM #MovementBefore
 EXCEPT SELECT Id,HandoverId,RiderProfileId,OperationId,TransferredDebtId,Direction,Purpose,Amount,ActorUserId,OccurredAtUtc,Reference,Reason,CreatedAtUtc,CreatedByUserId,CommissionPaid,DashboardDebtPaid FROM jahez_v2.MoneyMovements WHERE Id='01a19250-7397-7943-828b-7e919ff916b3')
 THROW 51000,'Receipt fields changed unexpectedly.',1;
IF EXISTS(SELECT * FROM #DebtBefore EXCEPT SELECT * FROM jahez_v2.TransferredDebts WHERE Id='01a186be-0eaa-78d5-a22f-2a4ceee00b0d') THROW 51000,'Debt changed unexpectedly.',1;
INSERT jahez_v2.AccountingEntries(Id,HandoverId,Bucket,Kind,Amount,SourceId,OccurredAtUtc,Reason,CreatedAtUtc,CreatedByUserId,CalculationJson)
SELECT NEWID(),HandoverId,3,6,0,NEWID(),@Now,N'تصحيح طريقة تحصيل المديونية من الخزنة إلى بترواب بناءً على طلب المستخدم',@Now,NULL,
 (SELECT 'DebtPaymentMethodUpdated' Action,Id MovementId,1 PreviousFund,2 Fund,Amount,Reference FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM #MovementBefore;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','JahezDebtPaymentMethodCorrection','jahez','JahezMoneyMovement',Id,@Now,@Correlation,
 N'User confirmed account 456433 receipt 40.66 SAR should be recorded through PetroApp instead of Cashbox.',
 (SELECT b.* FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 (SELECT m.* FROM jahez_v2.MoneyMovements m WHERE m.Id=b.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,NULL FROM #MovementBefore b;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=NULL WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected Jahez dataset version.',1;
SELECT Id,Fund,Amount,Reference,Purpose,CommissionPaid,DashboardDebtPaid FROM jahez_v2.MoneyMovements WHERE Id='01a19250-7397-7943-828b-7e919ff916b3';
SELECT COUNT(*) VerifiedRoutes FROM #Routes;
SELECT COUNT_BIG(*) MovementCount,SUM(Amount) MovementTotal FROM jahez_v2.MoneyMovements;
SELECT name,is_disabled,is_not_trusted FROM sys.check_constraints WHERE name='CK_JahezMoney_Routing';
'@
    if ($before.Tables[4].Rows[0].MovementCount -ne $result.Tables[2].Rows[0].MovementCount -or $before.Tables[4].Rows[0].MovementTotal -ne $result.Tables[2].Rows[0].MovementTotal) { throw 'Movement totals changed.' }
    $result.WriteXml((Join-Path $taskOutput 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    if ($Apply) {
        $taskTransaction.Commit(); $taskTransaction.Dispose(); $taskTransaction=$null
        $taskConnection.Close(); $taskConnection.Open()
        $verified=Query @'
IF (SELECT COUNT(*) FROM jahez_v2.MoneyMovements WHERE Id='01a19250-7397-7943-828b-7e919ff916b3' AND Fund=2 AND Amount=40.66 AND Purpose=4 AND Reference='JHZ/2026/10/00821')<>1 THROW 51000,'Saved receipt mismatch.',1;
IF (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation)<>1 THROW 51000,'Audit missing.',1;
SELECT Id,Fund,Amount,Reference,Purpose FROM jahez_v2.MoneyMovements WHERE Id='01a19250-7397-7943-828b-7e919ff916b3';
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId='20261031143205_AllowJahezPetroAppDebtReceipts';
'@
        $verified.WriteXml((Join-Path $taskOutput 'post-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
        Write-Output 'COMMITTED AND VERIFIED: migration and 40.66 SAR PetroApp debt receipt.'
    } else {
        $taskTransaction.Rollback(); $taskTransaction.Dispose(); $taskTransaction=$null
        Write-Output 'VALIDATED AND ROLLED BACK: migration, 40 routing cases, receipt correction and preservation.'
    }
    Write-Output ('Evidence: '+$taskOutput)
} finally {
    if ($taskTransaction) { $taskTransaction.Rollback(); $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
