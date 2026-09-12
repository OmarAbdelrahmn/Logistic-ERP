using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHrDashboardReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_SponsorId",
                schema: "app",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SponsorId_Status_IsEmployee",
                schema: "app",
                table: "Employees",
                columns: new[] { "SponsorId", "Status", "IsEmployee" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Status_IsEmployee",
                schema: "app",
                table: "Employees",
                columns: new[] { "Status", "IsEmployee" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_SponsorId_Status_IsEmployee",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Status_IsEmployee",
                schema: "app",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SponsorId",
                schema: "app",
                table: "Employees",
                column: "SponsorId");
        }
    }
}
