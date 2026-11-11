using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerCr005SealEvidenceExceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAtUtc",
                schema: "hunger",
                table: "FinancialEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "hunger",
                table: "FinancialEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationChannel",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SealAccountsJson",
                schema: "hunger",
                table: "FinancialEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EvidenceFiles",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ObservedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    FinancialEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_EvidenceFiles", x => x.Id);
                    table.CheckConstraint("CK_HungerEvidenceFiles_Purpose", "[Purpose] IN ('WalletBefore','WalletAfter','Wallet','Arrival')");
                    table.ForeignKey(
                        name: "FK_EvidenceFiles_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceFiles_FinancialEvents_FinancialEventId",
                        column: x => x.FinancialEventId,
                        principalSchema: "hunger",
                        principalTable: "FinancialEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceFiles_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExceptionApprovals",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientPlatformId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupervisorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "Requested"),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConsumedByEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_ExceptionApprovals", x => x.Id);
                    table.CheckConstraint("CK_HungerExceptionApprovals_Values", "[Kind] IN ('NonZeroHandover','CustodyIdentityMismatch') AND [Status] IN ('Requested','Approved','Rejected','Consumed') AND [Amount]<>0");
                    table.ForeignKey(
                        name: "FK_ExceptionApprovals_ClientPlatforms_ClientPlatformId",
                        column: x => x.ClientPlatformId,
                        principalSchema: "platform",
                        principalTable: "ClientPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionApprovals_Employees_SupervisorEmployeeId",
                        column: x => x.SupervisorEmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionApprovals_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000384"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد أو رفض المحاسب للعمليات المالية التي رحّلها المشرف.", "Accountant seal or rejection of supervisor-posted finance events.", 384, "HIGH_TRUST_ONLY", false, false, true, true, "hunger.finance.seal", "اعتماد المحاسب لعمليات هنقر", "Seal Hunger finance events", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000385"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد أو تعديل أو رفض طلب تمويل العهدة قبل التسليم.", "Approve, edit or reject a custody funding request before delivery.", 385, "HIGH_TRUST_ONLY", false, false, true, true, "hunger.finance.funding.approve", "اعتماد طلب عهدة هنقر", "Approve Hunger custody funding", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000386"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "طلب استثناء تسليم برصيد غير صفري أو فرق تسوية عهدة.", "Request an exception for a non-zero hand-over or a custody settlement mismatch.", 386, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.exception.request", "طلب استثناء مالي لهنقر", "Request Hunger finance exception", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000387"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد أو رفض طلبات الاستثناء المالية.", "Approve or reject finance exception requests.", 387, "HIGH_TRUST_ONLY", false, false, true, true, "hunger.finance.exception.approve", "اعتماد استثناء مالي لهنقر", "Approve Hunger finance exception", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000388"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تأكيد استلام نقد التحصيل في خزنة أو حساب تحصيلات المالك.", "Confirm receipt of collected cash into the owner collections vault or account.", 388, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.owner_collections.confirm", "تأكيد استلام تحصيلات المالك", "Confirm owner collections receipt", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_ClientPlatformId_ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents",
                columns: new[] { "ClientPlatformId", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "ExceptionApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "SupersedesEventId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HungerFinancialEvents_ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents",
                sql: "[ApprovalStatus] IN ('NotRequired','PendingSeal','Sealed','Rejected','Cancelled')");

            // Rows posted before the seal existed were already acted on; record them as sealed at creation time.
            // ApprovedByUserId stays NULL so they remain distinguishable from a real accountant seal.
            migrationBuilder.Sql("""
                UPDATE [hunger].[FinancialEvents]
                SET [ApprovalStatus] = 'Sealed', [ApprovedAtUtc] = [CreatedAtUtc]
                WHERE [Kind] IN ('TopUpAdvance','TopUpCollected','Collection','WithdrawalCompany','WithdrawalSupervisor','RiderPayment','WalletDelivery','WalletReturn');
                UPDATE [hunger].[FinancialEvents]
                SET [Role] = CASE
                    WHEN [Kind] IN ('Funding','TopUpAdvance','TopUpCollected') THEN 'Custody'
                    WHEN [Kind] = 'Collection' THEN 'Collections'
                    WHEN [Kind] IN ('WithdrawalCompany','WithdrawalSupervisor','WalletDelivery','WalletReturn') THEN 'Wallet'
                    WHEN [Kind] = 'RiderPayment' THEN 'RiderClearing' END
                WHERE [Role] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_ClientPlatformId",
                schema: "hunger",
                table: "EvidenceFiles",
                column: "ClientPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_FinancialEventId",
                schema: "hunger",
                table: "EvidenceFiles",
                column: "FinancialEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_IsDeleted",
                schema: "hunger",
                table: "EvidenceFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_PlatformRiderAccountId_ObservedOn",
                schema: "hunger",
                table: "EvidenceFiles",
                columns: new[] { "PlatformRiderAccountId", "ObservedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionApprovals_ClientPlatformId_IdempotencyKey",
                schema: "hunger",
                table: "ExceptionApprovals",
                columns: new[] { "ClientPlatformId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionApprovals_ClientPlatformId_Status",
                schema: "hunger",
                table: "ExceptionApprovals",
                columns: new[] { "ClientPlatformId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionApprovals_IsDeleted",
                schema: "hunger",
                table: "ExceptionApprovals",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionApprovals_PlatformRiderAccountId",
                schema: "hunger",
                table: "ExceptionApprovals",
                column: "PlatformRiderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionApprovals_SupervisorEmployeeId",
                schema: "hunger",
                table: "ExceptionApprovals",
                column: "SupervisorEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEvents_ExceptionApprovals_ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "ExceptionApprovalId",
                principalSchema: "hunger",
                principalTable: "ExceptionApprovals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialEvents_FinancialEvents_SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents",
                column: "SupersedesEventId",
                principalSchema: "hunger",
                principalTable: "FinancialEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialEvents_ExceptionApprovals_ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialEvents_FinancialEvents_SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropTable(
                name: "EvidenceFiles",
                schema: "hunger");

            migrationBuilder.DropTable(
                name: "ExceptionApprovals",
                schema: "hunger");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvents_ClientPlatformId_ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvents_ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropIndex(
                name: "IX_FinancialEvents_SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HungerFinancialEvents_ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000384"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000385"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000386"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000387"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000388"));

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "DestinationChannel",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "ExceptionApprovalId",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "SealAccountsJson",
                schema: "hunger",
                table: "FinancialEvents");

            migrationBuilder.DropColumn(
                name: "SupersedesEventId",
                schema: "hunger",
                table: "FinancialEvents");
        }
    }
}
