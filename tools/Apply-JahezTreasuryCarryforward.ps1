param([switch]$Apply)
$ErrorActionPreference='Stop'
$carryWorkspace=Split-Path -Parent $PSScriptRoot
$carryFolder=Join-Path $carryWorkspace 'outputs/jahez-v2-treasury-carryforward-20261019'
$carryPlanPath=Join-Path $carryFolder 'carryforward-plan.json'
$carryPlanJson=Get-Content -LiteralPath $carryPlanPath -Raw
$carryPlan=$carryPlanJson|ConvertFrom-Json
$carryPlanSha=(Get-FileHash -LiteralPath $carryPlanPath -Algorithm SHA256).Hash
$carryGuards=Get-Content -LiteralPath (Join-Path $carryFolder 'source-guards.json') -Raw|ConvertFrom-Json
$carryConfig=Get-Content (Join-Path $carryWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$carryConnection=[System.Data.SqlClient.SqlConnection]::new($(if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$carryConfig.ConnectionStrings.LogisticsDatabase}))
$carryTransaction=$null
function Read-CarryData([string]$Sql,[int]$Timeout=120){
 $command=$carryConnection.CreateCommand();$command.Transaction=$carryTransaction;$command.CommandTimeout=$Timeout;$command.CommandText=$Sql
 if($Sql.Contains('@Plan')){
  [void]$command.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1);$command.Parameters['@Plan'].Value=$carryPlanJson
  [void]$command.Parameters.Add('@PlanSha256',[System.Data.SqlDbType]::NVarChar,64);$command.Parameters['@PlanSha256'].Value=$carryPlanSha
 }
 $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command)
 try{$data=[System.Data.DataSet]::new();[void]$adapter.Fill($data);return ,$data}finally{$adapter.Dispose();$command.Dispose()}
}
$carryVerify=@'
IF EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1) THROW 51000,'Carryforward must not activate the treasury.',1;
IF (SELECT COUNT(*) FROM jahez_v3.Ownership)<>(SELECT COUNT(*) FROM OPENJSON(@Plan,'$.ownership'))
 OR (SELECT COUNT(*) FROM jahez_v3.Postings)<>(SELECT COUNT(*) FROM OPENJSON(@Plan,'$.postings'))
 OR (SELECT COUNT(*) FROM jahez_v3.HandoverLines)<>(SELECT COUNT(*) FROM OPENJSON(@Plan,'$.handoverLines'))
 OR (SELECT COUNT(*) FROM jahez_v3.HandoverPostings)<>(SELECT COUNT(*) FROM OPENJSON(@Plan,'$.handoverPostings'))
 THROW 51000,'Unexpected treasury population after historical carryforward.',1;
IF EXISTS(SELECT p.Source,p.SourceId,p.PlatformAccountId,p.SponsorId,p.SponsorIdentifier,p.SponsorNameAr
 FROM OPENJSON(@Plan,'$.ownership') WITH(Source int '$.source',SourceId uniqueidentifier '$.sourceId',PlatformAccountId uniqueidentifier '$.platformAccountId',
 SponsorId uniqueidentifier '$.sponsorId',SponsorIdentifier nvarchar(100) '$.sponsorIdentifier',SponsorNameAr nvarchar(200) '$.sponsorNameAr') p
 EXCEPT SELECT Source,SourceId,PlatformAccountId,SponsorId,SponsorIdentifier,SponsorNameAr FROM jahez_v3.Ownership)
 THROW 51000,'Preserved ownership does not match the reviewed evidence.',1;
