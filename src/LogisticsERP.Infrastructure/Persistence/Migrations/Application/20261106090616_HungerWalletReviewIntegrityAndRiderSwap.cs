using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class HungerWalletReviewIntegrityAndRiderSwap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletReviews_PlatformRiderAccountId",
                schema: "hunger",
                table: "WalletReviews");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "hunger",
                table: "WalletReviews",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<Guid>(
                name: "SwapId",
                schema: "hunger",
                table: "WalletReviews",
                type: "uniqueidentifier",
                nullable: true);

            // Historical confirmations keep their replay result; pending previews need fresh evidence
            // because this release expands the revision inputs and introduces explicit supersession.
            migrationBuilder.Sql("UPDATE [hunger].[WalletReviews] SET [Status] = CASE WHEN [ResultJson] IS NOT NULL THEN 'Confirmed' ELSE 'Superseded' END;");

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_PlatformRiderAccountId_Status",
                schema: "hunger",
                table: "WalletReviews",
                columns: new[] { "PlatformRiderAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_SwapId",
                schema: "hunger",
                table: "WalletReviews",
                column: "SwapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletReviews_PlatformRiderAccountId_Status",
                schema: "hunger",
                table: "WalletReviews");

            migrationBuilder.DropIndex(
                name: "IX_WalletReviews_SwapId",
                schema: "hunger",
                table: "WalletReviews");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "hunger",
                table: "WalletReviews");

            migrationBuilder.DropColumn(
                name: "SwapId",
                schema: "hunger",
                table: "WalletReviews");

            migrationBuilder.CreateIndex(
                name: "IX_WalletReviews_PlatformRiderAccountId",
                schema: "hunger",
                table: "WalletReviews",
                column: "PlatformRiderAccountId");
        }
    }
}
