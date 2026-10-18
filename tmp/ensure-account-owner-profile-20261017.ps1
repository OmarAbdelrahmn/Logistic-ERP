$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$accountId = [Guid]'e919ea20-2a10-44c7-b385-9f9a3950f965'
$employeeId = [Guid]'01a09667-03ce-74ab-acd3-18db0c5f2ee1'
$correlation = 'owner-profile-repair-' + [Guid]::NewGuid().ToString('N')
$outputDir = Join-Path $workspace ('outputs/owner-profile-repair-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
[void](New-Item -ItemType Directory -Path $outputDir)
$config = Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connectionString = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$transaction = $null
$sql = @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
IF NOT EXISTS (SELECT 1 FROM app.PlatformRiderAccounts WITH (UPDLOCK,HOLDLOCK)
 WHERE Id=@AccountId AND RegisteredEmployeeId=@EmployeeId AND IsDeleted=0)
 THROW 51000,'Account ownership no longer matches the reviewed target.',1;
IF NOT EXISTS (SELECT 1 FROM app.Employees WITH (UPDLOCK,HOLDLOCK) WHERE Id=@EmployeeId AND IsDeleted=0)
 THROW 51001,'Registered owner is not available.',1;
DECLARE @EmployeeVersion binary(8)=(SELECT RowVersion FROM app.Employees WHERE Id=@EmployeeId);
DECLARE @AccountVersion binary(8)=(SELECT RowVersion FROM app.PlatformRiderAccounts WHERE Id=@AccountId);
DECLARE @ProfileId uniqueidentifier, @Deleted bit, @Created bit=0;
SELECT @ProfileId=Id,@Deleted=IsDeleted FROM app.RiderProfiles WITH (UPDLOCK,HOLDLOCK) WHERE EmployeeId=@EmployeeId;
IF @Deleted=1 THROW 51002,'An archived profile exists; automatic restoration is not authorized.',1;
IF @ProfileId IS NULL
BEGIN
 SET @ProfileId=NEWID();
 DECLARE @Now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
 INSERT app.RiderProfiles (Id,EmployeeId,CreatedAtUtc,IsDeleted)
 VALUES (@ProfileId,@EmployeeId,@Now,0);
 DECLARE @After nvarchar(max)=(SELECT Id,EmployeeId,CreatedAtUtc,IsDeleted FROM app.RiderProfiles WHERE Id=@ProfileId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 INSERT audit.AuditEntries (Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,
 OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES (NEWID(),NEWID(),NULL,'System','Created','Persistence','RiderProfile',@ProfileId,
 @Now,@CorrelationId,N'User authorized creating the missing registered-owner profile to resolve platform account assignment error 334f32f52c4bc150b6189d8bb187e1e6.',
 NULL,@After,'CodexAuthorizedMaintenance',1,@Now,NULL);
 SET @Created=1;
END;
IF (SELECT COUNT(*) FROM app.RiderProfiles WHERE EmployeeId=@EmployeeId AND IsDeleted=0)<>1
 THROW 51003,'Profile verification failed.',1;
IF NOT EXISTS (SELECT 1 FROM app.Employees WHERE Id=@EmployeeId AND RowVersion=@EmployeeVersion)
 OR NOT EXISTS (SELECT 1 FROM app.PlatformRiderAccounts WHERE Id=@AccountId AND RowVersion=@AccountVersion)
 THROW 51004,'Employee or account unexpectedly changed.',1;
SELECT @Created AS Created,@ProfileId AS RiderProfileId,@EmployeeId AS EmployeeId,@AccountId AS AccountId,
 @CorrelationId AS CorrelationId FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
'@
try {
 $connection.Open()
 if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database target.' }
 $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $command = $connection.CreateCommand()
 $command.Transaction = $transaction
 $command.CommandTimeout = 30
 $command.CommandText = $sql
 [void]$command.Parameters.AddWithValue('@AccountId',$accountId)
 [void]$command.Parameters.AddWithValue('@EmployeeId',$employeeId)
 [void]$command.Parameters.AddWithValue('@CorrelationId',$correlation)
 $sql | Set-Content (Join-Path $outputDir 'executed.sql') -Encoding utf8
 $result = $command.ExecuteScalar()
 $result | Set-Content (Join-Path $outputDir 'transaction-result.json') -Encoding utf8
 $transaction.Commit()
 $transaction.Dispose()
 $transaction = $null
 $connection.Close()
 $connection.Open()
 $command.Transaction = $null
 $command.CommandText = @'
SELECT r.Id RiderProfileId,r.EmployeeId,e.FullNameAr,e.IsEmployee,r.IsDeleted,
 a.Id AccountId,a.RegisteredEmployeeId,a.Status AccountStatus,
 (SELECT COUNT(*) FROM audit.AuditEntries x WHERE x.EntityType='RiderProfile' AND x.EntityId=r.Id AND x.CorrelationId=@CorrelationId) RepairAuditCount
FROM app.RiderProfiles r JOIN app.Employees e ON e.Id=r.EmployeeId
JOIN app.PlatformRiderAccounts a ON a.RegisteredEmployeeId=e.Id
WHERE e.Id=@EmployeeId AND a.Id=@AccountId AND r.IsDeleted=0
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
'@
 $verified = $command.ExecuteScalar()
 if ([string]::IsNullOrWhiteSpace([string]$verified)) { throw 'Post-commit verification returned no active profile; do not replay without inspection.' }
 $verified | Set-Content (Join-Path $outputDir 'post-commit-verification.json') -Encoding utf8
 Write-Output $result
 Write-Output $verified
 Write-Output ('Evidence: ' + $outputDir)
} catch [System.Data.SqlClient.SqlException] {
 if ($transaction) { $transaction.Rollback() }
 throw ('Profile operation SQL failure number ' + $_.Exception.Number + '. Check saved evidence before retrying.')
} catch {
 if ($transaction) { $transaction.Rollback() }
 throw
} finally {
 if ($transaction) { $transaction.Dispose() }
 $connection.Dispose()
}
