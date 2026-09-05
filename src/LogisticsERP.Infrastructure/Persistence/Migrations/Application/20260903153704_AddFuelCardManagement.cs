using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddFuelCardManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelCardImports",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ReportMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    ReportThroughAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Sha256Checksum = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SourceRows = table.Column<int>(type: "int", nullable: false),
                    CardRows = table.Column<int>(type: "int", nullable: false),
                    CreatedCards = table.Column<int>(type: "int", nullable: false),
                    CreatedMonthlyRecords = table.Column<int>(type: "int", nullable: false),
                    UpdatedMonthlyRecords = table.Column<int>(type: "int", nullable: false),
                    UnassignedCards = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    RowErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCardImports", x => x.Id);
                    table.CheckConstraint("CK_FuelCardImports_Counts", "[SourceRows] >= 0 AND [CardRows] >= 0 AND [CreatedCards] >= 0 AND [CreatedMonthlyRecords] >= 0 AND [UpdatedMonthlyRecords] >= 0 AND [UnassignedCards] >= 0 AND [InvalidRows] >= 0");
                    table.CheckConstraint("CK_FuelCardImports_MonthStart", "DAY([ReportMonth]) = 1");
                    table.CheckConstraint("CK_FuelCardImports_Provider", "[Provider] BETWEEN 1 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "FuelCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    IdentifierType = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedCardNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlateNumberText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedPlateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_FuelCards", x => x.Id);
                    table.CheckConstraint("CK_FuelCards_IdentifierType", "[IdentifierType] BETWEEN 1 AND 2");
                    table.CheckConstraint("CK_FuelCards_Provider", "[Provider] BETWEEN 1 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "FuelCardMonthlyUsages",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalLiters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountBeforeTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TransactionCount = table.Column<int>(type: "int", nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourcePlateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedSourcePlateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstTransactionAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastTransactionAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReportThroughAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_FuelCardMonthlyUsages", x => x.Id);
                    table.CheckConstraint("CK_FuelCardMonthlyUsages_Amounts", "[TotalLiters] >= 0 AND [TotalAmount] >= 0 AND ([AmountBeforeTax] IS NULL OR [AmountBeforeTax] >= 0) AND ([VatAmount] IS NULL OR [VatAmount] >= 0)");
                    table.CheckConstraint("CK_FuelCardMonthlyUsages_MonthStart", "DAY([ReportMonth]) = 1");
                    table.CheckConstraint("CK_FuelCardMonthlyUsages_TransactionCount", "[TransactionCount] IS NULL OR [TransactionCount] >= 0");
                    table.ForeignKey(
                        name: "FK_FuelCardMonthlyUsages_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCardMonthlyUsages_FuelCardImports_LastImportId",
                        column: x => x.LastImportId,
                        principalSchema: "app",
                        principalTable: "FuelCardImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCardMonthlyUsages_FuelCards_FuelCardId",
                        column: x => x.FuelCardId,
                        principalSchema: "app",
                        principalTable: "FuelCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCardMonthlyUsages_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuelCardRiderAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EndReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCardRiderAssignments", x => x.Id);
                    table.CheckConstraint("CK_FuelCardRiderAssignments_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_FuelCardRiderAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCardRiderAssignments_FuelCards_FuelCardId",
                        column: x => x.FuelCardId,
                        principalSchema: "app",
                        principalTable: "FuelCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelCardRiderAssignments_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000090"), "Fuel", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بطاقات الوقود وإسناداتها واستهلاكها الشهري.", "View fuel cards, rider assignments, and monthly usage.", 90, "SENSITIVE_DATA", false, false, false, true, "fuel.read", "عرض بطاقات واستهلاك الوقود", "Read fuel cards and usage", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000091"), "Fuel", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء بطاقات الوقود وإسنادها إلى رايدر واحد في الشهر وإيقاف الإسناد.", "Create fuel cards, enforce one rider per month, and stop assignments.", 91, "SENSITIVE_DATA", false, false, false, true, "fuel.manage", "إدارة بطاقات الوقود", "Manage fuel cards", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000092"), "Fuel", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد تقارير بترو أب وسيارة أب وتحديث السجل الشهري للبطاقة.", "Import Petro App and Sayara App reports and update each card's monthly record.", 92, "SENSITIVE_DATA", false, false, false, true, "fuel.import", "استيراد تقارير الوقود", "Import fuel reports", null, false, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardImports_CreatedAtUtc",
                schema: "app",
                table: "FuelCardImports",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardImports_ReportMonth_Provider",
                schema: "app",
                table: "FuelCardImports",
                columns: new[] { "ReportMonth", "Provider" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardImports_Sha256Checksum",
                schema: "app",
                table: "FuelCardImports",
                column: "Sha256Checksum");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_EmployeeId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_FuelCardId_ReportMonth",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                columns: new[] { "FuelCardId", "ReportMonth" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_IsDeleted",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_LastImportId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                column: "LastImportId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_ReportMonth_EmployeeId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                columns: new[] { "ReportMonth", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_ReportMonth_RiderProfileId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                columns: new[] { "ReportMonth", "RiderProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardMonthlyUsages_RiderProfileId",
                schema: "app",
                table: "FuelCardMonthlyUsages",
                column: "RiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardRiderAssignments_EmployeeId_EffectiveFrom",
                schema: "app",
                table: "FuelCardRiderAssignments",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardRiderAssignments_FuelCardId",
                schema: "app",
                table: "FuelCardRiderAssignments",
                column: "FuelCardId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardRiderAssignments_FuelCardId_EffectiveFrom",
                schema: "app",
                table: "FuelCardRiderAssignments",
                columns: new[] { "FuelCardId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCardRiderAssignments_RiderProfileId_EffectiveFrom",
                schema: "app",
                table: "FuelCardRiderAssignments",
                columns: new[] { "RiderProfileId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_IsDeleted",
                schema: "app",
                table: "FuelCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_Provider_IdentifierType",
                schema: "app",
                table: "FuelCards",
                columns: new[] { "Provider", "IdentifierType" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_Provider_NormalizedCardNumber",
                schema: "app",
                table: "FuelCards",
                columns: new[] { "Provider", "NormalizedCardNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelCardMonthlyUsages",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FuelCardRiderAssignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FuelCardImports",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FuelCards",
                schema: "app");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000090"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000091"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000092"));
        }
    }
}
