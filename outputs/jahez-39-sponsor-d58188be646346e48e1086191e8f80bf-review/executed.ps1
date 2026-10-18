param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskIds = @(
    '498895','498846','498898','499583','499584','499552','491297','491274','491300','491298',
    '499117','499551','491301','491296','491576','491258','491304','491575','499120','498844',
    '491299','491311','491310','498897','499308','491295','491309','491302','499118','491306',
    '499122','499550','498848','499572','491577','498845','499116','499580','499581'
)
if ($taskIds.Count -ne 39 -or @($taskIds | Select-Object -Unique).Count -ne 39) { throw 'Expected 39 unique driver IDs.' }
$taskPlan = $taskIds | ConvertTo-Json -Compress
$taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$taskCs = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $taskConfig.ConnectionStrings.LogisticsDatabase }
$taskConnection = [System.Data.SqlClient.SqlConnection]::new($taskCs)
$taskTransaction = $null
$taskCommitted = $false
$taskCorrelation = 'jahez-39-sponsor-' + [guid]::NewGuid().ToString('N')
$taskOutput = Join-Path $taskRoot ('outputs/' + $taskCorrelation + $(if ($Apply) { '-applied' } else { '-review' }))
[void](New-Item -ItemType Directory -Path $taskOutput)
$taskPlan | Set-Content -LiteralPath (Join-Path $taskOutput 'driver-ids.json') -Encoding utf8
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $taskOutput 'executed.ps1')

function Read-TaskData([string]$Sql) {
    $taskCommand = $taskConnection.CreateCommand()
    $taskCommand.Transaction = $taskTransaction
    $taskCommand.CommandTimeout = 60
    $taskCommand.CommandText = $Sql
    [void]$taskCommand.Parameters.Add('@Plan', [System.Data.SqlDbType]::NVarChar, -1)
    $taskCommand.Parameters['@Plan'].Value = $taskPlan
    [void]$taskCommand.Parameters.AddWithValue('@Correlation', $taskCorrelation)
    try {
        $taskData = [System.Data.DataSet]::new()
        $taskAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($taskCommand)
        try { [void]$taskAdapter.Fill($taskData) } finally { $taskAdapter.Dispose() }
        return ,$taskData
    } finally { $taskCommand.Dispose() }
}

