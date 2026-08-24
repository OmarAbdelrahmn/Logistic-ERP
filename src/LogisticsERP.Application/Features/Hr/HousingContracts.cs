using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record HousingUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    Guid CityId,
    AddressRequest? Address,
    decimal? Latitude,
    decimal? Longitude,
    int TotalCapacity,
    string? ContactPhone,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    string Status,
    string? StatusReason,
    string? Notes,
    string? RowVersion);

public sealed record HousingResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    Guid CityId,
    string CityAr,
    AddressResponse Address,
    decimal? Latitude,
    decimal? Longitude,
    int TotalCapacity,
    int CurrentResidents,
    int AvailableCapacity,
    string? ContactPhone,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    string Status,
    string? StatusReason,
    string? Notes,
    string RowVersion);

public sealed record AssignHousingResidentRequest(
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    string? MoveInReason,
    string? SourceReference,
    bool CapacityOverrideUsed,
    string? CapacityOverrideReason);

public sealed record AssignHousingSupervisorRequest(
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    string? AssignmentReason);

public sealed record HousingPeriodResponse(
    Guid Id,
    Guid HousingId,
    Guid EmployeeId,
    string? IqamaNo,
    string EmployeeNameAr,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? StartReason,
    string? EndReason,
    bool CapacityOverrideUsed,
    string? CapacityOverrideReason);

public interface IHousingService
{
    Task<Result<IReadOnlyList<HousingResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<HousingResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<HousingResponse>> UpsertAsync(Guid? id, HousingUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, ArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetResidentsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignResidentAsync(Guid housingId, AssignHousingResidentRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseResidenceAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetSupervisorsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignSupervisorAsync(Guid housingId, AssignHousingSupervisorRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseSupervisorAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);
}
