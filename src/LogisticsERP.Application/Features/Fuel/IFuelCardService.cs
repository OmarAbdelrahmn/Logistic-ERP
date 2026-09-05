using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fuel;

public interface IFuelCardService
{
    Task<Result<FuelCardPageResponse>> GetCardsAsync(
        string? search,
        string? provider,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<FuelCardResponse>> GetCardAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<FuelCardResponse>> CreateCardAsync(
        CreateFuelCardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FuelCardAssignmentResponse>>> GetAssignmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<FuelCardAssignmentResponse>> AssignRiderAsync(
        Guid id,
        AssignFuelCardRiderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<FuelCardAssignmentResponse>> StopRiderAsync(
        Guid id,
        StopFuelCardRiderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<FuelMonthlyUsagePageResponse>> GetMonthlyUsageAsync(
        DateOnly month,
        string? search,
        string? provider,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<FuelImportResponse>> ImportAsync(
        PrivateFileUpload file,
        DateOnly? expectedMonth,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FuelImportHistoryResponse>>> GetImportsAsync(
        DateOnly? month,
        string? provider,
        CancellationToken cancellationToken = default);
}
