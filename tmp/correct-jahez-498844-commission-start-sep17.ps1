$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null
$taskCorrelation='jahez-commission-start-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation)
[void](New-Item -ItemType Directory -Path $taskOutput)
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction;$taskCommand.CommandTimeout=30;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{$taskData=[System.Data.DataSet]::new();$taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
  try{[void]$taskAdapter.Fill($taskData)}finally{$taskAdapter.Dispose()};return ,$taskData
 }finally{$taskCommand.Dispose()}
}
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBefore=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id='bc886558-35a7-403c-b59f-3a64ce47a02a';
'@
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskResult=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Session uniqueidentifier='bc886558-35a7-403c-b59f-3a64ce47a02a',
 @Account uniqueidentifier='37b9d0b6-4845-4bed-9e61-d12b44e80c1c',
 @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Now datetimeoffset=SYSUTCDATETIME(),
 @Reason nvarchar(500)=N'User explicitly requested setting current commission accrual start for Jahez account 498844 from 15 September 2026 to 17 September 2026.';
IF NOT EXISTS(SELECT 1 FROM app.PlatformRiderAccounts p JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
 WHERE p.Id=@Account AND p.ExternalAccountId='498844' AND p.IsDeleted=0 AND c.Code='JAHEZ')
 THROW 51000,'Expected Jahez account not found.',1;
IF (SELECT COUNT(*) FROM jahez_v2.AccountSessions WHERE PlatformRiderAccountId=@Account AND EndedAtUtc IS NULL AND IsDeleted=0)<>1
 THROW 51000,'Expected exactly one current session.',1;
IF NOT EXISTS(SELECT 1 FROM jahez_v2.AccountSessions WHERE Id=@Session AND PlatformRiderAccountId=@Account AND EndedAtUtc IS NULL
 AND IsDeleted=0 AND CommissionStartsOn='20260915' AND CommissionPostedThrough IS NULL
 AND CONVERT(date,SWITCHOFFSET(StartedAtUtc,'+03:00'))<='20260915'
 AND CONVERT(date,SWITCHOFFSET(LastSettlementPaymentAtUtc,'+03:00'))='20260915')
 THROW 51000,'Current session changed or requested commission start is invalid.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Operations WHERE HandoverId=@Session AND ExecutedAtUtc IS NOT NULL)
 THROW 51000,'Executed operations require review before correcting commission start.',1;
DECLARE @Before nvarchar(max)=(SELECT Id,PlatformRiderAccountId,CommissionStartsOn FROM jahez_v2.AccountSessions WHERE Id=@Session FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
DECLARE @PreservedBefore nvarchar(max)=(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions WHERE Id=@Session FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER);
UPDATE jahez_v2.AccountSessions SET CommissionStartsOn='20260917',UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
WHERE Id=@Session AND CommissionStartsOn='20260915' AND CommissionPostedThrough IS NULL AND IsDeleted=0;
IF @@ROWCOUNT<>1 THROW 51000,'Expected exactly one updated session.',1;
DECLARE @PreservedAfter nvarchar(max)=(SELECT Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,CommissionPostedThrough,LastSettlementPaymentAtUtc,Reason,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM jahez_v2.AccountSessions WHERE Id=@Session FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER);
IF @PreservedBefore<>@PreservedAfter THROW 51000,'Unrelated session fields changed.',1;
DECLARE @After nvarchar(max)=(SELECT Id,PlatformRiderAccountId,CommissionStartsOn FROM jahez_v2.AccountSessions WHERE Id=@Session FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
VALUES(NEWID(),NEWID(),@Actor,'User','JahezCommissionStartDateCorrection','jahez','JahezAccountSession',@Session,@Now,@Correlation,@Reason,@Before,@After,'CodexAuthorizedMaintenance',1,@Now,@Actor);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='jahez';
SELECT @Before BeforeJson,@After AfterJson,@Correlation CorrelationId;
'@
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-result.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
 $taskConnection.Close();$taskConnection.Open()
 $taskVerified=Read-TaskData @'
SELECT Id,CONVERT(varchar(10),CommissionStartsOn,23) CommissionStartsOn,CommissionPostedThrough,
CONVERT(varchar(10),SWITCHOFFSET(LastSettlementPaymentAtUtc,'+03:00'),23) RiyadhSettlementDate
FROM jahez_v2.AccountSessions WHERE Id='bc886558-35a7-403c-b59f-3a64ce47a02a';
SELECT Action,BeforeJson,AfterJson FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
 $taskVerified.WriteXml((Join-Path $taskOutput 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($taskVerified.Tables[0].Rows.Count -ne 1 -or $taskVerified.Tables[0].Rows[0].CommissionStartsOn -ne '2026-09-17' -or $taskVerified.Tables[0].Rows[0].RiyadhSettlementDate -ne '2026-09-15' -or $taskVerified.Tables[1].Rows.Count -ne 1){throw 'Post-commit verification failed; do not replay.'}
 Write-Output 'COMMITTED AND VERIFIED: Jahez account 498844 commission accrues starting 2026-09-17, inclusive.'
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}

