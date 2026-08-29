using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AllowRepeatedTemporaryCredentialHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemporaryCredentials_CredentialHash",
                schema: "identity",
                table: "TemporaryCredentials");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TemporaryCredentials_CredentialHash",
                schema: "identity",
                table: "TemporaryCredentials",
                column: "CredentialHash",
                unique: true);
        }
    }
}
