param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$chefzWorkspace = Split-Path -Parent $PSScriptRoot
$chefzPrivate = Join-Path $chefzWorkspace ('artifacts/chefz-key-recovery-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
[void](New-Item -ItemType Directory -Path $chefzPrivate)
$chefzKeyFile = Join-Path $chefzWorkspace 'artifacts/chefz-import-20261015/encryption-key.txt'
$chefzMasterText = (Get-Content -LiteralPath $chefzKeyFile -Raw).Trim()
$chefzMasterBytes = [Convert]::FromBase64String($chefzMasterText)
if ($chefzMasterBytes.Length -lt 32) { throw 'Original key backup is invalid.' }
$chefzProfile = [xml](Get-Content -LiteralPath (Join-Path $chefzWorkspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml') -Raw)
$chefzPrivateProfile = [xml](Get-Content -LiteralPath (Join-Path $chefzWorkspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml.user') -Raw)
$chefzDeployBytes = [Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($chefzPrivateProfile.Project.PropertyGroup.EncryptedPassword), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
$chefzDeployEncoding = if (@($chefzDeployBytes | Where-Object { $_ -eq 0 }).Count -gt $chefzDeployBytes.Length / 4) { [Text.Encoding]::Unicode } else { [Text.Encoding]::UTF8 }
$chefzDeploySecret = $chefzDeployEncoding.GetString($chefzDeployBytes)
[Array]::Clear($chefzDeployBytes, 0, $chefzDeployBytes.Length)
$chefzSite = [string]$chefzProfile.Project.PropertyGroup.DeployIisAppPath
$chefzDeployServer = [string]$chefzProfile.Project.PropertyGroup.MSDeployServiceURL
$chefzDeployUser = [string]$chefzProfile.Project.PropertyGroup.UserName
if ($chefzSite -ne 'site91012' -or $chefzDeployServer -ne 'site91012.siteasp.net') { throw 'Unexpected hosting target.' }
$chefzEndpoint = "https://${chefzDeployServer}:8172/msdeploy.axd?site=$chefzSite"

function Sync-ChefzConfig([string]$Remote, [string]$Local, [switch]$Upload) {
    $connection = "computerName='$chefzEndpoint',userName='$chefzDeployUser',password='$($chefzDeploySecret.Replace("'", "''"))',authType='Basic'"
    $arguments = if ($Upload) {
        @('-verb:sync', "-source:contentPath='$Local'", "-dest:contentPath='$chefzSite/$Remote',$connection")
    } else {
        @('-verb:sync', "-source:contentPath='$chefzSite/$Remote',$connection", "-dest:contentPath='$Local'")
    }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo.FileName = 'C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe'
    $process.StartInfo.Arguments = $arguments -join ' '
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (!$process.WaitForExit(45000)) { $process.Kill(); throw 'Hosting configuration operation timed out.' }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw ('Hosting configuration operation failed: ' + $output.Replace($chefzDeploySecret, '[redacted]').Replace($chefzMasterText, '[redacted]')) }
    } finally { $process.Dispose() }
}

$chefzChanged = $false
try {
    $chefzOldPath = Join-Path $chefzPrivate 'appsettings.before.json'
    $chefzWebPath = Join-Path $chefzPrivate 'web.before.config'
    Sync-ChefzConfig 'appsettings.json' $chefzOldPath
    Sync-ChefzConfig 'web.config' $chefzWebPath
    $chefzOld = Get-Content -LiteralPath $chefzOldPath -Raw | ConvertFrom-Json -AsHashtable
    $chefzWeb = [xml](Get-Content -LiteralPath $chefzWebPath -Raw)
    $chefzVariables = @($chefzWeb.configuration.location.'system.webServer'.aspNetCore.environmentVariables.environmentVariable)
    if ($chefzVariables | Where-Object { $_.name -in @('SensitiveData__EncryptionKey', 'SensitiveData:EncryptionKey') }) { throw 'A hosted environment key override requires separate review.' }
    if ($chefzOld.SensitiveData.EncryptionKey -and $chefzOld.SensitiveData.EncryptionKey -cne $chefzMasterText) { throw 'A different key is configured; it will not be replaced.' }

    $chefzDbConfig = Get-Content -LiteralPath (Join-Path $chefzWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
    $chefzConnection = [Data.SqlClient.SqlConnection]::new($chefzDbConfig.ConnectionStrings.LogisticsDatabase)
    if ($chefzConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $chefzConnection.Database -ne 'db67927') { throw 'Unexpected database target.' }
    $chefzDerived = [Security.Cryptography.HMACSHA256]::HashData($chefzMasterBytes, [Text.Encoding]::UTF8.GetBytes('LogisticsERP.PlatformCredential.v1'))
    $chefzAes = [Security.Cryptography.AesGcm]::new($chefzDerived, 16)
    try {
        $chefzConnection.Open(); $chefzCommand = $chefzConnection.CreateCommand()
        $chefzCommand.CommandText = @'
SELECT v.PlatformRiderAccountId,v.Ciphertext,v.Nonce,v.AuthenticationTag
FROM app.PlatformAccountCredentialVersions v
JOIN app.PlatformRiderAccounts a ON a.Id=v.PlatformRiderAccountId
JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
WHERE p.Code IN(N'CHEFZ',N'THECHEFZ',N'CHEFSZ',N'SHIFTZ');
'@
        $chefzReader = $chefzCommand.ExecuteReader(); $chefzChecked = 0; $chefzTargetChecked = 0
        while ($chefzReader.Read()) {
            $plaintext = [byte[]]::new($chefzReader.GetValue(1).Length)
            try { $chefzAes.Decrypt([byte[]]$chefzReader.GetValue(2), [byte[]]$chefzReader.GetValue(1), [byte[]]$chefzReader.GetValue(3), $plaintext) }
            finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($plaintext) }
            $chefzChecked++
            if ($chefzReader.GetGuid(0) -eq [Guid]'2dee6b7f-a813-4429-a9bc-3b3d0c1330fe') { $chefzTargetChecked++ }
        }
        $chefzReader.Close()
        if ($chefzChecked -eq 0 -or $chefzTargetChecked -eq 0) { throw 'No matching Chefz credentials were verified.' }
    } finally {
        if ($chefzReader) { $chefzReader.Dispose() }; if ($chefzCommand) { $chefzCommand.Dispose() }
        $chefzConnection.Dispose(); $chefzAes.Dispose(); [Security.Cryptography.CryptographicOperations]::ZeroMemory($chefzDerived)
    }
    if ($chefzOld.SensitiveData.EncryptionKey) {
        [pscustomobject]@{AlreadyConfigured=$true;ChefzCredentialsVerified=$chefzChecked;TargetCredentialVerified=$true;DatabaseWrites=0} | ConvertTo-Json
        return
    }
    $chefzCandidate = $chefzOld | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
    if (!$chefzCandidate.ContainsKey('SensitiveData')) { $chefzCandidate['SensitiveData'] = [ordered]@{} }
    $chefzCandidate.SensitiveData['EncryptionKey'] = $chefzMasterText
    $chefzComparison = $chefzCandidate | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
    [void]$chefzComparison.SensitiveData.Remove('EncryptionKey')
    if (!$chefzOld.ContainsKey('SensitiveData')) { [void]$chefzComparison.Remove('SensitiveData') }
    if (($chefzOld | ConvertTo-Json -Depth 100 -Compress) -cne ($chefzComparison | ConvertTo-Json -Depth 100 -Compress)) { throw 'Candidate changes unrelated configuration.' }
    $chefzCandidatePath = Join-Path $chefzPrivate 'appsettings.proposed.json'
    $chefzCandidate | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $chefzCandidatePath -Encoding utf8NoBOM
    $chefzNewWebPath = Join-Path $chefzPrivate 'web.proposed.config'
    [IO.File]::WriteAllText($chefzNewWebPath, ((Get-Content -LiteralPath $chefzWebPath -Raw) + [Environment]::NewLine + '<!-- Reload after restoring the original Chefz credential key, 2026-10-25. -->'), [Text.UTF8Encoding]::new($false))
    [void][xml](Get-Content -LiteralPath $chefzNewWebPath -Raw)
    if (!$Apply) {
        [pscustomobject]@{Mode='Plan';ChefzCredentialsVerified=$chefzChecked;TargetCredentialVerified=$true;Change='Restore original encryption key and recycle API';DatabaseWrites=0;PrivateBackupDirectory=$chefzPrivate} | ConvertTo-Json
        return
    }
    $chefzFreshPath = Join-Path $chefzPrivate 'appsettings.fresh.json'
    $chefzFreshWeb = Join-Path $chefzPrivate 'web.fresh.config'
    Sync-ChefzConfig 'appsettings.json' $chefzFreshPath
    Sync-ChefzConfig 'web.config' $chefzFreshWeb
    if ((Get-FileHash $chefzFreshPath).Hash -ne (Get-FileHash $chefzOldPath).Hash -or (Get-FileHash $chefzFreshWeb).Hash -ne (Get-FileHash $chefzWebPath).Hash) { throw 'Hosted configuration changed since preparation.' }
    Sync-ChefzConfig 'appsettings.json' $chefzCandidatePath -Upload; $chefzChanged = $true
    Sync-ChefzConfig 'web.config' $chefzNewWebPath -Upload
    $chefzActualPath = Join-Path $chefzPrivate 'appsettings.verified.json'
    Sync-ChefzConfig 'appsettings.json' $chefzActualPath
    if ((Get-Content -LiteralPath $chefzActualPath -Raw | ConvertFrom-Json).SensitiveData.EncryptionKey -cne $chefzMasterText) { throw 'Hosted key read-back verification failed.' }
    $chefzHealthUri = ([string]$chefzProfile.Project.PropertyGroup.SiteUrlToLaunchAfterPublish).Replace('http://', 'https://').TrimEnd('/') + '/health/live'
    $chefzHealthy = $false
    for ($chefzAttempt = 0; $chefzAttempt -lt 4; $chefzAttempt++) {
        try { $chefzHealthy = (Invoke-WebRequest -Uri $chefzHealthUri -TimeoutSec 15).StatusCode -eq 200 } catch { }
        if ($chefzHealthy) { break }; Start-Sleep -Seconds 2
    }
    if (!$chefzHealthy) { throw 'API health did not recover after the configuration reload.' }
    $chefzReport = [ordered]@{VerifiedAtUtc=[DateTime]::UtcNow.ToString('o');HostedOriginalKeyRestored=$true;ConfigurationReadBackVerified=$true;ApiHealthStatus=200;ChefzCredentialsVerified=$chefzChecked;TargetCredentialVerified=$true;DatabaseWrites=0;PasswordsReset=0;AuthenticatedEndpointVerified=$false;PrivateBackupDirectory=$chefzPrivate}
    $chefzReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $chefzPrivate 'report.json') -Encoding utf8
    $chefzReport | ConvertTo-Json -Depth 4
} catch {
    if ($chefzChanged) {
        Sync-ChefzConfig 'appsettings.json' $chefzOldPath -Upload
        Sync-ChefzConfig 'web.config' $chefzWebPath -Upload
        Write-Output 'Previous hosted configuration restored after a failed verification.'
    }
    throw
} finally {
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($chefzMasterBytes)
    $chefzMasterText = $null; $chefzDeploySecret = $null
}
