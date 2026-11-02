using System;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261102120000_AddChefzSettlementDeletePermission")]
public partial class AddChefzSettlementDeletionCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(schema: "platform", table: "PermissionDefinitions",
            columns: new[] { "Id", "Category", "CreatedAtUtc", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "RequiresClientScope", "RequiresHousingScope", "Version" },
            values: new object[] { Guid.Parse("019c18d5-62e1-7000-a000-000000000320"), "Chefz", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "حذف تصفيات شيفز لفترة لإعادة استيرادها.", "Delete Chefz settlements for a period for reimport.", 320, "HIGH_TRUST_ONLY", false, false, true, true, "chefz.settlements.delete", "حذف تصفيات شيفز", "Delete Chefz settlements", true, false, 1 });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DeleteData(schema: "platform", table: "PermissionDefinitions", keyColumn: "Id", keyValue: Guid.Parse("019c18d5-62e1-7000-a000-000000000320"));
}
