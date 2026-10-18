using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezDebtFreezeAndClearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccountClearedAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DebtPaidAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DebtStatus",
                schema: "jahez",
                table: "JahezAccountHandover",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ExternalClearanceReference",
                schema: "jahez",
                table: "JahezAccountHandover",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FrozenAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'UPDATE [jahez].[JahezAccountHandover]
                SET [DebtStatus] = CASE WHEN [DebtTransferred] = 1 THEN 3 WHEN [EndedAtUtc] IS NOT NULL THEN 2 ELSE 1 END,
                    [AccountClearedAtUtc] = CASE WHEN [DebtTransferred] = 1 THEN [EndedAtUtc] ELSE NULL END;');
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezLedger_StatusAmount",
                schema: "jahez",
                table: "JahezLedgerEntry",
                sql: "[Kind] <> 6 OR [Amount] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezHandover_DebtStatus",
                schema: "jahez",
                table: "JahezAccountHandover",
                sql: "[DebtStatus] BETWEEN 1 AND 5 AND ([DebtStatus] <> 4 OR ([FrozenAtUtc] IS NOT NULL AND [EndedAtUtc] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezHandover_FrozenRange",
                schema: "jahez",
                table: "JahezAccountHandover",
                sql: "[FrozenAtUtc] IS NULL OR ([FrozenAtUtc] >= [StartedAtUtc] AND ([EndedAtUtc] IS NULL OR [EndedAtUtc] >= [FrozenAtUtc]))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezLedger_StatusAmount",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezHandover_DebtStatus",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezHandover_FrozenRange",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropColumn(
                name: "AccountClearedAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropColumn(
                name: "DebtPaidAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropColumn(
                name: "DebtStatus",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropColumn(
                name: "ExternalClearanceReference",
                schema: "jahez",
                table: "JahezAccountHandover");

            migrationBuilder.DropColumn(
                name: "FrozenAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover");
        }
    }
}
