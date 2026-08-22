using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SeedPrimarySponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "app",
                table: "Sponsors",
                columns: new[] { "Id", "ActiveFrom", "ActiveTo", "CommercialRegistrationNumber", "CompanyProfileId", "ContactEmail", "ContactName", "ContactPhone", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "EmployerIdentityNumber", "IsDeleted", "Notes", "RegistryNameAr", "RegistryNameEn", "SponsorType", "Status", "UnifiedNationalNumber", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-8000-000000000040"), new DateOnly(2026, 1, 1), null, null, new Guid("019c18d5-62e1-7000-8000-000000000001"), null, null, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "7038745530", false, null, "مؤسسة البوابة التجارية", null, 1, 1, null, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000041"), new DateOnly(2026, 1, 1), null, null, new Guid("019c18d5-62e1-7000-8000-000000000001"), null, null, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "7015658094", false, null, "شركة البوابة المقبلة", null, 2, 1, null, null, null },
                    { new Guid("019c18d5-62e1-7000-8000-000000000042"), new DateOnly(2026, 1, 1), null, null, new Guid("019c18d5-62e1-7000-8000-000000000001"), null, null, null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "7034861059", false, null, "اكسبرس جايت", null, 2, 1, null, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "app",
                table: "Sponsors",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "app",
                table: "Sponsors",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "app",
                table: "Sponsors",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-8000-000000000042"));
        }
    }
}