IF EXISTS(SELECT p.Id,p.SubAccountId,p.SponsorId,p.MoneyMovementId,p.CashboxHandoverId,p.BatchId,p.Kind,p.Amount,p.OccurredAtUtc,p.ActorUserId,p.ActorNameAr
 FROM OPENJSON(@Plan,'$.postings') WITH(Id uniqueidentifier '$.id',SubAccountId uniqueidentifier '$.subAccountId',SponsorId uniqueidentifier '$.sponsorId',
 MoneyMovementId uniqueidentifier '$.moneyMovementId',CashboxHandoverId uniqueidentifier '$.cashboxHandoverId',BatchId uniqueidentifier '$.batchId',Kind int '$.kind',
 Amount decimal(18,2) '$.amount',OccurredAtUtc datetimeoffset '$.occurredAtUtc',ActorUserId uniqueidentifier '$.actorUserId',ActorNameAr nvarchar(200) '$.actorNameAr') p
 EXCEPT SELECT Id,SubAccountId,SponsorId,MoneyMovementId,CashboxHandoverId,BatchId,Kind,Amount,OccurredAtUtc,ActorUserId,ActorNameAr FROM jahez_v3.Postings)
 THROW 51000,'Postings differ from the reviewed source and exact cash transfer plan.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements m WHERE NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=4 AND o.SourceId=m.Id)
 OR NOT EXISTS(SELECT 1 FROM jahez_v3.Postings p WHERE p.Kind=1 AND p.MoneyMovementId=m.Id))
 THROW 51000,'A historical movement is missing attribution or its posting.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers h WHERE h.Status=2 AND NOT EXISTS(SELECT 1 FROM jahez_v3.HandoverLines l WHERE l.HandoverId=h.Id))
 THROW 51000,'A confirmed historical handover was not reconstructed.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.Postings WHERE Kind IN(3,4,5)) OR EXISTS(SELECT 1 FROM jahez_v3.BalanceVerifications) OR EXISTS(SELECT 1 FROM jahez_v3.PetroEvidence)
 THROW 51000,'Do not invent opening balances, adjustments, reversals, balance verification or wallet evidence.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.Postings p JOIN jahez_v3.TreasurySubAccounts a ON a.Id=p.SubAccountId WHERE p.SponsorId<>a.SponsorId)
 THROW 51000,'A sponsor posting crossed into another sponsor account.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.TreasurySubAccounts a CROSS APPLY(SELECT COALESCE(SUM(p.Amount),0) AS Amount FROM jahez_v3.Postings p
 WHERE p.SubAccountId=a.Id AND p.CashboxHandoverId IS NULL AND NOT EXISTS(SELECT 1 FROM jahez_v3.HandoverPostings l
 JOIN jahez_v2.CashboxHandovers h ON h.Id=l.HandoverId AND h.Status=2 WHERE l.PostingId=p.Id)) remaining
 CROSS APPLY(SELECT COALESCE(SUM(CASE WHEN m.Direction=1 THEN m.Amount ELSE -m.Amount END),0) AS Amount
 FROM jahez_v2.MoneyMovements m JOIN jahez_v3.Ownership o ON o.Source=4 AND o.SourceId=m.Id
 WHERE m.Fund=1 AND o.SponsorId=a.SponsorId AND a.Purpose=CASE WHEN m.Purpose=3 THEN 2 ELSE 1 END
 AND NOT EXISTS(SELECT 1 FROM jahez_v2.CashboxHandoverMovements l JOIN jahez_v2.CashboxHandovers h ON h.Id=l.CashboxHandoverId AND h.Status=2 WHERE l.MoneyMovementId=m.Id)) old
 WHERE a.IsPosting=1 AND a.Location=1 AND remaining.Amount<>old.Amount)
 THROW 51000,'Remaining custody differs from original unreconciled cash membership.',1;
SELECT COUNT(*) AS ProvenSources FROM jahez_v3.Ownership;
SELECT Kind,COUNT(*) AS EntryCount,SUM(Amount) AS SignedAmount FROM jahez_v3.Postings GROUP BY Kind;
SELECT s.RegistryNameAr AS SponsorName,a.NameAr AS AccountName,a.Location,a.Purpose,COALESCE(SUM(p.Amount),0) AS ReconstructedBalance
 FROM jahez_v3.TreasurySubAccounts a JOIN app.Sponsors s ON s.Id=a.SponsorId LEFT JOIN jahez_v3.Postings p ON p.SubAccountId=a.Id
 WHERE a.IsPosting=1 GROUP BY s.RegistryNameAr,a.NameAr,a.Location,a.Purpose,a.Code ORDER BY a.Code;
SELECT COUNT(*) AS PreservedMovements,SUM(Amount) AS OriginalAmounts FROM jahez_v2.MoneyMovements;
SELECT COUNT(*) AS PreservedHandovers,SUM(CashboxAmount) AS ConfirmedPhysicalCash FROM jahez_v2.CashboxHandovers WHERE Status=2;
SELECT COUNT(*) AS ExactPostingMemberships FROM jahez_v3.HandoverPostings;
SELECT IsActive,ActivatedAtUtc FROM jahez_v3.State;
SELECT COUNT(*) AS MissingHistoricalOwnership FROM (
 SELECT f.Id FROM jahez_v2.AccountFees f WHERE NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=1 AND o.SourceId=f.Id)
 UNION ALL SELECT p.Id FROM jahez_v2.Operations p WHERE p.Status=3 AND NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=2 AND o.SourceId=p.Id)
 UNION ALL SELECT d.Id FROM jahez_v2.TransferredDebts d WHERE NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=3 AND o.SourceId=d.Id)
 UNION ALL SELECT m.Id FROM jahez_v2.MoneyMovements m WHERE NOT EXISTS(SELECT 1 FROM jahez_v3.Ownership o WHERE o.Source=4 AND o.SourceId=m.Id)) missing;
