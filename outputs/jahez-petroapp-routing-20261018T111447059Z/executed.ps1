param([switch]$Apply)

$ErrorActionPreference = 'Stop'
$routingRoot = Split-Path -Parent $PSScriptRoot
$routingConfig = Get-Content -LiteralPath (Join-Path $routingRoot 'src/LogisticsERP.Api/appsettings.json') -Raw | ConvertFrom-Json
$routingConnectionText = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $routingConfig.ConnectionStrings.LogisticsDatabase }
$routingConnection = [System.Data.SqlClient.SqlConnection]::new($routingConnectionText)
$routingTransaction = $null
$routingMigration = '20261018111059_AllowJahezPetroAppSettlementReceipts'

function Read-RoutingData([string]$Sql) {
    $routingCommand = $routingConnection.CreateCommand()
    $routingCommand.Transaction = $routingTransaction
    $routingCommand.CommandTimeout = 120
    $routingCommand.CommandText = $Sql
    try {
        $routingData = [System.Data.DataSet]::new()
        $routingAdapter = [System.Data.SqlClient.SqlDataAdapter]::new($routingCommand)
        try { [void]$routingAdapter.Fill($routingData) } finally { $routingAdapter.Dispose() }
        return ,$routingData
    } finally { $routingCommand.Dispose() }
}

$routingEvidenceSql = @'
SELECT COUNT_BIG(*) MovementCount,COALESCE(SUM(Amount),0) MovementAmount FROM jahez_v2.MoneyMovements;
SELECT COUNT_BIG(*) LedgerCount,COALESCE(SUM(Amount),0) LedgerAmount FROM jahez_v2.AccountingEntries;
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
 WHERE parent_object_id=OBJECT_ID('jahez_v2.MoneyMovements') AND name='CK_JahezMoney_Routing';
SELECT MigrationId FROM migration.__ApplicationMigrationsHistory
 WHERE MigrationId='20261018111059_AllowJahezPetroAppSettlementReceipts';
SELECT Id,Status,Net,Commission,Remaining,Fund,ExecutedAtUtc FROM jahez_v2.Operations
 WHERE Id='01a14e94-839d-7b17-a42a-a8ae2c9d6130';
'@

try {
    $routingConnection.Open()
    if ($routingConnection.DataSource -ne 'db67927.public.databaseasp.net' -or $routingConnection.Database -ne 'db67927') {
        throw 'Unexpected database target; refusing routing migration.'
    }
    $routingTransaction = $routingConnection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $routingBefore = Read-RoutingData $routingEvidenceSql
    if ($routingBefore.Tables[2].Rows.Count -ne 1 -or $routingBefore.Tables[2].Rows[0].is_disabled -or $routingBefore.Tables[2].Rows[0].is_not_trusted) {
        throw 'Expected one enabled and trusted routing constraint.'
    }
    if ($routingBefore.Tables[3].Rows.Count -eq 1) {
        $routingTransaction.Rollback()
        Write-Output 'Routing migration is already recorded; no changes.'
        return
    }
    if (!$Apply) {
        $routingTransaction.Rollback()
        Write-Output ('Preflight passed. Existing constraint: ' + $routingBefore.Tables[2].Rows[0].definition)
        Write-Output 'Read-only; no changes.'
        return
    }
    $routingScriptPath = Join-Path $routingRoot 'database/scripts/jahez-petroapp-settlement-receipts.sql'
    $routingScript = Get-Content -LiteralPath $routingScriptPath -Raw
    if (([regex]::Matches($routingScript,'(?im)^ALTER TABLE \[jahez_v2\]\.\[MoneyMovements\]')).Count -ne 2 -or
        ([regex]::Matches($routingScript,'(?im)^INSERT INTO')).Count -ne 1 -or
        ([regex]::Matches($routingScript,'(?im)^BEGIN TRANSACTION;')).Count -ne 1 -or
        ([regex]::Matches($routingScript,'(?im)^COMMIT;')).Count -ne 1 -or
        $routingScript -notmatch [regex]::Escape($routingMigration) -or
        $routingScript -match '(?im)^\s*(UPDATE|DELETE|TRUNCATE|CREATE|DROP)\b') {
        throw 'Unexpected routing migration script structure.'
    }
    $routingOutput = Join-Path $routingRoot ('outputs/jahez-petroapp-routing-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
    [void](New-Item -ItemType Directory -Path $routingOutput)
    $routingBefore.WriteXml((Join-Path $routingOutput 'before.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    Copy-Item -LiteralPath $routingScriptPath -Destination (Join-Path $routingOutput 'applied.sql')
    Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $routingOutput 'executed.ps1')
    $routingBody = $routingScript -replace '(?im)^BEGIN TRANSACTION;\s*$','' -replace '(?im)^COMMIT;\s*$','' -replace '(?im)^GO\s*$',''
    [void](Read-RoutingData $routingBody)
    $routingAfter = Read-RoutingData $routingEvidenceSql
    if ($routingAfter.Tables[3].Rows.Count -ne 1 -or $routingAfter.Tables[2].Rows.Count -ne 1 -or
        $routingAfter.Tables[2].Rows[0].is_disabled -or $routingAfter.Tables[2].Rows[0].is_not_trusted) {
        throw 'Updated routing constraint was not verified.'
    }
    $routingMatrix = Read-RoutingData @'
DECLARE @Definition nvarchar(max)=(SELECT definition FROM sys.check_constraints
 WHERE parent_object_id=OBJECT_ID('jahez_v2.MoneyMovements') AND name='CK_JahezMoney_Routing');
DECLARE @Verify nvarchar(max)=N'
SELECT Fund,Direction,Purpose,Expected,CASE WHEN '+@Definition+N' THEN 1 ELSE 0 END AS Allowed
INTO #RoutingVerification
FROM (VALUES (2,1,1,1),(2,1,2,1),(2,2,1,0),(2,2,2,0),(2,1,3,0),(2,1,4,0),
 (1,1,1,1),(1,2,1,1),(3,1,1,0),(3,2,1,1)) v(Fund,Direction,Purpose,Expected);
IF EXISTS(SELECT 1 FROM #RoutingVerification WHERE Expected<>Allowed)
 THROW 51000,''Routing acceptance matrix failed.'',1;
SELECT * FROM #RoutingVerification;';
EXEC sys.sp_executesql @Verify;
'@
    foreach ($routingTableIndex in @(0,1)) {
        for ($routingColumnIndex=0; $routingColumnIndex -lt 2; $routingColumnIndex++) {
            if ($routingBefore.Tables[$routingTableIndex].Rows[0][$routingColumnIndex] -ne $routingAfter.Tables[$routingTableIndex].Rows[0][$routingColumnIndex]) {
                throw 'Financial totals changed during a schema-only migration.'
            }
        }
    }
    $routingAfter.WriteXml((Join-Path $routingOutput 'after.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $routingMatrix.WriteXml((Join-Path $routingOutput 'routing-matrix.xml'), [System.Data.XmlWriteMode]::WriteSchema)
    $routingTransaction.Commit()
    Write-Output ('Committed routing migration; constraint enabled and trusted, financial totals unchanged. Evidence: ' + $routingOutput)
} catch {
    if ($routingTransaction -and $routingTransaction.Connection) { $routingTransaction.Rollback() }
    throw
} finally {
    if ($routingTransaction) { $routingTransaction.Dispose() }
    $routingConnection.Dispose()
}
