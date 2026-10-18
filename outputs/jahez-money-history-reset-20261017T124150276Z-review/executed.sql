-- One-off reset authorized by the user. Caller supplies a transaction, backup,
-- preserved-record checks, and @ResetDate in Riyadh local calendar time.
-- Keep current accounts, credentials, assignments, and their session identities.
SET XACT_ABORT ON;
IF @@TRANCOUNT = 0 THROW 51000,'An explicit transaction is required.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('jahez_v2')) <> 17
    THROW 51000,'Unexpected Jahez schema inventory.',1;
IF EXISTS(SELECT 1 FROM jahez_v2.AccountSessions WHERE EndedAtUtc IS NOT NULL)
    THROW 51000,'Closed sessions require a separate reviewed history reset.',1;

DELETE FROM jahez_v2.MoneyMovements;
DELETE FROM jahez_v2.TransferredDebts;
DELETE FROM jahez_v2.AccountFees;
DELETE FROM jahez_v2.CommissionPolicies;
DELETE FROM jahez_v2.RequestDecisions;
-- Requests and Operations have nullable references in both directions.
UPDATE jahez_v2.Operations SET ApprovedRequestId=NULL WHERE ApprovedRequestId IS NOT NULL;
UPDATE jahez_v2.Requests SET OperationId=NULL WHERE OperationId IS NOT NULL;
DELETE FROM jahez_v2.Requests;
DELETE FROM jahez_v2.Operations;
DELETE FROM jahez_v2.AccountingEntries;
DELETE FROM jahez_v2.EarningsStatements;
DELETE FROM jahez_v2.PlatformTransactions;
DELETE FROM jahez_v2.DailyOrders;
DELETE FROM jahez_v2.ImportRows;
DELETE FROM jahez_v2.ImportFiles;
DELETE FROM jahez_v2.ImportBatches;
DELETE FROM jahez_v2.SettlementReminders;
DELETE FROM jahez_v2.CommandReceipts;

-- Commission is computed dynamically at SAR 15/day. Clearing the ledger alone
-- would immediately recreate old balances. Resume accrual after the reset day.
UPDATE jahez_v2.AccountSessions
SET CommissionStartsOn=CASE WHEN CommissionStartsOn>DATEADD(day,1,@ResetDate)
                           THEN CommissionStartsOn ELSE DATEADD(day,1,@ResetDate) END,
    CommissionPostedThrough=NULL,LastSettlementPaymentAtUtc=NULL,
    UpdatedAtUtc=SYSUTCDATETIME();

DELETE FROM app.Notifications
WHERE EventType LIKE N'jahez.%' OR SourceEntityType LIKE N'Jahez%';
