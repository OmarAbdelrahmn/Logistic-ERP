using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehiclePlatformAccountAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehiclePlatformAccountAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePlatformAccountAssignments", x => x.Id);
                    table.CheckConstraint("CK_VehiclePlatformAccountAssignments_AlwaysApproved", "[ApprovalStatus] = 1");
                    table.CheckConstraint("CK_VehiclePlatformAccountAssignments_Status", "([Status] = 1 AND [EndedAtUtc] IS NULL) OR ([Status] = 2 AND [EndedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_VehiclePlatformAccountAssignments_TimeRange", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [AssignedAtUtc]");
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountAssignments_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_IsDeleted",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_PlatformRiderAccountId_EndedAtUtc",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                columns: new[] { "PlatformRiderAccountId", "EndedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_Status_ApprovedAtUtc",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                columns: new[] { "Status", "ApprovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_VehicleId_ApprovedAtUtc",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                columns: new[] { "VehicleId", "ApprovedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_VehicleId_EndedAtUtc",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                columns: new[] { "VehicleId", "EndedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountAssignments_VehicleId_PlatformRiderAccountId",
                schema: "app",
                table: "VehiclePlatformAccountAssignments",
                columns: new[] { "VehicleId", "PlatformRiderAccountId" },
                filter: "[EndedAtUtc] IS NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehiclePlatformAccountAssignments",
                schema: "app");
        }
    }
}
