using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application;

/// <inheritdoc />
public partial class UpgradeHousingToRoomAssignments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_HousingResidencePeriods_Housing_HousingId",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.AddColumn<Guid>(
            name: "RoomId",
            schema: "app",
            table: "HousingResidencePeriods",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "HousingRooms",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Capacity = table.Column<int>(type: "int", nullable: false),
                CurrentOccupancy = table.Column<int>(type: "int", nullable: false),
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
                table.PrimaryKey("PK_HousingRooms", x => x.Id);
                table.CheckConstraint("CK_HousingRooms_Capacity", "[Capacity] > 0");
                table.CheckConstraint(
                    "CK_HousingRooms_CurrentOccupancy",
                    "[CurrentOccupancy] >= 0 AND [CurrentOccupancy] <= [Capacity]");
                table.ForeignKey(
                    name: "FK_HousingRooms_Housing_HousingId",
                    column: x => x.HousingId,
                    principalSchema: "app",
                    principalTable: "Housing",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // Preserve every housing and every historical/current residence period. Existing housing
        // capacity becomes the migrated room capacity, increased only when required to contain
        // already-active residents without data loss.
        migrationBuilder.Sql(
            """
            INSERT INTO [app].[HousingRooms]
                ([Id], [HousingId], [Name], [Capacity], [CurrentOccupancy],
                 [CreatedAtUtc], [CreatedByUserId], [UpdatedAtUtc], [UpdatedByUserId],
                 [IsDeleted], [DeletedAtUtc], [DeletedByUserId], [DeletionReason])
            SELECT
                NEWID(),
                housing.[Id],
                N'Migrated room',
                CASE
                    WHEN housing.[TotalCapacity] > occupancy.[CurrentOccupancy]
                        THEN housing.[TotalCapacity]
                    WHEN occupancy.[CurrentOccupancy] > 0
                        THEN occupancy.[CurrentOccupancy]
                    ELSE 1
                END,
                occupancy.[CurrentOccupancy],
                SYSUTCDATETIME(),
                NULL,
                NULL,
                NULL,
                0,
                NULL,
                NULL,
                NULL
            FROM [app].[Housing] AS housing
            CROSS APPLY
            (
                SELECT COUNT(*) AS [CurrentOccupancy]
                FROM [app].[HousingResidencePeriods] AS residence
                WHERE residence.[HousingId] = housing.[Id]
                  AND residence.[EffectiveTo] IS NULL
            ) AS occupancy;

            UPDATE residence
            SET residence.[RoomId] = room.[Id]
            FROM [app].[HousingResidencePeriods] AS residence
            INNER JOIN [app].[HousingRooms] AS room
                ON room.[HousingId] = residence.[HousingId]
               AND room.[Name] = N'Migrated room';

            IF EXISTS
            (
                SELECT 1
                FROM [app].[HousingResidencePeriods]
                WHERE [RoomId] IS NULL
            )
                THROW 51000, 'Housing room migration aborted: a residence period was not mapped to a room.', 1;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "RoomId",
            schema: "app",
            table: "HousingResidencePeriods",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.DropIndex(
            name: "IX_HousingResidencePeriods_HousingId_EffectiveFrom",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.DropColumn(
            name: "HousingId",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Housing_TotalCapacity",
            schema: "app",
            table: "Housing");

        migrationBuilder.DropColumn(
            name: "TotalCapacity",
            schema: "app",
            table: "Housing");

        migrationBuilder.CreateIndex(
            name: "IX_HousingResidencePeriods_RoomId_EffectiveFrom",
            schema: "app",
            table: "HousingResidencePeriods",
            columns: new[] { "RoomId", "EffectiveFrom" });

        migrationBuilder.CreateIndex(
            name: "IX_HousingRooms_HousingId_IsDeleted",
            schema: "app",
            table: "HousingRooms",
            columns: new[] { "HousingId", "IsDeleted" });

        migrationBuilder.CreateIndex(
            name: "IX_HousingRooms_HousingId_Name",
            schema: "app",
            table: "HousingRooms",
            columns: new[] { "HousingId", "Name" },
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_HousingRooms_IsDeleted",
            schema: "app",
            table: "HousingRooms",
            column: "IsDeleted");

        migrationBuilder.AddForeignKey(
            name: "FK_HousingResidencePeriods_HousingRooms_RoomId",
            schema: "app",
            table: "HousingResidencePeriods",
            column: "RoomId",
            principalSchema: "app",
            principalTable: "HousingRooms",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_HousingResidencePeriods_HousingRooms_RoomId",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.AddColumn<int>(
            name: "TotalCapacity",
            schema: "app",
            table: "Housing",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<Guid>(
            name: "HousingId",
            schema: "app",
            table: "HousingResidencePeriods",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE residence
            SET residence.[HousingId] = room.[HousingId]
            FROM [app].[HousingResidencePeriods] AS residence
            INNER JOIN [app].[HousingRooms] AS room ON room.[Id] = residence.[RoomId];

            IF EXISTS
            (
                SELECT 1
                FROM [app].[HousingResidencePeriods]
                WHERE [HousingId] IS NULL
            )
                THROW 51001, 'Housing room rollback aborted: a residence period was not mapped to housing.', 1;

            UPDATE housing
            SET housing.[TotalCapacity] =
                CASE WHEN totals.[Capacity] > 0 THEN totals.[Capacity] ELSE 1 END
            FROM [app].[Housing] AS housing
            OUTER APPLY
            (
                SELECT COALESCE(SUM(room.[Capacity]), 0) AS [Capacity]
                FROM [app].[HousingRooms] AS room
                WHERE room.[HousingId] = housing.[Id]
                  AND room.[IsDeleted] = 0
            ) AS totals;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "HousingId",
            schema: "app",
            table: "HousingResidencePeriods",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.DropIndex(
            name: "IX_HousingResidencePeriods_RoomId_EffectiveFrom",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.DropColumn(
            name: "RoomId",
            schema: "app",
            table: "HousingResidencePeriods");

        migrationBuilder.DropTable(
            name: "HousingRooms",
            schema: "app");

        migrationBuilder.CreateIndex(
            name: "IX_HousingResidencePeriods_HousingId_EffectiveFrom",
            schema: "app",
            table: "HousingResidencePeriods",
            columns: new[] { "HousingId", "EffectiveFrom" });

        migrationBuilder.AddCheckConstraint(
            name: "CK_Housing_TotalCapacity",
            schema: "app",
            table: "Housing",
            sql: "[TotalCapacity] > 0");

        migrationBuilder.AddForeignKey(
            name: "FK_HousingResidencePeriods_Housing_HousingId",
            schema: "app",
            table: "HousingResidencePeriods",
            column: "HousingId",
            principalSchema: "app",
            principalTable: "Housing",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
