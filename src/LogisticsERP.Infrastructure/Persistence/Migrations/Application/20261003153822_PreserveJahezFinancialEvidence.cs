using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class PreserveJahezFinancialEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalculationJson",
                schema: "jahez",
                table: "JahezLedgerEntry",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FromDate",
                schema: "jahez",
                table: "JahezLedgerEntry",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ThroughDate",
                schema: "jahez",
                table: "JahezLedgerEntry",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationReason",
                schema: "jahez",
                table: "JahezCashboxHandover",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionReason",
                schema: "jahez",
                table: "JahezCashboxHandover",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountFee_ApprovalRequestId",
                schema: "jahez",
                table: "JahezAccountFee",
                column: "ApprovalRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_JahezAccountFee_JahezApprovalRequest_ApprovalRequestId",
                schema: "jahez",
                table: "JahezAccountFee",
                column: "ApprovalRequestId",
                principalSchema: "jahez",
                principalTable: "JahezApprovalRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JahezAccountFee_JahezApprovalRequest_ApprovalRequestId",
                schema: "jahez",
                table: "JahezAccountFee");

            migrationBuilder.DropIndex(
                name: "IX_JahezAccountFee_ApprovalRequestId",
                schema: "jahez",
                table: "JahezAccountFee");

            migrationBuilder.DropColumn(
                name: "CalculationJson",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropColumn(
                name: "FromDate",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropColumn(
                name: "ThroughDate",
                schema: "jahez",
                table: "JahezLedgerEntry");

            migrationBuilder.DropColumn(
                name: "ConfirmationReason",
                schema: "jahez",
                table: "JahezCashboxHandover");

            migrationBuilder.DropColumn(
                name: "DecisionReason",
                schema: "jahez",
                table: "JahezCashboxHandover");
        }
    }
}
