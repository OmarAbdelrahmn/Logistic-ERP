using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddRealRiderToVehicleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRealRider",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "RealRiders",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IqamaNo = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    RelationshipToAssignedRider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealRiders", x => x.Id);
                    table.CheckConstraint("CK_RealRiders_IqamaNo", "LEN([IqamaNo]) = 10 AND [IqamaNo] NOT LIKE '%[^0-9]%'");
                    table.ForeignKey(
                        name: "FK_RealRiders_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealRiders_RiderVehicleAssignmentId",
                schema: "app",
                table: "RealRiders",
                column: "RiderVehicleAssignmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealRiders",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "IsRealRider",
                schema: "app",
                table: "RiderVehicleAssignments");
        }
    }
}
