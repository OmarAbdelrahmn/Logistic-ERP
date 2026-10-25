param(
    [switch]$Apply,
    [string]$PlanPath
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
# The user changed the hosting master key on 25 October (Riyadh).
# Only credentials saved before that local date are eligible.
$cutoff = [DateTimeOffset]::Parse('2026-10-24T21:00:00Z')
$settings = Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $settings.ConnectionStrings.LogisticsDatabase }
$connection = [Data.SqlClient.SqlConnection]::new($connectionText)
$transaction = $null
$runDirectory = Join-Path $workspace ('artifacts/ninja-password-cleanup-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))

function Read-CleanupData([string]$Sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 60
    $command.Transaction = $transaction
    if ($Sql.Contains('@cutoff', [StringComparison]::Ordinal)) {
        [void]$command.Parameters.AddWithValue('@cutoff', $cutoff)
    }
    $adapter = [Data.SqlClient.SqlDataAdapter]::new($command)
    try {
        $data = [Data.DataSet]::new()
        [void]$adapter.Fill($data)
        return ,$data
    } finally { $adapter.Dispose(); $command.Dispose() }
}

function Credential-Fingerprint($Row) {
    $fields = @($Row.Table.Columns | ForEach-Object {
        $value = $Row[$_.ColumnName]
        if ($value -is [byte[]]) { [Convert]::ToBase64String($value) }
        elseif ($value -is [DBNull]) { $null }
        elseif ($value -is [DateTimeOffset]) { $value.ToString('o') }
        else { [string]$value }
    })
    $bytes = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json -InputObject $fields -Compress))
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
}

function Target-Manifest($Table) {
    return @($Table.Rows | ForEach-Object {
        [ordered]@{Id=$_.Id.ToString();AccountId=$_.PlatformRiderAccountId.ToString();Fingerprint=(Credential-Fingerprint $_)}
    } | Sort-Object { $_.Id })
}

$targetSql = @'
SELECT v.* FROM app.PlatformAccountCredentialVersions v
JOIN app.PlatformRiderAccounts a ON a.Id=v.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE p.Code=N'NINJA' AND v.RotatedAtUtc < @cutoff ORDER BY v.Id;
SELECT p.Code,COUNT_BIG(*) AS CredentialVersions,COUNT(DISTINCT v.PlatformRiderAccountId) AS AccountsWithPasswords
FROM app.PlatformAccountCredentialVersions v
JOIN app.PlatformRiderAccounts a ON a.Id=v.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId GROUP BY p.Code ORDER BY p.Code;
'@

