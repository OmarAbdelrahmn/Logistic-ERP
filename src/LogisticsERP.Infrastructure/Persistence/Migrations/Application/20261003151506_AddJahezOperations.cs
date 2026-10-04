using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jahez");

            migrationBuilder.AddColumn<Guid>(
                name: "DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JahezAccountHandover",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderClientAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CommissionStartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CommissionPostedThrough = table.Column<DateOnly>(type: "date", nullable: true),
                    LastSettlementPaymentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsLegacy = table.Column<bool>(type: "bit", nullable: false),
                    DebtTransferred = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezAccountHandover", x => x.Id);
                    table.CheckConstraint("CK_JahezHandover_Range", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_JahezAccountHandover_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezAccountHandover_RiderClientAssignments_RiderClientAssignmentId",
                        column: x => x.RiderClientAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderClientAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezAccountHandover_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezCashboxHandover",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    SettlementAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    AccountantFeeAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    AccountantSettlementAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezCashboxHandover", x => x.Id);
                    table.CheckConstraint("CK_JahezCashboxHandover_Amounts", "[FeeAmount] >= 0 AND [SettlementAmount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "JahezCommandReceipt",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezCommandReceipt", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JahezImportBatch",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacesBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezImportBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezImportBatch_JahezImportBatch_ReplacesBatchId",
                        column: x => x.ReplacesBatchId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezAccountFee",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    WaivedAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezAccountFee", x => x.Id);
                    table.CheckConstraint("CK_JahezFee_Amount", "[Amount] >= 0 AND [WaivedAmount] >= 0 AND [WaivedAmount] <= [Amount]");
                    table.ForeignKey(
                        name: "FK_JahezAccountFee_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezApprovalRequest",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaiverAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExternalResetReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezApprovalRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezApprovalRequest_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezApprovalRequest_PlatformRiderAccounts_TargetAccountId",
                        column: x => x.TargetAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezEarningsStatement",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalDeliveryPrice = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalPenalties = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalCashAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalDriverDebit = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalServiceDeduction = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalDriverCredit = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalBonuses = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalTips = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalFreeOrders = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezEarningsStatement", x => x.Id);
                    table.CheckConstraint("CK_JahezEarnings_Range", "[ToDate] >= [FromDate]");
                    table.ForeignKey(
                        name: "FK_JahezEarningsStatement_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezEarningsStatement_JahezEarningsStatement_SupersedesId",
                        column: x => x.SupersedesId,
                        principalSchema: "jahez",
                        principalTable: "JahezEarningsStatement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezLedgerEntry",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversesEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezLedgerEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezLedgerEntry_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezLedgerEntry_JahezLedgerEntry_ReversesEntryId",
                        column: x => x.ReversesEntryId,
                        principalSchema: "jahez",
                        principalTable: "JahezLedgerEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezReminderState",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnchorAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezReminderState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezReminderState_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezRiderSettlement",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThroughDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CollectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeePayment = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    DebtPayment = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CommissionPayment = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CountsAsSettlement = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezRiderSettlement", x => x.Id);
                    table.CheckConstraint("CK_JahezSettlement_Payments", "[FeePayment] >= 0 AND [DebtPayment] >= 0 AND [CommissionPayment] >= 0");
                    table.ForeignKey(
                        name: "FK_JahezRiderSettlement_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezImportFile",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezImportFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezImportFile_JahezImportBatch_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezApprovalDecision",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezApprovalDecision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezApprovalDecision_JahezApprovalRequest_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "jahez",
                        principalTable: "JahezApprovalRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezCommissionPolicyPeriod",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezCommissionPolicyPeriod", x => x.Id);
                    table.CheckConstraint("CK_JahezPolicy_Range", "[ToDate] >= [FromDate] AND [Rate] = 0.15");
                    table.ForeignKey(
                        name: "FK_JahezCommissionPolicyPeriod_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezCommissionPolicyPeriod_JahezApprovalRequest_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalSchema: "jahez",
                        principalTable: "JahezApprovalRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezCashboxEntry",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CollectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CashboxHandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezCashboxEntry", x => x.Id);
                    table.CheckConstraint("CK_JahezCashbox_Positive", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_JahezCashboxEntry_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezCashboxEntry_JahezCashboxHandover_CashboxHandoverId",
                        column: x => x.CashboxHandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezCashboxHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezCashboxEntry_JahezRiderSettlement_SettlementId",
                        column: x => x.SettlementId,
                        principalSchema: "jahez",
                        principalTable: "JahezRiderSettlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezImportRow",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeliveryPrice = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    DriverAdjustment = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    Dispatches = table.Column<int>(type: "int", nullable: true),
                    RawValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParseError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezImportRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezImportRow_JahezImportFile_FileId",
                        column: x => x.FileId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportFile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezDailyDispatch",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    AllocationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezDailyDispatch", x => x.Id);
                    table.CheckConstraint("CK_JahezDispatch_Count", "[Count] >= 0");
                    table.ForeignKey(
                        name: "FK_JahezDailyDispatch_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezDailyDispatch_JahezImportBatch_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezDailyDispatch_JahezImportRow_ImportRowId",
                        column: x => x.ImportRowId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezTransaction",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportRowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveryPrice = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    DriverAdjustment = table.Column<decimal>(type: "decimal(19,6)", precision: 19, scale: 6, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JahezTransaction_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezTransaction_JahezImportBatch_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezTransaction_JahezImportRow_ImportRowId",
                        column: x => x.ImportRowId,
                        principalSchema: "jahez",
                        principalTable: "JahezImportRow",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000130"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض جاهز ضمن نطاق جاهز.", "Read Jahez within Jahez scope.", 130, "SENSITIVE_DATA", false, false, false, true, "jahez.read", "عرض جاهز", "Read Jahez", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000131"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسليم وإغلاق حسابات جاهز ضمن نطاق جاهز.", "Manage Jahez handovers within Jahez scope.", 131, "SENSITIVE_DATA", false, false, false, true, "jahez.handovers.manage", "تسليم وإغلاق حسابات جاهز", "Manage Jahez handovers", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000132"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تحصيل مبالغ جاهز ضمن نطاق جاهز.", "Collect Jahez payments within Jahez scope.", 132, "SENSITIVE_DATA", false, false, false, true, "jahez.collections.manage", "تحصيل مبالغ جاهز", "Collect Jahez payments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000133"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء طلبات جاهز ضمن نطاق جاهز.", "Create Jahez requests within Jahez scope.", 133, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.create", "إنشاء طلبات جاهز", "Create Jahez requests", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000134"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد استثناءات جاهز ضمن نطاق جاهز.", "Approve Jahez exceptions within Jahez scope.", 134, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.approve", "اعتماد استثناءات جاهز", "Approve Jahez exceptions", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000135"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد تصفير حسابات جاهز ضمن نطاق جاهز.", "Approve Jahez resets within Jahez scope.", 135, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.resets.approve", "اعتماد تصفير حسابات جاهز", "Approve Jahez resets", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000136"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل أرباح جاهز ضمن نطاق جاهز.", "Manage Jahez earnings within Jahez scope.", 136, "SENSITIVE_DATA", false, false, false, true, "jahez.earnings.manage", "تسجيل أرباح جاهز", "Manage Jahez earnings", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000137"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "استيراد تقارير جاهز ضمن نطاق جاهز.", "Import Jahez reports within Jahez scope.", 137, "SENSITIVE_DATA", false, false, false, true, "jahez.imports.manage", "استيراد تقارير جاهز", "Import Jahez reports", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000138"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسويات وأرصدة جاهز ضمن نطاق جاهز.", "Adjust Jahez balances within Jahez scope.", 138, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.adjustments.manage", "تسويات وأرصدة جاهز", "Adjust Jahez balances", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000139"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض صندوق جاهز ضمن نطاق جاهز.", "Read Jahez cashbox within Jahez scope.", 139, "SENSITIVE_DATA", false, false, false, true, "jahez.cashbox.read", "عرض صندوق جاهز", "Read Jahez cashbox", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000140"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسليم صندوق جاهز ضمن نطاق جاهز.", "Submit Jahez cashbox within Jahez scope.", 140, "SENSITIVE_DATA", false, false, false, true, "jahez.cashbox.submit", "تسليم صندوق جاهز", "Submit Jahez cashbox", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000141"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تأكيد استلام صندوق جاهز ضمن نطاق جاهز.", "Confirm Jahez cashbox receipt within Jahez scope.", 141, "SENSITIVE_DATA", false, false, false, true, "jahez.cashbox.confirm", "تأكيد استلام صندوق جاهز", "Confirm Jahez cashbox receipt", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000142"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد تسليم صندوق جاهز ضمن نطاق جاهز.", "Approve Jahez cashbox handovers within Jahez scope.", 142, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.cashbox.approve", "اعتماد تسليم صندوق جاهز", "Approve Jahez cashbox handovers", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRiderAccounts_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "DashboardSponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountFee_HandoverId",
                schema: "jahez",
                table: "JahezAccountFee",
                column: "HandoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountFee_IsDeleted",
                schema: "jahez",
                table: "JahezAccountFee",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountHandover_IsDeleted",
                schema: "jahez",
                table: "JahezAccountHandover",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountHandover_PlatformRiderAccountId",
                schema: "jahez",
                table: "JahezAccountHandover",
                column: "PlatformRiderAccountId",
                unique: true,
                filter: "[EndedAtUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountHandover_RiderClientAssignmentId",
                schema: "jahez",
                table: "JahezAccountHandover",
                column: "RiderClientAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezAccountHandover_RiderProfileId_StartedAtUtc",
                schema: "jahez",
                table: "JahezAccountHandover",
                columns: new[] { "RiderProfileId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalDecision_RequestId",
                schema: "jahez",
                table: "JahezApprovalDecision",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalRequest_HandoverId",
                schema: "jahez",
                table: "JahezApprovalRequest",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalRequest_IsDeleted",
                schema: "jahez",
                table: "JahezApprovalRequest",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalRequest_Status_CreatedAtUtc",
                schema: "jahez",
                table: "JahezApprovalRequest",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalRequest_TargetAccountId",
                schema: "jahez",
                table: "JahezApprovalRequest",
                column: "TargetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxEntry_CashboxHandoverId_ReceivedAtUtc",
                schema: "jahez",
                table: "JahezCashboxEntry",
                columns: new[] { "CashboxHandoverId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxEntry_HandoverId",
                schema: "jahez",
                table: "JahezCashboxEntry",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxEntry_IsDeleted",
                schema: "jahez",
                table: "JahezCashboxEntry",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxEntry_SettlementId_Section",
                schema: "jahez",
                table: "JahezCashboxEntry",
                columns: new[] { "SettlementId", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxHandover_IsDeleted",
                schema: "jahez",
                table: "JahezCashboxHandover",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezCashboxHandover_Status_BusinessDate",
                schema: "jahez",
                table: "JahezCashboxHandover",
                columns: new[] { "Status", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezCommandReceipt_ActorUserId_Operation_CommandKey",
                schema: "jahez",
                table: "JahezCommandReceipt",
                columns: new[] { "ActorUserId", "Operation", "CommandKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezCommissionPolicyPeriod_ApprovalRequestId",
                schema: "jahez",
                table: "JahezCommissionPolicyPeriod",
                column: "ApprovalRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezCommissionPolicyPeriod_HandoverId_FromDate_ToDate",
                schema: "jahez",
                table: "JahezCommissionPolicyPeriod",
                columns: new[] { "HandoverId", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezDailyDispatch_BatchId",
                schema: "jahez",
                table: "JahezDailyDispatch",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezDailyDispatch_HandoverId_Date",
                schema: "jahez",
                table: "JahezDailyDispatch",
                columns: new[] { "HandoverId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezDailyDispatch_ImportRowId_HandoverId",
                schema: "jahez",
                table: "JahezDailyDispatch",
                columns: new[] { "ImportRowId", "HandoverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezEarningsStatement_HandoverId_FromDate_ToDate",
                schema: "jahez",
                table: "JahezEarningsStatement",
                columns: new[] { "HandoverId", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezEarningsStatement_SupersedesId",
                schema: "jahez",
                table: "JahezEarningsStatement",
                column: "SupersedesId",
                unique: true,
                filter: "[SupersedesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezImportBatch_IsDeleted",
                schema: "jahez",
                table: "JahezImportBatch",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezImportBatch_Kind_ContentHash",
                schema: "jahez",
                table: "JahezImportBatch",
                columns: new[] { "Kind", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezImportBatch_ReplacesBatchId",
                schema: "jahez",
                table: "JahezImportBatch",
                column: "ReplacesBatchId",
                unique: true,
                filter: "[ReplacesBatchId] IS NOT NULL AND [CommittedAtUtc] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezImportFile_BatchId_ContentHash",
                schema: "jahez",
                table: "JahezImportFile",
                columns: new[] { "BatchId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezImportRow_FileId_RowNumber",
                schema: "jahez",
                table: "JahezImportRow",
                columns: new[] { "FileId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezLedgerEntry_HandoverId_Bucket_OccurredAtUtc",
                schema: "jahez",
                table: "JahezLedgerEntry",
                columns: new[] { "HandoverId", "Bucket", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezLedgerEntry_ReversesEntryId",
                schema: "jahez",
                table: "JahezLedgerEntry",
                column: "ReversesEntryId",
                unique: true,
                filter: "[ReversesEntryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezLedgerEntry_SourceId_Bucket_Kind",
                schema: "jahez",
                table: "JahezLedgerEntry",
                columns: new[] { "SourceId", "Bucket", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezReminderState_HandoverId_AnchorAtUtc",
                schema: "jahez",
                table: "JahezReminderState",
                columns: new[] { "HandoverId", "AnchorAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezReminderState_IsDeleted",
                schema: "jahez",
                table: "JahezReminderState",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezRiderSettlement_HandoverId_RecordedAtUtc",
                schema: "jahez",
                table: "JahezRiderSettlement",
                columns: new[] { "HandoverId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransaction_BatchId",
                schema: "jahez",
                table: "JahezTransaction",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransaction_HandoverId_OccurredAtUtc",
                schema: "jahez",
                table: "JahezTransaction",
                columns: new[] { "HandoverId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransaction_ImportRowId",
                schema: "jahez",
                table: "JahezTransaction",
                column: "ImportRowId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts",
                column: "DashboardSponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformRiderAccounts_Sponsors_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DropTable(
                name: "JahezAccountFee",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezApprovalDecision",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezCashboxEntry",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezCommandReceipt",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezCommissionPolicyPeriod",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezDailyDispatch",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezEarningsStatement",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezLedgerEntry",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezReminderState",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezTransaction",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezCashboxHandover",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezRiderSettlement",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezApprovalRequest",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezImportRow",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezAccountHandover",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezImportFile",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezImportBatch",
                schema: "jahez");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRiderAccounts_DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000130"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000131"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000132"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000133"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000134"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000135"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000136"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000137"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000138"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000139"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000140"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000141"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000142"));

            migrationBuilder.DropColumn(
                name: "DashboardSponsorId",
                schema: "app",
                table: "PlatformRiderAccounts");
        }
    }
}
