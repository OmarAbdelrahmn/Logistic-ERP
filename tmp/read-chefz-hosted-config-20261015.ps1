$ErrorActionPreference='Stop'
$profile=[xml](Get-Content 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml' -Raw)
$private=[xml](Get-Content 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml.user' -Raw)
$plain=[Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($private.Project.PropertyGroup.EncryptedPassword),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
$enc=if(@($plain|Where-Object {$_ -eq 0}).Count -gt $plain.Length/4){[Text.Encoding]::Unicode}else{[Text.Encoding]::UTF8}
$secret=$enc.GetString($plain);[Array]::Clear($plain,0,$plain.Length)
$site=[string]$profile.Project.PropertyGroup.DeployIisAppPath;$server=[string]$profile.Project.PropertyGroup.MSDeployServiceURL;$username=[string]$profile.Project.PropertyGroup.UserName
$endpoint="https://${server}:8172/msdeploy.axd?site=$site"
$dir=Join-Path (Get-Location) 'artifacts/chefz-import-20261015';[void](New-Item -ItemType Directory -Path $dir -Force)
try {
 foreach($file in @('appsettings.json','web.config')){
  $argsList=@('-verb:sync',"-source:contentPath='$site/$file',computerName='$endpoint',userName='$username',password='$($secret.Replace("'","''"))',authType='Basic'","-dest:contentPath='$(Join-Path $dir ('current-'+$file))'")
  $p=[Diagnostics.Process]::new();$p.StartInfo.FileName='C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe';$p.StartInfo.Arguments=$argsList -join ' ';$p.StartInfo.UseShellExecute=$false;$p.StartInfo.CreateNoWindow=$true;$p.StartInfo.RedirectStandardOutput=$true;$p.StartInfo.RedirectStandardError=$true
  [void]$p.Start();$stdout=$p.StandardOutput.ReadToEndAsync();$stderr=$p.StandardError.ReadToEndAsync();$p.WaitForExit();$output=$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult();$code=$p.ExitCode;$p.Dispose()
  if($code -ne 0){throw ('Read-only remote config retrieval failed: '+$output.Replace($secret,'[redacted]'))}
  Write-Output ('Retrieved '+$file+' without displaying its contents.')
 }
 $cfg=Get-Content (Join-Path $dir 'current-appsettings.json') -Raw|ConvertFrom-Json
 $web=[xml](Get-Content (Join-Path $dir 'current-web.config') -Raw)
 $vars=@($web.configuration.location.'system.webServer'.aspNetCore.environmentVariables.environmentVariable)
 Write-Output ('Configured encryption key: '+[bool]$cfg.SensitiveData.EncryptionKey)
 Write-Output ('Environment variable names: '+(($vars|ForEach-Object {$_.name}) -join ', '))
}finally{$secret=$null;$argsList=$null}
