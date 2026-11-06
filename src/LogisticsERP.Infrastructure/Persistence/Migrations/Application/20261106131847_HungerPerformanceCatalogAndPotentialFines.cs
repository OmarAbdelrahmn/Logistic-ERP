using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerPerformanceCatalogAndPotentialFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ActualWorkingHours",
                schema: "hunger",
                table: "DailyOperations",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AddColumn<int>(
                name: "AcceptedOrders",
                schema: "hunger",
                table: "DailyOperations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoursUnit",
                schema: "hunger",
                table: "DailyOperations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "DecimalHours");

            migrationBuilder.AddColumn<int>(
                name: "StackedOrders",
                schema: "hunger",
                table: "DailyOperations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveThrough",
                schema: "hunger",
                table: "AlertRules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                schema: "hunger",
                table: "AlertRules",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Alert");

            migrationBuilder.AddColumn<string>(
                name: "Metric",
                schema: "hunger",
                table: "AlertRules",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "WorkingHours");

            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "hunger",
                table: "AlertRules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                schema: "hunger",
                table: "AlertRules",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "LessThan");

            migrationBuilder.AddColumn<string>(
                name: "Period",
                schema: "hunger",
                table: "AlertRules",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Day");

            migrationBuilder.CreateTable(
                name: "PotentialFines",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoShowCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvoiceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceSourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResolutionEvidence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PotentialFines", x => x.Id);
                    table.CheckConstraint("CK_HungerPotentialFine_Status", "[NoShowCount]>0 AND [Status] IN ('Potential','Invoiced','Waived')");
                    table.ForeignKey(
                        name: "FK_PotentialFines_DailyOperations_DailyOperationId",
                        column: x => x.DailyOperationId,
                        principalSchema: "hunger",
                        principalTable: "DailyOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PotentialFines_SettlementDocuments_InvoiceDocumentId",
                        column: x => x.InvoiceDocumentId,
                        principalSchema: "hunger",
                        principalTable: "SettlementDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_OperatingCityId",
                schema: "hunger",
                table: "AlertRules",
                column: "OperatingCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PotentialFines_DailyOperationId",
                schema: "hunger",
                table: "PotentialFines",
                column: "DailyOperationId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PotentialFines_InvoiceDocumentId",
                schema: "hunger",
                table: "PotentialFines",
                column: "InvoiceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PotentialFines_IsDeleted",
                schema: "hunger",
                table: "PotentialFines",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRules_OperatingCities_OperatingCityId",
                schema: "hunger",
                table: "AlertRules",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Preserve existing rule meaning and make historical missed shifts reviewable without a financial posting.
            migrationBuilder.Sql("""
                UPDATE [hunger].[AlertRules]
                SET [Metric] = 'NoShowCount', [Operator] = 'GreaterThan'
                WHERE [Code] = 'NoShowPositive';
                INSERT INTO [hunger].[PotentialFines]
                    ([Id], [DailyOperationId], [NoShowCount], [Status], [CreatedAtUtc], [CreatedByUserId], [IsDeleted])
                SELECT NEWID(), [Id], [NoShowCount], 'Potential', [CreatedAtUtc], [CreatedByUserId], 0
                FROM [hunger].[DailyOperations]
                WHERE [IsDeleted] = 0 AND [NoShowCount] > 0;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [hunger].[DailyOperations] WHERE [ActualWorkingHours] IS NULL)
                    THROW 51000, 'Cannot downgrade unknown hours into zero; preserve the performance evidence and use a forward migration.', 1;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_AlertRules_OperatingCities_OperatingCityId",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropTable(
                name: "PotentialFines",
                schema: "hunger");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_OperatingCityId",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "AcceptedOrders",
                schema: "hunger",
                table: "DailyOperations");

            migrationBuilder.DropColumn(
                name: "HoursUnit",
                schema: "hunger",
                table: "DailyOperations");

            migrationBuilder.DropColumn(
                name: "StackedOrders",
                schema: "hunger",
                table: "DailyOperations");

            migrationBuilder.DropColumn(
                name: "EffectiveThrough",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Metric",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Operator",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "Period",
                schema: "hunger",
                table: "AlertRules");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualWorkingHours",
                schema: "hunger",
                table: "DailyOperations",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);
        }
    }
}
