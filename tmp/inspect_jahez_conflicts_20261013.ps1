$ErrorActionPreference='Stop'
$plan=Get-Content tmp/chefz-assignment-plan-20261013.json -Raw | ConvertFrom-Json
$db=Get-Content tmp/chefz-assignment-db-20261013.json -Raw | ConvertFrom-Json
$rows=@($plan.rows | Where-Object {$_.finalIssues -contains 'Sheet date conflicts with Jahez assignment or posted commission'})
$hs=@(foreach($r in $rows){$db.handovers | Where-Object RiderClientAssignmentId -eq $r.otherAssignments[0].Id | Select-Object -ExpandProperty Id})
$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
SELECT t.name TableName,col.name ColumnName,rt.name ReferencedTable FROM sys.foreign_key_columns f JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.columns col ON col.object_id=t.object_id AND col.column_id=f.parent_column_id JOIN sys.tables rt ON rt.object_id=f.referenced_object_id WHERE f.referenced_object_id IN(OBJECT_ID('jahez.JahezAccountHandover'),OBJECT_ID('app.RiderClientAssignments'));
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='jahez';
'@
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d);$d.Tables[0] | Format-Table
 $counts=@()
 foreach($row in $d.Tables[0].Rows){
  if($row.ReferencedTable -ne 'JahezAccountHandover'){continue}
  $cmd=$c.CreateCommand();$cmd.CommandText="SELECT COUNT(*) N FROM jahez.[$($row.TableName)] WHERE [$($row.ColumnName)] IN(SELECT CONVERT(uniqueidentifier,value) FROM OPENJSON(@ids))"
  [void]$cmd.Parameters.Add('@ids',[System.Data.SqlDbType]::NVarChar,-1);$cmd.Parameters['@ids'].Value=ConvertTo-Json -InputObject $hs -Compress
  $counts += [pscustomobject]@{Table=$row.TableName;Count=$cmd.ExecuteScalar()}
 }
 $counts | Format-Table
 $counts | ConvertTo-Json | Set-Content tmp/jahez-conflict-dependencies-20261013.json
}finally{$c.Dispose()}
