using System;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261103120000_AddChefzPerformanceDeletePermission")]
public partial class AddChefzPerformanceDeletionCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(schema: "platform", table: "PermissionDefinitions",
            columns: new[] { "Id", "Category", "CreatedAtUtc", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "RequiresClientScope", "RequiresHousingScope", "Version" },
            values: new object[] { Guid.Parse("019c18d5-62e1-7000-a000-000000000321"), "Chefz", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "حذف أداء شيفز اليومي ليوم محدد لإعادة استيرادها.", "Delete Chefz daily performance for a selected day for reimport.", 321, "HIGH_TRUST_ONLY", false, false, true, true, "chefz.performance.delete", "حذف أداء شيفز اليومي", "Delete Chefz daily performance", true, false, 1 });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DeleteData(schema: "platform", table: "PermissionDefinitions", keyColumn: "Id", keyValue: Guid.Parse("019c18d5-62e1-7000-a000-000000000321"));
}
