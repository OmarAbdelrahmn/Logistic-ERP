BEGIN TRANSACTION;
ALTER TABLE [chefz].[WalletSettlements] ADD [FreelancerCommissionBase] decimal(18,2) NULL;

ALTER TABLE [chefz].[WalletSettlements] ADD [FullTimeDays] int NULL;

CREATE TABLE [chefz].[AccountTypeChanges] (
    [Id] uniqueidentifier NOT NULL,
    [PlatformRiderAccountId] uniqueidentifier NOT NULL,
    [PreviousType] int NOT NULL,
    [AccountType] int NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [CreatedAtUtc] datetimeoffset NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_AccountTypeChanges] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ChefzTypeChange_Types] CHECK ([PreviousType] IN (1, 2) AND [AccountType] IN (1, 2) AND [PreviousType] <> [AccountType]),
    CONSTRAINT [FK_AccountTypeChanges_PlatformRiderAccounts_PlatformRiderAccountId] FOREIGN KEY ([PlatformRiderAccountId]) REFERENCES [app].[PlatformRiderAccounts] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_AccountTypeChanges_PlatformRiderAccountId_EffectiveFrom] ON [chefz].[AccountTypeChanges] ([PlatformRiderAccountId], [EffectiveFrom]);

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261101140621_AddChefzAccountTypeHistory', N'10.0.11');

COMMIT;
GO

