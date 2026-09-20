using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHrLegalCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrLegalCases",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PersonType = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorPartyRole = table.Column<int>(type: "int", nullable: false),
                    CaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResponsibleUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_HrLegalCases", x => x.Id);
                    table.CheckConstraint("CK_HrLegalCases_PersonReference", "([PersonType] = 1 AND [EmployeeId] IS NOT NULL AND [RiderProfileId] IS NULL) OR ([PersonType] = 2 AND [EmployeeId] IS NULL AND [RiderProfileId] IS NOT NULL) OR ([PersonType] = 3 AND [EmployeeId] IS NULL AND [RiderProfileId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_HrLegalCases_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrLegalCases_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrLegalCases_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrLegalCaseHearings",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HearingNumber = table.Column<int>(type: "int", nullable: false),
                    HearingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HearingTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_HrLegalCaseHearings", x => x.Id);
                    table.CheckConstraint("CK_HrLegalCaseHearings_Number", "[HearingNumber] > 0");
                    table.ForeignKey(
                        name: "FK_HrLegalCaseHearings_HrLegalCases_LegalCaseId",
                        column: x => x.LegalCaseId,
                        principalSchema: "app",
                        principalTable: "HrLegalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrLegalCaseHearingFiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HearingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_HrLegalCaseHearingFiles", x => x.Id);
                    table.CheckConstraint("CK_HrLegalCaseHearingFiles_Size", "[FileSizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_HrLegalCaseHearingFiles_HrLegalCaseHearings_HearingId",
                        column: x => x.HearingId,
                        principalSchema: "app",
                        principalTable: "HrLegalCaseHearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrLegalCaseHistory",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HearingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrLegalCaseHistory", x => x.Id);
                    table.CheckConstraint("CK_HrLegalCaseHistory_After", "ISJSON([AfterJson]) = 1");
                    table.CheckConstraint("CK_HrLegalCaseHistory_Before", "[BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1");
                    table.CheckConstraint("CK_HrLegalCaseHistory_ChangedFields", "ISJSON([ChangedFieldsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_HrLegalCaseHistory_HrLegalCaseHearings_HearingId",
                        column: x => x.HearingId,
                        principalSchema: "app",
                        principalTable: "HrLegalCaseHearings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrLegalCaseHistory_HrLegalCases_LegalCaseId",
                        column: x => x.LegalCaseId,
                        principalSchema: "app",
                        principalTable: "HrLegalCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000117"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض قضايا الموارد البشرية والجلسات وسجل التغييرات.", "View HR legal cases, hearings, and immutable change history.", 117, "SENSITIVE_DATA", false, false, false, true, "legal_cases.read", "عرض القضايا القانونية", "Read legal cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000118"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء وتعديل وأرشفة القضايا والجلسات ورفع الملفات.", "Create, update, and archive legal cases and hearings, and upload files.", 118, "SENSITIVE_DATA", false, false, false, true, "legal_cases.manage", "إدارة القضايا القانونية", "Manage legal cases", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000119"), "Workflows", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنزيل ملفات جلسات القضايا الخاصة.", "Download private legal-case hearing files.", 119, "HIGH_TRUST_ONLY", false, false, true, true, "legal_cases.files.download", "تنزيل ملفات القضايا", "Download legal case files", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHearingFiles_HearingId_UploadedAtUtc",
                schema: "app",
                table: "HrLegalCaseHearingFiles",
                columns: new[] { "HearingId", "UploadedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHearingFiles_IsDeleted",
                schema: "app",
                table: "HrLegalCaseHearingFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHearings_IsDeleted",
                schema: "app",
                table: "HrLegalCaseHearings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHearings_LegalCaseId_HearingNumber",
                schema: "app",
                table: "HrLegalCaseHearings",
                columns: new[] { "LegalCaseId", "HearingNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHearings_Status_HearingDate_HearingTime",
                schema: "app",
                table: "HrLegalCaseHearings",
                columns: new[] { "Status", "HearingDate", "HearingTime" });

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHistory_HearingId",
                schema: "app",
                table: "HrLegalCaseHistory",
                column: "HearingId");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCaseHistory_LegalCaseId_CreatedAtUtc",
                schema: "app",
                table: "HrLegalCaseHistory",
                columns: new[] { "LegalCaseId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_CaseNumber",
                schema: "app",
                table: "HrLegalCases",
                column: "CaseNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_EmployeeId",
                schema: "app",
                table: "HrLegalCases",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_IsDeleted",
                schema: "app",
                table: "HrLegalCases",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_ResponsibleUserId",
                schema: "app",
                table: "HrLegalCases",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_RiderProfileId",
                schema: "app",
                table: "HrLegalCases",
                column: "RiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_SponsorId",
                schema: "app",
                table: "HrLegalCases",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_HrLegalCases_Status_CaseDate_CaseTime",
                schema: "app",
                table: "HrLegalCases",
                columns: new[] { "Status", "CaseDate", "CaseTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrLegalCaseHearingFiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HrLegalCaseHistory",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HrLegalCaseHearings",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HrLegalCases",
                schema: "app");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000117"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000118"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000119"));
        }
    }
}
