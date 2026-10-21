param([switch]$Apply,[switch]$Validate)
$ErrorActionPreference='Stop'
if($Apply -and $Validate){throw 'Choose validation or application, not both.'}
$freshWorkspace=Split-Path -Parent $PSScriptRoot
$freshFolder=Join-Path $freshWorkspace 'outputs/jahez-financial-reset-20261019'
$freshRunFolder=Join-Path $freshFolder $(if($Apply){'applied'}else{'validation'})
$freshDate=[datetime]'2026-10-19'
$freshCarryPlan=Get-Content -LiteralPath (Join-Path $freshWorkspace 'outputs/jahez-v2-treasury-carryforward-20261019/carryforward-plan.json') -Raw
$freshConfig=Get-Content (Join-Path $freshWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$freshConnection=[System.Data.SqlClient.SqlConnection]::new($(if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$freshConfig.ConnectionStrings.LogisticsDatabase}))
$freshTransaction=$null
$freshBeforeSummary='{}'
$freshFinancialReceipt="Operation LIKE N'v2.operation.%' OR Operation LIKE N'v2.cashbox.%' OR Operation LIKE N'v2.fee.%' OR Operation LIKE N'v2.debt.%' OR Operation IN(N'earnings',N'v3.cash',N'v3.transfer',N'v3.reverse',N'v3.attribute',N'v3.backfill',N'v3.verify',N'v3.activate',N'v3.petro.evidence') OR Operation LIKE N'v3.handover.%'"
$freshFinancialNotification="SourceEntityType IN(N'JahezTransferredDebt',N'JahezOperation',N'JahezCashboxHandover') OR EventType IN(N'jahez.rider.block.created',N'jahez.rider.block.released',N'jahez.settlement.overdue')"
$freshClear=@('jahez_v2.AccountFees','jahez_v2.AccountingEntries','jahez_v2.CashboxHandoverMovements','jahez_v2.CashboxHandovers','jahez_v2.EarningsStatements','jahez_v2.MoneyMovements','jahez_v2.Operations','jahez_v2.SettlementReminders','jahez_v2.TransferredDebts','jahez_v3.HandoverLines','jahez_v3.HandoverPostings','jahez_v3.Ownership','jahez_v3.Postings')
function Read-FreshData([string]$Sql,[int]$Timeout=180){
 $command=$freshConnection.CreateCommand();$command.Transaction=$freshTransaction;$command.CommandTimeout=$Timeout;$command.CommandText=$Sql
 if($Sql.Contains('@ResetDate') -or $Sql.Contains('@CarryPlan') -or $Sql.Contains('@BeforeSummary')){
  [void]$command.Parameters.Add('@ResetDate',[System.Data.SqlDbType]::Date);$command.Parameters['@ResetDate'].Value=$freshDate
  [void]$command.Parameters.Add('@CarryPlan',[System.Data.SqlDbType]::NVarChar,-1);$command.Parameters['@CarryPlan'].Value=$freshCarryPlan
  [void]$command.Parameters.Add('@BeforeSummary',[System.Data.SqlDbType]::NVarChar,-1);$command.Parameters['@BeforeSummary'].Value=$freshBeforeSummary
 }
 $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command)
 try{$data=[System.Data.DataSet]::new();[void]$adapter.Fill($data);return ,$data}finally{$adapter.Dispose();$command.Dispose()}
}
$freshPreflight=@'
IF EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1 OR ActivatedAtUtc IS NOT NULL)
 THROW 51000,'This is a pre-activation reset only.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers WHERE TreasuryVersion<>2)
 OR EXISTS(SELECT 1 FROM jahez_v3.BalanceVerifications) OR EXISTS(SELECT 1 FROM jahez_v3.PetroEvidence)
 THROW 51000,'New V3 workflow evidence exists; review required.',1;
IF EXISTS(SELECT Id FROM jahez_v3.Postings EXCEPT SELECT Id FROM OPENJSON(@CarryPlan,'$.postings') WITH(Id uniqueidentifier '$.id'))
 THROW 51000,'Postings outside the legacy carryforward exist.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Requests WHERE Status=1) THROW 51000,'Resolve pending requests before reset.',1;
