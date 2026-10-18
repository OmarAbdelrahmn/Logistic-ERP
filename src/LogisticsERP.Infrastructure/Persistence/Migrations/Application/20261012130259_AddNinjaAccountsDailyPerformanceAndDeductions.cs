using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaAccountsDailyPerformanceAndDeductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ninja");

            migrationBuilder.CreateTable(
                name: "NinjaAccountDetails",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_NinjaAccountDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NinjaAccountDetails_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NinjaDailyImports",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDeductions = table.Column<bool>(type: "bit", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedRows = table.Column<int>(type: "int", nullable: false),
                    SkippedRows = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaDailyImports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NinjaDailyDeductions",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalCollectedAmountAtDelivery = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaptainId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaDailyDeductions", x => x.Id);
                    table.CheckConstraint("CK_NinjaDeductions_CollectedAmount", "[TotalCollectedAmountAtDelivery] >= 0");
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductions_NinjaDailyImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductions_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductions_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NinjaDailyPerformance",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    TotalWorkingHours = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaptainId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaDailyPerformance", x => x.Id);
                    table.CheckConstraint("CK_NinjaPerformance_Values", "[TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24");
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformance_NinjaDailyImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformance_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformance_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformance_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountDetails_IsDeleted",
                schema: "ninja",
                table: "NinjaAccountDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountDetails_PlatformRiderAccountId",
                schema: "ninja",
                table: "NinjaAccountDetails",
                column: "PlatformRiderAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_ImportId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_RiderProfileId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                columns: new[] { "RiderProfileId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyImports_ReportDate_IsDeductions_ContentHash",
                schema: "ninja",
                table: "NinjaDailyImports",
                columns: new[] { "ReportDate", "IsDeductions", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_ImportId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_RiderProfileId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                columns: new[] { "RiderProfileId", "ReportDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NinjaAccountDetails",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "NinjaDailyDeductions",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "NinjaDailyPerformance",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "NinjaDailyImports",
                schema: "ninja");
        }
    }
}
