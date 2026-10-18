param([Parameter(Mandatory=$true)][string]$SourceJsonPath,[switch]$Apply)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
$source=Get-Content -LiteralPath $SourceJsonPath -Raw
$parsed=$source | ConvertFrom-Json
if ($parsed.accounts.Count -ne 464 -or $parsed.statistics.Count -ne 3) { throw 'Expected the extracted 464-account, three-sponsor workbook.' }
$migration=Get-Content (Join-Path $workspace 'database/scripts/chefz-workbook-reports.sql') -Raw
if ([string]::IsNullOrWhiteSpace($migration) -or $migration -notmatch '20261011191524_AddChefzWorkbookReportsAndStatistics') { throw 'Chefz schema script is missing or empty. Build the infrastructure project and regenerate it before applying.' }
$migration=[regex]::Replace($migration,'(?im)^\s*GO\s*\r?$','')
$migration=$migration -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$',''
$config=Get-Content (Join-Path $workspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$connection=[System.Data.SqlClient.SqlConnection]::new($config.ConnectionStrings.LogisticsDatabase)
$transaction=$null
function Read-Data([string]$Sql) {
 $q=$connection.CreateCommand(); $q.CommandTimeout=90; $q.Transaction=$transaction; $q.CommandText=$Sql
 try { $d=[System.Data.DataSet]::new(); $a=[System.Data.SqlClient.SqlDataAdapter]::new($q); try {[void]$a.Fill($d)} finally {$a.Dispose()}; return ,$d } finally {$q.Dispose()}
}
try {
 $connection.Open()
 if ($connection.Database -ne 'db67927' -or $connection.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database; refusing update.' }
 $preflight=Read-Data @'
SELECT COUNT(*) Accounts FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ' AND a.IsDeleted=0;
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory WHERE MigrationId IN ('20261011122817_AddJahezDebtFreezeAndClearance','20261011191524_AddChefzWorkbookReportsAndStatistics');
'@
 $preflight.Tables | ForEach-Object {$_ | Format-Table -AutoSize}
 if (!$Apply) {Write-Output 'Preflight only. Use -Apply for the authorized Chefz reset and schema update.'; return}
 $stamp=[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
 $output=Join-Path $workspace ('outputs/chefz-workbook-update-'+$stamp)
 [void](New-Item -ItemType Directory -Path $output -Force)
 $transaction=$connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 $backup=Read-Data @'
SELECT a.* FROM app.PlatformRiderAccounts a WITH(UPDLOCK,HOLDLOCK) JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ';
SELECT d.* FROM chefz.AccountDetails d;
SELECT r.* FROM app.RiderClientAssignments r WITH(UPDLOCK,HOLDLOCK) JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ';
SELECT e.* FROM app.RiderAssignmentEvents e JOIN app.RiderClientAssignments r ON r.Id=e.RiderClientAssignmentId JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ';
SELECT * FROM chefz.DailyPerformance;
SELECT * FROM chefz.WalletSettlements;
SELECT * FROM chefz.PerformanceImports;
SELECT * FROM migration.__ApplicationMigrationsHistory;
SELECT a.Id,a.Status,a.SponsorId,a.OperatingCityId,a.ExternalAccountId INTO #OtherAccounts FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ';
SELECT r.* INTO #OtherAssignments FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ';
'@
 $names=@('Accounts','Details','Assignments','AssignmentEvents','DailyPerformance','WalletSettlements','PerformanceImports','Migrations')
 for($i=0;$i -lt $names.Count;$i++) {$backup.Tables[$i].TableName=$names[$i]}
 $backup.WriteXml((Join-Path $output 'before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
 Copy-Item -LiteralPath $SourceJsonPath -Destination (Join-Path $output 'source.json')
 $q=$connection.CreateCommand();$q.Transaction=$transaction;$q.CommandTimeout=90;$q.CommandText=$migration
 try {[void]$q.ExecuteNonQuery()} finally {$q.Dispose()}
 $reset=Get-Content (Join-Path $workspace 'database/scripts/chefz-workbook-reset.sql') -Raw
 $q=$connection.CreateCommand();$q.Transaction=$transaction;$q.CommandTimeout=90;$q.CommandText=$reset
 [void]$q.Parameters.Add('@WorkbookJson',[System.Data.SqlDbType]::NVarChar,-1);$q.Parameters['@WorkbookJson'].Value=$source
 try {$summary=[System.Data.DataTable]::new();$summary.Load($q.ExecuteReader())} finally {$q.Dispose()}
 $verified=Read-Data @'
IF EXISTS (SELECT * FROM #OtherAccounts EXCEPT SELECT a.Id,a.Status,a.SponsorId,a.OperatingCityId,a.ExternalAccountId FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ')
 OR EXISTS(SELECT a.Id,a.Status,a.SponsorId,a.OperatingCityId,a.ExternalAccountId FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ' EXCEPT SELECT * FROM #OtherAccounts)
 THROW 51000,'Other-platform accounts changed.',1;
IF EXISTS(SELECT * FROM #OtherAssignments EXCEPT SELECT r.* FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ')
 OR EXISTS(SELECT r.* FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code<>'SHIFTZ' EXCEPT SELECT * FROM #OtherAssignments)
 THROW 51000,'Other-platform assignments changed.',1;
IF EXISTS(SELECT 1 FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code='SHIFTZ')
 THROW 51000,'Chefz assignments remain.',1;
SELECT AccountType,DashboardStatus,COUNT(*) Accounts FROM chefz.AccountDetails WHERE IsDeleted=0 GROUP BY AccountType,DashboardStatus;
SELECT COUNT(*) Settings FROM chefz.PerformanceSettings;
SELECT COUNT(*) StatisticsRows FROM chefz.SponsorStatistics WHERE IsDeleted=0;
'@
 $transaction.Commit();$transaction=$null
 $summary | Format-Table -AutoSize
 $verified.Tables | ForEach-Object {$_ | Format-Table -AutoSize}
 $summary | Export-Csv (Join-Path $output 'summary.csv') -NoTypeInformation -Encoding utf8
 Write-Output ('Backup and source saved in '+$output)
} catch { if ($transaction) {$transaction.Rollback()}; throw } finally { if ($transaction) {$transaction.Dispose()}; $connection.Dispose() }
