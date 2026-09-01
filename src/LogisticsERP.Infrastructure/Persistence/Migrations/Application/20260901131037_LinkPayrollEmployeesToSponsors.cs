using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class LinkPayrollEmployeesToSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "PayrollEmployees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [app].[PayrollEmployees] SET [SponsorId] = '019c18d5-62e1-7000-8000-000000000040' WHERE [SponsorId] IS NULL");

            migrationBuilder.AlterColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "PayrollEmployees",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEmployees_SponsorId",
                schema: "app",
                table: "PayrollEmployees",
                column: "SponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollEmployees_Sponsors_SponsorId",
                schema: "app",
                table: "PayrollEmployees",
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
                name: "FK_PayrollEmployees_Sponsors_SponsorId",
                schema: "app",
                table: "PayrollEmployees");

            migrationBuilder.DropIndex(
                name: "IX_PayrollEmployees_SponsorId",
                schema: "app",
                table: "PayrollEmployees");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "PayrollEmployees");
        }
    }
}
