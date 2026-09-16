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
    string RowVersion,
    IReadOnlyList<HousingRoomResponse>? Rooms = null);

public sealed record HousingRoomUpsertRequest(string Name, int Capacity, string? RowVersion);

public sealed record ArchiveHousingRoomRequest(string Reason, string RowVersion);

public sealed record HousingRoomResponse(
    Guid Id,
    Guid HousingId,
    string Name,
    int Capacity,
    int CurrentOccupancy,
    int AvailableCapacity,
    string RowVersion,
    IReadOnlyList<RoomOccupantResponse> Occupants);

public sealed record RoomOccupantResponse(
    Guid OccupancyPeriodId,
    Guid RoomId,
    Guid HousingId,
    Guid EmployeeId,
    Guid? RiderProfileId,
    string PersonType,
    string? IqamaNo,
    string EmployeeNameAr,
    string? EmployeeNameEn,
    DateOnly EffectiveFrom,
    string? MoveInReason,
    string? SourceReference);

public sealed record AssignRoomEmployeeRequest(
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    string? MoveInReason,
    string? SourceReference);

public sealed record AssignRoomRiderRequest(
    Guid RiderProfileId,
    DateOnly EffectiveFrom,
    string? MoveInReason,
    string? SourceReference);

public sealed record MoveRoomOccupantRequest(Guid DestinationRoomId, DateOnly EffectiveFrom, string? Reason);

public sealed record RemoveRoomOccupantRequest(DateOnly EffectiveTo, string Reason);

// Compatibility contract for the former housing-level resident endpoint. RoomId is now mandatory.
public sealed record AssignHousingResidentRequest(
    Guid RoomId,
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    string? MoveInReason,
    string? SourceReference);

public sealed record AssignHousingSupervisorRequest(
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    string? AssignmentReason);

public sealed record HousingPeriodResponse(
    Guid Id,
    Guid HousingId,
    Guid? RoomId,
    string? RoomName,
    Guid EmployeeId,
    Guid? RiderProfileId,
    string PersonType,
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

    Task<Result<IReadOnlyList<HousingRoomResponse>>> GetRoomsAsync(Guid housingId, CancellationToken cancellationToken = default);
    Task<Result<HousingRoomResponse>> GetRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<Result<HousingRoomResponse>> UpsertRoomAsync(Guid? housingId, Guid? roomId, HousingRoomUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveRoomAsync(Guid roomId, ArchiveHousingRoomRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoomOccupantResponse>> AssignEmployeeToRoomAsync(Guid roomId, AssignRoomEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoomOccupantResponse>> AssignRiderToRoomAsync(Guid roomId, AssignRoomRiderRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoomOccupantResponse>> MoveOccupantAsync(Guid occupancyPeriodId, MoveRoomOccupantRequest request, CancellationToken cancellationToken = default);
    Task<Result> RemoveOccupantAsync(Guid occupancyPeriodId, RemoveRoomOccupantRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetResidentsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignResidentAsync(Guid housingId, AssignHousingResidentRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseResidenceAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetSupervisorsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignSupervisorAsync(Guid housingId, AssignHousingSupervisorRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseSupervisorAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);
}
