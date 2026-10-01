using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class ScopeHousingRoomNumbersToFloor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HousingRooms_FloorId",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.DropIndex(
                name: "IX_HousingRooms_HousingId_Name",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.CreateIndex(
                name: "IX_HousingRooms_FloorId_Name",
                schema: "app",
                table: "HousingRooms",
                columns: new[] { "FloorId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HousingRooms_FloorId_Name",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.CreateIndex(
                name: "IX_HousingRooms_FloorId",
                schema: "app",
                table: "HousingRooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingRooms_HousingId_Name",
                schema: "app",
                table: "HousingRooms",
                columns: new[] { "HousingId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
