param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTransaction=$null
$taskCorrelation='ninja-delete-performance-20261026-'+[Guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation)
[void](New-Item -ItemType Directory -Path $taskOutput)
try {
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskConnection.Open()
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskSql=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'delete-ninja-performance-20261026.sql') -Raw
 $taskParts=$taskSql -split '-- EXECUTE_MUTATION',2
 if($taskParts.Count -ne 2){throw 'Invalid maintenance script.'}
 $taskCommand=$taskConnection.CreateCommand()
 $taskCommand.Transaction=$taskTransaction
 $taskCommand.CommandTimeout=90
 $taskCommand.CommandText=$taskParts[0]
 $taskBackup=[System.Data.DataSet]::new()
 $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 [void]$taskAdapter.Fill($taskBackup)
 $taskBackup.WriteXml((Join-Path $taskOutput 'backup-before-deletion.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'delete-ninja-performance-20261026.sql') -Destination (Join-Path $taskOutput 'executed.sql')
 $taskCommand.CommandText=$taskParts[1]
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 $taskResult=[System.Data.DataSet]::new()
 $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 [void]$taskAdapter.Fill($taskResult)
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){$taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null;$taskMode='COMMITTED'}
 else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;$taskMode='VALIDATED AND ROLLED BACK'}
 $taskSummary=[ordered]@{Mode=$taskMode;Correlation=$taskCorrelation;Evidence=$taskOutput;Deleted=@(foreach($taskRow in $taskResult.Tables[0].Rows){[pscustomobject]@{Table=$taskRow.TableName;Rows=$taskRow.DeletedRows}});RemainingPerformance=$taskResult.Tables[1].Rows[0].RemainingPerformance;RemainingSources=$taskResult.Tables[1].Rows[0].RemainingSources;RemainingImports=$taskResult.Tables[1].Rows[0].RemainingImports}
 $taskJson=ConvertTo-Json -InputObject $taskSummary -Depth 5
 Set-Content -LiteralPath (Join-Path $taskOutput 'summary.json') -Value $taskJson -Encoding utf8
 $taskJson
}catch{if($taskTransaction -and $taskTransaction.Connection){$taskTransaction.Rollback()};throw}
finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
