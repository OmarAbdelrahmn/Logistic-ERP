$ErrorActionPreference='Stop'
$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw|ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandTimeout=90
 $q.CommandText=@'
SELECT (SELECT * FROM jahez_v2.AccountSessions FOR JSON PATH) Json;
SELECT (SELECT * FROM jahez_v2.AccountFees FOR JSON PATH) Json;
SELECT (SELECT * FROM jahez_v2.AccountingEntries FOR JSON PATH) Json;
SELECT (SELECT * FROM jahez_v2.Operations FOR JSON PATH) Json;
SELECT (SELECT * FROM jahez_v2.MoneyMovements FOR JSON PATH) Json;
SELECT (SELECT * FROM app.DatasetVersions FOR JSON PATH) Json;
'@
 $d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d)
 $names=@('sessions','fees','ledger','operations','money','versions')
 for($i=0;$i -lt $names.Count;$i++){$d.Tables[$i].Rows[0].Json|Set-Content ('tmp/jahez-db-20261017/'+$names[$i]+'.json') -Encoding utf8;Write-Output ($names[$i]+': '+@($d.Tables[$i].Rows[0].Json|ConvertFrom-Json).Count)}
}finally{$c.Dispose()}

