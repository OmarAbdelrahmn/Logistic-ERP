param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskPlan=Get-Content (Join-Path $taskRoot 'tmp/requested-oil-deletions-plan.json') -Raw
$taskCorrelation='oil-delete-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-review'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
$taskPlan|Set-Content (Join-Path $taskOutput 'plan.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskRoot 'tmp/delete-requested-oil-changes.sql') -Destination (Join-Path $taskOutput 'executed.sql')
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase);$taskTransaction=$null
function Read-TaskData([string]$Sql){
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.Transaction=$taskTransaction;$taskCommand.CommandTimeout=90;$taskCommand.CommandText=$Sql
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1);$taskCommand.Parameters['@Plan'].Value=$taskPlan
 [void]$taskCommand.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 try{$taskData=[System.Data.DataSet]::new();$taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
 try{[void]$taskAdapter.Fill($taskData)}finally{$taskAdapter.Dispose()};return ,$taskData}finally{$taskCommand.Dispose()}
}
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTransaction=$taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskBackup=Read-TaskData @'
SET XACT_ABORT ON;SET LOCK_TIMEOUT 15000;
DECLARE @Vehicles TABLE(Id uniqueidentifier PRIMARY KEY);INSERT @Vehicles SELECT DISTINCT VehicleId FROM OPENJSON(@Plan) WITH(VehicleId uniqueidentifier);
DECLARE @Usages TABLE(Id uniqueidentifier PRIMARY KEY);INSERT @Usages SELECT OilMaterialUsageId FROM OPENJSON(@Plan) WITH(OilMaterialUsageId uniqueidentifier);
SELECT * FROM maintenance.OilChangeOperations WITH(UPDLOCK,HOLDLOCK) WHERE VehicleId IN(SELECT Id FROM @Vehicles);
SELECT * FROM app.Vehicles WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT Id FROM @Vehicles);
SELECT * FROM maintenance.MaterialUsages WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT Id FROM @Usages);
SELECT * FROM maintenance.StockCostAllocations WITH(UPDLOCK,HOLDLOCK) WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM @Usages);
SELECT * FROM maintenance.OilBarrelUsageAllocations WITH(UPDLOCK,HOLDLOCK) WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM @Usages);
SELECT * FROM maintenance.StockCostLayers WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT StockCostLayerId FROM maintenance.StockCostAllocations WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM @Usages));
SELECT * FROM maintenance.OilBarrels WITH(UPDLOCK,HOLDLOCK) WHERE Id IN(SELECT OilBarrelId FROM maintenance.OilBarrelUsageAllocations WHERE MaintenanceMaterialUsageId IN(SELECT Id FROM @Usages));
SELECT b.* FROM maintenance.StockBalances b WITH(UPDLOCK,HOLDLOCK) WHERE EXISTS(SELECT 1 FROM maintenance.MaterialUsages u WHERE u.Id IN(SELECT Id FROM @Usages) AND u.InventoryItemId=b.InventoryItemId AND u.InventoryLocationId=b.InventoryLocationId);
SELECT * FROM maintenance.VehicleExpenses WITH(UPDLOCK,HOLDLOCK) WHERE SourceEntityType='MaintenanceMaterialUsage' AND SourceEntityId IN(SELECT Id FROM @Usages);
SELECT * FROM app.VehicleOdometerReadings WITH(UPDLOCK,HOLDLOCK) WHERE SourceEntityId IN(SELECT Id FROM OPENJSON(@Plan) WITH(Id uniqueidentifier));
SELECT * FROM maintenance.VehicleSchedules WITH(UPDLOCK,HOLDLOCK) WHERE VehicleId IN(SELECT Id FROM @Vehicles);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('maintenance','fleet');
'@
 $taskNames=@('OilChanges','Vehicles','MaterialUsages','CostAllocations','BarrelAllocations','CostLayers','Barrels','StockBalances','Expenses','OdometerReadings','OilSchedules','DatasetVersions')
 for($taskI=0;$taskI -lt $taskNames.Count;$taskI++){$taskBackup.Tables[$taskI].TableName=$taskNames[$taskI]}
 $taskBackup.WriteXml((Join-Path $taskOutput 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 $taskResult=Read-TaskData (Get-Content (Join-Path $taskRoot 'tmp/delete-requested-oil-changes.sql') -Raw)
 $taskResult.WriteXml((Join-Path $taskOutput 'transaction-result.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if($Apply){
  $taskTransaction.Commit();$taskTransaction.Dispose();$taskTransaction=$null
  $taskConnection.Close();$taskConnection.Open()
  $taskVerified=Read-TaskData @'
SELECT COUNT(*) RemainingDeletedOperations FROM maintenance.OilChangeOperations WHERE Id IN(SELECT Id FROM OPENJSON(@Plan) WITH(Id uniqueidentifier));
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation;
SELECT COUNT(*) ReversalCount,SUM(Quantity) RestoredLiters FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IN(SELECT OilMaterialUsageId FROM OPENJSON(@Plan) WITH(OilMaterialUsageId uniqueidentifier));
SELECT v.PlateNumberAr,s.LastCompletedAtUtc,s.LastCompletedOdometer,s.ComputedStatus FROM app.Vehicles v JOIN maintenance.VehicleSchedules s ON s.VehicleId=v.Id JOIN maintenance.Plans p ON p.Id=s.MaintenancePlanId WHERE p.VehicleType=1 AND p.TriggerType=3 AND s.IsDeleted=0 AND v.Id IN(SELECT VehicleId FROM OPENJSON(@Plan) WITH(VehicleId uniqueidentifier));
'@
  $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskVerified.Tables[0].Rows[0].RemainingDeletedOperations -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 10 -or $taskVerified.Tables[2].Rows[0].ReversalCount -ne 10 -or $taskVerified.Tables[2].Rows[0].RestoredLiters -ne 10){throw 'Post-commit verification failed. Do not replay.'}
  Write-Output 'COMMITTED AND VERIFIED ON A FRESH CONNECTION'
 }else{$taskTransaction.Rollback();$taskTransaction.Dispose();$taskTransaction=$null;Write-Output 'DRY RUN VERIFIED AND ROLLED BACK'}
 $taskRows=@(foreach($taskRow in $taskResult.Tables[0].Rows){[pscustomobject]@{Plate=$taskRow.PlateNumberAr;Deleted=$taskRow.DeletedOilChanges;LatestRemainingOilDate=$taskRow.LatestRemainingOilDate;LatestRemainingOilOdometer=$taskRow.LatestRemainingOilOdometer}})
 $taskRows|ConvertTo-Json -Depth 3|Set-Content (Join-Path $taskOutput 'report.json') -Encoding utf8
 $taskRows|Format-Table -AutoSize
 Write-Output ('Evidence: '+$taskOutput)
}catch{if($taskTransaction){$taskTransaction.Rollback()};throw}finally{if($taskTransaction){$taskTransaction.Dispose()};$taskConnection.Dispose()}
