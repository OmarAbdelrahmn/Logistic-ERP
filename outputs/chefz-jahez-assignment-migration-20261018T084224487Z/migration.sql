BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018083620_AllowChefzAndJahezRiderAssignments'
)
BEGIN
    ALTER TABLE [app].[RiderClientAssignments] DROP CONSTRAINT [CK_RiderClientAssignments_RiderAccountSlot];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018083620_AllowChefzAndJahezRiderAssignments'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[RiderClientAssignments] ADD CONSTRAINT [CK_RiderClientAssignments_RiderAccountSlot] CHECK ([RiderAccountSlot] IN (1, 2, 3))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018083620_AllowChefzAndJahezRiderAssignments'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018083620_AllowChefzAndJahezRiderAssignments', N'10.0.11');
END;

COMMIT;
GO

