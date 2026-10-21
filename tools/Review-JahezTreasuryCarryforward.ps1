param([string]$PythonPath = 'python')
$ErrorActionPreference='Stop'
$carryWorkspace=Split-Path -Parent $PSScriptRoot
$carryFolder=Join-Path $carryWorkspace 'outputs/jahez-v2-treasury-carryforward-20261019'
$carryRosterFolder=Join-Path $carryWorkspace 'outputs/jahez-account-sheet-20261017T115636184Z-applied'
$carryRoster=Get-Content (Join-Path $carryRosterFolder 'plan.json') -Raw|ConvertFrom-Json
$carryConfig=Get-Content (Join-Path $carryWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$carryConnection=[System.Data.SqlClient.SqlConnection]::new($(if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$carryConfig.ConnectionStrings.LogisticsDatabase}))
$carryTransaction=$null
function Read-CarryData([string]$Sql){
 $command=$carryConnection.CreateCommand();$command.Transaction=$carryTransaction;$command.CommandTimeout=90;$command.CommandText=$Sql
 [void]$command.Parameters.Add('@Correlation',[System.Data.SqlDbType]::NVarChar,200);$command.Parameters['@Correlation'].Value=$carryRoster.correlationId
 $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command)
 try{$data=[System.Data.DataSet]::new();[void]$adapter.Fill($data);return ,$data}finally{$adapter.Dispose();$command.Dispose()}
}
try{
 $carryConnection.Open()
 if($carryConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $carryConnection.Database -ne 'db67927'){throw 'Unexpected hosted target.'}
 $carryTransaction=$carryConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 [void](Read-CarryData 'SET LOCK_TIMEOUT 15000;')
 $source=Read-CarryData @'
SELECT a.Id,a.ExternalAccountId,a.SponsorId,s.RegistryNameAr AS SponsorName,s.EmployerIdentityNumber,a.CreatedAtUtc
FROM app.PlatformRiderAccounts a JOIN app.Sponsors s ON s.Id=a.SponsorId
WHERE EXISTS(SELECT 1 FROM jahez_v2.AccountSessions h WHERE h.PlatformRiderAccountId=a.Id AND (EXISTS(SELECT 1 FROM jahez_v2.AccountFees f WHERE f.HandoverId=h.Id) OR EXISTS(SELECT 1 FROM jahez_v2.Operations o WHERE o.HandoverId=h.Id) OR EXISTS(SELECT 1 FROM jahez_v2.MoneyMovements m WHERE m.HandoverId=h.Id)));
SELECT h.Id,h.PlatformRiderAccountId,h.RiderProfileId,e.FullNameAr AS RiderName,h.CreatedAtUtc,h.StartedAtUtc FROM jahez_v2.AccountSessions h JOIN app.RiderProfiles r ON r.Id=h.RiderProfileId JOIN app.Employees e ON e.Id=r.EmployeeId;
SELECT * FROM jahez_v2.AccountFees;
SELECT o.Id,o.HandoverId,o.Status,o.ExpectedSponsorId,o.CreatedAtUtc,o.ExecutedAtUtc,o.EffectiveAtUtc,o.Fund,o.Purpose,o.PaymentStatus,o.Net,o.Remaining,o.PaymentReference FROM jahez_v2.Operations o;
SELECT * FROM jahez_v2.MoneyMovements;
SELECT * FROM jahez_v2.TransferredDebts;
SELECT au.Id,au.Sequence,au.EntityId,au.OccurredAtUtc,au.Action,au.BeforeJson,au.AfterJson,au.Reason FROM audit.AuditEntries au
WHERE au.EntityType=N'PlatformRiderAccount' AND EXISTS(SELECT 1 FROM jahez_v2.AccountSessions h WHERE h.PlatformRiderAccountId=au.EntityId)
ORDER BY au.OccurredAtUtc,au.Sequence;
SELECT au.Id,au.Sequence,au.EntityType,au.EntityId,au.OccurredAtUtc,au.Action,au.BeforeJson,au.AfterJson,au.Reason FROM audit.AuditEntries au
WHERE (au.EntityType=N'JahezAccountFee' AND EXISTS(SELECT 1 FROM jahez_v2.AccountFees f WHERE f.Id=au.EntityId))
 OR (au.EntityType=N'JahezOperation' AND EXISTS(SELECT 1 FROM jahez_v2.Operations o WHERE o.Id=au.EntityId))
 OR (au.EntityType=N'JahezCashboxHandover' AND EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers h WHERE h.Id=au.EntityId))
