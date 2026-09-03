using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SeedVehicleDailyDistancePermissions : Migration
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
                    { new Guid("019c18d5-62e1-7000-a000-000000000087"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض المسافة اليومية من نظام GPS أو الإدخال اليدوي للمركبات.", "View each vehicle's daily GPS or manually entered distance.", 87, null, false, false, false, false, "fleet.daily_distances.read", "عرض المسافات اليومية للمركبات", "Read vehicle daily distances", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000088"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدخال وتعديل قراءة العداد اليدوية اليومية للمركبات.", "Enter and update a vehicle's daily manual odometer reading.", 88, "SENSITIVE_DATA", false, false, false, true, "fleet.daily_distances.manage", "إدارة المسافات اليومية للمركبات", "Manage vehicle daily distances", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000089"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد ملف GPS اليومي وتطبيق المسافات على المركبات المطابقة.", "Import a daily GPS report and apply distances to matching vehicles.", 89, "SENSITIVE_DATA", false, false, false, true, "fleet.daily_distances.import", "استيراد مسافات GPS اليومية", "Import daily GPS distances", null, false, false, null, null, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000087"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000088"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000089"));
        }
    }
}
