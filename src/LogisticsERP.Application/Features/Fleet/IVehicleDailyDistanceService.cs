using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Features.Fleet;

public interface IVehicleDailyDistanceService
{
    Task<Result<VehicleDistancePeriodReportResponse>> GetVehiclePeriodReportAsync(
        Guid vehicleId, DateOnly fromDate, DateOnly toDate,
        CancellationToken cancellationToken = default);

    Task<Result<VehicleMissingDistanceReportResponse>> GetMissingRecordsReportAsync(
        DateOnly fromDate, DateOnly toDate, string? search, Guid? operatingCityId,
        VehicleType? vehicleType, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<VehicleDailyDistancePageResponse>> GetDailyAsync(
        DateOnly workDate,
        string? search,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<VehicleDailyDistanceResponse>> UpsertManualAsync(
        Guid vehicleId,
        DateOnly workDate,
        UpsertManualVehicleDistanceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<GpsDistanceImportResponse>> ImportGpsAsync(
        PrivateFileUpload file,
        DateOnly? expectedWorkDate,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<GpsDistanceImportHistoryResponse>>> GetImportsAsync(
        DateOnly? workDate,
        CancellationToken cancellationToken = default);
}
