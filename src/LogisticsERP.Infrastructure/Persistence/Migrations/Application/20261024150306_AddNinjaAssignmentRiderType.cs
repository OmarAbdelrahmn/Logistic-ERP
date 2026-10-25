using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaAssignmentRiderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NinjaRiderType",
                schema: "app",
                table: "RiderClientAssignments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderClientAssignments_NinjaRiderType",
                schema: "app",
                table: "RiderClientAssignments",
                sql: "[NinjaRiderType] IS NULL OR [NinjaRiderType] IN ('InternalCar', 'ExternalCar', 'InternalMotorcycle', 'ExternalMotorcycle')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderClientAssignments_NinjaRiderType",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropColumn(
                name: "NinjaRiderType",
                schema: "app",
                table: "RiderClientAssignments");
        }
    }
}
