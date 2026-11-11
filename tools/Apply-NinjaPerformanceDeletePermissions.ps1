param(
    [ValidateSet('Preflight', 'Rehearse', 'Apply', 'Verify')][string]$Mode = 'Preflight',
    [string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$deletionRoot = Split-Path -Parent $PSScriptRoot
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path $deletionRoot ('outputs/ninja-performance-deletion-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')) }
[void](New-Item -ItemType Directory -Force -Path $EvidenceDirectory)
$deletionConfig = Get-Content -LiteralPath (Join-Path $deletionRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$deletionConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $deletionConfig.ConnectionStrings.LogisticsDatabase }
$deletionConnection = [Data.SqlClient.SqlConnection]::new($deletionConnectionText)
$deletionTransaction = $null
$deletionApplicationId = '20261110101250_AddNinjaPerformanceDeletePermission'
$deletionIdentityId = '20261110101358_GrantNinjaPerformanceDeletePermission'
$deletionFiles = @('ninja-performance-deletion-application.sql', 'ninja-performance-deletion-identity.sql')
$deletionHashes = [ordered]@{}
$deletionBodies = @()
foreach ($deletionFile in $deletionFiles) {
    $deletionFilePath = Join-Path $deletionRoot ('database/scripts/' + $deletionFile)
    $deletionSql = Get-Content -LiteralPath $deletionFilePath -Raw
    $deletionTargets = @([regex]::Matches($deletionSql, '(?im)^\s*INSERT INTO\s+(\[[^\]]+\]\.\[[^\]]+\])') | ForEach-Object { $_.Groups[1].Value })
    $deletionExpected = if ($deletionFile -like '*application.sql') { @('[platform].[PermissionDefinitions]', '[migration].[__ApplicationMigrationsHistory]') } else { @('[identity].[RolePermissions]', '[migration].[__IdentityMigrationsHistory]') }
    if ($deletionSql -match '(?im)^\s*(DELETE|UPDATE|DROP|ALTER|TRUNCATE|CREATE|MERGE|EXEC)\b' -or
        ($deletionTargets -join ',') -cne ($deletionExpected -join ',') -or
        ([regex]::Matches($deletionSql, '(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($deletionSql, '(?im)^COMMIT;')).Count -ne 1 -or
        !$deletionSql.Contains("N'ninja.performance.delete'")) { throw 'Unexpected permission migration script; refusing unrelated writes.' }
    $deletionHashes[$deletionFile] = (Get-FileHash -LiteralPath $deletionFilePath -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $deletionFilePath -Destination (Join-Path $EvidenceDirectory $deletionFile)
    $deletionBodies += $deletionSql -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', '' -replace '(?im)^GO\s*$', ''
}

function Read-DeletionSql([string]$Sql) {
    $deletionCommand = $deletionConnection.CreateCommand()
    $deletionCommand.CommandTimeout = 60; $deletionCommand.CommandText = $Sql; $deletionCommand.Transaction = $deletionTransaction
    $deletionAdapter = [Data.SqlClient.SqlDataAdapter]::new($deletionCommand)
    try { $deletionData = [Data.DataSet]::new(); [void]$deletionAdapter.Fill($deletionData); return ,$deletionData }
    finally { $deletionAdapter.Dispose(); $deletionCommand.Dispose() }
}

function Get-DeletionState {
    return Read-DeletionSql @'
SELECT Id,[Key],Category,NameAr,NameEn,IsDeleted,IsDeprecated,IsSensitive,IsHighTrust,RequiresClientScope,RequiresHousingScope,Version
FROM platform.PermissionDefinitions WHERE Id='019c18d5-62e1-7000-a000-000000000322' OR [Key]=N'ninja.performance.delete';
SELECT Id,RoleId,PermissionKey,IsDeleted FROM [identity].RolePermissions
WHERE Id='019c18d5-62e1-7000-b000-000000000317'
 OR (RoleId='019c18d5-62e1-7000-9000-000000000001' AND PermissionKey=N'ninja.performance.delete');
SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261110101250_AddNinjaPerformanceDeletePermission') THEN 1 ELSE 0 END AS bit) ApplicationApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261110101358_GrantNinjaPerformanceDeletePermission') THEN 1 ELSE 0 END AS bit) IdentityApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261106143740_HungerSourceCaptainClassification') THEN 1 ELSE 0 END AS bit) ApplicationPrerequisite,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261106094011_GrantFleetVehicleIdentityFieldPermissions') THEN 1 ELSE 0 END AS bit) IdentityPrerequisite,
(SELECT COUNT(*) FROM platform.ClientPlatforms WHERE Code=N'NINJA' AND IsDeleted=0) NinjaPlatforms,
(SELECT COUNT(*) FROM [identity].Roles WHERE Id='019c18d5-62e1-7000-9000-000000000001' AND IsDeleted=0 AND Status=2) ActiveAdministratorRoles,
(SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE [Key] LIKE N'ninja.%' AND IsDeleted=0 AND IsDeprecated=0) NinjaDefinitions,
(SELECT COUNT(*) FROM [identity].RolePermissions WHERE RoleId='019c18d5-62e1-7000-9000-000000000001' AND PermissionKey LIKE N'ninja.%' AND IsDeleted=0) AdministratorNinjaGrants;
SELECT N'NinjaAccounts' AS Dataset,COUNT_BIG(*) AS Records,CHECKSUM_AGG(BINARY_CHECKSUM(a.Id,a.SponsorId,a.Status,a.IsDeleted,a.RowVersion)) AS Fingerprint
FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA'
UNION ALL SELECT N'NinjaAssignments',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(r.Id,r.RiderProfileId,r.PlatformRiderAccountId,r.EffectiveFrom,r.EffectiveTo,r.Status,r.IsDeleted,r.RowVersion))
FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON a.Id=r.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA'
UNION ALL SELECT N'Performance',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SourceId,PlatformRiderAccountId,RiderClientAssignmentId,RiderProfileId,ReportDate,TotalOrders,TotalWorkingHours)) FROM ninja.NinjaDailyPerformance
UNION ALL SELECT N'Deductions',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SourceId,PlatformRiderAccountId,RiderClientAssignmentId,RiderProfileId,ReportDate,TotalDeductions,TotalCollectedAmountAtDelivery)) FROM ninja.NinjaDailyDeductions
UNION ALL SELECT N'NinjaDayUsages',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(u.Id,u.RiderClientAssignmentId,u.BusinessDate,u.Sequence,u.OrdersCount,u.WorkingHours,u.CollectedAmount,u.DeductionsAmount,u.IsDeleted,u.RowVersion))
FROM app.PlatformAccountDayUsages u JOIN app.PlatformRiderAccounts a ON a.Id=u.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=a.ClientPlatformId WHERE p.Code=N'NINJA'
UNION ALL SELECT N'ExistingDefinitions',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,[Key],Category,IsDeleted,RequiresClientScope,IsSensitive,RowVersion)) FROM platform.PermissionDefinitions WHERE Id<>'019c18d5-62e1-7000-a000-000000000322'
UNION ALL SELECT N'ExistingRoleGrants',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,RoleId,PermissionKey,IsDeleted,RowVersion)) FROM [identity].RolePermissions WHERE Id<>'019c18d5-62e1-7000-b000-000000000317';
'@
}

