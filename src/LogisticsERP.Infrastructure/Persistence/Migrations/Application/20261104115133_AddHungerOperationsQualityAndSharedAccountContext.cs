using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHungerOperationsQualityAndSharedAccountContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hunger");

            migrationBuilder.AddColumn<string>(
                name: "CompensationModel",
                schema: "app",
                table: "RiderClientAssignments",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientPlatformId",
                schema: "jahez_v3",
                table: "Postings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceEventId",
                schema: "jahez_v3",
                table: "Postings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupervisorEmployeeId",
                schema: "jahez_v3",
                table: "Postings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityCompletedOrders",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityCompletedOrdersInTime",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityFailedOrders",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityGrossOrders",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualitySuccessfulVerifications",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityVerificationRequests",
                schema: "app",
                table: "PlatformAccountDayUsages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ownership",
                schema: "app",
                table: "PhoneSimCards",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateOnly>(
                name: "OwnershipEffectiveFrom",
                schema: "app",
                table: "PhoneSimCards",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipEvidence",
                schema: "app",
                table: "PhoneSimCards",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertRules",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRules_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConfigurations",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupervisorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SponsorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodySubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionsSubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletSubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClearingSubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_FinanceConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_Employees_SupervisorEmployeeId",
                        column: x => x.SupervisorEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalSchema: "app",
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_TreasurySubAccounts_CollectionsSubAccountId",
                        column: x => x.CollectionsSubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_TreasurySubAccounts_CustodySubAccountId",
                        column: x => x.CustodySubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_TreasurySubAccounts_RiderClearingSubAccountId",
                        column: x => x.RiderClearingSubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceConfigurations_TreasurySubAccounts_WalletSubAccountId",
                        column: x => x.WalletSubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEvents",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupervisorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromSubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToSubAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RiderBalanceChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_Employees_SupervisorEmployeeId",
                        column: x => x.SupervisorEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_FinancialEvents_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalSchema: "hunger",
                        principalTable: "FinancialEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_TreasurySubAccounts_FromSubAccountId",
                        column: x => x.FromSubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_TreasurySubAccounts_ToSubAccountId",
                        column: x => x.ToSubAccountId,
                        principalSchema: "jahez_v3",
                        principalTable: "TreasurySubAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportPreviews",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InputRevision = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_ImportPreviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneSimOwnershipRevisions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneSimCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ownership = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneSimOwnershipRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneSimOwnershipRevisions_PhoneSimCards_PhoneSimCardId",
                        column: x => x.PhoneSimCardId,
                        principalSchema: "app",
                        principalTable: "PhoneSimCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccountBusinessDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RegistryIdentifier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SettlementModel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RegisteredVehicleType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RegisteredVehiclePlate = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RegisteredVehicleSerial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperationalDisposition = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DispositionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeferredUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    SimOwnership = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SourceStatus = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SourceActualRiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceActualIqama = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceCompensationModel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SourceVehicleDescription = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SourceAssignmentFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceOpeningWallet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SourceOpeningOrders = table.Column<int>(type: "int", nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PlatformAccountBusinessDetails", x => x.Id);
                    table.CheckConstraint("CK_PlatformBusiness_Disposition", "[OperationalDisposition] IN ('Normal','Problem','AdministrativeStop','Blocked','DeferredNextMonth')");
                    table.ForeignKey(
                        name: "FK_PlatformAccountBusinessDetails_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformAccountBusinessDetails_RiderProfiles_SourceActualRiderProfileId",
                        column: x => x.SourceActualRiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccountBusinessRevisions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAccountBusinessRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAccountBusinessRevisions_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SettlementDocuments",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InvoicePeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    ChargeFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ChargeThrough = table.Column<DateOnly>(type: "date", nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CompanyControlTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CanonicalGrossTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementDocuments_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementDocuments_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementDocuments_SettlementDocuments_InvoiceDocumentId",
                        column: x => x.InvoiceDocumentId,
                        principalSchema: "hunger",
                        principalTable: "SettlementDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletReviews",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputRevision = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_WalletReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletReviews_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RemittanceLines",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemittanceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfirmedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_RemittanceLines", x => x.Id);
                    table.CheckConstraint("CK_HungerRemittance_Amounts", "[Amount]>0 AND [ConfirmedAmount]>=0 AND [ConfirmedAmount]<=[Amount]");
                    table.ForeignKey(
                        name: "FK_RemittanceLines_FinancialEvents_RemittanceEventId",
                        column: x => x.RemittanceEventId,
                        principalSchema: "hunger",
                        principalTable: "FinancialEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RemittanceLines_FinancialEvents_SourceEventId",
                        column: x => x.SourceEventId,
                        principalSchema: "hunger",
                        principalTable: "FinancialEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyOperations",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    CompletedOrders = table.Column<int>(type: "int", nullable: false),
                    ActualWorkingHours = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RejectedOrders = table.Column<int>(type: "int", nullable: true),
                    CancelledOrders = table.Column<int>(type: "int", nullable: true),
                    NoShowCount = table.Column<int>(type: "int", nullable: true),
                    ManualOrders = table.Column<int>(type: "int", nullable: true),
                    ManualHours = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    ManualReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AllocationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllocationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_DailyOperations", x => x.Id);
                    table.CheckConstraint("CK_HungerDaily_Values", "[CompletedOrders]>=0 AND [ActualWorkingHours] BETWEEN 0 AND 24 AND ([RejectedOrders] IS NULL OR [RejectedOrders]>=0) AND ([CancelledOrders] IS NULL OR [CancelledOrders]>=0) AND ([NoShowCount] IS NULL OR [NoShowCount]>=0) AND ([ManualOrders] IS NULL OR [ManualOrders]>=0) AND ([ManualHours] IS NULL OR [ManualHours] BETWEEN 0 AND 24)");
                    table.ForeignKey(
                        name: "FK_DailyOperations_ImportPreviews_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "hunger",
                        principalTable: "ImportPreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyOperations_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualitySnapshots",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CoverageFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    CountersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyCountersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VerificationSuccessRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    OnTimeDeliveryScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    FinalDeliveryQualityScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    AllocationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SoleRiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllocationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_QualitySnapshots", x => x.Id);
                    table.CheckConstraint("CK_HungerQuality_Rates", "[VerificationSuccessRate] BETWEEN 0 AND 1 AND [OnTimeDeliveryScore] BETWEEN 0 AND 1 AND [FinalDeliveryQualityScore] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_QualitySnapshots_ImportPreviews_ImportId",
                        column: x => x.ImportId,
                        principalSchema: "hunger",
                        principalTable: "ImportPreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySnapshots_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySnapshots_RiderProfiles_SoleRiderProfileId",
                        column: x => x.SoleRiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityRevisions",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerImportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityRevisions_ImportPreviews_TriggerImportId",
                        column: x => x.TriggerImportId,
                        principalSchema: "hunger",
                        principalTable: "ImportPreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityRevisions_QualitySnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalSchema: "hunger",
                        principalTable: "QualitySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000350"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض حسابات وتقارير هنقر.", "Read Hunger accounts and reports.", 350, "SENSITIVE_DATA", false, false, false, true, "hunger.read", "عرض هنقرستيشن", "Read Hunger", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000351"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "معاينة واعتماد الجودة التراكمية.", "Preview and confirm cumulative quality.", 351, "SENSITIVE_DATA", false, false, false, true, "hunger.quality.import", "استيراد جودة هنقر", "Import Hunger quality", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000352"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصدير الجودة المسموح بها.", "Export authorized quality.", 352, "SENSITIVE_DATA", false, false, false, true, "hunger.quality.export", "تصدير جودة هنقر", "Export Hunger quality", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000353"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "مطابقة واستيراد الحسابات وأصحابها.", "Match and import accounts and owners.", 353, "SENSITIVE_DATA", false, false, false, true, "hunger.accounts.import", "استيراد حسابات هنقر", "Import Hunger accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000354"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل بيانات وحالة هنقر.", "Update Hunger account details and disposition.", 354, "SENSITIVE_DATA", false, false, false, true, "hunger.accounts.update", "تعديل حسابات هنقر", "Update Hunger accounts", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000355"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد التشغيل اليومي.", "Import daily operational performance.", 355, "SENSITIVE_DATA", false, false, false, true, "hunger.performance.import", "استيراد تشغيل هنقر", "Import Hunger operations", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000356"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "مراجعة واعتماد عمليات محافظ هنقر.", "Review and confirm Hunger wallet operations.", 356, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.manage", "إدارة مالية هنقر", "Manage Hunger finance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000357"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تأكيد استلام التوريد الفعلي.", "Confirm actual remittance receipts.", 357, "SENSITIVE_DATA", false, false, false, true, "hunger.remittances.confirm", "تأكيد توريد هنقر", "Confirm Hunger remittance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000359"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد الربط المحاسبي الصريح للمشرف.", "Approve explicit supervisor accounting mappings.", 359, "HIGH_TRUST_ONLY", false, false, true, true, "hunger.finance.configure", "ربط حسابات هنقر المحاسبية", "Configure Hunger finance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000360"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: هوية الحساب.", "Independent Identity field access.", 360, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.identity", "هوية الحساب", "Identity field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000361"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: صاحب الحساب.", "Independent Owner field access.", 361, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.owner", "صاحب الحساب", "Owner field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000362"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: مدينة الحساب.", "Independent City field access.", 362, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.city", "مدينة الحساب", "City field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000363"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: سجل الحساب.", "Independent Registry field access.", 363, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.registry", "سجل الحساب", "Registry field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000364"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: هاتف وشريحة الحساب.", "Independent Contact field access.", 364, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.contact", "هاتف وشريحة الحساب", "Contact field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000365"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: المركبة المسجلة.", "Independent Vehicle field access.", 365, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.registered_vehicle", "المركبة المسجلة", "Vehicle field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000366"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: نظام تسوية الحساب.", "Independent Settlement field access.", 366, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.fields.settlement", "نظام تسوية الحساب", "Settlement field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000367"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: محاسبة المندوب.", "Independent Compensation field access.", 367, "SENSITIVE_DATA", false, false, false, true, "platform_assignments.fields.compensation", "محاسبة المندوب", "Compensation field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000368"), "PlatformFields", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مستقلة: قراءة كلمة مرور الحساب.", "Independent Reveal field access.", 368, "SENSITIVE_DATA", false, false, false, true, "platform_accounts.credentials.reveal", "قراءة كلمة مرور الحساب", "Reveal field access", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000369"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصدير كامل النطاق المسموح للتشغيل.", "Export all authorized operational rows.", 369, "SENSITIVE_DATA", false, false, false, true, "hunger.performance.export", "تصدير تشغيل هنقر", "Export Hunger operations", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000370"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إضافة نسخ مؤرخة من قواعد المتابعة.", "Add versioned monitoring rules.", 370, "SENSITIVE_DATA", false, false, false, true, "hunger.alerts.manage", "إدارة تنبيهات هنقر", "Manage Hunger alerts", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Postings_ClientPlatformId_SupervisorEmployeeId_SubAccountId",
                schema: "jahez_v3",
                table: "Postings",
                columns: new[] { "ClientPlatformId", "SupervisorEmployeeId", "SubAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Postings_SourceEventId_SubAccountId",
                schema: "jahez_v3",
                table: "Postings",
                columns: new[] { "SourceEventId", "SubAccountId" },
                unique: true,
                filter: "[SourceEventId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhoneSim_Ownership",
                schema: "app",
                table: "PhoneSimCards",
                sql: "[Ownership] IN ('Company','Private','Unknown')");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_ClientPlatformId_Code_EffectiveFrom_CreatedAtUtc",
                schema: "hunger",
                table: "AlertRules",
                columns: new[] { "ClientPlatformId", "Code", "EffectiveFrom", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyOperations_ImportId",
                schema: "hunger",
                table: "DailyOperations",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOperations_IsDeleted",
                schema: "hunger",
                table: "DailyOperations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyOperations_PlatformRiderAccountId_ReportDate",
                schema: "hunger",
                table: "DailyOperations",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_ClientPlatformId_SupervisorEmployeeId",
                schema: "hunger",
                table: "FinanceConfigurations",
                columns: new[] { "ClientPlatformId", "SupervisorEmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_CollectionsSubAccountId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "CollectionsSubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_CustodySubAccountId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "CustodySubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_IsDeleted",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_RiderClearingSubAccountId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "RiderClearingSubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_SponsorId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_SupervisorEmployeeId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "SupervisorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConfigurations_WalletSubAccountId",
                schema: "hunger",
                table: "FinanceConfigurations",
                column: "WalletSubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_ClientPlatformId_IdempotencyKey",
                schema: "hunger",
                table: "FinancialEvents",
                columns: new[] { "ClientPlatformId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_FromSubAccountId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "FromSubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_PlatformRiderAccountId_BusinessDate",
                schema: "hunger",
                table: "FinancialEvents",
                columns: new[] { "PlatformRiderAccountId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_RelatedEventId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_RiderProfileId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "RiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_SupervisorEmployeeId_BusinessDate",
                schema: "hunger",
                table: "FinancialEvents",
                columns: new[] { "SupervisorEmployeeId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_ToSubAccountId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "ToSubAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportPreviews_IsDeleted",
                schema: "hunger",
                table: "ImportPreviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ImportPreviews_UserId_ExpiresAtUtc",
                schema: "hunger",
                table: "ImportPreviews",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimOwnershipRevisions_PhoneSimCardId_EffectiveFrom",
                schema: "app",
                table: "PhoneSimOwnershipRevisions",
                columns: new[] { "PhoneSimCardId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountBusinessDetails_IsDeleted",
                schema: "app",
                table: "PlatformAccountBusinessDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountBusinessDetails_PlatformRiderAccountId",
                schema: "app",
                table: "PlatformAccountBusinessDetails",
                column: "PlatformRiderAccountId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountBusinessDetails_SourceActualRiderProfileId",
                schema: "app",
                table: "PlatformAccountBusinessDetails",
                column: "SourceActualRiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccountBusinessRevisions_PlatformRiderAccountId_EffectiveFrom",
                schema: "app",
                table: "PlatformAccountBusinessRevisions",
                columns: new[] { "PlatformRiderAccountId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityRevisions_SnapshotId",
                schema: "hunger",
                table: "QualityRevisions",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityRevisions_TriggerImportId",
                schema: "hunger",
                table: "QualityRevisions",
                column: "TriggerImportId");

            migrationBuilder.CreateIndex(
                name: "IX_QualitySnapshots_ImportId",
                schema: "hunger",
                table: "QualitySnapshots",
                column: "ImportId");

            migrationBuilder.CreateIndex(
                name: "IX_QualitySnapshots_IsDeleted",
                schema: "hunger",
                table: "QualitySnapshots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_QualitySnapshots_PlatformRiderAccountId_ReportDate",
                schema: "hunger",
                table: "QualitySnapshots",
                columns: new[] { "PlatformRiderAccountId", "ReportDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_QualitySnapshots_SoleRiderProfileId",
                schema: "hunger",
                table: "QualitySnapshots",
                column: "SoleRiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_IsDeleted",
                schema: "hunger",
                table: "RemittanceLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_RemittanceEventId_SourceEventId",
                schema: "hunger",
                table: "RemittanceLines",
                columns: new[] { "RemittanceEventId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceLines_SourceEventId",
                schema: "hunger",
                table: "RemittanceLines",
                column: "SourceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDocuments_ClientPlatformId_IdempotencyKey",
                schema: "hunger",
                table: "SettlementDocuments",
                columns: new[] { "ClientPlatformId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDocuments_InvoiceDocumentId",
                schema: "hunger",
                table: "SettlementDocuments",
                column: "InvoiceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementDocuments_PlatformRiderAccountId_Kind_ChargeFrom_ChargeThrough",
                schema: "hunger",
                table: "SettlementDocuments",
                columns: new[] { "PlatformRiderAccountId", "Kind", "ChargeFrom", "ChargeThrough" },
                unique: true,
                filter: "[Kind]='MonthClose' AND [PlatformRiderAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_IsDeleted",
                schema: "hunger",
                table: "WalletReviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_PlatformRiderAccountId",
                schema: "hunger",
                table: "WalletReviews",
                column: "PlatformRiderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_UserId_ExpiresAtUtc",
                schema: "hunger",
                table: "WalletReviews",
                columns: new[] { "UserId", "ExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM hunger.FinancialEvents)
                   OR EXISTS (SELECT 1 FROM hunger.QualitySnapshots)
                   OR EXISTS (SELECT 1 FROM hunger.DailyOperations)
                   OR EXISTS (SELECT 1 FROM hunger.SettlementDocuments)
                   OR EXISTS (SELECT 1 FROM app.PlatformAccountBusinessDetails)
                   OR EXISTS (SELECT 1 FROM app.PhoneSimOwnershipRevisions)
                    THROW 51000, 'Hunger contains business evidence. Use a reviewed forward correction; rollback would discard history.', 1;
                """);
            migrationBuilder.DropTable(
                name: "AlertRules",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "DailyOperations",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "FinanceConfigurations",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "PhoneSimOwnershipRevisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlatformAccountBusinessDetails",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PlatformAccountBusinessRevisions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "QualityRevisions",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "RemittanceLines",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "SettlementDocuments",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "WalletReviews",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "QualitySnapshots",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "FinancialEvents",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "ImportPreviews",
                schema: "hunger");

            migrationBuilder.DropIndex(
                name: "IX_Postings_ClientPlatformId_SupervisorEmployeeId_SubAccountId",
                schema: "jahez_v3",
                table: "Postings");

            migrationBuilder.DropIndex(
                name: "IX_Postings_SourceEventId_SubAccountId",
                schema: "jahez_v3",
                table: "Postings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhoneSim_Ownership",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000350"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000351"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000352"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000353"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000354"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000355"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000356"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000357"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000359"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000360"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000361"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000362"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000363"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000364"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000365"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000366"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000367"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000368"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000369"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000370"));

            migrationBuilder.DropColumn(
                name: "CompensationModel",
                schema: "app",
                table: "RiderClientAssignments");

            migrationBuilder.DropColumn(
                name: "ClientPlatformId",
                schema: "jahez_v3",
                table: "Postings");

            migrationBuilder.DropColumn(
                name: "SourceEventId",
                schema: "jahez_v3",
                table: "Postings");

            migrationBuilder.DropColumn(
                name: "SupervisorEmployeeId",
                schema: "jahez_v3",
                table: "Postings");

            migrationBuilder.DropColumn(
                name: "QualityCompletedOrders",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "QualityCompletedOrdersInTime",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "QualityFailedOrders",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "QualityGrossOrders",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "QualitySuccessfulVerifications",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "QualityVerificationRequests",
                schema: "app",
                table: "PlatformAccountDayUsages");

            migrationBuilder.DropColumn(
                name: "Ownership",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "OwnershipEffectiveFrom",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "OwnershipEvidence",
                schema: "app",
                table: "PhoneSimCards");
        }
    }
}
