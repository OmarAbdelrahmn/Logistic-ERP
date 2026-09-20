using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowMotorcycleOperationCardsAndRepeatedNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleOperationCards_VehicleId_CardNumber",
                schema: "app",
                table: "VehicleOperationCards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_VehicleId_CardNumber",
                schema: "app",
                table: "VehicleOperationCards",
                columns: new[] { "VehicleId", "CardNumber" },
                unique: true);
        }
    }
}
