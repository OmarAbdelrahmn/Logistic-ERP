param(
    [ValidateSet('Preflight', 'Rehearse', 'Apply', 'Verify')][string]$Mode = 'Preflight',
    [string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$datesRoot = Split-Path -Parent $PSScriptRoot
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path $datesRoot ('outputs/ninja-assignment-dates-hosted-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')) }
[void](New-Item -ItemType Directory -Force -Path $EvidenceDirectory)
$datesConfig = Get-Content -LiteralPath (Join-Path $datesRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$datesConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $datesConfig.ConnectionStrings.LogisticsDatabase }
$datesConnection = [Data.SqlClient.SqlConnection]::new($datesConnectionText)
$datesTransaction = $null
$datesApplicationId = '20261028111233_AddNinjaAssignmentDatePermissionCatalog'
$datesIdentityId = '20261028111320_GrantNinjaAssignmentDateCorrectionAccess'
$datesFiles = @('ninja-assignment-dates-application.sql', 'ninja-assignment-dates-identity.sql')
$datesHashes = [ordered]@{}
$datesBodies = @()
foreach ($datesFile in $datesFiles) {
    $datesFilePath = Join-Path $datesRoot ('database/scripts/' + $datesFile)
    $datesSql = Get-Content -LiteralPath $datesFilePath -Raw
    $datesTargets = @([regex]::Matches($datesSql, '(?im)^\s*INSERT INTO\s+(\[[^\]]+\]\.\[[^\]]+\])') | ForEach-Object { $_.Groups[1].Value })
    $datesExpected = if ($datesFile -like '*application.sql') { @('[platform].[PermissionDefinitions]', '[migration].[__ApplicationMigrationsHistory]') } else { @('[identity].[RolePermissions]', '[migration].[__IdentityMigrationsHistory]') }
    if ($datesSql -match '(?im)^\s*(DELETE|UPDATE|DROP|ALTER|TRUNCATE|CREATE|MERGE|EXEC)\b' -or
        ($datesTargets -join ',') -cne ($datesExpected -join ',') -or
        ([regex]::Matches($datesSql, '(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($datesSql, '(?im)^COMMIT;')).Count -ne 1 -or
        !$datesSql.Contains("N'ninja.assignments.dates.update'")) { throw 'Unexpected permission migration script; refusing unrelated writes.' }
    $datesHashes[$datesFile] = (Get-FileHash -LiteralPath $datesFilePath -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $datesFilePath -Destination (Join-Path $EvidenceDirectory $datesFile)
    $datesBodies += $datesSql -replace '(?im)^BEGIN TRANSACTION;\s*$', '' -replace '(?im)^COMMIT;\s*$', '' -replace '(?im)^GO\s*$', ''
}

function Read-DatesSql([string]$Sql) {
    $datesCommand = $datesConnection.CreateCommand()
    $datesCommand.CommandTimeout = 60; $datesCommand.CommandText = $Sql; $datesCommand.Transaction = $datesTransaction
    $datesAdapter = [Data.SqlClient.SqlDataAdapter]::new($datesCommand)
    try { $datesData = [Data.DataSet]::new(); [void]$datesAdapter.Fill($datesData); return ,$datesData }
    finally { $datesAdapter.Dispose(); $datesCommand.Dispose() }
}

function Get-DatesState {
    return Read-DatesSql @'
SELECT Id,[Key],Category,NameAr,NameEn,IsDeleted,IsDeprecated,IsSensitive,IsHighTrust,RequiresClientScope,RequiresHousingScope,Version
FROM platform.PermissionDefinitions WHERE Id='019c18d5-62e1-7000-a000-000000000319' OR [Key]=N'ninja.assignments.dates.update';
SELECT Id,RoleId,PermissionKey,IsDeleted FROM [identity].RolePermissions
WHERE Id='019c18d5-62e1-7000-b000-000000000283'
 OR (RoleId='019c18d5-62e1-7000-9000-000000000001' AND PermissionKey=N'ninja.assignments.dates.update');
SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261028111233_AddNinjaAssignmentDatePermissionCatalog') THEN 1 ELSE 0 END AS bit) ApplicationApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261028111320_GrantNinjaAssignmentDateCorrectionAccess') THEN 1 ELSE 0 END AS bit) IdentityApplied,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261026180608_AddNinjaEvaluationSuggestions') THEN 1 ELSE 0 END AS bit) ApplicationPrerequisite,
CAST(CASE WHEN EXISTS(SELECT 1 FROM migration.__IdentityMigrationsHistory WHERE MigrationId=N'20261026180713_GrantNinjaEvaluationPermissions') THEN 1 ELSE 0 END AS bit) IdentityPrerequisite,
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
UNION ALL SELECT N'ExistingDefinitions',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,[Key],Category,IsDeleted,RequiresClientScope,IsSensitive,RowVersion)) FROM platform.PermissionDefinitions WHERE Id<>'019c18d5-62e1-7000-a000-000000000319'
UNION ALL SELECT N'ExistingRoleGrants',COUNT_BIG(*),CHECKSUM_AGG(BINARY_CHECKSUM(Id,RoleId,PermissionKey,IsDeleted,RowVersion)) FROM [identity].RolePermissions WHERE Id<>'019c18d5-62e1-7000-b000-000000000283';
'@
}

