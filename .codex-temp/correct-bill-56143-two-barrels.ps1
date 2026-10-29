param([switch]$Apply)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskCorrelation='bill-56143-two-barrels-'+[guid]::NewGuid().ToString('N')
$taskOutput=Join-Path $taskRoot ('outputs/'+$taskCorrelation+$(if($Apply){'-applied'}else{'-validation'}))
[void](New-Item -ItemType Directory -Path $taskOutput)
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'correct-bill-56143-two-barrels.sql') -Destination (Join-Path $taskOutput 'executed.sql')
function New-TaskV7 {
 $taskHex=[DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds().ToString('x12')
 $taskRandom=[guid]::NewGuid().ToString('N')
 [guid]::Parse(('{0}-{1}-7{2}-a{3}-{4}' -f $taskHex.Substring(0,8),$taskHex.Substring(8,4),$taskRandom.Substring(0,3),$taskRandom.Substring(3,3),$taskRandom.Substring(6,12)))
}
$taskConn=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
$taskTx=$null
$taskCommitted=$false
try {
 $taskConn.Open()
 if($taskConn.Database -ne 'db67927' -or $taskConn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $taskTx=$taskConn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $taskCmd=$taskConn.CreateCommand()
 $taskCmd.Transaction=$taskTx
 $taskCmd.CommandTimeout=45
 $taskCmd.CommandText=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'correct-bill-56143-two-barrels.sql') -Raw
 foreach($taskName in @('BarrelId','AuditId','EventId')){[void]$taskCmd.Parameters.Add('@'+$taskName,[System.Data.SqlDbType]::UniqueIdentifier);$taskCmd.Parameters['@'+$taskName].Value=New-TaskV7}
 [void]$taskCmd.Parameters.AddWithValue('@Correlation',$taskCorrelation)
 $taskReader=$taskCmd.ExecuteReader()
 try {
  if(-not $taskReader.Read()){throw 'Missing before snapshot.'}
  $taskReader.GetString(0)|Set-Content -LiteralPath (Join-Path $taskOutput 'before.json') -Encoding utf8
  if(-not $taskReader.NextResult() -or -not $taskReader.Read()){throw 'Missing verified after snapshot.'}
  $taskReader.GetString(0)|Set-Content -LiteralPath (Join-Path $taskOutput 'after.json') -Encoding utf8
  while($taskReader.NextResult()){while($taskReader.Read()){}}
 } finally {$taskReader.Dispose();$taskCmd.Dispose()}
 if($Apply){$taskTx.Commit();$taskCommitted=$true; 'COMMITTED'|Set-Content -LiteralPath (Join-Path $taskOutput 'status.txt')}
 else{$taskTx.Rollback();'VALIDATED AND ROLLED BACK'|Set-Content -LiteralPath (Join-Path $taskOutput 'status.txt')}
 $taskTx.Dispose();$taskTx=$null
 if($Apply){
  $taskConn.Close();$taskConn.Open()
  $taskVerify=$taskConn.CreateCommand()
  $taskVerify.CommandText=@'
SET NOCOUNT ON;
SELECT p.ReceiptNumber,p.Subtotal,p.TaxAmount,p.TotalAmount,l.PackageCount,l.ReceivedBaseQuantity,l.PackageUnitPrice,c.OriginalQuantity,c.RemainingQuantity,m.Quantity AS MovementQuantity,b.QuantityOnHand,
 (SELECT COUNT(*) FROM maintenance.OilBarrels o WHERE o.PurchaseReceiptLineId=l.Id AND o.IsDeleted=0) AS BarrelCount,
 (SELECT SUM(o.RemainingLiters) FROM maintenance.OilBarrels o WHERE o.PurchaseReceiptLineId=l.Id AND o.IsDeleted=0) AS BarrelLiters,
 (SELECT COUNT(*) FROM audit.AuditEntries WHERE CorrelationId=@Correlation) AS AuditCount
FROM maintenance.PurchaseReceipts p JOIN maintenance.PurchaseReceiptLines l ON l.PurchaseReceiptId=p.Id JOIN maintenance.StockCostLayers c ON c.Id=l.StockCostLayerId JOIN maintenance.StockMovementLines m ON m.Id=l.StockMovementLineId JOIN maintenance.StockBalances b ON b.InventoryItemId=l.InventoryItemId AND b.InventoryLocationId=p.InventoryLocationId AND b.IsDeleted=0 WHERE p.Id='01a17896-4ec9-731b-aadb-ef96b1809e6d';
'@
  [void]$taskVerify.Parameters.AddWithValue('@Correlation',$taskCorrelation)
  $taskDs=[System.Data.DataSet]::new();[void]([System.Data.SqlClient.SqlDataAdapter]::new($taskVerify)).Fill($taskDs)
  $taskDs.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
  if($taskDs.Tables[0].Rows.Count -ne 1){throw 'Missing post-commit receipt; do not replay.'}
  $taskRow=$taskDs.Tables[0].Rows[0]
  if($taskRow.PackageCount -ne 2 -or $taskRow.ReceivedBaseQuantity -ne 416 -or $taskRow.TotalAmount -ne [decimal]8800.04 -or $taskRow.BarrelCount -ne 2 -or $taskRow.BarrelLiters -ne 416 -or $taskRow.QuantityOnHand -ne 418 -or $taskRow.AuditCount -ne 1){throw 'Post-commit verification failed; do not replay.'}
  [pscustomobject]@{Status='COMMITTED AND VERIFIED';Quantity=$taskRow.PackageCount;Liters=$taskRow.ReceivedBaseQuantity;Total=$taskRow.TotalAmount;Barrels=$taskRow.BarrelCount;StockLiters=$taskRow.QuantityOnHand;Evidence=$taskOutput}|ConvertTo-Json
 }else{[pscustomobject]@{Status='VALIDATED AND ROLLED BACK';Evidence=$taskOutput}|ConvertTo-Json}
}catch{if($taskTx -and -not $taskCommitted){$taskTx.Rollback()};throw}
finally{if($taskTx){$taskTx.Dispose()};$taskConn.Dispose()}
