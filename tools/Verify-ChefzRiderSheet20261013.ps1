param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$planJson=Get-Content (Join-Path $OutputDirectory 'plan.json') -Raw
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open();$q=$conn.CreateCommand();$q.CommandTimeout=90;$q.CommandText=@'
DECLARE @Platform uniqueidentifier=JSON_VALUE(@j,'$.platformId');
SELECT * INTO #s FROM OPENJSON(@j,'$.rows') WITH(SheetRow int '$.row',OwnerIqama varchar(10) '$.ownerIqama',UserIqama varchar(10) '$.userIqama',
 AccountId uniqueidentifier '$.accountId',RiderId uniqueidentifier '$.riderId',AssignmentId uniqueidentifier '$.assignmentId',FromDate date '$.effectiveFrom',Action varchar(20) '$.assignmentAction');
SELECT 'Assignments' CheckName,COUNT(*) Mismatches FROM #s s
 LEFT JOIN app.PlatformRiderAccounts a ON a.ClientPlatformId=@Platform AND a.ExternalAccountId=s.OwnerIqama AND a.IsDeleted=0
 LEFT JOIN app.RiderClientAssignments r ON r.PlatformRiderAccountId=a.Id AND r.EffectiveTo IS NULL AND r.IsDeleted=0
 LEFT JOIN app.RiderProfiles p ON p.Id=r.RiderProfileId AND p.IsDeleted=0
 LEFT JOIN app.Employees e ON e.Id=p.EmployeeId AND e.IsDeleted=0
 WHERE s.Action='Assign' AND (a.Id IS NULL OR r.Id IS NULL OR r.Id<>s.AssignmentId OR r.RiderProfileId<>s.RiderId OR r.EffectiveFrom<>s.FromDate OR e.IqamaNo<>s.UserIqama OR r.Status<>2 OR a.Status<>2)
UNION ALL SELECT 'Pending rows',COUNT(*) FROM #s s JOIN app.RiderClientAssignments r ON r.PlatformRiderAccountId=s.AccountId AND r.EffectiveTo IS NULL AND r.IsDeleted=0 WHERE s.Action='Pending'
UNION ALL SELECT 'IBANs',COUNT(*) FROM OPENJSON(@j,'$.ibanChecks') WITH(EmployeeId uniqueidentifier '$.employeeId',Iqama varchar(10) '$.iqama',Iban varchar(34) '$.iban') i
 LEFT JOIN app.Employees e ON e.IqamaNo=i.Iqama AND e.IsDeleted=0 WHERE e.Id IS NULL OR e.Id<>i.EmployeeId OR REPLACE(UPPER(ISNULL(e.Iban,'')),' ','')<>i.Iban
UNION ALL SELECT 'Removed Jahez assignments',COUNT(*) FROM OPENJSON(@j,'$.removeJahez') WITH(AssignmentId uniqueidentifier '$.assignmentId') x JOIN app.RiderClientAssignments a ON a.Id=x.AssignmentId
UNION ALL SELECT 'Removed Jahez handovers',COUNT(*) FROM OPENJSON(@j,'$.removeJahez') WITH(HandoverId uniqueidentifier '$.handoverId') x JOIN jahez.JahezAccountHandover a ON a.Id=x.HandoverId
UNION ALL SELECT 'Removed Jahez ledger',COUNT(*) FROM OPENJSON(@j,'$.removeJahez') WITH(HandoverId uniqueidentifier '$.handoverId') x JOIN jahez.JahezLedgerEntry a ON a.HandoverId=x.HandoverId
UNION ALL SELECT 'Removed Jahez fees',COUNT(*) FROM OPENJSON(@j,'$.removeJahez') WITH(HandoverId uniqueidentifier '$.handoverId') x JOIN jahez.JahezAccountFee a ON a.HandoverId=x.HandoverId
UNION ALL SELECT 'Closed Jahez assignment',COUNT(*) FROM OPENJSON(@j,'$.moves') WITH(AssignmentId uniqueidentifier '$.assignmentId',HandoverId uniqueidentifier '$.handoverId',EndDate date '$.endDate') x
 LEFT JOIN app.RiderClientAssignments r ON r.Id=x.AssignmentId LEFT JOIN jahez.JahezAccountHandover h ON h.Id=x.HandoverId
 WHERE r.Id IS NULL OR r.Status<>3 OR r.EffectiveTo<>x.EndDate OR h.EndedAtUtc IS NULL OR CONVERT(date,SWITCHOFFSET(h.EndedAtUtc,'+03:00'))<>x.EndDate OR h.DebtStatus<>2;
SELECT COUNT(*) ChefzAssignments FROM app.RiderClientAssignments a JOIN app.PlatformRiderAccounts p ON p.Id=a.PlatformRiderAccountId WHERE p.ClientPlatformId=@Platform AND a.IsDeleted=0 AND a.EffectiveTo IS NULL;
SELECT COUNT(*) AuditEvents FROM audit.AuditEntries WHERE CorrelationId='chefz-rider-sheet-20261013-88db9f8f' AND Action='ChefzRiderSheetImport';
SELECT l.HandoverId,l.Amount,l.FromDate,l.ThroughDate,l.Kind FROM jahez.JahezLedgerEntry l JOIN OPENJSON(@j,'$.moves') WITH(HandoverId uniqueidentifier '$.handoverId') x ON x.HandoverId=l.HandoverId WHERE l.Reason=N'User authorized Jahez closure for Chefz sheet import.';
'@
 [void]$q.Parameters.Add('@j',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@j'].Value=$planJson
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 foreach($t in $d.Tables){$t | Format-Table -AutoSize}
 if(@($d.Tables[0].Rows | Where-Object Mismatches -ne 0).Count){throw 'Post-commit comparison failed.'}
 if($d.Tables[1].Rows[0].ChefzAssignments -ne 217 -or $d.Tables[2].Rows[0].AuditEvents -ne 1){throw 'Post-commit counts failed.'}
 if($d.Tables[3].Rows.Count -ne 1 -or $d.Tables[3].Rows[0].Amount -ne 75){throw 'Jahez closure commission verification failed.'}
 $d.Tables[0].TableName='Checks';$d.Tables[1].TableName='ChefzAssignmentCount';$d.Tables[2].TableName='Audit';$d.Tables[3].TableName='JahezClosureCommission'
 $d.WriteXml((Join-Path $OutputDirectory 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 'POST-COMMIT VERIFICATION PASSED'
}finally{$conn.Dispose()}
