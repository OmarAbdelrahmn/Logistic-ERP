param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$cfg=Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$cs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$cfg.ConnectionStrings.LogisticsDatabase}
$connection=[System.Data.SqlClient.SqlConnection]::new($cs)
$transaction=$null
$resetDate=[DateTime]::UtcNow.AddHours(3).Date
if($resetDate.ToString('yyyy-MM-dd') -ne '2026-10-17'){throw 'This one-off reset is restricted to 17 October 2026.'}
function Read-Data([string]$sql){
 $command=$connection.CreateCommand();$command.Transaction=$transaction;$command.CommandTimeout=180;$command.CommandText=$sql
 if($sql.Contains('@ResetDate')){[void]$command.Parameters.Add('@ResetDate',[System.Data.SqlDbType]::Date);$command.Parameters['@ResetDate'].Value=$resetDate}
 try{$data=[System.Data.DataSet]::new();$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($command)
  try{[void]$adapter.Fill($data)}finally{$adapter.Dispose()};return ,$data
 }finally{$command.Dispose()}
}
try{
 $connection.Open()
 if($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected hosted target.'}
 $out=Join-Path $workspace ('outputs/jahez-money-history-reset-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')+$(if($Apply){'-applied'}else{'-review'}))
 [void](New-Item -ItemType Directory -Path $out)
 $sqlPath=Join-Path $workspace 'database/scripts/jahez-money-history-reset-20261017.sql'
 Copy-Item -LiteralPath $sqlPath -Destination (Join-Path $out 'executed.sql')
 $transaction=$connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 [void](Read-Data 'SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;')
 $inventory=Read-Data "SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez_v2') ORDER BY name;"
 $expected=@('AccountFees','AccountingEntries','AccountSessions','CommandReceipts','CommissionPolicies','DailyOrders','EarningsStatements','ImportBatches','ImportFiles','ImportRows','MoneyMovements','Operations','PlatformTransactions','RequestDecisions','Requests','SettlementReminders','TransferredDebts')
 if((Compare-Object $expected @($inventory.Tables[0].Rows|ForEach-Object{[string]$_.name}))){throw 'Unexpected Jahez table inventory.'}
 # Refuse unexpected external dependencies before any write.
 $external=Read-Data "SELECT f.name FROM sys.foreign_keys f WHERE OBJECT_SCHEMA_NAME(f.referenced_object_id)='jahez_v2' AND OBJECT_SCHEMA_NAME(f.parent_object_id)<>'jahez_v2';"
 if($external.Tables[0].Rows.Count){throw 'Unexpected external references to Jahez history.'}
 $before=[ordered]@{}
 foreach($row in $inventory.Tables[0].Rows){
  $name=[string]$row.name
  $backup=Read-Data "SELECT * FROM jahez_v2.[$name] WITH(UPDLOCK,HOLDLOCK);"
  $before[$name]=$backup.Tables[0].Rows.Count
  $backup.WriteXml((Join-Path $out ('jahez_v2.'+$name+'-before.xml')),[System.Data.XmlWriteMode]::WriteSchema)
 }
 $notifications=Read-Data "SELECT * FROM app.Notifications WITH(UPDLOCK,HOLDLOCK) WHERE EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%';"
 $notifications.WriteXml((Join-Path $out 'jahez-notifications-before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $checks=[System.Collections.Generic.List[string]]::new()
 $protected=@('app.PlatformRiderAccounts','app.RiderClientAssignments','app.RiderAssignmentEvents','app.PlatformAccountCredentialVersions','app.PlatformAccountRegistrations','app.Employees','app.RiderProfiles','app.VehiclePlatformAccountAssignments','app.RiderVehicleAssignments','app.Vehicles','platform.ClientPlatforms')
 $otherTables=Read-Data "SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) QualifiedName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name LIKE 'chefz%' OR s.name LIKE 'ninja%';"
 $protected+=@($otherTables.Tables[0].Rows|ForEach-Object{[string]$_.QualifiedName})
 $i=0
 foreach($qualified in $protected){
  $metadata=Read-Data "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(N'$qualified') ORDER BY column_id;"
  if($metadata.Tables[0].Rows.Count -eq 0){throw "Protected table missing: $qualified"}
  $cols=($metadata.Tables[0].Rows|ForEach-Object{'['+$_.name.Replace(']',']]')+']'}) -join ','
  [void](Read-Data "SELECT $cols INTO #Keep$i FROM $qualified WITH(HOLDLOCK);")
  $checks.Add("IF EXISTS(SELECT $cols FROM #Keep$i EXCEPT SELECT $cols FROM $qualified) OR EXISTS(SELECT $cols FROM $qualified EXCEPT SELECT $cols FROM #Keep$i) THROW 51000,'Protected table changed: $qualified',1;")
  $i++
 }
 [void](Read-Data "SELECT * INTO #KeepSessions FROM jahez_v2.AccountSessions;SELECT * INTO #KeepNotifications FROM app.Notifications WHERE NOT(EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%');")
 [void](Read-Data (Get-Content -LiteralPath $sqlPath -Raw))
 [void](Read-Data ($checks -join [Environment]::NewLine))
 $sessionCols='Id,PlatformRiderAccountId,RiderProfileId,RiderClientAssignmentId,StartedAtUtc,EndedAtUtc,Reason,CreatedAtUtc,CreatedByUserId,UpdatedByUserId,IsDeleted,DeletedAtUtc,DeletedByUserId,DeletionReason'
 [void](Read-Data "IF EXISTS(SELECT $sessionCols FROM #KeepSessions EXCEPT SELECT $sessionCols FROM jahez_v2.AccountSessions) OR EXISTS(SELECT $sessionCols FROM jahez_v2.AccountSessions EXCEPT SELECT $sessionCols FROM #KeepSessions) THROW 51000,'Session identities or assignments changed.',1;
 IF EXISTS(SELECT * FROM #KeepNotifications EXCEPT SELECT * FROM app.Notifications WHERE NOT(EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%')) OR EXISTS(SELECT * FROM app.Notifications WHERE NOT(EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%') EXCEPT SELECT * FROM #KeepNotifications) THROW 51000,'Other notifications changed.',1;
 IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions WHERE CommissionStartsOn<=@ResetDate OR CommissionPostedThrough IS NOT NULL OR LastSettlementPaymentAtUtc IS NOT NULL) THROW 51000,'Computed commission was not reset.',1;
 IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE (OBJECT_SCHEMA_NAME(parent_object_id)='jahez_v2') AND (is_disabled=1 OR is_not_trusted=1)) THROW 51000,'Jahez foreign keys are untrusted.',1;")
 $countSql=($expected|Where-Object{$_ -ne 'AccountSessions'}|ForEach-Object{"SELECT N'$_' TableName,COUNT_BIG(*) RecordCount FROM jahez_v2.[$_]"}) -join ' UNION ALL '
 $verification=Read-Data $countSql
 if(@($verification.Tables[0].Rows|Where-Object RecordCount -ne 0).Count){throw 'Financial/history rows remain.'}
 $verification.WriteXml((Join-Path $out 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $transaction.Commit();$transaction.Dispose();$transaction=$null
  $connection.Close();$connection.Open()
  $verification=Read-Data $countSql
  $verification.WriteXml((Join-Path $out 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if(@($verification.Tables[0].Rows|Where-Object RecordCount -ne 0).Count){throw 'Post-commit verification failed. Do not replay.'}
 }else{$transaction.Rollback();$transaction.Dispose();$transaction=$null}
 $report=[ordered]@{Applied=[bool]$Apply;VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');ResetThroughRiyadhDate=$resetDate.ToString('yyyy-MM-dd');CommissionResumesOn=$resetDate.AddDays(1).ToString('yyyy-MM-dd');Before=$before;EmptyFinancialHistoryTables=16;PreservedSessions=$before.AccountSessions;ProtectedTablesVerified=$protected.Count;OtherPlatformDataPreserved=$true;BackupDirectory=$out;SqlSha256=(Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash}
 $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $out 'report.json') -Encoding utf8
 $report|ConvertTo-Json -Depth 6
}catch{if($transaction){try{$transaction.Rollback()}catch{}};throw}finally{if($transaction){$transaction.Dispose()};$connection.Dispose()}
