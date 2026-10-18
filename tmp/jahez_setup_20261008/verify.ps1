param([string]$PlanFile='plan.json',[string]$ResultFile='outputs/jahez-setup-20261008/import-result.json')
$ErrorActionPreference='Stop'
$taskPlan=Get-Content (Join-Path 'tmp/jahez_setup_20261008' $PlanFile) -Raw
$taskExpectedCount=@(($taskPlan|ConvertFrom-Json).rows).Count
$taskConfig=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$taskConnection=[System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try{
 $taskConnection.Open()
 if($taskConnection.Database -ne 'db67927'){throw 'Unexpected database'}
 $taskCommand=$taskConnection.CreateCommand();$taskCommand.CommandTimeout=90
 $taskCommand.CommandText=@'
SELECT * INTO #Expected FROM OPENJSON(@Plan,'$.rows') WITH (
 AccountId uniqueidentifier,RiderId uniqueidentifier,AssignmentId uniqueidentifier,HandoverId uniqueidentifier,
 FeeId uniqueidentifier,EmployeeId uniqueidentifier,ExternalId nvarchar(150),Iqama nvarchar(20),
 StartedOn date,LastSettlementOn date,FinancialStartOn date,CommissionThrough date,Slot int,
 OpeningDebt decimal(18,6),OpeningFees decimal(18,6),OpeningCommission decimal(18,6));
SELECT (SELECT COUNT(*) FROM #Expected) AS ExpectedAssignments,
 (SELECT COUNT(*) FROM jahez.JahezAccountHandover h JOIN #Expected s ON s.HandoverId=h.Id WHERE h.IsDeleted=0) AS Handovers,
 (SELECT COUNT(*) FROM app.RiderClientAssignments a JOIN #Expected s ON s.AssignmentId=a.Id WHERE a.IsDeleted=0 AND a.Status=2 AND a.EffectiveTo IS NULL) AS ActiveAssignments,
 (SELECT COUNT(*) FROM jahez.JahezLedgerEntry l JOIN #Expected s ON s.HandoverId=l.HandoverId WHERE l.Kind=5) AS OpeningEntries,
 (SELECT SUM(l.Amount) FROM jahez.JahezLedgerEntry l JOIN #Expected s ON s.HandoverId=l.HandoverId WHERE l.Bucket=2) AS OpeningDebt,
 (SELECT SUM(l.Amount) FROM jahez.JahezLedgerEntry l JOIN #Expected s ON s.HandoverId=l.HandoverId WHERE l.Bucket=1) AS OpeningFees,
 (SELECT SUM(l.Amount) FROM jahez.JahezLedgerEntry l JOIN #Expected s ON s.HandoverId=l.HandoverId WHERE l.Bucket=3) AS OpeningCommission,
 (SELECT COUNT(*) FROM jahez.JahezCashboxEntry c JOIN #Expected s ON s.HandoverId=c.HandoverId) AS CashReceiptsCreated,
 (SELECT COUNT(*) FROM jahez.JahezRiderSettlement p JOIN #Expected s ON s.HandoverId=p.HandoverId) AS PaymentReceiptsCreated,
 (SELECT COUNT(*) FROM #Expected s LEFT JOIN jahez.JahezAccountHandover h ON h.Id=s.HandoverId
  LEFT JOIN app.RiderClientAssignments a ON a.Id=s.AssignmentId LEFT JOIN app.PlatformRiderAccounts p ON p.Id=s.AccountId
  WHERE h.Id IS NULL OR a.Id IS NULL OR p.Status<>2 OR h.RiderProfileId<>s.RiderId OR a.RiderProfileId<>s.RiderId
   OR h.RiderClientAssignmentId<>a.Id OR h.PlatformRiderAccountId<>s.AccountId
   OR CONVERT(date,SWITCHOFFSET(h.StartedAtUtc,'+03:00'))<>s.StartedOn
   OR ISNULL(CONVERT(date,SWITCHOFFSET(h.LastSettlementPaymentAtUtc,'+03:00')),'1900-01-01')<>ISNULL(s.LastSettlementOn,'1900-01-01')
   OR h.CommissionStartsOn<>s.FinancialStartOn OR a.RiderAccountSlot<>s.Slot) AS AssignmentDateMismatches,
 (SELECT COUNT(*) FROM #Expected s CROSS APPLY(VALUES(1,s.OpeningFees),(2,s.OpeningDebt),(3,s.OpeningCommission)) ex(Bucket,Amount)
  OUTER APPLY(SELECT SUM(Amount) AS Amount FROM jahez.JahezLedgerEntry WHERE HandoverId=s.HandoverId AND Bucket=ex.Bucket) actual
  WHERE actual.Amount IS NULL OR actual.Amount<>ex.Amount) AS BalanceMismatches,
 (SELECT COUNT(*) FROM app.RiderClientAssignments a WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL
   AND NOT EXISTS(SELECT 1 FROM #Expected s WHERE s.AssignmentId=a.Id)) AS OtherActiveAssignments,
 (SELECT COUNT(*) FROM (SELECT RiderProfileId FROM app.RiderClientAssignments WHERE IsDeleted=0 AND EffectiveTo IS NULL
   GROUP BY RiderProfileId HAVING COUNT(*)>2) limits) AS RidersOverLimit
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
SELECT a.Id,a.ExternalAccountId,a.RegisteredEmployeeId,a.OperatingCityId,a.SponsorId,a.Status,a.PaymentModel,a.RowVersion
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE p.Code='JAHEZ' AND a.IsDeleted=0 FOR JSON PATH,INCLUDE_NULL_VALUES;
'@
 [void]$taskCommand.Parameters.Add('@Plan',[System.Data.SqlDbType]::NVarChar,-1)
 $taskCommand.Parameters['@Plan'].Value=$taskPlan
 $taskReader=$taskCommand.ExecuteReader();$taskBuffer=[System.Text.StringBuilder]::new()
 while($taskReader.Read()){[void]$taskBuffer.Append($taskReader.GetString(0))}
 $taskSummary=$taskBuffer.ToString()|ConvertFrom-Json
 [void]$taskReader.NextResult();$taskBuffer=[System.Text.StringBuilder]::new()
 while($taskReader.Read()){[void]$taskBuffer.Append($taskReader.GetString(0))};$taskReader.Dispose()
 [System.IO.File]::WriteAllText((Join-Path (Get-Location).Path 'tmp/jahez_setup_20261008/post_accounts.json'),$taskBuffer.ToString(),[System.Text.UTF8Encoding]::new($false))
 if($taskSummary.Handovers -ne $taskExpectedCount -or $taskSummary.ActiveAssignments -ne $taskExpectedCount -or $taskSummary.OpeningEntries -ne ($taskExpectedCount*3) -or
  $taskSummary.CashReceiptsCreated -ne 0 -or $taskSummary.PaymentReceiptsCreated -ne 0 -or
  $taskSummary.AssignmentDateMismatches -ne 0 -or $taskSummary.BalanceMismatches -ne 0 -or $taskSummary.RidersOverLimit -ne 0){throw 'Post-import verification failed'}
 $taskSummary|ConvertTo-Json|Set-Content $ResultFile -Encoding UTF8
 $taskSummary|ConvertTo-Json -Compress|Write-Output
}finally{$taskConnection.Dispose()}
