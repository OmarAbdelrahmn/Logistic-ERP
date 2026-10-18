using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezV2AccountantHandovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashboxHandovers",
                schema: "jahez_v2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashboxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PetroAppAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BankAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MovementCount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AccountantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_CashboxHandovers", x => x.Id);
                    table.CheckConstraint("CK_JahezCashboxHandover_Count", "[MovementCount] > 0");
                    table.CheckConstraint("CK_JahezCashboxHandover_State", "[IsDeleted] = 0 AND (([Status] = 1 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NULL) OR ([Status] = 2 AND [AccountantUserId] IS NOT NULL AND [AccountantUserId] <> [RequestedByUserId] AND [ConfirmedAtUtc] IS NOT NULL AND [CancelledAtUtc] IS NULL) OR ([Status] = 3 AND [AccountantUserId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [CancelledAtUtc] IS NOT NULL))");
                });

            migrationBuilder.CreateTable(
                name: "CashboxHandoverMovements",
                schema: "jahez_v2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashboxHandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MoneyMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashboxHandoverMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashboxHandoverMovements_CashboxHandovers_CashboxHandoverId",
                        column: x => x.CashboxHandoverId,
                        principalSchema: "jahez_v2",
                        principalTable: "CashboxHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashboxHandoverMovements_MoneyMovements_MoneyMovementId",
                        column: x => x.MoneyMovementId,
                        principalSchema: "jahez_v2",
                        principalTable: "MoneyMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandoverMovements_CashboxHandoverId_MoneyMovementId",
                schema: "jahez_v2",
                table: "CashboxHandoverMovements",
                columns: new[] { "CashboxHandoverId", "MoneyMovementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandoverMovements_MoneyMovementId",
                schema: "jahez_v2",
                table: "CashboxHandoverMovements",
                column: "MoneyMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandovers_BusinessDate_Id",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                columns: new[] { "BusinessDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandovers_IsDeleted",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CashboxHandovers_Status",
                schema: "jahez_v2",
                table: "CashboxHandovers",
                column: "Status",
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashboxHandoverMovements",
                schema: "jahez_v2");

            migrationBuilder.DropTable(
                name: "CashboxHandovers",
                schema: "jahez_v2");
        }
    }
}
