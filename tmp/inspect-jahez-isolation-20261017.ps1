$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw|ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try{$c.Open();$q=$c.CreateCommand();$q.CommandText='SELECT snapshot_isolation_state_desc,is_read_committed_snapshot_on FROM sys.databases WHERE name=DB_NAME();';$d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d);$d.Tables[0]|Format-Table -AutoSize}finally{$c.Dispose()}
