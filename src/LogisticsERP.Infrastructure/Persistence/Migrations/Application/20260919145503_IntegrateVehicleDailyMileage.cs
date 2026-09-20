using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class IntegrateVehicleDailyMileage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.AlterColumn<decimal>(
                name: "ManualBaselineOdometerReading",
                schema: "app",
                table: "VehicleDailyDistances",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EffectiveOdometerAfterKm",
                schema: "app",
                table: "VehicleDailyDistances",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE [app].[Vehicles]
                SET [TrackedDistanceKm] = CONVERT(decimal(18,2), [CurrentOdometer])
                WHERE [TrackedDistanceKm] < CONVERT(decimal(18,2), [CurrentOdometer]);

                WITH [Mileage] AS
                (
                    SELECT
                        [d].[Id],
                        [v].[TrackedDistanceKm]
                            - SUM([d].[AppliedDistanceKm]) OVER (PARTITION BY [d].[VehicleId])
                            + SUM([d].[AppliedDistanceKm]) OVER (
                                PARTITION BY [d].[VehicleId]
                                ORDER BY [d].[WorkDate], [d].[Id]
                                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS [EffectiveOdometerAfterKm]
                    FROM [app].[VehicleDailyDistances] AS [d]
                    INNER JOIN [app].[Vehicles] AS [v] ON [v].[Id] = [d].[VehicleId]
                    WHERE [d].[IsDeleted] = 0
                )
                UPDATE [d]
                SET [EffectiveOdometerAfterKm] =
                    CASE WHEN [m].[EffectiveOdometerAfterKm] < 0 THEN 0 ELSE [m].[EffectiveOdometerAfterKm] END
                FROM [app].[VehicleDailyDistances] AS [d]
                INNER JOIN [Mileage] AS [m] ON [m].[Id] = [d].[Id];

                UPDATE [app].[Vehicles]
                SET [CurrentOdometer] = CONVERT(bigint, FLOOR([TrackedDistanceKm]));
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleDailyDistances_EffectiveOdometer",
                schema: "app",
                table: "VehicleDailyDistances",
                sql: "[EffectiveOdometerAfterKm] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances",
                sql: "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= [ManualBaselineOdometerReading])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleDailyDistances_EffectiveOdometer",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.DropColumn(
                name: "EffectiveOdometerAfterKm",
                schema: "app",
                table: "VehicleDailyDistances");

            migrationBuilder.Sql(
                "UPDATE [app].[VehicleDailyDistances] SET [ManualBaselineOdometerReading] = FLOOR([ManualBaselineOdometerReading]) WHERE [ManualBaselineOdometerReading] IS NOT NULL;");

            migrationBuilder.AlterColumn<long>(
                name: "ManualBaselineOdometerReading",
                schema: "app",
                table: "VehicleDailyDistances",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleDailyDistances_ManualOdometer",
                schema: "app",
                table: "VehicleDailyDistances",
                sql: "[ManualOdometerReading] IS NULL OR ([ManualBaselineOdometerReading] IS NOT NULL AND [ManualOdometerReading] >= [ManualBaselineOdometerReading])");
        }
    }
}
