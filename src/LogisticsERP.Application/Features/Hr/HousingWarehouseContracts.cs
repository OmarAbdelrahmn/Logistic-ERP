using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record HousingWarehouseResponse(
    Guid Id,
    Guid HousingId,
    string HousingCode,
    string HousingNameAr,
    string HousingNameEn,
    string NameAr,
    string NameEn,
    bool IsDefault,
    int ItemCount,
    decimal TotalQuantity,
    string RowVersion);

public sealed record CreateHousingWarehouseItemRequest(
    string NameAr,
    decimal Quantity,
    string Status,
    string? Notes);

public sealed record UpdateHousingWarehouseItemRequest(
    string NameAr,
    string? Notes,
    string RowVersion);

public sealed record SetHousingWarehouseItemStatusQuantityRequest(
    decimal Quantity,
    string RowVersion);

public sealed record TransferHousingWarehouseItemStatusRequest(
    string FromStatus,
    string ToStatus,
    decimal Quantity,
    string RowVersion);

public sealed record TransferHousingWarehouseItemToHousingRequest(
    Guid DestinationHousingId,
    decimal Quantity,
    string RowVersion);

public sealed record ArchiveHousingWarehouseItemRequest(string Reason, string RowVersion);

public sealed record HousingWarehouseItemResponse(
    Guid Id,
    Guid WarehouseId,
    Guid HousingId,
    string NameAr,
    decimal TotalQuantity,
    decimal UnusedQuantity,
    decimal UsedQuantity,
    decimal DamagedQuantity,
    string? Notes,
    string RowVersion);

public sealed record HousingWarehouseItemHousingTransferResponse(
    Guid SourceHousingId,
    Guid DestinationHousingId,
    decimal Quantity,
    HousingWarehouseItemResponse SourceItem,
    HousingWarehouseItemResponse DestinationItem);

public interface IHousingWarehouseService
{
    Task<Result<HousingWarehouseResponse>> GetWarehouseAsync(Guid housingId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingWarehouseItemResponse>>> GetItemsAsync(Guid housingId, string? search, string? status, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemResponse>> GetItemAsync(Guid housingId, Guid itemId, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemResponse>> CreateItemAsync(Guid housingId, CreateHousingWarehouseItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemResponse>> UpdateItemAsync(Guid housingId, Guid itemId, UpdateHousingWarehouseItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemResponse>> SetStatusQuantityAsync(Guid housingId, Guid itemId, string status, SetHousingWarehouseItemStatusQuantityRequest request, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemResponse>> TransferStatusAsync(Guid housingId, Guid itemId, TransferHousingWarehouseItemStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result<HousingWarehouseItemHousingTransferResponse>> TransferUnusedToHousingAsync(Guid housingId, Guid itemId, TransferHousingWarehouseItemToHousingRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveItemAsync(Guid housingId, Guid itemId, ArchiveHousingWarehouseItemRequest request, CancellationToken cancellationToken = default);
}
