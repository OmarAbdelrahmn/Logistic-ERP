BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006155322_AllowLowerVehicleReturnOdometer'
)
BEGIN
    ALTER TABLE [app].[RiderVehicleAssignments] DROP CONSTRAINT [CK_RiderVehicleAssignments_Odometer];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006155322_AllowLowerVehicleReturnOdometer'
)
BEGIN
    EXEC(N'ALTER TABLE [app].[RiderVehicleAssignments] ADD CONSTRAINT [CK_RiderVehicleAssignments_Odometer] CHECK ([StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261006155322_AllowLowerVehicleReturnOdometer'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006155322_AllowLowerVehicleReturnOdometer', N'10.0.11');
END;

COMMIT;
GO

