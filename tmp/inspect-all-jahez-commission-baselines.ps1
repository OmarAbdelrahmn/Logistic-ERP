$ErrorActionPreference='Stop'
$taskRoot=Split-Path -Parent $PSScriptRoot
$taskConfig=Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target'}
 $taskCommand=$taskConnection.CreateCommand()
 $taskCommand.CommandText=@'
SELECT p.Id AccountId,p.ExternalAccountId Account,p.Status AccountStatus,s.Id SessionId,s.RiderClientAssignmentId AssignmentId,
 s.StartedAtUtc,s.EndedAtUtc,s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc,s.RowVersion,
 (SELECT COUNT(*) FROM jahez_v2.Operations o WHERE o.HandoverId=s.Id AND o.ExecutedAtUtc IS NOT NULL) ExecutedOperations,
 (SELECT COUNT(*) FROM jahez_v2.Operations o WHERE o.HandoverId=s.Id AND o.ExecutedAtUtc IS NULL) PendingOperations,
 (SELECT COUNT(*) FROM jahez_v2.AccountingEntries l WHERE l.HandoverId=s.Id AND l.Bucket=3) CommissionEntries
FROM app.PlatformRiderAccounts p JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
LEFT JOIN jahez_v2.AccountSessions s ON s.PlatformRiderAccountId=p.Id AND s.IsDeleted=0
WHERE c.Code='JAHEZ' AND p.IsDeleted=0 ORDER BY p.ExternalAccountId,s.StartedAtUtc;
'@
 $taskData=[System.Data.DataSet]::new(); $taskAdapter=[System.Data.SqlClient.SqlDataAdapter]::new($taskCommand); [void]$taskAdapter.Fill($taskData)
 $taskBackup=[xml](Get-Content (Join-Path $taskRoot 'outputs/jahez-money-history-reset-20261017T124303649Z-applied/jahez_v2.AccountSessions-before.xml') -Raw)
 $taskOriginal=@{}; foreach($taskRow in $taskBackup.NewDataSet.Table){$taskOriginal[[string]$taskRow.Id]=$taskRow}
 $taskRows=@(foreach($taskRow in $taskData.Tables[0].Rows){
  $taskOld=$taskOriginal[$taskRow.SessionId.ToString()]
  $taskHasSession=$taskRow.SessionId -isnot [DBNull]
  $taskRecorded=$taskRow.LastSettlementPaymentAtUtc -isnot [DBNull]
  $taskCanRestore=$taskHasSession -and -not $taskRecorded -and $taskOld -and $taskOld.LastSettlementPaymentAtUtc
  $taskCategory=if(-not $taskHasSession){'NoSession'}elseif($taskRecorded){'RecordedSettlement'}elseif($taskCanRestore){'RecoverableSettlement'}else{'NoRecordedSettlement'}
  [pscustomobject]@{Account=$taskRow.Account;AccountId=$taskRow.AccountId.ToString();AccountStatus=[int]$taskRow.AccountStatus;SessionId=$taskRow.SessionId.ToString();AssignmentId=$taskRow.AssignmentId.ToString();StartedAtUtc=if($taskHasSession){$taskRow.StartedAtUtc.ToString('o')}else{''};EndedAtUtc=if($taskRow.EndedAtUtc -isnot [DBNull]){$taskRow.EndedAtUtc.ToString('o')}else{''};CommissionStartsOn=if($taskHasSession){$taskRow.CommissionStartsOn.ToString('yyyy-MM-dd')}else{''};CommissionPostedThrough=if($taskRow.CommissionPostedThrough -isnot [DBNull]){$taskRow.CommissionPostedThrough.ToString('yyyy-MM-dd')}else{''};LastSettlementPaymentAtUtc=if($taskRecorded){$taskRow.LastSettlementPaymentAtUtc.ToString('o')}else{''};RowVersion=if($taskHasSession){[Convert]::ToBase64String($taskRow.RowVersion)}else{''};OriginalSettlement=if($taskOld){[string]$taskOld.LastSettlementPaymentAtUtc}else{''};OriginalStartedAtUtc=if($taskOld){[string]$taskOld.StartedAtUtc}else{''};Category=$taskCategory;ExecutedOperations=[int]$taskRow.ExecutedOperations;PendingOperations=[int]$taskRow.PendingOperations;CommissionEntries=[int]$taskRow.CommissionEntries}
 })
 $taskRows|ConvertTo-Json -Depth 4|Set-Content (Join-Path $taskRoot 'tmp/jahez-all-commission-inspection.json') -Encoding utf8
 $taskRows|Group-Object Category|Select-Object Name,Count|Format-Table -AutoSize
 $taskRows|Where-Object Category -eq 'NoRecordedSettlement'|Select-Object Account,StartedAtUtc,EndedAtUtc,CommissionStartsOn,ExecutedOperations,CommissionEntries|Format-Table -AutoSize|Out-String -Width 200|Write-Output
 $taskRows|Where-Object Category -eq 'RecoverableSettlement'|Group-Object ExecutedOperations,PendingOperations,CommissionEntries|Select-Object Name,Count|Format-Table -AutoSize
}finally{$taskConnection.Dispose()}
