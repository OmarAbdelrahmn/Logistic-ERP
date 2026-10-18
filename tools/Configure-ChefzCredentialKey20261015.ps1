# Applies only the prepared SensitiveData encryption setting and an IIS recycle comment.
param([switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$private=Join-Path $workspace 'artifacts/chefz-import-20261015'
$oldPath=Join-Path $private 'current-appsettings.json';$candidatePath=Join-Path $private 'appsettings.proposed.json'
$old=Get-Content -LiteralPath $oldPath -Raw|ConvertFrom-Json -AsHashtable
$candidate=Get-Content -LiteralPath $candidatePath -Raw|ConvertFrom-Json -AsHashtable
$key=Get-Content (Join-Path $private 'encryption-key.txt') -Raw
if($old.SensitiveData.EncryptionKey){throw 'A hosted key is already configured. Do not replace it.'}
if($candidate.SensitiveData.EncryptionKey -cne $key.Trim()){throw 'Candidate key does not match the prepared encrypted passwords.'}
$comparison=$candidate|ConvertTo-Json -Depth 30|ConvertFrom-Json -AsHashtable
[void]$comparison.SensitiveData.Remove('EncryptionKey')
if(!$comparison.SensitiveData.Count -and !$old.ContainsKey('SensitiveData')){[void]$comparison.Remove('SensitiveData')}
if(($old|ConvertTo-Json -Depth 30 -Compress) -cne ($comparison|ConvertTo-Json -Depth 30 -Compress)){throw 'The candidate changes settings beyond the requested encryption key.'}
$web=Get-Content (Join-Path $private 'current-web.config') -Raw
$newWeb=$web+[Environment]::NewLine+'<!-- Reload after initial credential encryption configuration, 2026-10-15. -->'+[Environment]::NewLine
$webPath=Join-Path $private 'web.proposed.config';[IO.File]::WriteAllText($webPath,$newWeb,[Text.UTF8Encoding]::new($false))
if(!$Apply){Write-Output 'Reviewed: only SensitiveData:EncryptionKey is added; web.config adds a comment to recycle the API. Secret files remain in ignored artifacts.';return}
$cfg=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$conn=[Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try{
 $conn.Open();$q=$conn.CreateCommand();$q.CommandText=@'
SELECT (SELECT COUNT(*) FROM app.PlatformAccountCredentialVersions)
 +(SELECT COUNT(*) FROM app.RiderHealthCards WHERE CardNumberCiphertext IS NOT NULL)
 +(SELECT COUNT(*) FROM app.EmployeeDriverLicenses WHERE LicenseNumberCiphertext IS NOT NULL)
 +(SELECT COUNT(*) FROM app.EmployeeMedicalInsurancePolicies WHERE PolicyNumberCiphertext IS NOT NULL OR MemberNumberCiphertext IS NOT NULL);
'@
 if([int]$q.ExecuteScalar() -ne 0){throw 'Existing encrypted records appeared; do not configure a new key.'}
}finally{$conn.Dispose()}
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
& (Join-Path $workspace 'tmp/read-chefz-hosted-config-20261015.ps1')
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Hosted settings changed since preparation; review again.'}
Copy-Item -LiteralPath $oldPath -Destination (Join-Path $private 'appsettings.before-key.json')
Copy-Item -LiteralPath (Join-Path $private 'current-web.config') -Destination (Join-Path $private 'web.before-key.config')
$profile=[xml](Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml') -Raw)
$privateProfile=[xml](Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/Properties/PublishProfiles/site91012-WebDeploy.pubxml.user') -Raw)
$plain=[Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($privateProfile.Project.PropertyGroup.EncryptedPassword),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
$enc=if(@($plain|Where-Object {$_ -eq 0}).Count -gt $plain.Length/4){[Text.Encoding]::Unicode}else{[Text.Encoding]::UTF8}
$secret=$enc.GetString($plain);[Array]::Clear($plain,0,$plain.Length)
$site=[string]$profile.Project.PropertyGroup.DeployIisAppPath;$server=[string]$profile.Project.PropertyGroup.MSDeployServiceURL;$username=[string]$profile.Project.PropertyGroup.UserName
if($site -ne 'site91012'){throw 'Unexpected hosting site.'}
$endpoint="https://${server}:8172/msdeploy.axd?site=$site"
function Upload-Config([string]$local,[string]$remote){
 $arguments=@('-verb:sync',"-source:contentPath='$local'","-dest:contentPath='$site/$remote',computerName='$endpoint',userName='$username',password='$($secret.Replace("'","''"))',authType='Basic'")
 $p=[Diagnostics.Process]::new();$p.StartInfo.FileName='C:/Program Files/IIS/Microsoft Web Deploy V3/msdeploy.exe';$p.StartInfo.Arguments=$arguments -join ' ';$p.StartInfo.UseShellExecute=$false;$p.StartInfo.CreateNoWindow=$true;$p.StartInfo.RedirectStandardOutput=$true;$p.StartInfo.RedirectStandardError=$true
 [void]$p.Start();$stdout=$p.StandardOutput.ReadToEndAsync();$stderr=$p.StandardError.ReadToEndAsync();$p.WaitForExit();$output=$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult();$code=$p.ExitCode;$p.Dispose()
 if($code -ne 0){throw ('Hosted configuration upload failed: '+$output.Replace($secret,'[redacted]'))}
}
$changed=$false
try{
 Upload-Config $candidatePath 'appsettings.json';$changed=$true
 Upload-Config $webPath 'web.config'
 & (Join-Path $workspace 'tmp/read-chefz-hosted-config-20261015.ps1')
 $actual=Get-Content -LiteralPath $oldPath -Raw|ConvertFrom-Json
 if($actual.SensitiveData.EncryptionKey -cne $key.Trim()){throw 'Hosted key verification failed.'}
 Write-Output 'CONFIGURED AND RE-READ VERIFIED: initial credential encryption key; API recycle requested via web.config change.'
}catch{
 if($changed){Upload-Config (Join-Path $private 'appsettings.before-key.json') 'appsettings.json';Upload-Config (Join-Path $private 'web.before-key.config') 'web.config'}
 throw
}finally{$secret=$null;$key=$null}
