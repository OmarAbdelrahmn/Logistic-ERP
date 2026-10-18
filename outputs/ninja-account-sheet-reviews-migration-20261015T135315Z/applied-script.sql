BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE TABLE [ninja].[NinjaAccountUpdateUploads] (
        [Id] uniqueidentifier NOT NULL,
        [ClientPlatformId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [SheetName] nvarchar(31) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [Note] nvarchar(4000) NULL,
        [IgnoredFooterRows] int NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_NinjaAccountUpdateUploads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NinjaAccountUpdateUploads_ClientPlatforms_ClientPlatformId] FOREIGN KEY ([ClientPlatformId]) REFERENCES [platform].[ClientPlatforms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE TABLE [ninja].[NinjaAccountUpdateRows] (
        [Id] uniqueidentifier NOT NULL,
        [UploadId] uniqueidentifier NOT NULL,
        [SourceRowNumber] int NOT NULL,
        [CaptainId] nvarchar(150) NOT NULL,
        [SheetOwnerName] nvarchar(200) NOT NULL,
        [SheetCity] nvarchar(200) NOT NULL,
        [SheetIqamaNo] nvarchar(100) NOT NULL,
        [SheetStatus] nvarchar(100) NOT NULL,
        [SheetSuspended] bit NULL,
        [PlatformRiderAccountId] uniqueidentifier NULL,
        [MatchStatus] nvarchar(20) NOT NULL,
        [HasProblems] bit NOT NULL,
        [CityMismatch] bit NOT NULL,
        [IqamaProblem] bit NOT NULL,
        [NaqlBlocked] bit NOT NULL,
        [SystemSnapshotJson] nvarchar(max) NULL,
        [ProblemsJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_NinjaAccountUpdateRows] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NinjaAccountUpdateRows_MatchStatus] CHECK ([MatchStatus] IN ('Found', 'NotFound', 'Ambiguous')),
        CONSTRAINT [FK_NinjaAccountUpdateRows_NinjaAccountUpdateUploads_UploadId] FOREIGN KEY ([UploadId]) REFERENCES [ninja].[NinjaAccountUpdateUploads] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NinjaAccountUpdateRows_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE INDEX [IX_NinjaAccountUpdateRows_PlatformRiderAccountId_CreatedAtUtc_Id] ON [ninja].[NinjaAccountUpdateRows] ([PlatformRiderAccountId], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaAccountUpdateRows_UploadId_CaptainId] ON [ninja].[NinjaAccountUpdateRows] ([UploadId], [CaptainId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NinjaAccountUpdateRows_UploadId_SourceRowNumber] ON [ninja].[NinjaAccountUpdateRows] ([UploadId], [SourceRowNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE INDEX [IX_NinjaAccountUpdateUploads_ClientPlatformId_CreatedAtUtc_Id] ON [ninja].[NinjaAccountUpdateUploads] ([ClientPlatformId], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    CREATE INDEX [IX_NinjaAccountUpdateUploads_IsDeleted] ON [ninja].[NinjaAccountUpdateUploads] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261015134151_AddNinjaAccountSheetReviews'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015134151_AddNinjaAccountSheetReviews', N'10.0.11');
END;

COMMIT;
GO

