param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$ninjaWorkspace = Split-Path -Parent $PSScriptRoot
$ninjaConfig = Get-Content -LiteralPath (Join-Path $ninjaWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$ninjaConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $ninjaConfig.ConnectionStrings.LogisticsDatabase }
$ninjaConnection = [System.Data.SqlClient.SqlConnection]::new($ninjaConnectionText)
$ninjaTransaction = $null

function Read-NinjaPermissionData([string]$Sql) {
    $ninjaCommand = $ninjaConnection.CreateCommand()
    $ninjaCommand.CommandTimeout = 60
    $ninjaCommand.Transaction = $ninjaTransaction
    $ninjaCommand.CommandText = $Sql
    try {
        $ninjaData = [System.Data.DataSet]::new()
        $ninjaAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($ninjaCommand)
        try { [void]$ninjaAdapter.Fill($ninjaData) } finally { $ninjaAdapter.Dispose() }
        return ,$ninjaData
    } finally { $ninjaCommand.Dispose() }
}

$ninjaPreflightSql = @'
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017095007_AddNinjaPermissionCatalog';
SELECT MigrationId,ProductVersion FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261017095013_GrantNinjaPermissions';
SELECT * FROM platform.PermissionDefinitions WHERE [Key] LIKE N'ninja.%';
SELECT * FROM [identity].RolePermissions WHERE PermissionKey LIKE N'ninja.%';
SELECT u.UserName,r.Name AS RoleName,a.IsAllClientScope,a.StartsAtUtc,a.ExpiresAtUtc
FROM [identity].Users u JOIN [identity].UserRoleAssignments a ON a.UserId=u.Id JOIN [identity].Roles r ON r.Id=a.RoleId
WHERE u.NormalizedUserName=N'OMAR' AND u.IsDeleted=0 AND a.IsDeleted=0 AND r.IsDeleted=0;
IF NOT EXISTS (SELECT 1 FROM [identity].Roles WHERE Id='019c18d5-62e1-7000-9000-000000000001' AND IsDeleted=0 AND Status=2)
    THROW 51000, 'The expected active system administrator role is missing.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017095007_AddNinjaPermissionCatalog')
 AND EXISTS (SELECT 1 FROM platform.PermissionDefinitions WHERE [Key] LIKE N'ninja.%' OR Id BETWEEN '019c18d5-62e1-7000-a000-000000000310' AND '019c18d5-62e1-7000-a000-000000000316')
    THROW 51000, 'Partial or conflicting Ninja catalog seed detected.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261017095013_GrantNinjaPermissions')
 AND EXISTS (SELECT 1 FROM [identity].RolePermissions WHERE Id BETWEEN '019c18d5-62e1-7000-b000-000000000265' AND '019c18d5-62e1-7000-b000-000000000271'
 OR (RoleId='019c18d5-62e1-7000-9000-000000000001' AND PermissionKey LIKE N'ninja.%' AND IsDeleted=0))
    THROW 51000, 'Partial or conflicting Ninja administrator grants detected.', 1;
'@

$ninjaVerificationSql = @'
DECLARE @now datetimeoffset=SYSUTCDATETIME();
DECLARE @platform uniqueidentifier=(SELECT Id FROM platform.ClientPlatforms WHERE Code=N'NINJA' AND IsDeleted=0);
DECLARE @keys TABLE (PermissionKey nvarchar(150) PRIMARY KEY);
INSERT INTO @keys VALUES(N'ninja.read'),(N'ninja.accounts.update'),(N'ninja.assignments.create'),(N'ninja.assignments.delete'),
(N'ninja.credentials.rotate'),(N'ninja.performance.import'),(N'ninja.deductions.import');
IF @platform IS NULL THROW 51000, 'Ninja platform is missing.', 1;
IF EXISTS (SELECT PermissionKey FROM @keys EXCEPT SELECT [Key] FROM platform.PermissionDefinitions WHERE IsDeleted=0 AND IsDeprecated=0 AND RequiresClientScope=1)
    THROW 51000, 'Ninja permission definitions were not verified.', 1;
