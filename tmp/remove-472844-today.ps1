param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskCs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$taskConfig.ConnectionStrings.LogisticsDatabase}
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskCs)
$taskTransaction=$null
$taskCorrelation='jahez-remove-472844-20261022'
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+'/'+$(if($Apply){'applied'}else{'validation'}))
[void](New-Item -ItemType Directory -Path $taskOutput -Force)
try {
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $cmd=$taskConnection.CreateCommand();$cmd.Transaction=$taskTransaction;$cmd.CommandTimeout=60
 $cmd.CommandText=Get-Content (Join-Path $PSScriptRoot 'remove-472844-today.sql') -Raw
 [void]$cmd.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 $ds=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$ad.Fill($ds)
 $ds.WriteXml((Join-Path $taskOutput 'transaction-evidence.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'remove-472844-today.sql') -Destination (Join-Path $taskOutput 'executed.sql')
 if($Apply){$taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'COMMITTED'}
 else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'VALIDATED AND ROLLED BACK'}
 $ds.Tables[1]|Select-Object CashBefore,CashAfter,TotalBefore,TotalAfter,OutstandingBefore,OutstandingAfter|ConvertTo-Json -Compress
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction -and $taskTransaction.Connection){$taskTransaction.Rollback()};throw}
finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