ORDER BY au.OccurredAtUtc,au.Sequence;
SELECT * FROM jahez_v2.CashboxHandovers; SELECT * FROM jahez_v2.CashboxHandoverMovements;
SELECT Id,RegistryNameAr,EmployerIdentityNumber FROM app.Sponsors;
'@
 $metadata=Read-CarryData @'
SELECT Id,Sequence,OccurredAtUtc,Action,BeforeJson,AfterJson,CorrelationId FROM audit.AuditEntries WHERE CorrelationId=@Correlation AND Action=N'JahezAccountRosterImport';
SELECT Id,DisplayNameAr,DisplayNameEn,UserName FROM [identity].[Users] WHERE Id IN(SELECT ActorUserId FROM jahez_v2.MoneyMovements UNION SELECT RequestedByUserId FROM jahez_v2.CashboxHandovers UNION SELECT AccountantUserId FROM jahez_v2.CashboxHandovers);
SELECT * FROM jahez_v3.TreasurySubAccounts;
SELECT * FROM jahez_v3.Ownership;
SELECT * FROM jahez_v3.Postings;
SELECT * FROM jahez_v3.HandoverLines;
SELECT * FROM jahez_v3.HandoverPostings;
SELECT IsActive FROM jahez_v3.State;
'@
 if($metadata.Tables[7].Rows[0].IsActive){throw 'Historical carryforward is only allowed before activation.'}
 $guards=@()
 $guardTables=@('jahez_v2.AccountFees','jahez_v2.AccountSessions','jahez_v2.Operations','jahez_v2.MoneyMovements','jahez_v2.TransferredDebts','jahez_v2.CashboxHandovers','jahez_v2.CashboxHandoverMovements','app.PlatformRiderAccounts','app.Sponsors','jahez_v3.Ownership','jahez_v3.Postings','jahez_v3.TreasurySubAccounts','jahez_v3.HandoverLines','jahez_v3.HandoverPostings')
 foreach($table in $guardTables){
  $parts=$table.Split('.');$qualified='['+$parts[0]+'].['+$parts[1]+']'
  $sql="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT * FROM $qualified ORDER BY Id FOR JSON PATH)),2) AS SnapshotHash;"
  $result=Read-CarryData $sql
  $guards+=@{Table=$table;Sql=$sql;Sha256=[string]$result.Tables[0].Rows[0].SnapshotHash}
 }
 $auditSql="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT * FROM audit.AuditEntries WHERE EntityType=N'PlatformRiderAccount' OR Action=N'JahezAccountRosterImport' ORDER BY Sequence FOR JSON PATH)),2) AS SnapshotHash;"
 $auditGuard=Read-CarryData $auditSql
 $guards+=@{Table='sponsor-audit-history';Sql=$auditSql;Sha256=[string]$auditGuard.Tables[0].Rows[0].SnapshotHash}
 $carryTransaction.Commit();$carryTransaction.Dispose();$carryTransaction=$null
 [void](New-Item -ItemType Directory -Path $carryFolder -Force)
 $source.WriteXml((Join-Path $carryFolder 'source-evidence.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $metadata.WriteXml((Join-Path $carryFolder 'metadata-evidence.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $guards|ConvertTo-Json -Depth 5|Set-Content (Join-Path $carryFolder 'source-guards.json') -Encoding utf8
 & $PythonPath (Join-Path $PSScriptRoot 'Build-JahezTreasuryCarryforwardPlan.py') $carryFolder $carryRosterFolder
 if($LASTEXITCODE -ne 0){throw 'Evidence review did not produce a complete plan.'}
 Write-Output 'Read-only ownership and carryforward plan generated; no financial records changed.'
}catch{if($carryTransaction){try{$carryTransaction.Rollback()}catch{}};throw}finally{if($carryTransaction){$carryTransaction.Dispose()};$carryConnection.Dispose()}
