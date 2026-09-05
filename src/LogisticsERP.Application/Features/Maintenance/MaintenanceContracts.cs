using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Maintenance;

public sealed record MaintenanceLocationRequest(
    string Code,
    string NameAr,
    string NameEn,
    Guid OperatingCityId,
    MaintenanceLocationType LocationType,
    bool AllowsCompanyVehicles,
    bool AllowsExternalVehicles,
    bool AllowsSparePartSales,
    bool AllowsPaidExternalRepairs,
    bool InventoryEnabled,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes,
    string? RowVersion);

public sealed record MaintenanceLocationResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid OperatingCityId,
    string OperatingCityNameAr,
    MaintenanceLocationType LocationType,
    bool AllowsCompanyVehicles,
    bool AllowsExternalVehicles,
    bool AllowsSparePartSales,
    bool AllowsPaidExternalRepairs,
    bool InventoryEnabled,
    CatalogStatus Status,
    string? Address,
    string? Notes,
    string RowVersion);

public sealed record InventoryItemRequest(
    string Sku,
    string? Barcode,
    InventoryItemType ItemType,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    InventoryUnitOfMeasure BaseUnitOfMeasure,
    InventoryUnitOfMeasure PurchaseUnitOfMeasure,
    decimal? DefaultPackageQuantity,
    decimal MinimumStockLevel,
    decimal ReorderQuantity,
    bool IsSerialized,
    bool IsLotTracked,
    string? RowVersion);

public sealed record InventoryItemResponse(
    Guid Id,
    string Sku,
    string? Barcode,
    InventoryItemType ItemType,
    string NameAr,
    string NameEn,
    InventoryUnitOfMeasure BaseUnitOfMeasure,
    InventoryUnitOfMeasure PurchaseUnitOfMeasure,
    decimal? DefaultPackageQuantity,
    decimal MinimumStockLevel,
    decimal ReorderQuantity,
    CatalogStatus Status,
    string RowVersion);

public sealed record MaintenanceSupplierRequest(
    string SupplierNumber,
    string LegalNameAr,
    string LegalNameEn,
    string? VatNumber,
    string? CommercialRegistrationNumber,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int? PaymentTermsDays,
    string? Notes,
    string? RowVersion);

public sealed record MaintenanceSupplierResponse(
    Guid Id,
    string SupplierNumber,
    string LegalNameAr,
    string LegalNameEn,
    string? VatNumber,
    string? CommercialRegistrationNumber,
    string? Phone,
    CatalogStatus Status,
    string? Notes,
    string RowVersion);

public sealed record StockBalanceResponse(
    Guid Id,
    Guid InventoryItemId,
    string Sku,
    string ItemNameAr,
    Guid InventoryLocationId,
    string LocationNameAr,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal ReportingAverageUnitCost,
    decimal InventoryValue,
    DateTimeOffset? LastMovementAtUtc,
    string RowVersion);

public sealed record StockCostLayerResponse(
    Guid Id,
    Guid InventoryItemId,
    Guid InventoryLocationId,
    DateTimeOffset ReceivedAtUtc,
    long OriginalSequence,
    decimal OriginalQuantity,
    decimal RemainingQuantity,
    InventoryUnitOfMeasure BaseUnitOfMeasure,
    decimal UnitCost,
    decimal RemainingValue,
    string? LotNumber,
    DateOnly? ExpiryDate,
    Guid? SourceReceiptLineId,
    Guid? SourceCostLayerId,
    string RowVersion);

public sealed record PurchaseReceiptLineRequest(
    Guid InventoryItemId,
    InventoryUnitOfMeasure PurchaseUnit,
    decimal PackageCount,
    decimal DeclaredQuantityPerPackage,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    decimal PackageUnitPrice,
    decimal DiscountAmount,
    decimal TaxAmount,
    string? LotNumber,
    DateOnly? ExpiryDate);

public sealed record PostPurchaseReceiptRequest(
    Guid SupplierId,
    string? SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateTimeOffset ReceivedAtUtc,
    Guid InventoryLocationId,
    string CurrencyCode,
    IReadOnlyList<PurchaseReceiptLineRequest> Lines);

