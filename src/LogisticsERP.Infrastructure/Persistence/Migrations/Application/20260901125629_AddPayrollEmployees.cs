using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPayrollEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollEmployees",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalId = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JoiningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PersonalIban = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollEmployees", x => x.Id);
                    table.CheckConstraint("CK_PayrollEmployees_NationalId", "LEN([NationalId]) = 10 AND [NationalId] NOT LIKE '%[^0-9]%'");
                    table.CheckConstraint("CK_PayrollEmployees_Number", "[Number] > 0");
                    table.CheckConstraint("CK_PayrollEmployees_PersonalIban", "LEN([PersonalIban]) = 24 AND LEFT([PersonalIban], 2) = 'SA' AND SUBSTRING([PersonalIban], 3, 22) NOT LIKE '%[^0-9]%'");
                    table.CheckConstraint("CK_PayrollEmployees_Salary", "[Salary] >= 0");
                });

            migrationBuilder.InsertData(
                schema: "app",
                table: "PayrollEmployees",
                columns: new[] { "Id", "Country", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "JoiningDate", "Name", "NationalId", "Number", "PersonalIban", "Salary", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("01990000-0000-7000-8000-000000000001"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 9, 24), "جمانه عبدالكريم بن حسن القحطاني", "1125236081", 1, "SA6980000107608016495857", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000002"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 10, 14), "ندى علي سلمان غمقه", "1055695991", 2, "SA6980000209608016472812", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000003"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 10, 14), "ريم محمد ابن حابي آل بسام", "1094893391", 3, "SA7680000688608010011525", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000004"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 10, 14), "هتون سعد سالم آل بسام", "1109500338", 4, "SA6380000209608016490962", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000005"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 10, 14), "هديل سعد سالم آل بسام", "1120249709", 5, "SA7480000209608014899867", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000006"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 11, 4), "فيصل سعد سالم آل بسام", "1140492552", 6, "SA8080000107608016555023", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000007"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 10, 14), "رغد عبدالله بن محمد آل هادي", "1124916642", 7, "SA2380000437608016041454", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000008"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 12, 22), "بتلا يحي محمد القحطاني", "1012865497", 8, "SA5880000347608010801019", 1000m, "", null, null },
                    { new Guid("01990000-0000-7000-8000-000000000010"), "السعودية", new DateTimeOffset(new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new DateOnly(2025, 12, 30), "شذي مشعل بن جبر السلمى", "1108386739", 10, "SA3980000176608010913604", 1500m, "", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_IsDeleted",
                schema: "app",
                table: "PayrollEmployees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_Name",
                schema: "app",
                table: "PayrollEmployees",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_NationalId",
                schema: "app",
                table: "PayrollEmployees",
                column: "NationalId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_Number",
                schema: "app",
                table: "PayrollEmployees",
                column: "Number",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_PersonalIban",
                schema: "app",
                table: "PayrollEmployees",
                column: "PersonalIban",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_Status_JoiningDate",
                schema: "app",
                table: "PayrollEmployees",
                columns: new[] { "Status", "JoiningDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollEmployees",
                schema: "app");
        }
    }
}
