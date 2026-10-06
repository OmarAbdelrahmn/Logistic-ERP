using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AllowUnassignedFuelCardUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RiderProfileId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FuelCardMonthlyUsages_RiderPair",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                sql: "([RiderProfileId] IS NULL AND [EmployeeId] IS NULL) OR ([RiderProfileId] IS NOT NULL AND [EmployeeId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [app].[FuelCardMonthlyUsages] WHERE [RiderProfileId] IS NULL OR [EmployeeId] IS NULL)
                    THROW 51000, 'Cannot roll back while unassigned fuel usage exists; attribute or remove it first.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_FuelCardMonthlyUsages_RiderPair",
                schema: "app",
                table: "FuelCardMonthlyUsages");

            migrationBuilder.AlterColumn<Guid>(
                name: "RiderProfileId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
