param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$removalRoot = Split-Path -Parent $PSScriptRoot
$removalConfig = Get-Content -LiteralPath (Join-Path $removalRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$removalConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $removalConfig.ConnectionStrings.LogisticsDatabase }
$removalConnection = [System.Data.SqlClient.SqlConnection]::new($removalConnectionText)
$removalTransaction = $null
$removalVersion = $null
$removalCorrelation = 'jahez-remove-mistaken-request-' + [guid]::NewGuid().ToString('N')

function Read-RemovalData([string]$Sql) {
    $removalCommand = $removalConnection.CreateCommand()
    $removalCommand.Transaction = $removalTransaction
    $removalCommand.CommandTimeout = 45
    $removalCommand.CommandText = $Sql
    [void]$removalCommand.Parameters.AddWithValue('@Correlation', $removalCorrelation)
    if ($removalVersion) {
        [void]$removalCommand.Parameters.Add('@Version', [System.Data.SqlDbType]::Binary, 8)
        $removalCommand.Parameters['@Version'].Value = $removalVersion
    }
    try {
        $removalData = [System.Data.DataSet]::new()
        $removalAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($removalCommand)
        try { [void]$removalAdapter.Fill($removalData) } finally { $removalAdapter.Dispose() }
        return ,$removalData
    } finally { $removalCommand.Dispose() }
}

$removalEvidenceSql = @'
SELECT * FROM jahez_v2.Requests WITH(UPDLOCK,HOLDLOCK) WHERE Id='01a14eb0-eb73-73f8-9284-56a936381181';
SELECT Id,Status,Net,Commission,Remaining,Fund,ExecutedAtUtc,RowVersion FROM jahez_v2.Operations WITH(HOLDLOCK)
 WHERE Id='01a14e94-839d-7b17-a42a-a8ae2c9d6130';
SELECT COUNT_BIG(*) MovementCount,COALESCE(SUM(Amount),0) MovementAmount FROM jahez_v2.MoneyMovements WITH(HOLDLOCK)
 WHERE OperationId='01a14e94-839d-7b17-a42a-a8ae2c9d6130';
'@

try {
    $removalConnection.Open()
    if ($removalConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $removalConnection.Database -ne 'db67927') {
        throw 'Unexpected database target; refusing removal.'
    }
    $removalTransaction = $removalConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $removalBefore = Read-RemovalData $removalEvidenceSql
    if ($removalBefore.Tables[0].Rows.Count -ne 1 -or $removalBefore.Tables[1].Rows.Count -ne 1) { throw 'Expected request and operation are missing.' }
    $removalRequest = $removalBefore.Tables[0].Rows[0]
    if ($removalRequest.IsDeleted) {
        $removalTransaction.Rollback()
        Write-Output 'Request already removed from the active list; no changes.'
        return
    }
    if ($removalRequest.Status -ne 4 -or $removalRequest.Kind -ne 5 -or
        $removalRequest.OperationId -ne [guid]'01a14e94-839d-7b17-a42a-a8ae2c9d6130') {
        throw 'Request is not the expected cancelled mistaken review.'
    }
    $removalVersion = $removalRequest.RowVersion
    if (!$Apply) {
        $removalTransaction.Rollback()
        Write-Output 'Preflight passed: exact cancelled request identified; no changes.'
        return
    }
    $removalOutput = Join-Path $removalRoot ('outputs/' + $removalCorrelation + '-applied')
    [void](New-Item -ItemType Directory -Path $removalOutput)
    $removalBefore.WriteXml((Join-Path $removalOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $removalOutput 'executed.ps1')
    [void](Read-RemovalData @'
DECLARE @Request uniqueidentifier='01a14eb0-eb73-73f8-9284-56a936381181';
DECLARE @Now datetimeoffset=SYSUTCDATETIME();
DECLARE @Reason nvarchar(500)=N'User explicitly requested removing the mistakenly created and cancelled review request from the requests list.';
IF EXISTS(SELECT 1 FROM jahez_v2.RequestDecisions WHERE RequestId=@Request)
 OR EXISTS(SELECT 1 FROM jahez_v2.Operations WHERE ApprovedRequestId=@Request)
 THROW 51000,'Request has a decision or is used as an approval; refusing removal.',1;
DECLARE @Before nvarchar(max)=(SELECT Id,Status,IsDeleted,OperationId FROM jahez_v2.Requests WHERE Id=@Request FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
UPDATE jahez_v2.Requests SET IsDeleted=1,DeletedAtUtc=@Now,DeletedByUserId=NULL,DeletionReason=@Reason,UpdatedAtUtc=@Now,UpdatedByUserId=NULL
 WHERE Id=@Request AND RowVersion=@Version AND IsDeleted=0 AND Status=4;
IF @@ROWCOUNT<>1 THROW 51000,'Request concurrency check failed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),NULL,'System','SoftDeleteMistakenReview','jahez','JahezApprovalRequest',r.Id,@Now,@Correlation,@Reason,@Before,
 (SELECT r.Id,r.Status,r.IsDeleted,r.OperationId,r.DeletedAtUtc,r.DeletionReason FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,NULL FROM jahez_v2.Requests r WHERE r.Id=@Request;
IF @@ROWCOUNT<>1 THROW 51000,'Audit entry missing.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=NULL WHERE ModuleKey='jahez';
IF @@ROWCOUNT<>1 THROW 51000,'Expected one Jahez dataset version.',1;
'@)
    $removalAfter = Read-RemovalData $removalEvidenceSql
    if (!$removalAfter.Tables[0].Rows[0].IsDeleted -or
        [Convert]::ToBase64String($removalBefore.Tables[1].Rows[0].RowVersion) -ne [Convert]::ToBase64String($removalAfter.Tables[1].Rows[0].RowVersion) -or
        $removalBefore.Tables[2].Rows[0].MovementCount -ne $removalAfter.Tables[2].Rows[0].MovementCount -or
        $removalBefore.Tables[2].Rows[0].MovementAmount -ne $removalAfter.Tables[2].Rows[0].MovementAmount) {
        throw 'Removal verification failed or settlement data changed.'
    }
    $removalAfter.WriteXml((Join-Path $removalOutput 'after.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $removalTransaction.Commit()
    Write-Output ('Committed: mistaken request removed from active lists; operation and money movements unchanged. Evidence: ' + $removalOutput)
} catch {
    if ($removalTransaction -and $removalTransaction.Connection) { $removalTransaction.Rollback() }
    throw
} finally {
    if ($removalTransaction) { $removalTransaction.Dispose() }
    $removalConnection.Dispose()
}
