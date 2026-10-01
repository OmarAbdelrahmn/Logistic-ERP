using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHousingFloorsEquipmentExternalOccupants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FloorId",
                schema: "app",
                table: "HousingRooms",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "app",
                table: "HousingRooms",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HousingExternalOccupants",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_HousingExternalOccupants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HousingExternalOccupants_HousingRooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "HousingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingFloors",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_HousingFloors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HousingFloors_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingEquipment",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_HousingEquipment", x => x.Id);
                    table.CheckConstraint("CK_HousingEquipment_Quantity", "[Quantity] >= 0");
                    table.CheckConstraint("CK_HousingEquipment_Scope", "([FloorId] IS NULL AND [RoomId] IS NOT NULL) OR ([FloorId] IS NOT NULL AND [RoomId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_HousingEquipment_HousingFloors_FloorId",
                        column: x => x.FloorId,
                        principalSchema: "app",
                        principalTable: "HousingFloors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HousingEquipment_HousingRooms_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "app",
                        principalTable: "HousingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Give every pre-existing housing one floor and preserve all existing room IDs.
            migrationBuilder.Sql("""
                INSERT INTO [app].[HousingFloors]
                    ([Id], [HousingId], [Name], [CreatedAtUtc], [IsDeleted])
                SELECT NEWID(), h.[Id], N'1', SYSUTCDATETIME(), 0
                FROM [app].[Housing] h
                WHERE NOT EXISTS (SELECT 1 FROM [app].[HousingFloors] f WHERE f.[HousingId] = h.[Id]);

                UPDATE r SET [FloorId] = f.[Id]
                FROM [app].[HousingRooms] r
                JOIN [app].[HousingFloors] f ON f.[HousingId] = r.[HousingId] AND f.[Name] = N'1';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "FloorId",
                schema: "app",
                table: "HousingRooms",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HousingRooms_FloorId",
                schema: "app",
                table: "HousingRooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingEquipment_FloorId_Name",
                schema: "app",
                table: "HousingEquipment",
                columns: new[] { "FloorId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [FloorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HousingEquipment_IsDeleted",
                schema: "app",
                table: "HousingEquipment",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HousingEquipment_RoomId_Name",
                schema: "app",
                table: "HousingEquipment",
                columns: new[] { "RoomId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [RoomId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HousingExternalOccupants_IsDeleted",
                schema: "app",
                table: "HousingExternalOccupants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HousingExternalOccupants_RoomId",
                schema: "app",
                table: "HousingExternalOccupants",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingFloors_HousingId_Name",
                schema: "app",
                table: "HousingFloors",
                columns: new[] { "HousingId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HousingFloors_IsDeleted",
                schema: "app",
                table: "HousingFloors",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_HousingRooms_HousingFloors_FloorId",
                schema: "app",
                table: "HousingRooms",
                column: "FloorId",
                principalSchema: "app",
                principalTable: "HousingFloors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HousingRooms_HousingFloors_FloorId",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.DropTable(
                name: "HousingEquipment",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingExternalOccupants",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingFloors",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_HousingRooms_FloorId",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.DropColumn(
                name: "FloorId",
                schema: "app",
                table: "HousingRooms");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "app",
                table: "HousingRooms");
        }
    }
}
