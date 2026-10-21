using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class UseGranularJahezTreasuryPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000751"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.accounts.create", "jahez.treasury.accounts.create", "jahez.treasury.accounts.create", "إنشاء حساب خزينة", "jahez.treasury.accounts.create" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000752"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.accounts.update", "jahez.treasury.accounts.update", "jahez.treasury.accounts.update", "تحديث حساب خزينة", "jahez.treasury.accounts.update" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000753"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.transfers.create", "jahez.treasury.transfers.create", "jahez.treasury.transfers.create", "إنشاء تحويل خزينة", "jahez.treasury.transfers.create" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000754"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.entries.create", "jahez.treasury.entries.create", "jahez.treasury.entries.create", "إنشاء قبض وصرف مركزي", "jahez.treasury.entries.create" });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000755"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.treasury.petroapp-evidence.create", "jahez.treasury.petroapp-evidence.create", 755, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.treasury.petroapp-evidence.create", "إنشاء دليل بترو آب", "jahez.treasury.petroapp-evidence.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000756"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.treasury.adjust", "jahez.treasury.adjust", 756, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.treasury.adjust", "تسوية وترحيل الخزينة", "jahez.treasury.adjust", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000757"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.financial-reports.read", "jahez.financial-reports.read", 757, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.financial-reports.read", "عرض التقارير المالية", "jahez.financial-reports.read", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000758"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.financial-reports.export", "jahez.financial-reports.export", 758, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.financial-reports.export", "تصدير التقارير المالية", "jahez.financial-reports.export", null, true, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000755"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000756"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000757"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000758"));

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000751"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.manage", "jahez.treasury.manage", "jahez.treasury.manage", "إدارة الخزينة", "jahez.treasury.manage" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000752"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.treasury.adjust", "jahez.treasury.adjust", "jahez.treasury.adjust", "تسوية وترحيل الخزينة", "jahez.treasury.adjust" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000753"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.financial-reports.read", "jahez.financial-reports.read", "jahez.financial-reports.read", "عرض التقارير المالية", "jahez.financial-reports.read" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000754"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "jahez.financial-reports.export", "jahez.financial-reports.export", "jahez.financial-reports.export", "تصدير التقارير المالية", "jahez.financial-reports.export" });
        }
    }
}
