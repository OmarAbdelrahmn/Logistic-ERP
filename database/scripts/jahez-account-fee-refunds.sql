BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    CREATE TABLE [jahez_v2].[AccountFeeRefunds] (
        [Id] uniqueidentifier NOT NULL,
        [HandoverId] uniqueidentifier NOT NULL,
        [AccountFeeId] uniqueidentifier NOT NULL,
        [MoneyMovementId] uniqueidentifier NOT NULL,
        [GrossFeeRefund] decimal(18,2) NOT NULL,
        [CancelledUnpaidFee] decimal(18,2) NOT NULL,
        [WaiveCommission] bit NOT NULL,
        [CalculatedCommission] decimal(18,2) NOT NULL,
        [DeductedCommission] decimal(18,2) NOT NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [CalculationJson] nvarchar(max) NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [Reference] nvarchar(500) NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AccountFeeRefunds] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JahezFeeRefund_Amounts] CHECK ([GrossFeeRefund] > 0 AND [CancelledUnpaidFee] >= 0 AND [CalculatedCommission] >= 0 AND [DeductedCommission] >= 0 AND [RefundAmount] > 0 AND [RefundAmount] = [GrossFeeRefund] - [DeductedCommission] AND (([WaiveCommission] = 1 AND [DeductedCommission] = 0) OR ([WaiveCommission] = 0 AND [DeductedCommission] = [CalculatedCommission]))),
        CONSTRAINT [FK_AccountFeeRefunds_AccountFees_AccountFeeId] FOREIGN KEY ([AccountFeeId]) REFERENCES [jahez_v2].[AccountFees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountFeeRefunds_AccountSessions_HandoverId] FOREIGN KEY ([HandoverId]) REFERENCES [jahez_v2].[AccountSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountFeeRefunds_MoneyMovements_MoneyMovementId] FOREIGN KEY ([MoneyMovementId]) REFERENCES [jahez_v2].[MoneyMovements] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountFeeRefunds_AccountFeeId] ON [jahez_v2].[AccountFeeRefunds] ([AccountFeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountFeeRefunds_HandoverId] ON [jahez_v2].[AccountFeeRefunds] ([HandoverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountFeeRefunds_MoneyMovementId] ON [jahez_v2].[AccountFeeRefunds] ([MoneyMovementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    CREATE INDEX [IX_AccountFeeRefunds_OccurredAtUtc] ON [jahez_v2].[AccountFeeRefunds] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [migration].[__ApplicationMigrationsHistory]
    WHERE [MigrationId] = N'20261024132700_AddJahezAccountFeeRefunds'
)
BEGIN
    INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024132700_AddJahezAccountFeeRefunds', N'10.0.11');
END;

COMMIT;
GO

