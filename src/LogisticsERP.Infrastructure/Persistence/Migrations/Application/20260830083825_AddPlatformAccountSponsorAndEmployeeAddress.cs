using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPlatformAccountSponsorAndEmployeeAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE account
                SET account.[SponsorId] = COALESCE(registration.[SponsorId], employee.[SponsorId], '019c18d5-62e1-7000-8000-000000000042')
                FROM [app].[PlatformRiderAccounts] AS account
                LEFT JOIN [app].[PlatformAccountRegistrations] AS registration
                    ON registration.[PlatformRiderAccountId] = account.[Id]
                    AND registration.[IsDeleted] = 0
                LEFT JOIN [app].[Employees] AS employee
                    ON employee.[Id] = account.[RegisteredEmployeeId]
                    AND employee.[IsDeleted] = 0;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressAdditionalNumber",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressBuildingNumber",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressCity",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressDistrict",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressPostalCode",
                schema: "app",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressStreet",
                schema: "app",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "OperatingCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_OperatingCityId_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "RegisteredEmployeeId", "ClientPlatformId", "OperatingCityId", "SponsorId" },
                unique: true,
                filter: "[RegisteredEmployeeId] IS NOT NULL AND [Status] IN (1, 2) AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_SponsorId_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "SponsorId", "OperatingCityId", "Status", "ClientPlatformId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "SponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId_OperatingCityId_SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_SponsorId_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "AddressAdditionalNumber",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressBuildingNumber",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressCity",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressDistrict",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressPostalCode",
                schema: "app",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AddressStreet",
                schema: "app",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "OperatingCityId", "Status", "ClientPlatformId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_RegisteredEmployeeId_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "RegisteredEmployeeId", "ClientPlatformId" },
                unique: true,
                filter: "[RegisteredEmployeeId] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
