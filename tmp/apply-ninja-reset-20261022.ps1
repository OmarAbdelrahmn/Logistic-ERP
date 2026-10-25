param([switch]$Apply)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$base=Join-Path $root 'outputs/ninja-reset-20261022'
$planJson=Get-Content (Join-Path $base 'plan.json') -Raw
$plan=$planJson|ConvertFrom-Json
if(-not $plan.ready){throw 'Plan is not ready.'}
if((Get-FileHash -LiteralPath $plan.sourcePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $plan.sourceSha256){throw 'Workbook changed.'}
$cfg=Get-Content (Join-Path $root 'src/LogisticsERP.Api/appsettings.json') -Raw|ConvertFrom-Json
$conn=[System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.LogisticsDatabase);$tx=$null
function Read-Data([string]$sql){
 $q=$conn.CreateCommand();$q.Transaction=$tx;$q.CommandTimeout=120;$q.CommandText=$sql
 [void]$q.Parameters.Add('@PlanJson',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@PlanJson'].Value=$planJson
 try{$d=[System.Data.DataSet]::new();$ad=[System.Data.SqlClient.SqlDataAdapter]::new($q);try{[void]$ad.Fill($d)}finally{$ad.Dispose()};return ,$d}finally{$q.Dispose()}
}
$before=@'
SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
DECLARE @platform uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
SELECT * INTO #BeforeAccounts FROM app.PlatformRiderAccounts WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeAssignments FROM app.RiderClientAssignments WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeRegistrations FROM app.PlatformAccountRegistrations WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeEvents FROM app.RiderAssignmentEvents WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeUsages FROM app.PlatformAccountDayUsages WITH(UPDLOCK,HOLDLOCK);
SELECT * INTO #BeforeEmployees FROM app.Employees WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM #BeforeAccounts WHERE ClientPlatformId=@platform;
SELECT * FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform) OR ClientContractId IN(SELECT Id FROM app.ClientContracts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeRegistrations WHERE ClientPlatformId=@platform OR PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeEvents WHERE RiderClientAssignmentId IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform));
SELECT * FROM #BeforeUsages WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM #BeforeEmployees WHERE Id IN(SELECT OwnerId FROM OPENJSON(@PlanJson,'$.rows') WITH(OwnerId uniqueidentifier '$.ownerId'));
SELECT * FROM ninja.NinjaAccountDetails WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.VehiclePlatformAccountAssignments WITH(UPDLOCK,HOLDLOCK) WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM app.VehiclePlatformAccountSwitches WITH(UPDLOCK,HOLDLOCK) WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform);
SELECT * FROM app.DatasetVersions WITH(UPDLOCK,HOLDLOCK) WHERE ModuleKey IN('ninja','platform-operations','employees','workforce');
SELECT * FROM platform.GlobalCities WITH(UPDLOCK,HOLDLOCK);
SELECT * FROM app.OperatingCities WITH(UPDLOCK,HOLDLOCK);
'@
$protected=@'
DECLARE @platform2 uniqueidentifier=JSON_VALUE(@PlanJson,'$.platformId');
IF EXISTS(SELECT * FROM #BeforeAccounts WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM app.PlatformRiderAccounts WHERE ClientPlatformId<>@platform2) OR EXISTS(SELECT * FROM app.PlatformRiderAccounts WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM #BeforeAccounts WHERE ClientPlatformId<>@platform2) THROW 51000,'Other platform accounts changed.',1;
IF EXISTS(SELECT * FROM #BeforeAssignments WHERE PlatformRiderAccountId NOT IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2) EXCEPT SELECT * FROM app.RiderClientAssignments) OR EXISTS(SELECT * FROM app.RiderClientAssignments EXCEPT SELECT * FROM #BeforeAssignments) THROW 51000,'Other platform assignments changed.',1;
IF EXISTS(SELECT * FROM #BeforeRegistrations WHERE ClientPlatformId<>@platform2 EXCEPT SELECT * FROM app.PlatformAccountRegistrations) OR EXISTS(SELECT * FROM app.PlatformAccountRegistrations EXCEPT SELECT * FROM #BeforeRegistrations) THROW 51000,'Other platform registrations changed.',1;
IF EXISTS(SELECT * FROM #BeforeEvents WHERE RiderClientAssignmentId NOT IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2)) EXCEPT SELECT * FROM app.RiderAssignmentEvents) OR EXISTS(SELECT * FROM app.RiderAssignmentEvents EXCEPT SELECT * FROM #BeforeEvents) THROW 51000,'Other platform events changed.',1;
IF EXISTS(SELECT * FROM #BeforeUsages WHERE PlatformRiderAccountId NOT IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2) EXCEPT SELECT * FROM app.PlatformAccountDayUsages) OR EXISTS(SELECT * FROM app.PlatformAccountDayUsages EXCEPT SELECT * FROM #BeforeUsages) THROW 51000,'Other platform day allocations changed.',1;
IF EXISTS(SELECT * FROM #BeforeEmployees WHERE Id NOT IN(SELECT OwnerId FROM OPENJSON(@PlanJson,'$.rows') WITH(OwnerId uniqueidentifier '$.ownerId')) EXCEPT SELECT * FROM app.Employees) THROW 51000,'Unrelated employees changed.',1;
IF EXISTS(SELECT 1 FROM app.RiderAssignmentEvents WHERE RiderClientAssignmentId IN(SELECT Id FROM #BeforeAssignments WHERE PlatformRiderAccountId IN(SELECT Id FROM #BeforeAccounts WHERE ClientPlatformId=@platform2))) THROW 51000,'Ninja assignment history remains.',1;
'@
try{
 $conn.Open();if($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net'){throw 'Unexpected database target.'}
 $dir=Join-Path $base ($(if($Apply){'applied-'}else{'rehearsal-'})+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'));New-Item -ItemType Directory -Path $dir|Out-Null
 Copy-Item (Join-Path $base 'plan.json') $dir
 $tx=$conn.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $sql=$before+[Environment]::NewLine+(Get-Content (Join-Path $base 'sync.sql') -Raw)+[Environment]::NewLine+$protected+[Environment]::NewLine+(Get-Content (Join-Path $base 'verify.sql') -Raw)
 $sql | Set-Content (Join-Path $dir 'executed.sql') -Encoding utf8
 $result=Read-Data $sql
 $names=@('Accounts','Assignments','Registrations','AssignmentEvents','DayUsages','Owners','AccountDetails','VehicleAssignments','VehicleSwitches','DatasetVersions','GlobalCities','OperatingCities','Verification')
 if($result.Tables.Count -ne $names.Count){throw 'Unexpected result tables.'}
 for($i=0;$i -lt $names.Count;$i++){$result.Tables[$i].TableName=$names[$i]}
 $result.WriteXml((Join-Path $dir 'before-and-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 if(@($result.Tables['Verification'].Rows|Where-Object Mismatches -ne 0).Count){throw 'Verification failed.'}
 if($Apply){$tx.Commit();$tx.Dispose();$tx=$null;$conn.Close();$conn.Open();$post=Read-Data (Get-Content (Join-Path $base 'verify.sql') -Raw);$post.WriteXml((Join-Path $dir 'postcommit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema);if(@($post.Tables[0].Rows|Where-Object Mismatches -ne 0).Count){throw 'Postcommit check failed. Do not rerun.'};'COMMITTED AND VERIFIED ON FRESH CONNECTION'}else{$tx.Rollback();$tx.Dispose();$tx=$null;'REHEARSED, VERIFIED AND ROLLED BACK'}
 $result.Tables['Verification']|Format-Table -AutoSize
 'Output: '+$dir
}catch{if($tx){try{$tx.Rollback()}catch{}};throw}finally{if($tx){$tx.Dispose()};$conn.Dispose()}
