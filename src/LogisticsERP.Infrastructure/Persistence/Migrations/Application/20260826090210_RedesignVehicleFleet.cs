using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class RedesignVehicleFleet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RiderVehicleAssignments_FleetLocations_EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderVehicleAssignments_FleetLocations_StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAccidents_FleetLocations_LocationId",
                schema: "app",
                table: "VehicleAccidents");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleIssues_FleetLocations_LocationId",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_FleetLocations_CurrentLocationId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.AddColumn<Guid>(
                name: "OperatingCityId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationDescription",
                schema: "app",
                table: "VehicleIssues",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndLocationSnapshot",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartLocationSnapshot",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE assignment
                SET StartLocationSnapshot = CONCAT(location.NameAr, N' / ', location.NameEn)
                FROM app.RiderVehicleAssignments assignment
                INNER JOIN app.FleetLocations location ON assignment.StartLocationId = location.Id;

                UPDATE assignment
                SET EndLocationSnapshot = CONCAT(location.NameAr, N' / ', location.NameEn)
                FROM app.RiderVehicleAssignments assignment
                INNER JOIN app.FleetLocations location ON assignment.EndLocationId = location.Id;

                UPDATE issue
                SET LocationDescription = CONCAT(location.NameAr, N' / ', location.NameEn)
                FROM app.VehicleIssues issue
                INNER JOIN app.FleetLocations location ON issue.LocationId = location.Id;

                UPDATE accident
                SET LocationDescription = COALESCE(NULLIF(accident.LocationDescription, N''), CONCAT(location.NameAr, N' / ', location.NameEn))
                FROM app.VehicleAccidents accident
                INNER JOIN app.FleetLocations location ON accident.LocationId = location.Id;

                UPDATE vehicle
                SET OperatingCityId = operatingCity.Id
                FROM app.Vehicles vehicle
                INNER JOIN app.FleetLocations location ON vehicle.CurrentLocationId = location.Id
                INNER JOIN app.Housing housing ON location.HousingId = housing.Id
                INNER JOIN app.OperatingCities operatingCity ON operatingCity.GlobalCityId = housing.CityId;
                """);

            migrationBuilder.DropTable(
                name: "FleetLocations",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CurrentLocationId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CurrentOperationalStatus_CurrentLocationId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_VehicleIssues_LocationId",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropIndex(
                name: "IX_VehicleAccidents_LocationId",
                schema: "app",
                table: "VehicleAccidents");

            migrationBuilder.DropIndex(
                name: "IX_RiderVehicleAssignments_EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RiderVehicleAssignments_StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "app",
                table: "VehicleAccidents");

            migrationBuilder.DropColumn(
                name: "EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "CurrentLocationId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.AddColumn<Guid>(
                name: "SponsorId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "Category",
                schema: "app",
                table: "VehicleAttachments",
                newName: "Kind");

            migrationBuilder.Sql(
                """
                ;WITH RankedRegistrations AS
                (
                    SELECT attachment.Id,
                           ROW_NUMBER() OVER (PARTITION BY attachment.VehicleId ORDER BY version.UploadedAtUtc DESC, attachment.CreatedAtUtc DESC, attachment.Id DESC) AS Position
                    FROM app.VehicleAttachments attachment
                    LEFT JOIN app.VehicleAttachmentVersions version ON attachment.CurrentVersionId = version.Id
                    WHERE attachment.Kind = 1 AND attachment.IsDeleted = 0
                )
                UPDATE attachment
                SET Kind = CASE WHEN ranked.Position = 1 THEN 1 ELSE 99 END,
                    DisplayName = CASE WHEN ranked.Position = 1 THEN N'الاستمارة' ELSE attachment.DisplayName END
                FROM app.VehicleAttachments attachment
                LEFT JOIN RankedRegistrations ranked ON attachment.Id = ranked.Id;
                """);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedChassisNumber",
                schema: "app",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedSerialNumber",
                schema: "app",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegistrationType",
                schema: "app",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                schema: "app",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE app.Vehicles
                SET NormalizedChassisNumber = NULLIF(
                    TRANSLATE(UPPER(REPLACE(REPLACE(REPLACE(REPLACE(ChassisNumber, N' ', N''), N'-', N''), N'_', N''), N'/', N'')), N'٠١٢٣٤٥٦٧٨٩', N'0123456789'), N'')
                WHERE ChassisNumber IS NOT NULL;

                ;WITH Duplicates AS
                (
                    SELECT NormalizedChassisNumber
                    FROM app.Vehicles
                    WHERE NormalizedChassisNumber IS NOT NULL AND IsDeleted = 0
                    GROUP BY NormalizedChassisNumber
                    HAVING COUNT(*) > 1
                )
                UPDATE vehicle
                SET NormalizedChassisNumber = NULL
                FROM app.Vehicles vehicle
                INNER JOIN Duplicates duplicate ON vehicle.NormalizedChassisNumber = duplicate.NormalizedChassisNumber;
                """);

            migrationBuilder.CreateTable(
                name: "VehicleIdentityCorrections",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentVersionReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleIdentityCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleIdentityCorrections_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleRegistrationTransitions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromType = table.Column<int>(type: "int", nullable: false),
                    ToType = table.Column<int>(type: "int", nullable: false),
                    OldPlateNumberAr = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OldPlateNumberEn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NewPlateNumberAr = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NewPlateNumberEn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OldPlateLettersAr = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    OldPlateLettersEn = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    OldPlateDigits = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    NewPlateLettersAr = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    NewPlateLettersEn = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    NewPlateDigits = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IstimaraVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationCardVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRegistrationTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleRegistrationTransitions_VehicleAttachmentVersions_IstimaraVersionId",
                        column: x => x.IstimaraVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAttachmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleRegistrationTransitions_VehicleAttachmentVersions_OperationCardVersionId",
                        column: x => x.OperationCardVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAttachmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleRegistrationTransitions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleSuppliers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CommercialRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressBuildingNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressDistrict = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AddressAdditionalNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VehicleSuppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiderPromissoryFiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_RiderPromissoryFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderPromissoryFiles_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderPromissoryFileVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderPromissoryFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderPromissoryFileVersions", x => x.Id);
                    table.CheckConstraint("CK_RiderPromissoryFileVersions_Size", "[FileSizeBytes] > 0");
                    table.CheckConstraint("CK_RiderPromissoryFileVersions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_RiderPromissoryFileVersions_RiderPromissoryFileVersions_SupersededVersionId",
                        column: x => x.SupersededVersionId,
                        principalSchema: "app",
                        principalTable: "RiderPromissoryFileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderPromissoryFileVersions_RiderPromissoryFiles_RiderPromissoryFileId",
                        column: x => x.RiderPromissoryFileId,
                        principalSchema: "app",
                        principalTable: "RiderPromissoryFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderVehicleAssignmentPromissoryFiles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderPromissoryFileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderVehicleAssignmentPromissoryFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignmentPromissoryFiles_RiderPromissoryFileVersions_RiderPromissoryFileVersionId",
                        column: x => x.RiderPromissoryFileVersionId,
                        principalSchema: "app",
                        principalTable: "RiderPromissoryFileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignmentPromissoryFiles_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000056"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "عرض المركبات وهويتها وحالتها.", "View vehicle identity and operational status.", false });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000057"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000060"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000061"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000063"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000064"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000065"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000066"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000067"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000068"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "رفع نسخ ملفات المركبات الثابتة.", "Upload fixed vehicle file versions.", false });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000069"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000070"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000071"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000073"),
                column: "RequiresHousingScope",
                value: false);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000074"),
                columns: new[] { "DescriptionAr", "DescriptionEn" },
                values: new object[] { "تنفيذ تصحيحات هوية المركبة والعداد والحالة عالية الثقة.", "Perform high-trust vehicle identity, odometer, and status corrections." });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[] { new Guid("019c18d5-62e1-7000-a000-000000000082"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تحويل تسجيل المركبة من نقل خاص إلى نقل عام مع حفظ سجل غير قابل للتعديل.", "Convert private-transport registration to public transport with immutable history.", 82, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.registration_transitions.manage", "تحويل تسجيل المركبة", "Manage vehicle registration transitions", null, false, false, null, null, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CurrentOperationalStatus_OperatingCityId",
                schema: "app",
                table: "Vehicles",
                columns: new[] { "CurrentOperationalStatus", "OperatingCityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NormalizedChassisNumber",
                schema: "app",
                table: "Vehicles",
                column: "NormalizedChassisNumber",
                unique: true,
                filter: "[NormalizedChassisNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NormalizedSerialNumber",
                schema: "app",
                table: "Vehicles",
                column: "NormalizedSerialNumber",
                unique: true,
                filter: "[NormalizedSerialNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OperatingCityId",
                schema: "app",
                table: "Vehicles",
                column: "OperatingCityId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles",
                column: "PurchasedFromSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_SponsorId_RegistrationType",
                schema: "app",
                table: "Vehicles",
                columns: new[] { "SponsorId", "RegistrationType" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachments_VehicleId_Kind",
                schema: "app",
                table: "VehicleAttachments",
                columns: new[] { "VehicleId", "Kind" },
                unique: true,
                filter: "[Kind] <> 99 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderPromissoryFiles_CurrentVersionId",
                schema: "app",
                table: "RiderPromissoryFiles",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderPromissoryFiles_IsDeleted",
                schema: "app",
                table: "RiderPromissoryFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderPromissoryFiles_RiderProfileId_IsDeleted",
                schema: "app",
                table: "RiderPromissoryFiles",
                columns: new[] { "RiderProfileId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderPromissoryFileVersions_RiderPromissoryFileId_VersionNumber",
                schema: "app",
                table: "RiderPromissoryFileVersions",
                columns: new[] { "RiderPromissoryFileId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiderPromissoryFileVersions_SupersededVersionId",
                schema: "app",
                table: "RiderPromissoryFileVersions",
                column: "SupersededVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignmentPromissoryFiles_RiderPromissoryFileVersionId",
                schema: "app",
                table: "RiderVehicleAssignmentPromissoryFiles",
                column: "RiderPromissoryFileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignmentPromissoryFiles_RiderVehicleAssignmentId_RiderPromissoryFileVersionId",
                schema: "app",
                table: "RiderVehicleAssignmentPromissoryFiles",
                columns: new[] { "RiderVehicleAssignmentId", "RiderPromissoryFileVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIdentityCorrections_VehicleId_EffectiveAtUtc",
                schema: "app",
                table: "VehicleIdentityCorrections",
                columns: new[] { "VehicleId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrationTransitions_IstimaraVersionId",
                schema: "app",
                table: "VehicleRegistrationTransitions",
                column: "IstimaraVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrationTransitions_OperationCardVersionId",
                schema: "app",
                table: "VehicleRegistrationTransitions",
                column: "OperationCardVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrationTransitions_VehicleId_EffectiveAtUtc",
                schema: "app",
                table: "VehicleRegistrationTransitions",
                columns: new[] { "VehicleId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSuppliers_Code",
                schema: "app",
                table: "VehicleSuppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSuppliers_CommercialRegistrationNumber",
                schema: "app",
                table: "VehicleSuppliers",
                column: "CommercialRegistrationNumber",
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSuppliers_IsDeleted",
                schema: "app",
                table: "VehicleSuppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSuppliers_Status_NameAr",
                schema: "app",
                table: "VehicleSuppliers",
                columns: new[] { "Status", "NameAr" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSuppliers_TaxNumber",
                schema: "app",
                table: "VehicleSuppliers",
                column: "TaxNumber",
                unique: true,
                filter: "[TaxNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_OperatingCities_OperatingCityId",
                schema: "app",
                table: "Vehicles",
                column: "OperatingCityId",
                principalSchema: "app",
                principalTable: "OperatingCities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Sponsors_SponsorId",
                schema: "app",
                table: "Vehicles",
                column: "SponsorId",
                principalSchema: "app",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleSuppliers_PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles",
                column: "PurchasedFromSupplierId",
                principalSchema: "app",
                principalTable: "VehicleSuppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderPromissoryFiles_RiderPromissoryFileVersions_CurrentVersionId",
                schema: "app",
                table: "RiderPromissoryFiles",
                column: "CurrentVersionId",
                principalSchema: "app",
                principalTable: "RiderPromissoryFileVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_OperatingCities_OperatingCityId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Sponsors_SponsorId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleSuppliers_PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_RiderPromissoryFiles_RiderPromissoryFileVersions_CurrentVersionId",
                schema: "app",
                table: "RiderPromissoryFiles");

            migrationBuilder.DropTable(
                name: "RiderVehicleAssignmentPromissoryFiles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleIdentityCorrections",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleRegistrationTransitions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleSuppliers",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderPromissoryFileVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderPromissoryFiles",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CurrentOperationalStatus_OperatingCityId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_NormalizedChassisNumber",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_NormalizedSerialNumber",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_OperatingCityId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_SponsorId_RegistrationType",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_VehicleAttachments_VehicleId_Kind",
                schema: "app",
                table: "VehicleAttachments");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000082"));

            migrationBuilder.DropColumn(
                name: "NormalizedChassisNumber",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "NormalizedSerialNumber",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "OperatingCityId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PurchasedFromSupplierId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegistrationType",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LocationDescription",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropColumn(
                name: "EndLocationSnapshot",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "StartLocationSnapshot",
                schema: "app",
                table: "RiderVehicleAssignments");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                schema: "app",
                table: "Vehicles");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentLocationId",
                schema: "app",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "Kind",
                schema: "app",
                table: "VehicleAttachments",
                newName: "Category");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                schema: "app",
                table: "VehicleIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                schema: "app",
                table: "VehicleAccidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FleetLocations",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    LocationType = table.Column<int>(type: "int", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetLocations", x => x.Id);
                    table.CheckConstraint("CK_FleetLocations_Housing", "([LocationType] = 2 AND [HousingId] IS NOT NULL) OR ([LocationType] <> 2 AND [HousingId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_FleetLocations_Housing_HousingId",
                        column: x => x.HousingId,
                        principalSchema: "app",
                        principalTable: "Housing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000056"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "عرض المركبات ومواقعها وحالتها.", "View vehicles, locations, and operational status.", true });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000057"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000060"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000061"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000063"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000064"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000065"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000066"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000067"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000068"),
                columns: new[] { "DescriptionAr", "DescriptionEn", "RequiresHousingScope" },
                values: new object[] { "رفع وأرشفة نسخ ملفات المركبات.", "Upload and archive vehicle file versions.", true });

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000069"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000070"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000071"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000073"),
                column: "RequiresHousingScope",
                value: true);

            migrationBuilder.UpdateData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000074"),
                columns: new[] { "DescriptionAr", "DescriptionEn" },
                values: new object[] { "تنفيذ تصحيحات العداد والحالة عالية الثقة.", "Perform high-trust odometer and status corrections." });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CurrentLocationId",
                schema: "app",
                table: "Vehicles",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CurrentOperationalStatus_CurrentLocationId",
                schema: "app",
                table: "Vehicles",
                columns: new[] { "CurrentOperationalStatus", "CurrentLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_LocationId",
                schema: "app",
                table: "VehicleIssues",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_LocationId",
                schema: "app",
                table: "VehicleAccidents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EndLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "StartLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetLocations_Code",
                schema: "app",
                table: "FleetLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FleetLocations_HousingId",
                schema: "app",
                table: "FleetLocations",
                column: "HousingId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetLocations_IsDeleted",
                schema: "app",
                table: "FleetLocations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FleetLocations_LocationType_Status",
                schema: "app",
                table: "FleetLocations",
                columns: new[] { "LocationType", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_RiderVehicleAssignments_FleetLocations_EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EndLocationId",
                principalSchema: "app",
                principalTable: "FleetLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderVehicleAssignments_FleetLocations_StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "StartLocationId",
                principalSchema: "app",
                principalTable: "FleetLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAccidents_FleetLocations_LocationId",
                schema: "app",
                table: "VehicleAccidents",
                column: "LocationId",
                principalSchema: "app",
                principalTable: "FleetLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleIssues_FleetLocations_LocationId",
                schema: "app",
                table: "VehicleIssues",
                column: "LocationId",
                principalSchema: "app",
                principalTable: "FleetLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_FleetLocations_CurrentLocationId",
                schema: "app",
                table: "Vehicles",
                column: "CurrentLocationId",
                principalSchema: "app",
                principalTable: "FleetLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
