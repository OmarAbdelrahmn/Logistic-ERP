using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakeVehicleTakeReturnReasonOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                schema: "app",
                table: "VehicleOperationalStatusPeriods",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Not provided.",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "AssignmentReason",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Not provided.",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                schema: "app",
                table: "RiderVehicleAssignmentEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Not provided.",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                schema: "app",
                table: "VehicleOperationalStatusPeriods",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldDefaultValue: "Not provided.");

            migrationBuilder.AlterColumn<string>(
                name: "AssignmentReason",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldDefaultValue: "Not provided.");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                schema: "app",
                table: "RiderVehicleAssignmentEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldDefaultValue: "Not provided.");
        }
    }
}
