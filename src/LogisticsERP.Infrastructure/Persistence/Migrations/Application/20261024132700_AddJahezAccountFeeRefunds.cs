using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddJahezAccountFeeRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountFeeRefunds",
                schema: "jahez_v2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandoverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MoneyMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrossFeeRefund = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CancelledUnpaidFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WaiveCommission = table.Column<bool>(type: "bit", nullable: false),
                    CalculatedCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductedCommission = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CalculationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountFeeRefunds", x => x.Id);
                    table.CheckConstraint("CK_JahezFeeRefund_Amounts", "[GrossFeeRefund] > 0 AND [CancelledUnpaidFee] >= 0 AND [CalculatedCommission] >= 0 AND [DeductedCommission] >= 0 AND [RefundAmount] > 0 AND [RefundAmount] = [GrossFeeRefund] - [DeductedCommission] AND (([WaiveCommission] = 1 AND [DeductedCommission] = 0) OR ([WaiveCommission] = 0 AND [DeductedCommission] = [CalculatedCommission]))");
                    table.ForeignKey(
                        name: "FK_AccountFeeRefunds_AccountFees_AccountFeeId",
                        column: x => x.AccountFeeId,
                        principalSchema: "jahez_v2",
                        principalTable: "AccountFees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountFeeRefunds_AccountSessions_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "jahez_v2",
                        principalTable: "AccountSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountFeeRefunds_MoneyMovements_MoneyMovementId",
                        column: x => x.MoneyMovementId,
                        principalSchema: "jahez_v2",
                        principalTable: "MoneyMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountFeeRefunds_AccountFeeId",
                schema: "jahez_v2",
                table: "AccountFeeRefunds",
                column: "AccountFeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFeeRefunds_HandoverId",
                schema: "jahez_v2",
                table: "AccountFeeRefunds",
                column: "HandoverId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFeeRefunds_MoneyMovementId",
                schema: "jahez_v2",
                table: "AccountFeeRefunds",
                column: "MoneyMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountFeeRefunds_OccurredAtUtc",
                schema: "jahez_v2",
                table: "AccountFeeRefunds",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountFeeRefunds",
                schema: "jahez_v2");
        }
    }
}
