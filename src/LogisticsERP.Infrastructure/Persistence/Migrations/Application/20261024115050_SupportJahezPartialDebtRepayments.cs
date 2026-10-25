using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SupportJahezPartialDebtRepayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET XACT_ABORT ON;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezTransferredDebt_Amount",
                schema: "jahez_v2",
                table: "TransferredDebts");

            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                schema: "jahez_v2",
                table: "TransferredDebts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPaid",
                schema: "jahez_v2",
                table: "MoneyMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DashboardDebtPaid",
                schema: "jahez_v2",
                table: "MoneyMovements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Existing receipts were full repayments. Stop for inconsistent legacy data rather
            // than reopening a paid debt or silently treating an existing receipt as unpaid.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM jahez_v2.MoneyMovements m
                    JOIN jahez_v2.TransferredDebts d ON d.Id = m.TransferredDebtId
                    WHERE m.Direction <> 1 OR d.PaidAtUtc IS NULL OR m.Amount <> d.Amount
                )
                    THROW 51000, 'Review inconsistent legacy Jahez debt receipts before migration.', 1;
                EXEC(N'UPDATE jahez_v2.TransferredDebts SET PaidAmount = Amount WHERE PaidAtUtc IS NOT NULL;');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezTransferredDebt_Amount",
                schema: "jahez_v2",
                table: "TransferredDebts",
                sql: "[Amount] >= 0 AND [PaidAmount] >= 0 AND [PaidAmount] <= [Amount] AND (([PaidAmount] = [Amount] AND [PaidAtUtc] IS NOT NULL) OR ([PaidAmount] < [Amount] AND [PaidAtUtc] IS NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "TransferredDebtId",
                filter: "[TransferredDebtId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_DebtAllocation",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "([DashboardDebtPaid] IS NULL AND [CommissionPaid] IS NULL) OR ([DashboardDebtPaid] IS NOT NULL AND [CommissionPaid] IS NOT NULL AND [TransferredDebtId] IS NOT NULL AND [Direction] = 1 AND [DashboardDebtPaid] >= 0 AND [CommissionPaid] >= 0 AND [DashboardDebtPaid] + [CommissionPaid] = [Amount])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET XACT_ABORT ON;
                IF EXISTS (SELECT 1 FROM jahez_v2.TransferredDebts WHERE Amount <= 0 OR (PaidAmount > 0 AND PaidAtUtc IS NULL))
                    OR EXISTS (SELECT TransferredDebtId FROM jahez_v2.MoneyMovements WHERE TransferredDebtId IS NOT NULL GROUP BY TransferredDebtId HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot roll back Jahez partial repayments: legacy schema cannot represent these balances or receipts.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezTransferredDebt_Amount",
                schema: "jahez_v2",
                table: "TransferredDebts");

            migrationBuilder.DropIndex(
                name: "IX_MoneyMovements_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_DebtAllocation",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                schema: "jahez_v2",
                table: "TransferredDebts");

            migrationBuilder.DropColumn(
                name: "CommissionPaid",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "DashboardDebtPaid",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezTransferredDebt_Amount",
                schema: "jahez_v2",
                table: "TransferredDebts",
                sql: "[Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyMovements_TransferredDebtId",
                schema: "jahez_v2",
                table: "MoneyMovements",
                column: "TransferredDebtId",
                unique: true,
                filter: "[TransferredDebtId] IS NOT NULL");
        }
    }
}
