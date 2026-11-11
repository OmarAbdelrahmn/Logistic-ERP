using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <summary>
    /// CR-005 F1-F3: read-only, versioned views for the finance reader. Column names are a contract; change them only in a _v2 view.
    /// Views expose every row with its approval state; the reader filters IsSealed = 1 for company books.
    /// SELECT on the schema is granted to the reader login by the deployment script, not here.
    /// </summary>
    public partial class HungerCr005FinanceReadViews : Migration
    {
        public static readonly string[] ViewNames =
        [
            "vw_hunger_financial_events_v1", "vw_hunger_wallet_reviews_v1", "vw_hunger_month_close_v1",
            "vw_hunger_adjustments_v1", "vw_hunger_custody_v1"
        ];

        public static readonly string[] CreateStatements =
        [
            "IF SCHEMA_ID('finance_read') IS NULL EXEC('CREATE SCHEMA finance_read')",
            """
            CREATE OR ALTER VIEW finance_read.vw_hunger_financial_events_v1 AS
            SELECT e.Id AS EventId, e.ClientPlatformId, e.SupervisorEmployeeId, e.PlatformRiderAccountId, e.RiderProfileId,
                   e.RelatedEventId, e.SupersedesEventId, e.Kind, e.Status, e.Role, e.Amount, e.RiderBalanceChange, e.BusinessDate,
                   e.FromSubAccountId, e.ToSubAccountId, e.DestinationType, e.DestinationChannel, e.DestinationOwnerId,
                   e.ApprovalStatus, e.ApprovedAtUtc, e.ApprovedByUserId, e.RejectionReason, e.SealAccountsJson, e.ExceptionApprovalId,
                   CAST(CASE WHEN e.ApprovalStatus = 'Sealed' THEN 1 ELSE 0 END AS bit) AS IsSealed,
                   CASE WHEN e.Kind = 'TopUpCollected' AND e.DestinationChannel = 'OwnerVault' THEN 'HungerWalletTopUpExpense' END AS ExpenseLabel,
                   e.EvidenceReference, e.CreatedAtUtc, e.CreatedByUserId
            FROM hunger.FinancialEvents e
            """,
            """
            CREATE OR ALTER VIEW finance_read.vw_hunger_wallet_reviews_v1 AS
            SELECT r.Id AS ReviewId, r.PlatformRiderAccountId, r.Status, r.SwapId, r.UserId AS ReviewedByUserId, r.CreatedAtUtc,
                   JSON_VALUE(r.RequestJson, '$.action') AS Direction,
                   TRY_CONVERT(uniqueidentifier, JSON_VALUE(r.RequestJson, '$.riderProfileId')) AS RiderProfileId,
                   TRY_CONVERT(uniqueidentifier, JSON_VALUE(r.RequestJson, '$.supervisorEmployeeId')) AS SupervisorEmployeeId,
                   TRY_CONVERT(date, JSON_VALUE(r.RequestJson, '$.businessDate')) AS MObservedAt,
                   TRY_CONVERT(decimal(18,2), JSON_VALUE(r.RequestJson, '$.mobileBalance')) AS M,
                   TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, '$.closingBalance')) AS C,
                   TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, '$.provenAdjustments')) AS A,
                   JSON_VALUE(e.DetailsJson, '$.closingSource') AS ClosingSource,
                   TRY_CONVERT(decimal(18,2), JSON_VALUE(e.DetailsJson, '$.attributedBalance')) AS ExplicitBalance,
                   e.ExceptionApprovalId AS ApprovalId, e.Id AS EventId, e.Kind AS EventKind, e.RiderBalanceChange,
                   e.ApprovalStatus, e.ApprovedAtUtc, e.ApprovedByUserId, e.EvidenceReference AS WalletEvidence
            FROM hunger.WalletReviews r
            LEFT JOIN hunger.FinancialEvents e ON e.IdempotencyKey = CONCAT('WalletReview:', LOWER(CONVERT(nvarchar(36), r.Id)))
            WHERE r.IsDeleted = 0
            """,
            """
            CREATE OR ALTER VIEW finance_read.vw_hunger_month_close_v1 AS
            SELECT d.Id AS DocumentId, d.ClientPlatformId, d.PlatformRiderAccountId, d.ChargeFrom AS PeriodStart, d.ChargeThrough AS PeriodEnd,
                   d.WalletBalance AS C, d.Status, d.ObservedOn, d.InvoiceDocumentId,
                   CASE WHEN d.IdempotencyKey LIKE 'InvoiceClose:%' THEN 'Invoice' ELSE 'Manual' END AS Source,
                   ISNULL((SELECT SUM(a.WalletBalance) FROM hunger.SettlementDocuments a
                           WHERE a.Kind = 'Adjustment' AND a.Status = 'Matched' AND a.ClosingSnapshotId = d.Id), 0) AS A,
                   d.EvidenceReference, d.CreatedAtUtc
            FROM hunger.SettlementDocuments d
            WHERE d.Kind = 'MonthClose'
            """,
            """
            CREATE OR ALTER VIEW finance_read.vw_hunger_adjustments_v1 AS
            SELECT d.Id AS DocumentId, d.ClientPlatformId, d.PlatformRiderAccountId, d.ClosingSnapshotId AS MonthCloseDocumentId,
                   d.InvoicePeriod, d.WalletBalance AS A, d.Status, d.ApprovalDate, d.ObservedOn,
                   d.SourceDocumentType, d.SourceReference, d.ReconciliationEvidence, d.CreatedAtUtc
            FROM hunger.SettlementDocuments d
            WHERE d.Kind = 'Adjustment'
            """,
            """
            CREATE OR ALTER VIEW finance_read.vw_hunger_custody_v1 AS
            SELECT x.ClientPlatformId, x.SupervisorEmployeeId, x.SponsorId, x.CustodySubAccountId,
                   x.FundingDelivered, x.TopUpAdvance, x.TopUpCollected,
                   x.FundingDelivered - x.TopUpAdvance - x.TopUpCollected AS ExpectedBalance, x.PostingBalance,
                   x.PostingBalance - (x.FundingDelivered - x.TopUpAdvance - x.TopUpCollected) AS Difference
            FROM (
                SELECT c.ClientPlatformId, c.SupervisorEmployeeId, c.SponsorId, c.CustodySubAccountId,
                       ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                               AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = 'Funding'), 0) AS FundingDelivered,
                       ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                               AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = 'TopUpAdvance' AND e.Status = 'Posted'), 0) AS TopUpAdvance,
                       ISNULL((SELECT SUM(e.Amount) FROM hunger.FinancialEvents e WHERE e.ClientPlatformId = c.ClientPlatformId
                               AND e.SupervisorEmployeeId = c.SupervisorEmployeeId AND e.Kind = 'TopUpCollected' AND e.Status = 'Posted'), 0) AS TopUpCollected,
                       ISNULL((SELECT SUM(p.Amount) FROM jahez_v3.Postings p WHERE p.ClientPlatformId = c.ClientPlatformId
                               AND p.SupervisorEmployeeId = c.SupervisorEmployeeId AND p.SubAccountId = c.CustodySubAccountId), 0) AS PostingBalance
                FROM hunger.FinanceConfigurations c
                WHERE c.IsDeleted = 0
            ) x
            """
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CREATE VIEW must be alone in its batch; EXEC keeps that true inside idempotent deployment scripts as well.
            foreach (var statement in CreateStatements)
                migrationBuilder.Sql("EXEC(N'" + statement.Replace("'", "''") + "')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var view in ViewNames)
                migrationBuilder.Sql($"DROP VIEW IF EXISTS finance_read.{view}");
            migrationBuilder.Sql("IF SCHEMA_ID('finance_read') IS NOT NULL EXEC('DROP SCHEMA finance_read')");
        }
    }
}
