$ErrorActionPreference='Stop'
$p=Get-Content tmp/jahez-plan-20261017.json -Raw|ConvertFrom-Json
$cfg=Get-Content src/LogisticsERP.Api/appsettings.json -Raw|ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandTimeout=60
 $q.CommandText=@'
SELECT p.Id,p.EmployeeId,p.Iqama,e.Id FoundEmployeeId,e.IqamaNo,e.IsEmployee,e.IsDeleted EmployeeDeleted,r.Id ExistingProfile,r.IsDeleted ProfileDeleted
FROM OPENJSON(@Json) WITH(Id uniqueidentifier '$.id',EmployeeId uniqueidentifier '$.employeeId',Iqama varchar(10) '$.iqama') p
LEFT JOIN app.Employees e ON e.Id=p.EmployeeId LEFT JOIN app.RiderProfiles r ON r.EmployeeId=p.EmployeeId;
SELECT (SELECT * FROM app.RiderProfiles FOR JSON PATH) Json;
'@
 [void]$q.Parameters.Add('@Json',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@Json'].Value=ConvertTo-Json -InputObject @($p.profiles) -Depth 10 -Compress
 $d=[System.Data.DataSet]::new();$a=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$a.Fill($d)
 $d.Tables[0].Rows|ForEach-Object { [pscustomobject]@{Iqama=$_.Iqama;ActualIqama=$_.IqamaNo;IsEmployee=$_.IsEmployee;EmployeeDeleted=$_.EmployeeDeleted;ExistingProfile=[string]$_.ExistingProfile;ProfileDeleted=[string]$_.ProfileDeleted} }|ConvertTo-Json -Compress
 $d.Tables[1].Rows[0].Json|Set-Content 'tmp/jahez-db-20261017/riders.json' -Encoding utf8
}finally{$c.Dispose()}
