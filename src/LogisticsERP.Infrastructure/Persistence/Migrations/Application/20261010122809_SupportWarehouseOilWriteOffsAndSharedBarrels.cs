using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SupportWarehouseOilWriteOffsAndSharedBarrels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrelLosses_Values",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.AddColumn<bool>(
                name: "AllowBothVehicleTypes",
                schema: "maintenance",
                table: "OilBarrels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RecordedMissingLiters",
                schema: "maintenance",
                table: "OilBarrels",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MissingReason",
                schema: "maintenance",
                table: "OilBarrelLosses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenCarSlot",
                schema: "maintenance",
                table: "OilBarrels",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN [AllowBothVehicleTypes] = 1 OR [AllowedVehicleType] IS NULL OR [AllowedVehicleType] = 2 THEN 1 ELSE NULL END",
                stored: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenMotorcycleSlot",
                schema: "maintenance",
                table: "OilBarrels",
                type: "int",
                nullable: true,
                computedColumnSql: "CASE WHEN [AllowBothVehicleTypes] = 1 OR [AllowedVehicleType] IS NULL OR [AllowedVehicleType] = 1 THEN 1 ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenCarSlot",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId", "OpenCarSlot" },
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenMotorcycleSlot",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId", "OpenMotorcycleSlot" },
                unique: true,
                filter: "[Status] = 2 AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "([AllowBothVehicleTypes] = 1 AND [AllowedVehicleType] IS NULL) OR ([AllowBothVehicleTypes] = 0 AND ([AllowedVehicleType] IS NULL OR [AllowedVehicleType] IN (1, 2)))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters] AND [RecordedMissingLiters] >= 0 AND [RemainingLiters] + [RecordedLossLiters] + [RecordedMissingLiters] <= [NominalCapacityLiters]");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelLosses_ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "ResponsibleInventoryLocationId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrelLosses_Values",
                schema: "maintenance",
                table: "OilBarrelLosses",
                sql: "[QuantityLiters] > 0 AND [CostAmount] >= 0 AND (([MissingReason] IS NULL AND [ResponsibleInventoryLocationId] IS NULL) OR ([MissingReason] IS NOT NULL AND [MissingReason] IN (1, 2, 3) AND [ResponsibleInventoryLocationId] IS NOT NULL))");

            migrationBuilder.AddForeignKey(
                name: "FK_OilBarrelLosses_InventoryLocations_ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "ResponsibleInventoryLocationId",
                principalSchema: "maintenance",
                principalTable: "InventoryLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OilBarrelLosses_InventoryLocations_ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenCarSlot",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_OpenMotorcycleSlot",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrelLosses_ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrelLosses_Values",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropColumn(
                name: "OpenCarSlot",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "OpenMotorcycleSlot",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "AllowBothVehicleTypes",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "RecordedMissingLiters",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.DropColumn(
                name: "MissingReason",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropColumn(
                name: "ResponsibleInventoryLocationId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_AllowedVehicleType",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[AllowedVehicleType] IS NULL OR [AllowedVehicleType] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrelLosses_Values",
                schema: "maintenance",
                table: "OilBarrelLosses",
                sql: "[QuantityLiters] > 0 AND [CostAmount] >= 0");
        }
    }
}
