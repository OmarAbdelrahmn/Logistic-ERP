$ErrorActionPreference='Stop'
$cfg=Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$c=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase)
try {
 $c.Open();$q=$c.CreateCommand();$q.CommandTimeout=90;$q.CommandText=@'
SELECT DB_NAME() DatabaseName;
SELECT Id,Code,NameAr FROM platform.ClientPlatforms WHERE Code='SHIFTZ' AND IsDeleted=0;
SELECT a.*,d.Id DetailsId,d.ReportIdNumber,d.AccountType,d.VehicleType,d.DashboardStatus,e.IqamaNo OwnerIqama
 FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId
 LEFT JOIN chefz.AccountDetails d ON d.PlatformRiderAccountId=a.Id AND d.IsDeleted=0
 LEFT JOIN app.Employees e ON e.Id=a.RegisteredEmployeeId WHERE p.Code='SHIFTZ' AND a.IsDeleted=0;
SELECT e.Id,e.IqamaNo,e.FullNameAr,e.Status,e.IsEmployee,e.IsDeleted,r.Id RiderId,r.IsDeleted RiderDeleted
 FROM app.Employees e LEFT JOIN app.RiderProfiles r ON r.EmployeeId=e.Id WHERE e.IqamaNo IS NOT NULL;
SELECT r.*,p.Code PlatformCode,a.ExternalAccountId FROM app.RiderClientAssignments r
 JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE r.IsDeleted=0;
SELECT * FROM app.ClientContracts WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='SHIFTZ');
SELECT o.Id,g.NameAr FROM app.OperatingCities o JOIN platform.GlobalCities g ON g.Id=o.GlobalCityId;
SELECT * FROM app.Sponsors WHERE IsDeleted=0;
SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE,COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS
 WHERE (TABLE_SCHEMA='app' AND TABLE_NAME IN ('RiderClientAssignments','RiderAssignmentEvents','PlatformRiderAccounts','PlatformAccountCredentialVersions','Sponsors')) OR (TABLE_SCHEMA='chefz' AND TABLE_NAME='AccountDetails');
SELECT Id,UserName FROM [identity].Users WHERE UserName='Omar' AND IsDeleted=0;
SELECT name,definition FROM sys.check_constraints WHERE parent_object_id IN(OBJECT_ID('app.RiderClientAssignments'),OBJECT_ID('app.PlatformRiderAccounts'));
SELECT name,filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID('app.RiderClientAssignments');
SELECT c.PlatformRiderAccountId,MAX(c.KeyVersion) KeyVersion,COUNT(*) Versions FROM app.PlatformAccountCredentialVersions c JOIN app.PlatformRiderAccounts a ON a.Id=c.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ' GROUP BY c.PlatformRiderAccountId;
SELECT 'Performance' Kind,COUNT(*) N FROM chefz.DailyPerformance UNION ALL SELECT 'Settlements',COUNT(*) FROM chefz.WalletSettlements;
SELECT * FROM chefz.DailyPerformance;
SELECT * FROM app.PlatformAccountRegistrations WHERE ClientPlatformId IN(SELECT Id FROM platform.ClientPlatforms WHERE Code='SHIFTZ');
IF OBJECT_ID('jahez.JahezTransferredDebt') IS NOT NULL EXEC('SELECT RiderProfileId FROM jahez.JahezTransferredDebt WHERE PaidAtUtc IS NULL AND IsDeleted=0');
ELSE SELECT RiderProfileId FROM jahez_v2.TransferredDebts WHERE PaidAtUtc IS NULL AND IsDeleted=0;
SELECT s.name SchemaName,t.name TableName,c.name ColumnName FROM sys.foreign_key_columns f JOIN sys.tables t ON t.object_id=f.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id WHERE f.referenced_object_id=OBJECT_ID('app.RiderClientAssignments');
SELECT s.name SchemaName,t.name TableName FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='jahez';
SELECT s.name SchemaName,t.name TableName,c.name ColumnName,ty.name DataType FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE c.name LIKE '%Cipher%' OR c.name LIKE '%Encrypted%' OR c.name LIKE '%Protected%';
SELECT COUNT(*) Credentials FROM app.PlatformAccountCredentialVersions;
SELECT 'HealthCards' Kind,COUNT(*) N FROM app.RiderHealthCards WHERE CardNumberCiphertext IS NOT NULL
UNION ALL SELECT 'DriverLicenses',COUNT(*) FROM app.EmployeeDriverLicenses WHERE LicenseNumberCiphertext IS NOT NULL
UNION ALL SELECT 'InsurancePolicies',COUNT(*) FROM app.EmployeeMedicalInsurancePolicies WHERE PolicyNumberCiphertext IS NOT NULL OR MemberNumberCiphertext IS NOT NULL;
'@
 $d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);[void]$ad.Fill($d)
 $names=@('target','platforms','accounts','employees','assignments','contracts','cities','sponsors','columns','actors','constraints','indexes','credentials','financialCounts','performance','registrations','blockedRiders','assignmentReferences','jahezTables','encryptedColumns','allCredentials','encryptedCounts');$out=@{}
 for($i=0;$i -lt $names.Count;$i++){
 $out[$names[$i]]=@(foreach($r in $d.Tables[$i].Rows){$o=[ordered]@{};foreach($col in $d.Tables[$i].Columns){$v=$r[$col.ColumnName];$o[$col.ColumnName]=if($v -is [DBNull]){$null}elseif($v -is [DateTime]){$v.ToString('yyyy-MM-ddTHH:mm:ss')}elseif($v -is [byte[]]){[Convert]::ToBase64String($v)}else{$v}};[pscustomobject]$o})
 Write-Output ($names[$i]+': '+$out[$names[$i]].Count)
 };$out|ConvertTo-Json -Depth 12|Set-Content 'tmp/chefz-db-20261015.json' -Encoding utf8
}finally{$c.Dispose()}
