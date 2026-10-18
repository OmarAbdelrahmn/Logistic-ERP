using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowChefzAndJahezRiderAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments",
                sql: "[RiderAccountSlot] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderClientAssignments_RiderAccountSlot",
                schema: "app",
                table: "RiderClientAssignments",
                sql: "[RiderAccountSlot] IN (1, 2)");
        }
    }
}
