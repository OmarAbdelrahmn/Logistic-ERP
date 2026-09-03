using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddSponsorVehicleLeaseAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SponsorVehicleLeaseAgreements",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessorSponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LesseeSponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AgreementReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorVehicleLeaseAgreements", x => x.Id);
                    table.CheckConstraint("CK_SponsorVehicleLeaseAgreements_DifferentSponsors", "[LessorSponsorId] <> [LesseeSponsorId]");
                    table.CheckConstraint("CK_SponsorVehicleLeaseAgreements_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_SponsorVehicleLeaseAgreements_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorVehicleLeaseAgreements_Sponsors_LesseeSponsorId",
                        column: x => x.LesseeSponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorVehicleLeaseAgreements_Sponsors_LessorSponsorId",
                        column: x => x.LessorSponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SponsorVehicleLeaseAgreementVehicles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorVehicleLeaseAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorVehicleLeaseAgreementVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorVehicleLeaseAgreementVehicles_SponsorVehicleLeaseAgreements_SponsorVehicleLeaseAgreementId",
                        column: x => x.SponsorVehicleLeaseAgreementId,
                        principalSchema: "app",
                        principalTable: "SponsorVehicleLeaseAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorVehicleLeaseAgreementVehicles_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreements_ClientPlatformId_EffectiveFrom_EffectiveTo",
                schema: "app",
                table: "SponsorVehicleLeaseAgreements",
                columns: new[] { "ClientPlatformId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreements_ClientPlatformId_LessorSponsorId_LesseeSponsorId_EffectiveFrom",
                schema: "app",
                table: "SponsorVehicleLeaseAgreements",
                columns: new[] { "ClientPlatformId", "LessorSponsorId", "LesseeSponsorId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreements_LesseeSponsorId",
                schema: "app",
                table: "SponsorVehicleLeaseAgreements",
                column: "LesseeSponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreements_LessorSponsorId",
                schema: "app",
                table: "SponsorVehicleLeaseAgreements",
                column: "LessorSponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreementVehicles_SponsorVehicleLeaseAgreementId_VehicleId",
                schema: "app",
                table: "SponsorVehicleLeaseAgreementVehicles",
                columns: new[] { "SponsorVehicleLeaseAgreementId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SponsorVehicleLeaseAgreementVehicles_VehicleId_SponsorVehicleLeaseAgreementId",
                schema: "app",
                table: "SponsorVehicleLeaseAgreementVehicles",
                columns: new[] { "VehicleId", "SponsorVehicleLeaseAgreementId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SponsorVehicleLeaseAgreementVehicles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "SponsorVehicleLeaseAgreements",
                schema: "app");
        }
    }
}
