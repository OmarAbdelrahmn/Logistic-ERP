using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddChefzAccountTypeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FreelancerCommissionBase",
                schema: "chefz",
                table: "WalletSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FullTimeDays",
                schema: "chefz",
                table: "WalletSettlements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountTypeChanges",
                schema: "chefz",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformRiderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousType = table.Column<int>(type: "int", nullable: false),
                    AccountType = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypeChanges", x => x.Id);
                    table.CheckConstraint("CK_ChefzTypeChange_Types", "[PreviousType] IN (1, 2) AND [AccountType] IN (1, 2) AND [PreviousType] <> [AccountType]");
                    table.ForeignKey(
                        name: "FK_AccountTypeChanges_PlatformRiderAccounts_PlatformRiderAccountId",
                        column: x => x.PlatformRiderAccountId,
                        principalSchema: "app",
                        principalTable: "PlatformRiderAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypeChanges_PlatformRiderAccountId_EffectiveFrom",
                schema: "chefz",
                table: "AccountTypeChanges",
                columns: new[] { "PlatformRiderAccountId", "EffectiveFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountTypeChanges",
                schema: "chefz");

            migrationBuilder.DropColumn(
                name: "FreelancerCommissionBase",
                schema: "chefz",
                table: "WalletSettlements");

            migrationBuilder.DropColumn(
                name: "FullTimeDays",
                schema: "chefz",
                table: "WalletSettlements");
        }
    }
}
