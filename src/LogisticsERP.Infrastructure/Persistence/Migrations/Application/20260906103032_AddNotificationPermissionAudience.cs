using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddNotificationPermissionAudience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudiencePermissionKeysJson",
                schema: "app",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [app].[Notifications]
                SET [AudiencePermissionKeysJson] = N'["fleet.accidents.read","fleet.vehicles.read","platform_assignments.read","maintenance.work_orders.read"]'
                WHERE [EventType] LIKE N'fleet.accident.%';

                UPDATE [app].[Notifications]
                SET [AudiencePermissionKeysJson] = N'["fleet.compliance.read"]'
                WHERE [EventType] LIKE N'fleet.registration.%' OR [EventType] LIKE N'fleet.insurance.%'
                   OR [EventType] LIKE N'fleet.inspection.%' OR [EventType] LIKE N'fleet.permit.%'
                   OR [EventType] LIKE N'fleet.operation-card.%';

                UPDATE [app].[Notifications]
                SET [AudiencePermissionKeysJson] = N'["employees.read"]'
                WHERE [EventType] LIKE N'employee.compliance.%';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_AudiencePermissions",
                schema: "app",
                table: "Notifications",
                sql: "[AudiencePermissionKeysJson] IS NULL OR ISJSON([AudiencePermissionKeysJson]) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_AudiencePermissions",
                schema: "app",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AudiencePermissionKeysJson",
                schema: "app",
                table: "Notifications");
        }
    }
}
