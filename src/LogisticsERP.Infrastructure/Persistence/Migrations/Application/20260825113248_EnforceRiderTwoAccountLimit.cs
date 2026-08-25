using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class EnforceRiderTwoAccountLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "RiderAccountSlot" },
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments",
                sql: "[RiderAccountSlot] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropColumn(
                name: "RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments");
        }
    }
}
