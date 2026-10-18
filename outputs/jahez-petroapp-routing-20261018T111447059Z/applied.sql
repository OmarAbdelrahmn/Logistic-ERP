BEGIN TRANSACTION;
ALTER TABLE [jahez_v2].[MoneyMovements] DROP CONSTRAINT [CK_JahezMoney_Routing];

ALTER TABLE [jahez_v2].[MoneyMovements] ADD CONSTRAINT [CK_JahezMoney_Routing] CHECK (([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2));

INSERT INTO [migration].[__ApplicationMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261018111059_AllowJahezPetroAppSettlementReceipts', N'10.0.11');

COMMIT;
GO

