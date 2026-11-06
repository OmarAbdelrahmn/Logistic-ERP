using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerReceiptLifecycleAndFinancePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptAllocations",
                schema: "hunger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemittanceLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptAllocations", x => x.Id);
                    table.CheckConstraint("CK_HungerReceiptAllocation_Amount", "[Amount]>0");
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_FinancialEvents_ConfirmationEventId",
                        column: x => x.ConfirmationEventId,
                        principalSchema: "hunger",
                        principalTable: "FinancialEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptAllocations_RemittanceLines_RemittanceLineId",
                        column: x => x.RemittanceLineId,
                        principalSchema: "hunger",
                        principalTable: "RemittanceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000371"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: FinanceRead.", 371, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.read", "عرض مالية هنقر", "Read Hunger finance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000372"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: FundingRequest.", 372, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.funding.request", "طلب تمويل عهدة هنقر", "Request Hunger custody funding", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000373"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: FundingExecute.", 373, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.funding.execute", "تنفيذ تمويل عهدة هنقر", "Execute Hunger custody funding", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000374"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: TopUpRequest.", 374, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.topup.request", "طلب شحن محفظة هنقر", "Request Hunger wallet top-up", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000375"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: TopUpExecute.", 375, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.topup.execute", "تنفيذ شحن محفظة هنقر", "Execute Hunger wallet top-up", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000376"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: Collection.", 376, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.collection", "تحصيل مستحقات هنقر", "Collect Hunger rider payments", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000377"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: WithdrawalRequest.", 377, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.withdrawal.request", "طلب سحب محفظة هنقر", "Request Hunger wallet withdrawal", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000378"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: WithdrawalArrivalConfirm.", 378, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.withdrawal.arrival.confirm", "تأكيد وصول سحب هنقر", "Confirm Hunger withdrawal arrival", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000379"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: RiderPayment.", 379, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.rider.payment", "صرف مستحقات مندوب هنقر", "Pay Hunger rider entitlement", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000380"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: WalletReview.", 380, "SENSITIVE_DATA", false, false, false, true, "hunger.finance.wallet.review", "مراجعة محفظة هنقر", "Review Hunger wallet", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000381"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: RemittancesSubmit.", 381, "SENSITIVE_DATA", false, false, false, true, "hunger.remittances.submit", "تقديم توريد هنقر", "Submit Hunger remittance", null, true, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000382"), "Hunger", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "صلاحية مالية مستقلة لهنقر.", "Independent Hunger finance action: ReceiptsCancelReverse.", 382, "SENSITIVE_DATA", false, false, false, true, "hunger.remittances.cancel_reverse", "إلغاء وعكس توريد هنقر", "Cancel or reverse Hunger receipt", null, true, false, null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_ConfirmationEventId_RemittanceLineId",
                schema: "hunger",
                table: "ReceiptAllocations",
                columns: new[] { "ConfirmationEventId", "RemittanceLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptAllocations_RemittanceLineId",
                schema: "hunger",
                table: "ReceiptAllocations",
                column: "RemittanceLineId");
            // Historical confirmations stored their source selection in DetailsJson, but not immutable allocations.
            // Replay in creation order; all-remaining confirmations consume only what earlier confirmations left.
            migrationBuilder.Sql("""
                DECLARE @confirmation uniqueidentifier, @batch uniqueidentifier, @details nvarchar(max),
                    @created datetimeoffset, @actor uniqueidentifier, @amount decimal(18,2);
                DECLARE confirmations CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [Id], [RelatedEventId], [DetailsJson], [CreatedAtUtc], [CreatedByUserId], [Amount]
                    FROM [hunger].[FinancialEvents] WHERE [Kind] = N'RemittanceConfirmation'
                    ORDER BY [CreatedAtUtc], [Id];
                OPEN confirmations;
                FETCH NEXT FROM confirmations INTO @confirmation, @batch, @details, @created, @actor, @amount;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF JSON_QUERY(@details, '$.sources') IS NOT NULL
                        INSERT INTO [hunger].[ReceiptAllocations]
                            ([Id], [ConfirmationEventId], [RemittanceLineId], [Amount], [CreatedAtUtc], [CreatedByUserId])
                        SELECT NEWID(), @confirmation, l.[Id], j.[Amount], @created, @actor
                        FROM OPENJSON(@details, '$.sources') WITH
                            ([SourceEventId] uniqueidentifier '$.sourceEventId', [Amount] decimal(18,2) '$.amount') j
                        JOIN [hunger].[RemittanceLines] l ON l.[RemittanceEventId] = @batch AND l.[SourceEventId] = j.[SourceEventId];
                    ELSE
                        INSERT INTO [hunger].[ReceiptAllocations]
                            ([Id], [ConfirmationEventId], [RemittanceLineId], [Amount], [CreatedAtUtc], [CreatedByUserId])
                        SELECT NEWID(), @confirmation, l.[Id], l.[Amount] - COALESCE(a.[Allocated], 0), @created, @actor
                        FROM [hunger].[RemittanceLines] l
                        OUTER APPLY (SELECT SUM(r.[Amount]) AS [Allocated] FROM [hunger].[ReceiptAllocations] r WHERE r.[RemittanceLineId] = l.[Id]) a
                        WHERE l.[RemittanceEventId] = @batch AND l.[Amount] > COALESCE(a.[Allocated], 0);
                    IF COALESCE((SELECT SUM([Amount]) FROM [hunger].[ReceiptAllocations] WHERE [ConfirmationEventId] = @confirmation), 0) <> @amount
                        THROW 51000, 'Historical Hunger receipt source allocations do not reconcile. Migration rolled back.', 1;
                    FETCH NEXT FROM confirmations INTO @confirmation, @batch, @details, @created, @actor, @amount;
                END;
                CLOSE confirmations;
                DEALLOCATE confirmations;
                IF EXISTS (
                    SELECT 1 FROM [hunger].[RemittanceLines] l
                    OUTER APPLY (SELECT SUM(r.[Amount]) AS [Allocated] FROM [hunger].[ReceiptAllocations] r WHERE r.[RemittanceLineId] = l.[Id]) a
                    WHERE COALESCE(a.[Allocated], 0) <> l.[ConfirmedAmount])
                    THROW 51000, 'Historical Hunger receipt line totals do not reconcile. Migration rolled back.', 1;
                UPDATE b SET [Status] = CASE WHEN COALESCE(a.[Confirmed], 0) = b.[Amount] THEN N'Confirmed'
                    WHEN COALESCE(a.[Confirmed], 0) > 0 THEN N'PartiallyConfirmed' ELSE N'Submitted' END
                FROM [hunger].[FinancialEvents] b
                OUTER APPLY (SELECT SUM(l.[ConfirmedAmount]) AS [Confirmed] FROM [hunger].[RemittanceLines] l WHERE l.[RemittanceEventId] = b.[Id]) a
                WHERE b.[Kind] = N'Remittance' AND b.[Status] = N'Posted';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptAllocations",
                schema: "hunger");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000371"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000372"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000373"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000374"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000375"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000376"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000377"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000378"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000379"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000380"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000381"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000382"));
        }
    }
}
