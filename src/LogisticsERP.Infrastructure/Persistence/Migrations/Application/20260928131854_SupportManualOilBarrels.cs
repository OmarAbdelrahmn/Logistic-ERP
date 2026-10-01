using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SupportManualOilBarrels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_PurchaseReceiptLineId_PackageSequence",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseReceiptLineId",
                schema: "maintenance",
                table: "OilBarrels",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements",
                sql: "[MovementType] BETWEEN 1 AND 10");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_PurchaseReceiptLineId_PackageSequence",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "PurchaseReceiptLineId", "PackageSequence" },
                unique: true,
                filter: "[PurchaseReceiptLineId] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_PurchaseReceiptLineId_PackageSequence",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseReceiptLineId",
                schema: "maintenance",
                table: "OilBarrels",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements",
                sql: "[MovementType] BETWEEN 1 AND 9");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_PurchaseReceiptLineId_PackageSequence",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "PurchaseReceiptLineId", "PackageSequence" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
