using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezWorkflowV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentTarget",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAdjustment",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsV2",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProposedCommission",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProposedNet",
                schema: "jahez",
                table: "JahezApprovalRequest",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionJson",
                schema: "jahez",
                table: "JahezApprovalDecision",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JahezOperation",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    ThroughDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Net = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Commission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculatedCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remaining = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fund = table.Column<int>(type: "int", nullable: true),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    TransferToRider = table.Column<bool>(type: "bit", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ApprovedRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_JahezOperation", x => x.Id);
                    table.CheckConstraint("CK_JahezOperation_Amounts", "[Commission] >= 0 AND [CalculatedCommission] >= 0 AND [Remaining] = [Net] + [Commission]");
                    table.CheckConstraint("CK_JahezOperation_State", "[Mode] BETWEEN 1 AND 3 AND [Status] BETWEEN 1 AND 3 AND [PaymentStatus] BETWEEN 1 AND 4 AND (([Status] = 3 AND [ExecutedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [ExecutedAtUtc] IS NULL))");
                    table.ForeignKey(
                        name: "FK_JahezOperation_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezOperation_JahezApprovalRequest_ApprovedRequestId",
                        column: x => x.ApprovedRequestId,
                        principalSchema: "jahez",
                        principalTable: "JahezApprovalRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezTransferredDebt",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_JahezTransferredDebt", x => x.Id);
                    table.CheckConstraint("CK_JahezTransferredDebt_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_JahezTransferredDebt_JahezOperation_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "jahez",
                        principalTable: "JahezOperation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezTransferredDebt_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JahezMoneyMovement",
                schema: "jahez",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferredDebtId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LegacyCashboxEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fund = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JahezMoneyMovement", x => x.Id);
                    table.CheckConstraint("CK_JahezMoney_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_JahezMoney_Routing", "([Direction] = 1 AND ([Fund] = 1 OR ([Fund] = 2 AND [Purpose] = 2))) OR ([Direction] = 2 AND [Fund] IN (1, 3) AND [Purpose] <> 2)");
                    table.ForeignKey(
                        name: "FK_JahezMoneyMovement_JahezAccountHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez",
                        principalTable: "JahezAccountHandover",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezMoneyMovement_JahezCashboxEntry_LegacyCashboxEntryId",
                        column: x => x.LegacyCashboxEntryId,
                        principalSchema: "jahez",
                        principalTable: "JahezCashboxEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezMoneyMovement_JahezOperation_OperationId",
                        column: x => x.OperationId,
                        principalSchema: "jahez",
                        principalTable: "JahezOperation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezMoneyMovement_JahezTransferredDebt_TransferredDebtId",
                        column: x => x.TransferredDebtId,
                        principalSchema: "jahez",
                        principalTable: "JahezTransferredDebt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JahezMoneyMovement_RiderProfiles_RiderProfileId",
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
                    { new Guid("019c18d5-62e1-7000-a000-000000000700"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "سحب حساب جاهز", "jahez.withdraw", 700, "SENSITIVE_DATA", false, false, false, true, "jahez.withdraw", "سحب حساب جاهز", "jahez.withdraw", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000701"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنفيذ تصفية جاهز", "jahez.settle", 701, "SENSITIVE_DATA", false, false, false, true, "jahez.settle", "تنفيذ تصفية جاهز", "jahez.settle", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000702"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل قبض رسوم جاهز", "jahez.fees.record", 702, "SENSITIVE_DATA", false, false, false, true, "jahez.fees.record", "تسجيل قبض رسوم جاهز", "jahez.fees.record", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000703"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تحديث حالة حساب جاهز", "jahez.accounts.status.update", 703, "SENSITIVE_DATA", false, false, false, true, "jahez.accounts.status.update", "تحديث حالة حساب جاهز", "jahez.accounts.status.update", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000704"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "قبض أموال جاهز", "jahez.money.receive", 704, "SENSITIVE_DATA", false, false, false, true, "jahez.money.receive", "قبض أموال جاهز", "jahez.money.receive", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000705"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صرف أموال جاهز", "jahez.money.disburse", 705, "SENSITIVE_DATA", false, false, false, true, "jahez.money.disburse", "صرف أموال جاهز", "jahez.money.disburse", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000706"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تعديل المديونية الإدارية", "jahez.administrative-debt.adjust", 706, "SENSITIVE_DATA", false, false, false, true, "jahez.administrative-debt.adjust", "تعديل المديونية الإدارية", "jahez.administrative-debt.adjust", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000707"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تأكيد سداد المستحق المرحل", "jahez.transferred-debt.repay", 707, "SENSITIVE_DATA", false, false, false, true, "jahez.transferred-debt.repay", "تأكيد سداد المستحق المرحل", "jahez.transferred-debt.repay", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000708"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب إعفاء رسوم", "jahez.requests.fee.create", 708, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.fee.create", "طلب إعفاء رسوم", "jahez.requests.fee.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000709"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد إعفاء رسوم", "jahez.requests.fee.approve", 709, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.fee.approve", "اعتماد إعفاء رسوم", "jahez.requests.fee.approve", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000710"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب تبديل حساب", "jahez.requests.switch.create", 710, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.switch.create", "طلب تبديل حساب", "jahez.requests.switch.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000711"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد تبديل حساب", "jahez.requests.switch.approve", 711, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.switch.approve", "اعتماد تبديل حساب", "jahez.requests.switch.approve", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000712"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب ترحيل مستحق", "jahez.requests.transfer.create", 712, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.transfer.create", "طلب ترحيل مستحق", "jahez.requests.transfer.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000713"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد ترحيل مستحق", "jahez.requests.transfer.approve", 713, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.transfer.approve", "اعتماد ترحيل مستحق", "jahez.requests.transfer.approve", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000714"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب سياسة عمولة", "jahez.requests.commission.create", 714, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.commission.create", "طلب سياسة عمولة", "jahez.requests.commission.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000715"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد سياسة عمولة", "jahez.requests.commission.approve", 715, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.commission.approve", "اعتماد سياسة عمولة", "jahez.requests.commission.approve", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000716"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب تعديل تصفية أو مديونية", "jahez.requests.adjustment.create", 716, "SENSITIVE_DATA", false, false, false, true, "jahez.requests.adjustment.create", "طلب تعديل تصفية أو مديونية", "jahez.requests.adjustment.create", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000717"), "Jahez", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد تعديل تصفية أو مديونية", "jahez.requests.adjustment.approve", 717, "HIGH_TRUST_ONLY", false, false, true, true, "jahez.requests.adjustment.approve", "اعتماد تعديل تصفية أو مديونية", "jahez.requests.adjustment.approve", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JahezApprovalRequest_OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest",
                column: "OperationId",
                unique: true,
                filter: "[OperationId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_Fund_OccurredAtUtc",
                schema: "jahez",
                table: "JahezMoneyMovement",
                columns: new[] { "Fund", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_HandoverId",
                schema: "jahez",
                table: "JahezMoneyMovement",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_LegacyCashboxEntryId",
                schema: "jahez",
                table: "JahezMoneyMovement",
                column: "LegacyCashboxEntryId",
                unique: true,
                filter: "[LegacyCashboxEntryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_OperationId",
                schema: "jahez",
                table: "JahezMoneyMovement",
                column: "OperationId",
                unique: true,
                filter: "[OperationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_RiderProfileId",
                schema: "jahez",
                table: "JahezMoneyMovement",
                column: "RiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezMoneyMovement_TransferredDebtId",
                schema: "jahez",
                table: "JahezMoneyMovement",
                column: "TransferredDebtId",
                unique: true,
                filter: "[TransferredDebtId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JahezOperation_ApprovedRequestId",
                schema: "jahez",
                table: "JahezOperation",
                column: "ApprovedRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_JahezOperation_HandoverId",
                schema: "jahez",
                table: "JahezOperation",
                column: "HandoverId",
                unique: true,
                filter: "[ExecutedAtUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JahezOperation_HandoverId_CreatedAtUtc",
                schema: "jahez",
                table: "JahezOperation",
                columns: new[] { "HandoverId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_JahezOperation_IsDeleted",
                schema: "jahez",
                table: "JahezOperation",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransferredDebt_IsDeleted",
                schema: "jahez",
                table: "JahezTransferredDebt",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransferredDebt_OperationId",
                schema: "jahez",
                table: "JahezTransferredDebt",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JahezTransferredDebt_RiderProfileId_PaidAtUtc",
                schema: "jahez",
                table: "JahezTransferredDebt",
                columns: new[] { "RiderProfileId", "PaidAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_JahezApprovalRequest_JahezOperation_OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest",
                column: "OperationId",
                principalSchema: "jahez",
                principalTable: "JahezOperation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Opening journal: preserve every historical receipt exactly once, including approved handovers.
            // This changes neither the historical cashbox evidence nor the informational rider ledger.
            migrationBuilder.Sql("""
                INSERT INTO [jahez].[JahezMoneyMovement]
                    ([Id], [HandoverId], [RiderProfileId], [LegacyCashboxEntryId], [Fund], [Direction], [Purpose],
                     [Amount], [ActorUserId], [OccurredAtUtc], [Reference], [Reason], [CreatedAtUtc], [CreatedByUserId])
                SELECT e.[Id], e.[HandoverId], h.[RiderProfileId], e.[Id], 1, 1,
                    CASE WHEN e.[Section] = 1 THEN 3 ELSE 1 END,
                    e.[Amount], e.[CollectedByUserId], e.[ReceivedAtUtc],
                    CONCAT('legacy:', CONVERT(nvarchar(36), e.[Id])), N'ترحيل المقبوضات التاريخية مرة واحدة',
                    e.[CreatedAtUtc], e.[CreatedByUserId]
                FROM [jahez].[JahezCashboxEntry] e
                JOIN [jahez].[JahezAccountHandover] h ON h.[Id] = e.[HandoverId]
                WHERE e.[IsDeleted] = 0 AND NOT EXISTS
                    (SELECT 1 FROM [jahez].[JahezMoneyMovement] m WHERE m.[LegacyCashboxEntryId] = e.[Id]);
                IF (SELECT COALESCE(SUM([Amount]), 0) FROM [jahez].[JahezCashboxEntry] WHERE [IsDeleted] = 0)
                   <> (SELECT COALESCE(SUM([Amount]), 0) FROM [jahez].[JahezMoneyMovement] WHERE [LegacyCashboxEntryId] IS NOT NULL)
                    THROW 51000, 'Jahez legacy receipt reconciliation failed.', 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JahezApprovalRequest_JahezOperation_OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropTable(
                name: "JahezMoneyMovement",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezTransferredDebt",
                schema: "jahez");

            migrationBuilder.DropTable(
                name: "JahezOperation",
                schema: "jahez");

            migrationBuilder.DropIndex(
                name: "IX_JahezApprovalRequest_OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000700"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000701"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000702"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000703"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000704"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000705"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000706"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000707"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000708"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000709"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000710"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000711"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000712"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000713"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000714"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000715"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000716"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000717"));

            migrationBuilder.DropColumn(
                name: "AdjustmentTarget",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "DebtAdjustment",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "IsV2",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "OperationId",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "ProposedCommission",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "ProposedNet",
                schema: "jahez",
                table: "JahezApprovalRequest");

            migrationBuilder.DropColumn(
                name: "DecisionJson",
                schema: "jahez",
                table: "JahezApprovalDecision");
        }
    }
}
