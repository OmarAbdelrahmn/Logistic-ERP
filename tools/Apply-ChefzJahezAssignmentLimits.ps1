param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$assignmentWorkspace = Split-Path -Parent $PSScriptRoot
$assignmentConfig = Get-Content -LiteralPath (Join-Path $assignmentWorkspace 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$assignmentConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $assignmentConfig.ConnectionStrings.LogisticsDatabase }
$assignmentConnection = [System.Data.SqlClient.SqlConnection]::new($assignmentConnectionText)
$assignmentTransaction = $null
$assignmentMigration = '20261018083620_AllowChefzAndJahezRiderAssignments'
$assignmentScriptPath = Join-Path $assignmentWorkspace 'database/scripts/rider-chefz-jahez-assignment-limits.sql'

function Read-AssignmentMigrationData([string]$Sql) {
    $assignmentCommand = $assignmentConnection.CreateCommand()
    $assignmentCommand.CommandText = $Sql
    $assignmentCommand.CommandTimeout = 120
    $assignmentCommand.Transaction = $assignmentTransaction
    try {
        $assignmentData = [System.Data.DataSet]::new()
        $assignmentAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($assignmentCommand)
        try { [void]$assignmentAdapter.Fill($assignmentData) } finally { $assignmentAdapter.Dispose() }
        return ,$assignmentData
    } finally { $assignmentCommand.Dispose() }
}

$assignmentPreflightSql = @'
IF NOT EXISTS(SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261017165534_AddJahezV2AccountantHandovers')
    THROW 51000,'Expected preceding application migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'app.RiderClientAssignments')
 AND name=N'CK_RiderClientAssignments_RiderAccountSlot' AND is_disabled=0 AND is_not_trusted=0)
    THROW 51000,'Trusted assignment slot constraint is missing.',1;
IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments') AND is_unique=1 AND has_filter=1 AND is_disabled=0
 AND name IN(N'IX_RiderClientAssignments_RiderProfileId_RiderAccountSlot',N'IX_RiderClientAssignments_RiderProfileId_PaymentModel',N'IX_RiderClientAssignments_PlatformRiderAccountId'))<>3
    THROW 51000,'Expected assignment uniqueness indexes are missing.',1;
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
 WHERE parent_object_id=OBJECT_ID(N'app.RiderClientAssignments') AND name=N'CK_RiderClientAssignments_RiderAccountSlot';
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=N'20261018083620_AllowChefzAndJahezRiderAssignments';
SELECT COUNT_BIG(*) AS AssignmentCount,SUM(CASE WHEN IsDeleted=0 AND EffectiveTo IS NULL THEN 1 ELSE 0 END) AS OpenAssignments FROM app.RiderClientAssignments;
SELECT name,is_unique,filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments') AND is_unique=1 AND has_filter=1 AND is_disabled=0 ORDER BY name;
SELECT COUNT_BIG(*) AS RidersAboveNewCaps FROM (
 SELECT a.RiderProfileId FROM app.RiderClientAssignments a
 JOIN app.PlatformRiderAccounts r ON r.Id=a.PlatformRiderAccountId JOIN platform.ClientPlatforms p ON p.Id=r.ClientPlatformId
 WHERE a.IsDeleted=0 AND a.EffectiveTo IS NULL GROUP BY a.RiderProfileId
 HAVING SUM(CASE WHEN p.Code=N'JAHEZ' THEN 1 ELSE 0 END)>1
 OR SUM(CASE WHEN p.Code IN(N'CHEFZ',N'THECHEFZ',N'CHEFSZ',N'SHIFTZ') THEN 1 ELSE 0 END)>2
) q;
'@

function Assert-ThreeAssignmentSlots($Data) {
    if ($Data.Tables[1].Rows.Count -ne 1) { throw 'Assignment migration history was not recorded.' }
    $assignmentDefinition = [string]$Data.Tables[0].Rows[0].definition
    $assignmentNormalized = $assignmentDefinition -replace '[\[\]\(\)\s]', ''
    $assignmentTerms = @($assignmentNormalized -split 'OR' | Sort-Object)
    if (($assignmentTerms -join '|') -ne 'RiderAccountSlot=1|RiderAccountSlot=2|RiderAccountSlot=3' -and
        $assignmentNormalized -ne 'RiderAccountSlotIN1,2,3') {
        throw 'The hosted assignment constraint does not permit exactly slots 1, 2 and 3.'
    }
}

