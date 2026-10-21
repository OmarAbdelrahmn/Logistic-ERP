-- One-off user-authorized financial reset. The caller provides a serializable transaction,
-- backups, exact preservation checks, @ResetDate and @CarryPlan. Operational history stays.
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF @@TRANCOUNT=0 THROW 51000,'An explicit reset transaction is required.',1;
IF @ResetDate<>CONVERT(date,'20261019',112) OR CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+03:00'))<>@ResetDate
 THROW 51000,'This reset is restricted to the authorized Riyadh business date.',1;
IF EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1 OR ActivatedAtUtc IS NOT NULL)
 THROW 51000,'An activated treasury cannot use this pre-start reset.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.CashboxHandovers WHERE TreasuryVersion<>2)
 OR EXISTS(SELECT 1 FROM jahez_v3.BalanceVerifications) OR EXISTS(SELECT 1 FROM jahez_v3.PetroEvidence)
 THROW 51000,'New V3 workflow evidence exists; a separate review is required.',1;
IF EXISTS(SELECT Id FROM jahez_v3.Postings EXCEPT
 SELECT Id FROM OPENJSON(@CarryPlan,'$.postings') WITH(Id uniqueidentifier '$.id'))
 OR EXISTS(SELECT 1 FROM jahez_v3.Postings WHERE Kind NOT IN(1,2))
 THROW 51000,'V3 contains postings outside the historical carryforward.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.Requests WHERE Status=1)
 THROW 51000,'Resolve pending requests before clearing their financial context.',1;
IF EXISTS(SELECT 1 FROM audit.AuditEntries WHERE CorrelationId=N'jahez-financial-fresh-start-20261019')
 THROW 51000,'This reset was already committed; do not clear new work.',1;
DECLARE @now datetimeoffset=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');

-- Retain request outcomes and decisions. Their removed settlement links are preserved
-- in the pre-reset archive and reset audit, rather than leaving dangling foreign keys.
DECLARE @requestLinks nvarchar(max)=(SELECT Id,OperationId FROM jahez_v2.Requests WHERE OperationId IS NOT NULL FOR JSON PATH);
UPDATE jahez_v2.Requests SET OperationId=NULL,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE OperationId IS NOT NULL;

-- Temporarily bypass only the four immutable tables containing copied legacy history.
-- Table/schema locks and the surrounding transaction prevent another writer using this window.
DISABLE TRIGGER [jahez_v3].[TR_HandoverPostings_Immutable] ON [jahez_v3].[HandoverPostings];
DISABLE TRIGGER [jahez_v3].[TR_HandoverLines_Immutable] ON [jahez_v3].[HandoverLines];
DISABLE TRIGGER [jahez_v3].[TR_Postings_Immutable] ON [jahez_v3].[Postings];
DISABLE TRIGGER [jahez_v3].[TR_Ownership_Immutable] ON [jahez_v3].[Ownership];
DELETE FROM jahez_v3.HandoverPostings;
DELETE FROM jahez_v3.HandoverLines;
DELETE FROM jahez_v3.Postings;
DELETE FROM jahez_v3.Ownership;
ENABLE TRIGGER [jahez_v3].[TR_HandoverPostings_Immutable] ON [jahez_v3].[HandoverPostings];
ENABLE TRIGGER [jahez_v3].[TR_HandoverLines_Immutable] ON [jahez_v3].[HandoverLines];
ENABLE TRIGGER [jahez_v3].[TR_Postings_Immutable] ON [jahez_v3].[Postings];
ENABLE TRIGGER [jahez_v3].[TR_Ownership_Immutable] ON [jahez_v3].[Ownership];

DELETE FROM jahez_v2.CashboxHandoverMovements;
DELETE FROM jahez_v2.CashboxHandovers;
DELETE FROM jahez_v2.MoneyMovements;
DELETE FROM jahez_v2.TransferredDebts;
DELETE FROM jahez_v2.AccountFees;
DELETE FROM jahez_v2.Operations;
DELETE FROM jahez_v2.AccountingEntries;
DELETE FROM jahez_v2.EarningsStatements;
DELETE FROM jahez_v2.SettlementReminders;

-- Preserve assignment identities and dates. Reset only financial accrual/payment anchors.
-- Future contractual commission starts must never be moved earlier by this reset.
UPDATE jahez_v2.AccountSessions
 SET CommissionStartsOn=CASE WHEN CommissionStartsOn>DATEADD(day,1,@ResetDate) THEN CommissionStartsOn ELSE DATEADD(day,1,@ResetDate) END,
 CommissionPostedThrough=NULL,LastSettlementPaymentAtUtc=NULL,UpdatedAtUtc=@now,UpdatedByUserId=NULL;

-- Keep the idempotency keys as retired markers: old queued create/execute commands
-- must conflict instead of replaying a deleted result or recreating old money.
UPDATE jahez_v2.CommandReceipts SET PayloadHash=N'retired:financial-reset:20261019',ResultJson=N'{}'
 WHERE Operation LIKE N'v2.operation.%' OR Operation LIKE N'v2.cashbox.%'
 OR Operation LIKE N'v2.fee.%' OR Operation LIKE N'v2.debt.%'
 OR Operation IN(N'earnings',N'v3.cash',N'v3.transfer',N'v3.reverse',N'v3.attribute',N'v3.backfill',N'v3.verify',N'v3.activate',N'v3.petro.evidence')
 OR Operation LIKE N'v3.handover.%';

DELETE FROM app.Notifications WHERE SourceEntityType IN(N'JahezTransferredDebt',N'JahezOperation',N'JahezCashboxHandover')
 OR EventType IN(N'jahez.rider.block.created',N'jahez.rider.block.released',N'jahez.settlement.overdue');

DECLARE @after nvarchar(max)=(SELECT @ResetDate AS ResetThroughRiyadhDate,DATEADD(day,1,@ResetDate) AS CommissionResumesOn,
 CAST(1 AS bit) AS OrdersPreserved,CAST(1 AS bit) AS ImportsPreserved,CAST(1 AS bit) AS RequestsPreserved,
 CAST(1 AS bit) AS AccountAndAssignmentIdentitiesPreserved,CAST(0 AS bit) AS TreasuryActivated,
 JSON_QUERY(@requestLinks) AS ArchivedRequestOperationLinks FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
INSERT audit.AuditEntries(Id,EventId,ActorUserId,ActorType,Action,Category,EntityType,OccurredAtUtc,CorrelationId,Reason,
 BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc,CreatedByUserId)
 VALUES(NEWID(),NEWID(),NULL,N'System',N'JahezFinancialFreshStart',N'jahez',N'TreasuryFinancialReset',@now,
 N'jahez-financial-fresh-start-20261019',
 N'User explicitly requested clearing financial history before V3, including V2 balances copied into V3, while preserving orders, imports, requests, accounts, riders and assignments. Existing history was backed up and archived; no real-world opening balance was verified or invented.',
 @BeforeSummary,@after,N'CodexAuthorizedMaintenance',1,@now,NULL);
UPDATE app.DatasetVersions SET Version=Version+1,LastChangedAtUtc=@now,UpdatedAtUtc=@now,UpdatedByUserId=NULL WHERE ModuleKey=N'jahez';
