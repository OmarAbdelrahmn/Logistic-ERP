using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public sealed record VehicleDistanceReportVehicleResponse(
    Guid VehicleId, string AssetNumber, string? PlateNumberAr, string? PlateNumberEn,
    VehicleType VehicleType, VehicleOperationalStatus CurrentOperationalStatus,
    Guid? OperatingCityId, string? OperatingCity, string? SerialNumber = null);

public sealed record VehicleDistanceReportDayResponse(
    Guid? Id, DateOnly WorkDate, bool HasRecord, bool HasDistance,
    decimal? GpsDistanceKm, string? GpsPlateNumber,
    long? ManualOdometerReading, decimal? ManualBaselineOdometerReading,
    decimal? ManualDistanceKm, decimal AppliedDistanceKm,
    VehicleDailyDistanceSource AppliedSource, decimal? EffectiveOdometerAfterKm,
    DateTimeOffset? GpsImportedAtUtc, Guid? LastGpsImportId, Guid? GpsImportedByUserId,
    DateTimeOffset? ManualEnteredAtUtc, Guid? ManualEnteredByUserId, string? ManualNotes);

public sealed record VehicleDistancePeriodReportResponse(
    VehicleDistanceReportVehicleResponse Vehicle, DateOnly FromDate, DateOnly ToDate,
    int TotalDays, int RecordedDays, int GpsDays, int ManualDays,
    int ManualFallbackDays, int MissingDays, decimal GpsTotalKm,
    decimal ManualTotalKm, decimal AppliedTotalKm,
    IReadOnlyList<VehicleDistanceReportDayResponse> Days);

public sealed record VehicleDistancePeriodReportsPageResponse(
    DateOnly FromDate, DateOnly ToDate, int Page, int PageSize, int TotalCount,
    IReadOnlyList<VehicleDistancePeriodReportResponse> Items);

public sealed record VehicleMissingDistanceReportItemResponse(
    VehicleDistanceReportVehicleResponse Vehicle, int RecordedDays, int MissingDays,
    IReadOnlyList<DateOnly> MissingDates);

public sealed record VehicleMissingDistanceReportResponse(
    DateOnly FromDate, DateOnly ToDate, int TotalDays,
    VehicleOperationalStatus WorkingStatus, int WorkingVehicleCount,
    int TotalCount, long TotalMissingDays, int Page, int PageSize,
    IReadOnlyList<VehicleMissingDistanceReportItemResponse> Items);
