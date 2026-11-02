using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowDailyOdometerCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances",
                sql: "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= 0 AND ([IsOdometerCorrection] = 1 OR [ManualOdometerReading] >= [ManualBaselineOdometerReading]))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances",
                sql: "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= [ManualBaselineOdometerReading])");
        }
    }
}
