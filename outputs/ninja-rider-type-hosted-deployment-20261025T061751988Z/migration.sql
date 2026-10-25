BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024150306_AddNinjaAssignmentRiderType'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] ADD [NinjaRiderType] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024150306_AddNinjaAssignmentRiderType'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [CK_RiderClientAssignments_NinjaRiderType] CHECK ([NinjaRiderType] IS NULL OR [NinjaRiderType] IN (''InternalCar'', ''ExternalCar'', ''InternalMotorcycle'', ''ExternalMotorcycle''))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024150306_AddNinjaAssignmentRiderType'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024150306_AddNinjaAssignmentRiderType', N'10.0.11');
END;

COMMIT;
GO

