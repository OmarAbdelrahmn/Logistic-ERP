using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Maintenance;

public interface IMaintenanceService
{
    Task<Result<IReadOnlyList<MaintenanceLocationResponse>>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<Result<MaintenanceLocationResponse>> UpsertLocationAsync(Guid? id, MaintenanceLocationRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryItemResponse>>> GetItemsAsync(string? search, CancellationToken cancellationToken = default);
    Task<Result<InventoryItemResponse>> UpsertItemAsync(Guid? id, InventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MaintenanceSupplierResponse>>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<Result<MaintenanceSupplierResponse>> UpsertSupplierAsync(Guid? id, MaintenanceSupplierRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<StockBalanceResponse>>> GetBalancesAsync(Guid? inventoryLocationId, Guid? inventoryItemId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<StockCostLayerResponse>>> GetCostLayersAsync(Guid? inventoryLocationId, Guid? inventoryItemId, bool availableOnly, CancellationToken cancellationToken = default);
    Task<Result<PurchaseReceiptResponse>> PostPurchaseReceiptAsync(PostPurchaseReceiptRequest request, PrivateFileUpload billFile, CancellationToken cancellationToken = default);
    Task<Result<PurchaseReceiptResponse>> GetPurchaseReceiptAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PrivateFileDownload>> DownloadPurchaseReceiptAttachmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OilBarrelResponse>>> GetOilBarrelsAsync(Guid? inventoryLocationId, Guid? inventoryItemId, string? status, CancellationToken cancellationToken = default);
    Task<Result<OpenOilBarrelResponse>> OpenOilBarrelAsync(Guid id, OpenOilBarrelRequest request, CancellationToken cancellationToken = default);
    Task<Result<OilBarrelLossResponse>> RecordOilBarrelLossAsync(Guid id, RecordOilBarrelLossRequest request, CancellationToken cancellationToken = default);
    Task<Result<StockTransferResponse>> PostTransferAsync(PostStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<Result<SupplierReturnResponse>> PostSupplierReturnAsync(PostSupplierReturnRequest request, CancellationToken cancellationToken = default);
    Task<Result<RiderInventoryIssueResponse>> PostRiderIssueAsync(PostRiderInventoryIssueRequest request, CancellationToken cancellationToken = default);
    Task<Result<MaintenanceWorkOrderResponse>> CreateWorkOrderAsync(CreateMaintenanceWorkOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<MaintenanceWorkOrderResponse>> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MaintenanceWorkOrderResponse>>> GetWorkOrdersAsync(Guid? maintenanceLocationId, Guid? vehicleId, string? status, CancellationToken cancellationToken = default);
    Task<Result<MaintenanceWorkOrderResponse>> ActOnWorkOrderAsync(Guid id, string action, MaintenanceWorkOrderActionRequest request, CancellationToken cancellationToken = default);
    Task<Result<MaintenanceMaterialUsageResponse>> PostMaterialUsageAsync(Guid workOrderId, PostMaterialUsageRequest request, CancellationToken cancellationToken = default);
    Task<Result<MaintenanceMaterialUsageResponse>> ReverseMaterialUsageAsync(Guid usageId, ReverseMaterialUsageRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MaintenanceMaterialUsageResponse>>> GetVehicleMaterialHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MaintenanceMaterialUsageResponse>>> GetRiderMaterialHistoryAsync(Guid riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MaintenancePlanResponse>>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<Result<MaintenancePlanResponse>> UpsertPlanAsync(Guid? id, MaintenancePlanRequest request, CancellationToken cancellationToken = default);
    Task<Result<OilChangeResponse>> CompleteOilChangeAsync(Guid workOrderId, CompleteOilChangeRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<OilReminderResponse>>> GetOilRemindersAsync(CancellationToken cancellationToken = default);
    Task<Result<ExternalPartSaleResponse>> PostExternalPartSaleAsync(Guid workOrderId, ExternalPartSaleRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExternalFinancialEntryResponse>> PostCustomerLaborChargeAsync(Guid workOrderId, ExternalFinancialEntryRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExternalFinancialEntryResponse>> PostMechanicLaborPaymentAsync(Guid workOrderId, MechanicLaborPaymentRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExternalFinancialEntryResponse>> PostOtherFinancialEntryAsync(Guid workOrderId, bool income, ExternalFinancialEntryRequest request, CancellationToken cancellationToken = default);
    Task<Result<ExternalCustomerPaymentResponse>> PostCustomerPaymentAsync(Guid workOrderId, ExternalCustomerPaymentRequest request, CancellationToken cancellationToken = default);
    Task<Result<WorkshopProfitReportResponse>> GetWorkshopProfitAsync(Guid maintenanceLocationId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
