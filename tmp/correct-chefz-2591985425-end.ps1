$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction = $null
$taskCorrelation = 'chefz-end-correction-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation)
[void](New-Item -ItemType Directory -Path $taskOutput)
function Read-TaskData([string]$Sql) {
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandTimeout = 30
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
SELECT * FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK)
WHERE PlatformRiderAccountId='ccc819cd-58fd-4eed-b32c-e046f4288326' AND IsDeleted=0;
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('chefz','platform-operations');
'@
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @Assignment uniqueidentifier='ce52e580-07b7-4476-9711-8a48db7039bb',
 @Account uniqueidentifier='ccc819cd-58fd-4eed-b32c-e046f4288326',
 @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Now datetimeoffset=SYSUTCDATETIME(),
 @Reason nvarchar(500)=N'User requested correction of previous Chefz account 2591985425 assignment end date from 2026-10-06 to 2026-10-07.';
IF NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts p JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
 WHERE p.Id=@Account AND p.ExternalAccountId='2591985425' AND p.IsDeleted=0 AND c.Code IN('CHEFZ','THECHEFZ','CHEFSZ','SHIFTZ'))
 THROW 51000,'Expected Chefz account was not found.',1;
IF NOT EXISTS(SELECT 1 FROM app.RiderClientAssignments WHERE Id=@Assignment AND PlatformRiderAccountId=@Account
 AND EffectiveFrom='20261001' AND EffectiveTo='20261006' AND Status=3 AND IsDeleted=0)
 THROW 51000,'Historical assignment changed; aborting correction.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments WHERE Id<>@Assignment AND PlatformRiderAccountId=@Account
 AND IsDeleted=0 AND Status<>4 AND EffectiveFrom<='20261007' AND (EffectiveTo IS NULL OR EffectiveTo>='20261001'))
 THROW 51000,'Correction would overlap another assignment.',1;
IF NOT EXISTS(SELECT 1 FROM app.RiderClientAssignments WHERE Id='0acffaea-fb38-42a8-bf02-6ef3ce233eea'
 AND PlatformRiderAccountId=@Account AND EffectiveFrom='20261010' AND EffectiveTo IS NULL AND Status=2 AND IsDeleted=0)
 THROW 51000,'Expected current assignment changed.',1;
DECLARE @Before nvarchar(max)=(SELECT Id,PlatformRiderAccountId,EffectiveFrom,EffectiveTo,Status FROM app.RiderClientAssignments WHERE Id=@Assignment FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
UPDATE app.RiderClientAssignments SET EffectiveTo='20261007',UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
WHERE Id=@Assignment AND EffectiveTo='20261006' AND Status=3 AND IsDeleted=0;
IF @@ROWCOUNT<>1 THROW 51000,'Expected exactly one corrected assignment.',1;
INSERT app.RiderAssignmentEvents(Id,RiderClientAssignmentId,FromStatus,ToStatus,OccurredAtUtc,ActorUserId,Reason,ChangeSnapshotJson,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),@Assignment,3,3,@Now,@Actor,@Reason,N'{"PreviousEffectiveTo":"2026-10-06","EffectiveTo":"2026-10-07"}',@Now,@Actor);
DECLARE @After nvarchar(max)=(SELECT Id,PlatformRiderAccountId,EffectiveFrom,EffectiveTo,Status FROM app.RiderClientAssignments WHERE Id=@Assignment FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','ChefzAssignmentEndDateCorrection','chefz','RiderClientAssignment',@Assignment,@Now,@Correlation,@Reason,@Before,@After,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey IN('chefz','platform-operations');
SELECT @Before BeforeJson,@After AfterJson,@Correlation CorrelationId;
'@
    $taskResult.WriteXml((Join-Path $taskOutput 'transaction-result.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskTransaction.Commit()
    $taskTransaction.Dispose(); $taskTransaction = $null
    $taskConnection.Close(); $taskConnection.Open()
    $taskVerified = Read-TaskData @'
SELECT Id,CONVERT(varchar(10),EffectiveFrom,23) EffectiveFrom,CONVERT(varchar(10),EffectiveTo,23) EffectiveTo,Status
FROM app.RiderClientAssignments WHERE PlatformRiderAccountId='ccc819cd-58fd-4eed-b32c-e046f4288326' AND IsDeleted=0 ORDER BY EffectiveFrom;
SELECT Action,BeforeJson,AfterJson FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
    $taskVerified.WriteXml((Join-Path $taskOutput 'verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskHistorical = @($taskVerified.Tables[0].Rows | Where-Object { $_.Id.ToString() -eq 'ce52e580-07b7-4476-9711-8a48db7039bb' })
    if ($taskHistorical.Count -ne 1 -or $taskHistorical[0].EffectiveTo -ne '2026-10-07' -or $taskVerified.Tables[1].Rows.Count -ne 1) { throw 'Post-commit verification failed; do not replay.' }
    Write-Output 'COMMITTED AND VERIFIED: previous period 2026-10-01 through 2026-10-07; current period remains open from 2026-10-10.'
    Write-Output ('Evidence: ' + $taskOutput)
} catch {
    if ($taskTransaction) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
