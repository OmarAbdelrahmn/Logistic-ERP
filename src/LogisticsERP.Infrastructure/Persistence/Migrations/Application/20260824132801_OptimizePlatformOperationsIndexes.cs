using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class OptimizePlatformOperationsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_PlatformRiderAccountId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "PlatformRiderAccountId", "EffectiveFrom" })
                .Annotation("SqlServer:Include", new[] { "RiderProfileId", "EffectiveTo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "EffectiveFrom" })
                .Annotation("SqlServer:Include", new[] { "PlatformRiderAccountId", "EffectiveTo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts",
                columns: new[] { "OperatingCityId", "Status", "ClientPlatformId" },
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_PlatformRiderAccountId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId_Status_ClientPlatformId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_OperatingCityId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "OperatingCityId");
        }
    }
}