'@
try{
 $carryConnection.Open()
 if($carryConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $carryConnection.Database -ne 'db67927'){throw 'Unexpected hosted target.'}
 $sourceHash=(Get-FileHash -LiteralPath (Join-Path $carryFolder 'source-evidence.xml') -Algorithm SHA256).Hash
 if($sourceHash -ne $carryPlan.sourceSnapshotSha256){throw 'The reviewed source evidence file changed.'}
 $prior=Read-CarryData "SELECT AfterJson FROM audit.AuditEntries WHERE CorrelationId=N'jahez-v2-treasury-carryforward-20261019' AND Action=N'JahezV2TreasuryCarryforward';"
 if($prior.Tables[0].Rows.Count -gt 0){
  if($prior.Tables[0].Rows.Count -ne 1 -or ($prior.Tables[0].Rows[0].AfterJson|ConvertFrom-Json).PlanSha256 -ne $carryPlanSha){throw 'A different carryforward was already committed. Do not replay or overwrite it.'}
  $verified=Read-CarryData $carryVerify
  $verified.WriteXml((Join-Path $carryFolder 'idempotent-recheck.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  Write-Output 'The identical carryforward is already committed and verified; no duplicate rows created.'
  return
 }
 foreach($guard in $carryGuards){
  $actual=Read-CarryData $guard.Sql
  if([string]$actual.Tables[0].Rows[0].SnapshotHash -ne $guard.Sha256){throw "Source changed since review: $($guard.Table). Refresh the review, do not apply a stale plan."}
 }
 if(!$Apply){Write-Output 'Reviewed source hashes match. No financial data changed. Use -Apply to carry the reviewed history.';return}

 $backup=Read-CarryData @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Server backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-jahez-carryforward-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,CAST(0 AS bit) AS RestoreVerifyOnlyPerformed,
 TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00') AS CompletedAtUtc;
'@ 180
 $backup.WriteXml((Join-Path $carryFolder 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $carryTransaction=$carryConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 [void](Read-CarryData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @lock int;
EXEC @lock=sys.sp_getapplock @Resource=N'jahez-v2-treasury-carryforward-20261019',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @lock<0 THROW 51000,'Could not acquire carryforward lock.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-v2-treasury-carryforward-20261019')
 THROW 51000,'Carryforward was committed concurrently; verify before replay.',1;
'@)
 $tableRows=(Read-CarryData "SELECT s.name AS SchemaName,t.name AS TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=N'jahez_v2' OR (s.name=N'app' AND t.name IN(N'PlatformRiderAccounts',N'Sponsors',N'RiderClientAssignments')) ORDER BY s.name,t.name;").Tables[0].Rows
 $checks=@();$index=0
 foreach($row in $tableRows){
  $qualified='['+$row.SchemaName+'].['+$row.TableName+']';$temp='#CarryKeep'+$index++
  $columns=(Read-CarryData "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$qualified') ORDER BY column_id;").Tables[0].Rows
  $cols=(@($columns|ForEach-Object{'['+([string]$_.name).Replace(']',']]')+']'}))-join ','
  [void](Read-CarryData "SELECT $cols INTO $temp FROM $qualified WITH(TABLOCKX,HOLDLOCK);")
  $checks+="IF EXISTS(SELECT $cols FROM $temp EXCEPT SELECT $cols FROM $qualified) OR EXISTS(SELECT $cols FROM $qualified EXCEPT SELECT $cols FROM $temp) THROW 51000,'An original V2 financial or platform record changed.',1;"
 }
 foreach($guard in $carryGuards){
  $actual=Read-CarryData $guard.Sql
  if([string]$actual.Tables[0].Rows[0].SnapshotHash -ne $guard.Sha256){throw "Locked source changed since review: $($guard.Table)."}
 }
 $sqlPath=Join-Path $carryWorkspace 'database/scripts/jahez-treasury-carryforward-20261019.sql'
 [void](Read-CarryData (Get-Content -LiteralPath $sqlPath -Raw))
 foreach($check in $checks){[void](Read-CarryData $check)}
 $verified=Read-CarryData $carryVerify
 $verified.WriteXml((Join-Path $carryFolder 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $carryTransaction.Commit();$carryTransaction.Dispose();$carryTransaction=$null
 $carryConnection.Close();$carryConnection.Open()
 $verified=Read-CarryData $carryVerify
 $verified.WriteXml((Join-Path $carryFolder 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $report=[ordered]@{Applied=$true;VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');ProvenSources=[int]$verified.Tables[0].Rows[0].ProvenSources;
  OriginalMovements=[int]$verified.Tables[3].Rows[0].PreservedMovements;ConfirmedHandovers=[int]$verified.Tables[4].Rows[0].PreservedHandovers;
  ConfirmedPhysicalCash=[decimal]$verified.Tables[4].Rows[0].ConfirmedPhysicalCash;ExactMemberships=[int]$verified.Tables[5].Rows[0].ExactPostingMemberships;
  TreasuryActivated=$false;FundingInvented=$false;OriginalV2TablesPreserved=$tableRows.Count;ExistingRowsComparedInBothDirections=$true;
  RestoreVerifyOnlyPerformed=$false;PlanSha256=$carryPlanSha;SqlSha256=(Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash;
  BankReconciliation='Historical payouts carried without invented funding; verified opening/funding evidence is still required.'}
 $report|ConvertTo-Json -Depth 5|Set-Content (Join-Path $carryFolder 'report.json') -Encoding utf8
 $report|ConvertTo-Json -Depth 5
}catch{if($carryTransaction){try{$carryTransaction.Rollback()}catch{}};throw}finally{if($carryTransaction){$carryTransaction.Dispose()};$carryConnection.Dispose()}
