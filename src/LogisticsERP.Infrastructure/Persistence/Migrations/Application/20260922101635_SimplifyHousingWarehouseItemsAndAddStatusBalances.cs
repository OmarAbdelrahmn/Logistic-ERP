using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class SimplifyHousingWarehouseItemsAndAddStatusBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_Code",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_Condition",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HousingWarehouseItems_Condition",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HousingWarehouseItems_Quantity",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.CreateTable(
                name: "HousingWarehouseItemBalances",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
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
                    table.PrimaryKey("PK_HousingWarehouseItemBalances", x => x.Id);
                    table.CheckConstraint("CK_HousingWarehouseItemBalances_Quantity", "[Quantity] >= 0");
                    table.CheckConstraint("CK_HousingWarehouseItemBalances_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_HousingWarehouseItemBalances_HousingWarehouseItems_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "app",
                        principalTable: "HousingWarehouseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [app].[HousingWarehouseItemBalances]
                    ([Id], [ItemId], [Status], [Quantity], [CreatedAtUtc], [CreatedByUserId], [UpdatedAtUtc], [UpdatedByUserId], [IsDeleted], [DeletedAtUtc], [DeletedByUserId], [DeletionReason])
                SELECT
                    NEWID(), [Id], [Condition], [Quantity], [CreatedAtUtc], [CreatedByUserId], [UpdatedAtUtc], [UpdatedByUserId], [IsDeleted], [DeletedAtUtc], [DeletedByUserId], [DeletionReason]
                FROM [app].[HousingWarehouseItems];
                """);

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropColumn(
                name: "Condition",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropColumn(
                name: "NameEn",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_NameAr",
                schema: "app",
                table: "HousingWarehouseItems",
                columns: new[] { "WarehouseId", "NameAr" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItemBalances_IsDeleted",
                schema: "app",
                table: "HousingWarehouseItemBalances",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItemBalances_ItemId_Status",
                schema: "app",
                table: "HousingWarehouseItemBalances",
                columns: new[] { "ItemId", "Status" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousingWarehouseItemBalances",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_NameAr",
                schema: "app",
                table: "HousingWarehouseItems");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "app",
                table: "HousingWarehouseItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                schema: "app",
                table: "HousingWarehouseItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                schema: "app",
                table: "HousingWarehouseItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                schema: "app",
                table: "HousingWarehouseItems",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                schema: "app",
                table: "HousingWarehouseItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_Code",
                schema: "app",
                table: "HousingWarehouseItems",
                columns: new[] { "WarehouseId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItems_WarehouseId_Condition",
                schema: "app",
                table: "HousingWarehouseItems",
                columns: new[] { "WarehouseId", "Condition" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_HousingWarehouseItems_Condition",
                schema: "app",
                table: "HousingWarehouseItems",
                sql: "[Condition] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HousingWarehouseItems_Quantity",
                schema: "app",
                table: "HousingWarehouseItems",
                sql: "[Quantity] >= 0");
        }
    }
}
