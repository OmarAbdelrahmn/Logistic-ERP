BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261029071925_MoveDayContributionsToAssignment'
)
BEGIN
    CREATE TABLE [app].[PlatformAccountDayContributions] (
        [Id] uniqueidentifier NOT NULL,
        [PlatformRiderAccountId] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [RiderProfileId] uniqueidentifier NOT NULL,
        [RepresentativeAssignmentId] uniqueidentifier NOT NULL,
        [AssignmentIdsJson] nvarchar(max) NOT NULL,
        [FiguresJson] nvarchar(max) NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_PlatformAccountDayContributions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlatformAccountDayContributions_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlatformAccountDayContributions_RiderClientAssignments_RepresentativeAssignmentId] FOREIGN KEY ([RepresentativeAssignmentId]) REFERENCES [app].[RiderClientAssignments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261029071925_MoveDayContributionsToAssignment'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountDayContributions_IsDeleted] ON [app].[PlatformAccountDayContributions] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261029071925_MoveDayContributionsToAssignment'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PlatformAccountDayContributions_PlatformRiderAccountId_BusinessDate_RepresentativeAssignmentId] ON [app].[PlatformAccountDayContributions] ([PlatformRiderAccountId], [BusinessDate], [RepresentativeAssignmentId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261029071925_MoveDayContributionsToAssignment'
)
BEGIN
    CREATE INDEX [IX_PlatformAccountDayContributions_RepresentativeAssignmentId] ON [app].[PlatformAccountDayContributions] ([RepresentativeAssignmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261029071925_MoveDayContributionsToAssignment'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029071925_MoveDayContributionsToAssignment', N'10.0.11');
END;

COMMIT;
GO

