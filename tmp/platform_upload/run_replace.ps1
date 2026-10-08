param([switch]$Commit)
$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw|ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
  $conn.Open()
  if($conn.Database -ne 'db67927'){throw 'Unexpected database'}
  $cmd=$conn.CreateCommand();$cmd.CommandText=@'
CREATE TABLE #Accounts(Platform nvarchar(20),ExternalId nvarchar(150),OwnerIqama nvarchar(20),Vehicle nvarchar(100),City nvarchar(100),Sponsor nvarchar(200),SourceStatus nvarchar(100),SheetRow int);
CREATE TABLE #OwnerNames(Iqama nvarchar(20),FullNameAr nvarchar(200));
CREATE TABLE #ExpectedOldAccounts(Id uniqueidentifier NOT NULL PRIMARY KEY);
'@
  [void]$cmd.ExecuteNonQuery()
  $data=[System.Data.DataTable]::new()
  foreach($name in @('Platform','ExternalId','OwnerIqama','Vehicle','City','Sponsor','SourceStatus')){[void]$data.Columns.Add($name,[string])}
  [void]$data.Columns.Add('SheetRow',[int])
  foreach($r in (Import-Csv 'tmp/platform_upload/accounts.csv' -Encoding UTF8)){
    [void]$data.Rows.Add($r.platform,$r.external_id,$r.owner_iqama,$r.vehicle,$r.city,$r.sponsor,$r.status,[int]$r.sheet_row)
  }
  $names=[System.Data.DataTable]::new();[void]$names.Columns.Add('Iqama',[string]);[void]$names.Columns.Add('FullNameAr',[string])
  foreach($r in (Import-Csv 'tmp/platform_upload/owner_names.csv' -Encoding UTF8)){[void]$names.Rows.Add($r.owner_iqama,$r.owner_name)}
  $old=[System.Data.DataTable]::new();[void]$old.Columns.Add('Id',[guid])
  foreach($r in (Get-Content 'outputs/platform-account-live-20261007-backup/app.PlatformRiderAccounts.json' -Raw | ConvertFrom-Json)){
    [void]$old.Rows.Add([guid]$r.Id)
  }
  foreach($item in @(@('#Accounts',$data),@('#OwnerNames',$names),@('#ExpectedOldAccounts',$old))){
    $bulk=[System.Data.SqlClient.SqlBulkCopy]::new($conn)
    try{$bulk.DestinationTableName=$item[0];$bulk.BatchSize=1000;$bulk.WriteToServer($item[1])}finally{$bulk.Dispose()}
  }
  $cmd=$conn.CreateCommand();$cmd.CommandText=Get-Content 'tmp/platform_upload/replace.sql' -Raw;$cmd.CommandTimeout=300
  [void]$cmd.Parameters.Add('@DryRun',[System.Data.SqlDbType]::Bit)
  $cmd.Parameters['@DryRun'].Value=if($Commit){0}else{1}
  $adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);$ds=[System.Data.DataSet]::new();[void]$adapter.Fill($ds)
  foreach($table in $ds.Tables){$table|Format-Table -AutoSize|Out-String|Write-Output}
  if($Commit){Write-Output 'COMMITTED live account replacement'}else{Write-Output 'DRY RUN rolled back all changes'}
} finally {$conn.Dispose()}
