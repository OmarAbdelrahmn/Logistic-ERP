using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddInventorySupplyRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplyRequests",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderInventoryIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalIssuedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_SupplyRequests", x => x.Id);
                    table.CheckConstraint("CK_InventorySupplyRequests_Cost", "[TotalIssuedCost] >= 0");
                    table.CheckConstraint("CK_InventorySupplyRequests_Issuance", "([Status] = 2 AND [DecidedAtUtc] IS NOT NULL AND [DecidedByUserId] IS NOT NULL AND [IssuedAtUtc] IS NOT NULL AND [IssuedByUserId] IS NOT NULL) OR ([Status] <> 2 AND [IssuedAtUtc] IS NULL AND [IssuedByUserId] IS NULL)");
                    table.CheckConstraint("CK_InventorySupplyRequests_Status", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_InventorySupplyRequests_Subject", "([SubjectType] = 1 AND [MaintenanceWorkOrderId] IS NOT NULL AND [VehicleId] IS NOT NULL AND [RiderProfileId] IS NULL) OR ([SubjectType] = 2 AND [MaintenanceWorkOrderId] IS NULL AND [VehicleId] IS NULL AND [RiderProfileId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SupplyRequests_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_RiderInventoryIssues_RiderInventoryIssueId",
                        column: x => x.RiderInventoryIssueId,
                        principalSchema: "maintenance",
                        principalTable: "RiderInventoryIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequests_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyRequestLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventorySupplyRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MaintenanceUsageType = table.Column<int>(type: "int", nullable: true),
                    ExpectedReturn = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderInventoryIssueLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SupplyRequestLines", x => x.Id);
                    table.CheckConstraint("CK_InventorySupplyRequestLines_Values", "[RequestedQuantity] > 0 AND [IssuedQuantity] >= 0 AND [IssuedQuantity] <= [RequestedQuantity] AND [IssuedCost] >= 0");
                    table.ForeignKey(
                        name: "FK_SupplyRequestLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequestLines_MaterialUsages_MaintenanceMaterialUsageId",
                        column: x => x.MaintenanceMaterialUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequestLines_RiderInventoryIssueLines_RiderInventoryIssueLineId",
                        column: x => x.RiderInventoryIssueLineId,
                        principalSchema: "maintenance",
                        principalTable: "RiderInventoryIssueLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyRequestLines_SupplyRequests_InventorySupplyRequestId",
                        column: x => x.InventorySupplyRequestId,
                        principalSchema: "maintenance",
                        principalTable: "SupplyRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000114"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إرسال طلب موحد لقطع صيانة مركبة أو عهدة رايدر دون خصم المخزون.", "Submit a vehicle-maintenance or rider supply request without deducting stock.", 114, null, false, false, false, false, "inventory.supply_requests.submit", "إرسال طلبات الصرف", "Submit supply requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000115"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض طابور طلبات الصرف وحالة الموافقة والمركبة أو الرايدر المرتبط.", "View the supply-request queue, approval state, and related vehicle or rider.", 115, null, false, false, false, false, "inventory.supply_requests.read", "عرض طلبات الصرف", "Read supply requests", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000116"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد الطلب وتسليم الأصناف فعليًا وترحيل خصم FIFO في عملية واحدة.", "Approve a request, physically issue its items, and post FIFO stock deduction atomically.", 116, "HIGH_TRUST_ONLY", false, false, true, true, "inventory.supply_requests.approve", "اعتماد وتسليم طلبات الصرف", "Approve and issue supply requests", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestLines_InventoryItemId",
                schema: "maintenance",
                table: "SupplyRequestLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestLines_InventorySupplyRequestId_InventoryItemId",
                schema: "maintenance",
                table: "SupplyRequestLines",
                columns: new[] { "InventorySupplyRequestId", "InventoryItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestLines_IsDeleted",
                schema: "maintenance",
                table: "SupplyRequestLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestLines_MaintenanceMaterialUsageId",
                schema: "maintenance",
                table: "SupplyRequestLines",
                column: "MaintenanceMaterialUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequestLines_RiderInventoryIssueLineId",
                schema: "maintenance",
                table: "SupplyRequestLines",
                column: "RiderInventoryIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_InventoryLocationId_Status_RequestedAtUtc",
                schema: "maintenance",
                table: "SupplyRequests",
                columns: new[] { "InventoryLocationId", "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_IsDeleted",
                schema: "maintenance",
                table: "SupplyRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "SupplyRequests",
                column: "MaintenanceWorkOrderId",
                unique: true,
                filter: "[MaintenanceWorkOrderId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_RequestNumber",
                schema: "maintenance",
                table: "SupplyRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_RiderInventoryIssueId",
                schema: "maintenance",
                table: "SupplyRequests",
                column: "RiderInventoryIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_RiderProfileId_RequestedAtUtc",
                schema: "maintenance",
                table: "SupplyRequests",
                columns: new[] { "RiderProfileId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_VehicleId",
                schema: "maintenance",
                table: "SupplyRequests",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplyRequestLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "SupplyRequests",
                schema: "maintenance");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000114"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000115"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000116"));
        }
    }
}
