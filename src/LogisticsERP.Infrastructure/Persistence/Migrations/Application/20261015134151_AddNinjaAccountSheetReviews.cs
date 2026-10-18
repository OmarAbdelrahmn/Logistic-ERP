using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaAccountSheetReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NinjaAccountUpdateUploads",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IgnoredFooterRows = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NinjaAccountUpdateUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NinjaAccountUpdateUploads_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NinjaAccountUpdateRows",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    CaptainId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SheetOwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SheetCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SheetIqamaNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SheetStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SheetSuspended = table.Column<bool>(type: "bit", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HasProblems = table.Column<bool>(type: "bit", nullable: false),
                    CityMismatch = table.Column<bool>(type: "bit", nullable: false),
                    IqamaProblem = table.Column<bool>(type: "bit", nullable: false),
                    NaqlBlocked = table.Column<bool>(type: "bit", nullable: false),
                    SystemSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProblemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaAccountUpdateRows", x => x.Id);
                    table.CheckConstraint("CK_NinjaAccountUpdateRows_MatchStatus", "[MatchStatus] IN ('Found', 'NotFound', 'Ambiguous')");
                    table.ForeignKey(
                        name: "FK_NinjaAccountUpdateRows_NinjaAccountUpdateUploads_UploadId",
                        column: x => x.UploadId,
                        principalSchema: "ninja",
                        principalTable: "NinjaAccountUpdateUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaAccountUpdateRows_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountUpdateRows_PlatformRiderAccountId_CreatedAtUtc_Id",
                schema: "ninja",
                table: "NinjaAccountUpdateRows",
                columns: new[] { "PlatformRiderAccountId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountUpdateRows_UploadId_CaptainId",
                schema: "ninja",
                table: "NinjaAccountUpdateRows",
                columns: new[] { "UploadId", "CaptainId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountUpdateRows_UploadId_SourceRowNumber",
                schema: "ninja",
                table: "NinjaAccountUpdateRows",
                columns: new[] { "UploadId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountUpdateUploads_ClientPlatformId_CreatedAtUtc_Id",
                schema: "ninja",
                table: "NinjaAccountUpdateUploads",
                columns: new[] { "ClientPlatformId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_NinjaAccountUpdateUploads_IsDeleted",
                schema: "ninja",
                table: "NinjaAccountUpdateUploads",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NinjaAccountUpdateRows",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "NinjaAccountUpdateUploads",
                schema: "ninja");
        }
    }
}
