using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class RestrictOilBarrelsByVehicleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.AddColumn<int>(
                name: "AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId", "AllowedVehicleType" },
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[AllowedVehicleType] IS NULL OR [AllowedVehicleType] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId" },
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");
        }
    }
}