function Assert-DeletionState($State, [bool]$Complete) {
    $deletionSummary = $State.Tables[2].Rows[0]
    if (!$deletionSummary.ApplicationPrerequisite -or !$deletionSummary.IdentityPrerequisite -or $deletionSummary.NinjaPlatforms -ne 1 -or $deletionSummary.ActiveAdministratorRoles -ne 1) { throw 'Hosted migration prerequisites, Ninja platform or active administrator role are missing.' }
    if ($State.Tables[0].Rows.Count -ne [int]$deletionSummary.ApplicationApplied -or $State.Tables[1].Rows.Count -ne [int]$deletionSummary.IdentityApplied) { throw 'Partial or conflicting permission/migration state.' }
    if ($deletionSummary.ApplicationApplied) {
        $deletionDefinition = $State.Tables[0].Rows[0]
        if ([string]$deletionDefinition.Id -ne '019c18d5-62e1-7000-a000-000000000322' -or $deletionDefinition.Key -cne 'ninja.performance.delete' -or
            $deletionDefinition.Category -cne 'Ninja' -or $deletionDefinition.IsDeleted -or $deletionDefinition.IsDeprecated -or !$deletionDefinition.IsSensitive -or
            !$deletionDefinition.RequiresClientScope -or $deletionDefinition.RequiresHousingScope -or !$deletionDefinition.IsHighTrust -or $deletionDefinition.Version -ne 1) { throw 'Permission definition does not match the reviewed migration.' }
    }
    if ($deletionSummary.IdentityApplied) {
        $deletionGrant = $State.Tables[1].Rows[0]
        if ([string]$deletionGrant.Id -ne '019c18d5-62e1-7000-b000-000000000317' -or [string]$deletionGrant.RoleId -ne '019c18d5-62e1-7000-9000-000000000001' -or
            $deletionGrant.PermissionKey -cne 'ninja.performance.delete' -or $deletionGrant.IsDeleted) { throw 'Administrator grant does not match the reviewed migration.' }
    }
    if ($Complete -and (!$deletionSummary.ApplicationApplied -or !$deletionSummary.IdentityApplied)) { throw 'Permission migrations are not both applied.' }
}

