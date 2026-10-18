BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrels] DROP CONSTRAINT [CK_OilBarrels_AllowedVehicleType];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrels] DROP CONSTRAINT [CK_OilBarrels_Quantities];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] DROP CONSTRAINT [CK_OilBarrelLosses_Values];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrels] ADD [AllowBothVehicleTypes] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrels] ADD [RecordedMissingLiters] decimal(18,3) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] ADD [MissingReason] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] ADD [ResponsibleInventoryLocationId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrels] ADD [OpenCarSlot] AS CASE WHEN [AllowBothVehicleTypes] = 1 OR [AllowedVehicleType] IS NULL OR [AllowedVehicleType] = 2 THEN 1 ELSE NULL END PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrels] ADD [OpenMotorcycleSlot] AS CASE WHEN [AllowBothVehicleTypes] = 1 OR [AllowedVehicleType] IS NULL OR [AllowedVehicleType] = 1 THEN 1 ELSE NULL END PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenCarSlot] ON [maintenance].[OilBarrels] ([InventoryLocationId], [InventoryItemId], [OpenCarSlot]) WHERE [Status] = 2 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenMotorcycleSlot] ON [maintenance].[OilBarrels] ([InventoryLocationId], [InventoryItemId], [OpenMotorcycleSlot]) WHERE [Status] = 2 AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrels] ADD CONSTRAINT [CK_OilBarrels_AllowedVehicleType] CHECK (([AllowBothVehicleTypes] = 1 AND [AllowedVehicleType] IS NULL) OR ([AllowBothVehicleTypes] = 0 AND ([AllowedVehicleType] IS NULL OR [AllowedVehicleType] IN (1, 2))))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrels] ADD CONSTRAINT [CK_OilBarrels_Quantities] CHECK ([NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters] AND [RecordedMissingLiters] >= 0 AND [RemainingLiters] + [RecordedLossLiters] + [RecordedMissingLiters] <= [NominalCapacityLiters])');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    CREATE INDEX [IX_OilBarrelLosses_ResponsibleInventoryLocationId] ON [maintenance].[OilBarrelLosses] ([ResponsibleInventoryLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    EXEC(N'ALTER TABLE [maintenance].[OilBarrelLosses] ADD CONSTRAINT [CK_OilBarrelLosses_Values] CHECK ([QuantityLiters] > 0 AND [CostAmount] >= 0 AND (([MissingReason] IS NULL AND [ResponsibleInventoryLocationId] IS NULL) OR ([MissingReason] IS NOT NULL AND [MissingReason] IN (1, 2, 3) AND [ResponsibleInventoryLocationId] IS NOT NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    ALTER TABLE [maintenance].[OilBarrelLosses] ADD CONSTRAINT [FK_OilBarrelLosses_InventoryLocations_ResponsibleInventoryLocationId] FOREIGN KEY ([ResponsibleInventoryLocationId]) REFERENCES [maintenance].[InventoryLocations] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010122809_SupportWarehouseOilWriteOffsAndSharedBarrels', N'10.0.11');
END;

COMMIT;
GO

