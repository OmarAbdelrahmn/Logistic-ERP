using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Telecom;

public interface IPlaceService
{
    Task<Result<IReadOnlyList<PlaceResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PlaceResponse>> CreateAsync(SavePlaceRequest request, CancellationToken cancellationToken = default);
    Task<Result<PlaceResponse>> UpdateAsync(Guid id, SavePlaceRequest request, CancellationToken cancellationToken = default);
}