public sealed record PurchaseReceiptLineResponse(
    Guid Id,
    Guid InventoryItemId,
    string Sku,
    InventoryUnitOfMeasure PurchaseUnit,
    decimal PackageCount,
    decimal DeclaredQuantityPerPackage,
    decimal ReceivedBaseQuantity,
    InventoryUnitOfMeasure BaseUnitOfMeasure,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    decimal PackageUnitPrice,
    decimal LineSubtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal InventoryValuationAmount,
    decimal BaseUnitCost,
    Guid StockCostLayerId);

public sealed record PurchaseReceiptAttachmentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Sha256Checksum,
    DateTimeOffset UploadedAtUtc);

public sealed record PurchaseReceiptResponse(
    Guid Id,
    string ReceiptNumber,
    Guid SupplierId,
    string SupplierNameAr,
    string? SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    DateTimeOffset ReceivedAtUtc,
    Guid InventoryLocationId,
    string InventoryLocationNameAr,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal InventoryValuationAmount,
    decimal TotalAmount,
    string CurrencyCode,
    InventoryDocumentStatus Status,
    IReadOnlyList<PurchaseReceiptLineResponse> Lines,
    PurchaseReceiptAttachmentResponse Attachment,
    IReadOnlyList<OilBarrelResponse> OilBarrels,
    string RowVersion);

public sealed record OilBarrelResponse(
    Guid Id,
    string BarrelNumber,
    Guid PurchaseReceiptLineId,
    Guid InventoryItemId,
    Guid InventoryLocationId,
    Guid StockCostLayerId,
    int PackageSequence,
    decimal NominalCapacityLiters,
    decimal ConsumedLiters,
    decimal RemainingLiters,
    decimal UnitCostPerLiter,
    decimal RemainingInventoryValue,
    decimal MaximumAllowedLossLiters,
    decimal RecordedLossLiters,
    decimal RemainingLossAllowanceLiters,
    OilBarrelStatus Status,
    DateTimeOffset? OpenedAtUtc,
    DateTimeOffset? DepletedAtUtc,
    string RowVersion);

public sealed record OpenOilBarrelRequest(DateTimeOffset OpenedAtUtc, string RowVersion);

public sealed record OpenOilBarrelResponse(
    OilBarrelResponse Barrel,
    bool Opened,
    bool HasPreviousBarrelWarning,
    decimal PreviousOpenBarrelsRemainingLiters,
    string? WarningCode,
    string? WarningMessageAr);

public sealed record RecordOilBarrelLossRequest(
    DateTimeOffset OccurredAtUtc,
    decimal QuantityLiters,
    string Reason,
    string RowVersion);

public sealed record OilBarrelLossResponse(
    Guid Id,
    Guid OilBarrelId,
    DateTimeOffset OccurredAtUtc,
    decimal QuantityLiters,
    decimal CostAmount,
    decimal BarrelRecordedLossLiters,
    decimal BarrelRemainingLiters,
    decimal BarrelRemainingLossAllowanceLiters);

public sealed record StockTransferLineRequest(Guid InventoryItemId, decimal Quantity);
public sealed record PostStockTransferRequest(Guid SourceLocationId, Guid DestinationLocationId, DateTimeOffset PostedAtUtc, string Reason, IReadOnlyList<StockTransferLineRequest> Lines);
public sealed record StockTransferResponse(Guid Id, string TransferNumber, Guid SourceLocationId, Guid DestinationLocationId, DateTimeOffset PostedAtUtc, decimal TotalCost, InventoryDocumentStatus Status, string RowVersion);

public sealed record SupplierReturnLineRequest(Guid InventoryItemId, Guid StockCostLayerId, decimal Quantity, string Reason);
public sealed record PostSupplierReturnRequest(Guid SupplierId, Guid InventoryLocationId, Guid? PurchaseReceiptId, DateTimeOffset ReturnedAtUtc, string Reason, IReadOnlyList<SupplierReturnLineRequest> Lines);
public sealed record SupplierReturnResponse(Guid Id, string ReturnNumber, Guid SupplierId, Guid InventoryLocationId, DateTimeOffset ReturnedAtUtc, decimal TotalCost, InventoryDocumentStatus Status, string RowVersion);

public sealed record RiderInventoryIssueLineRequest(Guid InventoryItemId, decimal Quantity, bool ExpectedReturn);
public sealed record PostRiderInventoryIssueRequest(Guid RiderProfileId, Guid InventoryLocationId, DateTimeOffset IssuedAtUtc, string? Notes, IReadOnlyList<RiderInventoryIssueLineRequest> Lines);
public sealed record RiderInventoryIssueResponse(Guid Id, string IssueNumber, Guid RiderProfileId, Guid? RelatedAssignmentId, Guid InventoryLocationId, DateTimeOffset IssuedAtUtc, decimal TotalCost, InventoryDocumentStatus Status, string RowVersion);

