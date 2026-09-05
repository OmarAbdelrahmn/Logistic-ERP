using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class UpdateOilBarrelLossAllowanceToTwoPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.Sql("UPDATE [maintenance].[OilBarrels] SET [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3);");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.02, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels");

            migrationBuilder.Sql("UPDATE [maintenance].[OilBarrels] SET [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.025, 3);");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilBarrels_Quantities",
                schema: "maintenance",
                table: "OilBarrels",
                sql: "[NominalCapacityLiters] > 0 AND [RemainingLiters] >= 0 AND [RemainingLiters] <= [NominalCapacityLiters] AND [UnitCostPerLiter] >= 0 AND [MaximumAllowedLossLiters] = ROUND([NominalCapacityLiters] * 0.025, 3) AND [RecordedLossLiters] >= 0 AND [RecordedLossLiters] <= [MaximumAllowedLossLiters]");
        }
    }
}
