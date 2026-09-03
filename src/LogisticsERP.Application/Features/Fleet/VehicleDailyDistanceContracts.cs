using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public sealed record UpsertManualVehicleDistanceRequest(
    long OdometerReading,
    long? BaselineOdometerReading,
    string? Notes,
    string? RowVersion);

public sealed record VehicleDailyDistanceResponse(
    Guid? Id,
    Guid VehicleId,
    DateOnly WorkDate,
    string AssetNumber,
    string? PlateNumberAr,
    string? PlateNumberEn,
    long CurrentOdometer,
    decimal VehicleTrackedDistanceKm,
    decimal? GpsDistanceKm,
    long? ManualOdometerReading,
    long? ManualBaselineOdometerReading,
    decimal? ManualDistanceKm,
    decimal AppliedDistanceKm,
    VehicleDailyDistanceSource AppliedSource,
    DateTimeOffset? GpsImportedAtUtc,
    DateTimeOffset? ManualEnteredAtUtc,
    string? ManualNotes,
    string? RowVersion);

public sealed record VehicleDailyDistancePageResponse(
    IReadOnlyList<VehicleDailyDistanceResponse> Items,
    DateOnly WorkDate,
    int Page,
    int PageSize,
    int TotalCount,
    int GpsCount,
    int ManualFallbackCount,
    int MissingCount,
    decimal AppliedTotalKm);

public sealed record GpsDistanceImportRowError(
    int RowNumber,
    string? PlateNumber,
    string Code,
    string Message);

public sealed record GpsDistanceImportResponse(
    Guid ImportId,
    DateOnly WorkDate,
    string OriginalFileName,
    string Sha256Checksum,
    int TotalVehicleRows,
    int GpsRows,
    int NoGpsRows,
    int MatchedRows,
    int CreatedRows,
    int UpdatedRows,
    int UnmatchedRows,
    int InvalidRows,
    IReadOnlyList<GpsDistanceImportRowError> Errors,
    DateTimeOffset ImportedAtUtc);

public sealed record GpsDistanceImportHistoryResponse(
    Guid Id,
    DateOnly WorkDate,
    string OriginalFileName,
    string Sha256Checksum,
    int TotalVehicleRows,
    int GpsRows,
    int NoGpsRows,
    int MatchedRows,
    int CreatedRows,
    int UpdatedRows,
    int UnmatchedRows,
    int InvalidRows,
    DateTimeOffset ImportedAtUtc,
    Guid? ImportedByUserId);
