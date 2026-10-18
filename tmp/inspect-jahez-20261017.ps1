$ErrorActionPreference='Stop'
$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw|ConvertFrom-Json
$cs=if($env:ConnectionStrings__LogisticsDatabase){$env:ConnectionStrings__LogisticsDatabase}else{$cfg.ConnectionStrings.LogisticsDatabase}
$c=[System.Data.SqlClient.SqlConnection]::new($cs)
try{
 $c.Open()
 if($c.Database -ne 'db67927' -or $c.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target'}
 $q=$c.CreateCommand();$q.CommandTimeout=120
 $q.CommandText=@'
SELECT (SELECT s.name [schema],t.name [table],c.name [column],TYPE_NAME(c.user_type_id) [type],c.max_length,c.is_nullable,c.is_identity,dc.definition [default]
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id LEFT JOIN sys.default_constraints dc ON c.default_object_id=dc.object_id
WHERE t.name IN('Employees','RiderProfiles','Sponsors','PlatformRiderAccounts','RiderClientAssignments','ClientContracts','ClientPlatforms','Vehicles','VehiclePlatformAccountAssignments','AccountSessions','JahezAccountSession','RiderAssignmentEvents','AuditEntries','DatasetVersions') OR s.name='jahez_v2'
ORDER BY s.name,t.name,c.column_id FOR JSON PATH) Json;
SELECT (SELECT * FROM app.PlatformRiderAccounts FOR JSON PATH) Json;
SELECT (SELECT Id,IqamaNo,FullNameAr,IsEmployee,Status,SponsorId,IsDeleted,OperatingCityId FROM app.Employees FOR JSON PATH) Json;
SELECT (SELECT * FROM app.RiderProfiles FOR JSON PATH) Json;
SELECT (SELECT * FROM app.Sponsors FOR JSON PATH) Json;
SELECT (SELECT * FROM platform.ClientPlatforms FOR JSON PATH) Json;
SELECT (SELECT * FROM app.ClientContracts FOR JSON PATH) Json;
SELECT (SELECT * FROM app.Vehicles FOR JSON PATH) Json;
SELECT (SELECT * FROM app.RiderClientAssignments FOR JSON PATH) Json;
SELECT (SELECT * FROM app.VehiclePlatformAccountAssignments FOR JSON PATH) Json;
SELECT (SELECT s.name [schema],t.name [table] FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='jahez_v2' FOR JSON PATH) Json;
'@
 $d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d)
 $names=@('schema','accounts','employees','riders','sponsors','platforms','contracts','vehicles','assignments','vehicleAssignments','v2tables')
 $out='tmp/jahez-db-20261017';[void](New-Item -ItemType Directory $out -Force)
 for($i=0;$i -lt $names.Count;$i++){$d.Tables[$i].Rows[0].Json|Set-Content (Join-Path $out ($names[$i]+'.json')) -Encoding utf8}
 Write-Output ('Snapshot saved: '+$out)
}finally{$c.Dispose()}