IF (SELECT COUNT(*) FROM sys.triggers WHERE parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) AND is_disabled=0)<>8
 THROW 51000,'Treasury protections must be enabled before reset.',1;
SELECT s.name AS SchemaName,t.name AS TableName,SUM(p.rows) AS Records FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
 JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1) WHERE s.name IN(N'jahez_v2',N'jahez_v3') GROUP BY s.name,t.name ORDER BY s.name,t.name;
SELECT IsActive,ActivatedAtUtc,NextHandoverNumber FROM jahez_v3.State;
SELECT COUNT(*) AS Requests,COUNT(OperationId) AS LinksToRemovedOperations FROM jahez_v2.Requests;
'@
$freshVerify=@'
IF EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1 OR ActivatedAtUtc IS NOT NULL)
 THROW 51000,'Reset must not activate treasury.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions WHERE CommissionStartsOn<DATEADD(day,1,@ResetDate) OR CommissionPostedThrough IS NOT NULL OR LastSettlementPaymentAtUtc IS NOT NULL)
 THROW 51000,'Old commission or settlement anchors remain.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Requests WHERE OperationId IS NOT NULL)
 THROW 51000,'A request still references a removed settlement.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE schema_id IN(SCHEMA_ID(N'jahez_v2'),SCHEMA_ID(N'jahez_v3')) AND (is_disabled=1 OR is_not_trusted=1))
 OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE schema_id IN(SCHEMA_ID(N'jahez_v2'),SCHEMA_ID(N'jahez_v3')) AND (is_disabled=1 OR is_not_trusted=1))
 THROW 51000,'Financial SQL constraints must remain enabled and trusted.',1;
IF (SELECT COUNT(*) FROM sys.triggers WHERE parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) AND is_disabled=0)<>8
 THROW 51000,'All eight treasury protection triggers must be re-enabled.',1;
SELECT COUNT(*) AS DailyOrders,SUM(Count) AS TotalOrders FROM jahez_v2.DailyOrders;
SELECT COUNT(*) AS Batches FROM jahez_v2.ImportBatches;
SELECT COUNT(*) AS Files FROM jahez_v2.ImportFiles;
SELECT COUNT(*) AS ImportRows FROM jahez_v2.ImportRows;
SELECT COUNT(*) AS Requests FROM jahez_v2.Requests;
SELECT COUNT(*) AS Decisions FROM jahez_v2.RequestDecisions;
SELECT COUNT(*) AS Sessions,SUM(CASE WHEN EndedAtUtc IS NOT NULL THEN 1 ELSE 0 END) AS ClosedSessions FROM jahez_v2.AccountSessions;
SELECT COUNT(*) AS TreasuryAccounts FROM jahez_v3.TreasurySubAccounts;
SELECT IsActive,ActivatedAtUtc,NextHandoverNumber FROM jahez_v3.State;
'@

