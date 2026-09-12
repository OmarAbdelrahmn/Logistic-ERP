using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleRegistrationTransitionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleRegistrationTransitionSnapshots",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleRegistrationTransitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldVehicleDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewVehicleDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRegistrationTransitionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleRegistrationTransitionSnapshots_VehicleRegistrationTransitions_VehicleRegistrationTransitionId",
                        column: x => x.VehicleRegistrationTransitionId,
                        principalSchema: "app",
                        principalTable: "VehicleRegistrationTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrationTransitionSnapshots_VehicleRegistrationTransitionId",
                schema: "app",
                table: "VehicleRegistrationTransitionSnapshots",
                column: "VehicleRegistrationTransitionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleRegistrationTransitionSnapshots",
                schema: "app");
        }
    }
}
