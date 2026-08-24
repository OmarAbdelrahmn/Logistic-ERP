using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class DetachUsersFromResetEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[identity].[Users]', N'U') IS NOT NULL
                BEGIN
                    UPDATE [identity].[Users]
                    SET [EmployeeId] = NULL
                    WHERE [EmployeeId] IS NOT NULL
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM [app].[Employees]
                          WHERE [Employees].[Id] = [Users].[EmployeeId]
                      );
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted employee identifiers cannot be restored safely.
        }
    }
}
