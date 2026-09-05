using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Platform;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Maintenance;

public sealed class MaintenanceLocation : AuditableEntity
{
    public static readonly Guid JeddahWarehouseId = Guid.Parse("019d77f0-0000-7000-8000-000000000001");
    public static readonly Guid RiyadhWorkshopId = Guid.Parse("019d77f0-0000-7000-8000-000000000002");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public Guid OperatingCityId { get; set; }
    public MaintenanceLocationType LocationType { get; set; }
    public bool AllowsCompanyVehicles { get; set; }
    public bool AllowsExternalVehicles { get; set; }
    public bool AllowsSparePartSales { get; set; }
    public bool AllowsPaidExternalRepairs { get; set; }
    public bool InventoryEnabled { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public string? Notes { get; set; }
}

public sealed class InventoryLocation : AuditableEntity
{
    public static readonly Guid JeddahWarehouseInventoryId = Guid.Parse("019d77f0-0000-7000-8000-000000000003");
    public static readonly Guid RiyadhWorkshopInventoryId = Guid.Parse("019d77f0-0000-7000-8000-000000000004");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public Guid MaintenanceLocationId { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}

public sealed class InventoryItem : AuditableEntity
{
    public string Sku { get; set; } = string.Empty;
    public string NormalizedSku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public InventoryItemType ItemType { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public InventoryUnitOfMeasure BaseUnitOfMeasure { get; set; }
    public InventoryUnitOfMeasure PurchaseUnitOfMeasure { get; set; }
    public decimal? DefaultPackageQuantity { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public bool IsSerialized { get; set; }
    public bool IsLotTracked { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}

public sealed class MaintenanceSupplier : AuditableEntity
{
    public string SupplierNumber { get; set; } = string.Empty;
    public string LegalNameAr { get; set; } = string.Empty;
    public string LegalNameEn { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? CommercialRegistrationNumber { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? PaymentTermsDays { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public string? Notes { get; set; }
}

public sealed class StockBalance : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal ReportingAverageUnitCost { get; set; }
    public DateTimeOffset? LastMovementAtUtc { get; set; }
}

public sealed class StockCostLayer : AuditableEntity
{
    public Guid InventoryItemId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid? SourceReceiptLineId { get; set; }
    public Guid? SourceMovementLineId { get; set; }
    public Guid? SourceCostLayerId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public long OriginalSequence { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public InventoryUnitOfMeasure BaseUnitOfMeasure { get; set; }
    public decimal UnitCost { get; set; }
    public decimal OriginalTotalCost { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed class StockMovement : HistoryEntity
{
    public string MovementNumber { get; set; } = string.Empty;
    public StockMovementType MovementType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? DestinationLocationId { get; set; }
    public string SourceDocumentType { get; set; } = string.Empty;
    public Guid SourceDocumentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid PostedByUserId { get; set; }
    public Guid? ReversalOfMovementId { get; set; }
}

public sealed class StockMovementLine : HistoryEntity
{
    public Guid StockMovementId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public InventoryUnitOfMeasure BaseUnitOfMeasure { get; set; }
    public Guid? CostLayerId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class StockCostAllocation : HistoryEntity
{
    public Guid StockMovementLineId { get; set; }
    public Guid? MaintenanceMaterialUsageId { get; set; }
    public Guid? RiderInventoryIssueLineId { get; set; }
    public Guid StockCostLayerId { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AllocatedCost { get; set; }
}

public sealed class PurchaseReceipt : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal InventoryValuationAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public InventoryDocumentStatus Status { get; set; } = InventoryDocumentStatus.Posted;
    public Guid PostedMovementId { get; set; }
}

public sealed class PurchaseReceiptLine : HistoryEntity
{
    public Guid PurchaseReceiptId { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryUnitOfMeasure PurchaseUnit { get; set; }
    public decimal PackageCount { get; set; }
    public decimal DeclaredQuantityPerPackage { get; set; }
    public decimal ReceivedBaseQuantity { get; set; }
    public InventoryUnitOfMeasure BaseUnitOfMeasure { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? NetWeightKg { get; set; }
    public decimal PackageUnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal InventoryValuationAmount { get; set; }
    public decimal BaseUnitCost { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public Guid StockMovementLineId { get; set; }
    public Guid StockCostLayerId { get; set; }
}

public sealed class PurchaseReceiptAttachment : HistoryEntity
{
    public Guid PurchaseReceiptId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; }
}

public sealed class OilBarrel : AuditableEntity
{
    public string BarrelNumber { get; set; } = string.Empty;
    public Guid PurchaseReceiptLineId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid StockCostLayerId { get; set; }
    public int PackageSequence { get; set; }
    public decimal NominalCapacityLiters { get; set; }
    public decimal RemainingLiters { get; set; }
    public decimal UnitCostPerLiter { get; set; }
    public decimal MaximumAllowedLossLiters { get; set; }
    public decimal RecordedLossLiters { get; set; }
    public OilBarrelStatus Status { get; set; } = OilBarrelStatus.Sealed;
    public DateTimeOffset? OpenedAtUtc { get; set; }
    public Guid? OpenedByUserId { get; set; }
    public DateTimeOffset? DepletedAtUtc { get; set; }
}

public sealed class OilBarrelUsageAllocation : HistoryEntity
{
    public Guid MaintenanceMaterialUsageId { get; set; }
    public Guid OilBarrelId { get; set; }
    public decimal QuantityLiters { get; set; }
    public MaintenanceUsageDirection Direction { get; set; } = MaintenanceUsageDirection.Issue;
    public Guid? ReversalOfAllocationId { get; set; }
}

public sealed class OilBarrelLoss : HistoryEntity
{
    public Guid OilBarrelId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public decimal QuantityLiters { get; set; }
    public decimal CostAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid StockMovementId { get; set; }
    public Guid StockMovementLineId { get; set; }
    public Guid RecordedByUserId { get; set; }
}

public sealed class StockTransfer : AuditableEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public Guid SourceLocationId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public DateTimeOffset PostedAtUtc { get; set; }
    public Guid PostedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public InventoryDocumentStatus Status { get; set; } = InventoryDocumentStatus.Posted;
    public Guid SourceMovementId { get; set; }
    public Guid DestinationMovementId { get; set; }
}

public sealed class StockTransferLine : HistoryEntity
{
    public Guid StockTransferId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public InventoryUnitOfMeasure BaseUnitOfMeasure { get; set; }
    public decimal TotalCost { get; set; }
}

public sealed class SupplierReturn : AuditableEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid? PurchaseReceiptId { get; set; }
    public DateTimeOffset ReturnedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public InventoryDocumentStatus Status { get; set; } = InventoryDocumentStatus.Posted;
    public Guid PostedMovementId { get; set; }
}

public sealed class SupplierReturnLine : HistoryEntity
{
    public Guid SupplierReturnId { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid StockCostLayerId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class RiderInventoryIssue : AuditableEntity
{
    public string IssueNumber { get; set; } = string.Empty;
    public Guid RiderProfileId { get; set; }
    public Guid IssuedFromLocationId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public Guid IssuedByUserId { get; set; }
    public Guid? RelatedAssignmentId { get; set; }
    public InventoryDocumentStatus Status { get; set; } = InventoryDocumentStatus.Posted;
    public string? Notes { get; set; }
    public Guid PostedMovementId { get; set; }
}

public sealed class RiderInventoryIssueLine : HistoryEntity
{
    public Guid RiderInventoryIssueId { get; set; }
    public Guid InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public Guid StockMovementLineId { get; set; }
    public bool ExpectedReturn { get; set; }
    public decimal ReturnedQuantity { get; set; }
}