function Assert-DatesState($State, [bool]$Complete) {
    $datesSummary = $State.Tables[2].Rows[0]
    if (!$datesSummary.ApplicationPrerequisite -or !$datesSummary.IdentityPrerequisite -or $datesSummary.NinjaPlatforms -ne 1 -or $datesSummary.ActiveAdministratorRoles -ne 1) { throw 'Hosted migration prerequisites, Ninja platform or active administrator role are missing.' }
    if ($State.Tables[0].Rows.Count -ne [int]$datesSummary.ApplicationApplied -or $State.Tables[1].Rows.Count -ne [int]$datesSummary.IdentityApplied) { throw 'Partial or conflicting permission/migration state.' }
    if ($datesSummary.ApplicationApplied) {
        $datesDefinition = $State.Tables[0].Rows[0]
        if ([string]$datesDefinition.Id -ne '019c18d5-62e1-7000-a000-000000000319' -or $datesDefinition.Key -cne 'ninja.assignments.dates.update' -or
            $datesDefinition.Category -cne 'Ninja' -or $datesDefinition.IsDeleted -or $datesDefinition.IsDeprecated -or !$datesDefinition.IsSensitive -or
            !$datesDefinition.RequiresClientScope -or $datesDefinition.RequiresHousingScope -or $datesDefinition.IsHighTrust -or $datesDefinition.Version -ne 1) { throw 'Permission definition does not match the reviewed migration.' }
    }
    if ($datesSummary.IdentityApplied) {
        $datesGrant = $State.Tables[1].Rows[0]
        if ([string]$datesGrant.Id -ne '019c18d5-62e1-7000-b000-000000000283' -or [string]$datesGrant.RoleId -ne '019c18d5-62e1-7000-9000-000000000001' -or
            $datesGrant.PermissionKey -cne 'ninja.assignments.dates.update' -or $datesGrant.IsDeleted) { throw 'Administrator grant does not match the reviewed migration.' }
    }
    if ($Complete -and (!$datesSummary.ApplicationApplied -or !$datesSummary.IdentityApplied)) { throw 'Permission migrations are not both applied.' }
}

function Assert-DatesPreserved($Before, $After) {
    foreach ($datesRow in $Before.Tables[3].Rows) {
        $datesMatch = @($After.Tables[3].Rows | Where-Object Dataset -eq $datesRow.Dataset)
        if ($datesMatch.Count -ne 1 -or $datesMatch[0].Records -ne $datesRow.Records -or [string]$datesMatch[0].Fingerprint -ne [string]$datesRow.Fingerprint) { throw ('Unrelated data changed: ' + $datesRow.Dataset) }
    }
}

