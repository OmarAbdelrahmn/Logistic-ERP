using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed partial class VehicleDailyDistanceService
{
    public async Task<Result<VehicleDistancePeriodReportResponse>> GetVehiclePeriodReportAsync(
        Guid vehicleId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesRead, null, cancellationToken))
            return Result.Failure<VehicleDistancePeriodReportResponse>(FleetErrors.Forbidden);
        if (!ValidReportPeriod(fromDate, toDate))
            return Result.Failure<VehicleDistancePeriodReportResponse>(FleetErrors.InvalidDistanceReportPeriod);

        var vehicle = await dbContext.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null)
            return Result.Failure<VehicleDistancePeriodReportResponse>(FleetErrors.NotFound);

        var distances = await dbContext.VehicleDailyDistances.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.WorkDate >= fromDate && x.WorkDate <= toDate)
            .ToDictionaryAsync(x => x.WorkDate, cancellationToken);
        var cityNames = await GetOperatingCityNamesAsync(
            vehicle.OperatingCityId.HasValue ? [vehicle.OperatingCityId.Value] : [], cancellationToken);
        var dates = ReportDates(fromDate, toDate);
        var days = dates.Select(date => MapReportDay(date, distances.GetValueOrDefault(date))).ToArray();
        return Result.Success(new VehicleDistancePeriodReportResponse(
            MapReportVehicle(vehicle, cityNames), fromDate, toDate, dates.Length,
            days.Count(x => x.HasDistance), days.Count(x => x.GpsDistanceKm.HasValue),
            days.Count(x => x.ManualDistanceKm.HasValue),
            days.Count(x => x.AppliedSource == VehicleDailyDistanceSource.Manual),
            days.Count(x => !x.HasDistance), days.Sum(x => x.GpsDistanceKm ?? 0m),
            days.Sum(x => x.ManualDistanceKm ?? 0m), days.Sum(x => x.AppliedDistanceKm), days));
    }

    public async Task<Result<VehicleMissingDistanceReportResponse>> GetMissingRecordsReportAsync(
        DateOnly fromDate, DateOnly toDate, string? search, Guid? operatingCityId,
        VehicleType? vehicleType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesRead, null, cancellationToken))
            return Result.Failure<VehicleMissingDistanceReportResponse>(FleetErrors.Forbidden);
        if (!ValidReportPeriod(fromDate, toDate))
            return Result.Failure<VehicleMissingDistanceReportResponse>(FleetErrors.InvalidDistanceReportPeriod);
        if (vehicleType.HasValue && !Enum.IsDefined(vehicleType.Value))
            return Result.Failure<VehicleMissingDistanceReportResponse>(FleetErrors.InvalidRequest);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;
        var vehicles = dbContext.Vehicles.AsNoTracking()
            .Where(x => x.CurrentOperationalStatus == VehicleOperationalStatus.Assigned);
        if (operatingCityId.HasValue) vehicles = vehicles.Where(x => x.OperatingCityId == operatingCityId.Value);
        if (vehicleType.HasValue) vehicles = vehicles.Where(x => x.VehicleType == vehicleType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = FleetServiceSupport.NormalizeIdentifier(search);
            vehicles = vehicles.Where(x => x.NormalizedAssetNumber.Contains(normalized) ||
                (x.NormalizedPlateNumberAr != null && x.NormalizedPlateNumberAr.Contains(normalized)) ||
                (x.NormalizedPlateNumberEn != null && x.NormalizedPlateNumberEn.Contains(normalized)));
        }

        var recordedDistances = dbContext.VehicleDailyDistances.AsNoTracking().Where(x =>
            x.WorkDate >= fromDate && x.WorkDate <= toDate && x.AppliedSource != VehicleDailyDistanceSource.None);
        var workingVehicleCount = await vehicles.CountAsync(cancellationToken);
        var recordedDayCount = await recordedDistances
            .Where(x => vehicles.Any(v => v.Id == x.VehicleId)).LongCountAsync(cancellationToken);
        var incompleteVehicles = vehicles.Where(v => recordedDistances.Count(x => x.VehicleId == v.Id) < totalDays);
        var totalCount = await incompleteVehicles.CountAsync(cancellationToken);
        // Use a long offset to prevent page overflow from wrapping into the first page.
        var offset = ((long)page - 1) * pageSize;
        var selectedVehicles = offset >= totalCount ? [] : await incompleteVehicles
            .OrderBy(x => x.AssetNumber).ThenBy(x => x.Id)
            .Skip((int)offset).Take(pageSize).ToArrayAsync(cancellationToken);
        var ids = selectedVehicles.Select(x => x.Id).ToArray();
        var recordedDates = ids.Length == 0 ? [] : await recordedDistances
            .Where(x => ids.Contains(x.VehicleId)).Select(x => new { x.VehicleId, x.WorkDate })
            .ToArrayAsync(cancellationToken);
        var lookup = recordedDates.ToLookup(x => x.VehicleId, x => x.WorkDate);
        var cityNames = await GetOperatingCityNamesAsync(
            selectedVehicles.Where(x => x.OperatingCityId.HasValue).Select(x => x.OperatingCityId!.Value),
            cancellationToken);
        var dates = ReportDates(fromDate, toDate);
        var items = selectedVehicles.Select(vehicle =>
        {
            var present = lookup[vehicle.Id].ToHashSet();
            var missing = dates.Where(date => !present.Contains(date)).ToArray();
            return new VehicleMissingDistanceReportItemResponse(
                MapReportVehicle(vehicle, cityNames), present.Count, missing.Length, missing);
        }).ToArray();
        return Result.Success(new VehicleMissingDistanceReportResponse(
            fromDate, toDate, totalDays, VehicleOperationalStatus.Assigned, workingVehicleCount,
            totalCount, (long)workingVehicleCount * totalDays - recordedDayCount, page, pageSize, items));
    }

    private static bool ValidReportPeriod(DateOnly fromDate, DateOnly toDate) =>
        fromDate != default && toDate != default && toDate >= fromDate && toDate.DayNumber - fromDate.DayNumber < 366;

    private static DateOnly[] ReportDates(DateOnly fromDate, DateOnly toDate) =>
        Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1).Select(fromDate.AddDays).ToArray();

    private static VehicleDistanceReportVehicleResponse MapReportVehicle(Vehicle vehicle, Dictionary<Guid, string> cityNames) =>
        new(vehicle.Id, vehicle.AssetNumber, vehicle.PlateNumberAr, vehicle.PlateNumberEn,
            vehicle.VehicleType, vehicle.CurrentOperationalStatus, vehicle.OperatingCityId,
            vehicle.OperatingCityId.HasValue ? cityNames.GetValueOrDefault(vehicle.OperatingCityId.Value) : null);

    private static VehicleDistanceReportDayResponse MapReportDay(DateOnly date, VehicleDailyDistance? distance) =>
        new(distance?.Id, date, distance is not null,
            distance is not null && distance.AppliedSource != VehicleDailyDistanceSource.None,
            distance?.GpsDistanceKm, distance?.GpsPlateNumber, distance?.ManualOdometerReading,
            distance?.ManualBaselineOdometerReading, distance?.ManualDistanceKm,
            distance?.AppliedDistanceKm ?? 0m, distance?.AppliedSource ?? VehicleDailyDistanceSource.None,
            distance?.EffectiveOdometerAfterKm, distance?.GpsImportedAtUtc, distance?.LastGpsImportId,
            distance?.GpsImportedByUserId, distance?.ManualEnteredAtUtc, distance?.ManualEnteredByUserId,
            distance?.ManualNotes);
}
