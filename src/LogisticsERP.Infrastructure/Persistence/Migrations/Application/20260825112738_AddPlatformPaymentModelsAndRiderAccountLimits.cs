using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPlatformPaymentModelsAndRiderAccountLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.AddColumn<int>(
                name: "PaymentModel",
                schema: "app",
                table: "RiderClientAssignments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PaymentModel",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SupportedPaymentModels",
                schema: "platform",
                table: "ClientPlatforms",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.Sql(
                "UPDATE [platform].[ClientPlatforms] SET [SupportedPaymentModels] = 1 WHERE UPPER([Code]) = 'JAHEZ'");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId",
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_PaymentModel",
                schema: "app",
                table: "RiderClientAssignments",
                columns: new[] { "RiderProfileId", "PaymentModel" },
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [PaymentModel] = 2 AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiderClientAssignments_PaymentModel",
                schema: "app",
                table: "RiderClientAssignments",
                sql: "[PaymentModel] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlatformRiderAccounts_PaymentModel",
                schema: "app",
                table: "PlatformRiderAccounts",
                sql: "[PaymentModel] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClientPlatforms_SupportedPaymentModels",
                schema: "platform",
                table: "ClientPlatforms",
                sql: "[SupportedPaymentModels] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderClientAssignments_RiderProfileId_PaymentModel",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiderClientAssignments_PaymentModel",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlatformRiderAccounts_PaymentModel",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClientPlatforms_SupportedPaymentModels",
                schema: "platform",
                table: "ClientPlatforms");

            migrationBuilder.DropColumn(
                name: "PaymentModel",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropColumn(
                name: "PaymentModel",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropColumn(
                name: "SupportedPaymentModels",
                schema: "platform",
                table: "ClientPlatforms");

            migrationBuilder.CreateIndex(
                name: "IX_RiderClientAssignments_RiderProfileId",
                schema: "app",
                table: "RiderClientAssignments",
                column: "RiderProfileId",
                unique: true,
                filter: "[EffectiveTo] IS NULL AND [IsDeleted] = 0");
        }
    }
}
