using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class LinkFuelCardsToSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [app].[FuelCards]) THROW 51000, 'Assign sponsors to existing fuel cards before applying this migration.', 1;");

            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "FuelCards",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_FuelCards_SponsorId",
                schema: "app",
                table: "FuelCards",
                column: "SponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelCards_Sponsors_SponsorId",
                schema: "app",
                table: "FuelCards",
                column: "SponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelCards_Sponsors_SponsorId",
                schema: "app",
                table: "FuelCards");

            migrationBuilder.DropIndex(
                name: "IX_FuelCards_SponsorId",
                schema: "app",
                table: "FuelCards");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "FuelCards");
        }
    }
}
