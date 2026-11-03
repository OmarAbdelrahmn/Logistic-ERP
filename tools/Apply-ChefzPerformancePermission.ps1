$ErrorActionPreference = 'Stop'
$config = Get-Content 'src/LogisticsERP.Api/appsettings.json' -Raw | ConvertFrom-Json
$text = if ($env:ConnectionStrings__LogisticsDatabase) { $env:ConnectionStrings__LogisticsDatabase } else { $config.ConnectionStrings.LogisticsDatabase }
$conn = [System.Data.SqlClient.SqlConnection]::new($text)
try {
    $conn.Open()
    if ($conn.Database -ne 'db67927' -or $conn.DataSource -ne 'db67927.public.databaseasp.net') { throw 'Unexpected database target.' }
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @id uniqueidentifier='019c18d5-62e1-7000-a000-000000000321', @key nvarchar(150)=N'chefz.performance.delete', @migration nvarchar(150)=N'20261103120000_AddChefzPerformanceDeletePermission';
IF EXISTS (SELECT 1 FROM platform.PermissionDefinitions WHERE (Id=@id AND [Key]<>@key) OR ([Key]=@key AND Id<>@id)) THROW 51000, 'Conflicting permission identity.', 1;
IF NOT EXISTS (SELECT 1 FROM platform.PermissionDefinitions WHERE Id=@id)
 INSERT INTO platform.PermissionDefinitions (Id,Category,CreatedAtUtc,DescriptionAr,DescriptionEn,DisplayOrder,GrantabilityRule,IsDeleted,IsDeprecated,IsHighTrust,IsSensitive,[Key],NameAr,NameEn,RequiresClientScope,RequiresHousingScope,Version)
 VALUES (@id,N'Chefz','2026-01-01T00:00:00+00:00',N'حذف أداء شيفز اليومي ليوم محدد لإعادة استيرادها.',N'Delete Chefz daily performance for a selected day for reimport.',321,N'HIGH_TRUST_ONLY',0,0,1,1,@key,N'حذف أداء شيفز اليومي',N'Delete Chefz daily performance',1,0,1);
IF NOT EXISTS (SELECT 1 FROM platform.PermissionDefinitions WHERE Id=@id AND [Key]=@key AND IsDeleted=0 AND IsDeprecated=0 AND IsHighTrust=1 AND IsSensitive=1 AND RequiresClientScope=1 AND GrantabilityRule=N'HIGH_TRUST_ONLY') THROW 51000, 'Permission definition does not match expected settings.', 1;
IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory WHERE MigrationId=@migration)
 INSERT INTO migration.__ApplicationMigrationsHistory (MigrationId,ProductVersion) VALUES (@migration,N'10.0.11');
COMMIT;
SELECT [Key],IsHighTrust,IsSensitive,RequiresClientScope FROM platform.PermissionDefinitions WHERE Id=@id;
'@
    $reader=$cmd.ExecuteReader()
    while ($reader.Read()) { Write-Output ("Verified: {0}, high trust={1}, sensitive={2}, client scope={3}" -f $reader.GetString(0),$reader.GetBoolean(1),$reader.GetBoolean(2),$reader.GetBoolean(3)) }
    $reader.Close()
} finally { $conn.Dispose() }