try{
 $freshConnection.Open()
 if($freshConnection.Database -ne 'db67927' -or $freshConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected hosted target.'}
 $prior=Read-FreshData "SELECT COUNT(*) AS Applied FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019';"
 if($prior.Tables[0].Rows[0].Applied -ne 0){Write-Output 'The one-off reset was already committed. No new work was removed.';return}
 $preflight=Read-FreshData $freshPreflight
 $preflight.Tables[0]|Format-Table -AutoSize|Out-String|Write-Output
 if(!$Apply -and !$Validate){Write-Output 'Read-only preflight complete. Orders, imports and requests are outside the financial reset.';return}
 [void](New-Item -ItemType Directory -Path $freshRunFolder -Force)
 $preflight.WriteXml((Join-Path $freshRunFolder 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $backup=Read-FreshData @'
DECLARE @folder nvarchar(4000)=CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000));
IF NULLIF(@folder,N'') IS NULL THROW 51000,'Backup location unavailable.',1;
DECLARE @path nvarchar(4000)=@folder+N'\db67927-before-jahez-financial-reset-'+REPLACE(CONVERT(nvarchar(36),NEWID()),N'-',N'')+N'.bak';
BACKUP DATABASE [db67927] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
SELECT @path AS ServerBackupPath,CAST(1 AS bit) AS BackupCommandCompleted,CAST(0 AS bit) AS RestoreVerifyOnlyPerformed,TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00') AS CompletedAtUtc;
'@
  $backup.WriteXml((Join-Path $freshRunFolder 'server-backup.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 }
 $freshTransaction=$freshConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 [void](Read-FreshData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @lock int;EXEC @lock=sys.sp_getapplock @Resource=N'jahez-financial-fresh-start-20261019',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @lock<0 THROW 51000,'Could not acquire reset lock.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019') THROW 51000,'Reset committed concurrently.',1;
'@)
 $locked=Read-FreshData $freshPreflight
 $summary=@($locked.Tables[0].Rows|ForEach-Object{[ordered]@{Schema=$_.SchemaName;Table=$_.TableName;Rows=[long]$_.Records}})
 $freshBeforeSummary=$summary|ConvertTo-Json -Depth 4 -Compress
 $protected=@('jahez_v2.DailyOrders','jahez_v2.ImportBatches','jahez_v2.ImportFiles','jahez_v2.ImportRows','jahez_v2.PlatformTransactions','jahez_v2.Requests','jahez_v2.RequestDecisions','jahez_v2.CommissionPolicies','jahez_v2.AccountSessions','jahez_v2.CommandReceipts',
 'jahez_v3.State','jahez_v3.TreasurySubAccounts','jahez_v3.BalanceVerifications','jahez_v3.PetroEvidence',
 'app.PlatformRiderAccounts','app.Employees','app.RiderProfiles','app.RiderClientAssignments','app.RiderAssignmentEvents','app.PlatformAccountCredentialVersions','app.VehiclePlatformAccountAssignments','app.RiderVehicleAssignments','app.Vehicles','app.Sponsors','app.ClientContracts','platform.ClientPlatforms')
 $other=(Read-FreshData "SELECT s.name+N'.'+t.name AS QualifiedName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name LIKE N'chefz%' OR s.name LIKE N'ninja%';").Tables[0].Rows
 $protected+=@($other|ForEach-Object{[string]$_.QualifiedName})
 $allTables=@($protected+$freshClear|Select-Object -Unique)
 $snapshots=@{};$index=0
 foreach($table in $allTables){
  $parts=$table.Split('.');$qualified='['+$parts[0]+'].['+$parts[1]+']';$temp='#FreshKeep'+$index++
  $columns=(Read-FreshData "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$qualified') ORDER BY column_id;").Tables[0].Rows
  if(!$columns.Count){throw "Missing table: $table"}
  $columnNames=@($columns|ForEach-Object{[string]$_.name});$cols=($columnNames|ForEach-Object{'['+$_.Replace(']',']]')+']'})-join ','
  [void](Read-FreshData "SELECT $cols INTO $temp FROM $qualified WITH(TABLOCKX,HOLDLOCK);")
  $snapshots[$table]=@{Temp=$temp;Qualified=$qualified;Columns=$columnNames}
  if($table.StartsWith('jahez_v2.') -or $freshClear -contains $table){
   $archive=Read-FreshData "SELECT * FROM $temp;"
   $archive.WriteXml((Join-Path $freshRunFolder ($table+'-before.xml')),[System.Data.XmlWriteMode]::WriteSchema)
  }
 }
 # Preserve non-financial notifications and all existing audit records; append only our reset event.
 [void](Read-FreshData "SELECT * INTO #FreshNotifications FROM app.Notifications WITH(HOLDLOCK) WHERE CASE WHEN ($freshFinancialNotification) THEN 1 ELSE 0 END=0;SELECT * INTO #FreshAudit FROM audit.AuditEntries WITH(HOLDLOCK);SELECT tr.object_id,tr.name,tr.is_disabled,m.definition INTO #FreshTriggers FROM sys.triggers tr JOIN sys.sql_modules m ON m.object_id=tr.object_id WHERE tr.parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id IN(SCHEMA_ID(N'jahez_v2'),SCHEMA_ID(N'jahez_v3')));")
 $notificationArchive=Read-FreshData "SELECT * FROM app.Notifications WHERE $freshFinancialNotification;"
 $notificationArchive.WriteXml((Join-Path $freshRunFolder 'financial-notifications-before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 [void](Read-FreshData $freshPreflight)
 $sqlPath=Join-Path $freshWorkspace 'database/scripts/jahez-financial-fresh-start-20261019.sql'
 Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $freshRunFolder 'executed.sql')
 [void](Read-FreshData (Get-Content -LiteralPath $sqlPath -Raw))

 foreach($table in $protected){
  $snapshot=$snapshots[$table]
  $except=if($table -eq 'jahez_v2.AccountSessions'){@('CommissionStartsOn','CommissionPostedThrough','LastSettlementPaymentAtUtc','UpdatedAtUtc','UpdatedByUserId','RowVersion')}
   elseif($table -eq 'jahez_v2.Requests'){@('OperationId','UpdatedAtUtc','UpdatedByUserId','RowVersion')}
   elseif($table -eq 'jahez_v2.CommandReceipts'){@('PayloadHash','ResultJson')}else{@()}
  $cols=(@($snapshot.Columns|Where-Object{$_ -notin $except}|ForEach-Object{'['+$_.Replace(']',']]')+']'}))-join ','
  [void](Read-FreshData "IF EXISTS(SELECT $cols FROM $($snapshot.Temp) EXCEPT SELECT $cols FROM $($snapshot.Qualified)) OR EXISTS(SELECT $cols FROM $($snapshot.Qualified) EXCEPT SELECT $cols FROM $($snapshot.Temp)) THROW 51000,'A protected operational record changed.',1;")
 }
 $session=$snapshots['jahez_v2.AccountSessions'].Temp
 $receipt=$snapshots['jahez_v2.CommandReceipts'].Temp
 $request=$snapshots['jahez_v2.Requests'].Temp
 [void](Read-FreshData @"
IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions h JOIN $session old ON old.Id=h.Id WHERE h.CommissionStartsOn<>CASE WHEN old.CommissionStartsOn>DATEADD(day,1,@ResetDate) THEN old.CommissionStartsOn ELSE DATEADD(day,1,@ResetDate) END)
 THROW 51000,'A future commission start was changed incorrectly.',1;
IF EXISTS(SELECT * FROM $receipt WHERE NOT($freshFinancialReceipt) EXCEPT SELECT * FROM jahez_v2.CommandReceipts WHERE NOT($freshFinancialReceipt))
 OR EXISTS(SELECT * FROM jahez_v2.CommandReceipts WHERE NOT($freshFinancialReceipt) EXCEPT SELECT * FROM $receipt WHERE NOT($freshFinancialReceipt))
 THROW 51000,'Operational command receipts changed.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CommandReceipts WHERE ($freshFinancialReceipt) AND (PayloadHash<>N'retired:financial-reset:20261019' OR ResultJson<>N'{}'))
 THROW 51000,'An old financial command can still replay.',1;
IF EXISTS(SELECT * FROM $request WHERE OperationId IS NULL EXCEPT SELECT * FROM jahez_v2.Requests WHERE Id IN(SELECT Id FROM $request WHERE OperationId IS NULL))
 THROW 51000,'An unrelated request was changed.',1;
IF EXISTS(SELECT * FROM #FreshNotifications EXCEPT SELECT * FROM app.Notifications WHERE CASE WHEN ($freshFinancialNotification) THEN 1 ELSE 0 END=0)
 OR EXISTS(SELECT * FROM app.Notifications WHERE CASE WHEN ($freshFinancialNotification) THEN 1 ELSE 0 END=0 EXCEPT SELECT * FROM #FreshNotifications)
 THROW 51000,'Unrelated notifications changed.',1;
IF EXISTS(SELECT * FROM #FreshAudit EXCEPT SELECT * FROM audit.AuditEntries) THROW 51000,'Existing audit history changed.',1;
IF EXISTS(SELECT object_id,name,is_disabled,definition FROM #FreshTriggers EXCEPT
 SELECT tr.object_id,tr.name,tr.is_disabled,m.definition FROM sys.triggers tr JOIN sys.sql_modules m ON m.object_id=tr.object_id WHERE tr.parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id IN(SCHEMA_ID(N'jahez_v2'),SCHEMA_ID(N'jahez_v3'))))
 THROW 51000,'A treasury trigger changed or remained disabled.',1;
"@)
 foreach($table in $freshClear){$q=$snapshots[$table].Qualified;[void](Read-FreshData "IF EXISTS(SELECT 1 FROM $q) THROW 51000,'Financial rows remain after reset.',1;")}
 $verified=Read-FreshData $freshVerify
 $verified.WriteXml((Join-Path $freshRunFolder 'verification-in-transaction.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $retired=(Read-FreshData "SELECT COUNT(*) AS Retired FROM jahez_v2.CommandReceipts WHERE PayloadHash=N'retired:financial-reset:20261019';").Tables[0].Rows[0].Retired
 if($Apply){$freshTransaction.Commit()}else{$freshTransaction.Rollback()}
 $freshTransaction.Dispose();$freshTransaction=$null
 $freshConnection.Close();$freshConnection.Open()
 if($Apply){
  $verified=Read-FreshData $freshVerify
  foreach($table in $freshClear){$parts=$table.Split('.');$q='['+$parts[0]+'].['+$parts[1]+']';[void](Read-FreshData "IF EXISTS(SELECT 1 FROM $q) THROW 51000,'Financial rows remain after commit.',1;")}
  $verified.WriteXml((Join-Path $freshRunFolder 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 }else{
  [void](Read-FreshData "IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019') THROW 51000,'Validation unexpectedly persisted the reset.',1;IF EXISTS(SELECT 1 FROM sys.triggers WHERE parent_id IN(SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v3')) AND is_disabled=1) THROW 51000,'Validation left a trigger disabled.',1;")
 }
 $report=[ordered]@{Applied=[bool]$Apply;ValidationRolledBack=[bool](!$Apply);VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');ResetThroughRiyadhDate='2026-10-19';CommissionResumesOn='2026-10-20';ClearedFinancialTables=$freshClear.Count;PreservedTables=$protected.Count;
  DailyOrderRows=[long]$verified.Tables[0].Rows[0].DailyOrders;TotalOrders=[long]$verified.Tables[0].Rows[0].TotalOrders;ImportBatches=[int]$verified.Tables[1].Rows[0].Batches;ImportFiles=[int]$verified.Tables[2].Rows[0].Files;ImportRows=[long]$verified.Tables[3].Rows[0].ImportRows;
  PreservedRequests=[int]$verified.Tables[4].Rows[0].Requests;PreservedDecisions=[int]$verified.Tables[5].Rows[0].Decisions;PreservedSessions=[int]$verified.Tables[6].Rows[0].Sessions;TreasuryAccounts=[int]$verified.Tables[7].Rows[0].TreasuryAccounts;
  RetiredFinancialKeys=[int]$retired;TreasuryActivated=$false;RealWorldBalancesVerified=$false;RestoreVerifyOnlyPerformed=$false;OtherPlatformsPreserved=$true;SqlSha256=(Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash}
 $report|ConvertTo-Json -Depth 5|Set-Content (Join-Path $freshRunFolder 'report.json') -Encoding utf8
 $report|ConvertTo-Json -Depth 5
}catch{if($freshTransaction){try{$freshTransaction.Rollback()}catch{}};throw}finally{if($freshTransaction){$freshTransaction.Dispose()};$freshConnection.Dispose()}
