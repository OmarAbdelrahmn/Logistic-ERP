$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null
$taskCorrelation='fuel-card-rename-'+[guid]::NewGuid().ToString('N')
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
SELECT * FROM app.FuelCards WITH(UPDLOCK,HOLDLOCK) WHERE Id IN('01a10b3c-19d1-75f4-bcb0-aa43e891a6dc','01a10b3c-19d0-7f72-b14f-c28cffeace4a');
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey='fuel';
'@
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskResult=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Now datetimeoffset=SYSUTCDATETIME(),@Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Reason nvarchar(500)=N'User requested correcting fuel card numbers 2318SKA to 2318SXA and 2323SKA to 2323SXA.';
DECLARE @Targets TABLE(Id uniqueidentifier PRIMARY KEY,OldNumber nvarchar(100),NewNumber nvarchar(100));
INSERT @Targets VALUES('01a10b3c-19d1-75f4-bcb0-aa43e891a6dc','2318SKA','2318SXA'),('01a10b3c-19d0-7f72-b14f-c28cffeace4a','2323SKA','2323SXA');
SELECT c.* INTO #Before FROM app.FuelCards c WITH(UPDLOCK,HOLDLOCK) JOIN @Targets t ON t.Id=c.Id;
IF (SELECT COUNT(*) FROM #Before)<>2 OR EXISTS(SELECT 1 FROM @Targets t LEFT JOIN #Before c ON c.Id=t.Id WHERE c.Id IS NULL OR c.CardNumber<>t.OldNumber OR c.NormalizedCardNumber<>t.OldNumber OR c.Provider<>1 OR c.IsDeleted=1)
 THROW 51000,'Original card records changed or missing.',1;
IF EXISTS(SELECT 1 FROM app.FuelCards c WITH(UPDLOCK,HOLDLOCK) JOIN @Targets t ON t.NewNumber=c.NormalizedCardNumber WHERE c.Provider=1 AND c.IsDeleted=0)
 THROW 51000,'A destination card number already exists.',1;
SELECT * INTO #AssignmentsBefore FROM app.FuelCardRiderAssignments WITH(HOLDLOCK) WHERE FuelCardId IN(SELECT Id FROM @Targets);
SELECT * INTO #UsageBefore FROM app.FuelCardMonthlyUsages WITH(HOLDLOCK) WHERE FuelCardId IN(SELECT Id FROM @Targets);
UPDATE c SET CardNumber=t.NewNumber,NormalizedCardNumber=t.NewNumber,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM app.FuelCards c JOIN @Targets t ON t.Id=c.Id;
IF @@ROWCOUNT<>2 THROW 51000,'Expected exactly two renamed cards.',1;
IF EXISTS(SELECT Id,Provider,IdentifierType,PlateNumberText,NormalizedPlateNumber,Notes,SponsorId,OperatingCityId,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM #Before
 EXCEPT SELECT Id,Provider,IdentifierType,PlateNumberText,NormalizedPlateNumber,Notes,SponsorId,OperatingCityId,CreatedAtUtc,CreatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason FROM app.FuelCards WHERE Id IN(SELECT Id FROM @Targets))
 OR EXISTS(SELECT * FROM #AssignmentsBefore EXCEPT SELECT * FROM app.FuelCardRiderAssignments WHERE FuelCardId IN(SELECT Id FROM @Targets))
 OR EXISTS(SELECT * FROM app.FuelCardRiderAssignments WHERE FuelCardId IN(SELECT Id FROM @Targets) EXCEPT SELECT * FROM #AssignmentsBefore)
 OR EXISTS(SELECT * FROM #UsageBefore EXCEPT SELECT * FROM app.FuelCardMonthlyUsages WHERE FuelCardId IN(SELECT Id FROM @Targets))
 OR EXISTS(SELECT * FROM app.FuelCardMonthlyUsages WHERE FuelCardId IN(SELECT Id FROM @Targets) EXCEPT SELECT * FROM #UsageBefore)
 THROW 51000,'Unrelated card fields, assignments or usage changed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','FuelCardNumberCorrected','fuel','FuelCard',t.Id,@Now,@Correlation,@Reason,
(SELECT b.CardNumber,b.NormalizedCardNumber FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),
(SELECT c.CardNumber,c.NormalizedCardNumber FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM @Targets t JOIN #Before b ON b.Id=t.Id JOIN app.FuelCards c ON c.Id=t.Id;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor WHERE ModuleKey='fuel';
SELECT t.OldNumber,c.Id,c.CardNumber,c.NormalizedCardNumber FROM @Targets t JOIN app.FuelCards c ON c.Id=t.Id;
'@
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-result.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
 $taskConnection.Close();$taskConnection.Open()
 $taskVerified=Read-TaskData @'
SELECT Id,CardNumber,NormalizedCardNumber FROM app.FuelCards WHERE Id IN('01a10b3c-19d1-75f4-bcb0-aa43e891a6dc','01a10b3c-19d0-7f72-b14f-c28cffeace4a') AND IsDeleted=0;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
 $taskVerified.WriteXml((Join-Path $taskOutput 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($taskVerified.Tables[0].Rows.Count -ne 2 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 2){throw 'Post-commit verification failed; do not replay.'}
 foreach($taskRow in $taskVerified.Tables[0].Rows){$taskExpected=if($taskRow.Id.ToString() -eq '01a10b3c-19d1-75f4-bcb0-aa43e891a6dc'){'2318SXA'}else{'2323SXA'};if($taskRow.CardNumber -ne $taskExpected -or $taskRow.NormalizedCardNumber -ne $taskExpected){throw 'Saved card number mismatch; do not replay.'}}
 Write-Output 'COMMITTED AND VERIFIED: 2318SKA -> 2318SXA; 2323SKA -> 2323SXA.'
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
