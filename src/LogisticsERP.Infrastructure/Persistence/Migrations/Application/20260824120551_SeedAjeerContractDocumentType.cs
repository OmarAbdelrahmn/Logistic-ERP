using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SeedAjeerContractDocumentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000030"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000031"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000032"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000033"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000034"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000035"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp");

            migrationBuilder.InsertData(
                schema: "platform",
                table: "DocumentTypes",
                columns: new[] { "Id", "AllowedMimeTypes", "AppliesToOutsideRider", "AppliesToRiderProfile", "AppliesToSponsoredInternal", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "IsDeleted", "MaxFileSizeBytes", "NameAr", "NameEn", "RequiresExpiryDate", "RequiresFile", "RequiresIssueDate", "RequiresNumber", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { new Guid("019c18d5-62e1-7000-8000-000000000036"), "application/pdf,image/jpeg,image/png,image/webp,image/gif,image/bmp", true, true, true, "AJEER_CONTRACT", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, false, 10485760L, "عقود اجير", "Ajeer Contracts", true, true, true, true, 1, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000036"));

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000030"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000031"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000032"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000033"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000034"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "DocumentTypes",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000035"),
                column: "AllowedMimeTypes",
                value: "application/pdf,image/jpeg,image/png");
        }
    }
}