public sealed record ExternalVehicleSnapshotRequest(
    string? PlateOrReference,
    VehicleType? VehicleType,
    string? CustomerName,
    string? CustomerPhone,
    string? Notes);

public sealed record CreateMaintenanceWorkOrderRequest(
    MaintenanceServiceSubjectType ServiceSubjectType,
    Guid? VehicleId,
    Guid? VehicleIssueId,
    Guid MaintenanceLocationId,
    MaintenanceType MaintenanceType,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    long? OdometerAtOpen,
    decimal EstimatedCost,
    string? Diagnosis,
    string? Notes,
    ExternalVehicleSnapshotRequest? ExternalVehicle);

public sealed record MaintenanceWorkOrderActionRequest(DateTimeOffset OccurredAtUtc, string? WorkPerformed, string? QualityCheckNotes, string? Notes, string RowVersion);

public sealed record ExternalVehicleSnapshotResponse(
    string? PlateOrReference,
    VehicleType? VehicleType,
    string? CustomerName,
    string? CustomerPhone,
    string? Notes);

public sealed record MaintenanceWorkOrderResponse(
    Guid Id,
    string WorkOrderNumber,
    MaintenanceServiceSubjectType ServiceSubjectType,
    Guid? VehicleId,
    string? VehicleAssetNumber,
    Guid? VehicleIssueId,
    Guid MaintenanceLocationId,
    string MaintenanceLocationNameAr,
    MaintenanceType MaintenanceType,
    MaintenanceWorkOrderStatus Status,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long? OdometerAtOpen,
    long? OdometerAtCompletion,
    Guid? RiderVehicleAssignmentId,
    Guid? AttributedRiderProfileId,
    decimal EstimatedCost,
    decimal ActualMaterialCost,
    decimal ActualLaborCost,
    decimal ActualOtherCost,
    decimal ActualTotalCost,
    ExternalVehicleSnapshotResponse? ExternalVehicle,
    string? Notes,
    string RowVersion);

public sealed record PostMaterialUsageRequest(Guid InventoryItemId, Guid InventoryLocationId, decimal Quantity, MaintenanceUsageType UsageType, DateTimeOffset UsedAtUtc, string? Notes);
public sealed record StockCostAllocationResponse(Guid StockCostLayerId, decimal Quantity, decimal UnitCost, decimal Cost);
public sealed record MaintenanceMaterialUsageResponse(
    Guid Id,
    Guid MaintenanceWorkOrderId,
    Guid InventoryItemId,
    string Sku,
    string ItemNameAr,
    Guid InventoryLocationId,
    MaintenanceUsageType UsageType,
    MaintenanceUsageDirection Direction,
    decimal Quantity,
    InventoryUnitOfMeasure UnitOfMeasure,
    decimal TotalCost,
    Guid? VehicleId,
    Guid? RiderVehicleAssignmentId,
    Guid? RiderProfileId,
    InventoryAttributionStatus AttributionStatus,
    DateTimeOffset UsedAtUtc,
    Guid? ReversalOfUsageId,
    IReadOnlyList<StockCostAllocationResponse> CostAllocations);

public sealed record ReverseMaterialUsageRequest(DateTimeOffset ReversedAtUtc, string Reason);

public sealed record MaintenancePlanRequest(
    string Code,
    string NameAr,
    string NameEn,
    Guid? VehicleModelId,
    VehicleType? VehicleType,
    MaintenanceTriggerType TriggerType,
    int? IntervalDays,
    long? IntervalKilometers,
    long? ReminderAfterKilometers,
    long? MaximumAfterKilometers,
    int? AlertDaysBefore,
    long? AlertKilometersBefore,
    Guid? InventoryItemId,
    decimal? DefaultOilQuantityLiters,
    string? ChecklistJson,
    string? RowVersion);

public sealed record MaintenancePlanResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid? VehicleModelId,
    VehicleType? VehicleType,
    MaintenanceTriggerType TriggerType,
    int? IntervalDays,
    long? IntervalKilometers,
    long? ReminderAfterKilometers,
    long? MaximumAfterKilometers,
    decimal? DefaultOilQuantityLiters,
    CatalogStatus Status,
    string RowVersion);