function Assert-DeletionPreserved($Before, $After) {
    foreach ($deletionRow in $Before.Tables[3].Rows) {
        $deletionMatch = @($After.Tables[3].Rows | Where-Object Dataset -eq $deletionRow.Dataset)
        if ($deletionMatch.Count -ne 1 -or $deletionMatch[0].Records -ne $deletionRow.Records -or [string]$deletionMatch[0].Fingerprint -ne [string]$deletionRow.Fingerprint) { throw ('Unrelated data changed: ' + $deletionRow.Dataset) }
    }
}

try {
    if ($deletionConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $deletionConnection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $deletionConnection.Open()
    $deletionBefore = Get-DeletionState
    Assert-DeletionState $deletionBefore $false
    $deletionBefore.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-before.xml')), [Data.XmlWriteMode]::WriteSchema)
    $deletionInitial = $deletionBefore.Tables[2].Rows[0]
    $deletionAlreadyApplied = $deletionInitial.ApplicationApplied -and $deletionInitial.IdentityApplied
    if ($Mode -eq 'Verify') { Assert-DeletionState $deletionBefore $true }
    if ($Mode -in @('Rehearse', 'Apply') -and !$deletionAlreadyApplied) {
        if ($Mode -eq 'Apply') {
            $deletionRehearsal = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'rehearse.json') -Raw | ConvertFrom-Json
            if (!$deletionRehearsal.Passed -or $deletionRehearsal.Applied -or $deletionRehearsal.Mode -ne 'Rehearse') { throw 'A successful rollback rehearsal is required.' }
            foreach ($deletionFile in $deletionFiles) { if ($deletionRehearsal.ScriptSha256.$deletionFile -cne $deletionHashes[$deletionFile]) { throw 'Scripts changed after rehearsal.' } }
        }
        $deletionTransaction = $deletionConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
        [void](Read-DeletionSql @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'ninja-performance-deletion-permission-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Cannot acquire the permission migration lock.',1;
'@)
        $deletionLockedBefore = Get-DeletionState; Assert-DeletionState $deletionLockedBefore $false
        if (!$deletionLockedBefore.Tables[2].Rows[0].ApplicationApplied) { [void](Read-DeletionSql $deletionBodies[0]) }
        if (!$deletionLockedBefore.Tables[2].Rows[0].IdentityApplied) { [void](Read-DeletionSql $deletionBodies[1]) }
        $deletionInside = Get-DeletionState; Assert-DeletionState $deletionInside $true
        Assert-DeletionPreserved $deletionLockedBefore $deletionInside
        $deletionInside.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-inside.xml')), [Data.XmlWriteMode]::WriteSchema)
        if ($Mode -eq 'Apply') { $deletionTransaction.Commit() } else { $deletionTransaction.Rollback() }
        $deletionTransaction.Dispose(); $deletionTransaction = $null
    }
    $deletionConnection.Close(); $deletionConnection.Open()
    $deletionAfter = Get-DeletionState
    Assert-DeletionState $deletionAfter ($Mode -in @('Apply', 'Verify') -or $deletionAlreadyApplied)
    if ($Mode -eq 'Rehearse' -and !$deletionAlreadyApplied -and
        ($deletionAfter.Tables[2].Rows[0].ApplicationApplied -ne $deletionInitial.ApplicationApplied -or $deletionAfter.Tables[2].Rows[0].IdentityApplied -ne $deletionInitial.IdentityApplied)) { throw 'Rehearsal rollback was not verified.' }
    $deletionAfter.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-after.xml')), [Data.XmlWriteMode]::WriteSchema)
    $deletionReport = [ordered]@{
        Passed = $true; Mode = $Mode; Server = $deletionConnection.DataSource; Database = $deletionConnection.Database
        VerifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); Permission = 'ninja.performance.delete'
        Applied = [bool]($deletionAfter.Tables[2].Rows[0].ApplicationApplied -and $deletionAfter.Tables[2].Rows[0].IdentityApplied)
        AlreadyAppliedBeforeRun = [bool]$deletionAlreadyApplied
        NinjaDefinitions = $deletionAfter.Tables[2].Rows[0].NinjaDefinitions
        AdministratorNinjaGrants = $deletionAfter.Tables[2].Rows[0].AdministratorNinjaGrants
        FreshConnectionVerified = $true; ScriptSha256 = $deletionHashes; EvidenceDirectory = $EvidenceDirectory
        ApiPublishedByThisRun = $false
    }
    if ($Mode -in @('Rehearse', 'Apply') -and !$deletionAlreadyApplied) { $deletionReport.ExistingDataPreservedInsideTransaction = $true }
    $deletionReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '.json')) -Encoding utf8
    $deletionReport | ConvertTo-Json -Depth 5
} finally {
    if ($deletionTransaction) { try { $deletionTransaction.Rollback() } finally { $deletionTransaction.Dispose() } }
    $deletionConnection.Dispose()
}

