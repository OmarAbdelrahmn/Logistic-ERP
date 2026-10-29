using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MoveDayContributionsToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformAccountDayContributions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepresentativeAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiguresJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("PK_PlatformAccountDayContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAccountDayContributions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountDayContributions_RiderClientAssignments_RepresentativeAssignmentId",
                        column: x => x.RepresentativeAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayContributions_IsDeleted",
                schema: "app",
                table: "PlatformAccountDayContributions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayContributions_PlatformRiderAccountId_BusinessDate_RepresentativeAssignmentId",
                schema: "app",
                table: "PlatformAccountDayContributions",
                columns: new[] { "PlatformRiderAccountId", "BusinessDate", "RepresentativeAssignmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayContributions_RepresentativeAssignmentId",
                schema: "app",
                table: "PlatformAccountDayContributions",
                column: "RepresentativeAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM app.PlatformAccountDayContributions) THROW 51000, 'Cannot discard grouped day contribution evidence; use a forward correction.', 1;");
            migrationBuilder.DropTable(
                name: "PlatformAccountDayContributions",
                schema: "app");
        }
    }
}
