using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakeJahezAdministrativeDebtAccountBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_Kind_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches");

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "Requests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "jahez_v2",
                table: "Requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                type: "uniqueidentifier",
                nullable: true);

            // Recover the original account identity even for archived sessions.
            migrationBuilder.Sql("""
                UPDATE r SET AccountId = h.PlatformRiderAccountId
                FROM jahez_v2.Requests r JOIN jahez_v2.AccountSessions h ON h.Id = r.HandoverId;
                UPDATE e SET AccountId = h.PlatformRiderAccountId
                FROM jahez_v2.AccountingEntries e JOIN jahez_v2.AccountSessions h ON h.Id = e.HandoverId;
                UPDATE t SET AccountId = h.PlatformRiderAccountId
                FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.AccountSessions h ON h.Id = t.HandoverId;
                UPDATE t SET SupersededByBatchId = b.Id
                FROM jahez_v2.PlatformTransactions t JOIN jahez_v2.ImportBatches b ON b.ReplacesBatchId = t.BatchId
                WHERE b.CommittedAtUtc IS NOT NULL AND b.IsDeleted = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AccountId",
                schema: "jahez_v2",
                table: "Requests",
                column: "AccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezRequest_AccountScope",
                schema: "jahez_v2",
                table: "Requests",
                sql: "[HandoverId] IS NOT NULL OR ([AccountId] IS NOT NULL AND [Kind] = 5 AND [AdjustmentTarget] IS NOT NULL AND [AdjustmentTarget] = 2)");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTransactions_AccountId_OccurredAtUtc",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                columns: new[] { "AccountId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTransactions_SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "SupersededByBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezTransaction_AccountScope",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                sql: "[AccountId] IS NOT NULL OR [HandoverId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Kind_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches",
                columns: new[] { "Kind", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches",
                column: "ReplacesBatchId",
                unique: true,
                filter: "[ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL AND [Kind] = 2");

            migrationBuilder.CreateIndex(
                name: "UX_JahezDailyImport_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches",
                columns: new[] { "Kind", "ContentHash" },
                unique: true,
                filter: "[Kind] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                column: "AccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezLedger_AccountScope",
                schema: "jahez_v2",
                table: "AccountingEntries",
                sql: "[HandoverId] IS NOT NULL OR ([AccountId] IS NOT NULL AND [Bucket] = 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingEntries_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                column: "AccountId",
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformTransactions_ImportBatches_SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "SupersededByBatchId",
                principalSchema: "jahez_v2",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformTransactions_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                column: "AccountId",
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "Requests",
                column: "AccountId",
                principalSchema: "app",
                principalTable: "PlatformRiderAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM jahez_v2.Requests WHERE HandoverId IS NULL)
                    OR EXISTS (SELECT 1 FROM jahez_v2.AccountingEntries WHERE HandoverId IS NULL)
                    OR EXISTS (SELECT 1 FROM jahez_v2.PlatformTransactions WHERE HandoverId IS NULL OR SupersededByBatchId IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM jahez_v2.ImportBatches GROUP BY Kind, ContentHash HAVING COUNT(*) > 1)
                    OR EXISTS (SELECT 1 FROM jahez_v2.ImportBatches WHERE ReplacesBatchId IS NOT NULL AND CommittedAtUtc IS NOT NULL
                        GROUP BY ReplacesBatchId HAVING COUNT(*) > 1)
                    THROW 51000, 'Account-only or replaced Jahez data cannot be rolled back to assignment-required storage. Restore a reviewed backup instead.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AccountingEntries_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformTransactions_ImportBatches_SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformTransactions_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_PlatformRiderAccounts_AccountId",
                schema: "jahez_v2",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_AccountId",
                schema: "jahez_v2",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezRequest_AccountScope",
                schema: "jahez_v2",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_PlatformTransactions_AccountId_OccurredAtUtc",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PlatformTransactions_SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezTransaction_AccountScope",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_Kind_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_ImportBatches_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "UX_JahezDailyImport_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_AccountingEntries_AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezLedger_AccountScope",
                schema: "jahez_v2",
                table: "AccountingEntries");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "jahez_v2",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropColumn(
                name: "SupersededByBatchId",
                schema: "jahez_v2",
                table: "PlatformTransactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "jahez_v2",
                table: "AccountingEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "Requests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "PlatformTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HandoverId",
                schema: "jahez_v2",
                table: "AccountingEntries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Kind_ContentHash",
                schema: "jahez_v2",
                table: "ImportBatches",
                columns: new[] { "Kind", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_ReplacesBatchId",
                schema: "jahez_v2",
                table: "ImportBatches",
                column: "ReplacesBatchId",
                unique: true,
                filter: "[ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL");
        }
    }
}