try {
    $assignmentConnection.Open()
    if ($assignmentConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $assignmentConnection.Database -ne 'db67927') {
        throw 'Unexpected hosted database target; refusing assignment migration.'
    }
    $assignmentBefore = Read-AssignmentMigrationData $assignmentPreflightSql
    Write-Output ('Verified target: {0} / {1}' -f $assignmentConnection.DataSource,$assignmentConnection.Database)
    Write-Output ('Assignments: {0}; open: {1}; migration recorded: {2}; existing riders above new caps: {3}' -f
        $assignmentBefore.Tables[2].Rows[0].AssignmentCount,$assignmentBefore.Tables[2].Rows[0].OpenAssignments,
        ($assignmentBefore.Tables[1].Rows.Count -eq 1),$assignmentBefore.Tables[4].Rows[0].RidersAboveNewCaps)
    Write-Output ('Slot constraint: ' + $assignmentBefore.Tables[0].Rows[0].definition)
    if (!$Apply) { Write-Output 'Read-only preflight completed.'; return }

    $assignmentScript = Get-Content -LiteralPath $assignmentScriptPath -Raw
    if (([regex]::Matches($assignmentScript,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($assignmentScript,'(?im)^COMMIT;')).Count -ne 1 -or
        $assignmentScript -match '(?im)\b(DELETE|UPDATE|TRUNCATE|CREATE TABLE|DROP TABLE)\b' -or
        $assignmentScript -notmatch [regex]::Escape($assignmentMigration) -or
        $assignmentScript -notmatch '\[RiderAccountSlot\] IN \(1, 2, 3\)') {
        throw 'Unexpected assignment migration script structure.'
    }
    $assignmentBody = $assignmentScript -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
    $assignmentOutput = Join-Path $assignmentWorkspace ('outputs/chefz-jahez-assignment-migration-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $assignmentOutput)
    $assignmentBefore.WriteXml((Join-Path $assignmentOutput 'preflight.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $assignmentScriptPath -Destination (Join-Path $assignmentOutput 'migration.sql')
    $assignmentHash = (Get-FileHash -LiteralPath $assignmentScriptPath -Algorithm SHA256).Hash

    $assignmentTransaction = $assignmentConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    [void](Read-AssignmentMigrationData @'
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 15000;
DECLARE @result int;
EXEC @result=sys.sp_getapplock @Resource=N'chefz-jahez-assignment-limits-migration',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000;
IF @result<0 THROW 51000,'Could not acquire assignment migration lock.',1;
'@)
    [void](Read-AssignmentMigrationData $assignmentPreflightSql)
    $assignmentBackup = Read-AssignmentMigrationData @'
SELECT * INTO #BeforeAssignments FROM app.RiderClientAssignments WITH(TABLOCKX,HOLDLOCK);
SELECT name,is_unique,is_disabled,filter_definition INTO #BeforeAssignmentIndexes FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments');
SELECT * FROM #BeforeAssignments;
'@
    $assignmentBackup.WriteXml((Join-Path $assignmentOutput 'assignments-before.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    [void](Read-AssignmentMigrationData $assignmentBody)
    [void](Read-AssignmentMigrationData @'
IF EXISTS(SELECT * FROM #BeforeAssignments EXCEPT SELECT * FROM app.RiderClientAssignments)
 OR EXISTS(SELECT * FROM app.RiderClientAssignments EXCEPT SELECT * FROM #BeforeAssignments)
    THROW 51000,'Existing assignment rows changed.',1;
IF EXISTS(SELECT * FROM #BeforeAssignmentIndexes EXCEPT SELECT name,is_unique,is_disabled,filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments'))
 OR EXISTS(SELECT name,is_unique,is_disabled,filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'app.RiderClientAssignments') EXCEPT SELECT * FROM #BeforeAssignmentIndexes)
    THROW 51000,'Existing assignment indexes changed.',1;
'@)
    $assignmentVerified = Read-AssignmentMigrationData $assignmentPreflightSql
    Assert-ThreeAssignmentSlots $assignmentVerified
    $assignmentVerified.WriteXml((Join-Path $assignmentOutput 'verification-before-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $assignmentTransaction.Commit()
    $assignmentTransaction.Dispose(); $assignmentTransaction = $null
    $assignmentConnection.Close(); $assignmentConnection.Open()
    $assignmentPostCommit = Read-AssignmentMigrationData $assignmentPreflightSql
    Assert-ThreeAssignmentSlots $assignmentPostCommit
    $assignmentPostCommit.WriteXml((Join-Path $assignmentOutput 'verification-after-commit.xml'),[System.Data.XmlWriteMode]::WriteSchema)
    $assignmentReport = [ordered]@{
        Server=$assignmentConnection.DataSource; Database=$assignmentConnection.Database; VerifiedAtUtc=[DateTime]::UtcNow.ToString('o')
        Migration=$assignmentMigration; PreviouslyApplied=($assignmentBefore.Tables[1].Rows.Count -eq 1)
        PostCommitVerified=$true; ExistingAssignmentRowsPreserved=$true; ExistingAssignmentIndexesPreserved=$true
        PermittedSlots=@(1,2,3); AssignmentCount=[long]$assignmentPostCommit.Tables[2].Rows[0].AssignmentCount
        OpenAssignments=[long]$assignmentPostCommit.Tables[2].Rows[0].OpenAssignments
        ExistingRidersAboveNewCaps=[long]$assignmentPostCommit.Tables[4].Rows[0].RidersAboveNewCaps
        ScriptSha256=$assignmentHash; EvidenceDirectory=$assignmentOutput
    }
    $assignmentReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $assignmentOutput 'report.json') -Encoding UTF8
    $assignmentReport | ConvertTo-Json -Depth 5
} catch {
    if ($assignmentTransaction) { try { $assignmentTransaction.Rollback() } catch {} }
    throw
} finally {
    if ($assignmentTransaction) { $assignmentTransaction.Dispose() }
    $assignmentConnection.Dispose()
}
