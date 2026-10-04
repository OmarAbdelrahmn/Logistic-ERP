using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SplitExternalRiderAndWorkOrderPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000096"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "فتح أوامر الصيانة.", "Create maintenance work orders.", "maintenance.work_orders.create", "إنشاء أوامر الصيانة", "Create maintenance work orders" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000121"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء ملفات المناديب الخارجيين.", "Create external rider profiles.", "external_riders.create", "إنشاء المناديب الخارجيين", "Create external riders" });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000122"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل ملفات المناديب الخارجيين.", "Update external rider profiles.", 122, null, false, false, false, false, "external_riders.update", "تعديل المناديب الخارجيين", "Edit external riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000123"), "Workforce", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "أرشفة ملفات المناديب الخارجيين مع حفظ التاريخ.", "Archive external rider profiles while retaining history.", 123, null, false, false, false, false, "external_riders.delete", "حذف المناديب الخارجيين", "Delete external riders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000124"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "بدء وإكمال وإغلاق أوامر الصيانة وتحديث موادها وتكاليفها.", "Start, complete, close, and update materials and costs of maintenance work orders.", 124, "SENSITIVE_DATA", false, false, false, true, "maintenance.work_orders.update", "تعديل أوامر الصيانة", "Edit maintenance work orders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000125"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إلغاء أوامر الصيانة مع حفظ التاريخ.", "Cancel maintenance work orders while retaining history.", 125, "SENSITIVE_DATA", false, false, false, true, "maintenance.work_orders.delete", "إلغاء أوامر الصيانة", "Cancel maintenance work orders", null, false, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000122"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000123"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000124"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000125"));

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000096"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "فتح وبدء وإكمال وإغلاق أوامر الصيانة وصرف موادها.", "Open, start, complete, close, and post materials to maintenance work orders.", "maintenance.work_orders.manage", "إدارة أوامر الصيانة", "Manage maintenance work orders" });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000121"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "Key", "NameAr", "NameEn" },
                values: new object[] { "إنشاء وتعديل ملفات المناديب الخارجيين.", "Create and update external rider profiles.", "external_riders.manage", "إدارة المناديب الخارجيين", "Manage external riders" });
        }
    }
}
