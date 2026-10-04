-- AddFuelCardOperatingCity only; the preceding application migration must already be applied.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET LOCK_TIMEOUT 15000;
IF NOT EXISTS (SELECT 1 FROM [migration].[__ApplicationMigrationsHistory] WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType')
    THROW 51000, 'Apply the preceding application migration before adding fuel-card cities.', 1;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    ALTER TABLE [app].[FuelCards] ADD [OperatingCityId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [app].[OperatingCities] WHERE [Id] = '019c18d5-62e1-7000-8000-000000000003')
        THROW 51000, 'The Jeddah operating city must exist before assigning fuel cards.', 1;

    EXEC(N'UPDATE [app].[FuelCards]
        SET [OperatingCityId] = ''019c18d5-62e1-7000-8000-000000000003''
        WHERE [OperatingCityId] IS NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[app].[FuelCards]') AND [c].[name] = N'OperatingCityId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [app].[FuelCards] DROP CONSTRAINT ' + @var + ';');
    EXEC(N'UPDATE [app].[FuelCards] SET [OperatingCityId] = ''019c18d5-62e1-7000-8000-000000000003'' WHERE [OperatingCityId] IS NULL');
    ALTER TABLE [app].[FuelCards] ALTER COLUMN [OperatingCityId] uniqueidentifier NOT NULL;
    ALTER TABLE [app].[FuelCards] ADD DEFAULT '019c18d5-62e1-7000-8000-000000000003' FOR [OperatingCityId];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    CREATE INDEX [IX_FuelCards_OperatingCityId] ON [app].[FuelCards] ([OperatingCityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    ALTER TABLE [app].[FuelCards] ADD CONSTRAINT [FK_FuelCards_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001193221_AddFuelCardOperatingCity'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001193221_AddFuelCardOperatingCity', N'10.0.11');
END;

COMMIT;
GO

