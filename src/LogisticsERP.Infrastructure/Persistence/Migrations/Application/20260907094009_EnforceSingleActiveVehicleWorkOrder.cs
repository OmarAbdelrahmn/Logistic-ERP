using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveVehicleWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_MaintenanceWorkOrders_ActiveVehicle",
                schema: "maintenance",
                table: "WorkOrders",
                column: "VehicleId",
                unique: true,
                filter: "[VehicleId] IS NOT NULL AND [Status] IN (1, 2, 3) AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MaintenanceWorkOrders_ActiveVehicle",
                schema: "maintenance",
                table: "WorkOrders");
        }
    }
}
