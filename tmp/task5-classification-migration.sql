BEGIN TRANSACTION;
ALTER TABLE [hunger].[DailyOperations] ADD [CaptainClassification] nvarchar(100) NULL;

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106143740_HungerSourceCaptainClassification', N'10.0.11');

COMMIT;
GO

