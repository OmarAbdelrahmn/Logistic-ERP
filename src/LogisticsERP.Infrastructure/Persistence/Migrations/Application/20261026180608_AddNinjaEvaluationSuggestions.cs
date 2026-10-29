using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaEvaluationSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllocationProvenance",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationProvenance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationProvenance_NinjaDailyPerformanceSources_SourceId",
                        column: x => x.SourceId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyPerformanceSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllocationProvenance_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationSnapshots",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InputRevision = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Frozen = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationSnapshots_EvaluationSnapshots_SupersedesId",
                        column: x => x.SupersedesId,
                        principalSchema: "ninja",
                        principalTable: "EvaluationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformancePreviews",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputRevision = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformancePreviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuggestionPolicies",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationContexts",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedFirstDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReviewedSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PricingPeriodsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationContexts_EvaluationContexts_SupersedesId",
                        column: x => x.SupersedesId,
                        principalSchema: "ninja",
                        principalTable: "EvaluationContexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationContexts_EvaluationSnapshots_ReviewedSnapshotId",
                        column: x => x.ReviewedSnapshotId,
                        principalSchema: "ninja",
                        principalTable: "EvaluationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationContexts_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityRevisions",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsibilityRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityRevisions_EvaluationSnapshots_ReviewedSnapshotId",
                        column: x => x.ReviewedSnapshotId,
                        principalSchema: "ninja",
                        principalTable: "EvaluationSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityRevisions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityRevisions_ResponsibilityRevisions_SupersedesId",
                        column: x => x.SupersedesId,
                        principalSchema: "ninja",
                        principalTable: "ResponsibilityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceArtifacts",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceArtifacts_PerformanceArtifacts_OriginalArtifactId",
                        column: x => x.OriginalArtifactId,
                        principalSchema: "ninja",
                        principalTable: "PerformanceArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceArtifacts_PerformancePreviews_PreviewId",
                        column: x => x.PreviewId,
                        principalSchema: "ninja",
                        principalTable: "PerformancePreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityLines",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsibilityLines", x => x.Id);
                    table.CheckConstraint("CK_NinjaResponsibility_Weight", "[Weight] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_ResponsibilityLines_ResponsibilityRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalSchema: "ninja",
                        principalTable: "ResponsibilityRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityLines_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceImportArtifacts",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceImportArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceImportArtifacts_NinjaDailyImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerformanceImportArtifacts_PerformanceArtifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalSchema: "ninja",
                        principalTable: "PerformanceArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000317"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة المسؤولية والتسعير وحفظ نسخ التقييم التقديري.", "Manage suggestion responsibility, pricing context and snapshots.", 317, "SENSITIVE_DATA", false, false, false, true, "ninja.evaluation.manage", "إدارة تقييم نينجا", "Manage Ninja evaluation", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000318"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصدير التقييم التقديري المسموح.", "Export authorized Ninja monitoring suggestions.", 318, "SENSITIVE_DATA", false, false, false, true, "ninja.evaluation.export", "تصدير تقييم نينجا", "Export Ninja evaluation", null, true, false, null, null, 1 }
                });

            migrationBuilder.InsertData(
                schema: "ninja",
                table: "SuggestionPolicies",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "DefinitionJson", "Version" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000390"), new DateTimeOffset(new DateTime(2026, 10, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "{\"monthBasis\":\"CalendarDays\",\"attendance\":0.85,\"hours\":8,\"reduction\":0.15,\"rounding\":\"AwayFromZero\",\"bike\":[450,5700,11,16,17,18,9],\"car\":[460,6500,12,22,22.5,23,10],\"invoice\":\"External\"}", "NINJA-SUGGESTION-V1" });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationProvenance_RiderClientAssignmentId",
                schema: "ninja",
                table: "AllocationProvenance",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AllocationProvenance_SourceId_RiderClientAssignmentId",
                schema: "ninja",
                table: "AllocationProvenance",
                columns: new[] { "SourceId", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationContexts_PlatformRiderAccountId_Revision",
                schema: "ninja",
                table: "EvaluationContexts",
                columns: new[] { "PlatformRiderAccountId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationContexts_ReviewedSnapshotId",
                schema: "ninja",
                table: "EvaluationContexts",
                column: "ReviewedSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationContexts_SupersedesId",
                schema: "ninja",
                table: "EvaluationContexts",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSnapshots_CreatedAtUtc",
                schema: "ninja",
                table: "EvaluationSnapshots",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationSnapshots_SupersedesId",
                schema: "ninja",
                table: "EvaluationSnapshots",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceArtifacts_ContentHash",
                schema: "ninja",
                table: "PerformanceArtifacts",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceArtifacts_OriginalArtifactId",
                schema: "ninja",
                table: "PerformanceArtifacts",
                column: "OriginalArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceArtifacts_PreviewId",
                schema: "ninja",
                table: "PerformanceArtifacts",
                column: "PreviewId",
                unique: true,
                filter: "[PreviewId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceImportArtifacts_ArtifactId_ImportId",
                schema: "ninja",
                table: "PerformanceImportArtifacts",
                columns: new[] { "ArtifactId", "ImportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceImportArtifacts_ImportId",
                schema: "ninja",
                table: "PerformanceImportArtifacts",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformancePreviews_ExpiresAtUtc",
                schema: "ninja",
                table: "PerformancePreviews",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityLines_RevisionId_BusinessDate_RiderClientAssignmentId",
                schema: "ninja",
                table: "ResponsibilityLines",
                columns: new[] { "RevisionId", "BusinessDate", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityLines_RiderClientAssignmentId",
                schema: "ninja",
                table: "ResponsibilityLines",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityRevisions_PlatformRiderAccountId_Month_Revision",
                schema: "ninja",
                table: "ResponsibilityRevisions",
                columns: new[] { "PlatformRiderAccountId", "Month", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityRevisions_ReviewedSnapshotId",
                schema: "ninja",
                table: "ResponsibilityRevisions",
                column: "ReviewedSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityRevisions_SupersedesId",
                schema: "ninja",
                table: "ResponsibilityRevisions",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestionPolicies_Version",
                schema: "ninja",
                table: "SuggestionPolicies",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllocationProvenance",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "EvaluationContexts",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "PerformanceImportArtifacts",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "ResponsibilityLines",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "SuggestionPolicies",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "PerformanceArtifacts",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "ResponsibilityRevisions",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "PerformancePreviews",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "EvaluationSnapshots",
                schema: "ninja");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000317"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000318"));
        }
    }
}
