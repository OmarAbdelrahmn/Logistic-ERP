BEGIN TRANSACTION;
DROP INDEX [IX_WalletReviews_PlatformRiderAccountId] ON [hunger].[WalletReviews];

ALTER TABLE [hunger].[WalletReviews] ADD [Status] nvarchar(20) NOT NULL DEFAULT N'Pending';

ALTER TABLE [hunger].[WalletReviews] ADD [SwapId] uniqueidentifier NULL;

UPDATE [hunger].[WalletReviews] SET [Status] = CASE WHEN [ResultJson] IS NOT NULL THEN 'Confirmed' ELSE 'Superseded' END;

CREATE INDEX [IX_WalletReviews_PlatformRiderAccountId_Status] ON [hunger].[WalletReviews] ([PlatformRiderAccountId], [Status]);

CREATE INDEX [IX_WalletReviews_SwapId] ON [hunger].[WalletReviews] ([SwapId]);

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261106090616_HungerWalletReviewIntegrityAndRiderSwap', N'10.0.11');

COMMIT;
GO

