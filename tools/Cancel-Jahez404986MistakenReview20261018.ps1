param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$repairRoot = Split-Path -Parent $PSScriptRoot
$repairConfig = Get-Content -LiteralPath (Join-Path $repairRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$repairConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $repairConfig.ConnectionStrings.LogisticsDatabase }
$repairConnection = [System.Data.SqlClient.SqlConnection]::new($repairConnectionText)
$repairTransaction = $null
$repairCorrelation = 'jahez-404986-cancel-mistaken-review-' + [guid]::NewGuid().ToString('N')
$repairOutput = Join-Path $repairRoot ('outputs/' + $repairCorrelation + $(if ($Apply) { '-applied' } else { '-review' }))
[void](New-Item -ItemType Directory -Path $repairOutput)

function Read-RepairData([string]$Sql) {
    $repairCommand = $repairConnection.CreateCommand()
    $repairCommand.Transaction = $repairTransaction
    $repairCommand.CommandTimeout = 45
    $repairCommand.CommandText = $Sql
    [void]$repairCommand.Parameters.AddWithValue('@Correlation', $repairCorrelation)
    try {
        $repairData = [System.Data.DataSet]::new()
        $repairAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($repairCommand)
        try { [void]$repairAdapter.Fill($repairData) } finally { $repairAdapter.Dispose() }
        return ,$repairData
    } finally { $repairCommand.Dispose() }
}

try {
    $repairConnection.Open()
    if ($repairConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $repairConnection.Database -ne 'db67927') {
        throw 'Unexpected database target; refusing repair.'
    }
    $repairTransaction = $repairConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $repairPreflightSql = @'
DECLARE @Operation uniqueidentifier='01a14e94-839d-7b17-a42a-a8ae2c9d6130';
DECLARE @Request uniqueidentifier='01a14eb0-eb73-73f8-9284-56a936381181';
IF NOT EXISTS(SELECT 1 FROM jahez_v2.Operations o WITH(UPDLOCK,HOLDLOCK)
 JOIN jahez_v2.AccountSessions s ON s.Id=o.HandoverId
 JOIN app.PlatformRiderAccounts a ON a.Id=s.PlatformRiderAccountId
 WHERE o.Id=@Operation AND a.Code='JHZ-404986' AND o.IsDeleted=0 AND o.Status=2
 AND o.ExecutedAtUtc IS NULL AND o.Net=-395.01 AND o.Commission=465 AND o.Remaining=69.99)
 THROW 51000,'Operation changed or is not the expected unexecuted review.',1;
IF NOT EXISTS(SELECT 1 FROM jahez_v2.Requests WITH(UPDLOCK,HOLDLOCK)
 WHERE Id=@Request AND OperationId=@Operation AND Status=1 AND Kind=5 AND AdjustmentTarget=1 AND IsDeleted=0)
 THROW 51000,'Expected pending settlement adjustment request is missing.',1;
IF (SELECT COUNT(*) FROM jahez_v2.Requests WHERE OperationId=@Operation AND Status=1 AND IsDeleted=0)<>1
 THROW 51000,'Unexpected additional pending review.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.RequestDecisions WHERE RequestId=@Request)
 OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements WHERE OperationId=@Operation)
 THROW 51000,'A decision or financial movement already exists; refusing repair.',1;
SELECT * INTO #OperationBefore FROM jahez_v2.Operations WHERE Id=@Operation;
SELECT * INTO #RequestBefore FROM jahez_v2.Requests WHERE Id=@Request;
SELECT * FROM #OperationBefore;
SELECT * FROM #RequestBefore;
SELECT * FROM jahez_v2.AccountSessions WHERE Id=(SELECT HandoverId FROM #OperationBefore);
'@
    $repairBefore = Read-RepairData $repairPreflightSql
    $repairBefore.WriteXml((Join-Path $repairOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $repairOutput 'executed.ps1')
    if (!$Apply) {
        $repairTransaction.Rollback()
        Write-Output ('Preflight passed; no changes. Evidence: ' + $repairOutput)
        return
    }
    $repairApplySql = @'
DECLARE @Now datetimeoffset=SYSUTCDATETIME();
DECLARE @Reason nvarchar(1000)=N'User explicitly requested cancelling the mistakenly created review and restoring the unexecuted settlement draft. No approval granted and no money recorded.';
UPDATE r SET Status=4,UpdatedAtUtc=@Now,UpdatedByUserId=NULL
 FROM jahez_v2.Requests r JOIN #RequestBefore b ON b.Id=r.Id AND b.RowVersion=r.RowVersion
 WHERE r.Status=1;
IF @@ROWCOUNT<>1 THROW 51000,'Request concurrency check failed.',1;
UPDATE o SET Status=1,UpdatedAtUtc=@Now,UpdatedByUserId=NULL
 FROM jahez_v2.Operations o JOIN #OperationBefore b ON b.Id=o.Id AND b.RowVersion=o.RowVersion
 WHERE o.Status=2 AND o.ExecutedAtUtc IS NULL;
IF @@ROWCOUNT<>1 THROW 51000,'Operation concurrency check failed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','CancelMistakenReview','jahez','JahezApprovalRequest',r.Id,@Now,@Correlation,@Reason,
 (SELECT b.Id,b.Status,b.OperationId,b.ProposedNet,b.ProposedCommission FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT r.Id,r.Status,r.OperationId,r.ProposedNet,r.ProposedCommission FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,NULL
 FROM jahez_v2.Requests r JOIN #RequestBefore b ON b.Id=r.Id;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','RestoreDraftAfterReviewCancellation','jahez','JahezOperation',o.Id,@Now,@Correlation,@Reason,
 (SELECT b.Id,b.Status,b.Net,b.Commission,b.Remaining,b.Fund,b.ExecutedAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT o.Id,o.Status,o.Net,o.Commission,o.Remaining,o.Fund,o.ExecutedAtUtc FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,NULL
 FROM jahez_v2.Operations o JOIN #OperationBefore b ON b.Id=o.Id;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=NULL WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected one Jahez dataset version.',1;
IF (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation)<>2
 THROW 51000,'Audit evidence is incomplete.',1;
SELECT o.Id,o.Status,o.Net,o.Commission,o.Remaining,o.Fund,o.ExecutedAtUtc FROM jahez_v2.Operations o JOIN #OperationBefore b ON b.Id=o.Id;
SELECT r.Id,r.Status,r.OperationId FROM jahez_v2.Requests r JOIN #RequestBefore b ON b.Id=r.Id;
SELECT COUNT(*) MoneyMovementCount FROM jahez_v2.MoneyMovements WHERE OperationId=(SELECT Id FROM #OperationBefore);
'@
    $repairAfter = Read-RepairData ($repairPreflightSql + [Environment]::NewLine + $repairApplySql)
    $repairAfter.WriteXml((Join-Path $repairOutput 'after.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $repairTransaction.Commit()
    Write-Output ('Committed: request Cancelled (4), operation Ready (1), no money movement. Evidence: ' + $repairOutput)
} catch {
    if ($repairTransaction -and $repairTransaction.Connection) { $repairTransaction.Rollback() }
    throw
} finally {
    if ($repairTransaction) { $repairTransaction.Dispose() }
    $repairConnection.Dispose()
}
