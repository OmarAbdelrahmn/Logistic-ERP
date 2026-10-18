BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountHandover] ADD [AccountClearedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountHandover] ADD [DebtPaidAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountHandover] ADD [DebtStatus] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountHandover] ADD [ExternalClearanceReference] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountHandover] ADD [FrozenAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    EXEC(N'UPDATE [jahez].[JahezAccountHandover]
    SET [DebtStatus] = CASE WHEN [DebtTransferred] = 1 THEN 3 WHEN [EndedAtUtc] IS NOT NULL THEN 2 ELSE 1 END,
        [AccountClearedAtUtc] = CASE WHEN [DebtTransferred] = 1 THEN [EndedAtUtc] ELSE NULL END;');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez].[JahezLedgerEntry] ADD CONSTRAINT [CK_JahezLedger_StatusAmount] CHECK ([Kind] <> 6 OR [Amount] = 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez].[JahezAccountHandover] ADD CONSTRAINT [CK_JahezHandover_DebtStatus] CHECK ([DebtStatus] BETWEEN 1 AND 5 AND ([DebtStatus] <> 4 OR ([FrozenAtUtc] IS NOT NULL AND [EndedAtUtc] IS NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    EXEC(N'ALTER TABLE [jahez].[JahezAccountHandover] ADD CONSTRAINT [CK_JahezHandover_FrozenRange] CHECK ([FrozenAtUtc] IS NULL OR ([FrozenAtUtc] >= [StartedAtUtc] AND ([EndedAtUtc] IS NULL OR [EndedAtUtc] >= [FrozenAtUtc])))');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261011122817_AddJahezDebtFreezeAndClearance'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011122817_AddJahezDebtFreezeAndClearance', N'10.0.11');
END;

COMMIT;
GO

