using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezSettlementCommissionStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Amount",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CommissionStartDate",
                schema: "jahez_v2",
                table: "Operations",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezOperation_CommissionStart",
                schema: "jahez_v2",
                table: "Operations",
                sql: "[CommissionStartDate] IS NULL OR [CommissionStartDate] <= [ThroughDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Amount",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "[Amount] > 0 OR ([Amount] = 0 AND [Direction] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2) OR ([Direction] = 0 AND [Amount] = 0 AND [Purpose] = 1 AND [OperationId] IS NOT NULL AND [Fund] IN (1, 2, 3))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezOperation_CommissionStart",
                schema: "jahez_v2",
                table: "Operations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Amount",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements");

            migrationBuilder.DropColumn(
                name: "CommissionStartDate",
                schema: "jahez_v2",
                table: "Operations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Amount",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "[Amount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JahezMoney_Routing",
                schema: "jahez_v2",
                table: "MoneyMovements",
                sql: "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] IN (1, 2)))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2)");
        }
    }
}
