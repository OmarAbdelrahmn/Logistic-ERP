using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddOilBarrelControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.CreateTable(
                name: "OilBarrels",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarrelNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    PurchaseReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageSequence = table.Column<int>(type: "int", nullable: false),
                    NominalCapacityLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MaximumAllowedLossLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RecordedLossLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilBarrels", x => x.Id);
                    table.CheckConstraint("CK_OilBarrels_Quantities", "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.025, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
                    table.CheckConstraint("CK_OilBarrels_Status", "([Status] = 1 AND [OpenedAtUtc] IS NULL AND [RemainingLiters] > 0) OR ([Status] = 2 AND [OpenedAtUtc] IS NOT NULL AND [RemainingLiters] > 0) OR ([Status] = 3 AND [OpenedAtUtc] IS NOT NULL AND [RemainingLiters] = 0) OR [Status] = 4");
                    table.ForeignKey(
                        name: "FK_OilBarrels_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrels_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrels_PurchaseReceiptLines_PurchaseReceiptLineId",
                        column: x => x.PurchaseReceiptLineId,
                        principalSchema: "maintenance",
                        principalTable: "PurchaseReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrels_StockCostLayers_StockCostLayerId",
                        column: x => x.StockCostLayerId,
                        principalSchema: "maintenance",
                        principalTable: "StockCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OilBarrelLosses",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OilBarrelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    QuantityLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilBarrelLosses", x => x.Id);
                    table.CheckConstraint("CK_OilBarrelLosses_Values", "[QuantityLiters] > 0 AND [CostAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_OilBarrelLosses_OilBarrels_OilBarrelId",
                        column: x => x.OilBarrelId,
                        principalSchema: "maintenance",
                        principalTable: "OilBarrels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrelLosses_StockMovementLines_StockMovementLineId",
                        column: x => x.StockMovementLineId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovementLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrelLosses_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OilBarrelUsageAllocations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OilBarrelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    ReversalOfAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilBarrelUsageAllocations", x => x.Id);
                    table.CheckConstraint("CK_OilBarrelUsageAllocations_Values", "[QuantityLiters] > 0 AND ([Direction] = 1 AND [ReversalOfAllocationId] IS NULL OR [Direction] = 2 AND [ReversalOfAllocationId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_OilBarrelUsageAllocations_MaterialUsages_MaintenanceMaterialUsageId",
                        column: x => x.MaintenanceMaterialUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrelUsageAllocations_OilBarrelUsageAllocations_ReversalOfAllocationId",
                        column: x => x.ReversalOfAllocationId,
                        principalSchema: "maintenance",
                        principalTable: "OilBarrelUsageAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilBarrelUsageAllocations_OilBarrels_OilBarrelId",
                        column: x => x.OilBarrelId,
                        principalSchema: "maintenance",
                        principalTable: "OilBarrels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements",
                sql: "[MovementType] BETWEEN 1 AND 9");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelLosses_OilBarrelId_OccurredAtUtc",
                schema: "maintenance",
                table: "OilBarrelLosses",
                columns: new[] { "OilBarrelId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelLosses_StockMovementId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelLosses_StockMovementLineId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "StockMovementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_BarrelNumber",
                schema: "maintenance",
                table: "OilBarrels",
                column: "BarrelNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryItemId",
                schema: "maintenance",
                table: "OilBarrels",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_InventoryLocationId_InventoryItemId_Status_OpenedAtUtc",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "InventoryLocationId", "InventoryItemId", "Status", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_IsDeleted",
                schema: "maintenance",
                table: "OilBarrels",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_PurchaseReceiptLineId_PackageSequence",
                schema: "maintenance",
                table: "OilBarrels",
                columns: new[] { "PurchaseReceiptLineId", "PackageSequence" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrels_StockCostLayerId",
                schema: "maintenance",
                table: "OilBarrels",
                column: "StockCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelUsageAllocations_MaintenanceMaterialUsageId_OilBarrelId_Direction",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations",
                columns: new[] { "MaintenanceMaterialUsageId", "OilBarrelId", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelUsageAllocations_OilBarrelId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations",
                column: "OilBarrelId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelUsageAllocations_ReversalOfAllocationId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations",
                column: "ReversalOfAllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OilBarrelLosses",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "OilBarrelUsageAllocations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "OilBarrels",
                schema: "maintenance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Type",
                schema: "maintenance",
                table: "StockMovements",
                sql: "[MovementType] BETWEEN 1 AND 8");
        }
    }
}
