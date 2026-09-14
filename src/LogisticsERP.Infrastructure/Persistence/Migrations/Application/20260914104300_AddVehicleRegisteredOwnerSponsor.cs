using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleRegisteredOwnerSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles",
                column: "RegisteredOwnerSponsorId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Vehicles_RegisteredOwner",
                schema: "app",
                table: "Vehicles",
                sql: "[RegisteredOwnerSupplierId] IS NULL OR [RegisteredOwnerSponsorId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Sponsors_RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles",
                column: "RegisteredOwnerSponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Sponsors_RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehicles_RegisteredOwner",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegisteredOwnerSponsorId",
                schema: "app",
                table: "Vehicles");
        }
    }
}
