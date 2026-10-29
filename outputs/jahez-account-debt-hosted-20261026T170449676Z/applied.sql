DROP INDEX [IX_ImportBatches_Kind_ContentHash] ON [jahez_v2].[ImportBatches];
GO

DROP INDEX [IX_ImportBatches_ReplacesBatchId] ON [jahez_v2].[ImportBatches];
GO

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez_v2].[Requests]') AND [c].[name] = N'HandoverId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [jahez_v2].[Requests] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [jahez_v2].[Requests] ALTER COLUMN [HandoverId] uniqueidentifier NULL;
GO

ALTER TABLE [jahez_v2].[Requests] ADD [AccountId] uniqueidentifier NULL;
GO

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez_v2].[PlatformTransactions]') AND [c].[name] = N'HandoverId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [jahez_v2].[PlatformTransactions] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [jahez_v2].[PlatformTransactions] ALTER COLUMN [HandoverId] uniqueidentifier NULL;
GO

ALTER TABLE [jahez_v2].[PlatformTransactions] ADD [AccountId] uniqueidentifier NULL;
GO

ALTER TABLE [jahez_v2].[PlatformTransactions] ADD [SupersededByBatchId] uniqueidentifier NULL;
GO

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[jahez_v2].[AccountingEntries]') AND [c].[name] = N'HandoverId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [jahez_v2].[AccountingEntries] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [jahez_v2].[AccountingEntries] ALTER COLUMN [HandoverId] uniqueidentifier NULL;
GO

ALTER TABLE [jahez_v2].[AccountingEntries] ADD [AccountId] uniqueidentifier NULL;
GO

UPDATE r SET AccountId = h.PlatformRiderAccountId
FROM jahez_v2.Requests r JOIN jahez_v2.AccountSessions h ON h.Id = r.HandoverId;
UPDATE e SET AccountId = h.PlatformRiderAccountId
FROM jahez_v2.AccountingEntries e JOIN jahez_v2.AccountSessions h ON h.Id = e.HandoverId;
UPDATE t SET AccountId = h.PlatformRiderAccountId
FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.AccountSessions h ON h.Id = t.HandoverId;
UPDATE t SET SupersededByBatchId = b.Id
FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.ImportBatches b ON b.ReplacesBatchId = t.BatchId
WHERE b.CommittedAtUtc IS NOT NULL AND b.IsDeleted = 0;
GO

CREATE INDEX [IX_Requests_AccountId] ON [jahez_v2].[Requests] ([AccountId]);
GO

ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [CK_JahezRequest_AccountScope] CHECK ([HandoverId] IS NOT NULL OR ([AccountId] IS NOT NULL AND [Kind] = 5 AND [AdjustmentTarget] IS NOT NULL AND [AdjustmentTarget] = 2));
GO

CREATE INDEX [IX_PlatformTransactions_AccountId_OccurredAtUtc] ON [jahez_v2].[PlatformTransactions] ([AccountId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_PlatformTransactions_SupersededByBatchId] ON [jahez_v2].[PlatformTransactions] ([SupersededByBatchId]);
GO

ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [CK_JahezTransaction_AccountScope] CHECK ([AccountId] IS NOT NULL OR [HandoverId] IS NOT NULL);
GO

CREATE INDEX [IX_ImportBatches_Kind_ContentHash] ON [jahez_v2].[ImportBatches] ([Kind], [ContentHash]);
GO

CREATE UNIQUE INDEX [IX_ImportBatches_ReplacesBatchId] ON [jahez_v2].[ImportBatches] ([ReplacesBatchId]) WHERE [ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL AND [Kind] = 2;
GO

CREATE UNIQUE INDEX [UX_JahezDailyImport_ContentHash] ON [jahez_v2].[ImportBatches] ([Kind], [ContentHash]) WHERE [Kind] = 2;
GO

CREATE INDEX [IX_AccountingEntries_AccountId] ON [jahez_v2].[AccountingEntries] ([AccountId]);
GO

ALTER TABLE [jahez_v2].[AccountingEntries] ADD CONSTRAINT [CK_JahezLedger_AccountScope] CHECK ([HandoverId] IS NOT NULL OR ([AccountId] IS NOT NULL AND [Bucket] = 2));
GO

ALTER TABLE [jahez_v2].[AccountingEntries] ADD CONSTRAINT [FK_AccountingEntries_PlatformRiderAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [FK_PlatformTransactions_ImportBatches_SupersededByBatchId] FOREIGN KEY ([SupersededByBatchId]) REFERENCES [jahez_v2].[ImportBatches] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [jahez_v2].[PlatformTransactions] ADD CONSTRAINT [FK_PlatformTransactions_PlatformRiderAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [jahez_v2].[Requests] ADD CONSTRAINT [FK_Requests_PlatformRiderAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261026165219_MakeJahezAdministrativeDebtAccountBased', N'10.0.11');
GO

