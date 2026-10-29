using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNinjaAssignmentDatePermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[] { new Guid("019c18d5-62e1-7000-a000-000000000319"), "Ninja", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصحيح تاريخ بداية التكليف الحالي وتاريخي بداية ونهاية التكليف المنتهي دون تغيير الأداء المسجل.", "Correct the current assignment start date or an ended assignment's start and end dates without changing recorded performance.", 319, "SENSITIVE_DATA", false, false, false, true, "ninja.assignments.dates.update", "تعديل تواريخ تكليفات نينجا", "Edit Ninja assignment dates", null, true, false, null, null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000319"));
        }
    }
}
