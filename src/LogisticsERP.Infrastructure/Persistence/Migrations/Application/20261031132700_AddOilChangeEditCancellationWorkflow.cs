using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddOilChangeEditCancellationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("""
                IF EXISTS (SELECT ReversalOfUsageId FROM maintenance.MaterialUsages WHERE ReversalOfUsageId IS NOT NULL GROUP BY ReversalOfUsageId HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate material reversals require reconciliation before migration.', 1;
                IF EXISTS (SELECT ReversalOfAllocationId FROM maintenance.OilBarrelUsageAllocations WHERE ReversalOfAllocationId IS NOT NULL GROUP BY ReversalOfAllocationId HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate barrel reversals require reconciliation before migration.', 1;
                IF EXISTS (SELECT ReversalOfExpenseId FROM maintenance.VehicleExpenses WHERE ReversalOfExpenseId IS NOT NULL GROUP BY ReversalOfExpenseId HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate expense reversals require reconciliation before migration.', 1;
                IF EXISTS (SELECT ReversalOfEntryId FROM maintenance.ExternalFinancialEntries WHERE ReversalOfEntryId IS NOT NULL GROUP BY ReversalOfEntryId HAVING COUNT(*) > 1)
                    THROW 51000, 'Duplicate workshop reversals require reconciliation before migration.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_VehicleExpenses_ReversalOfExpenseId",
                schema: "maintenance",
                table: "VehicleExpenses");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrelUsageAllocations_ReversalOfAllocationId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUsages_ReversalOfUsageId",
                schema: "maintenance",
                table: "MaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_ExternalFinancialEntries_ReversalOfEntryId",
                schema: "maintenance",
                table: "ExternalFinancialEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                schema: "app",
                table: "VehicleOdometerReadings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                schema: "maintenance",
                table: "OilChangeOperations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveServiceAtUtc",
                schema: "maintenance",
                table: "MaterialUsages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OilChangeRevisions",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OilChangeOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OldOilUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldFilterUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilChangeRevisions", x => x.Id);
                    table.CheckConstraint("CK_OilChangeRevisions_Number", "[RevisionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_OilChangeRevisions_MaterialUsages_OldFilterUsageId",
                        column: x => x.OldFilterUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeRevisions_MaterialUsages_OldOilUsageId",
                        column: x => x.OldOilUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeRevisions_OilChangeOperations_OilChangeOperationId",
                        column: x => x.OilChangeOperationId,
                        principalSchema: "maintenance",
                        principalTable: "OilChangeOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000900"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل عملية زيت مكتملة مع تدقيق.", "Correct completed oil changes with audit.", 900, "SENSITIVE_DATA", false, false, false, true, "maintenance.oil.edit", "تعديل تغيير الزيت", "Edit oil changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000901"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصحيح تاريخ خدمة الزيت.", "Correct oil service dates.", 901, "SENSITIVE_DATA", false, false, false, true, "maintenance.oil.date_edit", "تعديل تاريخ تغيير الزيت", "Edit oil-change dates", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000902"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إلغاء الزيت بعكس المخزون والتكاليف.", "Cancel oil changes with linked reversals.", 902, "SENSITIVE_DATA", false, false, false, true, "maintenance.oil.cancel", "إلغاء تغيير الزيت", "Cancel oil changes", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOdometerReadings_VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings",
                column: "VoidedByRevisionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleOdometerReadings_Void",
                schema: "app",
                table: "VehicleOdometerReadings",
                sql: "([IsVoided] = 0 AND [VoidedByRevisionId] IS NULL) OR ([IsVoided] = 1 AND [VoidedByRevisionId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_ReversalOfExpenseId",
                schema: "maintenance",
                table: "VehicleExpenses",
                column: "ReversalOfExpenseId",
                unique: true,
                filter: "[ReversalOfExpenseId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OilChangeOperations_Lifecycle",
                schema: "maintenance",
                table: "OilChangeOperations",
                sql: "[RevisionNumber] >= 0 AND (([IsCancelled] = 0 AND [CancelledAtUtc] IS NULL AND [CancelledByUserId] IS NULL AND [CancellationReason] IS NULL) OR ([IsCancelled] = 1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByUserId] IS NOT NULL AND [CancellationReason] IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelUsageAllocations_ReversalOfAllocationId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations",
                column: "ReversalOfAllocationId",
                unique: true,
                filter: "[ReversalOfAllocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelLosses_CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "CorrectionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_ReversalOfUsageId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "ReversalOfUsageId",
                unique: true,
                filter: "[ReversalOfUsageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_ReversalOfEntryId",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                column: "ReversalOfEntryId",
                unique: true,
                filter: "[ReversalOfEntryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeRevisions_OilChangeOperationId_IdempotencyKey",
                schema: "maintenance",
                table: "OilChangeRevisions",
                columns: new[] { "OilChangeOperationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeRevisions_OilChangeOperationId_RevisionNumber",
                schema: "maintenance",
                table: "OilChangeRevisions",
                columns: new[] { "OilChangeOperationId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeRevisions_OldFilterUsageId",
                schema: "maintenance",
                table: "OilChangeRevisions",
                column: "OldFilterUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeRevisions_OldOilUsageId",
                schema: "maintenance",
                table: "OilChangeRevisions",
                column: "OldOilUsageId");

            migrationBuilder.AddForeignKey(
                name: "FK_OilBarrelLosses_OilChangeRevisions_CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses",
                column: "CorrectionRevisionId",
                principalSchema: "maintenance",
                principalTable: "OilChangeRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleOdometerReadings_OilChangeRevisions_VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings",
                column: "VoidedByRevisionId",
                principalSchema: "maintenance",
                principalTable: "OilChangeRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM maintenance.OilChangeRevisions) THROW 51000, 'Oil correction history exists; use a forward migration.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_OilBarrelLosses_OilChangeRevisions_CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleOdometerReadings_OilChangeRevisions_VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings");

            migrationBuilder.DropTable(
                name: "OilChangeRevisions",
                schema: "maintenance");

            migrationBuilder.DropIndex(
                name: "IX_VehicleOdometerReadings_VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleOdometerReadings_Void",
                schema: "app",
                table: "VehicleOdometerReadings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleExpenses_ReversalOfExpenseId",
                schema: "maintenance",
                table: "VehicleExpenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OilChangeOperations_Lifecycle",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrelUsageAllocations_ReversalOfAllocationId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations");

            migrationBuilder.DropIndex(
                name: "IX_OilBarrelLosses_CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUsages_ReversalOfUsageId",
                schema: "maintenance",
                table: "MaterialUsages");

            migrationBuilder.DropIndex(
                name: "IX_ExternalFinancialEntries_ReversalOfEntryId",
                schema: "maintenance",
                table: "ExternalFinancialEntries");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000900"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000901"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000902"));

            migrationBuilder.DropColumn(
                name: "IsVoided",
                schema: "app",
                table: "VehicleOdometerReadings");

            migrationBuilder.DropColumn(
                name: "VoidedByRevisionId",
                schema: "app",
                table: "VehicleOdometerReadings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "RevisionNumber",
                schema: "maintenance",
                table: "OilChangeOperations");

            migrationBuilder.DropColumn(
                name: "CorrectionRevisionId",
                schema: "maintenance",
                table: "OilBarrelLosses");

            migrationBuilder.DropColumn(
                name: "EffectiveServiceAtUtc",
                schema: "maintenance",
                table: "MaterialUsages");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_ReversalOfExpenseId",
                schema: "maintenance",
                table: "VehicleExpenses",
                column: "ReversalOfExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_OilBarrelUsageAllocations_ReversalOfAllocationId",
                schema: "maintenance",
                table: "OilBarrelUsageAllocations",
                column: "ReversalOfAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_ReversalOfUsageId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "ReversalOfUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_ReversalOfEntryId",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                column: "ReversalOfEntryId");
        }
    }
}
