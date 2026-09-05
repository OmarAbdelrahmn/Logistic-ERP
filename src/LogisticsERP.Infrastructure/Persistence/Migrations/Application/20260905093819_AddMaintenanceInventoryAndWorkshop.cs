using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LogisticsERP.Infrastructure.Persistence.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddMaintenanceInventoryAndWorkshop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maintenance");

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedSku = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    PurchaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    DefaultPackageQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    MinimumStockLevel = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsSerialized = table.Column<bool>(type: "bit", nullable: false),
                    IsLotTracked = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.CheckConstraint("CK_InventoryItems_ItemType", "[ItemType] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_InventoryItems_OilUnit", "[ItemType] <> 3 OR [BaseUnitOfMeasure] = 2");
                    table.CheckConstraint("CK_InventoryItems_Quantities", "([DefaultPackageQuantity] IS NULL OR [DefaultPackageQuantity] > 0) AND [MinimumStockLevel] >= 0 AND [ReorderQuantity] >= 0");
                    table.CheckConstraint("CK_InventoryItems_Status", "[Status] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_InventoryItems_Units", "[BaseUnitOfMeasure] BETWEEN 1 AND 5 AND [PurchaseUnitOfMeasure] BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceLocations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OperatingCityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationType = table.Column<int>(type: "int", nullable: false),
                    AllowsCompanyVehicles = table.Column<bool>(type: "bit", nullable: false),
                    AllowsExternalVehicles = table.Column<bool>(type: "bit", nullable: false),
                    AllowsSparePartSales = table.Column<bool>(type: "bit", nullable: false),
                    AllowsPaidExternalRepairs = table.Column<bool>(type: "bit", nullable: false),
                    InventoryEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MaintenanceLocations", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceLocations_Latitude", "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_MaintenanceLocations_Longitude", "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_MaintenanceLocations_Status", "[Status] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_MaintenanceLocations_Type", "[LocationType] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_MaintenanceLocations_OperatingCities_OperatingCityId",
                        column: x => x.OperatingCityId,
                        principalSchema: "app",
                        principalTable: "OperatingCities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    LegalNameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LegalNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VatNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CommercialRegistrationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTermsDays = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceSuppliers_PaymentTerms", "[PaymentTermsDays] IS NULL OR [PaymentTermsDays] >= 0");
                    table.CheckConstraint("CK_MaintenanceSuppliers_Status", "[Status] BETWEEN 1 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "VehicleExpenses",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    AmountBeforeTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReversalOfExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleExpenses_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleExpenses_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleExpenses_VehicleExpenses_ReversalOfExpenseId",
                        column: x => x.ReversalOfExpenseId,
                        principalSchema: "maintenance",
                        principalTable: "VehicleExpenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleExpenses_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VehicleModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleType = table.Column<int>(type: "int", nullable: true),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: true),
                    IntervalKilometers = table.Column<long>(type: "bigint", nullable: true),
                    ReminderAfterKilometers = table.Column<long>(type: "bigint", nullable: true),
                    MaximumAfterKilometers = table.Column<long>(type: "bigint", nullable: true),
                    AlertDaysBefore = table.Column<int>(type: "int", nullable: true),
                    AlertKilometersBefore = table.Column<long>(type: "bigint", nullable: true),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultOilQuantityLiters = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: true),
                    ChecklistJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.CheckConstraint("CK_MaintenancePlans_Intervals", "([TriggerType] <> 3) OR ([ReminderAfterKilometers] > 0 AND [MaximumAfterKilometers] > [ReminderAfterKilometers])");
                    table.ForeignKey(
                        name: "FK_Plans_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plans_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalSchema: "app",
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLocations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaintenanceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_InventoryLocations", x => x.Id);
                    table.CheckConstraint("CK_InventoryLocations_Status", "[Status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_InventoryLocations_MaintenanceLocations_MaintenanceLocationId",
                        column: x => x.MaintenanceLocationId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenanceLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ServiceSubjectType = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaintenanceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OdometerAtOpen = table.Column<long>(type: "bigint", nullable: true),
                    OdometerAtCompletion = table.Column<long>(type: "bigint", nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WorkPerformed = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    QualityCheckNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedTechnicianUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributedRiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualMaterialCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualLaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualOtherCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualTotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceWorkOrders_Costs", "[EstimatedCost] >= 0 AND [ActualMaterialCost] >= 0 AND [ActualLaborCost] >= 0 AND [ActualOtherCost] >= 0 AND [ActualTotalCost] >= 0");
                    table.CheckConstraint("CK_MaintenanceWorkOrders_Odometers", "([OdometerAtOpen] IS NULL OR [OdometerAtOpen] >= 0) AND ([OdometerAtCompletion] IS NULL OR [OdometerAtCompletion] >= 0)");
                    table.CheckConstraint("CK_MaintenanceWorkOrders_Subject", "([ServiceSubjectType] = 1 AND [VehicleId] IS NOT NULL) OR ([ServiceSubjectType] = 2 AND [VehicleId] IS NULL AND [VehicleIssueId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_WorkOrders_MaintenanceLocations_MaintenanceLocationId",
                        column: x => x.MaintenanceLocationId,
                        principalSchema: "maintenance",
                        principalTable: "MaintenanceLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_RiderProfiles_AttributedRiderProfileId",
                        column: x => x.AttributedRiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_VehicleIssues_VehicleIssueId",
                        column: x => x.VehicleIssueId,
                        principalSchema: "app",
                        principalTable: "VehicleIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBalances",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReportingAverageUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LastMovementAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_StockBalances", x => x.Id);
                    table.CheckConstraint("CK_StockBalances_Quantities", "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand] AND [ReportingAverageUnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockBalances_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalances_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceDocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversalOfMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovements_Type", "[MovementType] BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockMovements_ReversalOfMovementId",
                        column: x => x.ReversalOfMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalCustomerPayments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversalOfPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCustomerPayments", x => x.Id);
                    table.CheckConstraint("CK_ExternalCustomerPayments_Amount", "[ReversalOfPaymentId] IS NOT NULL OR [Amount] > 0");
                    table.ForeignKey(
                        name: "FK_ExternalCustomerPayments_ExternalCustomerPayments_ReversalOfPaymentId",
                        column: x => x.ReversalOfPaymentId,
                        principalSchema: "maintenance",
                        principalTable: "ExternalCustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalCustomerPayments_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFinancialEntries",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AmountBeforeTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MechanicEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalMechanicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReversalOfEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFinancialEntries", x => x.Id);
                    table.CheckConstraint("CK_ExternalFinancialEntries_Reversal", "[ReversalOfEntryId] IS NOT NULL OR ([AmountBeforeTax] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0)");
                    table.ForeignKey(
                        name: "FK_ExternalFinancialEntries_ExternalFinancialEntries_ReversalOfEntryId",
                        column: x => x.ReversalOfEntryId,
                        principalSchema: "maintenance",
                        principalTable: "ExternalFinancialEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalFinancialEntries_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalVehicleSnapshots",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlateOrReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VehicleType = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalVehicleSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalVehicleSnapshots_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaborEntries",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicianUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalTechnicianName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborEntries", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceLaborEntries_Actor", "([TechnicianUserId] IS NOT NULL AND [ExternalTechnicianName] IS NULL) OR ([TechnicianUserId] IS NULL AND [ExternalTechnicianName] IS NOT NULL)");
                    table.CheckConstraint("CK_MaintenanceLaborEntries_Values", "[EndedAtUtc] >= [StartedAtUtc] AND [Hours] >= 0 AND [HourlyRate] >= 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_LaborEntries_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleSchedules",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenancePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastCompletedWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastCompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCompletedOdometer = table.Column<long>(type: "bigint", nullable: true),
                    NextDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ReminderFromOdometer = table.Column<long>(type: "bigint", nullable: true),
                    MaximumDueOdometer = table.Column<long>(type: "bigint", nullable: true),
                    ComputedStatus = table.Column<int>(type: "int", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_VehicleSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleSchedules_Plans_MaintenancePlanId",
                        column: x => x.MaintenancePlanId,
                        principalSchema: "maintenance",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleSchedules_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleSchedules_WorkOrders_LastCompletedWorkOrderId",
                        column: x => x.LastCompletedWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceipts",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryValuationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceipts_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [DiscountAmount] <= [Subtotal] AND [TaxAmount] >= 0 AND [InventoryValuationAmount] >= 0 AND [TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_StockMovements_PostedMovementId",
                        column: x => x.PostedMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "maintenance",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderInventoryIssues",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedFromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PostedMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_RiderInventoryIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssues_InventoryLocations_IssuedFromLocationId",
                        column: x => x.IssuedFromLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssues_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssues_RiderVehicleAssignments_RelatedAssignmentId",
                        column: x => x.RelatedAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssues_StockMovements_PostedMovementId",
                        column: x => x.PostedMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.CheckConstraint("CK_StockTransfers_Locations", "[SourceLocationId] <> [DestinationLocationId]");
                    table.ForeignKey(
                        name: "FK_StockTransfers_InventoryLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_InventoryLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_StockMovements_DestinationMovementId",
                        column: x => x.DestinationMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransfers_StockMovements_SourceMovementId",
                        column: x => x.SourceMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptAttachments",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptAttachments", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceiptAttachments_Size", "[FileSizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptAttachments_PurchaseReceipts_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalSchema: "maintenance",
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnNumber = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PostedMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_PurchaseReceipts_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalSchema: "maintenance",
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_StockMovements_PostedMovementId",
                        column: x => x.PostedMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "maintenance",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLines", x => x.Id);
                    table.CheckConstraint("CK_StockTransferLines_Values", "[Quantity] > 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockTransferLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalSchema: "maintenance",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalPartSaleLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    SellingUnitPriceBeforeTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaintenanceMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PartsGrossProfit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPartSaleLines", x => x.Id);
                    table.CheckConstraint("CK_ExternalPartSaleLines_Values", "[Quantity] > 0 AND [SellingUnitPriceBeforeTax] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [InventoryCost] >= 0");
                    table.ForeignKey(
                        name: "FK_ExternalPartSaleLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalPartSaleLines_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialUsages",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageType = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderVehicleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributionStatus = table.Column<int>(type: "int", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReversalOfUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialUsages", x => x.Id);
                    table.CheckConstraint("CK_MaterialUsages_Values", "[Quantity] > 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_MaterialUsages_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_MaterialUsages_ReversalOfUsageId",
                        column: x => x.ReversalOfUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_RiderProfiles_RiderProfileId",
                        column: x => x.RiderProfileId,
                        principalSchema: "app",
                        principalTable: "RiderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_RiderVehicleAssignments_RiderVehicleAssignmentId",
                        column: x => x.RiderVehicleAssignmentId,
                        principalSchema: "app",
                        principalTable: "RiderVehicleAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "app",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialUsages_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OilChangeOperations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceWorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OdometerAtChange = table.Column<long>(type: "bigint", nullable: false),
                    VehicleTypeSnapshot = table.Column<int>(type: "int", nullable: false),
                    OilInventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OilQuantityLiters = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    OilMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OilCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OilFilterChanged = table.Column<bool>(type: "bit", nullable: false),
                    OilFilterInventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OilFilterMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OilFilterCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilChangeOperations", x => x.Id);
                    table.CheckConstraint("CK_OilChangeOperations_CarQuantity", "[VehicleTypeSnapshot] <> 2 OR ([OilFilterChanged] = 0 AND [OilQuantityLiters] = 3.500) OR ([OilFilterChanged] = 1 AND [OilQuantityLiters] = 4.000)");
                    table.CheckConstraint("CK_OilChangeOperations_Filter", "([OilFilterChanged] = 0 AND [OilFilterInventoryItemId] IS NULL AND [OilFilterMaterialUsageId] IS NULL AND [OilFilterCost] = 0) OR ([OilFilterChanged] = 1 AND [OilFilterInventoryItemId] IS NOT NULL AND [OilFilterMaterialUsageId] IS NOT NULL)");
                    table.CheckConstraint("CK_OilChangeOperations_Values", "[OdometerAtChange] >= 0 AND [OilQuantityLiters] > 0 AND [OilCost] >= 0 AND [OilFilterCost] >= 0 AND [LaborCost] >= 0 AND [OtherCost] >= 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_OilChangeOperations_InventoryItems_OilFilterInventoryItemId",
                        column: x => x.OilFilterInventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeOperations_InventoryItems_OilInventoryItemId",
                        column: x => x.OilInventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeOperations_MaterialUsages_OilFilterMaterialUsageId",
                        column: x => x.OilFilterMaterialUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeOperations_MaterialUsages_OilMaterialUsageId",
                        column: x => x.OilMaterialUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilChangeOperations_WorkOrders_MaintenanceWorkOrderId",
                        column: x => x.MaintenanceWorkOrderId,
                        principalSchema: "maintenance",
                        principalTable: "WorkOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseUnit = table.Column<int>(type: "int", nullable: false),
                    PackageCount = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    DeclaredQuantityPerPackage = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReceivedBaseQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    GrossWeightKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    NetWeightKg = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    PackageUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineSubtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InventoryValuationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StockMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceiptLines_Values", "[PackageCount] > 0 AND [DeclaredQuantityPerPackage] > 0 AND [ReceivedBaseQuantity] > 0 AND ([GrossWeightKg] IS NULL OR [GrossWeightKg] > 0) AND ([NetWeightKg] IS NULL OR [NetWeightKg] > 0) AND [PackageUnitPrice] >= 0 AND [LineSubtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [InventoryValuationAmount] >= 0 AND [BaseUnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalSchema: "maintenance",
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderInventoryIssueLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiderInventoryIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedReturn = table.Column<bool>(type: "bit", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderInventoryIssueLines", x => x.Id);
                    table.CheckConstraint("CK_RiderInventoryIssueLines_Values", "[Quantity] > 0 AND [TotalCost] >= 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssueLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiderInventoryIssueLines_RiderInventoryIssues_RiderInventoryIssueId",
                        column: x => x.RiderInventoryIssueId,
                        principalSchema: "maintenance",
                        principalTable: "RiderInventoryIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCostAllocations",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceMaterialUsageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RiderInventoryIssueLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AllocatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCostAllocations", x => x.Id);
                    table.CheckConstraint("CK_StockCostAllocations_Values", "[AllocatedQuantity] > 0 AND [UnitCost] >= 0 AND [AllocatedCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockCostAllocations_MaterialUsages_MaintenanceMaterialUsageId",
                        column: x => x.MaintenanceMaterialUsageId,
                        principalSchema: "maintenance",
                        principalTable: "MaterialUsages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostAllocations_RiderInventoryIssueLines_RiderInventoryIssueLineId",
                        column: x => x.RiderInventoryIssueLineId,
                        principalSchema: "maintenance",
                        principalTable: "RiderInventoryIssueLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCostLayers",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceMovementLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginalSequence = table.Column<long>(type: "bigint", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    OriginalTotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_StockCostLayers", x => x.Id);
                    table.CheckConstraint("CK_StockCostLayers_Values", "[OriginalQuantity] > 0 AND [RemainingQuantity] >= 0 AND [RemainingQuantity] <= [OriginalQuantity] AND [UnitCost] >= 0 AND [OriginalTotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockCostLayers_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostLayers_InventoryLocations_InventoryLocationId",
                        column: x => x.InventoryLocationId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostLayers_PurchaseReceiptLines_SourceReceiptLineId",
                        column: x => x.SourceReceiptLineId,
                        principalSchema: "maintenance",
                        principalTable: "PurchaseReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCostLayers_StockCostLayers_SourceCostLayerId",
                        column: x => x.SourceCostLayerId,
                        principalSchema: "maintenance",
                        principalTable: "StockCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BaseUnitOfMeasure = table.Column<int>(type: "int", nullable: false),
                    CostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementLines", x => x.Id);
                    table.CheckConstraint("CK_StockMovementLines_Values", "[Quantity] > 0 AND [UnitCost] >= 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_StockMovementLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovementLines_StockCostLayers_CostLayerId",
                        column: x => x.CostLayerId,
                        principalSchema: "maintenance",
                        principalTable: "StockCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovementLines_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalSchema: "maintenance",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLines",
                schema: "maintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCostLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLines", x => x.Id);
                    table.CheckConstraint("CK_SupplierReturnLines_Values", "[Quantity] > 0 AND [UnitCost] >= 0 AND [TotalCost] >= 0");
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalSchema: "maintenance",
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_StockCostLayers_StockCostLayerId",
                        column: x => x.StockCostLayerId,
                        principalSchema: "maintenance",
                        principalTable: "StockCostLayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_SupplierReturns_SupplierReturnId",
                        column: x => x.SupplierReturnId,
                        principalSchema: "maintenance",
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "maintenance",
                table: "MaintenanceLocations",
                columns: new[] { "Id", "Address", "AllowsCompanyVehicles", "AllowsExternalVehicles", "AllowsPaidExternalRepairs", "AllowsSparePartSales", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "InventoryEnabled", "IsDeleted", "Latitude", "LocationType", "Longitude", "NameAr", "NameEn", "Notes", "OperatingCityId", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019d77f0-0000-7000-8000-000000000001"), null, true, false, false, false, "JEDDAH_WAREHOUSE", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, null, 3, null, "مستودع جدة", "Jeddah Warehouse", null, new Guid("019c18d5-62e1-7000-8000-000000000003"), 1, null, null },
                    { new Guid("019d77f0-0000-7000-8000-000000000002"), null, true, true, true, true, "RIYADH_WORKSHOP", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, null, 2, null, "ورشة الرياض", "Riyadh Workshop", null, new Guid("019c18d5-62e1-7000-8000-000000000005"), 1, null, null }
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "PermissionDefinitions",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "DescriptionAr", "DescriptionEn", "DisplayOrder", "GrantabilityRule", "IsDeleted", "IsDeprecated", "IsHighTrust", "IsSensitive", "Key", "NameAr", "NameEn", "ReplacementKey", "RequiresClientScope", "RequiresHousingScope", "UpdatedAtUtc", "UpdatedByUserId", "Version" },
                values: new object[,]
                {
                    { new Guid("019c18d5-62e1-7000-a000-000000000093"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض مستودع جدة وورشة الرياض ونطاقات خدمتهما.", "View maintenance locations and their service scopes.", 93, null, false, false, false, false, "maintenance.locations.read", "عرض مواقع الصيانة", "Read maintenance locations", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000094"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة مواقع الصيانة وربطها بالمدن.", "Manage maintenance locations and city links.", 94, "HIGH_TRUST_ONLY", false, false, true, false, "maintenance.locations.manage", "إدارة مواقع الصيانة", "Manage maintenance locations", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000095"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض أوامر صيانة مركبات الشركة والخارجية.", "View company and external maintenance work orders.", 95, null, false, false, false, false, "maintenance.work_orders.read", "عرض أوامر الصيانة", "Read maintenance work orders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000096"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "فتح وبدء وإكمال وإغلاق أوامر الصيانة وصرف موادها.", "Open, start, complete, close, and post materials to maintenance work orders.", 96, "SENSITIVE_DATA", false, false, false, true, "maintenance.work_orders.manage", "إدارة أوامر الصيانة", "Manage maintenance work orders", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000097"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض عمليات وتذكيرات تغيير الزيت.", "View oil-change operations and reminders.", 97, null, false, false, false, false, "maintenance.oil.read", "عرض تغيير الزيت", "Read oil changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000098"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "ترحيل الزيت والفلتر وتحديث العداد والتذكير.", "Post oil/filter usage and update odometer reminders.", 98, "SENSITIVE_DATA", false, false, false, true, "maintenance.oil.complete", "تنفيذ تغيير الزيت", "Complete oil changes", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000099"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض المرجع المختصر وأعمال المركبات الخارجية.", "View minimal external-vehicle references and jobs.", 99, "SENSITIVE_DATA", false, false, false, true, "maintenance.external_jobs.read", "عرض صيانة المركبات الخارجية", "Read external maintenance jobs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000100"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة أوامر المركبات الخارجية في المواقع المسموحة.", "Manage external-vehicle jobs at eligible locations.", 100, "SENSITIVE_DATA", false, false, false, true, "maintenance.external_jobs.manage", "إدارة صيانة المركبات الخارجية", "Manage external maintenance jobs", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000101"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "بيع قطع الغيار من ورشة الرياض مع صرف FIFO.", "Sell spare parts from Riyadh Workshop with FIFO costing.", 101, "SENSITIVE_DATA", false, false, false, true, "maintenance.part_sales.manage", "بيع قطع الغيار", "Manage spare-part sales", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000102"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل مصنعية الإصلاح المحملة على العميل.", "Record repair labor charged to the customer.", 102, "SENSITIVE_DATA", false, false, false, true, "maintenance.customer_labor_charges.manage", "إدارة مصنعية العميل", "Manage customer labor charges", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000103"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تسجيل المبلغ الفعلي المدفوع للميكانيكي.", "Record actual mechanic labor payments.", 103, "SENSITIVE_DATA", false, false, false, true, "maintenance.mechanic_labor_payments.manage", "إدارة أجرة الميكانيكي", "Manage mechanic labor payments", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000104"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض تكلفة FIFO والمصنعية والمكسب الحقيقي.", "View FIFO cost, labor margin, and true workshop profit.", 104, "HIGH_TRUST_ONLY", false, false, true, true, "maintenance.profit_reports.read", "عرض تقارير مكسب الورشة", "Read workshop profit reports", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000105"), "Maintenance", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "تصدير تقارير الدخل والمصروف والمكسب.", "Export workshop income, expense, and profit reports.", 105, "HIGH_TRUST_ONLY", false, false, true, true, "maintenance.profit_reports.export", "تصدير تقارير مكسب الورشة", "Export workshop profit reports", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000106"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض قطع الغيار والزيوت والإكسسوارات.", "View spare parts, oils, and accessories.", 106, null, false, false, false, false, "inventory.items.read", "عرض أصناف المخزون", "Read inventory items", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000107"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إدارة بيانات الأصناف ووحدات الشراء والمخزون.", "Manage item data and purchase/base units.", 107, "SENSITIVE_DATA", false, false, false, true, "inventory.items.manage", "إدارة أصناف المخزون", "Manage inventory items", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000108"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض الرصيد في مستودع جدة وورشة الرياض.", "View balances at Jeddah Warehouse and Riyadh Workshop.", 108, null, false, false, false, false, "inventory.stock.read", "عرض رصيد المخزون", "Read stock balances", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000109"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "ترحيل النقل والصرف وعهد الرايدر بطريقة FIFO.", "Post transfers, usages, and rider issues using FIFO.", 109, "SENSITIVE_DATA", false, false, false, true, "inventory.stock.move", "نقل وصرف المخزون", "Move inventory stock", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000110"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عكس الاستخدامات مع استعادة طبقات التكلفة الأصلية.", "Reverse usage into its original cost layers.", 110, "HIGH_TRUST_ONLY", false, false, true, true, "inventory.stock.adjust", "عكس حركات المخزون", "Adjust inventory stock", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000111"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "عرض الأسعار والكميات المتبقية لكل دفعة FIFO.", "View FIFO layer prices and remaining quantities.", 111, "HIGH_TRUST_ONLY", false, false, true, true, "inventory.cost_layers.read", "عرض طبقات تكلفة المخزون", "Read inventory cost layers", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000112"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "ترحيل الفاتورة وملفها وطبقات التكلفة.", "Post purchase receipts, bill files, and cost layers.", 112, "SENSITIVE_DATA", false, false, false, true, "inventory.receipts.manage", "إدارة فواتير الشراء", "Manage purchase receipts", null, false, false, null, null, 1 },
                    { new Guid("019c18d5-62e1-7000-a000-000000000113"), "Inventory", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, "إرجاع المخزون إلى المورد من طبقة التكلفة الأصلية.", "Return stock to the supplier from its original cost layer.", 113, "SENSITIVE_DATA", false, false, false, true, "inventory.returns.manage", "إدارة مرتجعات المورد", "Manage supplier returns", null, false, false, null, null, 1 }
                });

            migrationBuilder.InsertData(
                schema: "maintenance",
                table: "Plans",
                columns: new[] { "Id", "AlertDaysBefore", "AlertKilometersBefore", "ChecklistJson", "Code", "CreatedAtUtc", "CreatedByUserId", "DefaultOilQuantityLiters", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IntervalDays", "IntervalKilometers", "InventoryItemId", "IsDeleted", "MaximumAfterKilometers", "NameAr", "NameEn", "ReminderAfterKilometers", "Status", "TriggerType", "UpdatedAtUtc", "UpdatedByUserId", "VehicleModelId", "VehicleType" },
                values: new object[,]
                {
                    { new Guid("019d77f0-0000-7000-8000-000000000005"), null, null, null, "OIL_CHANGE_CAR", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, null, null, false, 5000L, "تغيير زيت السيارة", "Car oil change", 4000L, 1, 3, null, null, null, 2 },
                    { new Guid("019d77f0-0000-7000-8000-000000000006"), null, null, null, "OIL_CHANGE_MOTORCYCLE", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, null, null, null, null, false, 1000L, "تغيير زيت الدراجة النارية", "Motorcycle oil change", 800L, 1, 3, null, null, null, 1 }
                });

            migrationBuilder.InsertData(
                schema: "maintenance",
                table: "InventoryLocations",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "CreatedByUserId", "DeletedAtUtc", "DeletedByUserId", "DeletionReason", "IsDeleted", "MaintenanceLocationId", "NameAr", "NameEn", "Status", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("019d77f0-0000-7000-8000-000000000003"), "JEDDAH_WAREHOUSE_STOCK", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new Guid("019d77f0-0000-7000-8000-000000000001"), "مخزون مستودع جدة", "Jeddah Warehouse Stock", 1, null, null },
                    { new Guid("019d77f0-0000-7000-8000-000000000004"), "RIYADH_WORKSHOP_STOCK", new DateTimeOffset(new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, false, new Guid("019d77f0-0000-7000-8000-000000000002"), "مخزون ورشة الرياض", "Riyadh Workshop Stock", 1, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCustomerPayments_MaintenanceWorkOrderId_PaidAtUtc",
                schema: "maintenance",
                table: "ExternalCustomerPayments",
                columns: new[] { "MaintenanceWorkOrderId", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCustomerPayments_ReversalOfPaymentId",
                schema: "maintenance",
                table: "ExternalCustomerPayments",
                column: "ReversalOfPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_MaintenanceWorkOrderId_SourceType",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                columns: new[] { "MaintenanceWorkOrderId", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_OccurredAtUtc_EntryType",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                columns: new[] { "OccurredAtUtc", "EntryType" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFinancialEntries_ReversalOfEntryId",
                schema: "maintenance",
                table: "ExternalFinancialEntries",
                column: "ReversalOfEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPartSaleLines_InventoryItemId",
                schema: "maintenance",
                table: "ExternalPartSaleLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPartSaleLines_MaintenanceMaterialUsageId",
                schema: "maintenance",
                table: "ExternalPartSaleLines",
                column: "MaintenanceMaterialUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPartSaleLines_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "ExternalPartSaleLines",
                column: "MaintenanceWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalVehicleSnapshots_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "ExternalVehicleSnapshots",
                column: "MaintenanceWorkOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Barcode",
                schema: "maintenance",
                table: "InventoryItems",
                column: "Barcode",
                filter: "[Barcode] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_IsDeleted",
                schema: "maintenance",
                table: "InventoryItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_NormalizedSku",
                schema: "maintenance",
                table: "InventoryItems",
                column: "NormalizedSku",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_Code",
                schema: "maintenance",
                table: "InventoryLocations",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_IsDeleted",
                schema: "maintenance",
                table: "InventoryLocations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLocations_MaintenanceLocationId_Status",
                schema: "maintenance",
                table: "InventoryLocations",
                columns: new[] { "MaintenanceLocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborEntries_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "LaborEntries",
                column: "MaintenanceWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLocations_Code",
                schema: "maintenance",
                table: "MaintenanceLocations",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLocations_IsDeleted",
                schema: "maintenance",
                table: "MaintenanceLocations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLocations_OperatingCityId_Status",
                schema: "maintenance",
                table: "MaintenanceLocations",
                columns: new[] { "OperatingCityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_InventoryItemId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_InventoryLocationId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "MaintenanceWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_ReversalOfUsageId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "ReversalOfUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_RiderProfileId_UsedAtUtc",
                schema: "maintenance",
                table: "MaterialUsages",
                columns: new[] { "RiderProfileId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_RiderVehicleAssignmentId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "RiderVehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_StockMovementId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_StockMovementLineId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "StockMovementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUsages_VehicleId_UsedAtUtc",
                schema: "maintenance",
                table: "MaterialUsages",
                columns: new[] { "VehicleId", "UsedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_MaintenanceWorkOrderId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "MaintenanceWorkOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_OilFilterInventoryItemId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "OilFilterInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_OilFilterMaterialUsageId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "OilFilterMaterialUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_OilInventoryItemId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "OilInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_OilMaterialUsageId",
                schema: "maintenance",
                table: "OilChangeOperations",
                column: "OilMaterialUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_OilChangeOperations_VehicleTypeSnapshot_OdometerAtChange",
                schema: "maintenance",
                table: "OilChangeOperations",
                columns: new[] { "VehicleTypeSnapshot", "OdometerAtChange" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                schema: "maintenance",
                table: "Plans",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_InventoryItemId",
                schema: "maintenance",
                table: "Plans",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_IsDeleted",
                schema: "maintenance",
                table: "Plans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_VehicleModelId",
                schema: "maintenance",
                table: "Plans",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptAttachments_PurchaseReceiptId",
                schema: "maintenance",
                table: "PurchaseReceiptAttachments",
                column: "PurchaseReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLines_InventoryItemId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLines_PurchaseReceiptId_InventoryItemId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                columns: new[] { "PurchaseReceiptId", "InventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLines_StockCostLayerId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                column: "StockCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptLines_StockMovementLineId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                column: "StockMovementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_InventoryLocationId",
                schema: "maintenance",
                table: "PurchaseReceipts",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_IsDeleted",
                schema: "maintenance",
                table: "PurchaseReceipts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_PostedMovementId",
                schema: "maintenance",
                table: "PurchaseReceipts",
                column: "PostedMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_ReceiptNumber",
                schema: "maintenance",
                table: "PurchaseReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_SupplierId_SupplierInvoiceNumber",
                schema: "maintenance",
                table: "PurchaseReceipts",
                columns: new[] { "SupplierId", "SupplierInvoiceNumber" },
                unique: true,
                filter: "[SupplierInvoiceNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssueLines_InventoryItemId",
                schema: "maintenance",
                table: "RiderInventoryIssueLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssueLines_RiderInventoryIssueId",
                schema: "maintenance",
                table: "RiderInventoryIssueLines",
                column: "RiderInventoryIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssueLines_StockMovementLineId",
                schema: "maintenance",
                table: "RiderInventoryIssueLines",
                column: "StockMovementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_IsDeleted",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_IssuedFromLocationId",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                column: "IssuedFromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_IssueNumber",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                column: "IssueNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_PostedMovementId",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                column: "PostedMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_RelatedAssignmentId",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                column: "RelatedAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiderInventoryIssues_RiderProfileId_IssuedAtUtc",
                schema: "maintenance",
                table: "RiderInventoryIssues",
                columns: new[] { "RiderProfileId", "IssuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_InventoryItemId_InventoryLocationId",
                schema: "maintenance",
                table: "StockBalances",
                columns: new[] { "InventoryItemId", "InventoryLocationId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_InventoryLocationId",
                schema: "maintenance",
                table: "StockBalances",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBalances_IsDeleted",
                schema: "maintenance",
                table: "StockBalances",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostAllocations_MaintenanceMaterialUsageId",
                schema: "maintenance",
                table: "StockCostAllocations",
                column: "MaintenanceMaterialUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostAllocations_RiderInventoryIssueLineId",
                schema: "maintenance",
                table: "StockCostAllocations",
                column: "RiderInventoryIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostAllocations_StockCostLayerId",
                schema: "maintenance",
                table: "StockCostAllocations",
                column: "StockCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostAllocations_StockMovementLineId_StockCostLayerId",
                schema: "maintenance",
                table: "StockCostAllocations",
                columns: new[] { "StockMovementLineId", "StockCostLayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_InventoryItemId_InventoryLocationId_ReceivedAtUtc_OriginalSequence_Id",
                schema: "maintenance",
                table: "StockCostLayers",
                columns: new[] { "InventoryItemId", "InventoryLocationId", "ReceivedAtUtc", "OriginalSequence", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_InventoryLocationId",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_IsDeleted",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_SourceCostLayerId",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "SourceCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_SourceMovementLineId",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "SourceMovementLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCostLayers_SourceReceiptLineId",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "SourceReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementLines_CostLayerId",
                schema: "maintenance",
                table: "StockMovementLines",
                column: "CostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementLines_InventoryItemId",
                schema: "maintenance",
                table: "StockMovementLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementLines_StockMovementId_InventoryItemId",
                schema: "maintenance",
                table: "StockMovementLines",
                columns: new[] { "StockMovementId", "InventoryItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_DestinationLocationId_OccurredAtUtc",
                schema: "maintenance",
                table: "StockMovements",
                columns: new[] { "DestinationLocationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementNumber",
                schema: "maintenance",
                table: "StockMovements",
                column: "MovementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReversalOfMovementId",
                schema: "maintenance",
                table: "StockMovements",
                column: "ReversalOfMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SourceLocationId_OccurredAtUtc",
                schema: "maintenance",
                table: "StockMovements",
                columns: new[] { "SourceLocationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_InventoryItemId",
                schema: "maintenance",
                table: "StockTransferLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_StockTransferId",
                schema: "maintenance",
                table: "StockTransferLines",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationLocationId",
                schema: "maintenance",
                table: "StockTransfers",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_DestinationMovementId",
                schema: "maintenance",
                table: "StockTransfers",
                column: "DestinationMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_IsDeleted",
                schema: "maintenance",
                table: "StockTransfers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceLocationId",
                schema: "maintenance",
                table: "StockTransfers",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_SourceMovementId",
                schema: "maintenance",
                table: "StockTransfers",
                column: "SourceMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_TransferNumber",
                schema: "maintenance",
                table: "StockTransfers",
                column: "TransferNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_InventoryItemId",
                schema: "maintenance",
                table: "SupplierReturnLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_StockCostLayerId",
                schema: "maintenance",
                table: "SupplierReturnLines",
                column: "StockCostLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_SupplierReturnId",
                schema: "maintenance",
                table: "SupplierReturnLines",
                column: "SupplierReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_InventoryLocationId",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "InventoryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_IsDeleted",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PostedMovementId",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "PostedMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PurchaseReceiptId",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "PurchaseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_ReturnNumber",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                schema: "maintenance",
                table: "SupplierReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsDeleted",
                schema: "maintenance",
                table: "Suppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierNumber",
                schema: "maintenance",
                table: "Suppliers",
                column: "SupplierNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_VatNumber",
                schema: "maintenance",
                table: "Suppliers",
                column: "VatNumber",
                filter: "[VatNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_ReversalOfExpenseId",
                schema: "maintenance",
                table: "VehicleExpenses",
                column: "ReversalOfExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_RiderProfileId_OccurredOn",
                schema: "maintenance",
                table: "VehicleExpenses",
                columns: new[] { "RiderProfileId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_RiderVehicleAssignmentId",
                schema: "maintenance",
                table: "VehicleExpenses",
                column: "RiderVehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_VehicleId_OccurredOn",
                schema: "maintenance",
                table: "VehicleExpenses",
                columns: new[] { "VehicleId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedules_IsDeleted",
                schema: "maintenance",
                table: "VehicleSchedules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedules_LastCompletedWorkOrderId",
                schema: "maintenance",
                table: "VehicleSchedules",
                column: "LastCompletedWorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedules_MaintenancePlanId",
                schema: "maintenance",
                table: "VehicleSchedules",
                column: "MaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedules_VehicleId_MaintenancePlanId",
                schema: "maintenance",
                table: "VehicleSchedules",
                columns: new[] { "VehicleId", "MaintenancePlanId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AttributedRiderProfileId",
                schema: "maintenance",
                table: "WorkOrders",
                column: "AttributedRiderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_IsDeleted",
                schema: "maintenance",
                table: "WorkOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_MaintenanceLocationId_Status_OpenedAtUtc",
                schema: "maintenance",
                table: "WorkOrders",
                columns: new[] { "MaintenanceLocationId", "Status", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_RiderVehicleAssignmentId",
                schema: "maintenance",
                table: "WorkOrders",
                column: "RiderVehicleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_VehicleId_OpenedAtUtc",
                schema: "maintenance",
                table: "WorkOrders",
                columns: new[] { "VehicleId", "OpenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_VehicleIssueId",
                schema: "maintenance",
                table: "WorkOrders",
                column: "VehicleIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNumber",
                schema: "maintenance",
                table: "WorkOrders",
                column: "WorkOrderNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalPartSaleLines_MaterialUsages_MaintenanceMaterialUsageId",
                schema: "maintenance",
                table: "ExternalPartSaleLines",
                column: "MaintenanceMaterialUsageId",
                principalSchema: "maintenance",
                principalTable: "MaterialUsages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUsages_StockMovementLines_StockMovementLineId",
                schema: "maintenance",
                table: "MaterialUsages",
                column: "StockMovementLineId",
                principalSchema: "maintenance",
                principalTable: "StockMovementLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReceiptLines_StockCostLayers_StockCostLayerId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                column: "StockCostLayerId",
                principalSchema: "maintenance",
                principalTable: "StockCostLayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseReceiptLines_StockMovementLines_StockMovementLineId",
                schema: "maintenance",
                table: "PurchaseReceiptLines",
                column: "StockMovementLineId",
                principalSchema: "maintenance",
                principalTable: "StockMovementLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RiderInventoryIssueLines_StockMovementLines_StockMovementLineId",
                schema: "maintenance",
                table: "RiderInventoryIssueLines",
                column: "StockMovementLineId",
                principalSchema: "maintenance",
                principalTable: "StockMovementLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockCostAllocations_StockCostLayers_StockCostLayerId",
                schema: "maintenance",
                table: "StockCostAllocations",
                column: "StockCostLayerId",
                principalSchema: "maintenance",
                principalTable: "StockCostLayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockCostAllocations_StockMovementLines_StockMovementLineId",
                schema: "maintenance",
                table: "StockCostAllocations",
                column: "StockMovementLineId",
                principalSchema: "maintenance",
                principalTable: "StockMovementLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockCostLayers_StockMovementLines_SourceMovementLineId",
                schema: "maintenance",
                table: "StockCostLayers",
                column: "SourceMovementLineId",
                principalSchema: "maintenance",
                principalTable: "StockMovementLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceiptLines_InventoryItems_InventoryItemId",
                schema: "maintenance",
                table: "PurchaseReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCostLayers_InventoryItems_InventoryItemId",
                schema: "maintenance",
                table: "StockCostLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementLines_InventoryItems_InventoryItemId",
                schema: "maintenance",
                table: "StockMovementLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLocations_MaintenanceLocations_MaintenanceLocationId",
                schema: "maintenance",
                table: "InventoryLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceipts_InventoryLocations_InventoryLocationId",
                schema: "maintenance",
                table: "PurchaseReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCostLayers_InventoryLocations_InventoryLocationId",
                schema: "maintenance",
                table: "StockCostLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryLocations_DestinationLocationId",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryLocations_SourceLocationId",
                schema: "maintenance",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceiptLines_StockMovementLines_StockMovementLineId",
                schema: "maintenance",
                table: "PurchaseReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockCostLayers_StockMovementLines_SourceMovementLineId",
                schema: "maintenance",
                table: "StockCostLayers");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceipts_StockMovements_PostedMovementId",
                schema: "maintenance",
                table: "PurchaseReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId",
                schema: "maintenance",
                table: "PurchaseReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseReceiptLines_StockCostLayers_StockCostLayerId",
                schema: "maintenance",
                table: "PurchaseReceiptLines");

            migrationBuilder.DropTable(
                name: "ExternalCustomerPayments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "ExternalFinancialEntries",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "ExternalPartSaleLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "ExternalVehicleSnapshots",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "LaborEntries",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "OilChangeOperations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "PurchaseReceiptAttachments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockBalances",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockCostAllocations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockTransferLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "SupplierReturnLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "VehicleExpenses",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "VehicleSchedules",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaterialUsages",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "RiderInventoryIssueLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "SupplierReturns",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "WorkOrders",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "RiderInventoryIssues",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "InventoryItems",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "MaintenanceLocations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "InventoryLocations",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockMovementLines",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "PurchaseReceipts",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "StockCostLayers",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "PurchaseReceiptLines",
                schema: "maintenance");

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000093"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000094"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000095"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000096"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000097"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000098"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000099"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000100"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000101"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000102"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000103"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000104"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000105"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000106"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000107"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000108"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000109"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000110"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000111"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000112"));

            migrationBuilder.DeleteData(
                schema: "platform",
                table: "PermissionDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("019c18d5-62e1-7000-a000-000000000113"));
        }
    }
}
