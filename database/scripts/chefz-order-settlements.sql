-- Apply after AddChefzWorkbookReportsAndStatistics. No source orders or sample data are inserted.
SET XACT_ABORT ON;
IF NOT EXISTS (SELECT 1 FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011191524_AddChefzWorkbookReportsAndStatistics')
    THROW 51000, 'Apply the preceding Chefz workbook migration before this script.', 1;

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] DROP CONSTRAINT [CK_ChefzSettlement_Amounts];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [Bonuses] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [Compensation] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [Deductions] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [DeliveryFees] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [DriverId] nvarchar(100) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [ImportId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [RiderName] nvarchar(500) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [Source] nvarchar(20) NOT NULL DEFAULT N'Wallet';
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [SourceRows] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [Tips] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD [VatAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    CREATE TABLE [chefz].[OrderSettlementImports] (
        [Id] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [SourceRows] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_OrderSettlementImports] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChefzOrderImport_Rows] CHECK ([SourceRows] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    CREATE INDEX [IX_WalletSettlements_ImportId] ON [chefz].[WalletSettlements] ([ImportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    EXEC(N'ALTER TABLE [chefz].[WalletSettlements] ADD CONSTRAINT [CK_ChefzSettlement_Amounts] CHECK ((([Source] = ''Wallet'' AND [ImportId] IS NULL AND [StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount]) OR ([Source] = ''Orders'' AND [ImportId] IS NOT NULL AND [SourceRows] > 0 AND [StartWalletAmount] = 0 AND [EndWalletAmount] = 0 AND [Deductions] >= 0 AND [GrossEarnings] = [DeliveryFees] - [VatAmount] + [Tips] + [Compensation] + [Bonuses] - [Deductions])) AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderSettlementImports_ContentHash] ON [chefz].[OrderSettlementImports] ([ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    ALTER TABLE [chefz].[WalletSettlements] ADD CONSTRAINT [FK_WalletSettlements_OrderSettlementImports_ImportId] FOREIGN KEY ([ImportId]) REFERENCES [chefz].[OrderSettlementImports] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261012093719_AddChefzOrderSettlementImports'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012093719_AddChefzOrderSettlementImports', N'10.0.11');
END;

COMMIT;
GO

