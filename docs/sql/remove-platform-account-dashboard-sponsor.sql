BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261004092536_RemovePlatformAccountDashboardSponsor'
)
BEGIN
    ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT [FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261004092536_RemovePlatformAccountDashboardSponsor'
)
BEGIN
    DROP INDEX [IX_PlatformRiderAccounts_DashboardSponsorId] ON [app].[PlatformRiderAccounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261004092536_RemovePlatformAccountDashboardSponsor'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[PlatformRiderAccounts]') AND [c].[name] = N'DashboardSponsorId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [app].[PlatformRiderAccounts] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [app].[PlatformRiderAccounts] DROP COLUMN [DashboardSponsorId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261004092536_RemovePlatformAccountDashboardSponsor'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004092536_RemovePlatformAccountDashboardSponsor', N'10.0.11');
END;

COMMIT;
GO

