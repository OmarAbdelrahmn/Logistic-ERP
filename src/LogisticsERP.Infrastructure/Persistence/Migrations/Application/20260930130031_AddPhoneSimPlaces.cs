using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPhoneSimPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlaceId",
                schema: "app",
                table: "PhoneSimCards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Places",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSimCards_PlaceId",
                schema: "app",
                table: "PhoneSimCards",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Name",
                schema: "app",
                table: "Places",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneSimCards_Places_PlaceId",
                schema: "app",
                table: "PhoneSimCards",
                column: "PlaceId",
                principalSchema: "app",
                principalTable: "Places",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneSimCards_Places_PlaceId",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropTable(
                name: "Places",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_PhoneSimCards_PlaceId",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                schema: "app",
                table: "PhoneSimCards");
        }
    }
}
