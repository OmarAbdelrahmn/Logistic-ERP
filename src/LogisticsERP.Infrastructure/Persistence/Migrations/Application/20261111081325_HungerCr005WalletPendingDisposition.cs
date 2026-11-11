using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerCr005WalletPendingDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlatformBusiness_Disposition",
                schema: "app",
                table: "PlatformAccountBusinessDetails");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlatformBusiness_Disposition",
                schema: "app",
                table: "PlatformAccountBusinessDetails",
                sql: "[OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth','WalletPending')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlatformBusiness_Disposition",
                schema: "app",
                table: "PlatformAccountBusinessDetails");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlatformBusiness_Disposition",
                schema: "app",
                table: "PlatformAccountBusinessDetails",
                sql: "[OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth')");
        }
    }
}