try {
    if ($connection.DataSource -ne 'db67927.public.databaseasp.net' -or $connection.Database -ne 'db67927') {
        throw 'Unexpected database target. No query executed.'
    }
    $connection.Open()
    $before = Read-CleanupData $targetSql
    $targets = Target-Manifest $before.Tables[0]
    if (!$Apply) {
        [void](New-Item -ItemType Directory -Path $runDirectory)
        $before.WriteXml((Join-Path $runDirectory 'preflight-encrypted.xml'), [Data.XmlWriteMode]::WriteSchema)
        $plan = [ordered]@{Scope='NINJA';CutoffUtc=$cutoff.ToString('o');ExpectedVersions=89;Targets=$targets}
        $planFile = Join-Path $runDirectory 'plan.json'
        $plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $planFile -Encoding utf8
        [pscustomobject]@{Mode='Plan';Scope='NINJA';CandidateVersions=$targets.Count;ExpectedVersions=89;
            CutoffUtc=$cutoff.ToString('o');DatabaseWrites=0;PlanPath=$planFile} | ConvertTo-Json
        return
    }
    if (!$PlanPath) { throw 'Apply requires the exact plan file created by read-only preflight.' }
    $plan = Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
    if ($plan.Scope -ne 'NINJA' -or $plan.ExpectedVersions -ne 89 -or $plan.CutoffUtc -ne $cutoff.ToString('o') -or $plan.Targets.Count -ne 89) {
        throw 'The reviewed plan does not match this authorized Ninja cleanup.'
    }
    if ($targets.Count -eq 0) {
        $present = Read-CleanupData 'SELECT Id FROM app.PlatformAccountCredentialVersions;'
        if (@($present.Tables[0].Rows | Where-Object { $_.Id.ToString() -in $plan.Targets.Id }).Count -ne 0) {
            throw 'Planned credentials still exist with changed scope or timestamps.'
        }
        [pscustomobject]@{Mode='AlreadyRemoved';DeletedVersions=0;DatabaseWrites=0;Scope='NINJA'} | ConvertTo-Json
        return
    }
    if ($targets.Count -ne 89 -or (ConvertTo-Json -InputObject $targets -Depth 8 -Compress) -cne
        (ConvertTo-Json -InputObject @($plan.Targets | ForEach-Object { [ordered]@{Id=$_.Id;AccountId=$_.AccountId;Fingerprint=$_.Fingerprint} }) -Depth 8 -Compress)) {
        throw 'Credential rows changed since the reviewed plan. No deletion performed.'
    }

    [void](New-Item -ItemType Directory -Path $runDirectory)
    $transaction = $connection.BeginTransaction([Data.IsolationLevel]::Serializable)
    [void](Read-CleanupData 'SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;')
    # Match application write locks before taking the short credential-table lock.
    $accountIds = [Guid[]]@($targets.AccountId | Select-Object -Unique)
    [Array]::Sort($accountIds)
    foreach ($accountId in $accountIds) {
        $lockCommand = $connection.CreateCommand()
        $lockCommand.Transaction = $transaction
        $lockCommand.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; SELECT @r;"
        [void]$lockCommand.Parameters.AddWithValue('@resource', "platform-account:$($accountId.ToString('D'))")
        try { if ([int]$lockCommand.ExecuteScalar() -lt 0) { throw 'An account is being changed. Cleanup aborted.' } }
        finally { $lockCommand.Dispose() }
    }
    [void](Read-CleanupData @'
DECLARE @removalCutoff datetimeoffset='2026-10-24T21:00:00Z';
IF EXISTS(SELECT 1 FROM sys.triggers WHERE parent_id=OBJECT_ID(N'app.PlatformAccountCredentialVersions') AND is_disabled=0)
    THROW 51000,'Unexpected credential trigger. Cleanup aborted.',1;
SELECT * INTO #CredentialsBefore FROM app.PlatformAccountCredentialVersions WITH(TABLOCKX,HOLDLOCK);
SELECT a.* INTO #NinjaAccountsBefore FROM app.PlatformRiderAccounts a WITH(HOLDLOCK)
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA';
SELECT v.Id INTO #Remove FROM app.PlatformAccountCredentialVersions v
JOIN app.PlatformRiderAccounts a ON a.Id=v.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE p.Code=N'NINJA' AND v.RotatedAtUtc < @removalCutoff;
IF (SELECT COUNT_BIG(*) FROM #Remove)<>89 THROW 51000,'Target count changed. Cleanup aborted.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountCredentialVersions v JOIN #Remove r ON r.Id=v.SupersededVersionId)
    THROW 51000,'A credential history reference requires separate review. Cleanup aborted.',1;
'@)
    $locked = Read-CleanupData $targetSql
    $lockedTargets = Target-Manifest $locked.Tables[0]
    if ((ConvertTo-Json -InputObject $lockedTargets -Depth 8 -Compress) -cne (ConvertTo-Json -InputObject $targets -Depth 8 -Compress)) {
        throw 'Credentials changed while cleanup locks were acquired.'
    }
    $backup = Read-CleanupData 'SELECT * FROM #CredentialsBefore; SELECT * FROM #NinjaAccountsBefore;'
    $backupPath = Join-Path $runDirectory 'before-delete-encrypted.xml'
    $backup.WriteXml($backupPath, [Data.XmlWriteMode]::WriteSchema)
    $readBack = [Data.DataSet]::new()
    [void]$readBack.ReadXml($backupPath, [Data.XmlReadMode]::ReadSchema)
    if ($readBack.Tables[0].Rows.Count -ne $backup.Tables[0].Rows.Count -or
        (ConvertTo-Json -InputObject (Target-Manifest $readBack.Tables[0]) -Depth 8 -Compress) -cne
        (ConvertTo-Json -InputObject (Target-Manifest $backup.Tables[0]) -Depth 8 -Compress)) {
        throw 'Encrypted backup read-back verification failed.'
    }
    $deleted = Read-CleanupData @'
DELETE v FROM app.PlatformAccountCredentialVersions v JOIN #Remove r ON r.Id=v.Id;
SELECT @@ROWCOUNT AS DeletedVersions;
IF EXISTS(SELECT * FROM #CredentialsBefore WHERE Id NOT IN(SELECT Id FROM #Remove)
          EXCEPT SELECT * FROM app.PlatformAccountCredentialVersions)
 OR EXISTS(SELECT * FROM app.PlatformAccountCredentialVersions
          EXCEPT SELECT * FROM #CredentialsBefore WHERE Id NOT IN(SELECT Id FROM #Remove))
    THROW 51000,'Retained credential preservation failed.',1;
IF EXISTS(SELECT 1 FROM app.PlatformAccountCredentialVersions v JOIN #Remove r ON r.Id=v.Id)
    THROW 51000,'A targeted password remains.',1;
IF EXISTS(SELECT * FROM #NinjaAccountsBefore EXCEPT SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA')
 OR EXISTS(SELECT a.* FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA' EXCEPT SELECT * FROM #NinjaAccountsBefore)
    THROW 51000,'Ninja account preservation failed.',1;
'@
    if ([int]$deleted.Tables[0].Rows[0].DeletedVersions -ne 89) { throw 'Deletion count verification failed.' }
    $transaction.Commit()
    $transaction.Dispose()
    $transaction = $null
    $after = Read-CleanupData $targetSql
    if ($after.Tables[0].Rows.Count -ne 0) { throw 'Post-commit verification found legacy credentials.' }
    $after.WriteXml((Join-Path $runDirectory 'after.xml'), [Data.XmlWriteMode]::WriteSchema)
    $report = [ordered]@{Mode='Applied';VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');Scope='NINJA';DeletedVersions=89;
        RemainingLegacyNinjaVersions=0;OtherCredentialsPreserved=$true;NinjaAccountsPreserved=$true;EncryptedBackupVerified=$true;
        BackupPath=$backupPath;RunDirectory=$runDirectory;PlanPath=(Resolve-Path -LiteralPath $PlanPath).Path}
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runDirectory 'report.json') -Encoding utf8
    $report | ConvertTo-Json -Depth 5
} catch {
    if ($transaction) { try { $transaction.Rollback() } catch { }; $transaction.Dispose(); $transaction=$null }
    throw
} finally { $connection.Dispose(); $settings=$null; $connectionText=$null }
