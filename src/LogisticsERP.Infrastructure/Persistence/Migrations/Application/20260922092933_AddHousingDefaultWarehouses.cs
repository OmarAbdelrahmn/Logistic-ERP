using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddHousingDefaultWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HousingWarehouses",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_HousingWarehouses", x => x.Id);
                    table.CheckConstraint("CK_HousingWarehouses_Default", "[IsDefault] = 1");
                    table.ForeignKey(
                        name: "FK_HousingWarehouses_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HousingWarehouseItems",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_HousingWarehouseItems", x => x.Id);
                    table.CheckConstraint("CK_HousingWarehouseItems_Condition", "[Condition] IN (1, 2, 3)");
                    table.CheckConstraint("CK_HousingWarehouseItems_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_HousingWarehouseItems_HousingWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "app",
                        principalTable: "HousingWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouseItems_IsDeleted",
                schema: "app",
                table: "HousingWarehouseItems",
                column: "IsDeleted");

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

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouses_HousingId",
                schema: "app",
                table: "HousingWarehouses",
                column: "HousingId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HousingWarehouses_IsDeleted",
                schema: "app",
                table: "HousingWarehouses",
                column: "IsDeleted");

            migrationBuilder.Sql(
                """
                INSERT INTO [app].[HousingWarehouses]
                    ([Id], [HousingId], [NameAr], [NameEn], [IsDefault], [CreatedAtUtc], [CreatedByUserId], [UpdatedAtUtc], [UpdatedByUserId], [IsDeleted], [DeletedAtUtc], [DeletedByUserId], [DeletionReason])
                SELECT
                    NEWID(), [housing].[Id], N'المستودع الافتراضي', N'Default warehouse', 1, SYSUTCDATETIME(), NULL, NULL, NULL, 0, NULL, NULL, NULL
                FROM [app].[Housing] AS [housing]
                WHERE [housing].[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [app].[HousingWarehouses] AS [warehouse]
                      WHERE [warehouse].[HousingId] = [housing].[Id]
                        AND [warehouse].[IsDeleted] = 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousingWarehouseItems",
                schema: "app");

            migrationBuilder.DropTable(
                name: "HousingWarehouses",
                schema: "app");
        }
    }
}
