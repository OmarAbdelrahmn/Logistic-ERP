using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPhoneSimManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneSimCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedPhoneNumber = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Iccid = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true),
                    NormalizedIccid = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true),
                    CarrierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PhoneSimCards", x => x.Id);
                    table.CheckConstraint("CK_PhoneSimCards_Status", "[Status] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_PhoneSimCards_Employees_ResponsibleEmployeeId",
                        column: x => x.ResponsibleEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhoneSimResponsibilityChanges",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneSimCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousResponsibleEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneSimResponsibilityChanges", x => x.Id);
                    table.CheckConstraint("CK_PhoneSimResponsibilityChanges_ChangedResponsibleEmployee", "[PreviousResponsibleEmployeeId] IS NULL OR [PreviousResponsibleEmployeeId] <> [ResponsibleEmployeeId]");
                    table.ForeignKey(
                        name: "FK_PhoneSimResponsibilityChanges_Employees_PreviousResponsibleEmployeeId",
                        column: x => x.PreviousResponsibleEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneSimResponsibilityChanges_Employees_ResponsibleEmployeeId",
                        column: x => x.ResponsibleEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneSimResponsibilityChanges_PhoneSimCards_PhoneSimCardId",
                        column: x => x.PhoneSimCardId,
                        principalSchema: "app",
                        principalTable: "PhoneSimCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderPhoneSimAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneSimCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_RiderPhoneSimAssignments", x => x.Id);
                    table.CheckConstraint("CK_RiderPhoneSimAssignments_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_RiderPhoneSimAssignments_PhoneSimCards_PhoneSimCardId",
                        column: x => x.PhoneSimCardId,
                        principalSchema: "app",
                        principalTable: "PhoneSimCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderPhoneSimAssignments_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000085"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض شرائح الاتصال والمسؤول الحالي وسجل تسليمها للمناديب.", "View phone SIM inventory, current responsible employees, and rider assignment history.", 85, "SENSITIVE_DATA", false, false, false, true, "phone_sims.read", "عرض شرائح الاتصال", "Read phone SIMs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000086"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة بيانات شرائح الاتصال والمسؤولين وتسليم الشرائح للمناديب وإرجاعها.", "Manage phone SIM details, responsible employees, rider assignments, and returns.", 86, "SENSITIVE_DATA", false, false, false, true, "phone_sims.manage", "إدارة شرائح الاتصال", "Manage phone SIMs", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_IsDeleted",
                schema: "app",
                table: "PhoneSimCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_NormalizedIccid",
                schema: "app",
                table: "PhoneSimCards",
                column: "NormalizedIccid",
                unique: true,
                filter: "[NormalizedIccid] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_NormalizedPhoneNumber",
                schema: "app",
                table: "PhoneSimCards",
                column: "NormalizedPhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_ResponsibleEmployeeId",
                schema: "app",
                table: "PhoneSimCards",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_Status_ResponsibleEmployeeId",
                schema: "app",
                table: "PhoneSimCards",
                columns: new[] { "Status", "ResponsibleEmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimResponsibilityChanges_PhoneSimCardId_ChangedAtUtc",
                schema: "app",
                table: "PhoneSimResponsibilityChanges",
                columns: new[] { "PhoneSimCardId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimResponsibilityChanges_PreviousResponsibleEmployeeId",
                schema: "app",
                table: "PhoneSimResponsibilityChanges",
                column: "PreviousResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimResponsibilityChanges_ResponsibleEmployeeId_ChangedAtUtc",
                schema: "app",
                table: "PhoneSimResponsibilityChanges",
                columns: new[] { "ResponsibleEmployeeId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderPhoneSimAssignments_PhoneSimCardId",
                schema: "app",
                table: "RiderPhoneSimAssignments",
                column: "PhoneSimCardId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RiderPhoneSimAssignments_PhoneSimCardId_EffectiveFrom",
                schema: "app",
                table: "RiderPhoneSimAssignments",
                columns: new[] { "PhoneSimCardId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderPhoneSimAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderPhoneSimAssignments",
                columns: new[] { "RiderProfileId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneSimResponsibilityChanges",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderPhoneSimAssignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PhoneSimCards",
                schema: "app");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000085"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000086"));
        }
    }
}
