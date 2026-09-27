using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddInventoryItemVehicleCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompatibleVehicleTypesMask",
                schema: "maintenance",
                table: "InventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 31);

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryItems_CompatibleVehicleTypesMask",
                schema: "maintenance",
                table: "InventoryItems",
                sql: "[CompatibleVehicleTypesMask] BETWEEN 1 AND 31");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryItems_CompatibleVehicleTypesMask",
                schema: "maintenance",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CompatibleVehicleTypesMask",
                schema: "maintenance",
                table: "InventoryItems");
        }
    }
}
