using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class GrantNinjaPermissions : Migration
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
                    { new Guid("019c18d5-62e1-7000-b000-000000000265"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.accounts.update", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000266"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.assignments.create", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000267"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.assignments.delete", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000268"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.credentials.rotate", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000269"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.deductions.import", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000270"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.performance.import", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null },
                    { new Guid("019c18d5-62e1-7000-b000-000000000271"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, "ninja.read", new Guid("019c18d5-62e1-7000-9000-000000000001"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000265"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000266"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000267"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000268"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000269"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000270"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-b000-000000000271"));
        }
    }
}
