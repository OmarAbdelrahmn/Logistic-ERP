using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddChefzAccountsSettlementsAndPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chefz");

            migrationBuilder.CreateTable(
                name: "AccountDetails",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportIdNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AccountDetails", x => x.Id);
                    table.CheckConstraint("CK_ChefzDetails_Type", "[AccountType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_AccountDetails_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceImports",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedRows = table.Column<int>(type: "int", nullable: false),
                    SkippedRows = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceImports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletSettlements",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: false),
                    ChargedFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ChargedTo = table.Column<DateOnly>(type: "date", nullable: false),
                    AssignedDays = table.Column<int>(type: "int", nullable: false),
                    StartWalletAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EndWalletAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossEarnings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    DailyFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RiderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletSettlements", x => x.Id);
                    table.CheckConstraint("CK_ChefzSettlement_Amounts", "[StartWalletAmount] >= 0 AND [EndWalletAmount] >= [StartWalletAmount] AND [GrossEarnings] = [EndWalletAmount] - [StartWalletAmount] AND [RiderAmount] = [GrossEarnings] - [CompanyAmount] AND [AssignedDays] > 0 AND [ChargedFrom] >= [PeriodFrom] AND [ChargedTo] <= [PeriodTo] AND [ChargedTo] >= [ChargedFrom] AND [AccountType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_WalletSettlements_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletSettlements_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletSettlements_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyPerformance",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReportIdNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    TotalValidOffers = table.Column<int>(type: "int", nullable: false),
                    AcceptedOffers = table.Column<int>(type: "int", nullable: false),
                    DeclinedOffers = table.Column<int>(type: "int", nullable: false),
                    MissedOffers = table.Column<int>(type: "int", nullable: false),
                    AcceptanceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPerformance", x => x.Id);
                    table.CheckConstraint("CK_ChefzPerformance_Values", "[TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_DailyPerformance_PerformanceImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "chefz",
                        principalTable: "PerformanceImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyPerformance_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyPerformance_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyPerformance_RiderProfiles_RiderProfileId",
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
                    { new Guid("019c18d5-62e1-7000-a000-000000000306"), "Chefz", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حسابات وأداء وتصفيات شيفز.", "Read Chefz accounts, performance and settlements.", 306, "SENSITIVE_DATA", false, false, false, true, "chefz.read", "عرض شيفز", "Read Chefz", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000307"), "Chefz", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تحديد نوع الحساب ورقم الهوية في شيفز.", "Configure Chefz account type and report identity.", 307, "SENSITIVE_DATA", false, false, false, true, "chefz.accounts.update", "تعديل حسابات شيفز", "Edit Chefz accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000308"), "Chefz", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد أداء المندوب اليومي.", "Import daily rider performance.", 308, "SENSITIVE_DATA", false, false, false, true, "chefz.performance.import", "استيراد أداء شيفز", "Import Chefz performance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000309"), "Chefz", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "حفظ تصفيات محافظ شيفز نصف الشهرية.", "Save half-month Chefz wallet settlements.", 309, "SENSITIVE_DATA", false, false, false, true, "chefz.settlements.create", "إنشاء تصفيات شيفز", "Create Chefz settlements", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountDetails_IsDeleted",
                schema: "chefz",
                table: "AccountDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDetails_PlatformRiderAccountId",
                schema: "chefz",
                table: "AccountDetails",
                column: "PlatformRiderAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountDetails_ReportIdNumber",
                schema: "chefz",
                table: "AccountDetails",
                column: "ReportIdNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_ImportId",
                schema: "chefz",
                table: "DailyPerformance",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "chefz",
                table: "DailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_RiderClientAssignmentId",
                schema: "chefz",
                table: "DailyPerformance",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_RiderProfileId_ReportDate",
                schema: "chefz",
                table: "DailyPerformance",
                columns: new[] { "RiderProfileId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceImports_ReportDate_ContentHash",
                schema: "chefz",
                table: "PerformanceImports",
                columns: new[] { "ReportDate", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletSettlements_PlatformRiderAccountId",
                schema: "chefz",
                table: "WalletSettlements",
                column: "PlatformRiderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletSettlements_RiderClientAssignmentId_PeriodFrom",
                schema: "chefz",
                table: "WalletSettlements",
                columns: new[] { "RiderClientAssignmentId", "PeriodFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletSettlements_RiderProfileId_PeriodFrom",
                schema: "chefz",
                table: "WalletSettlements",
                columns: new[] { "RiderProfileId", "PeriodFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountDetails",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "DailyPerformance",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "WalletSettlements",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "PerformanceImports",
                schema: "chefz");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000306"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000307"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000308"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000309"));
        }
    }
}
