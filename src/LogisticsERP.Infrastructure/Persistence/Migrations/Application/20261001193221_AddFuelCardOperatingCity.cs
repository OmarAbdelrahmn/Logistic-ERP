using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddFuelCardOperatingCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "FuelCards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [app].[OperatingCities] WHERE [Id] = '019c18d5-62e1-7000-8000-000000000003')
                    THROW 51000, 'The Jeddah operating city must exist before assigning fuel cards.', 1;

                EXEC(N'UPDATE [app].[FuelCards]
                    SET [OperatingCityId] = ''019c18d5-62e1-7000-8000-000000000003''
                    WHERE [OperatingCityId] IS NULL;');
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "FuelCards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("019c18d5-62e1-7000-8000-000000000003"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_OperatingCityId",
                schema: "app",
                table: "FuelCards",
                column: "OperatingCityId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_OperatingCities_OperatingCityId",
                schema: "app",
                table: "FuelCards",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_OperatingCities_OperatingCityId",
                schema: "app",
                table: "FuelCards");

            migrationBuilder.DropIndex(
                name: "IX_FuelCards_OperatingCityId",
                schema: "app",
                table: "FuelCards");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "app",
                table: "FuelCards");
        }
    }
}
