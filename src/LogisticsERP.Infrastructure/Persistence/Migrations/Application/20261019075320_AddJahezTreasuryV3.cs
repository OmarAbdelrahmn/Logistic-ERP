using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezTreasuryV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jahez_v3");

            migrationBuilder.AddColumn<string>(
                name: "RequestedByNameAr", schema: "jahez_v2", table: "CashboxHandovers",
                type: "nvarchar(200)", maxLength: 200, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountantNameAr", schema: "jahez_v2", table: "CashboxHandovers",
                type: "nvarchar(200)", maxLength: 200, nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExpectedSponsorId",
                schema: "jahez_v2",
                table: "Operations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TreasuryVersion",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "Ownership",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SponsorNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ownership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ownership_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PetroEvidence",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedCollections = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReplacesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetroEvidence", x => x.Id);
                    table.CheckConstraint("CK_PetroEvidence_Amounts", "[Amount]>=0 AND [AllocatedCollections]>=0 AND [Kind] IN(1,2) AND ([Kind]=1 OR [AllocatedCollections]=0)");
                    table.ForeignKey(
                        name: "FK_PetroEvidence_PetroEvidence_ReplacesId",
                        column: x => x.ReplacesId,
                        principalSchema: "jahez_v3",
                        principalTable: "PetroEvidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PetroEvidence_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "State",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextHandoverNumber = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_State", x => x.Id);
                    table.CheckConstraint("CK_TreasuryState_Singleton", "[Id]='019d0000-0000-7000-8000-000000000001' AND [NextHandoverNumber]>0 AND [IsDeleted]=0");
                });

            migrationBuilder.CreateTable(
                name: "TreasurySubAccounts",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Location = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    AllowedDirections = table.Column<int>(type: "int", nullable: false),
                    IsPosting = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_TreasurySubAccounts", x => x.Id);
                    table.CheckConstraint("CK_TreasuryAccount_Classification", "[Location] BETWEEN 1 AND 4 AND [Purpose] BETWEEN 1 AND 3 AND [AllowedDirections] BETWEEN 1 AND 3 AND ([IsPosting]=0 OR [SponsorId] IS NOT NULL) AND [IsDeleted]=0");
                    table.ForeignKey(
                        name: "FK_TreasurySubAccounts_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreasurySubAccounts_TreasurySubAccounts_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BalanceVerifications",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalanceVerifications_TreasurySubAccounts_SubAccountId",
                        column: x => x.SubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverLines",
                schema: "jahez_v3",
                columns: table => new
                {
                    AccountCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SponsorIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MovementCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverLines_CashboxHandovers_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez_v2",
                        principalTable: "CashboxHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverLines_TreasurySubAccounts_SubAccountId",
                        column: x => x.SubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Postings",
                schema: "jahez_v3",
                columns: table => new
                {
                    ActorNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MoneyMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CashboxHandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    ReversesPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postings", x => x.Id);
                    table.CheckConstraint("CK_TreasuryPosting_Amount", "[Amount]<>0 AND [Kind] BETWEEN 1 AND 5 AND [Revision]>=0");
                    table.ForeignKey(
                        name: "FK_Postings_CashboxHandovers_CashboxHandoverId",
                        column: x => x.CashboxHandoverId,
                        principalSchema: "jahez_v2",
                        principalTable: "CashboxHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postings_MoneyMovements_MoneyMovementId",
                        column: x => x.MoneyMovementId,
                        principalSchema: "jahez_v2",
                        principalTable: "MoneyMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postings_Postings_ReversesPostingId",
                        column: x => x.ReversesPostingId,
                        principalSchema: "jahez_v3",
                        principalTable: "Postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postings_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postings_TreasurySubAccounts_SubAccountId",
                        column: x => x.SubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverPostings",
                schema: "jahez_v3",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverPostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverPostings_CashboxHandovers_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez_v2",
                        principalTable: "CashboxHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverPostings_Postings_PostingId",
                        column: x => x.PostingId,
                        principalSchema: "jahez_v3",
                        principalTable: "Postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000750"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.treasury.read", "jahez.treasury.read", 750, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.treasury.read", "عرض الخزينة", "jahez.treasury.read", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000751"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.treasury.manage", "jahez.treasury.manage", 751, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.treasury.manage", "إدارة الخزينة", "jahez.treasury.manage", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000752"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.treasury.adjust", "jahez.treasury.adjust", 752, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.treasury.adjust", "تسوية وترحيل الخزينة", "jahez.treasury.adjust", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000753"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.financial-reports.read", "jahez.financial-reports.read", 753, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.financial-reports.read", "عرض التقارير المالية", "jahez.financial-reports.read", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000754"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "jahez.financial-reports.export", "jahez.financial-reports.export", 754, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.financial-reports.export", "تصدير التقارير المالية", "jahez.financial-reports.export", null, true, false, null, null, 1 }
                });

            migrationBuilder.InsertData(
                schema: "jahez_v3",
                table: "State",
                columns: new[] { "Id", "ActivatedAtUtc", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsActive", "IsDeleted", "NextHandoverNumber", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019d0000-0000-7000-8000-000000000001"), null, new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, false, 1L, null, null });

            migrationBuilder.InsertData(
                schema: "jahez_v3",
                table: "TreasurySubAccounts",
                columns: new[] { "Id", "AllowedDirections", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsActive", "IsDefault", "IsDeleted", "IsPosting", "Location", "NameAr", "NameEn", "ParentId", "Purpose", "SponsorId", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019d0000-0000-7000-8000-000000000010"), 3, "SUP", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 1, "عهدة مشرف جاهز", "Jahez supervisor custody", null, 3, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000011"), 3, "MAIN", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 2, "الخزينة المركزية", "Central treasury", null, 3, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000012"), 3, "BANK", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 3, "البنك", "Bank", null, 3, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000013"), 3, "PETRO", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 4, "بترو آب - مقاصة التحصيل", "PetroApp collection clearing", null, 3, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000014"), 3, "MAIN.FEES", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 2, "رسوم البوابا وإكسبرس", "Al-Bwaba and Express fees", new Guid("019d0000-0000-7000-8000-000000000011"), 2, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000015"), 3, "BANK.BWEXP", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, false, false, 3, "بنك البوابا وإكسبرس", "Al-Bwaba and Express bank", new Guid("019d0000-0000-7000-8000-000000000012"), 3, null, null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000016"), 3, "SUP.BW.SET", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة تصفيات البوابا", "BW settlement custody", new Guid("019d0000-0000-7000-8000-000000000010"), 1, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000017"), 1, "SUP.BW.FEE", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة رسوم البوابا", "BW fee custody", new Guid("019d0000-0000-7000-8000-000000000010"), 2, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000019"), 3, "MAIN.BW.SET", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 2, "تصفيات البوابا", "BW central settlements", new Guid("019d0000-0000-7000-8000-000000000011"), 1, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000021"), 1, "PETRO.BW", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 4, "بترو آب البوابا", "BW PetroApp clearing", new Guid("019d0000-0000-7000-8000-000000000013"), 3, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000022"), 3, "SUP.EX.SET", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة تصفيات إكسبرس", "EX settlement custody", new Guid("019d0000-0000-7000-8000-000000000010"), 1, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000023"), 1, "SUP.EX.FEE", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة رسوم إكسبرس", "EX fee custody", new Guid("019d0000-0000-7000-8000-000000000010"), 2, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000025"), 3, "MAIN.EX.SET", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 2, "تصفيات إكسبرس", "EX central settlements", new Guid("019d0000-0000-7000-8000-000000000011"), 1, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000027"), 1, "PETRO.EX", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 4, "بترو آب إكسبرس", "EX PetroApp clearing", new Guid("019d0000-0000-7000-8000-000000000013"), 3, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000028"), 3, "SUP.TR.SET", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة تصفيات التجارية", "TR settlement custody", new Guid("019d0000-0000-7000-8000-000000000010"), 1, new Guid("019c18d5-62e1-7000-8000-000000000040"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000029"), 1, "SUP.TR.FEE", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 1, "عهدة رسوم التجارية", "TR fee custody", new Guid("019d0000-0000-7000-8000-000000000010"), 2, new Guid("019c18d5-62e1-7000-8000-000000000040"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000030"), 3, "MAIN.TR", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 2, "صندوق التجارية", "Trading central treasury", new Guid("019d0000-0000-7000-8000-000000000011"), 3, new Guid("019c18d5-62e1-7000-8000-000000000040"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000031"), 2, "BANK.TR", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 3, "بنك التجارية", "TR bank", new Guid("019d0000-0000-7000-8000-000000000012"), 3, new Guid("019c18d5-62e1-7000-8000-000000000040"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000032"), 1, "PETRO.TR", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 4, "بترو آب التجارية", "TR PetroApp clearing", new Guid("019d0000-0000-7000-8000-000000000013"), 3, new Guid("019c18d5-62e1-7000-8000-000000000040"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000018"), 1, "MAIN.BW.FEE", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 2, "رسوم البوابا", "BW central fees", new Guid("019d0000-0000-7000-8000-000000000014"), 2, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000020"), 2, "BANK.BW", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 3, "بنك البوابا", "BW bank", new Guid("019d0000-0000-7000-8000-000000000015"), 3, new Guid("019c18d5-62e1-7000-8000-000000000041"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000024"), 1, "MAIN.EX.FEE", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 2, "رسوم إكسبرس", "EX central fees", new Guid("019d0000-0000-7000-8000-000000000014"), 2, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null },
                    { new Guid("019d0000-0000-7000-8000-000000000026"), 2, "BANK.EX", new DateTimeOffset(new DateTime(2026, 10, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, true, false, true, 3, "بنك إكسبرس", "EX bank", new Guid("019d0000-0000-7000-8000-000000000015"), 3, new Guid("019c18d5-62e1-7000-8000-000000000042"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandovers_DocumentNumber",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                column: "DocumentNumber",
                unique: true,
                filter: "[DocumentNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceVerifications_SubAccountId_VerifiedAtUtc",
                schema: "jahez_v3",
                table: "BalanceVerifications",
                columns: new[] { "SubAccountId", "VerifiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverLines_HandoverId_SubAccountId",
                schema: "jahez_v3",
                table: "HandoverLines",
                columns: new[] { "HandoverId", "SubAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverLines_SubAccountId",
                schema: "jahez_v3",
                table: "HandoverLines",
                column: "SubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverPostings_HandoverId_PostingId",
                schema: "jahez_v3",
                table: "HandoverPostings",
                columns: new[] { "HandoverId", "PostingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverPostings_PostingId",
                schema: "jahez_v3",
                table: "HandoverPostings",
                column: "PostingId");

            migrationBuilder.CreateIndex(
                name: "IX_Ownership_PlatformAccountId_SponsorId",
                schema: "jahez_v3",
                table: "Ownership",
                columns: new[] { "PlatformAccountId", "SponsorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ownership_Source_SourceId",
                schema: "jahez_v3",
                table: "Ownership",
                columns: new[] { "Source", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ownership_SponsorId",
                schema: "jahez_v3",
                table: "Ownership",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_PetroEvidence_ReplacesId",
                schema: "jahez_v3",
                table: "PetroEvidence",
                column: "ReplacesId",
                unique: true,
                filter: "[ReplacesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PetroEvidence_SponsorId_BusinessDate",
                schema: "jahez_v3",
                table: "PetroEvidence",
                columns: new[] { "SponsorId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PetroEvidence_SponsorId_Reference",
                schema: "jahez_v3",
                table: "PetroEvidence",
                columns: new[] { "SponsorId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Postings_BatchId",
                schema: "jahez_v3",
                table: "Postings",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Postings_CashboxHandoverId",
                schema: "jahez_v3",
                table: "Postings",
                column: "CashboxHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_Postings_MoneyMovementId_Revision",
                schema: "jahez_v3",
                table: "Postings",
                columns: new[] { "MoneyMovementId", "Revision" },
                unique: true,
                filter: "[MoneyMovementId] IS NOT NULL AND [Kind] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Postings_ReversesPostingId",
                schema: "jahez_v3",
                table: "Postings",
                column: "ReversesPostingId",
                unique: true,
                filter: "[ReversesPostingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Postings_SponsorId_OccurredAtUtc",
                schema: "jahez_v3",
                table: "Postings",
                columns: new[] { "SponsorId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Postings_SubAccountId_OccurredAtUtc_Id",
                schema: "jahez_v3",
                table: "Postings",
                columns: new[] { "SubAccountId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_State_IsDeleted",
                schema: "jahez_v3",
                table: "State",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TreasurySubAccounts_Code",
                schema: "jahez_v3",
                table: "TreasurySubAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreasurySubAccounts_IsDeleted",
                schema: "jahez_v3",
                table: "TreasurySubAccounts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TreasurySubAccounts_ParentId",
                schema: "jahez_v3",
                table: "TreasurySubAccounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TreasurySubAccounts_SponsorId_Location_Purpose",
                schema: "jahez_v3",
                table: "TreasurySubAccounts",
                columns: new[] { "SponsorId", "Location", "Purpose" },
                unique: true,
                filter: "[IsDefault] = 1 AND [IsPosting] = 1 AND [IsDeleted] = 0");
            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Ownership_Immutable] ON [jahez_v3].[Ownership]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Postings_Immutable] ON [jahez_v3].[Postings]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_BalanceVerifications_Immutable] ON [jahez_v3].[BalanceVerifications]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_HandoverLines_Immutable] ON [jahez_v3].[HandoverLines]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_HandoverPostings_Immutable] ON [jahez_v3].[HandoverPostings]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_PetroEvidence_Immutable] ON [jahez_v3].[PetroEvidence]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, ''Treasury evidence is append-only. Record an audited reversal or replacement.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_Postings_Ownership] ON [jahez_v3].[Postings]
                AFTER INSERT AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS(SELECT 1 FROM inserted i JOIN jahez_v3.TreasurySubAccounts a ON a.Id=i.SubAccountId
                        WHERE a.IsPosting=0 OR a.SponsorId IS NULL OR a.SponsorId<>i.SponsorId)
                        THROW 51000, ''Posting sponsor must match its posting account.'', 1;
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'CREATE TRIGGER [jahez_v3].[TR_TreasuryAccounts_Identity] ON [jahez_v3].[TreasurySubAccounts]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF NOT EXISTS(SELECT 1 FROM inserted) OR UPDATE(Code) OR UPDATE(ParentId) OR UPDATE(SponsorId)
                        OR UPDATE(Location) OR UPDATE(Purpose) OR UPDATE(AllowedDirections) OR UPDATE(IsPosting) OR UPDATE(IsDefault)
                        THROW 51000, ''Treasury account identity and routing are immutable.'', 1;
                END');
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM jahez_v3.Postings) OR EXISTS(SELECT 1 FROM jahez_v3.State WHERE IsActive=1) THROW 51000, 'Treasury contains financial postings; use audited corrections instead of rolling back.', 1;");
            migrationBuilder.DropTable(
                name: "BalanceVerifications",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "HandoverLines",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "HandoverPostings",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "Ownership",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "PetroEvidence",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "State",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "Postings",
                schema: "jahez_v3");

            migrationBuilder.DropTable(
                name: "TreasurySubAccounts",
                schema: "jahez_v3");

            migrationBuilder.DropIndex(
                name: "IX_CashboxHandovers_DocumentNumber",
                schema: "jahez_v2",
                table: "CashboxHandovers");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000750"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000751"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000752"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000753"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000754"));

            migrationBuilder.DropColumn(name: "RequestedByNameAr", schema: "jahez_v2", table: "CashboxHandovers");
            migrationBuilder.DropColumn(name: "AccountantNameAr", schema: "jahez_v2", table: "CashboxHandovers");
            migrationBuilder.DropColumn(
                name: "ExpectedSponsorId",
                schema: "jahez_v2",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                schema: "jahez_v2",
                table: "CashboxHandovers");

            migrationBuilder.DropColumn(
                name: "TreasuryVersion",
                schema: "jahez_v2",
                table: "CashboxHandovers");
        }
    }
}
