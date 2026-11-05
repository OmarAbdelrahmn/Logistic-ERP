using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerWalletV21ReconciliationAndWithdrawalArrival : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationEvidence",
                schema: "hunger",
                table: "SettlementDocuments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentType",
                schema: "hunger",
                table: "SettlementDocuments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                schema: "hunger",
                table: "SettlementDocuments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrivalEvidence",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CompletionDate",
                schema: "hunger",
                table: "FinancialEvents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionHash",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionKey",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationOwnerId",
                schema: "hunger",
                table: "FinancialEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationType",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceWalletBalance",
                schema: "hunger",
                table: "FinancialEvents",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Posted");

            migrationBuilder.AddColumn<DateOnly>(
                name: "TransferDate",
                schema: "hunger",
                table: "FinancialEvents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferReference",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Old withdrawals already posted treasury transfers; preserve that historical state.
            migrationBuilder.Sql("EXEC(N'UPDATE [hunger].[FinancialEvents] SET [Status] = ''Arrived'' WHERE [Kind] IN (''WithdrawalCompany'', ''WithdrawalSupervisor'');');");
            // A legacy adjustment has neither a verified economic source nor an explicit closing link.
            migrationBuilder.Sql("EXEC(N'UPDATE [hunger].[SettlementDocuments] SET [Status] = ''Unmatched'' WHERE [Kind] = ''Adjustment'';');");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDocuments_ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments",
                column: "ClosingSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDocuments_PlatformRiderAccountId_InvoicePeriod_SourceDocumentType_SourceReference",
                schema: "hunger",
                table: "SettlementDocuments",
                columns: new[] { "PlatformRiderAccountId", "InvoicePeriod", "SourceDocumentType", "SourceReference" },
                unique: true,
                filter: "[Kind]='Adjustment' AND [PlatformRiderAccountId] IS NOT NULL AND [SourceDocumentType] IS NOT NULL AND [SourceReference] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SettlementDocuments_SettlementDocuments_ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments",
                column: "ClosingSnapshotId",
                principalSchema: "hunger",
                principalTable: "SettlementDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SettlementDocuments_SettlementDocuments_ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropIndex(
                name: "IX_SettlementDocuments_ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropIndex(
                name: "IX_SettlementDocuments_PlatformRiderAccountId_InvoicePeriod_SourceDocumentType_SourceReference",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropColumn(
                name: "ClosingSnapshotId",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropColumn(
                name: "ReconciliationEvidence",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropColumn(
                name: "SourceDocumentType",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                schema: "hunger",
                table: "SettlementDocuments");

            migrationBuilder.DropColumn(
                name: "ArrivalEvidence",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "CompletionDate",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "CompletionHash",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "CompletionKey",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "DestinationOwnerId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "DestinationType",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "SourceWalletBalance",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "TransferDate",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "TransferReference",
                schema: "hunger",
                table: "FinancialEvents");
        }
    }
}
