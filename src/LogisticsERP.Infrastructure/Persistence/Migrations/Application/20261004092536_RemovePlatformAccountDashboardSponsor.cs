using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class RemovePlatformAccountDashboardSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "DashboardSponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "DashboardSponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
