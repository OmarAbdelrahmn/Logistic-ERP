using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleDailyDistances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TrackedDistanceKm",
                schema: "app",
                table: "Vehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                "UPDATE [app].[Vehicles] SET [TrackedDistanceKm] = CONVERT(decimal(18,2), [CurrentOdometer]);");

            migrationBuilder.CreateTable(
                name: "VehicleDailyDistanceImports",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Sha256Checksum = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    TotalVehicleRows = table.Column<int>(type: "int", nullable: false),
                    GpsRows = table.Column<int>(type: "int", nullable: false),
                    NoGpsRows = table.Column<int>(type: "int", nullable: false),
                    MatchedRows = table.Column<int>(type: "int", nullable: false),
                    CreatedRows = table.Column<int>(type: "int", nullable: false),
                    UpdatedRows = table.Column<int>(type: "int", nullable: false),
                    UnmatchedRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    RowErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDailyDistanceImports", x => x.Id);
                    table.CheckConstraint("CK_VehicleDailyDistanceImports_Counts", "[TotalVehicleRows] >= 0 AND [GpsRows] >= 0 AND [NoGpsRows] >= 0 AND [MatchedRows] >= 0 AND [CreatedRows] >= 0 AND [UpdatedRows] >= 0 AND [UnmatchedRows] >= 0 AND [InvalidRows] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "VehicleDailyDistances",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GpsDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GpsPlateNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastGpsImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GpsImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GpsImportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualOdometerReading = table.Column<long>(type: "bigint", nullable: true),
                    ManualBaselineOdometerReading = table.Column<long>(type: "bigint", nullable: true),
                    ManualDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ManualEnteredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ManualEnteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppliedDistanceKm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedSource = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VehicleDailyDistances", x => x.Id);
                    table.CheckConstraint("CK_VehicleDailyDistances_AppliedDistance", "[AppliedDistanceKm] >= 0");
                    table.CheckConstraint("CK_VehicleDailyDistances_GpsDistance", "[GpsDistanceKm] IS NULL OR [GpsDistanceKm] >= 0");
                    table.CheckConstraint("CK_VehicleDailyDistances_ManualDistance", "[ManualDistanceKm] IS NULL OR [ManualDistanceKm] >= 0");
                    table.CheckConstraint("CK_VehicleDailyDistances_ManualOdometer", "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= [ManualBaselineOdometerReading])");
                    table.CheckConstraint("CK_VehicleDailyDistances_Source", "[AppliedSource] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_VehicleDailyDistances_VehicleDailyDistanceImports_LastGpsImportId",
                        column: x => x.LastGpsImportId,
                        principalSchema: "app",
                        principalTable: "VehicleDailyDistanceImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleDailyDistances_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vehicles_TrackedDistanceKm",
                schema: "app",
                table: "Vehicles",
                sql: "[TrackedDistanceKm] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistanceImports_CreatedAtUtc",
                schema: "app",
                table: "VehicleDailyDistanceImports",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistanceImports_WorkDate_Sha256Checksum",
                schema: "app",
                table: "VehicleDailyDistanceImports",
                columns: new[] { "WorkDate", "Sha256Checksum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistances_IsDeleted",
                schema: "app",
                table: "VehicleDailyDistances",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistances_LastGpsImportId",
                schema: "app",
                table: "VehicleDailyDistances",
                column: "LastGpsImportId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistances_VehicleId_WorkDate",
                schema: "app",
                table: "VehicleDailyDistances",
                columns: new[] { "VehicleId", "WorkDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDailyDistances_WorkDate_AppliedSource",
                schema: "app",
                table: "VehicleDailyDistances",
                columns: new[] { "WorkDate", "AppliedSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleDailyDistances",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleDailyDistanceImports",
                schema: "app");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehicles_TrackedDistanceKm",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TrackedDistanceKm",
                schema: "app",
                table: "Vehicles");
        }
    }
}
