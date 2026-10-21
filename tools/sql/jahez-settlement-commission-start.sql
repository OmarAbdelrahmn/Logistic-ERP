SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] DROP CONSTRAINT [CK_JahezMoney_Amount];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] DROP CONSTRAINT [CK_JahezMoney_Routing];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD [CommissionStartDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez_v2].[Operations] ADD CONSTRAINT [CK_JahezOperation_CommissionStart] CHECK ([CommissionStartDate] IS NULL OR [CommissionStartDate] <= [ThroughDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [CK_JahezMoney_Amount] CHECK ([Amount] > 0 OR ([Amount] = 0 AND [Direction] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [CK_JahezMoney_Routing] CHECK (([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2) OR ([Direction] = 0 AND [Amount] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL AND [Fund] IN (1, 2, 3)))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261018144507_AddJahezSettlementCommissionStart'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261018144507_AddJahezSettlementCommissionStart', N'10.0.11');
END;

COMMIT;
GO


