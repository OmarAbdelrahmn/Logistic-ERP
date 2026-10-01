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
    IReadOnlyList<HousingRoomResponse>? Rooms = null,
    IReadOnlyList<HousingFloorResponse>? Floors = null);

public sealed record HousingRoomUpsertRequest(string Name, int Capacity, string? RowVersion, Guid? FloorId = null, string? Notes = null);

public sealed record ArchiveHousingRoomRequest(string Reason, string RowVersion);

public sealed record HousingRoomResponse(
    Guid Id,
    Guid HousingId,
    string Name,
    int Capacity,
    int CurrentOccupancy,
    int AvailableCapacity,
    string RowVersion,
    IReadOnlyList<RoomOccupantResponse> Occupants,
    Guid FloorId,
    string? Notes,
    IReadOnlyList<HousingEquipmentResponse> Equipment,
    IReadOnlyList<HousingExternalOccupantResponse> ExternalOccupants,
    IReadOnlyList<HousingPendingOccupantResponse> PendingOccupants);

public sealed record HousingFloorUpsertRequest(string Name, string? RowVersion);
public sealed record HousingEquipmentUpsertRequest(string Name, int Quantity, string? RowVersion);
public sealed record HousingExternalOccupantUpsertRequest(string Name, Guid? RoomId, string? RowVersion);
public sealed record HousingEquipmentResponse(Guid Id, string Name, int Quantity, string RowVersion);
public sealed record HousingExternalOccupantResponse(Guid Id, Guid RoomId, string Name, string RowVersion);
public sealed record HousingPendingOccupantResponse(Guid Id, Guid RoomId, string IqamaNo, string Name, int SourceRow, string RowVersion);
public sealed record ResolvePendingOccupantRequest(DateOnly EffectiveFrom);
public sealed record HousingFloorResponse(Guid Id, Guid HousingId, string Name, string RowVersion,
    IReadOnlyList<HousingEquipmentResponse> Equipment,
    IReadOnlyList<HousingEquipmentResponse> TotalEquipment,
    IReadOnlyList<HousingRoomResponse> Rooms,
    int TotalCapacity,
    int CurrentOccupancy,
    int AvailableCapacity);

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

public sealed record AssignRoomByIqamaRequest(
    string IqamaNo,
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

public sealed record HousingStayReportRequest(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    Guid? HousingId = null,
    int Page = 1,
    int PageSize = 100);

public sealed record HousingStayReportRow(
    Guid RecordId,
    string RecordType,
    Guid HousingId,
    string HousingCode,
    string HousingNameAr,
    string HousingNameEn,
    Guid RoomId,
    string RoomName,
    Guid? FloorId,
    string? FloorName,
    Guid? EmployeeId,
    Guid? RiderProfileId,
    string? IqamaNo,
    string NameAr,
    string? NameEn,
    DateOnly? MoveInDate,
    DateOnly? MoveOutDate,
    bool IsCurrentlyInside,
    int? TotalStayDays,
    int? DaysInSelectedPeriod,
    string? MoveInReason,
    string? MoveOutReason,
    string? SourceReference);

public sealed record HousingStayReportResponse(
    DateOnly? FromDate,
    DateOnly? ToDate,
    DateOnly AsOfDate,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<HousingStayReportRow> Items);

public interface IHousingService
{
    Task<Result<HousingStayReportResponse>> GetStayReportAsync(HousingStayReportRequest request, CancellationToken cancellationToken = default);
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
    Task<Result<RoomOccupantResponse>> AssignByIqamaToRoomAsync(Guid roomId, AssignRoomByIqamaRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoomOccupantResponse>> MoveOccupantAsync(Guid occupancyPeriodId, MoveRoomOccupantRequest request, CancellationToken cancellationToken = default);
    Task<Result> RemoveOccupantAsync(Guid occupancyPeriodId, RemoveRoomOccupantRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetResidentsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignResidentAsync(Guid housingId, AssignHousingResidentRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseResidenceAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> GetSupervisorsAsync(Guid housingId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HousingPeriodResponse>>> AssignSupervisorAsync(Guid housingId, AssignHousingSupervisorRequest request, CancellationToken cancellationToken = default);
    Task<Result> CloseSupervisorAsync(Guid periodId, ClosePeriodRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<HousingFloorResponse>>> GetFloorsAsync(Guid housingId, CancellationToken cancellationToken = default);
    Task<Result<HousingFloorResponse>> UpsertFloorAsync(Guid housingId, Guid? floorId, HousingFloorUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteFloorAsync(Guid floorId, ArchiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<HousingEquipmentResponse>> UpsertEquipmentAsync(Guid? floorId, Guid? roomId, Guid? equipmentId, HousingEquipmentUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<Result<HousingExternalOccupantResponse>> UpsertExternalOccupantAsync(Guid? occupantId, Guid roomId, HousingExternalOccupantUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteExternalOccupantAsync(Guid occupantId, CancellationToken cancellationToken = default);
    Task<Result<RoomOccupantResponse>> ResolvePendingOccupantAsync(Guid pendingId, DateOnly effectiveFrom, CancellationToken cancellationToken = default);
    Task<Result> DeletePendingOccupantAsync(Guid pendingId, CancellationToken cancellationToken = default);
}
