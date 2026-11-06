BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[hunger].[DailyOperations]') AND [c].[name] = N'ActualWorkingHours');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [hunger].[DailyOperations] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [hunger].[DailyOperations] ALTER COLUMN [ActualWorkingHours] decimal(18,8) NULL;

ALTER TABLE [hunger].[DailyOperations] ADD [AcceptedOrders] int NULL;

ALTER TABLE [hunger].[DailyOperations] ADD [HoursUnit] nvarchar(32) NOT NULL DEFAULT N'DecimalHours';

ALTER TABLE [hunger].[DailyOperations] ADD [StackedOrders] int NULL;

ALTER TABLE [hunger].[AlertRules] ADD [EffectiveThrough] date NULL;

ALTER TABLE [hunger].[AlertRules] ADD [Kind] nvarchar(16) NOT NULL DEFAULT N'Alert';

ALTER TABLE [hunger].[AlertRules] ADD [Metric] nvarchar(50) NOT NULL DEFAULT N'WorkingHours';

ALTER TABLE [hunger].[AlertRules] ADD [OperatingCityId] uniqueidentifier NULL;

ALTER TABLE [hunger].[AlertRules] ADD [Operator] nvarchar(32) NOT NULL DEFAULT N'LessThan';

ALTER TABLE [hunger].[AlertRules] ADD [Period] nvarchar(16) NOT NULL DEFAULT N'Day';

CREATE TABLE [hunger].[PotentialFines] (
    [Id] uniqueidentifier NOT NULL,
    [DailyOperationId] uniqueidentifier NOT NULL,
    [NoShowCount] int NOT NULL,
    [Status] nvarchar(32) NOT NULL,
    [InvoiceDocumentId] uniqueidentifier NULL,
    [InvoiceSourceKey] nvarchar(200) NULL,
    [ResolutionEvidence] nvarchar(1000) NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [UpdatedAtUtc] datetimeoffset NULL,
    [UpdatedByUserId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAtUtc] datetimeoffset NULL,
    [DeletedByUserId] uniqueidentifier NULL,
    [DeletionReason] nvarchar(500) NULL,
    CONSTRAINT [PK_PotentialFines] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_HungerPotentialFine_Status] CHECK ([NoShowCount]>0 AND [Status] IN ('Potential','Invoiced','Waived')),
    CONSTRAINT [FK_PotentialFines_DailyOperations_DailyOperationId] FOREIGN KEY ([DailyOperationId]) REFERENCES [hunger].[DailyOperations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PotentialFines_SettlementDocuments_InvoiceDocumentId] FOREIGN KEY ([InvoiceDocumentId]) REFERENCES [hunger].[SettlementDocuments] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AlertRules_OperatingCityId] ON [hunger].[AlertRules] ([OperatingCityId]);

CREATE UNIQUE INDEX [IX_PotentialFines_DailyOperationId] ON [hunger].[PotentialFines] ([DailyOperationId]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_PotentialFines_InvoiceDocumentId] ON [hunger].[PotentialFines] ([InvoiceDocumentId]);

CREATE INDEX [IX_PotentialFines_IsDeleted] ON [hunger].[PotentialFines] ([IsDeleted]);

ALTER TABLE [hunger].[AlertRules] ADD CONSTRAINT [FK_AlertRules_OperatingCities_OperatingCityId] FOREIGN KEY ([OperatingCityId]) REFERENCES [app].[OperatingCities] ([Id]) ON DELETE NO ACTION;

UPDATE [hunger].[AlertRules]
SET [Metric] = 'NoShowCount', [Operator] = 'GreaterThan'
WHERE [Code] = 'NoShowPositive';
INSERT INTO [hunger].[PotentialFines]
    ([Id], [DailyOperationId], [NoShowCount], [Status], [CreatedAtUtc], [CreatedByUserId], [IsDeleted])
SELECT NEWID(), [Id], [NoShowCount], 'Potential', [CreatedAtUtc], [CreatedByUserId], 0
FROM [hunger].[DailyOperations]
WHERE [IsDeleted] = 0 AND [NoShowCount] > 0;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106131847_HungerPerformanceCatalogAndPotentialFines', N'10.0.11');

COMMIT;
GO

