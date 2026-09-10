using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class MakeHousingPermissionsModuleScoped : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000042"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "عرض السكن وفترات الإقامة.", "View housing and residence periods.", false });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000043"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "إدارة السكن والمشرفين وفترات الإقامة.", "Manage housing, supervisors, and residence periods.", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000042"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "عرض السكن وفترات الإقامة ضمن النطاق المسموح.", "View housing and residence periods within the allowed scope.", true });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000043"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "إدارة السكن والمشرفين وفترات الإقامة ضمن النطاق.", "Manage housing, supervisors, and residence periods within scope.", true });
        }
    }
}
