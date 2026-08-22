using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class ProtectDevelopmentAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDevelopmentOnly",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDevelopmentOnly",
                schema: "identity",
                table: "Users",
                column: "IsDevelopmentOnly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsDevelopmentOnly",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDevelopmentOnly",
                schema: "identity",
                table: "Users");
        }
    }
}
