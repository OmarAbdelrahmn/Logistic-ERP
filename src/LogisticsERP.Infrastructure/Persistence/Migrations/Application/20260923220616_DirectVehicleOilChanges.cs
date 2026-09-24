using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class DirectVehicleOilChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OilChangeOperations_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE operation
                SET operation.VehicleId = workOrder.VehicleId
                FROM maintenance.OilChangeOperations AS operation
                INNER JOIN maintenance.WorkOrders AS workOrder
                    ON workOrder.Id = operation.MaintenanceWorkOrderId
                WHERE workOrder.VehicleId IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "MaterialUsages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_IdempotencyKey",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "MaintenanceWorkOrderId",
                unique: true,
                filter: "[MaintenanceWorkOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_VehicleId_PerformedAtUtc",
                schema: "maintenance",
                table: "OilChangeOperations",
                columns: new[] { "VehicleId", "PerformedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_OilChangeOperations_Vehicles_VehicleId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "VehicleId",
                principalSchema: "app",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM maintenance.OilChangeOperations WHERE MaintenanceWorkOrderId IS NULL)
                   OR EXISTS (SELECT 1 FROM maintenance.MaterialUsages WHERE MaintenanceWorkOrderId IS NULL)
                    THROW 50000, 'Direct oil changes must be removed or migrated before rolling back.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_OilChangeOperations_Vehicles_VehicleId",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropIndex(
                name: "IX_OilChangeOperations_IdempotencyKey",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropIndex(
                name: "IX_OilChangeOperations_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropIndex(
                name: "IX_OilChangeOperations_VehicleId_PerformedAtUtc",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "MaterialUsages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "MaintenanceWorkOrderId",
                unique: true);
        }
    }
}
