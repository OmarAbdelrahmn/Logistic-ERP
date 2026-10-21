using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPlatformAccountDayAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyPerformance");

            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyDeductions");

            migrationBuilder.DropIndex(
                name: "IX_DailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "chefz",
                table: "DailyPerformance");

            migrationBuilder.AddColumn<string>(
                name: "DayUsageJson",
                schema: "jahez_v2",
                table: "Operations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                schema: "chefz",
                table: "DailyPerformance",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyPerformanceSources",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_DailyPerformanceSources", x => x.Id);
                    table.CheckConstraint("CK_ChefzPerformanceSource_Values", "[TotalValidOffers] >= 0 AND [AcceptedOffers] >= 0 AND [DeclinedOffers] >= 0 AND [MissedOffers] >= 0 AND [AcceptanceRate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_DailyPerformanceSources_PerformanceImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "chefz",
                        principalTable: "PerformanceImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyPerformanceSources_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportedOrderRows",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UsageSequence = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeliveryFees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tips = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Compensation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bonuses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashCollected = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedOrderRows", x => x.Id);
                    table.CheckConstraint("CK_ChefzImportedRow_Values", "[UsageSequence] > 0 AND [SourceRowNumber] > 1 AND [Deductions] >= 0 AND [CashCollected] >= 0");
                    table.ForeignKey(
                        name: "FK_ImportedOrderRows_OrderSettlementImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "chefz",
                        principalTable: "OrderSettlementImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedOrderRows_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedOrderRows_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NinjaDailyDeductionSources",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalCollectedAmountAtDelivery = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaptainId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaDailyDeductionSources", x => x.Id);
                    table.CheckConstraint("CK_NinjaDeductionSource_Values", "[TotalCollectedAmountAtDelivery] >= 0");
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductionSources_NinjaDailyImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyDeductionSources_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NinjaDailyPerformanceSources",
                schema: "ninja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    TotalWorkingHours = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CaptainId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NinjaDailyPerformanceSources", x => x.Id);
                    table.CheckConstraint("CK_NinjaPerformanceSource_Values", "[TotalOrders] >= 0 AND [TotalWorkingHours] BETWEEN 0 AND 24");
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformanceSources_NinjaDailyImports_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "ninja",
                        principalTable: "NinjaDailyImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NinjaDailyPerformanceSources_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccountDayUsages",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    OrdersCount = table.Column<int>(type: "int", nullable: true),
                    WorkingHours = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    CollectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeductionsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalValidOffers = table.Column<int>(type: "int", nullable: true),
                    DeclinedOffers = table.Column<int>(type: "int", nullable: true),
                    MissedOffers = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
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
                    table.PrimaryKey("PK_PlatformAccountDayUsages", x => x.Id);
                    table.CheckConstraint("CK_PlatformAccountDayUsage_Values", "([Sequence] IS NULL OR [Sequence] > 0) AND ([OrdersCount] IS NULL OR [OrdersCount] >= 0) AND ([WorkingHours] IS NULL OR [WorkingHours] BETWEEN 0 AND 24) AND ([CollectedAmount] IS NULL OR [CollectedAmount] >= 0) AND ([TotalValidOffers] IS NULL OR [TotalValidOffers] >= 0) AND ([DeclinedOffers] IS NULL OR [DeclinedOffers] >= 0) AND ([MissedOffers] IS NULL OR [MissedOffers] >= 0)");
                    table.ForeignKey(
                        name: "FK_PlatformAccountDayUsages_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountDayUsages_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Preserve existing source identities and attribution before introducing source foreign keys.
            // Dynamic SQL also supports idempotent scripts: SourceId is added earlier in the same SQL batch.
            migrationBuilder.Sql("""
                EXEC(N'
                INSERT INTO ninja.NinjaDailyPerformanceSources
                  (Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalOrders, TotalWorkingHours, CreatedAtUtc, CreatedByUserId)
                SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalOrders, TotalWorkingHours, CreatedAtUtc, CreatedByUserId
                FROM ninja.NinjaDailyPerformance;
                UPDATE ninja.NinjaDailyPerformance SET SourceId = Id;
                INSERT INTO ninja.NinjaDailyDeductionSources
                  (Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalCollectedAmountAtDelivery, TotalDeductions, CreatedAtUtc, CreatedByUserId)
                SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, CaptainId, SourceRowNumber, TotalCollectedAmountAtDelivery, TotalDeductions, CreatedAtUtc, CreatedByUserId
                FROM ninja.NinjaDailyDeductions;
                UPDATE ninja.NinjaDailyDeductions SET SourceId = Id;
                INSERT INTO chefz.DailyPerformanceSources
                  (Id, PlatformRiderAccountId, ImportId, ReportDate, ReportIdNumber, SourceRowNumber, TotalValidOffers,
                   AcceptedOffers, DeclinedOffers, MissedOffers, AcceptanceRate, CreatedAtUtc, CreatedByUserId, IsDeleted)
                SELECT Id, PlatformRiderAccountId, ImportId, ReportDate, ReportIdNumber, SourceRowNumber, TotalValidOffers,
                       AcceptedOffers, DeclinedOffers, MissedOffers, AcceptanceRate, CreatedAtUtc, CreatedByUserId, 0
                FROM chefz.DailyPerformance;
                UPDATE chefz.DailyPerformance SET SourceId = Id;

                -- Only boundary days are materialized; long-running assignments do not create a row per day.
                ;WITH BoundaryDates AS (
                  SELECT PlatformRiderAccountId, EffectiveFrom BusinessDate FROM app.RiderClientAssignments WHERE IsDeleted = 0 AND Status IN (2,3)
                  UNION
                  SELECT PlatformRiderAccountId, EffectiveTo FROM app.RiderClientAssignments WHERE IsDeleted = 0 AND Status IN (2,3) AND EffectiveTo IS NOT NULL
                ), Participants AS (
                  SELECT a.Id AssignmentId, a.PlatformRiderAccountId, d.BusinessDate, a.EffectiveFrom, a.EffectiveTo,
                         h.StartedAtUtc, h.EndedAtUtc,
                         COUNT(*) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) ParticipantCount,
                         COUNT(h.Id) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) SessionCount,
                         MIN(a.EffectiveFrom) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) FirstStart,
                         MAX(a.EffectiveFrom) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) LastStart,
                         MIN(a.EffectiveTo) OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate) FirstEnd,
                         ROW_NUMBER() OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate ORDER BY a.EffectiveFrom, a.EffectiveTo) DateSequence,
                         ROW_NUMBER() OVER (PARTITION BY a.PlatformRiderAccountId, d.BusinessDate ORDER BY h.StartedAtUtc) SessionSequence
                  FROM BoundaryDates d JOIN app.RiderClientAssignments a ON a.PlatformRiderAccountId = d.PlatformRiderAccountId
                    AND a.IsDeleted = 0 AND a.Status IN (2,3) AND a.EffectiveFrom <= d.BusinessDate
                    AND (a.EffectiveTo IS NULL OR a.EffectiveTo >= d.BusinessDate)
                  LEFT JOIN jahez_v2.AccountSessions h ON h.RiderClientAssignmentId = a.Id AND h.IsDeleted = 0
                  WHERE h.Id IS NULL OR (h.StartedAtUtc < TODATETIMEOFFSET(DATEADD(day,1,CAST(d.BusinessDate AS datetime2)), ''+03:00'')
                    AND (h.EndedAtUtc IS NULL OR h.EndedAtUtc > TODATETIMEOFFSET(CAST(d.BusinessDate AS datetime2), ''+03:00'')))
                )
                INSERT INTO app.PlatformAccountDayUsages
                  (Id, PlatformRiderAccountId, RiderClientAssignmentId, BusinessDate, [Sequence], Reason, CreatedAtUtc, IsDeleted)
                SELECT NEWID(), p.PlatformRiderAccountId, p.AssignmentId, p.BusinessDate,
                  CASE WHEN p.ParticipantCount = 1 THEN 1
                       WHEN p.SessionCount = p.ParticipantCount AND NOT EXISTS (
                         SELECT 1 FROM Participants x JOIN Participants y ON x.PlatformRiderAccountId = y.PlatformRiderAccountId
                           AND x.BusinessDate = y.BusinessDate AND x.AssignmentId <> y.AssignmentId
                           AND x.StartedAtUtc <= y.StartedAtUtc
                         WHERE x.PlatformRiderAccountId = p.PlatformRiderAccountId AND x.BusinessDate = p.BusinessDate
                           AND (x.StartedAtUtc = y.StartedAtUtc OR x.EndedAtUtc IS NULL OR x.EndedAtUtc > y.StartedAtUtc)
                       ) THEN CAST(p.SessionSequence AS int)
                       WHEN p.ParticipantCount = 2 AND p.FirstStart < p.LastStart AND p.FirstEnd = p.BusinessDate
                         AND p.LastStart = p.BusinessDate AND NOT EXISTS (
                           SELECT 1 FROM Participants x WHERE x.PlatformRiderAccountId = p.PlatformRiderAccountId
                             AND x.BusinessDate = p.BusinessDate AND x.EffectiveFrom = p.FirstStart
                             AND (x.EffectiveTo IS NULL OR x.EffectiveTo <> p.BusinessDate)
                         ) THEN CAST(p.DateSequence AS int)
                       ELSE NULL END,
                  ''Migration: boundary history; figures not inferred'', SYSUTCDATETIME(), 0
                FROM Participants p;
                ');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_SourceId_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                columns: new[] { "SourceId", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                columns: new[] { "PlatformRiderAccountId", "ReportDate", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_SourceId_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                columns: new[] { "SourceId", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "chefz",
                table: "DailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_SourceId_RiderClientAssignmentId",
                schema: "chefz",
                table: "DailyPerformance",
                columns: new[] { "SourceId", "RiderClientAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyOrders_DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "DayUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformanceSources_ImportId",
                schema: "chefz",
                table: "DailyPerformanceSources",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformanceSources_IsDeleted",
                schema: "chefz",
                table: "DailyPerformanceSources",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformanceSources_PlatformRiderAccountId_ReportDate",
                schema: "chefz",
                table: "DailyPerformanceSources",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOrderRows_ImportId_SourceRowNumber",
                schema: "chefz",
                table: "ImportedOrderRows",
                columns: new[] { "ImportId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOrderRows_PlatformRiderAccountId_BusinessDate",
                schema: "chefz",
                table: "ImportedOrderRows",
                columns: new[] { "PlatformRiderAccountId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedOrderRows_RiderClientAssignmentId",
                schema: "chefz",
                table: "ImportedOrderRows",
                column: "RiderClientAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductionSources_ImportId",
                schema: "ninja",
                table: "NinjaDailyDeductionSources",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductionSources_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyDeductionSources",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformanceSources_ImportId",
                schema: "ninja",
                table: "NinjaDailyPerformanceSources",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformanceSources_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyPerformanceSources",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayUsages_IsDeleted",
                schema: "app",
                table: "PlatformAccountDayUsages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_RiderClientAssignmentId",
                schema: "app",
                table: "PlatformAccountDayUsages",
                columns: new[] { "PlatformRiderAccountId", "BusinessDate", "RiderClientAssignmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayUsages_PlatformRiderAccountId_BusinessDate_Sequence",
                schema: "app",
                table: "PlatformAccountDayUsages",
                columns: new[] { "PlatformRiderAccountId", "BusinessDate", "Sequence" },
                unique: true,
                filter: "[Sequence] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountDayUsages_RiderClientAssignmentId",
                schema: "app",
                table: "PlatformAccountDayUsages",
                column: "RiderClientAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyOrders_PlatformAccountDayUsages_DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders",
                column: "DayUsageId",
                principalSchema: "app",
                principalTable: "PlatformAccountDayUsages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyPerformance_DailyPerformanceSources_SourceId",
                schema: "chefz",
                table: "DailyPerformance",
                column: "SourceId",
                principalSchema: "chefz",
                principalTable: "DailyPerformanceSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NinjaDailyDeductions_NinjaDailyDeductionSources_SourceId",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                column: "SourceId",
                principalSchema: "ninja",
                principalTable: "NinjaDailyDeductionSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NinjaDailyPerformance_NinjaDailyPerformanceSources_SourceId",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                column: "SourceId",
                principalSchema: "ninja",
                principalTable: "NinjaDailyPerformanceSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM ninja.NinjaDailyPerformance GROUP BY PlatformRiderAccountId,ReportDate HAVING COUNT(*) > 1)
                  OR EXISTS (SELECT 1 FROM ninja.NinjaDailyDeductions GROUP BY PlatformRiderAccountId,ReportDate HAVING COUNT(*) > 1)
                  OR EXISTS (SELECT 1 FROM chefz.DailyPerformance GROUP BY PlatformRiderAccountId,ReportDate HAVING COUNT(*) > 1)
                  OR EXISTS (SELECT 1 FROM ninja.NinjaDailyPerformance WHERE SourceId <> Id)
                  OR EXISTS (SELECT 1 FROM ninja.NinjaDailyDeductions WHERE SourceId <> Id)
                  OR EXISTS (SELECT 1 FROM chefz.DailyPerformance WHERE SourceId <> Id)
                  OR EXISTS (SELECT 1 FROM chefz.DailyPerformance p JOIN chefz.DailyPerformanceSources s ON s.Id=p.SourceId WHERE s.AcceptanceRate<>p.AcceptanceRate)
                  OR EXISTS (SELECT 1 FROM chefz.ImportedOrderRows)
                  OR EXISTS (SELECT 1 FROM app.PlatformAccountDayUsages WHERE OrdersCount IS NOT NULL OR WorkingHours IS NOT NULL
                    OR CollectedAmount IS NOT NULL OR DeductionsAmount IS NOT NULL OR TotalValidOffers IS NOT NULL OR DeclinedOffers IS NOT NULL OR MissedOffers IS NOT NULL)
                  OR EXISTS (SELECT 1 FROM jahez_v2.Operations WHERE DayUsageJson IS NOT NULL)
                  THROW 51000, 'Shared-day data exists. Preserve the database and use a forward correction instead of dropping allocation evidence.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DailyOrders_PlatformAccountDayUsages_DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyPerformance_DailyPerformanceSources_SourceId",
                schema: "chefz",
                table: "DailyPerformance");

            migrationBuilder.DropForeignKey(
                name: "FK_NinjaDailyDeductions_NinjaDailyDeductionSources_SourceId",
                schema: "ninja",
                table: "NinjaDailyDeductions");

            migrationBuilder.DropForeignKey(
                name: "FK_NinjaDailyPerformance_NinjaDailyPerformanceSources_SourceId",
                schema: "ninja",
                table: "NinjaDailyPerformance");

            migrationBuilder.DropTable(
                name: "DailyPerformanceSources",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "ImportedOrderRows",
                schema: "chefz");

            migrationBuilder.DropTable(
                name: "NinjaDailyDeductionSources",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "NinjaDailyPerformanceSources",
                schema: "ninja");

            migrationBuilder.DropTable(
                name: "PlatformAccountDayUsages",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyPerformance");

            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyPerformance_SourceId_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyPerformance");

            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyDeductions");

            migrationBuilder.DropIndex(
                name: "IX_NinjaDailyDeductions_SourceId_RiderClientAssignmentId",
                schema: "ninja",
                table: "NinjaDailyDeductions");

            migrationBuilder.DropIndex(
                name: "IX_DailyPerformance_PlatformRiderAccountId_ReportDate_RiderClientAssignmentId",
                schema: "chefz",
                table: "DailyPerformance");

            migrationBuilder.DropIndex(
                name: "IX_DailyPerformance_SourceId_RiderClientAssignmentId",
                schema: "chefz",
                table: "DailyPerformance");

            migrationBuilder.DropIndex(
                name: "IX_DailyOrders_DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders");

            migrationBuilder.DropColumn(
                name: "DayUsageJson",
                schema: "jahez_v2",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "ninja",
                table: "NinjaDailyPerformance");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "ninja",
                table: "NinjaDailyDeductions");

            migrationBuilder.DropColumn(
                name: "SourceId",
                schema: "chefz",
                table: "DailyPerformance");

            migrationBuilder.DropColumn(
                name: "DayUsageId",
                schema: "jahez_v2",
                table: "DailyOrders");

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NinjaDailyDeductions_PlatformRiderAccountId_ReportDate",
                schema: "ninja",
                table: "NinjaDailyDeductions",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPerformance_PlatformRiderAccountId_ReportDate",
                schema: "chefz",
                table: "DailyPerformance",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true);
        }
    }
}
