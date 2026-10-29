IF NOT EXISTS (SELECT 1 FROM migration.__ApplicationMigrationsHistory
    WHERE MigrationId=N'20261026165219_MakeJahezAdministrativeDebtAccountBased')
    THROW 51000,'Administrative debt migration is not recorded.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id IN
    (OBJECT_ID(N'jahez_v2.Requests'),OBJECT_ID(N'jahez_v2.AccountingEntries'),OBJECT_ID(N'jahez_v2.PlatformTransactions'))
    AND name=N'HandoverId' AND is_nullable=1)<>3
    THROW 51000,'Debt handover columns must be optional.',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id IN
    (OBJECT_ID(N'jahez_v2.Requests'),OBJECT_ID(N'jahez_v2.AccountingEntries'),OBJECT_ID(N'jahez_v2.PlatformTransactions'))
    AND name=N'AccountId' AND is_nullable=1 AND TYPE_NAME(system_type_id)=N'uniqueidentifier')<>3
    OR COL_LENGTH(N'jahez_v2.PlatformTransactions',N'SupersededByBatchId') IS NULL
    THROW 51000,'Account debt columns are missing.',1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'jahez_v2.DailyOrders')
    AND name=N'HandoverId' AND is_nullable=0)
    OR COL_LENGTH(N'jahez_v2.DailyOrders',N'AccountId') IS NOT NULL
    OR COL_LENGTH(N'jahez_v2.DailyOrders',N'SupersededByBatchId') IS NOT NULL
    THROW 51000,'Daily orders must retain the original assignment-based schema.',1;
IF (SELECT COUNT(*) FROM sys.check_constraints WHERE name IN
    (N'CK_JahezRequest_AccountScope',N'CK_JahezTransaction_AccountScope',N'CK_JahezLedger_AccountScope')
    AND is_disabled=0 AND is_not_trusted=0)<>3
    THROW 51000,'Account scope checks are missing or untrusted.',1;
IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE name IN
    (N'FK_AccountingEntries_PlatformRiderAccounts_AccountId',N'FK_PlatformTransactions_ImportBatches_SupersededByBatchId',
     N'FK_PlatformTransactions_PlatformRiderAccounts_AccountId',N'FK_Requests_PlatformRiderAccounts_AccountId')
    AND is_disabled=0 AND is_not_trusted=0 AND delete_referential_action=0)<>4
    THROW 51000,'Account debt foreign keys are missing or untrusted.',1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.ImportBatches')
    AND name=N'IX_ImportBatches_Kind_ContentHash' AND is_unique=0 AND is_disabled=0)
    OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.ImportBatches')
    AND name=N'UX_JahezDailyImport_ContentHash' AND is_unique=1 AND has_filter=1 AND is_disabled=0)
    OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'jahez_v2.ImportBatches')
    AND name=N'IX_ImportBatches_ReplacesBatchId' AND is_unique=1 AND has_filter=1 AND is_disabled=0)
    THROW 51000,'Import indexes do not preserve the daily-order rules.',1;
IF EXISTS (SELECT 1 FROM jahez_v2.Requests r JOIN jahez_v2.AccountSessions h ON h.Id=r.HandoverId
    WHERE r.AccountId IS NULL OR r.AccountId<>h.PlatformRiderAccountId)
    OR EXISTS (SELECT 1 FROM jahez_v2.AccountingEntries e JOIN jahez_v2.AccountSessions h ON h.Id=e.HandoverId
    WHERE e.AccountId IS NULL OR e.AccountId<>h.PlatformRiderAccountId)
    OR EXISTS (SELECT 1 FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.AccountSessions h ON h.Id=t.HandoverId
    WHERE t.AccountId IS NULL OR t.AccountId<>h.PlatformRiderAccountId)
    THROW 51000,'Account backfill does not match the recorded sessions.',1;
IF EXISTS (SELECT 1 FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.ImportBatches b ON b.ReplacesBatchId=t.BatchId
    WHERE b.CommittedAtUtc IS NOT NULL AND b.IsDeleted=0 AND (t.SupersededByBatchId IS NULL OR t.SupersededByBatchId<>b.Id))
    THROW 51000,'Legacy transaction replacements were not retained.',1;
SELECT MigrationId,ProductVersion FROM migration.__ApplicationMigrationsHistory
    WHERE MigrationId=N'20261026165219_MakeJahezAdministrativeDebtAccountBased';
SELECT COUNT_BIG(*) Requests,COUNT_BIG(AccountId) AccountLinkedRequests FROM jahez_v2.Requests;
SELECT COUNT_BIG(*) LedgerEntries,COUNT_BIG(AccountId) AccountLinkedEntries,COALESCE(SUM(Amount),0) LedgerAmount
    FROM jahez_v2.AccountingEntries;
SELECT COUNT_BIG(*) Transactions,COUNT_BIG(AccountId) AccountLinkedTransactions,
    COALESCE(SUM(NetAmount),0) NetAmount,COUNT_BIG(SupersededByBatchId) SupersededTransactions FROM jahez_v2.PlatformTransactions;
SELECT COUNT_BIG(*) DailyOrders,COALESCE(SUM(CONVERT(bigint,Count)),0) OrderCount FROM jahez_v2.DailyOrders;
SELECT COUNT_BIG(*) MoneyMovements,COALESCE(SUM(Amount),0) MovementAmount FROM jahez_v2.MoneyMovements;
