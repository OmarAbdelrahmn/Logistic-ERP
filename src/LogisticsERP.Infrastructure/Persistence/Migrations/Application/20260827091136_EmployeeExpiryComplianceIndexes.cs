using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class EmployeeExpiryComplianceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RiderHealthCards_IsCurrent_ExpiryDate_RiderProfileId",
                schema: "app",
                table: "RiderHealthCards",
                columns: new[] { "IsCurrent", "ExpiryDate", "RiderProfileId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderCards_IsCurrent_ExpiryDate_RiderProfileId",
                schema: "app",
                table: "RiderCards",
                columns: new[] { "IsCurrent", "ExpiryDate", "RiderProfileId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_IsCurrent_EndDate_EmployeeId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies",
                columns: new[] { "IsCurrent", "EndDate", "EmployeeId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDriverLicenses_IsCurrent_ExpiryDate_EmployeeId",
                schema: "app",
                table: "EmployeeDriverLicenses",
                columns: new[] { "IsCurrent", "ExpiryDate", "EmployeeId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_Status_ExpiryDate_EmployeeId",
                schema: "app",
                table: "EmployeeDocuments",
                columns: new[] { "Status", "ExpiryDate", "EmployeeId" },
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderHealthCards_IsCurrent_ExpiryDate_RiderProfileId",
                schema: "app",
                table: "RiderHealthCards");

            migrationBuilder.DropIndex(
                name: "IX_RiderCards_IsCurrent_ExpiryDate_RiderProfileId",
                schema: "app",
                table: "RiderCards");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeMedicalInsurancePolicies_IsCurrent_EndDate_EmployeeId",
                schema: "app",
                table: "EmployeeMedicalInsurancePolicies");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDriverLicenses_IsCurrent_ExpiryDate_EmployeeId",
                schema: "app",
                table: "EmployeeDriverLicenses");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_Status_ExpiryDate_EmployeeId",
                schema: "app",
                table: "EmployeeDocuments");
        }
    }
}