public sealed record CompleteOilChangeRequest(
    DateTimeOffset PerformedAtUtc,
    long OdometerAtChange,
    Guid InventoryLocationId,
    Guid OilInventoryItemId,
    Guid? NextOilBarrelId,
    bool OilFilterChanged,
    Guid? OilFilterInventoryItemId,
    decimal? ConfiguredOilQuantityLiters,
    decimal LaborCost,
    decimal OtherCost,
    string? Notes,
    string WorkOrderRowVersion);

public sealed record OilChangeResponse(
    Guid Id,
    Guid MaintenanceWorkOrderId,
    DateTimeOffset PerformedAtUtc,
    long OdometerAtChange,
    VehicleType VehicleType,
    decimal OilQuantityLiters,
    decimal OilCost,
    bool OilFilterChanged,
    decimal OilFilterCost,
    decimal LaborCost,
    decimal OtherCost,
    decimal TotalCost,
    Guid? VehicleId,
    Guid? RiderProfileId);

public sealed record OilReminderResponse(
    Guid VehicleId,
    string AssetNumber,
    VehicleType VehicleType,
    long CurrentOdometer,
    DateTimeOffset? LastCompletedAtUtc,
    long? LastOilChangeOdometer,
    long? ReminderFromOdometer,
    long? MaximumDueOdometer,
    long? DistanceSinceLastChange,
    MaintenanceDueStatus Status);

public sealed record ExternalPartSaleRequest(Guid InventoryItemId, Guid InventoryLocationId, decimal Quantity, decimal SellingUnitPriceBeforeTax, decimal DiscountAmount, decimal TaxAmount, DateTimeOffset OccurredAtUtc, string? Notes);
public sealed record ExternalPartSaleResponse(Guid Id, Guid MaintenanceWorkOrderId, Guid InventoryItemId, decimal Quantity, decimal PartsRevenueBeforeTax, decimal TaxAmount, decimal CustomerLineTotal, Guid MaintenanceMaterialUsageId);

public sealed record ExternalFinancialEntryRequest(decimal AmountBeforeTax, decimal TaxAmount, DateTimeOffset OccurredAtUtc, string Description);
public sealed record MechanicLaborPaymentRequest(Guid? MechanicEmployeeId, string? ExternalMechanicName, decimal Amount, DateTimeOffset PaidAtUtc, string Description);
public sealed record ExternalCustomerPaymentRequest(decimal Amount, WorkshopPaymentMethod PaymentMethod, DateTimeOffset PaidAtUtc, string? Reference);
public sealed record ExternalFinancialEntryResponse(Guid Id, Guid MaintenanceWorkOrderId, ExternalFinancialEntryType EntryType, ExternalFinancialSourceType SourceType, decimal AmountBeforeTax, decimal TaxAmount, decimal TotalAmount, DateTimeOffset OccurredAtUtc, string Description, Guid? MechanicEmployeeId, string? ExternalMechanicName);
public sealed record ExternalCustomerPaymentResponse(Guid Id, Guid MaintenanceWorkOrderId, decimal Amount, WorkshopPaymentMethod PaymentMethod, DateTimeOffset PaidAtUtc, string? Reference);

public sealed record WorkshopProfitWorkOrderResponse(
    Guid MaintenanceWorkOrderId,
    string WorkOrderNumber,
    string? ExternalVehicleReference,
    decimal PartsRevenueBeforeTax,
    decimal CustomerLaborRevenueBeforeTax,
    decimal OtherIncomeBeforeTax,
    decimal FifoInventoryCost,
    decimal MechanicLaborCost,
    decimal OtherExpense,
    decimal TaxCollected,
    decimal CustomerInvoiceTotal,
    decimal AmountPaid,
    decimal OutstandingAmount,
    WorkshopPaymentStatus PaymentStatus,
    decimal PartsGrossProfit,
    decimal LaborProfit,
    decimal NetProfitBeforeTax);

public sealed record WorkshopProfitReportResponse(
    Guid MaintenanceLocationId,
    DateOnly From,
    DateOnly To,
    decimal TotalIncomeBeforeTax,
    decimal TotalExpense,
    decimal TaxCollected,
    decimal CustomerInvoiceTotal,
    decimal AmountPaid,
    decimal NetProfitBeforeTax,
    IReadOnlyList<WorkshopProfitWorkOrderResponse> WorkOrders);
