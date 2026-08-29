using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHrFormTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrFormTemplates",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CurrentDraftVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentPublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_HrFormTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HrFormTemplateVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HrFormTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    DefinitionSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitionSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ChangeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrFormTemplateVersions", x => x.Id);
                    table.CheckConstraint("CK_HrFormTemplateVersions_VersionNumbers", "[VersionNumber] > 0 AND [DefinitionSchemaVersion] > 0");
                    table.ForeignKey(
                        name: "FK_HrFormTemplateVersions_HrFormTemplates_HrFormTemplateId",
                        column: x => x.HrFormTemplateId,
                        principalSchema: "app",
                        principalTable: "HrFormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000083"), "HrForms", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض القوالب المنشورة ومسودات تصميم نماذج الموارد البشرية.", "View published HR form templates and design drafts.", 83, null, false, false, false, false, "hr_forms.templates.read", "عرض قوالب نماذج الموارد البشرية", "Read HR form templates", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000084"), "HrForms", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء إصدارات قوالب النماذج ونشرها وأرشفتها.", "Create, version, publish, and archive HR form templates.", 84, "HIGH_TRUST_ONLY", false, false, true, true, "hr_forms.templates.manage", "إدارة قوالب نماذج الموارد البشرية", "Manage HR form templates", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplates_Code",
                schema: "app",
                table: "HrFormTemplates",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplates_CurrentDraftVersionId",
                schema: "app",
                table: "HrFormTemplates",
                column: "CurrentDraftVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplates_CurrentPublishedVersionId",
                schema: "app",
                table: "HrFormTemplates",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplates_IsActive_Category_NameAr",
                schema: "app",
                table: "HrFormTemplates",
                columns: new[] { "IsActive", "Category", "NameAr" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplates_IsDeleted",
                schema: "app",
                table: "HrFormTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplateVersions_DefinitionSha256",
                schema: "app",
                table: "HrFormTemplateVersions",
                column: "DefinitionSha256");

            migrationBuilder.CreateIndex(
                name: "IX_HrFormTemplateVersions_HrFormTemplateId_VersionNumber",
                schema: "app",
                table: "HrFormTemplateVersions",
                columns: new[] { "HrFormTemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HrFormTemplates_HrFormTemplateVersions_CurrentDraftVersionId",
                schema: "app",
                table: "HrFormTemplates",
                column: "CurrentDraftVersionId",
                principalSchema: "app",
                principalTable: "HrFormTemplateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HrFormTemplates_HrFormTemplateVersions_CurrentPublishedVersionId",
                schema: "app",
                table: "HrFormTemplates",
                column: "CurrentPublishedVersionId",
                principalSchema: "app",
                principalTable: "HrFormTemplateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HrFormTemplates_HrFormTemplateVersions_CurrentDraftVersionId",
                schema: "app",
                table: "HrFormTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_HrFormTemplates_HrFormTemplateVersions_CurrentPublishedVersionId",
                schema: "app",
                table: "HrFormTemplates");

            migrationBuilder.DropTable(
                name: "HrFormTemplateVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HrFormTemplates",
                schema: "app");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000083"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000084"));
        }
    }
}
