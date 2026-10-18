param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskPlan=Get-Content (Join-Path $taskRoot 'tmp/jahez-take-dates-plan-20261017.json') -Raw
$taskCorrelation='jahez-take-dates-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-review'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
$taskPlan|Set-Content (Join-Path $taskOutput 'plan.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskRoot 'tmp/update-jahez-take-dates-20261017.sql') -Destination (Join-Path $taskOutput 'executed.sql')
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase);$taskTransaction=$null
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction;$taskCommand.CommandTimeout=60;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1);$taskCommand.Parameters['@Plan'].Value=$taskPlan
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{$taskData=[System.Data.DataSet]::new();$taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 try{[void]$taskAdapter.Fill($taskData)}finally{$taskAdapter.Dispose()};return ,$taskData}finally{$taskCommand.Dispose()}
}
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBefore=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
SELECT * FROM jahez_v2.AccountSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT sessionId FROM OPENJSON(@Plan) WITH(sessionId uniqueidentifier));
SELECT * FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT assignmentId FROM OPENJSON(@Plan) WITH(assignmentId uniqueidentifier));
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('jahez','platform-operations');
'@
 $taskBefore.Tables[0].TableName='Sessions';$taskBefore.Tables[1].TableName='Assignments';$taskBefore.Tables[2].TableName='DatasetVersions'
 $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskResult=Read-TaskData (Get-Content (Join-Path $taskRoot 'tmp/update-jahez-take-dates-20261017.sql') -Raw)
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT COUNT(*) AccountsVerified,SUM(CASE WHEN CONVERT(date,SWITCHOFFSET(s.StartedAtUtc,'+03:00'))=t.takeDate AND a.EffectiveFrom=t.takeDate THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) WITH(takeDate date,sessionId uniqueidentifier,assignmentId uniqueidentifier) t JOIN jahez_v2.AccountSessions s ON s.Id=t.sessionId JOIN app.RiderClientAssignments a ON a.Id=t.assignmentId;
SELECT COUNT(*) AuditEntries FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows[0].AccountsVerified -ne 24 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditEntries -ne $taskResult.Tables[1].Rows[0].AuditEntries){throw 'Post-commit verification failed. Do not replay.'}
  Write-Output 'COMMITTED AND VERIFIED ON A FRESH CONNECTION'
 }else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK'}
 $taskRows=@(foreach($taskRow in $taskResult.Tables[0].Rows){[pscustomobject]@{Account=$taskRow.Account;TakeDate=$taskRow.TakeDate;Result=$taskRow.Result}})
 $taskRows|ConvertTo-Json -Depth 3|Set-Content (Join-Path $taskOutput 'report.json') -Encoding utf8
 $taskRows|Format-Table -AutoSize
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
