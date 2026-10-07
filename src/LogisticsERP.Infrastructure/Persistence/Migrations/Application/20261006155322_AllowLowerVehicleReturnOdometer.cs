using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowLowerVehicleReturnOdometer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderVehicleAssignments_Odometer",
                schema: "app",
                table: "RiderVehicleAssignments");
            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderVehicleAssignments_Odometer",
                schema: "app",
                table: "RiderVehicleAssignments",
                sql: "[StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= 0)");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderVehicleAssignments_Odometer",
                schema: "app",
                table: "RiderVehicleAssignments");
            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderVehicleAssignments_Odometer",
                schema: "app",
                table: "RiderVehicleAssignments",
                sql: "[StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= [StartOdometer] OR [CorrectionReason] IS NOT NULL)");

        }
    }
}
