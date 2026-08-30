using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehiclePlatformAccountSwitches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehiclePlatformAccountSwitches",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VehiclePlatformAccountSwitches", x => x.Id);
                    table.CheckConstraint("CK_VehiclePlatformAccountSwitches_Acceptance", "([Status] = 1 AND [EffectiveAtUtc] IS NULL AND [AcceptedAtUtc] IS NULL AND [AcceptedByUserId] IS NULL AND [NewAssignmentId] IS NULL) OR ([Status] = 2 AND [EffectiveAtUtc] IS NOT NULL AND [AcceptedAtUtc] IS NOT NULL AND [AcceptedByUserId] IS NOT NULL AND [NewAssignmentId] IS NOT NULL)");
                    table.CheckConstraint("CK_VehiclePlatformAccountSwitches_AcceptedAfterRequested", "[AcceptedAtUtc] IS NULL OR [AcceptedAtUtc] >= [RequestedAtUtc]");
                    table.CheckConstraint("CK_VehiclePlatformAccountSwitches_DifferentVehicles", "[SourceVehicleId] <> [TargetVehicleId]");
                    table.CheckConstraint("CK_VehiclePlatformAccountSwitches_ModeStatus", "([Mode] = 1 AND [Status] = 2) OR ([Mode] = 2 AND [Status] IN (1, 2))");
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountSwitches_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountSwitches_VehiclePlatformAccountAssignments_NewAssignmentId",
                        column: x => x.NewAssignmentId,
                        principalSchema: "app",
                        principalTable: "VehiclePlatformAccountAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountSwitches_VehiclePlatformAccountAssignments_SourceAssignmentId",
                        column: x => x.SourceAssignmentId,
                        principalSchema: "app",
                        principalTable: "VehiclePlatformAccountAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountSwitches_Vehicles_SourceVehicleId",
                        column: x => x.SourceVehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePlatformAccountSwitches_Vehicles_TargetVehicleId",
                        column: x => x.TargetVehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_IsDeleted",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_NewAssignmentId",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                column: "NewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_PlatformRiderAccountId_Status",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                columns: new[] { "PlatformRiderAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_SourceAssignmentId",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                column: "SourceAssignmentId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_SourceVehicleId",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                column: "SourceVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_Status_RequestedAtUtc",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePlatformAccountSwitches_TargetVehicleId_Status",
                schema: "app",
                table: "VehiclePlatformAccountSwitches",
                columns: new[] { "TargetVehicleId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehiclePlatformAccountSwitches",
                schema: "app");
        }
    }
}
