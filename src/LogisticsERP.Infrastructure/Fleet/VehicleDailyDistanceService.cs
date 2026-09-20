using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleDailyDistanceService(
    ApplicationDbContext dbContext,
    FleetServiceSupport support) : IVehicleDailyDistanceService
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    public async Task<Result<VehicleDailyDistancePageResponse>> GetDailyAsync(
        DateOnly workDate,
        string? search,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesRead, null, cancellationToken))
        {
            return Result.Failure<VehicleDailyDistancePageResponse>(FleetErrors.Forbidden);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 100 : pageSize, 1, 300);
        var normalizedSource = source?.Trim().ToLowerInvariant();
        if (normalizedSource is not null and not "gps" and not "manual" and not "missing")
        {
            return Result.Failure<VehicleDailyDistancePageResponse>(FleetErrors.InvalidRequest);
        }

        var query =
            from vehicle in dbContext.Vehicles.AsNoTracking()
            join distance in dbContext.VehicleDailyDistances.AsNoTracking().Where(x => x.WorkDate == workDate)
                on vehicle.Id equals distance.VehicleId into distanceGroup
            from distance in distanceGroup.DefaultIfEmpty()
            select new { Vehicle = vehicle, Distance = distance };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = FleetServiceSupport.NormalizeIdentifier(search);
            query = query.Where(x =>
                x.Vehicle.NormalizedAssetNumber.Contains(normalizedSearch)
                || x.Vehicle.NormalizedPlateNumberAr != null && x.Vehicle.NormalizedPlateNumberAr.Contains(normalizedSearch)
                || x.Vehicle.NormalizedPlateNumberEn != null && x.Vehicle.NormalizedPlateNumberEn.Contains(normalizedSearch));
        }

        var summaryQuery = query;
        var gpsCount = await summaryQuery.CountAsync(x => x.Distance != null && x.Distance.AppliedSource == VehicleDailyDistanceSource.Gps, cancellationToken);
        var manualCount = await summaryQuery.CountAsync(x => x.Distance != null && x.Distance.AppliedSource == VehicleDailyDistanceSource.Manual, cancellationToken);
        var missingCount = await summaryQuery.CountAsync(x => x.Distance == null || x.Distance.AppliedSource == VehicleDailyDistanceSource.None, cancellationToken);
        var appliedTotal = await summaryQuery
            .Where(x => x.Distance != null)
            .SumAsync(x => (decimal?)x.Distance!.AppliedDistanceKm, cancellationToken) ?? 0m;

        query = normalizedSource switch
        {
            "gps" => query.Where(x => x.Distance != null && x.Distance.AppliedSource == VehicleDailyDistanceSource.Gps),
            "manual" => query.Where(x => x.Distance != null && x.Distance.AppliedSource == VehicleDailyDistanceSource.Manual),
            "missing" => query.Where(x => x.Distance == null || x.Distance.AppliedSource == VehicleDailyDistanceSource.None),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Vehicle.AssetNumber)
            .ThenBy(x => x.Vehicle.PlateNumberAr)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        var items = rows.Select(x => Map(x.Vehicle, x.Distance, workDate)).ToArray();

        return Result.Success(new VehicleDailyDistancePageResponse(
            items,
            workDate,
            page,
            pageSize,
            totalCount,
            gpsCount,
            manualCount,
            missingCount,
            appliedTotal));
    }

    public async Task<Result<VehicleDailyDistanceResponse>> UpsertManualAsync(
        Guid vehicleId,
        DateOnly workDate,
        UpsertManualVehicleDistanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesManage, null, cancellationToken))
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.Forbidden);
        }

        var actor = support.UserId;
        if (!actor.HasValue)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.CurrentUserUnavailable);
        }

        if (request.OdometerReading < 0)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.InvalidManualOdometer);
        }

        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.NotFound);
        }

        var current = await dbContext.VehicleDailyDistances
            .SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.WorkDate == workDate, cancellationToken);

        if (current is not null && !FleetServiceSupport.MatchesRowVersion(current.RowVersion, request.RowVersion))
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.ConcurrencyConflict);
        }

        var isNew = current is null;
        current ??= new VehicleDailyDistance
        {
            Id = Guid.CreateVersion7(),
            VehicleId = vehicleId,
            WorkDate = workDate
        };

        current.ManualOdometerReading = request.OdometerReading;
        current.ManualEnteredAtUtc = support.UtcNow;
        current.ManualEnteredByUserId = actor.Value;
        current.ManualNotes = FleetServiceSupport.TrimOrNull(request.Notes);

        if (isNew)
        {
            dbContext.VehicleDailyDistances.Add(current);
        }

        var hasEarlierDailyRecord = await dbContext.VehicleDailyDistances
            .AnyAsync(x => x.VehicleId == vehicleId && x.WorkDate < workDate, cancellationToken);
        var explicitBaseline = hasEarlierDailyRecord ? null : request.BaselineOdometerReading;
        if (!hasEarlierDailyRecord
            && !explicitBaseline.HasValue
            && !await HasBaselineBeforeAsync(vehicle, workDate, cancellationToken))
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.ManualBaselineRequired);
        }

        var recalculation = await RecalculateMileageAsync(vehicle, workDate, explicitBaseline, cancellationToken);
        if (recalculation.IsFailure)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(recalculation.Error);
        }

        var recordedAt = EndOfWorkDateUtc(workDate);
        dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
        {
            VehicleId = vehicle.Id,
            Reading = request.OdometerReading,
            RecordedAtUtc = recordedAt,
            SourceType = VehicleOdometerSourceType.Manual,
            SourceEntityId = current.Id,
            Notes = $"قراءة العداد اليدوية للمسافة اليومية بتاريخ {workDate:yyyy-MM-dd}."
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.ConcurrencyConflict);
        }

        return Result.Success(Map(vehicle, current, workDate));
    }

    public async Task<Result<GpsDistanceImportResponse>> ImportGpsAsync(
        PrivateFileUpload file,
        DateOnly? expectedWorkDate,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesImport, null, cancellationToken))
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.Forbidden);
        }

        var actor = support.UserId;
        if (!actor.HasValue)
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.CurrentUserUnavailable);
        }

        var extension = Path.GetExtension(file.OriginalFileName).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > 10 * 1024 * 1024
            || extension is not ".xls" and not ".xlsx" and not ".htm" and not ".html" and not ".zip")
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.InvalidGpsFile);
        }

        await using var workbook = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await file.Content.CopyToAsync(workbook, cancellationToken);
        var checksum = Convert.ToHexString(SHA256.HashData(workbook.ToArray()));
        workbook.Position = 0;

        ParsedGpsDistanceReport report;
        try
        {
            report = extension == ".zip"
                ? GpsDistanceSpreadsheetParser.ParseArchive(workbook)
                : GpsDistanceSpreadsheetParser.Parse(workbook);
        }
        catch (GpsFramesetMissingSheetException)
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.GpsFramesetMissingSheet);
        }
        catch (Exception exception) when (exception is InvalidDataException
            or NotSupportedException
            or FormatException
            or System.Text.RegularExpressions.RegexMatchTimeoutException
            or ExcelDataReader.Exceptions.HeaderException)
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.InvalidGpsFile);
        }

        if (expectedWorkDate.HasValue && expectedWorkDate.Value != report.WorkDate)
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.GpsDateMismatch);
        }

        if (await dbContext.VehicleDailyDistanceImports.AnyAsync(
                x => x.WorkDate == report.WorkDate && x.Sha256Checksum == checksum,
                cancellationToken))
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.DuplicateGpsImport);
        }

        var vehicles = await dbContext.Vehicles.ToArrayAsync(cancellationToken);
        var vehicleMatches = BuildVehiclePlateIndex(vehicles);
        var existingRecords = await dbContext.VehicleDailyDistances
            .Where(x => x.WorkDate == report.WorkDate)
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);
        var import = new VehicleDailyDistanceImport
        {
            Id = Guid.CreateVersion7(),
            WorkDate = report.WorkDate,
            PeriodStartUtc = report.PeriodStartUtc,
            PeriodEndUtc = report.PeriodEndUtc,
            OriginalFileName = Path.GetFileName(file.OriginalFileName),
            Sha256Checksum = checksum,
            TotalVehicleRows = report.Rows.Count,
            GpsRows = report.Rows.Count(x => x.HasGpsDistance),
            NoGpsRows = report.Rows.Count(x => !x.HasGpsDistance && x.ErrorCode is null),
            CreatedAtUtc = support.UtcNow,
            CreatedByUserId = actor.Value
        };
        var errors = new List<GpsDistanceImportRowError>();
        var seenPlates = new HashSet<string>(StringComparer.Ordinal);
        var affectedVehicles = new Dictionary<Guid, Vehicle>();

        foreach (var row in report.Rows)
        {
            if (row.ErrorCode is not null)
            {
                import.InvalidRows++;
                errors.Add(new GpsDistanceImportRowError(row.RowNumber, row.PlateNumber, row.ErrorCode, row.ErrorMessage!));
                continue;
            }

            if (!row.HasGpsDistance)
            {
                continue;
            }

            var canonicalPlate = PlateNumberRules.CanonicalKey(row.PlateNumber);
            if (!seenPlates.Add(canonicalPlate))
            {
                import.InvalidRows++;
                errors.Add(new GpsDistanceImportRowError(row.RowNumber, row.PlateNumber, "duplicate_plate", "رقم اللوحة مكرر داخل ملف GPS."));
                continue;
            }

            var matchedVehicles = PlateNumberRules.BuildLookupKeys(row.PlateNumber)
                .Where(vehicleMatches.ContainsKey)
                .SelectMany(key => vehicleMatches[key])
                .DistinctBy(vehicle => vehicle.Id)
                .ToArray();
            if (matchedVehicles.Length == 0)
            {
                import.UnmatchedRows++;
                errors.Add(new GpsDistanceImportRowError(row.RowNumber, row.PlateNumber, "vehicle_not_found", "لم يتم العثور على مركبة مطابقة لرقم اللوحة."));
                continue;
            }

            if (matchedVehicles.Length > 1)
            {
                import.InvalidRows++;
                errors.Add(new GpsDistanceImportRowError(row.RowNumber, row.PlateNumber, "ambiguous_plate", "رقم اللوحة يطابق أكثر من مركبة ويحتاج إلى تصحيح بيانات المركبات."));
                continue;
            }

            var vehicle = matchedVehicles[0];
            var isNew = !existingRecords.TryGetValue(vehicle.Id, out var distance);
            distance ??= new VehicleDailyDistance
            {
                Id = Guid.CreateVersion7(),
                VehicleId = vehicle.Id,
                WorkDate = report.WorkDate
            };
            distance.GpsDistanceKm = decimal.Round(row.DistanceKm!.Value, 2, MidpointRounding.AwayFromZero);
            distance.GpsPlateNumber = row.PlateNumber.Trim();
            distance.LastGpsImportId = import.Id;
            distance.GpsImportedAtUtc = support.UtcNow;
            distance.GpsImportedByUserId = actor.Value;
            affectedVehicles[vehicle.Id] = vehicle;

            if (isNew)
            {
                dbContext.VehicleDailyDistances.Add(distance);
                existingRecords.Add(vehicle.Id, distance);
                import.CreatedRows++;
            }
            else
            {
                import.UpdatedRows++;
            }

            import.MatchedRows++;
        }

        foreach (var vehicle in affectedVehicles.Values)
        {
            var recalculation = await RecalculateMileageAsync(vehicle, report.WorkDate, null, cancellationToken);
            if (recalculation.IsFailure)
            {
                return Result.Failure<GpsDistanceImportResponse>(recalculation.Error);
            }

            var distance = existingRecords[vehicle.Id];
            var recordedAt = report.PeriodEndUtc ?? EndOfWorkDateUtc(report.WorkDate);
            dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
            {
                VehicleId = vehicle.Id,
                Reading = ToWholeOdometer(distance.EffectiveOdometerAfterKm),
                RecordedAtUtc = recordedAt,
                SourceType = VehicleOdometerSourceType.Gps,
                SourceEntityId = distance.Id,
                Notes = $"مسافة GPS المعتمدة بتاريخ {report.WorkDate:yyyy-MM-dd}: {distance.AppliedDistanceKm:0.00} كم."
            });
        }

        import.RowErrorsJson = JsonSerializer.Serialize(errors);
        dbContext.VehicleDailyDistanceImports.Add(import);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure<GpsDistanceImportResponse>(FleetErrors.Conflict);
        }

        return Result.Success(new GpsDistanceImportResponse(
            import.Id,
            import.WorkDate,
            import.OriginalFileName,
            import.Sha256Checksum,
            import.TotalVehicleRows,
            import.GpsRows,
            import.NoGpsRows,
            import.MatchedRows,
            import.CreatedRows,
            import.UpdatedRows,
            import.UnmatchedRows,
            import.InvalidRows,
            errors,
            import.CreatedAtUtc));
    }

    public async Task<Result<IReadOnlyList<GpsDistanceImportHistoryResponse>>> GetImportsAsync(
        DateOnly? workDate,
        CancellationToken cancellationToken = default)
    {
        if (!await support.HasPermissionAsync(PermissionKeys.Fleet.DailyDistancesRead, null, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<GpsDistanceImportHistoryResponse>>(FleetErrors.Forbidden);
        }

        var query = dbContext.VehicleDailyDistanceImports.AsNoTracking().AsQueryable();
        if (workDate.HasValue)
        {
            query = query.Where(x => x.WorkDate == workDate.Value);
        }

        var imports = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new GpsDistanceImportHistoryResponse(
                x.Id,
                x.WorkDate,
                x.OriginalFileName,
                x.Sha256Checksum,
                x.TotalVehicleRows,
                x.GpsRows,
                x.NoGpsRows,
                x.MatchedRows,
                x.CreatedRows,
                x.UpdatedRows,
                x.UnmatchedRows,
                x.InvalidRows,
                x.CreatedAtUtc,
                x.CreatedByUserId))
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GpsDistanceImportHistoryResponse>>(imports);
    }

    private async Task<bool> HasBaselineBeforeAsync(
        Vehicle vehicle,
        DateOnly workDate,
        CancellationToken cancellationToken)
    {
        var startUtc = StartOfWorkDateUtc(workDate);
        var hasHistoricalReading = await dbContext.VehicleOdometerReadings
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicle.Id && x.RecordedAtUtc < startUtc)
            .AnyAsync(cancellationToken);

        return hasHistoricalReading
            || !vehicle.LastOdometerAtUtc.HasValue
            || vehicle.LastOdometerAtUtc.Value < startUtc;
    }

    private async Task<Result> RecalculateMileageAsync(
        Vehicle vehicle,
        DateOnly fromWorkDate,
        decimal? explicitBaseline,
        CancellationToken cancellationToken)
    {
        var persisted = await dbContext.VehicleDailyDistances
            .Where(x => x.VehicleId == vehicle.Id && x.WorkDate >= fromWorkDate)
            .OrderBy(x => x.WorkDate)
            .ToListAsync(cancellationToken);
        var added = dbContext.ChangeTracker.Entries<VehicleDailyDistance>()
            .Where(x => x.State == EntityState.Added
                && x.Entity.VehicleId == vehicle.Id
                && x.Entity.WorkDate >= fromWorkDate)
            .Select(x => x.Entity);
        var affected = persisted
            .Concat(added)
            .DistinctBy(x => x.Id)
            .OrderBy(x => x.WorkDate)
            .ToArray();

        var previousAffectedTotal = affected.Sum(x => x.AppliedDistanceKm);
        var runningOdometer = explicitBaseline
            ?? Math.Max(0m, vehicle.TrackedDistanceKm - previousAffectedTotal);

        if (!VehicleDailyDistanceRules.TryRecalculate(affected, runningOdometer, out runningOdometer))
        {
            return Result.Failure(FleetErrors.InvalidManualOdometer);
        }

        if (affected.Length > 0)
        {
            var latestRecordedAt = EndOfWorkDateUtc(affected[^1].WorkDate);
            VehicleMileageRules.ApplyEffectiveMileage(vehicle, runningOdometer, latestRecordedAt);
        }

        return Result.Success();
    }

    private static long ToWholeOdometer(decimal value) => decimal.ToInt64(decimal.Floor(value));

    private static VehicleDailyDistanceResponse Map(Vehicle vehicle, VehicleDailyDistance? distance, DateOnly workDate) =>
        new(
            distance?.Id,
            vehicle.Id,
            workDate,
            vehicle.AssetNumber,
            vehicle.PlateNumberAr,
            vehicle.PlateNumberEn,
            vehicle.CurrentOdometer,
            vehicle.TrackedDistanceKm,
            distance?.GpsDistanceKm,
            distance?.ManualOdometerReading,
            distance?.ManualBaselineOdometerReading,
            distance?.ManualDistanceKm,
            distance?.AppliedDistanceKm ?? 0m,
            distance?.AppliedSource ?? VehicleDailyDistanceSource.None,
            distance?.EffectiveOdometerAfterKm ?? vehicle.TrackedDistanceKm,
            distance?.GpsImportedAtUtc,
            distance?.LastGpsImportId,
            distance?.GpsImportedByUserId,
            distance?.ManualEnteredAtUtc,
            distance?.ManualEnteredByUserId,
            distance?.ManualNotes,
            distance is null ? null : FleetServiceSupport.EncodeRowVersion(distance.RowVersion));

    private static Dictionary<string, List<Vehicle>> BuildVehiclePlateIndex(IEnumerable<Vehicle> vehicles)
    {
        var index = new Dictionary<string, List<Vehicle>>(StringComparer.Ordinal);
        foreach (var vehicle in vehicles)
        {
            var values = new[]
            {
                vehicle.PlateNumberAr,
                vehicle.PlateNumberEn,
                vehicle.NormalizedPlateNumberAr,
                vehicle.NormalizedPlateNumberEn,
                JoinPlate(vehicle.PlateDigits, vehicle.PlateLettersAr),
                JoinPlate(vehicle.PlateDigits, vehicle.PlateLettersEn)
            };
            foreach (var key in values.SelectMany(PlateNumberRules.BuildLookupKeys).Distinct(StringComparer.Ordinal))
            {
                if (!index.TryGetValue(key, out var matches))
                {
                    matches = [];
                    index.Add(key, matches);
                }

                if (matches.All(match => match.Id != vehicle.Id))
                {
                    matches.Add(vehicle);
                }
            }
        }

        return index;
    }

    private static string? JoinPlate(string? digits, string? letters) =>
        string.IsNullOrWhiteSpace(digits) || string.IsNullOrWhiteSpace(letters)
            ? null
            : digits + letters;

    private static DateTimeOffset StartOfWorkDateUtc(DateOnly workDate) =>
        new DateTimeOffset(workDate.ToDateTime(TimeOnly.MinValue), RiyadhOffset).ToUniversalTime();

    private static DateTimeOffset EndOfWorkDateUtc(DateOnly workDate) =>
        new DateTimeOffset(workDate.ToDateTime(TimeOnly.MaxValue), RiyadhOffset).ToUniversalTime();
}
