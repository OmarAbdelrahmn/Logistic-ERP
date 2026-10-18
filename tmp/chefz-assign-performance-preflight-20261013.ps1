$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $conn.Open(); $q=$conn.CreateCommand();$q.CommandTimeout=90
 $q.CommandText=@'
SELECT 'DailyPerformance' Kind,COUNT(*) N FROM chefz.DailyPerformance UNION ALL SELECT 'PerformanceImports',COUNT(*) FROM chefz.PerformanceImports UNION ALL SELECT 'WalletSettlements',COUNT(*) FROM chefz.WalletSettlements;
SELECT f.name FKName,OBJECT_SCHEMA_NAME(f.parent_object_id) SourceSchema,OBJECT_NAME(f.parent_object_id) SourceTable,pc.name SourceColumn,OBJECT_SCHEMA_NAME(f.referenced_object_id) TargetSchema,OBJECT_NAME(f.referenced_object_id) TargetTable FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON f.object_id=fc.constraint_object_id JOIN sys.columns pc ON pc.object_id=fc.parent_object_id AND pc.column_id=fc.parent_column_id WHERE f.referenced_object_id IN(OBJECT_ID('chefz.DailyPerformance'),OBJECT_ID('chefz.PerformanceImports'),OBJECT_ID('app.RiderClientAssignments'));
SELECT * FROM app.RiderClientAssignments WHERE PlatformRiderAccountId='b25a9fa4-7a01-417e-a596-8984be3a7e8a';
SELECT * FROM app.RiderAssignmentEvents WHERE RiderClientAssignmentId IN('c124ad30-172f-4160-96be-9b70ae01bc52','01a135c2-b4f4-79c4-8f04-e36bba44a366');
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='chefz' AND TABLE_NAME IN('OrderSettlements','DailyPerformance','PerformanceImports');
SELECT Id,UserName FROM [identity].Users WHERE UserName='Omar' AND IsDeleted=0;
SELECT * FROM app.DatasetVersions WHERE ModuleKey IN('chefz','platform-operations');
'@
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $out=@{};$names=@('counts','fks','assignments','events','columns','actors','versions')
 for($i=0;$i -lt $names.Count;$i++){$out[$names[$i]]=@(foreach($row in $d.Tables[$i].Rows){$o=[ordered]@{};foreach($c in $d.Tables[$i].Columns){$v=$row[$c.ColumnName];$o[$c.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [DateTime] -or $v -is [DateTimeOffset]){$v.ToString('o')}else{$v}};[pscustomobject]$o})}
 $out | ConvertTo-Json -Depth 10 | Set-Content 'tmp/chefz-assignment-performance-preflight-20261013.json' -Encoding utf8
 $out.counts | Format-Table; $out.fks | Format-Table; $out.assignments | Select-Object Id,RiderProfileId,EffectiveFrom,EffectiveTo,Status,EndReason,StartReason | Format-List
}finally{$conn.Dispose()}
