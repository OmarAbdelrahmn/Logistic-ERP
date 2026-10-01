BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType'
)
BEGIN
    DROP INDEX [IX_OilBarrels_InventoryLocationId_InventoryItemId] ON [maintenance].[OilBarrels];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrels] ADD [AllowedVehicleType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OilBarrels_InventoryLocationId_InventoryItemId_AllowedVehicleType] ON [maintenance].[OilBarrels] ([InventoryLocationId], [InventoryItemId], [AllowedVehicleType]) WHERE [Status] = 2 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrels] ADD CONSTRAINT [CK_OilBarrels_AllowedVehicleType] CHECK ([AllowedVehicleType] IS NULL OR [AllowedVehicleType] IN (1, 2))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261001073011_RestrictOilBarrelsByVehicleType'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001073011_RestrictOilBarrelsByVehicleType', N'10.0.11');
END;

COMMIT;
GO

