using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowJahezPetroAppSettlementReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] = 2))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2)");
        }
    }
}
