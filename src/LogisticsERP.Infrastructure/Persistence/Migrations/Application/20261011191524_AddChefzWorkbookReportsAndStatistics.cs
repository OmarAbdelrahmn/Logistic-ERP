using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddChefzWorkbookReportsAndStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DashboardStatus",
                schema: "chefz",
                table: "AccountDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                schema: "chefz",
                table: "AccountDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PerformanceSettings",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallAOrders = table.Column<int>(type: "int", nullable: false),
                    OverallBOrders = table.Column<int>(type: "int", nullable: false),
                    OverallCOrders = table.Column<int>(type: "int", nullable: false),
                    DailyAOrders = table.Column<int>(type: "int", nullable: false),
                    DailyBOrders = table.Column<int>(type: "int", nullable: false),
                    DailyCOrders = table.Column<int>(type: "int", nullable: false),
                    AAcceptanceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BAcceptanceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CAcceptanceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ChangeAfterNoOrderDays = table.Column<int>(type: "int", nullable: false),
                    FollowUpOnZeroOrdersWithMissedOffers = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PerformanceSettings", x => x.Id);
                    table.CheckConstraint("CK_ChefzSettings_Thresholds", "[OverallAOrders] > [OverallBOrders] AND [OverallBOrders] > [OverallCOrders] AND [OverallCOrders] > 0 AND [DailyAOrders] > [DailyBOrders] AND [DailyBOrders] > [DailyCOrders] AND [DailyCOrders] > 0 AND [AAcceptanceRate] BETWEEN 0 AND 100 AND [BAcceptanceRate] BETWEEN 0 AND [AAcceptanceRate] AND [CAcceptanceRate] BETWEEN 0 AND [BAcceptanceRate] AND [ChangeAfterNoOrderDays] > 0");
                });

            migrationBuilder.CreateTable(
                name: "SponsorStatistics",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorshipCount = table.Column<int>(type: "int", nullable: false),
                    Working = table.Column<int>(type: "int", nullable: false),
                    NotWorking = table.Column<int>(type: "int", nullable: false),
                    Blocked = table.Column<int>(type: "int", nullable: false),
                    PermanentlyBlocked = table.Column<int>(type: "int", nullable: false),
                    UpdateBlocked = table.Column<int>(type: "int", nullable: false),
                    Uploaded = table.Column<int>(type: "int", nullable: false),
                    PrivateLicense = table.Column<int>(type: "int", nullable: false),
                    PendingUpload = table.Column<int>(type: "int", nullable: false),
                    NoLicense = table.Column<int>(type: "int", nullable: false),
                    ManagementSuspended = table.Column<int>(type: "int", nullable: false),
                    Motorcycle = table.Column<int>(type: "int", nullable: false),
                    WorkingOutsideSponsorship = table.Column<int>(type: "int", nullable: false),
                    WorkingWithoutPerformance = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SponsorStatistics", x => x.Id);
                    table.CheckConstraint("CK_ChefzStatistics_Nonnegative", "[SponsorshipCount] >= 0 AND [Working] >= 0 AND [NotWorking] >= 0 AND [Blocked] >= 0 AND [PermanentlyBlocked] >= 0 AND [UpdateBlocked] >= 0 AND [Uploaded] >= 0 AND [PrivateLicense] >= 0 AND [PendingUpload] >= 0 AND [NoLicense] >= 0 AND [ManagementSuspended] >= 0 AND [Motorcycle] >= 0 AND [WorkingOutsideSponsorship] >= 0 AND [WorkingWithoutPerformance] >= 0");
                    table.ForeignKey(
                        name: "FK_SponsorStatistics_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "chefz",
                table: "PerformanceSettings",
                columns: new[] { "Id", "AAcceptanceRate", "BAcceptanceRate", "CAcceptanceRate", "ChangeAfterNoOrderDays", "CreatedAtUtc", "CreatedByUserId", "DailyAOrders", "DailyBOrders", "DailyCOrders", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "FollowUpOnZeroOrdersWithMissedOffers", "IsDeleted", "OverallAOrders", "OverallBOrders", "OverallCOrders", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019d18d5-62e1-7000-8000-000000000001"), 80m, 70m, 60m, 2, new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 7, 5, 3, null, null, null, true, false, 80, 60, 40, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceSettings_IsDeleted",
                schema: "chefz",
                table: "PerformanceSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorStatistics_IsDeleted",
                schema: "chefz",
                table: "SponsorStatistics",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorStatistics_SponsorId",
                schema: "chefz",
                table: "SponsorStatistics",
                column: "SponsorId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceSettings",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "SponsorStatistics",
                schema: "chefz");

            migrationBuilder.DropColumn(
                name: "DashboardStatus",
                schema: "chefz",
                table: "AccountDetails");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                schema: "chefz",
                table: "AccountDetails");
        }
    }
}
