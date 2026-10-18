using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddChefzOrderSettlementImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ChefzSettlement_Amounts",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.AddColumn<decimal>(
                name: "Bonuses",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Compensation",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Deductions",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFees",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                schema: "chefz",
                table: "WalletSettlements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportId",
                schema: "chefz",
                table: "WalletSettlements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiderName",
                schema: "chefz",
                table: "WalletSettlements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "chefz",
                table: "WalletSettlements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Wallet");

            migrationBuilder.AddColumn<int>(
                name: "SourceRows",
                schema: "chefz",
                table: "WalletSettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Tips",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "OrderSettlementImports",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceRows = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSettlementImports", x => x.Id);
                    table.CheckConstraint("CK_ChefzOrderImport_Rows", "[SourceRows] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletSettlements_ImportId",
                schema: "chefz",
                table: "WalletSettlements",
                column: "ImportId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChefzSettlement_Amounts",
                schema: "chefz",
                table: "WalletSettlements",
                sql: "(([Source] = 'Wallet' AND [ImportId] IS NULL AND [StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount]) OR ([Source] = 'Orders' AND [ImportId] IS NOT NULL AND [SourceRows] > 0 AND [StartWalletAmount] = 0 AND [EndWalletAmount] = 0 AND [Deductions] >= 0 AND [GrossEarnings] = [DeliveryFees] - [VatAmount] + [Tips] + [Compensation] + [Bonuses] - [Deductions])) AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSettlementImports_ContentHash",
                schema: "chefz",
                table: "OrderSettlementImports",
                column: "ContentHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WalletSettlements_OrderSettlementImports_ImportId",
                schema: "chefz",
                table: "WalletSettlements",
                column: "ImportId",
                principalSchema: "chefz",
                principalTable: "OrderSettlementImports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [chefz].[OrderSettlementImports]) OR EXISTS (SELECT 1 FROM [chefz].[WalletSettlements] WHERE [Source] = 'Orders') THROW 51000, 'Cannot roll back order settlement schema while financial import history exists.', 1;");

            migrationBuilder.DropForeignKey(
                name: "FK_WalletSettlements_OrderSettlementImports_ImportId",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropTable(
                name: "OrderSettlementImports",
                schema: "chefz");

            migrationBuilder.DropIndex(
                name: "IX_WalletSettlements_ImportId",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChefzSettlement_Amounts",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "Bonuses",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "Compensation",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "Deductions",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "DeliveryFees",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "DriverId",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "ImportId",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "RiderName",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "SourceRows",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "Tips",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChefzSettlement_Amounts",
                schema: "chefz",
                table: "WalletSettlements",
                sql: "[StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount] AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2)");
        }
    }
}
