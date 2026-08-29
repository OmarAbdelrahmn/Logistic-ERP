using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddVehicleOperationCardCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleOperationCards",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    PreviousRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProofAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_VehicleOperationCards", x => x.Id);
                    table.CheckConstraint("CK_VehicleOperationCards_DateRange", "[ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_VehicleOperationCards_VehicleOperationCards_PreviousRecordId",
                        column: x => x.PreviousRecordId,
                        principalSchema: "app",
                        principalTable: "VehicleOperationCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleOperationCards_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_ExpiryDate_IsCurrent",
                schema: "app",
                table: "VehicleOperationCards",
                columns: new[] { "ExpiryDate", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_IsDeleted",
                schema: "app",
                table: "VehicleOperationCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_PreviousRecordId",
                schema: "app",
                table: "VehicleOperationCards",
                column: "PreviousRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_VehicleId",
                schema: "app",
                table: "VehicleOperationCards",
                column: "VehicleId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationCards_VehicleId_CardNumber",
                schema: "app",
                table: "VehicleOperationCards",
                columns: new[] { "VehicleId", "CardNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleOperationCards",
                schema: "app");
        }
    }
}
