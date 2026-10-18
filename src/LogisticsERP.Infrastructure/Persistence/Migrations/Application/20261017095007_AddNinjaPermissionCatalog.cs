using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaPermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000310"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حسابات وتقارير وأداء وخصومات ومراجعات ملفات نينجا.", "Read Ninja accounts, reports, performance, deductions and sheet reviews.", 310, "SENSITIVE_DATA", false, false, false, true, "ninja.read", "عرض نينجا", "Read Ninja", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000311"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل حالة ورقم هاتف الحساب ورفع مراجعات الملفات وتعديل ملاحظاتها.", "Update Ninja account status and phone, upload sheet reviews and edit their notes.", 311, "SENSITIVE_DATA", false, false, false, true, "ninja.accounts.update", "تعديل حسابات نينجا", "Edit Ninja accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000312"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسليم حساب نينجا للمندوب الفعلي.", "Assign a Ninja account to its actual rider.", 312, "SENSITIVE_DATA", false, false, false, true, "ninja.assignments.create", "تسليم حسابات نينجا", "Assign Ninja accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000313"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استلام حساب نينجا وإنهاء تكليفه.", "Return a Ninja account and close its assignment.", 313, "SENSITIVE_DATA", false, false, false, true, "ninja.assignments.delete", "استلام حسابات نينجا", "Return Ninja accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000314"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تغيير كلمة مرور حساب نينجا ضمن سياسة مهلة التغيير.", "Change Ninja account passwords subject to the cooldown policy.", 314, "SENSITIVE_DATA", false, false, false, true, "ninja.credentials.rotate", "تغيير كلمات مرور نينجا", "Change Ninja passwords", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000315"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد الأداء اليومي لمندوبي نينجا.", "Import Ninja daily rider performance.", 315, "SENSITIVE_DATA", false, false, false, true, "ninja.performance.import", "استيراد أداء نينجا", "Import Ninja performance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000316"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد الخصومات اليومية لمندوبي نينجا.", "Import Ninja daily rider deductions.", 316, "SENSITIVE_DATA", false, false, false, true, "ninja.deductions.import", "استيراد خصومات نينجا", "Import Ninja deductions", null, true, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000310"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000311"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000312"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000313"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000314"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000315"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000316"));
        }
    }
}
