using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class SeedProtectedAuthorizationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "Roles",
                columns: new[] { "Id", "Code", "ConcurrencyStamp", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "IsDeleted", "IsProtected", "IsTemplate", "Name", "NameAr", "NameEn", "NormalizedName", "SourceTemplateId", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-9000-000000000001"), "SYSTEM_ADMIN", "protected-system_admin-v1", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة المستخدمين والأدوار والصلاحيات والأمن دون منح تلقائي لكل البيانات التشغيلية الحساسة.", "Manages users, roles, permissions, and security without automatic access to all sensitive operational data.", false, true, true, "SYSTEM_ADMIN", "مسؤول النظام", "System Administrator", "SYSTEM_ADMIN", null, 2, null, null },
                    { new Guid("019c18d5-62e1-7000-9000-000000000002"), "MANAGER", "protected-manager-v1", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "قراءة تشغيلية أساسية، وتضاف صلاحيات الإدارة والنطاقات حسب مسؤوليات الشخص.", "Minimal operational read access; management permissions and scopes are assigned per responsibility.", false, true, true, "MANAGER", "مدير", "Manager", "MANAGER", null, 2, null, null },
                    { new Guid("019c18d5-62e1-7000-9000-000000000003"), "USER", "protected-user-v1", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "الوصول إلى الملف الشخصي والجلسات فقط حتى تمنح صلاحيات إضافية.", "Access to the user's own profile and sessions until additional permissions are granted.", false, true, true, "USER", "مستخدم", "User", "USER", null, 2, null, null }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "PermissionKey", "RoleId", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-b000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "users.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "users.create", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "users.update", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "users.archive", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000005"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "roles.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000006"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "roles.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000007"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "permissions.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000008"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "permissions.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000009"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "audit.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000010"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "support_access.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000011"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "operating_cities.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000012"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "operating_cities.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000013"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "reports.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000014"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "operating_cities.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000015"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "employees.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000016"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "riders.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000017"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000018"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_assignments.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000019"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "housing.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000020"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "reports.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000021"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "notifications.read", new Guid("019c18d5-62e1-7000-9000-000000000002"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000010"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000012"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000018"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-9000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-9000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-9000-000000000002"));
        }
    }
}
