BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE TABLE [jahez_v2].[CashboxHandovers] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [Status] int NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [CashboxAmount] decimal(18,2) NOT NULL,
        [PetroAppAmount] decimal(18,2) NOT NULL,
        [BankAmount] decimal(18,2) NOT NULL,
        [MovementCount] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [AccountantUserId] uniqueidentifier NULL,
        [ConfirmedAtUtc] datetimeoffset NULL,
        [ConfirmationReason] nvarchar(2000) NULL,
        [CancelledAtUtc] datetimeoffset NULL,
        [CancellationReason] nvarchar(2000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DeletionReason] nvarchar(500) NULL,
        CONSTRAINT [PK_CashboxHandovers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezCashboxHandover_Count] CHECK ([MovementCount] > 0),
        CONSTRAINT [CK_JahezCashboxHandover_State] CHECK ([IsDeleted] = 0 AND (([Status] = 1 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [AccountantUserId] IS NOT NULL AND [AccountantUserId] <> [RequestedByUserId] AND [ConfirmedAtUtc] IS NOT NULL AND [CancelledAtUtc] IS NULL) OR ([Status] = 3 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NOT NULL)))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE TABLE [jahez_v2].[CashboxHandoverMovements] (
        [Id] uniqueidentifier NOT NULL,
        [CashboxHandoverId] uniqueidentifier NOT NULL,
        [MoneyMovementId] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_CashboxHandoverMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashboxHandoverMovements_CashboxHandovers_CashboxHandoverId] FOREIGN KEY ([CashboxHandoverId]) REFERENCES [jahez_v2].[CashboxHandovers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CashboxHandoverMovements_MoneyMovements_MoneyMovementId] FOREIGN KEY ([MoneyMovementId]) REFERENCES [jahez_v2].[MoneyMovements] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CashboxHandoverMovements_CashboxHandoverId_MoneyMovementId] ON [jahez_v2].[CashboxHandoverMovements] ([CashboxHandoverId], [MoneyMovementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE INDEX [IX_CashboxHandoverMovements_MoneyMovementId] ON [jahez_v2].[CashboxHandoverMovements] ([MoneyMovementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE INDEX [IX_CashboxHandovers_BusinessDate_Id] ON [jahez_v2].[CashboxHandovers] ([BusinessDate], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    CREATE INDEX [IX_CashboxHandovers_IsDeleted] ON [jahez_v2].[CashboxHandovers] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CashboxHandovers_Status] ON [jahez_v2].[CashboxHandovers] ([Status]) WHERE [Status] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261017165534_AddJahezV2AccountantHandovers'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261017165534_AddJahezV2AccountantHandovers', N'10.0.11');
END;

COMMIT;
GO

