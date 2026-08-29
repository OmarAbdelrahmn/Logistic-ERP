using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveUserSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ;WITH RankedOpenSessions AS
                (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY [UserId]
                            ORDER BY [LastUsedAtUtc] DESC, [CreatedAtUtc] DESC, [Id] DESC
                        ) AS [SessionRank]
                    FROM [identity].[UserSessions]
                    WHERE [RevokedAtUtc] IS NULL AND [IsDeleted] = 0
                )
                UPDATE [session]
                SET
                    [RevokedAtUtc] = SYSUTCDATETIME(),
                    [RevocationReason] = N'Revoked while enabling the single-active-session policy.',
                    [UpdatedAtUtc] = SYSUTCDATETIME()
                FROM [identity].[UserSessions] AS [session]
                INNER JOIN RankedOpenSessions AS [ranked] ON [ranked].[Id] = [session].[Id]
                WHERE [ranked].[SessionRank] > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_UserSessions_OneOpenSessionPerUser",
                schema: "identity",
                table: "UserSessions",
                column: "UserId",
                unique: true,
                filter: "[RevokedAtUtc] IS NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_UserSessions_OneOpenSessionPerUser",
                schema: "identity",
                table: "UserSessions");
        }
    }
}