try {
    if ($datesConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $datesConnection.Database -ne 'db67927') { throw 'Unexpected hosted database target.' }
    $datesConnection.Open()
    $datesBefore = Get-DatesState
    Assert-DatesState $datesBefore $false
    $datesBefore.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-before.xml')), [Data.XmlWriteMode]::WriteSchema)
    $datesInitial = $datesBefore.Tables[2].Rows[0]
    $datesAlreadyApplied = $datesInitial.ApplicationApplied -and $datesInitial.IdentityApplied
    if ($Mode -eq 'Verify') { Assert-DatesState $datesBefore $true }
    if ($Mode -in @('Rehearse', 'Apply') -and !$datesAlreadyApplied) {
        if ($Mode -eq 'Apply') {
            $datesRehearsal = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'rehearse.json') -Raw | ConvertFrom-Json
            if (!$datesRehearsal.Passed -or $datesRehearsal.Applied -or $datesRehearsal.Mode -ne 'Rehearse') { throw 'A successful rollback rehearsal is required.' }
            foreach ($datesFile in $datesFiles) { if ($datesRehearsal.ScriptSha256.$datesFile -cne $datesHashes[$datesFile]) { throw 'Scripts changed after rehearsal.' } }
        }
        $datesTransaction = $datesConnection.BeginTransaction([Data.IsolationLevel]::Serializable)
        [void](Read-DatesSql @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'ninja-assignment-date-permission-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Cannot acquire the permission migration lock.',1;
'@)
        $datesLockedBefore = Get-DatesState; Assert-DatesState $datesLockedBefore $false
        if (!$datesLockedBefore.Tables[2].Rows[0].ApplicationApplied) { [void](Read-DatesSql $datesBodies[0]) }
        if (!$datesLockedBefore.Tables[2].Rows[0].IdentityApplied) { [void](Read-DatesSql $datesBodies[1]) }
        $datesInside = Get-DatesState; Assert-DatesState $datesInside $true
        Assert-DatesPreserved $datesLockedBefore $datesInside
        $datesInside.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-inside.xml')), [Data.XmlWriteMode]::WriteSchema)
        if ($Mode -eq 'Apply') { $datesTransaction.Commit() } else { $datesTransaction.Rollback() }
        $datesTransaction.Dispose(); $datesTransaction = $null
    }
    $datesConnection.Close(); $datesConnection.Open()
    $datesAfter = Get-DatesState
    Assert-DatesState $datesAfter ($Mode -in @('Apply', 'Verify') -or $datesAlreadyApplied)
    if ($Mode -eq 'Rehearse' -and !$datesAlreadyApplied -and
        ($datesAfter.Tables[2].Rows[0].ApplicationApplied -ne $datesInitial.ApplicationApplied -or $datesAfter.Tables[2].Rows[0].IdentityApplied -ne $datesInitial.IdentityApplied)) { throw 'Rehearsal rollback was not verified.' }
    $datesAfter.WriteXml((Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '-after.xml')), [Data.XmlWriteMode]::WriteSchema)
    $datesReport = [ordered]@{
        Passed = $true; Mode = $Mode; Server = $datesConnection.DataSource; Database = $datesConnection.Database
        VerifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); Permission = 'ninja.assignments.dates.update'
        Applied = [bool]($datesAfter.Tables[2].Rows[0].ApplicationApplied -and $datesAfter.Tables[2].Rows[0].IdentityApplied)
        AlreadyAppliedBeforeRun = [bool]$datesAlreadyApplied
        NinjaDefinitions = $datesAfter.Tables[2].Rows[0].NinjaDefinitions
        AdministratorNinjaGrants = $datesAfter.Tables[2].Rows[0].AdministratorNinjaGrants
        FreshConnectionVerified = $true; ScriptSha256 = $datesHashes; EvidenceDirectory = $EvidenceDirectory
        ApiPublishedByThisRun = $false
    }
    if ($Mode -in @('Rehearse', 'Apply') -and !$datesAlreadyApplied) { $datesReport.ExistingDataPreservedInsideTransaction = $true }
    $datesReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $EvidenceDirectory ($Mode.ToLowerInvariant() + '.json')) -Encoding utf8
    $datesReport | ConvertTo-Json -Depth 5
} finally {
    if ($datesTransaction) { try { $datesTransaction.Rollback() } finally { $datesTransaction.Dispose() } }
    $datesConnection.Dispose()
}