try {
    $taskConnection.Open()
    if ($taskConnection.Database -ne 'db67927' -or $taskConnection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database target.' }
    $taskTransaction = $taskConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $taskBefore = Read-TaskData @'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE parent_id=OBJECT_ID('app.PlatformRiderAccounts') AND is_disabled=0)
 THROW 51000,'Review account triggers before applying.',1;
SELECT CONVERT(varchar(100),value) Account INTO #Targets FROM OPENJSON(@Plan);
IF (SELECT COUNT(*) FROM #Targets)<>39 OR (SELECT COUNT(DISTINCT Account) FROM #Targets)<>39
 THROW 51000,'Expected 39 unique account numbers.',1;
IF (SELECT COUNT(*) FROM app.Sponsors WITH(HOLDLOCK) WHERE EmployerIdentityNumber='7038745530' AND IsDeleted=0 AND Status=1)<>1
 THROW 51000,'Target sponsor is missing, ambiguous, or inactive.',1;
SELECT a.* INTO #AccountBefore FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK);
IF EXISTS(SELECT t.Account FROM #Targets t LEFT JOIN #AccountBefore a ON a.ExternalAccountId=t.Account
 GROUP BY t.Account HAVING COUNT(a.Id)<>1)
 THROW 51000,'A driver account is missing or ambiguous.',1;
IF EXISTS(SELECT 1 FROM #Targets t JOIN #AccountBefore a ON a.ExternalAccountId=t.Account
 LEFT JOIN platform.ClientPlatforms p WITH(HOLDLOCK) ON p.Id=a.ClientPlatformId
 LEFT JOIN app.Sponsors s WITH(HOLDLOCK) ON s.Id=a.SponsorId
 WHERE a.IsDeleted=1 OR p.Code IS NULL OR p.Code<>'JAHEZ' OR s.EmployerIdentityNumber IS NULL OR s.EmployerIdentityNumber<>'7015658094')
 THROW 51000,'An account no longer matches the reviewed platform or original sponsor.',1;
SELECT a.* FROM #AccountBefore a JOIN #Targets t ON t.Account=a.ExternalAccountId ORDER BY a.ExternalAccountId;
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('jahez','platform-operations');
SELECT Id,EmployerIdentityNumber,RegistryNameAr FROM app.Sponsors WHERE EmployerIdentityNumber IN('7015658094','7038745530');
'@
    $taskBefore.Tables[0].TableName = 'Accounts'
    $taskBefore.Tables[1].TableName = 'DatasetVersions'
    $taskBefore.Tables[2].TableName = 'Sponsors'
    $taskBefore.WriteXml((Join-Path $taskOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult = Read-TaskData @'
DECLARE @Now datetimeoffset=SYSUTCDATETIME(), @Actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001',
 @Sponsor uniqueidentifier=(SELECT Id FROM app.Sponsors WHERE EmployerIdentityNumber='7038745530' AND IsDeleted=0 AND Status=1),
 @Reason nvarchar(700)=N'User requested changing the sponsor of the supplied 39 Jahez driver accounts from 7015658094 to 7038745530. Account identities, statuses, owners, assignments and financial records are preserved.';
UPDATE a SET SponsorId=@Sponsor,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
FROM app.PlatformRiderAccounts a JOIN #Targets t ON t.Account=a.ExternalAccountId;
IF @@ROWCOUNT<>39 THROW 51000,'Expected exactly 39 updated accounts.',1;

-- Compare every field dynamically. Only sponsor and update metadata may differ on the 39 targets.
DECLARE @AllColumns nvarchar(max),@KeepColumns nvarchar(max),@Check nvarchar(max);
SELECT @AllColumns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP(ORDER BY column_id)
FROM sys.columns WHERE object_id=OBJECT_ID('app.PlatformRiderAccounts');
SELECT @KeepColumns=STRING_AGG(CAST(QUOTENAME(name) AS nvarchar(max)),',') WITHIN GROUP(ORDER BY column_id)
FROM sys.columns WHERE object_id=OBJECT_ID('app.PlatformRiderAccounts') AND name NOT IN('SponsorId','UpdatedAtUtc','UpdatedByUserId','RowVersion');
SET @Check=N'IF EXISTS(SELECT '+@KeepColumns+' FROM #AccountBefore EXCEPT SELECT '+@KeepColumns+' FROM app.PlatformRiderAccounts)
 OR EXISTS(SELECT '+@KeepColumns+' FROM app.PlatformRiderAccounts EXCEPT SELECT '+@KeepColumns+' FROM #AccountBefore)
 THROW 51000,''An unrelated account field changed.'',1;
 IF EXISTS(SELECT '+@AllColumns+' FROM #AccountBefore WHERE ExternalAccountId NOT IN(SELECT Account FROM #Targets)
 EXCEPT SELECT '+@AllColumns+' FROM app.PlatformRiderAccounts WHERE ExternalAccountId NOT IN(SELECT Account FROM #Targets))
 OR EXISTS(SELECT '+@AllColumns+' FROM app.PlatformRiderAccounts WHERE ExternalAccountId NOT IN(SELECT Account FROM #Targets)
 EXCEPT SELECT '+@AllColumns+' FROM #AccountBefore WHERE ExternalAccountId NOT IN(SELECT Account FROM #Targets))
 THROW 51000,''An account outside the supplied list changed.'',1;';
EXEC sys.sp_executesql @Check;
IF EXISTS(SELECT 1 FROM app.PlatformRiderAccounts a JOIN #Targets t ON t.Account=a.ExternalAccountId WHERE a.SponsorId IS NULL OR a.SponsorId<>@Sponsor)
 THROW 51000,'Sponsor verification failed.',1;
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
SELECT NEWID(),NEWID(),@Actor,'User','JahezAccountSponsorCorrection','jahez','PlatformRiderAccount',a.Id,@Now,@Correlation,@Reason,
 (SELECT b.ExternalAccountId,b.SponsorId,CAST('7015658094' AS varchar(10)) SponsorNumber FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 (SELECT a.ExternalAccountId,a.SponsorId,CAST('7038745530' AS varchar(10)) SponsorNumber FOR JSON PATH,INCLUDE_NULL_VALUES,WITHOUT_ARRAY_WRAPPER),
 'CodexAuthorizedMaintenance',1,@Now,@Actor
FROM app.PlatformRiderAccounts a JOIN #Targets t ON t.Account=a.ExternalAccountId JOIN #AccountBefore b ON b.Id=a.Id;
IF @@ROWCOUNT<>39 THROW 51000,'Expected 39 audit entries.',1;
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@Now,UpdatedAtUtc=@Now,UpdatedByUserId=@Actor
WHERE ModuleKey IN('jahez','platform-operations');
IF @@ROWCOUNT<>2 THROW 51000,'Expected two dataset versions.',1;
SELECT a.Id AccountId,a.ExternalAccountId Account,'7015658094' PreviousSponsor,s.EmployerIdentityNumber NewSponsor,s.RegistryNameAr SponsorName
FROM app.PlatformRiderAccounts a JOIN #Targets t ON t.Account=a.ExternalAccountId JOIN app.Sponsors s ON s.Id=a.SponsorId ORDER BY a.ExternalAccountId;
'@
    $taskResult.WriteXml((Join-Path $taskOutput 'transaction-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $taskResult.Tables[0] | Select-Object AccountId,Account,PreviousSponsor,NewSponsor,SponsorName |
        Export-Csv -LiteralPath (Join-Path $taskOutput 'report.csv') -NoTypeInformation -Encoding utf8
    if ($Apply) {
        $taskTransaction.Commit()
        $taskCommitted = $true
        $taskTransaction.Dispose()
        $taskTransaction = $null
        $taskConnection.Close()
        $taskConnection.Open()
        $taskVerified = Read-TaskData @'
SELECT COUNT(*) Verified,SUM(CASE WHEN p.Code='JAHEZ' AND a.IsDeleted=0 AND s.EmployerIdentityNumber='7038745530' THEN 0 ELSE 1 END) Mismatches
FROM OPENJSON(@Plan) t JOIN app.PlatformRiderAccounts a ON a.ExternalAccountId=t.value
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId LEFT JOIN app.Sponsors s ON s.Id=a.SponsorId;
SELECT COUNT(*) AuditCount FROM audit.AuditEntries WHERE CorrelationId=@Correlation AND Action='JahezAccountSponsorCorrection';
'@
        $taskVerified.WriteXml((Join-Path $taskOutput 'post-commit-verification.xml'), [System.Data.XmlWriteMode]::WriteSchema)
        if ($taskVerified.Tables[0].Rows[0].Verified -ne 39 -or $taskVerified.Tables[0].Rows[0].Mismatches -ne 0 -or $taskVerified.Tables[1].Rows[0].AuditCount -ne 39) {
            throw 'Post-commit verification failed; do not replay.'
        }
        Write-Output 'COMMITTED AND VERIFIED: 39 accounts now use sponsor 7038745530; 39 audit entries.'
    } else {
        $taskTransaction.Rollback()
        $taskTransaction.Dispose()
        $taskTransaction = $null
        Write-Output 'DRY RUN VERIFIED AND ROLLED BACK: exactly 39 sponsors changed; unrelated fields and other accounts preserved.'
    }
    Write-Output ('Evidence: ' + $taskOutput)
} catch {
    if ($taskTransaction -and -not $taskCommitted) { $taskTransaction.Rollback() }
    throw
} finally {
    if ($taskTransaction) { $taskTransaction.Dispose() }
    $taskConnection.Dispose()
}