IF EXISTS (SELECT PermissionKey FROM @keys EXCEPT SELECT PermissionKey FROM [identity].RolePermissions WHERE RoleId='019c18d5-62e1-7000-9000-000000000001' AND IsDeleted=0)
    THROW 51000, 'Ninja administrator permissions were not verified.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017095007_AddNinjaPermissionCatalog')
 OR NOT EXISTS (SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261017095013_GrantNinjaPermissions')
    THROW 51000, 'Ninja permission migration history was not verified.', 1;
IF NOT EXISTS (SELECT 1 FROM [identity].Users u JOIN [identity].UserRoleAssignments a ON a.UserId=u.Id
 JOIN [identity].Roles r ON r.Id=a.RoleId WHERE u.NormalizedUserName=N'OMAR' AND u.IsDeleted=0 AND u.Status=2
 AND u.RequiresPasswordChange=0 AND a.IsDeleted=0 AND a.StartsAtUtc<=@now AND (a.ExpiresAtUtc IS NULL OR a.ExpiresAtUtc>@now)
 AND a.IsAllClientScope=1 AND r.Id='019c18d5-62e1-7000-9000-000000000001' AND r.IsDeleted=0 AND r.Status=2)
    THROW 51000, 'Omar active administrator scope was not verified.', 1;
IF EXISTS (SELECT 1 FROM [identity].UserDirectPermissionAssignments d JOIN [identity].Users u ON u.Id=d.UserId
 JOIN @keys k ON k.PermissionKey=d.PermissionKey WHERE u.NormalizedUserName=N'OMAR' AND d.IsDeleted=0 AND d.Effect=2
 AND d.StartsAtUtc<=@now AND (d.ExpiresAtUtc IS NULL OR d.ExpiresAtUtc>@now)
 AND (d.IsAllClientScope=1 OR NOT EXISTS(SELECT 1 FROM [identity].AccessScopes s WHERE s.DirectPermissionAssignmentId=d.Id AND s.IsDeleted=0)
 OR EXISTS(SELECT 1 FROM [identity].AccessScopes s WHERE s.DirectPermissionAssignmentId=d.Id AND s.IsDeleted=0 AND s.ScopeType=2 AND s.TargetId=@platform)))
    THROW 51000, 'Omar has an applicable explicit Ninja permission denial.', 1;
SELECT p.[Key],p.RequiresClientScope,p.IsDeprecated FROM platform.PermissionDefinitions p JOIN @keys k ON k.PermissionKey=p.[Key] WHERE p.IsDeleted=0 ORDER BY p.[Key];
SELECT r.Name AS RoleName,p.PermissionKey FROM [identity].RolePermissions p JOIN [identity].Roles r ON r.Id=p.RoleId
 JOIN @keys k ON k.PermissionKey=p.PermissionKey WHERE r.Id='019c18d5-62e1-7000-9000-000000000001' AND p.IsDeleted=0 ORDER BY p.PermissionKey;
SELECT N'Omar' AS UserName,CAST(1 AS bit) AS ActiveAllClientAdministrator,CAST(0 AS bit) AS ApplicableNinjaDeny,
 (SELECT COUNT(*) FROM @keys) AS VerifiedNinjaPermissions,SYSUTCDATETIME() AS VerifiedAtUtc;
'@

try {
    $ninjaConnection.Open()
    if ($ninjaConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $ninjaConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted database target.'
    }
    $ninjaBefore = Read-NinjaPermissionData $ninjaPreflightSql
    Write-Output ('Verified target: ' + $ninjaConnection.DataSource + ' / ' + $ninjaConnection.Database)
    Write-Output ('Before: {0} Ninja definitions, {1} Ninja role grants.' -f $ninjaBefore.Tables[2].Rows.Count,$ninjaBefore.Tables[3].Rows.Count)
    if (!$Apply) { Write-Output 'Read-only preflight completed. Use -Apply to apply both reviewed migrations.'; return }

    $ninjaOutput = Join-Path $ninjaWorkspace ('outputs/ninja-permissions-migration-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
    [void](New-Item -ItemType Directory -Path $ninjaOutput)
    $ninjaBefore.WriteXml((Join-Path $ninjaOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaScripts = @('ninja-permissions-application.sql','ninja-permissions-identity.sql')
    $ninjaBodies = @()
    $ninjaHashes = [ordered]@{}
    foreach ($ninjaFile in $ninjaScripts) {
        $ninjaPath = Join-Path $ninjaWorkspace ('database/scripts/' + $ninjaFile)
        $ninjaSql = Get-Content -LiteralPath $ninjaPath -Raw
        if (([regex]::Matches($ninjaSql,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
            ([regex]::Matches($ninjaSql,'(?im)^COMMIT;')).Count -ne 1 -or $ninjaSql -match '(?im)^\s*(DELETE|UPDATE|DROP|ALTER|TRUNCATE)\b') {
            throw 'Unexpected migration script structure.'
        }
        $ninjaBodies += $ninjaSql -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
        Copy-Item -LiteralPath $ninjaPath -Destination (Join-Path $ninjaOutput $ninjaFile)
        $ninjaHashes[$ninjaFile] = (Get-FileHash -LiteralPath $ninjaPath -Algorithm SHA256).Hash
    }
    $ninjaTransaction = $ninjaConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-NinjaPermissionData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'ninja-permissions-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000, 'Could not acquire Ninja permission migration lock.', 1;
'@)
    [void](Read-NinjaPermissionData $ninjaPreflightSql)
    foreach ($ninjaBody in $ninjaBodies) { [void](Read-NinjaPermissionData $ninjaBody) }
    $ninjaVerification = Read-NinjaPermissionData $ninjaVerificationSql
    $ninjaTransaction.Commit()
    $ninjaTransaction.Dispose(); $ninjaTransaction = $null
    $ninjaVerification.WriteXml((Join-Path $ninjaOutput 'verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)

    $ninjaConnection.Close(); $ninjaConnection.Open()
    $ninjaPostCommit = Read-NinjaPermissionData $ninjaVerificationSql
    $ninjaPostCommit.WriteXml((Join-Path $ninjaOutput 'post-commit-verification.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $ninjaReport = [ordered]@{
        Server=$ninjaConnection.DataSource; Database=$ninjaConnection.Database; VerifiedAtUtc=[DateTime]::UtcNow.ToString('o')
        NinjaDefinitions=$ninjaPostCommit.Tables[0].Rows.Count; AdministratorNinjaGrants=$ninjaPostCommit.Tables[1].Rows.Count
        OmarAllSevenPermissionsVerified=$true; PostCommitVerified=$true; ScriptSha256=$ninjaHashes; EvidenceDirectory=$ninjaOutput
    }
    $ninjaReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $ninjaOutput 'verification.json') -Encoding UTF8
    $ninjaReport | ConvertTo-Json -Depth 5
} catch {
    if ($ninjaTransaction) { try { $ninjaTransaction.Rollback() } catch { } }
    throw
} finally {
    if ($ninjaTransaction) { $ninjaTransaction.Dispose() }
    $ninjaConnection.Dispose()
}
