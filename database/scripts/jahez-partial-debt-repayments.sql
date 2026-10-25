BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    SET XACT_ABORT ON;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    ALTER TABLE [jahez_v2].[TransferredDebts] DROP CONSTRAINT [CK_JahezTransferredDebt_Amount];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    DROP INDEX [IX_MoneyMovements_TransferredDebtId] ON [jahez_v2].[MoneyMovements];
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    ALTER TABLE [jahez_v2].[TransferredDebts] ADD [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD [CommissionPaid] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD [DashboardDebtPaid] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    IF EXISTS (
        SELECT 1 FROM jahez_v2.MoneyMovements m
        JOIN jahez_v2.TransferredDebts d ON d.Id = m.TransferredDebtId
        WHERE m.Direction <> 1 OR d.PaidAtUtc IS NULL OR m.Amount <> d.Amount
    )
        THROW 51000, 'Review inconsistent legacy Jahez debt receipts before migration.', 1;
    EXEC(N'UPDATE jahez_v2.TransferredDebts SET PaidAmount = Amount WHERE PaidAtUtc IS NOT NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez_v2].[TransferredDebts] ADD CONSTRAINT [CK_JahezTransferredDebt_Amount] CHECK ([Amount] >= 0 AND [PaidAmount] >= 0 AND [PaidAmount] <= [Amount] AND (([PaidAmount] = [Amount] AND [PaidAtUtc] IS NOT NULL) OR ([PaidAmount] < [Amount] AND [PaidAtUtc] IS NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_MoneyMovements_TransferredDebtId] ON [jahez_v2].[MoneyMovements] ([TransferredDebtId]) WHERE [TransferredDebtId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [CK_JahezMoney_DebtAllocation] CHECK (([DashboardDebtPaid] IS NULL AND [CommissionPaid] IS NULL) OR ([DashboardDebtPaid] IS NOT NULL AND [CommissionPaid] IS NOT NULL AND [TransferredDebtId] IS NOT NULL AND [Direction] = 1 AND [DashboardDebtPaid] >= 0 AND [CommissionPaid] >= 0 AND [DashboardDebtPaid] + [CommissionPaid] = [Amount]))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024115050_SupportJahezPartialDebtRepayments'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024115050_SupportJahezPartialDebtRepayments', N'10.0.11');
END;

COMMIT;
GO

