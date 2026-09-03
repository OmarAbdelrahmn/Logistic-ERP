using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleReturnConditionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedRepairCost",
                schema: "app",
                table: "VehicleIssues",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRiderResponsible",
                schema: "app",
                table: "VehicleIssues",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleIssueEvidenceFiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_VehicleIssueEvidenceFiles", x => x.Id);
                    table.CheckConstraint("CK_VehicleIssueEvidenceFiles_Size", "[FileSizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleIssueEvidenceFiles_VehicleIssues_VehicleIssueId",
                        column: x => x.VehicleIssueId,
                        principalSchema: "app",
                        principalTable: "VehicleIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleIssues_EstimatedRepairCost",
                schema: "app",
                table: "VehicleIssues",
                sql: "[EstimatedRepairCost] IS NULL OR [EstimatedRepairCost] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssueEvidenceFiles_IsDeleted",
                schema: "app",
                table: "VehicleIssueEvidenceFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssueEvidenceFiles_VehicleIssueId_UploadedAtUtc",
                schema: "app",
                table: "VehicleIssueEvidenceFiles",
                columns: new[] { "VehicleIssueId", "UploadedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleIssueEvidenceFiles",
                schema: "app");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleIssues_EstimatedRepairCost",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropColumn(
                name: "EstimatedRepairCost",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropColumn(
                name: "IsRiderResponsible",
                schema: "app",
                table: "VehicleIssues");
        }
    }
}
