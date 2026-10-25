param([string]$SqlFile)
$ErrorActionPreference='Stop'
$cfg=Get-Content (Join-Path $PSScriptRoot '../src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$cs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$cfg.ConnectionStrings.LogisticsDatabase}
$cn=[System.Data.SqlClient.SqlConnection]::new($cs)
try {
 $cn.Open()
 if($cn.Database -ne 'db67927' -or $cn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected target'}
 $cmd=$cn.CreateCommand();$cmd.CommandTimeout=45;$cmd.CommandText=Get-Content -LiteralPath $SqlFile -Raw
 $ds=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);[void]$ad.Fill($ds)
 for($i=0;$i -lt $ds.Tables.Count;$i++){
  Write-Output "Table $i"
  $ds.Tables[$i] | Select-Object * -ExcludeProperty RowError,RowState,Table,ItemArray,HasErrors | ConvertTo-Json -Depth 3 -Compress
 }
}finally{$cn.Dispose()}
