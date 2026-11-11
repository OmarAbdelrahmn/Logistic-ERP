param(
    [ValidateSet('Preflight', 'Rehearse', 'Apply', 'Verify')][string]$Mode = 'Preflight',
    [string]$EvidenceDirectory = './outputs/platform-performance-deletion-hosted-20261110'
)
$ErrorActionPreference = 'Stop'
$performanceRoot = Split-Path -Parent $PSScriptRoot
[void](New-Item -ItemType Directory -Force -Path $EvidenceDirectory)
$performanceConfig = Get-Content -LiteralPath (Join-Path $performanceRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$performanceText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $performanceConfig.ConnectionStrings.LogisticsDatabase }
$performanceConnection = [Data.SqlClient.SqlConnection]::new($performanceText)
$performanceTransaction = $null
$performanceFiles = @('platform-performance-deletion-application.sql', 'platform-performance-deletion-identity.sql')
$performanceHashes = [ordered]@{}; $performanceBodies = @()
foreach ($performanceFile in $performanceFiles) {
    $performancePath = Join-Path $performanceRoot ('database/scripts/' + $performanceFile)
    $performanceSql = Get-Content -LiteralPath $performancePath -Raw
    $performanceTargets = @([regex]::Matches($performanceSql, '(?im)^\s*INSERT INTO\s+(\[[^\]]+\]\.\[[^\]]+\])') | ForEach-Object { $_.Groups[1].Value })
    $performanceExpected = if ($performanceFile -like '*application.sql') { '[platform].[PermissionDefinitions],[migration].[__ApplicationMigrationsHistory]' } else { '[identity].[RolePermissions],[migration].[__IdentityMigrationsHistory]' }
    if ($performanceSql -match '(?im)^\s*(DELETE|UPDATE|DROP|ALTER|TRUNCATE|CREATE|MERGE|EXEC)\b' -or
        ($performanceTargets -join ',') -cne $performanceExpected -or
        ([regex]::Matches($performanceSql, '(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($performanceSql, '(?im)^COMMIT;')).Count -ne 1 -or
        !$performanceSql.Contains("N'hunger.performance.delete'") -or !$performanceSql.Contains("N'jahez.performance.delete'")) { throw 'Unexpected permission migration SQL.' }
    $performanceHashes[$performanceFile] = (Get-FileHash -LiteralPath $performancePath -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $performancePath -Destination (Join-Path $EvidenceDirectory $performanceFile)
    $performanceBodies += $performanceSql -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', '' -replace '(?im)^GO\s*$', ''
}
function Read-PerformanceSql([string]$Sql) {
    $performanceCommand = $performanceConnection.CreateCommand()
    $performanceCommand.CommandText = $Sql; $performanceCommand.CommandTimeout = 90; $performanceCommand.Transaction = $performanceTransaction
    $performanceAdapter = [Data.SqlClient.SqlDataAdapter]::new($performanceCommand)
    try { $performanceData = [Data.DataSet]::new(); [void]$performanceAdapter.Fill($performanceData); return ,$performanceData }
    finally { $performanceAdapter.Dispose(); $performanceCommand.Dispose() }
}
function Get-PerformanceState {
    Read-PerformanceSql @'
SELECT Id,[Key],Category,IsDeleted,IsDeprecated,IsHighTrust,IsSensitive,RequiresClientScope,RequiresHousingScope,GrantabilityRule,Version
FROM platform.PermissionDefinitions WHERE [Key] IN(N'hunger.performance.delete',N'jahez.performance.delete')
 OR Id IN('019c18d5-62e1-7000-a000-000000000323','019c18d5-62e1-7000-a000-000000000324');
SELECT Id,RoleId,PermissionKey,IsDeleted FROM [identity].RolePermissions WHERE Id IN('019c18d5-62e1-7000-b000-000000000318','019c18d5-62e1-7000-b000-000000000319','019c18d5-62e1-7000-b000-000000000320')
 OR (RoleId='019c18d5-62e1-7000-9000-000000000001' AND PermissionKey IN(N'hunger.performance.delete',N'jahez.performance.delete',N'chefz.performance.delete'));
SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261110105351_AddHungerJahezPerformanceDeletePermissions') THEN 1 ELSE 0 END AS bit) ApplicationApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261110105334_GrantHungerJahezPerformanceDeletePermissions') THEN 1 ELSE 0 END AS bit) IdentityApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261110101250_AddNinjaPerformanceDeletePermission') THEN 1 ELSE 0 END AS bit) ApplicationPrerequisite,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261110101358_GrantNinjaPerformanceDeletePermission') THEN 1 ELSE 0 END AS bit) IdentityPrerequisite,
(SELECT COUNT(*) FROM [identity].Roles WHERE Id='019c18d5-62e1-7000-9000-000000000001' AND IsDeleted=0 AND Status=2) AdministratorRoles,
(SELECT COUNT(*) FROM platform.PermissionDefinitions WHERE Id='019c18d5-62e1-7000-a000-000000000321' AND [Key]=N'chefz.performance.delete' AND IsDeleted=0 AND IsHighTrust=1 AND IsSensitive=1 AND RequiresClientScope=1) ChefzDefinitions;
SELECT N'ExistingDefinitions' Dataset,COUNT_BIG(*) Records,CHECKSUM_AGG(BINARY_CHECKSUM(Id,[Key],IsDeleted,IsHighTrust,RequiresClientScope,RowVersion)) Fingerprint
FROM platform.PermissionDefinitions WHERE Id NOT IN('019c18d5-62e1-7000-a000-000000000323','019c18d5-62e1-7000-a000-000000000324')
UNION ALL SELECT N'ExistingGrants',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,RoleId,PermissionKey,IsDeleted,RowVersion)) FROM [identity].RolePermissions
WHERE Id NOT IN('019c18d5-62e1-7000-b000-000000000318','019c18d5-62e1-7000-b000-000000000319','019c18d5-62e1-7000-b000-000000000320')
UNION ALL SELECT N'Accounts',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SponsorId,IsDeleted,RowVersion)) FROM app.PlatformRiderAccounts
UNION ALL SELECT N'Assignments',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,EffectiveFrom,EffectiveTo,IsDeleted,RowVersion)) FROM app.RiderClientAssignments
UNION ALL SELECT N'HungerOperations',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,IsDeleted,RowVersion)) FROM hunger.DailyOperations
UNION ALL SELECT N'HungerPotentialFines',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,IsDeleted,RowVersion)) FROM hunger.PotentialFines
UNION ALL SELECT N'ChefzPerformance',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,SourceId,RiderClientAssignmentId,ReportDate,AcceptedOffers)) FROM chefz.DailyPerformance
UNION ALL SELECT N'JahezOrders',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,BatchId,ImportRowId,HandoverId,[Date],[Count])) FROM jahez_v2.DailyOrders;
'@
}
function Assert-PerformanceState($State, [bool]$Complete) {
    $performanceSummary = $State.Tables[2].Rows[0]
    if (!$performanceSummary.ApplicationPrerequisite -or !$performanceSummary.IdentityPrerequisite -or $performanceSummary.AdministratorRoles -ne 1 -or $performanceSummary.ChefzDefinitions -ne 1) { throw 'Required prior migrations, Chefz definition or administrator role missing.' }
    if ($State.Tables[0].Rows.Count -ne (2 * [int]$performanceSummary.ApplicationApplied) -or $State.Tables[1].Rows.Count -ne (3 * [int]$performanceSummary.IdentityApplied)) { throw 'Permission state conflicts with migration history.' }
    foreach ($performanceRule in @(@('hunger.performance.delete','Hunger','019c18d5-62e1-7000-a000-000000000323','019c18d5-62e1-7000-b000-000000000318'), @('jahez.performance.delete','Jahez','019c18d5-62e1-7000-a000-000000000324','019c18d5-62e1-7000-b000-000000000319'), @('chefz.performance.delete','Chefz','019c18d5-62e1-7000-a000-000000000321','019c18d5-62e1-7000-b000-000000000320'))) {
        if ($performanceSummary.ApplicationApplied -and $performanceRule[1] -ne 'Chefz') {
            $performanceDefinitions = @($State.Tables[0].Rows | Where-Object { $_.Key -ceq $performanceRule[0] })
            if ($performanceDefinitions.Count -ne 1) { throw 'Permission missing or duplicated.' }; $performanceDefinition = $performanceDefinitions[0]
            if ([string]$performanceDefinition.Id -ne $performanceRule[2] -or $performanceDefinition.Category -cne $performanceRule[1] -or $performanceDefinition.IsDeleted -or $performanceDefinition.IsDeprecated -or !$performanceDefinition.IsHighTrust -or !$performanceDefinition.IsSensitive -or !$performanceDefinition.RequiresClientScope -or $performanceDefinition.RequiresHousingScope -or $performanceDefinition.GrantabilityRule -cne 'HIGH_TRUST_ONLY' -or $performanceDefinition.Version -ne 1) { throw 'Permission metadata does not match the migration.' }
        }
        if ($performanceSummary.IdentityApplied) {
            $performanceGrants = @($State.Tables[1].Rows | Where-Object { $_.PermissionKey -ceq $performanceRule[0] })
            if ($performanceGrants.Count -ne 1) { throw 'Administrator grant missing or duplicated.' }; $performanceGrant = $performanceGrants[0]
            if ([string]$performanceGrant.Id -ne $performanceRule[3] -or [string]$performanceGrant.RoleId -ne '019c18d5-62e1-7000-9000-000000000001' -or $performanceGrant.IsDeleted) { throw 'Administrator grant does not match the migration.' }
        }
    }
    if ($Complete -and (!$performanceSummary.ApplicationApplied -or !$performanceSummary.IdentityApplied)) { throw 'Both migrations are not applied.' }
}
function Assert-PerformancePreserved($Before, $After) {
    foreach ($performanceRow in $Before.Tables[3].Rows) {
        $performanceMatches = @($After.Tables[3].Rows | Where-Object Dataset -eq $performanceRow.Dataset)
        if ($performanceMatches.Count -ne 1 -or $performanceMatches[0].Records -ne $performanceRow.Records -or [string]$performanceMatches[0].Fingerprint -ne [string]$performanceRow.Fingerprint) { throw ('Unrelated data changed: ' + $performanceRow.Dataset) }
    }
}
try {
    if ($performanceConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $performanceConnection.Database -ne 'db67927') { throw 'Unexpected hosted target.' }
    $performanceConnection.Open(); $performanceBefore = Get-PerformanceState; Assert-PerformanceState $performanceBefore $false
    $performanceBefore.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-before.xml')), [Data.XmlWriteMode]::WriteSchema)
    $performanceSummary = $performanceBefore.Tables[2].Rows[0]; $performanceAlreadyApplied = $performanceSummary.ApplicationApplied -and $performanceSummary.IdentityApplied
    if ($Mode -eq 'Verify') { Assert-PerformanceState $performanceBefore $true }
    if ($Mode -in @('Rehearse','Apply') -and !$performanceAlreadyApplied) {
        if ($Mode -eq 'Apply') {
            $performanceRehearsal = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'rehearse.json') -Raw | ConvertFrom-Json
            if (!$performanceRehearsal.Passed -or $performanceRehearsal.Applied -or $performanceRehearsal.Mode -ne 'Rehearse') { throw 'Successful rollback rehearsal required.' }
            foreach ($performanceFile in $performanceFiles) { if ($performanceRehearsal.ScriptSha256.$performanceFile -cne $performanceHashes[$performanceFile]) { throw 'Migration changed since rehearsal.' } }
        }
        $performanceTransaction = $performanceConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
        [void](Read-PerformanceSql "SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'platform-performance-delete-permissions',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 51000,'Cannot lock permission migration.',1;")
        $performanceLocked = Get-PerformanceState; Assert-PerformanceState $performanceLocked $false
        if (!$performanceLocked.Tables[2].Rows[0].ApplicationApplied) { [void](Read-PerformanceSql $performanceBodies[0]) }
        if (!$performanceLocked.Tables[2].Rows[0].IdentityApplied) { [void](Read-PerformanceSql $performanceBodies[1]) }
        $performanceInside = Get-PerformanceState; Assert-PerformanceState $performanceInside $true; Assert-PerformancePreserved $performanceLocked $performanceInside
        $performanceInside.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-inside.xml')), [Data.XmlWriteMode]::WriteSchema)
        if ($Mode -eq 'Apply') { $performanceTransaction.Commit() } else { $performanceTransaction.Rollback() }
        $performanceTransaction.Dispose(); $performanceTransaction = $null
    }
    $performanceConnection.Close(); $performanceConnection.Open(); $performanceAfter = Get-PerformanceState
    Assert-PerformanceState $performanceAfter ($Mode -in @('Apply','Verify') -or $performanceAlreadyApplied)
    if ($Mode -eq 'Rehearse' -and !$performanceAlreadyApplied -and ($performanceAfter.Tables[2].Rows[0].ApplicationApplied -ne $performanceSummary.ApplicationApplied -or $performanceAfter.Tables[2].Rows[0].IdentityApplied -ne $performanceSummary.IdentityApplied)) { throw 'Rollback was not verified.' }
    $performanceAfter.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-after.xml')), [Data.XmlWriteMode]::WriteSchema)
    $performanceReport = [ordered]@{ Passed=$true; Mode=$Mode; Database=$performanceConnection.Database; Server=$performanceConnection.DataSource; VerifiedAtUtc=[DateTimeOffset]::UtcNow.ToString('o'); Applied=[bool]($performanceAfter.Tables[2].Rows[0].ApplicationApplied -and $performanceAfter.Tables[2].Rows[0].IdentityApplied); Permissions=@('hunger.performance.delete','jahez.performance.delete','chefz.performance.delete'); FreshConnectionVerified=$true; ScriptSha256=$performanceHashes; ApiPublishedByThisRun=$false }
    if ($Mode -in @('Apply','Rehearse') -and !$performanceAlreadyApplied) { $performanceReport.ExistingDataPreservedInsideTransaction=$true }
    $performanceReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant()+'.json')) -Encoding utf8
    $performanceReport | ConvertTo-Json -Depth 5
} finally {
    if ($performanceTransaction) { try { $performanceTransaction.Rollback() } finally { $performanceTransaction.Dispose() } }
    $performanceConnection.Dispose()
}
