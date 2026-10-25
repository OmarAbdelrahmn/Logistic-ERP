$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfig = Get-Content (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskConfig.ConnectionStrings.LogisticsDatabase)
try {
    $taskConnection.Open()
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database target.' }
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.CommandTimeout = 45
    $taskCommand.CommandText = @'
SET NOCOUNT ON; SET LOCK_TIMEOUT 15000;
SELECT p.ExternalAccountId Account,c.Code Platform,p.Id AccountId,p.IsDeleted AccountDeleted,
 s.Id SessionId,s.RiderClientAssignmentId AssignmentId,s.StartedAtUtc,s.EndedAtUtc,s.IsDeleted SessionDeleted,
 s.CommissionStartsOn,s.CommissionPostedThrough,s.LastSettlementPaymentAtUtc,CONVERT(varchar(18),s.RowVersion,1) RowVersion
FROM app.PlatformRiderAccounts p JOIN platform.ClientPlatforms c ON c.Id=p.ClientPlatformId
LEFT JOIN jahez_v2.AccountSessions s ON s.PlatformRiderAccountId=p.Id
WHERE p.ExternalAccountId IN('493593','483905','474572','408132','405092') ORDER BY p.ExternalAccountId,s.StartedAtUtc;
SELECT p.ExternalAccountId Account,o.Id,o.HandoverId,o.Status,o.Mode,o.ThroughDate,o.CommissionStartDate,o.Commission,o.ExecutedAtUtc,o.IsDeleted
FROM jahez_v2.Operations o JOIN jahez_v2.AccountSessions s ON s.Id=o.HandoverId JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId
WHERE p.ExternalAccountId IN('493593','483905','474572','408132','405092') ORDER BY p.ExternalAccountId,o.ThroughDate;
SELECT p.ExternalAccountId Account,l.HandoverId,l.Bucket,l.Kind,l.Amount,l.FromDate,l.ThroughDate
FROM jahez_v2.AccountingEntries l JOIN jahez_v2.AccountSessions s ON s.Id=l.HandoverId JOIN app.PlatformRiderAccounts p ON p.Id=s.PlatformRiderAccountId
WHERE p.ExternalAccountId IN('493593','483905','474572','408132','405092') ORDER BY p.ExternalAccountId,l.ThroughDate;
SELECT name,definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('jahez_v2.AccountSessions');
'@
    $taskData = [System.Data.DataSet]::new()
    $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
    [void]$taskAdapter.Fill($taskData)
    for ($taskIndex=0; $taskIndex -lt $taskData.Tables.Count; $taskIndex++) {
        Write-Output ('Result '+$taskIndex)
        if ($taskIndex -eq 0) {
            @($taskData.Tables[$taskIndex].Rows | ForEach-Object { [pscustomobject]@{ Account=$_.Account; SessionId=[string]$_.SessionId; RowVersion=$_.RowVersion; CommissionStartsOn=$_.CommissionStartsOn.ToString('yyyy-MM-dd'); CommissionPostedThrough=$(if($_.IsNull('CommissionPostedThrough')){$null}else{$_.CommissionPostedThrough.ToString('yyyy-MM-dd')}); LastSettlementPaymentAtUtc=$(if($_.IsNull('LastSettlementPaymentAtUtc')){$null}else{$_.LastSettlementPaymentAtUtc.ToString('o')}) } }) | ConvertTo-Json
        } else { $taskData.Tables[$taskIndex] | Format-Table -AutoSize | Out-String -Width 320 | Write-Output }
    }
} finally { $taskConnection.Dispose() }
