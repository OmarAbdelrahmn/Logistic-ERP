using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnforceSingleOpenOilBarrel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostPerLiter",
                schema: "maintenance",
                table: "OilBarrels",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId" },
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.025, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "UnitCostPerLiter",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.025, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
        }
    }
}
