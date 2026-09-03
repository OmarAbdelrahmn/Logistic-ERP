using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPhoneSimReceiptForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptFormContentType",
                schema: "app",
                table: "PhoneSimCards",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFormOriginalFileName",
                schema: "app",
                table: "PhoneSimCards",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFormSha256Checksum",
                schema: "app",
                table: "PhoneSimCards",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReceiptFormSizeBytes",
                schema: "app",
                table: "PhoneSimCards",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFormStoragePath",
                schema: "app",
                table: "PhoneSimCards",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFormStoredFileName",
                schema: "app",
                table: "PhoneSimCards",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptFormContentType",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "ReceiptFormOriginalFileName",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "ReceiptFormSha256Checksum",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "ReceiptFormSizeBytes",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "ReceiptFormStoragePath",
                schema: "app",
                table: "PhoneSimCards");

            migrationBuilder.DropColumn(
                name: "ReceiptFormStoredFileName",
                schema: "app",
                table: "PhoneSimCards");
        }
    }
}
