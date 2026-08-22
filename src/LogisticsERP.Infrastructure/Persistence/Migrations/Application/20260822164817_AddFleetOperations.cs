using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddFleetOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {migrationBuilder.CreateTable(
                name: "FleetCommandReceipts",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ResultEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetCommandReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetLocations",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationType = table.Column<int>(type: "int", nullable: false),
                    HousingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "VehicleManufacturers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VehicleManufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleModels",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleManufacturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    DefaultFuelType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_VehicleModels", x => x.Id);
                    table.UniqueConstraint("AK_VehicleModels_Id_VehicleManufacturerId", x => new { x.Id, x.VehicleManufacturerId });
                    table.ForeignKey(
                        name: "FK_VehicleModels_VehicleManufacturers_VehicleManufacturerId",
                        column: x => x.VehicleManufacturerId,
                        principalSchema: "app",
                        principalTable: "VehicleManufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NormalizedAssetNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlateNumberAr = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NormalizedPlateNumberAr = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PlateNumberEn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NormalizedPlateNumberEn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PlateLettersAr = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    PlateLettersEn = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    PlateDigits = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Vin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChassisNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EngineNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VehicleManufacturerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: true),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<int>(type: "int", nullable: false),
                    TransmissionType = table.Column<int>(type: "int", nullable: false),
                    ColorAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ColorEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnershipType = table.Column<int>(type: "int", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LeaseReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentOdometer = table.Column<long>(type: "bigint", nullable: false),
                    LastOdometerAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CurrentOperationalStatus = table.Column<int>(type: "int", nullable: false),
                    CurrentAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecommissionedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecommissionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.CheckConstraint("CK_Vehicles_ModelYear", "[ModelYear] IS NULL OR ([ModelYear] >= 1950 AND [ModelYear] <= 2200)");
                    table.CheckConstraint("CK_Vehicles_Odometer", "[CurrentOdometer] >= 0");
                    table.ForeignKey(
                        name: "FK_Vehicles_FleetLocations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalSchema: "app",
                        principalTable: "FleetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleManufacturers_VehicleManufacturerId",
                        column: x => x.VehicleManufacturerId,
                        principalSchema: "app",
                        principalTable: "VehicleManufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleModels_VehicleModelId_VehicleManufacturerId",
                        columns: x => new { x.VehicleModelId, x.VehicleManufacturerId },
                        principalSchema: "app",
                        principalTable: "VehicleModels",
                        principalColumns: new[] { "Id", "VehicleManufacturerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderVehicleAssignments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartOdometer = table.Column<long>(type: "bigint", nullable: false),
                    StartVehicleCondition = table.Column<int>(type: "int", nullable: false),
                    StartFuelLevelPercentage = table.Column<byte>(type: "tinyint", nullable: true),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EndOdometer = table.Column<long>(type: "bigint", nullable: true),
                    EndVehicleCondition = table.Column<int>(type: "int", nullable: true),
                    EndFuelLevelPercentage = table.Column<byte>(type: "tinyint", nullable: true),
                    PermissionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PermissionStartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PermissionEndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CompletionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WasBackdated = table.Column<bool>(type: "bit", nullable: false),
                    BackdatedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CorrectionOfAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_RiderVehicleAssignments", x => x.Id);
                    table.CheckConstraint("CK_RiderVehicleAssignments_Backdated", "[WasBackdated] = 0 OR [BackdatedReason] IS NOT NULL");
                    table.CheckConstraint("CK_RiderVehicleAssignments_EndFuel", "[EndFuelLevelPercentage] IS NULL OR [EndFuelLevelPercentage] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_RiderVehicleAssignments_Odometer", "[StartOdometer] >= 0 AND ([EndOdometer] IS NULL OR [EndOdometer] >= [StartOdometer] OR [CorrectionReason] IS NOT NULL)");
                    table.CheckConstraint("CK_RiderVehicleAssignments_Permission", "[PermissionEndsOn] IS NULL OR [PermissionStartsOn] IS NULL OR [PermissionEndsOn] >= [PermissionStartsOn]");
                    table.CheckConstraint("CK_RiderVehicleAssignments_StartFuel", "[StartFuelLevelPercentage] IS NULL OR [StartFuelLevelPercentage] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_RiderVehicleAssignments_TimeRange", "[EndedAtUtc] IS NULL OR [EndedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_FleetLocations_EndLocationId",
                        column: x => x.EndLocationId,
                        principalSchema: "app",
                        principalTable: "FleetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_FleetLocations_StartLocationId",
                        column: x => x.StartLocationId,
                        principalSchema: "app",
                        principalTable: "FleetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_RiderProfiles_RiderProfileId_EmployeeId",
                        columns: x => new { x.RiderProfileId, x.EmployeeId },
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumns: new[] { "Id", "EmployeeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_RiderVehicleAssignments_CorrectionOfAssignmentId",
                        column: x => x.CorrectionOfAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_RiderVehicleAssignments_PreviousAssignmentId",
                        column: x => x.PreviousAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleInsurancePolicies",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CoverageType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClaimReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClaimContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_VehicleInsurancePolicies", x => x.Id);
                    table.CheckConstraint("CK_VehicleInsurancePolicies_DateRange", "[ExpiryDate] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_VehicleInsurancePolicies_VehicleInsurancePolicies_PreviousRecordId",
                        column: x => x.PreviousRecordId,
                        principalSchema: "app",
                        principalTable: "VehicleInsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleInsurancePolicies_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleOdometerReadings",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reading = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvidenceAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCorrection = table.Column<bool>(type: "bit", nullable: false),
                    CorrectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleOdometerReadings", x => x.Id);
                    table.CheckConstraint("CK_VehicleOdometerReadings_Correction", "[IsCorrection] = 0 OR [CorrectionReason] IS NOT NULL");
                    table.CheckConstraint("CK_VehicleOdometerReadings_Value", "[Reading] >= 0");
                    table.ForeignKey(
                        name: "FK_VehicleOdometerReadings_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleOperationalStatusPeriods",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_VehicleOperationalStatusPeriods", x => x.Id);
                    table.CheckConstraint("CK_VehicleStatusPeriods_Range", "[EffectiveToUtc] IS NULL OR [EffectiveToUtc] >= [EffectiveFromUtc]");
                    table.ForeignKey(
                        name: "FK_VehicleOperationalStatusPeriods_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehiclePeriodicInspections",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InspectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Odometer = table.Column<long>(type: "bigint", nullable: true),
                    FailureNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_VehiclePeriodicInspections", x => x.Id);
                    table.CheckConstraint("CK_VehiclePeriodicInspections_DateRange", "[ExpiryDate] >= [InspectionDate]");
                    table.CheckConstraint("CK_VehiclePeriodicInspections_Odometer", "[Odometer] IS NULL OR [Odometer] >= 0");
                    table.ForeignKey(
                        name: "FK_VehiclePeriodicInspections_VehiclePeriodicInspections_PreviousRecordId",
                        column: x => x.PreviousRecordId,
                        principalSchema: "app",
                        principalTable: "VehiclePeriodicInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePeriodicInspections_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleRegistrations",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_VehicleRegistrations", x => x.Id);
                    table.CheckConstraint("CK_VehicleRegistrations_DateRange", "[ExpiryDate] >= [IssueDate]");
                    table.ForeignKey(
                        name: "FK_VehicleRegistrations_VehicleRegistrations_PreviousRecordId",
                        column: x => x.PreviousRecordId,
                        principalSchema: "app",
                        principalTable: "VehicleRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleRegistrations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderVehicleAssignmentEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangeSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderVehicleAssignmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderVehicleAssignmentEvents_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleIssues",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OdometerAtReport = table.Column<long>(type: "bigint", nullable: true),
                    RelatedAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlocksOperation = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VehicleIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleIssues_FleetLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "app",
                        principalTable: "FleetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleIssues_RiderVehicleAssignments_RelatedAssignmentId",
                        column: x => x.RelatedAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleIssues_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleIssueEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleIssueEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleIssueEvents_VehicleIssues_VehicleIssueId",
                        column: x => x.VehicleIssueId,
                        principalSchema: "app",
                        principalTable: "VehicleIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAccidentAttachments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAccidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_VehicleAccidentAttachments", x => x.Id);
                    table.CheckConstraint("CK_VehicleAccidentAttachments_Size", "[FileSizeBytes] > 0");
                });

            migrationBuilder.CreateTable(
                name: "VehicleAccidentEvents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAccidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAccidentEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAccidentReportVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAccidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupersedesReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAccidentReportVersions", x => x.Id);
                    table.CheckConstraint("CK_VehicleAccidentReportVersions_Size", "[FileSizeBytes] > 0");
                    table.CheckConstraint("CK_VehicleAccidentReportVersions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleAccidentReportVersions_VehicleAccidentReportVersions_SupersedesReportVersionId",
                        column: x => x.SupersedesReportVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentReportVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAccidents",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccidentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleInsurancePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PoliceReportNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InsuranceClaimNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsDrivable = table.Column<bool>(type: "bit", nullable: false),
                    HasInjuries = table.Column<bool>(type: "bit", nullable: false),
                    InjuryDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ThirdPartyDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DamageDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FaultAssessment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Narrative = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_VehicleAccidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "app",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_FleetLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "app",
                        principalTable: "FleetLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_RiderProfiles_RiderProfileId_EmployeeId",
                        columns: x => new { x.RiderProfileId, x.EmployeeId },
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumns: new[] { "Id", "EmployeeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_VehicleAccidentReportVersions_CurrentReportVersionId",
                        column: x => x.CurrentReportVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAccidentReportVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_VehicleInsurancePolicies_VehicleInsurancePolicyId",
                        column: x => x.VehicleInsurancePolicyId,
                        principalSchema: "app",
                        principalTable: "VehicleInsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_VehicleIssues_VehicleIssueId",
                        column: x => x.VehicleIssueId,
                        principalSchema: "app",
                        principalTable: "VehicleIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAccidents_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAttachments",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_VehicleAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAttachments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAttachmentVersions",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleAttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_VehicleAttachmentVersions", x => x.Id);
                    table.CheckConstraint("CK_VehicleAttachmentVersions_Size", "[FileSizeBytes] > 0");
                    table.CheckConstraint("CK_VehicleAttachmentVersions_Version", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_VehicleAttachmentVersions_VehicleAttachmentVersions_SupersededVersionId",
                        column: x => x.SupersededVersionId,
                        principalSchema: "app",
                        principalTable: "VehicleAttachmentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAttachmentVersions_VehicleAttachments_VehicleAttachmentId",
                        column: x => x.VehicleAttachmentId,
                        principalSchema: "app",
                        principalTable: "VehicleAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000056"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض المركبات ومواقعها وحالتها.", "View vehicles, locations, and operational status.", 56, null, false, false, false, false, "fleet.vehicles.read", "عرض المركبات", "Read vehicles", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000057"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنشاء وتعديل المركبات وحالتها التشغيلية.", "Create and update vehicles and their operational status.", 57, null, false, false, false, false, "fleet.vehicles.manage", "إدارة المركبات", "Manage vehicles", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000058"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "أرشفة واستعادة المركبات غير المستخدمة.", "Archive and restore unused vehicles.", 58, "HIGH_TRUST_ONLY", false, false, true, false, "fleet.vehicles.archive", "أرشفة المركبات", "Archive vehicles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000059"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إنهاء خدمة مركبة بشكل تشغيلي نهائي.", "Operationally decommission a vehicle.", 59, "HIGH_TRUST_ONLY", false, false, true, false, "fleet.vehicles.decommission", "إنهاء خدمة المركبات", "Decommission vehicles", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000060"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض العهد الحالية والتاريخية بين الرايدرز والمركبات.", "View current and historical rider-vehicle assignments.", 60, null, false, false, false, false, "fleet.assignments.read", "عرض عهد المركبات", "Read vehicle assignments", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000061"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنفيذ الاستلام والإرجاع والتبديل وتجديد التصريح.", "Execute take, return, switch, and permission renewal.", 61, null, false, false, false, false, "fleet.assignments.manage", "إدارة عهد المركبات", "Manage vehicle assignments", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000062"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصحيح العهد التاريخية مع سبب إلزامي.", "Correct historical assignments with a mandatory reason.", 62, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.assignments.correct", "تصحيح عهد المركبات", "Correct vehicle assignments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000063"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بلاغات وأعطال المركبات.", "View vehicle issues and faults.", 63, null, false, false, false, false, "fleet.issues.read", "عرض بلاغات المركبات", "Read vehicle issues", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000064"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل ومراجعة وحل وإغلاق البلاغات.", "Report, review, resolve, and close vehicle issues.", 64, null, false, false, false, false, "fleet.issues.manage", "إدارة بلاغات المركبات", "Manage vehicle issues", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000065"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض التسجيل والتأمين والفحص الدوري.", "View vehicle registration, insurance, and inspection.", 65, null, false, false, false, false, "fleet.compliance.read", "عرض التزام المركبات", "Read vehicle compliance", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000066"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إضافة وتجديد وثائق التزام المركبات.", "Add and renew vehicle compliance records.", 66, null, false, false, false, false, "fleet.compliance.manage", "إدارة التزام المركبات", "Manage vehicle compliance", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000067"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات ونسخ ملفات المركبات.", "View vehicle file metadata and versions.", 67, "SENSITIVE_DATA", false, false, false, true, "fleet.files.read", "عرض ملفات المركبات", "Read vehicle files", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000068"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "رفع وأرشفة نسخ ملفات المركبات.", "Upload and archive vehicle file versions.", 68, "SENSITIVE_DATA", false, false, false, true, "fleet.files.upload", "رفع ملفات المركبات", "Upload vehicle files", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000069"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنزيل محتوى ملفات المركبات الخاصة.", "Download private vehicle file content.", 69, "SENSITIVE_DATA", false, false, false, true, "fleet.files.download", "تنزيل ملفات المركبات", "Download vehicle files", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000070"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض بيانات الحوادث والأدلة.", "View vehicle accidents and evidence.", 70, "SENSITIVE_DATA", false, false, false, true, "fleet.accidents.read", "عرض حوادث المركبات", "Read vehicle accidents", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000071"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل حادث مرتبط برايدر وعهدة فعالة.", "Report an accident linked to a rider and active assignment.", 71, "SENSITIVE_DATA", false, false, false, true, "fleet.accidents.report", "تسجيل حوادث المركبات", "Report vehicle accidents", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000072"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "اعتماد وتصحيح وإغلاق تقارير الحوادث.", "Finalize, correct, and close accident reports.", 72, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.accidents.finalize", "اعتماد تقارير الحوادث", "Finalize accident reports", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000073"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنزيل الأدلة وتقارير الحوادث الخاصة.", "Download private accident evidence and reports.", 73, "SENSITIVE_DATA", false, false, false, true, "fleet.accidents.download", "تنزيل تقارير الحوادث", "Download accident reports", null, false, true, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000074"), "Fleet", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تنفيذ تصحيحات العداد والحالة عالية الثقة.", "Perform high-trust odometer and status corrections.", 74, "HIGH_TRUST_ONLY", false, false, true, true, "fleet.corrections.manage", "تصحيح بيانات الأسطول", "Manage fleet corrections", null, false, false, null, null, 1 },
                });

            migrationBuilder.CreateIndex(
                name: "IX_FleetCommandReceipts_CommandName_IdempotencyKey",
                schema: "app",
                table: "FleetCommandReceipts",
                columns: new[] { "CommandName", "IdempotencyKey" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignmentEvents_OperationId",
                schema: "app",
                table: "RiderVehicleAssignmentEvents",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignmentEvents_RiderVehicleAssignmentId_OccurredAtUtc",
                schema: "app",
                table: "RiderVehicleAssignmentEvents",
                columns: new[] { "RiderVehicleAssignmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_CorrectionOfAssignmentId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "CorrectionOfAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_EndLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "EndLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_IsDeleted",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_PreviousAssignmentId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "PreviousAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_RiderProfileId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "RiderProfileId",
                unique: true,
                filter: "[EndedAtUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_RiderProfileId_EmployeeId",
                schema: "app",
                table: "RiderVehicleAssignments",
                columns: new[] { "RiderProfileId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_RiderProfileId_StartedAtUtc",
                schema: "app",
                table: "RiderVehicleAssignments",
                columns: new[] { "RiderProfileId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_StartLocationId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "StartLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_VehicleId",
                schema: "app",
                table: "RiderVehicleAssignments",
                column: "VehicleId",
                unique: true,
                filter: "[EndedAtUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderVehicleAssignments_VehicleId_StartedAtUtc",
                schema: "app",
                table: "RiderVehicleAssignments",
                columns: new[] { "VehicleId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentAttachments_IsDeleted",
                schema: "app",
                table: "VehicleAccidentAttachments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentAttachments_VehicleAccidentId_IsDeleted",
                schema: "app",
                table: "VehicleAccidentAttachments",
                columns: new[] { "VehicleAccidentId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentEvents_VehicleAccidentId_OccurredAtUtc",
                schema: "app",
                table: "VehicleAccidentEvents",
                columns: new[] { "VehicleAccidentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentReportVersions_ReportNumber",
                schema: "app",
                table: "VehicleAccidentReportVersions",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentReportVersions_SupersedesReportVersionId",
                schema: "app",
                table: "VehicleAccidentReportVersions",
                column: "SupersedesReportVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidentReportVersions_VehicleAccidentId_VersionNumber",
                schema: "app",
                table: "VehicleAccidentReportVersions",
                columns: new[] { "VehicleAccidentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_AccidentNumber",
                schema: "app",
                table: "VehicleAccidents",
                column: "AccidentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_CurrentReportVersionId",
                schema: "app",
                table: "VehicleAccidents",
                column: "CurrentReportVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_EmployeeId",
                schema: "app",
                table: "VehicleAccidents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_IsDeleted",
                schema: "app",
                table: "VehicleAccidents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_LocationId",
                schema: "app",
                table: "VehicleAccidents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_RiderProfileId_EmployeeId",
                schema: "app",
                table: "VehicleAccidents",
                columns: new[] { "RiderProfileId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_RiderProfileId_OccurredAtUtc",
                schema: "app",
                table: "VehicleAccidents",
                columns: new[] { "RiderProfileId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_RiderVehicleAssignmentId",
                schema: "app",
                table: "VehicleAccidents",
                column: "RiderVehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_VehicleId_OccurredAtUtc",
                schema: "app",
                table: "VehicleAccidents",
                columns: new[] { "VehicleId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_VehicleInsurancePolicyId",
                schema: "app",
                table: "VehicleAccidents",
                column: "VehicleInsurancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAccidents_VehicleIssueId",
                schema: "app",
                table: "VehicleAccidents",
                column: "VehicleIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachments_CurrentVersionId",
                schema: "app",
                table: "VehicleAttachments",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachments_IsDeleted",
                schema: "app",
                table: "VehicleAttachments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachments_VehicleId_IsDeleted",
                schema: "app",
                table: "VehicleAttachments",
                columns: new[] { "VehicleId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachmentVersions_SupersededVersionId",
                schema: "app",
                table: "VehicleAttachmentVersions",
                column: "SupersededVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAttachmentVersions_VehicleAttachmentId_VersionNumber",
                schema: "app",
                table: "VehicleAttachmentVersions",
                columns: new[] { "VehicleAttachmentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInsurancePolicies_ExpiryDate_IsCurrent",
                schema: "app",
                table: "VehicleInsurancePolicies",
                columns: new[] { "ExpiryDate", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInsurancePolicies_IsDeleted",
                schema: "app",
                table: "VehicleInsurancePolicies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInsurancePolicies_PreviousRecordId",
                schema: "app",
                table: "VehicleInsurancePolicies",
                column: "PreviousRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInsurancePolicies_VehicleId",
                schema: "app",
                table: "VehicleInsurancePolicies",
                column: "VehicleId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleInsurancePolicies_VehicleId_PolicyNumber",
                schema: "app",
                table: "VehicleInsurancePolicies",
                columns: new[] { "VehicleId", "PolicyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssueEvents_VehicleIssueId_OccurredAtUtc",
                schema: "app",
                table: "VehicleIssueEvents",
                columns: new[] { "VehicleIssueId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_IsDeleted",
                schema: "app",
                table: "VehicleIssues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_IssueNumber",
                schema: "app",
                table: "VehicleIssues",
                column: "IssueNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_LocationId",
                schema: "app",
                table: "VehicleIssues",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_RelatedAssignmentId",
                schema: "app",
                table: "VehicleIssues",
                column: "RelatedAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleIssues_VehicleId_Status_BlocksOperation",
                schema: "app",
                table: "VehicleIssues",
                columns: new[] { "VehicleId", "Status", "BlocksOperation" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleManufacturers_Code",
                schema: "app",
                table: "VehicleManufacturers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleManufacturers_IsDeleted",
                schema: "app",
                table: "VehicleManufacturers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_IsDeleted",
                schema: "app",
                table: "VehicleModels",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleModels_VehicleManufacturerId_Code",
                schema: "app",
                table: "VehicleModels",
                columns: new[] { "VehicleManufacturerId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOdometerReadings_VehicleId_RecordedAtUtc",
                schema: "app",
                table: "VehicleOdometerReadings",
                columns: new[] { "VehicleId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationalStatusPeriods_IsDeleted",
                schema: "app",
                table: "VehicleOperationalStatusPeriods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationalStatusPeriods_VehicleId",
                schema: "app",
                table: "VehicleOperationalStatusPeriods",
                column: "VehicleId",
                unique: true,
                filter: "[EffectiveToUtc] IS NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleOperationalStatusPeriods_VehicleId_EffectiveFromUtc",
                schema: "app",
                table: "VehicleOperationalStatusPeriods",
                columns: new[] { "VehicleId", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePeriodicInspections_ExpiryDate_IsCurrent",
                schema: "app",
                table: "VehiclePeriodicInspections",
                columns: new[] { "ExpiryDate", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePeriodicInspections_IsDeleted",
                schema: "app",
                table: "VehiclePeriodicInspections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePeriodicInspections_PreviousRecordId",
                schema: "app",
                table: "VehiclePeriodicInspections",
                column: "PreviousRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePeriodicInspections_VehicleId",
                schema: "app",
                table: "VehiclePeriodicInspections",
                column: "VehicleId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePeriodicInspections_VehicleId_InspectionNumber",
                schema: "app",
                table: "VehiclePeriodicInspections",
                columns: new[] { "VehicleId", "InspectionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_ExpiryDate_IsCurrent",
                schema: "app",
                table: "VehicleRegistrations",
                columns: new[] { "ExpiryDate", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_IsDeleted",
                schema: "app",
                table: "VehicleRegistrations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_PreviousRecordId",
                schema: "app",
                table: "VehicleRegistrations",
                column: "PreviousRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleRegistrations_VehicleId",
                schema: "app",
                table: "VehicleRegistrations",
                column: "VehicleId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

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
                name: "IX_Vehicles_IsDeleted",
                schema: "app",
                table: "Vehicles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NormalizedAssetNumber",
                schema: "app",
                table: "Vehicles",
                column: "NormalizedAssetNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NormalizedPlateNumberAr",
                schema: "app",
                table: "Vehicles",
                column: "NormalizedPlateNumberAr",
                unique: true,
                filter: "[NormalizedPlateNumberAr] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_NormalizedPlateNumberEn",
                schema: "app",
                table: "Vehicles",
                column: "NormalizedPlateNumberEn",
                unique: true,
                filter: "[NormalizedPlateNumberEn] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleManufacturerId",
                schema: "app",
                table: "Vehicles",
                column: "VehicleManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleModelId_VehicleManufacturerId",
                schema: "app",
                table: "Vehicles",
                columns: new[] { "VehicleModelId", "VehicleManufacturerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Vin",
                schema: "app",
                table: "Vehicles",
                column: "Vin",
                unique: true,
                filter: "[Vin] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAccidentAttachments_VehicleAccidents_VehicleAccidentId",
                schema: "app",
                table: "VehicleAccidentAttachments",
                column: "VehicleAccidentId",
                principalSchema: "app",
                principalTable: "VehicleAccidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAccidentEvents_VehicleAccidents_VehicleAccidentId",
                schema: "app",
                table: "VehicleAccidentEvents",
                column: "VehicleAccidentId",
                principalSchema: "app",
                principalTable: "VehicleAccidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAccidentReportVersions_VehicleAccidents_VehicleAccidentId",
                schema: "app",
                table: "VehicleAccidentReportVersions",
                column: "VehicleAccidentId",
                principalSchema: "app",
                principalTable: "VehicleAccidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleAttachments_VehicleAttachmentVersions_CurrentVersionId",
                schema: "app",
                table: "VehicleAttachments",
                column: "CurrentVersionId",
                principalSchema: "app",
                principalTable: "VehicleAttachmentVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAccidents_RiderVehicleAssignments_RiderVehicleAssignmentId",
                schema: "app",
                table: "VehicleAccidents");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleIssues_RiderVehicleAssignments_RelatedAssignmentId",
                schema: "app",
                table: "VehicleIssues");

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

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAccidents_Vehicles_VehicleId",
                schema: "app",
                table: "VehicleAccidents");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAttachments_Vehicles_VehicleId",
                schema: "app",
                table: "VehicleAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleInsurancePolicies_Vehicles_VehicleId",
                schema: "app",
                table: "VehicleInsurancePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleIssues_Vehicles_VehicleId",
                schema: "app",
                table: "VehicleIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAccidentReportVersions_VehicleAccidents_VehicleAccidentId",
                schema: "app",
                table: "VehicleAccidentReportVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleAttachments_VehicleAttachmentVersions_CurrentVersionId",
                schema: "app",
                table: "VehicleAttachments");

            migrationBuilder.DropTable(
                name: "FleetCommandReceipts",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderVehicleAssignmentEvents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAccidentAttachments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAccidentEvents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleIssueEvents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleOdometerReadings",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleOperationalStatusPeriods",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehiclePeriodicInspections",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleRegistrations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "RiderVehicleAssignments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FleetLocations",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleModels",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleManufacturers",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAccidents",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAccidentReportVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleInsurancePolicies",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleIssues",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAttachmentVersions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "VehicleAttachments",
                schema: "app");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000056"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000057"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000058"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000059"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000060"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000061"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000062"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000063"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000064"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000065"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000066"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000067"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000068"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000069"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000070"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000071"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000072"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000073"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000074"));}
    }
}
