using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class LinkMechanicPaymentsToEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_MechanicEmployeeId",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                column: "MechanicEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalFinancialEntries_Employees_MechanicEmployeeId",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                column: "MechanicEmployeeId",
                principalSchema: "app",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalFinancialEntries_Employees_MechanicEmployeeId",
                schema: "maintenance",
                table: "ExternalFinancialEntries");

            migrationBuilder.DropIndex(
                name: "IX_ExternalFinancialEntries_MechanicEmployeeId",
                schema: "maintenance",
                table: "ExternalFinancialEntries");
        }
    }
}
