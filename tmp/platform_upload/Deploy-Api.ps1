param([ValidateSet('Plan', 'Apply', 'Backup')][string]$Mode = 'Plan')
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$profile = [xml](Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml') -Raw)
$privateProfile = [xml](Get-Content -LiteralPath (Join-Path $workspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml.user') -Raw)
$cipher = [Convert]::FromBase64String($privateProfile.Project.PropertyGroup.EncryptedPassword)
$plain = [System.Security.Cryptography.ProtectedData]::Unprotect($cipher, $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
$encoding = if (@($plain | Where-Object { $_ -eq 0 }).Count -gt $plain.Length / 4) { [Text.Encoding]::Unicode } else { [Text.Encoding]::UTF8 }
$secret = $encoding.GetString($plain)
[Array]::Clear($plain, 0, $plain.Length)
$site = [string]$profile.Project.PropertyGroup.DeployIisAppPath
$server = [string]$profile.Project.PropertyGroup.MSDeployServiceURL
$username = [string]$profile.Project.PropertyGroup.UserName
$endpoint = "https://${server}:8172/msdeploy.axd?site=$site"
$source = (Resolve-Path -LiteralPath 'C:\Users\omarf\.codex\worktrees\platform-owner-import\Logistic ERP\artifacts\platform-owner-api').Path
$destination = "-dest:iisApp='$site',computerName='$endpoint',userName='$username',password='$($secret.Replace("'", "''"))',authType='Basic'"
$arguments = @(
    '-verb:sync', "-source:iisApp='$source'", $destination,
    '-enableRule:DoNotDeleteRule', '-enableRule:AppOffline',
    '-skip:objectName=filePath,absolutePath=(?i)\\appsettings(\.[^\\]+)?\.json$',
    '-skip:objectName=dirPath,absolutePath=(?i)\\App_Data(\\|$)',
    '-skip:objectName=dirPath,absolutePath=(?i)\\wwwroot\\private(\\|$)'
)
if ($Mode -eq 'Plan') { $arguments += '-whatif' }
if ($Mode -eq 'Backup') {
    $remoteSource = $destination.Replace('-dest:', '-source:')
    $backupPath = Join-Path $PSScriptRoot 'previous-hosted-api.zip'
    if (Test-Path -LiteralPath $backupPath) { throw 'A previous API backup already exists; do not overwrite it.' }
    $arguments = @(
        '-verb:sync', $remoteSource, "-dest:package='$backupPath'",
        '-skip:objectName=dirPath,absolutePath=(?i)\\App_Data(\\|$)',
        '-skip:objectName=dirPath,absolutePath=(?i)\\wwwroot\\private(\\|$)'
    )
}
try {
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo.FileName = 'C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe'
    $process.StartInfo.Arguments = $arguments -join ' '
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $output = ($stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()) -split '\r?\n'
    $code = $process.ExitCode
    $process.Dispose()
    $sanitized = @($output | ForEach-Object { ([string]$_).Replace($secret, '[redacted]') })
    $sanitized | Set-Content -LiteralPath (Join-Path $PSScriptRoot "api-deploy-$($Mode.ToLowerInvariant()).log") -Encoding utf8
    $sanitized | Select-Object -Last 12
    if ($code -ne 0) { throw "API deployment $Mode failed with exit code $code. See the sanitized deployment log." }
} finally {
    $secret = $null
    $destination = $null
    $arguments = $null
}
