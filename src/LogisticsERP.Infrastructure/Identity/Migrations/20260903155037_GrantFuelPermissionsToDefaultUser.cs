using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class GrantFuelPermissionsToDefaultUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @OmarUserId uniqueidentifier;
                DECLARE @NowUtc datetimeoffset = SYSUTCDATETIME();
                DECLARE @InsertedCount int = 0;

                SELECT @OmarUserId = [Id]
                FROM [identity].[Users]
                WHERE [NormalizedUserName] = N'OMAR'
                    OR UPPER([UserName]) = N'OMAR';

                IF @OmarUserId IS NOT NULL
                BEGIN
                    INSERT INTO [identity].[UserDirectPermissionAssignments]
                    (
                        [Id], [UserId], [PermissionKey], [Effect], [StartsAtUtc], [ExpiresAtUtc],
                        [GrantedByUserId], [GrantReason], [IsAllHousingScope], [IsAllClientScope],
                        [IncludesFuturePlatformContracts], [CreatedAtUtc], [CreatedByUserId],
                        [UpdatedAtUtc], [UpdatedByUserId], [IsDeleted], [DeletedAtUtc],
                        [DeletedByUserId], [DeletionReason]
                    )
                    SELECT
                        source.[Id], @OmarUserId, source.[PermissionKey], 1, @NowUtc, NULL,
                        @OmarUserId, N'Direct fuel access grant for user omar.', 1, 1,
                        1, @NowUtc, @OmarUserId, NULL, NULL, 0, NULL, NULL, NULL
                    FROM (VALUES
                        (CAST('019c18d5-62e1-7000-e000-000000000001' AS uniqueidentifier), N'fuel.read'),
                        (CAST('019c18d5-62e1-7000-e000-000000000002' AS uniqueidentifier), N'fuel.manage'),
                        (CAST('019c18d5-62e1-7000-e000-000000000003' AS uniqueidentifier), N'fuel.import')
                    ) source([Id], [PermissionKey])
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM [identity].[UserDirectPermissionAssignments] existing
                        WHERE existing.[UserId] = @OmarUserId
                            AND existing.[PermissionKey] = source.[PermissionKey]
                            AND existing.[Effect] = 1
                            AND existing.[ExpiresAtUtc] IS NULL
                            AND existing.[IsDeleted] = 0
                            AND existing.[IsAllHousingScope] = 1
                            AND existing.[IsAllClientScope] = 1
                            AND existing.[IncludesFuturePlatformContracts] = 1)
                        AND NOT EXISTS (
                            SELECT 1
                            FROM [identity].[UserDirectPermissionAssignments] existing
                            WHERE existing.[Id] = source.[Id]);

                    SET @InsertedCount = @@ROWCOUNT;

                    IF @InsertedCount > 0
                    BEGIN
                        UPDATE [identity].[Users]
                        SET [AuthorizationVersion] = [AuthorizationVersion] + 1
                        WHERE [Id] = @OmarUserId;
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @OmarUserId uniqueidentifier;

                SELECT TOP (1) @OmarUserId = [UserId]
                FROM [identity].[UserDirectPermissionAssignments]
                WHERE [Id] IN
                (
                    '019c18d5-62e1-7000-e000-000000000001',
                    '019c18d5-62e1-7000-e000-000000000002',
                    '019c18d5-62e1-7000-e000-000000000003'
                );

                DELETE FROM [identity].[UserDirectPermissionAssignments]
                WHERE [Id] IN
                (
                    '019c18d5-62e1-7000-e000-000000000001',
                    '019c18d5-62e1-7000-e000-000000000002',
                    '019c18d5-62e1-7000-e000-000000000003'
                );

                IF @@ROWCOUNT > 0
                BEGIN
                    UPDATE [identity].[Users]
                    SET [AuthorizationVersion] = [AuthorizationVersion] + 1
                    WHERE [Id] = @OmarUserId;
                END
                """);
        }
    }
}
