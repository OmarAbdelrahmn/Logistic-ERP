$ErrorActionPreference='Stop'
$base='outputs/platform-account-live-20261007-backup'
New-Item -ItemType Directory -Force -Path $base | Out-Null
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
  $conn.Open()
  if($conn.Database -ne 'db67927'){throw 'Unexpected database'}
  $names=@('PlatformRiderAccounts','RiderClientAssignments','RiderAssignmentEvents','PlatformAccountRegistrations','PlatformAccountCredentialVersions','PlatformRiderAccountTags','VehiclePlatformAccountAssignments','VehiclePlatformAccountSwitches')
  $tables=@($names | ForEach-Object {"app.$_"})
  $q=$conn.CreateCommand();$q.CommandText="SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez') ORDER BY name"
  $reader=$q.ExecuteReader();while($reader.Read()){$tables+="jahez.$($reader.GetString(0))"};$reader.Dispose()
  foreach($table in $tables){
    $parts=$table.Split('.');$schema=$parts[0];$name=$parts[1]
    $cmd=$conn.CreateCommand();$cmd.CommandTimeout=120
    $cmd.CommandText="SELECT * FROM [$schema].[$name] FOR JSON PATH, INCLUDE_NULL_VALUES"
    $reader=$cmd.ExecuteReader();$builder=[System.Text.StringBuilder]::new()
    while($reader.Read()){[void]$builder.Append($reader.GetString(0))};$reader.Dispose()
    $path=Join-Path $base "$table.json"
    [System.IO.File]::WriteAllText((Join-Path (Get-Location).Path $path),$builder.ToString(),[System.Text.UTF8Encoding]::new($false))
    $parsed=ConvertFrom-Json $builder.ToString()
    Write-Output ("{0}: {1} rows" -f $table,@($parsed).Count)
  }
} finally {$conn.Dispose()}
