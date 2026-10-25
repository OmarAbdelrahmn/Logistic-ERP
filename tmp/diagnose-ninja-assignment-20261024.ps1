$ErrorActionPreference = 'Stop'
$profile = [xml](Get-Content 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml' -Raw)
$private = [xml](Get-Content 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml.user' -Raw)
$plain = [Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($private.Project.PropertyGroup.EncryptedPassword), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
$encoding = if (@($plain | Where-Object { $_ -eq 0 }).Count -gt $plain.Length / 4) { [Text.Encoding]::Unicode } else { [Text.Encoding]::UTF8 }
$secret = $encoding.GetString($plain)
[Array]::Clear($plain, 0, $plain.Length)
$site = [string]$profile.Project.PropertyGroup.DeployIisAppPath
$server = [string]$profile.Project.PropertyGroup.MSDeployServiceURL
$username = [string]$profile.Project.PropertyGroup.UserName
$endpoint = "https://${server}:8172/msdeploy.axd?site=$site"
$folder = Join-Path (Get-Location) 'tmp/ninja-assignment-diagnostic-20261024'
[void](New-Item -ItemType Directory -Path $folder -Force)
function Read-Remote([string]$remote, [string]$local) {
    $process = [Diagnostics.Process]::new()
    $process.StartInfo.FileName = 'C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe'
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $remoteSource = "-source:contentPath='$site/$remote',computerName='$endpoint',userName='$username',password='$($secret.Replace("'", "''"))',authType='Basic'"
    $process.StartInfo.Arguments = if ($local) { "-verb:sync $remoteSource -dest:contentPath='$local'" } else { "-verb:dump $remoteSource" }
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'Read-only remote retrieval timed out.' }
    $output = ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()).Replace($secret, '[redacted]')
    $code = $process.ExitCode
    $process.Dispose()
    if ($code -ne 0) { throw $output }
    if (-not $local) { Write-Output $output }
    return $true
}
try {
    $configFile = Join-Path $folder 'web.config'
    if (Read-Remote 'web.config' $configFile) {
        $web = [xml](Get-Content $configFile -Raw)
        $core = $web.configuration.location.'system.webServer'.aspNetCore
        [pscustomobject]@{ stdoutLogEnabled = $core.stdoutLogEnabled; stdoutLogFile = $core.stdoutLogFile; hostingModel = $core.hostingModel } | ConvertTo-Json -Compress
    }
    $settingsFile = Join-Path $folder 'appsettings.json'
    [void](Read-Remote 'appsettings.json' $settingsFile)
    $settings = Get-Content $settingsFile -Raw | ConvertFrom-Json
    $keyValue = [string]$settings.SensitiveData.EncryptionKey
    $keyValid = $false
    if ($keyValue) { try { $keyValid = [Convert]::FromBase64String($keyValue).Length -ge 32 } catch { } }
    $variables = @($core.environmentVariables.environmentVariable)
    [pscustomobject]@{ EncryptionKeyInAppsettings = [bool]$keyValue; EncryptionKeyValid = $keyValid;
        EncryptionKeyEnvironmentOverride = [bool]($variables | Where-Object name -eq 'SensitiveData__EncryptionKey');
        HostingEnvironment = ($variables | Where-Object name -in @('ASPNETCORE_ENVIRONMENT','DOTNET_ENVIRONMENT') | ForEach-Object value) } | ConvertTo-Json -Compress
    try {
        $productionFile = Join-Path $folder 'appsettings.Production.json'
        [void](Read-Remote 'appsettings.Production.json' $productionFile)
        $productionSettings = Get-Content $productionFile -Raw | ConvertFrom-Json
        Write-Output ('Encryption key in Production settings: ' + [bool]$productionSettings.SensitiveData.EncryptionKey)
    } catch {
        if ($_.Exception.Message -match 'FileOrFolderNotFound') { Write-Output 'No appsettings.Production.json exists on the hosted site.' }
        else { throw }
    }
} finally { $secret = $null }
