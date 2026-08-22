using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class ImplementMissingModelApis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RotationReason",
                schema: "app",
                table: "PlatformAccountCredentialVersions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreviousLeaveStatus",
                schema: "app",
                table: "LeaveCancellationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000075"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات الشركة المالكة وإعداداتها العامة.", "View the owning company profile and general settings.", 75, null, false, false, false, false, "company_profile.read", "عرض ملف الشركة", "Read company profile", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000076"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بيانات الشركة وإعداداتها دون تغيير التسلسل الداخلي.", "Update company settings without changing protected internal sequences.", 76, "HIGH_TRUST_ONLY", false, false, true, true, "company_profile.manage", "إدارة ملف الشركة", "Manage company profile", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000077"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض كتالوج الوسوم وروابطه التشغيلية.", "View the tag catalog and operational assignments.", 77, null, false, false, false, false, "tags.read", "عرض الوسوم", "Read tags", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000078"), "Catalog", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة كتالوج الوسوم وتعيينها للكيانات المسموحة.", "Manage tags and assign them to supported entities.", 78, null, false, false, false, false, "tags.manage", "إدارة الوسوم", "Manage tags", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000079"), "Documents", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة أنواع الوثائق ومتطلبات اكتمالها.", "Manage document types and completeness requirements.", 79, "SENSITIVE_DATA", false, false, false, true, "documents.catalog.manage", "إدارة كتالوج الوثائق", "Manage document catalog", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000080"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات وصفية فقط عن تدوير بيانات اعتماد حسابات المنصات.", "View metadata only for platform-account credential rotations.", 80, "HIGH_TRUST_ONLY", false, false, true, true, "platform_credentials.read", "عرض سجل بيانات اعتماد المنصات", "Read platform credential history", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000081"), "Operations", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استبدال بيانات اعتماد حساب منصة مع حفظ سجل مشفر غير قابل للتعديل.", "Replace a platform account credential while preserving encrypted immutable history.", 81, "HIGH_TRUST_ONLY", false, false, true, true, "platform_credentials.rotate", "تدوير بيانات اعتماد المنصات", "Rotate platform credentials", null, true, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000075"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000076"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000077"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000078"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000079"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000080"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000081"));

            migrationBuilder.DropColumn(
                name: "RotationReason",
                schema: "app",
                table: "PlatformAccountCredentialVersions");

            migrationBuilder.DropColumn(
                name: "PreviousLeaveStatus",
                schema: "app",
                table: "LeaveCancellationRequests");
        }
    }
}
