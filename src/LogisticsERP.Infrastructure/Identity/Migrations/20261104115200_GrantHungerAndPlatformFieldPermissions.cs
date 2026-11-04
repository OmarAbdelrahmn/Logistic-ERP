using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class GrantHungerAndPlatformFieldPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "PermissionKey", "RoleId", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-b000-000000000284"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.accounts.import", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000285"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.accounts.update", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000286"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.alerts.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000287"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.finance.configure", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000288"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.finance.manage", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000289"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.performance.export", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000290"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.performance.import", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000291"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.quality.export", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000292"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.quality.import", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000293"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000294"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "hunger.remittances.confirm", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000295"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.credentials.reveal", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000296"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.city", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000297"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.contact", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000298"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.identity", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000299"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.owner", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000300"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.registered_vehicle", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000301"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.registry", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000302"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_accounts.fields.settlement", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000303"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "platform_assignments.fields.compensation", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000284"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000285"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000286"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000287"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000288"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000289"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000290"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000291"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000292"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000293"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000294"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000295"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000296"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000297"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000298"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000299"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000300"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000301"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000302"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000303"));
        }
    }
}
