using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleRegisteredOwnerSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles",
                column: "RegisteredOwnerSupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleSuppliers_RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles",
                column: "RegisteredOwnerSupplierId",
                principalSchema: "app",
                principalTable: "VehicleSuppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleSuppliers_RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegisteredOwnerSupplierId",
                schema: "app",
                table: "Vehicles");
        }
    }
}
