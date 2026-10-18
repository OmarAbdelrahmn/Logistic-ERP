IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DECLARE @disable nvarchar(max)=N'', @erase nvarchar(max)=N'';
    SELECT @disable += N'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N' NOCHECK CONSTRAINT ALL;'
    FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez');
    EXEC sys.sp_executesql @disable;
    SELECT @erase += N'DELETE FROM '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N';'
    FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez');
    EXEC sys.sp_executesql @erase;
    DELETE e FROM app.RiderAssignmentEvents e JOIN app.RiderClientAssignments r ON e.RiderClientAssignmentId=r.Id
        JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
    DELETE r FROM app.PlatformAccountRegistrations r JOIN platform.ClientPlatforms p ON r.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
    DELETE v FROM app.PlatformAccountCredentialVersions v JOIN app.PlatformRiderAccounts a ON v.PlatformRiderAccountId=a.Id
        JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
    DELETE r FROM app.RiderClientAssignments r JOIN app.PlatformRiderAccounts a ON r.PlatformRiderAccountId=a.Id
        JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
    UPDATE a SET UserName=NULL,Status=CASE WHEN a.Status=2 THEN 1 ELSE a.Status END,
        StatusReason=CASE WHEN a.Status=2 THEN NULL ELSE a.StatusReason END,UpdatedAtUtc=SYSUTCDATETIME()
        FROM app.PlatformRiderAccounts a JOIN platform.ClientPlatforms p ON a.ClientPlatformId=p.Id WHERE p.Code=N'JAHEZ';
    DELETE FROM app.Notifications WHERE EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountFee] DROP CONSTRAINT [FK_JahezAccountFee_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountFee] DROP CONSTRAINT [FK_JahezAccountFee_JahezApprovalRequest_ApprovalRequestId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalDecision] DROP CONSTRAINT [FK_JahezApprovalDecision_JahezApprovalRequest_RequestId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT [FK_JahezApprovalRequest_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT [FK_JahezApprovalRequest_JahezOperation_OperationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT [FK_JahezApprovalRequest_PlatformRiderAccounts_TargetAccountId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezCommissionPolicyPeriod] DROP CONSTRAINT [FK_JahezCommissionPolicyPeriod_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezCommissionPolicyPeriod] DROP CONSTRAINT [FK_JahezCommissionPolicyPeriod_JahezApprovalRequest_ApprovalRequestId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezDailyDispatch] DROP CONSTRAINT [FK_JahezDailyDispatch_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezDailyDispatch] DROP CONSTRAINT [FK_JahezDailyDispatch_JahezImportBatch_BatchId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezDailyDispatch] DROP CONSTRAINT [FK_JahezDailyDispatch_JahezImportRow_ImportRowId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezEarningsStatement] DROP CONSTRAINT [FK_JahezEarningsStatement_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezEarningsStatement] DROP CONSTRAINT [FK_JahezEarningsStatement_JahezEarningsStatement_SupersedesId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportBatch] DROP CONSTRAINT [FK_JahezImportBatch_JahezImportBatch_ReplacesBatchId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportFile] DROP CONSTRAINT [FK_JahezImportFile_JahezImportBatch_BatchId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportRow] DROP CONSTRAINT [FK_JahezImportRow_JahezImportFile_FileId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] DROP CONSTRAINT [FK_JahezLedgerEntry_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] DROP CONSTRAINT [FK_JahezLedgerEntry_JahezLedgerEntry_ReversesEntryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [FK_JahezMoneyMovement_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [FK_JahezMoneyMovement_JahezCashboxEntry_LegacyCashboxEntryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [FK_JahezMoneyMovement_JahezOperation_OperationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [FK_JahezMoneyMovement_JahezTransferredDebt_TransferredDebtId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [FK_JahezMoneyMovement_RiderProfiles_RiderProfileId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezOperation] DROP CONSTRAINT [FK_JahezOperation_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezOperation] DROP CONSTRAINT [FK_JahezOperation_JahezApprovalRequest_ApprovedRequestId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezReminderState] DROP CONSTRAINT [FK_JahezReminderState_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransaction] DROP CONSTRAINT [FK_JahezTransaction_JahezAccountHandover_HandoverId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransaction] DROP CONSTRAINT [FK_JahezTransaction_JahezImportBatch_BatchId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransaction] DROP CONSTRAINT [FK_JahezTransaction_JahezImportRow_ImportRowId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransferredDebt] DROP CONSTRAINT [FK_JahezTransferredDebt_JahezOperation_OperationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransferredDebt] DROP CONSTRAINT [FK_JahezTransferredDebt_RiderProfiles_RiderProfileId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DROP TABLE [jahez].[JahezCashboxEntry];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DROP TABLE [jahez].[JahezCashboxHandover];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DROP TABLE [jahez].[JahezRiderSettlement];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DROP TABLE [jahez].[JahezAccountHandover];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransferredDebt] DROP CONSTRAINT [PK_JahezTransferredDebt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezTransaction] DROP CONSTRAINT [PK_JahezTransaction];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezReminderState] DROP CONSTRAINT [PK_JahezReminderState];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezOperation] DROP CONSTRAINT [PK_JahezOperation];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT [PK_JahezMoneyMovement];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DROP INDEX [IX_JahezMoneyMovement_LegacyCashboxEntryId] ON [jahez].[JahezMoneyMovement];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezLedgerEntry] DROP CONSTRAINT [PK_JahezLedgerEntry];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportRow] DROP CONSTRAINT [PK_JahezImportRow];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportFile] DROP CONSTRAINT [PK_JahezImportFile];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezImportBatch] DROP CONSTRAINT [PK_JahezImportBatch];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezEarningsStatement] DROP CONSTRAINT [PK_JahezEarningsStatement];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezDailyDispatch] DROP CONSTRAINT [PK_JahezDailyDispatch];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezCommissionPolicyPeriod] DROP CONSTRAINT [PK_JahezCommissionPolicyPeriod];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezCommandReceipt] DROP CONSTRAINT [PK_JahezCommandReceipt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT [PK_JahezApprovalRequest];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezApprovalDecision] DROP CONSTRAINT [PK_JahezApprovalDecision];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez].[JahezAccountFee] DROP CONSTRAINT [PK_JahezAccountFee];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez].[JahezMoneyMovement]') AND [c].[name] = N'LegacyCashboxEntryId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [jahez].[JahezMoneyMovement] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [jahez].[JahezMoneyMovement] DROP COLUMN [LegacyCashboxEntryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez].[JahezApprovalRequest]') AND [c].[name] = N'ExternalResetReference');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP COLUMN [ExternalResetReference];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez].[JahezApprovalRequest]') AND [c].[name] = N'IsV2');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [jahez].[JahezApprovalRequest] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [jahez].[JahezApprovalRequest] DROP COLUMN [IsV2];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    IF SCHEMA_ID(N'jahez_v2') IS NULL EXEC(N'CREATE SCHEMA [jahez_v2];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezTransferredDebt]', N'TransferredDebts', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[TransferredDebts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezTransaction]', N'PlatformTransactions', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[PlatformTransactions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezReminderState]', N'SettlementReminders', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[SettlementReminders];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezOperation]', N'Operations', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[Operations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezMoneyMovement]', N'MoneyMovements', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[MoneyMovements];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezLedgerEntry]', N'AccountingEntries', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[AccountingEntries];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezImportRow]', N'ImportRows', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[ImportRows];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezImportFile]', N'ImportFiles', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[ImportFiles];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezImportBatch]', N'ImportBatches', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[ImportBatches];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezEarningsStatement]', N'EarningsStatements', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[EarningsStatements];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezDailyDispatch]', N'DailyOrders', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[DailyOrders];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezCommissionPolicyPeriod]', N'CommissionPolicies', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[CommissionPolicies];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezCommandReceipt]', N'CommandReceipts', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[CommandReceipts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezApprovalRequest]', N'Requests', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[Requests];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezApprovalDecision]', N'RequestDecisions', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[RequestDecisions];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez].[JahezAccountFee]', N'AccountFees', 'OBJECT';
    ALTER SCHEMA [jahez_v2] TRANSFER [jahez].[AccountFees];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[TransferredDebts].[IX_JahezTransferredDebt_RiderProfileId_PaidAtUtc]', N'IX_TransferredDebts_RiderProfileId_PaidAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[TransferredDebts].[IX_JahezTransferredDebt_OperationId]', N'IX_TransferredDebts_OperationId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[TransferredDebts].[IX_JahezTransferredDebt_IsDeleted]', N'IX_TransferredDebts_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[PlatformTransactions].[IX_JahezTransaction_ImportRowId]', N'IX_PlatformTransactions_ImportRowId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[PlatformTransactions].[IX_JahezTransaction_HandoverId_OccurredAtUtc]', N'IX_PlatformTransactions_HandoverId_OccurredAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[PlatformTransactions].[IX_JahezTransaction_BatchId]', N'IX_PlatformTransactions_BatchId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[SettlementReminders].[IX_JahezReminderState_IsDeleted]', N'IX_SettlementReminders_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[SettlementReminders].[IX_JahezReminderState_HandoverId_AnchorAtUtc]', N'IX_SettlementReminders_HandoverId_AnchorAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Operations].[IX_JahezOperation_IsDeleted]', N'IX_Operations_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Operations].[IX_JahezOperation_HandoverId_CreatedAtUtc]', N'IX_Operations_HandoverId_CreatedAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Operations].[IX_JahezOperation_HandoverId]', N'IX_Operations_HandoverId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Operations].[IX_JahezOperation_ApprovedRequestId]', N'IX_Operations_ApprovedRequestId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[MoneyMovements].[IX_JahezMoneyMovement_TransferredDebtId]', N'IX_MoneyMovements_TransferredDebtId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[MoneyMovements].[IX_JahezMoneyMovement_RiderProfileId]', N'IX_MoneyMovements_RiderProfileId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[MoneyMovements].[IX_JahezMoneyMovement_OperationId]', N'IX_MoneyMovements_OperationId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[MoneyMovements].[IX_JahezMoneyMovement_HandoverId]', N'IX_MoneyMovements_HandoverId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[MoneyMovements].[IX_JahezMoneyMovement_Fund_OccurredAtUtc]', N'IX_MoneyMovements_Fund_OccurredAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountingEntries].[IX_JahezLedgerEntry_SourceId_Bucket_Kind]', N'IX_AccountingEntries_SourceId_Bucket_Kind', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountingEntries].[IX_JahezLedgerEntry_ReversesEntryId]', N'IX_AccountingEntries_ReversesEntryId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountingEntries].[IX_JahezLedgerEntry_HandoverId_Bucket_OccurredAtUtc]', N'IX_AccountingEntries_HandoverId_Bucket_OccurredAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[ImportRows].[IX_JahezImportRow_FileId_RowNumber]', N'IX_ImportRows_FileId_RowNumber', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[ImportFiles].[IX_JahezImportFile_BatchId_ContentHash]', N'IX_ImportFiles_BatchId_ContentHash', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[ImportBatches].[IX_JahezImportBatch_ReplacesBatchId]', N'IX_ImportBatches_ReplacesBatchId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[ImportBatches].[IX_JahezImportBatch_Kind_ContentHash]', N'IX_ImportBatches_Kind_ContentHash', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[ImportBatches].[IX_JahezImportBatch_IsDeleted]', N'IX_ImportBatches_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[EarningsStatements].[IX_JahezEarningsStatement_SupersedesId]', N'IX_EarningsStatements_SupersedesId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[EarningsStatements].[IX_JahezEarningsStatement_HandoverId_FromDate_ToDate]', N'IX_EarningsStatements_HandoverId_FromDate_ToDate', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[DailyOrders].[IX_JahezDailyDispatch_ImportRowId_HandoverId]', N'IX_DailyOrders_ImportRowId_HandoverId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[DailyOrders].[IX_JahezDailyDispatch_HandoverId_Date]', N'IX_DailyOrders_HandoverId_Date', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[DailyOrders].[IX_JahezDailyDispatch_BatchId]', N'IX_DailyOrders_BatchId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[CommissionPolicies].[IX_JahezCommissionPolicyPeriod_HandoverId_FromDate_ToDate]', N'IX_CommissionPolicies_HandoverId_FromDate_ToDate', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[CommissionPolicies].[IX_JahezCommissionPolicyPeriod_ApprovalRequestId]', N'IX_CommissionPolicies_ApprovalRequestId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[CommandReceipts].[IX_JahezCommandReceipt_ActorUserId_Operation_CommandKey]', N'IX_CommandReceipts_ActorUserId_Operation_CommandKey', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Requests].[IX_JahezApprovalRequest_TargetAccountId]', N'IX_Requests_TargetAccountId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Requests].[IX_JahezApprovalRequest_Status_CreatedAtUtc]', N'IX_Requests_Status_CreatedAtUtc', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Requests].[IX_JahezApprovalRequest_OperationId]', N'IX_Requests_OperationId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Requests].[IX_JahezApprovalRequest_IsDeleted]', N'IX_Requests_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[Requests].[IX_JahezApprovalRequest_HandoverId]', N'IX_Requests_HandoverId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[RequestDecisions].[IX_JahezApprovalDecision_RequestId]', N'IX_RequestDecisions_RequestId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountFees].[IX_JahezAccountFee_IsDeleted]', N'IX_AccountFees_IsDeleted', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountFees].[IX_JahezAccountFee_HandoverId]', N'IX_AccountFees_HandoverId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC sp_rename N'[jahez_v2].[AccountFees].[IX_JahezAccountFee_ApprovalRequestId]', N'IX_AccountFees_ApprovalRequestId', 'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[TransferredDebts] ADD CONSTRAINT [PK_TransferredDebts] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [PK_PlatformTransactions] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[SettlementReminders] ADD CONSTRAINT [PK_SettlementReminders] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD CONSTRAINT [PK_Operations] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [PK_MoneyMovements] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountingEntries] ADD CONSTRAINT [PK_AccountingEntries] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportRows] ADD CONSTRAINT [PK_ImportRows] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportFiles] ADD CONSTRAINT [PK_ImportFiles] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportBatches] ADD CONSTRAINT [PK_ImportBatches] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[EarningsStatements] ADD CONSTRAINT [PK_EarningsStatements] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD CONSTRAINT [PK_DailyOrders] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[CommissionPolicies] ADD CONSTRAINT [PK_CommissionPolicies] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[CommandReceipts] ADD CONSTRAINT [PK_CommandReceipts] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [PK_Requests] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[RequestDecisions] ADD CONSTRAINT [PK_RequestDecisions] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountFees] ADD CONSTRAINT [PK_AccountFees] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    CREATE TABLE [jahez_v2].[AccountSessions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [RiderClientAssignmentId] uniqueidentifier NOT NULL,
        [StartedAtUtc] datetimeoffset NOT NULL,
        [EndedAtUtc] datetimeoffset NULL,
        [CommissionStartsOn] date NOT NULL,
        [CommissionPostedThrough] date NULL,
        [LastSettlementPaymentAtUtc] datetimeoffset NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(2000) NULL,
        CONSTRAINT [PK_AccountSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezHandover_Range] CHECK ([EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]),
        CONSTRAINT [FK_AccountSessions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountSessions_RiderClientAssignments_RiderClientAssignmentId] FOREIGN KEY ([RiderClientAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountSessions_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    CREATE INDEX [IX_AccountSessions_IsDeleted] ON [jahez_v2].[AccountSessions] ([IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AccountSessions_PlatformRiderAccountId] ON [jahez_v2].[AccountSessions] ([PlatformRiderAccountId]) WHERE [EndedAtUtc] IS NULL AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountSessions_RiderClientAssignmentId] ON [jahez_v2].[AccountSessions] ([RiderClientAssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    CREATE INDEX [IX_AccountSessions_RiderProfileId_StartedAtUtc] ON [jahez_v2].[AccountSessions] ([RiderProfileId], [StartedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountFees] ADD CONSTRAINT [FK_AccountFees_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountFees] ADD CONSTRAINT [FK_AccountFees_Requests_ApprovalRequestId] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [jahez_v2].[Requests] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountingEntries] ADD CONSTRAINT [FK_AccountingEntries_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[AccountingEntries] ADD CONSTRAINT [FK_AccountingEntries_AccountingEntries_ReversesEntryId] FOREIGN KEY ([ReversesEntryId]) REFERENCES [jahez_v2].[AccountingEntries] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[CommissionPolicies] ADD CONSTRAINT [FK_CommissionPolicies_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[CommissionPolicies] ADD CONSTRAINT [FK_CommissionPolicies_Requests_ApprovalRequestId] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [jahez_v2].[Requests] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD CONSTRAINT [FK_DailyOrders_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD CONSTRAINT [FK_DailyOrders_ImportBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez_v2].[ImportBatches] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[DailyOrders] ADD CONSTRAINT [FK_DailyOrders_ImportRows_ImportRowId] FOREIGN KEY ([ImportRowId]) REFERENCES [jahez_v2].[ImportRows] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[EarningsStatements] ADD CONSTRAINT [FK_EarningsStatements_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[EarningsStatements] ADD CONSTRAINT [FK_EarningsStatements_EarningsStatements_SupersedesId] FOREIGN KEY ([SupersedesId]) REFERENCES [jahez_v2].[EarningsStatements] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportBatches] ADD CONSTRAINT [FK_ImportBatches_ImportBatches_ReplacesBatchId] FOREIGN KEY ([ReplacesBatchId]) REFERENCES [jahez_v2].[ImportBatches] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportFiles] ADD CONSTRAINT [FK_ImportFiles_ImportBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez_v2].[ImportBatches] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[ImportRows] ADD CONSTRAINT [FK_ImportRows_ImportFiles_FileId] FOREIGN KEY ([FileId]) REFERENCES [jahez_v2].[ImportFiles] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [FK_MoneyMovements_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [FK_MoneyMovements_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez_v2].[Operations] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [FK_MoneyMovements_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [FK_MoneyMovements_TransferredDebts_TransferredDebtId] FOREIGN KEY ([TransferredDebtId]) REFERENCES [jahez_v2].[TransferredDebts] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD CONSTRAINT [FK_Operations_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Operations] ADD CONSTRAINT [FK_Operations_Requests_ApprovedRequestId] FOREIGN KEY ([ApprovedRequestId]) REFERENCES [jahez_v2].[Requests] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [FK_PlatformTransactions_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [FK_PlatformTransactions_ImportBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [jahez_v2].[ImportBatches] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [FK_PlatformTransactions_ImportRows_ImportRowId] FOREIGN KEY ([ImportRowId]) REFERENCES [jahez_v2].[ImportRows] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[RequestDecisions] ADD CONSTRAINT [FK_RequestDecisions_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [jahez_v2].[Requests] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [FK_Requests_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [FK_Requests_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez_v2].[Operations] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [FK_Requests_PlatformRiderAccounts_TargetAccountId] FOREIGN KEY ([TargetAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[SettlementReminders] ADD CONSTRAINT [FK_SettlementReminders_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[TransferredDebts] ADD CONSTRAINT [FK_TransferredDebts_Operations_OperationId] FOREIGN KEY ([OperationId]) REFERENCES [jahez_v2].[Operations] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    ALTER TABLE [jahez_v2].[TransferredDebts] ADD CONSTRAINT [FK_TransferredDebts_RiderProfiles_RiderProfileId] FOREIGN KEY ([RiderProfileId]) REFERENCES [app].[RiderProfiles] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    DECLARE @validate nvarchar(max)=N'';
    SELECT @validate += N'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+N'.'+QUOTENAME(name)+N' WITH CHECK CHECK CONSTRAINT ALL;'
    FROM sys.tables WHERE schema_id=SCHEMA_ID(N'jahez_v2');
    EXEC sys.sp_executesql @validate;
    IF EXISTS(SELECT 1 FROM sys.objects WHERE schema_id=SCHEMA_ID(N'jahez'))
        THROW 51000,'Old Jahez schema still contains objects.',1;
    IF SCHEMA_ID(N'jahez') IS NOT NULL EXEC(N'DROP SCHEMA [jahez]');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015080537_ResetJahezToAccountsOnlyV2'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015080537_ResetJahezToAccountsOnlyV2', N'10.0.11');
END;
GO

