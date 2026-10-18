param([Parameter(Mandatory=$true)][string]$VerificationXml)
$ErrorActionPreference='Stop'
$master=[Convert]::FromBase64String((Get-Content 'artifacts/chefz-import-20261015/encryption-key.txt' -Raw).Trim())
$context=[Text.Encoding]::UTF8.GetBytes('LogisticsERP.PlatformCredential.v1')
$derived=[Security.Cryptography.HMACSHA256]::HashData($master,$context)
$aes=[Security.Cryptography.AesGcm]::new($derived,16)
try{
 $data=[xml](Get-Content -LiteralPath $VerificationXml -Raw)
 $plan=Get-Content 'tmp/chefz-plan-20261015.json' -Raw|ConvertFrom-Json
 $source=Get-Content 'artifacts/chefz-import-20261015/source.json' -Raw|ConvertFrom-Json
 $checked=0
 foreach($row in @($data.NewDataSet.EncryptedCredentials)){
  if(!$row){continue}
  $p=@($plan.passwordRows|Where-Object accountId -eq $row.PlatformRiderAccountId)
  if($p.Count -ne 1){throw 'Saved credential has no unique source account.'}
  $s=@($source.sheets[0].rows|Where-Object row -eq $p[0].row)
  $cipher=[Convert]::FromBase64String($row.Ciphertext);$nonce=[Convert]::FromBase64String($row.Nonce);$tag=[Convert]::FromBase64String($row.AuthenticationTag)
  $plain=[byte[]]::new($cipher.Length)
  $aes.Decrypt($nonce,$cipher,$tag,$plain)
  $expected=[Text.Encoding]::UTF8.GetBytes(([string]$s[0].cells.I).Trim())
  try{if(![Security.Cryptography.CryptographicOperations]::FixedTimeEquals($plain,$expected)){throw 'Decrypted credential differs from source.'}}finally{[Security.Cryptography.CryptographicOperations]::ZeroMemory($plain);[Security.Cryptography.CryptographicOperations]::ZeroMemory($expected)}
  $checked++
 }
 if($checked -ne 233){throw ('Unexpected decrypted credential count: '+$checked)}
 Write-Output ('VERIFIED: '+$checked+' saved passwords decrypt correctly using the API-compatible .NET protector and match the source. No secrets printed.')
}finally{$aes.Dispose();[Security.Cryptography.CryptographicOperations]::ZeroMemory($master);[Security.Cryptography.CryptographicOperations]::ZeroMemory($derived)}
