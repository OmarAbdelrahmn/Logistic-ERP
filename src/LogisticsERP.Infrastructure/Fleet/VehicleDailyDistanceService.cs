using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        var manualRecords = await dbContext.VehicleDailyDistances
            .Where(x => x.VehicleId == vehicleId && x.ManualOdometerReading != null)
            .OrderBy(x => x.WorkDate)
            .ToListAsync(cancellationToken);
        var current = await dbContext.VehicleDailyDistances
            .SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.WorkDate == workDate, cancellationToken);

        if (current is not null && !FleetServiceSupport.MatchesRowVersion(current.RowVersion, request.RowVersion))
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.ConcurrencyConflict);
        }

        var previousManual = manualRecords.LastOrDefault(x => x.WorkDate < workDate);
        var nextManual = manualRecords.FirstOrDefault(x => x.WorkDate > workDate);
        var baseline = previousManual?.ManualOdometerReading
            ?? await ResolveInitialBaselineAsync(vehicle, workDate, request.BaselineOdometerReading, cancellationToken);

        if (!baseline.HasValue)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.ManualBaselineRequired);
        }

        if (request.OdometerReading < baseline.Value
            || nextManual?.ManualOdometerReading is { } nextReading && nextReading < request.OdometerReading)
        {
            return Result.Failure<VehicleDailyDistanceResponse>(FleetErrors.InvalidManualOdometer);
        }

        var isNew = current is null;
        current ??= new VehicleDailyDistance
        {
            VehicleId = vehicleId,
            WorkDate = workDate
        };

        current.ManualOdometerReading = request.OdometerReading;
        current.ManualBaselineOdometerReading = baseline.Value;
        current.ManualDistanceKm = VehicleDailyDistanceRules.CalculateManualDistance(baseline.Value, request.OdometerReading);
        current.ManualEnteredAtUtc = support.UtcNow;
        current.ManualEnteredByUserId = actor.Value;
        current.ManualNotes = FleetServiceSupport.TrimOrNull(request.Notes);
        ApplyEffectiveDistance(vehicle, current);

        if (isNew)
        {
            dbContext.VehicleDailyDistances.Add(current);
            manualRecords.Add(current);
        }

        var followingRecords = manualRecords
            .Where(x => x.WorkDate > workDate)
            .OrderBy(x => x.WorkDate)
            .ToArray();
        var runningBaseline = request.OdometerReading;
        foreach (var following in followingRecords)
        {
            following.ManualBaselineOdometerReading = runningBaseline;
            following.ManualDistanceKm = VehicleDailyDistanceRules.CalculateManualDistance(
                runningBaseline,
                following.ManualOdometerReading!.Value);
            runningBaseline = following.ManualOdometerReading.Value;
            ApplyEffectiveDistance(vehicle, following);
        }

        var recordedAt = EndOfWorkDateUtc(workDate);
        if (request.OdometerReading > vehicle.CurrentOdometer
            && (!vehicle.LastOdometerAtUtc.HasValue || recordedAt >= vehicle.LastOdometerAtUtc.Value))
        {
            vehicle.CurrentOdometer = request.OdometerReading;
            vehicle.LastOdometerAtUtc = recordedAt;
            dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
            {
                VehicleId = vehicle.Id,
                Reading = request.OdometerReading,
                RecordedAtUtc = recordedAt,
                SourceType = VehicleOdometerSourceType.Manual,
                SourceEntityId = current.Id,
                Notes = $"قراءة العداد اليدوية للمسافة اليومية بتاريخ {workDate:yyyy-MM-dd}."
            });
        }

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

            var canonicalPlate = CanonicalPlateKey(row.PlateNumber);
            if (!seenPlates.Add(canonicalPlate))
            {
                import.InvalidRows++;
                errors.Add(new GpsDistanceImportRowError(row.RowNumber, row.PlateNumber, "duplicate_plate", "رقم اللوحة مكرر داخل ملف GPS."));
                continue;
            }

            var matchedVehicles = PlateKeys(row.PlateNumber)
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
                VehicleId = vehicle.Id,
                WorkDate = report.WorkDate
            };
            distance.GpsDistanceKm = decimal.Round(row.DistanceKm!.Value, 2, MidpointRounding.AwayFromZero);
            distance.GpsPlateNumber = row.PlateNumber.Trim();
            distance.LastGpsImportId = import.Id;
            distance.GpsImportedAtUtc = support.UtcNow;
            distance.GpsImportedByUserId = actor.Value;
            ApplyEffectiveDistance(vehicle, distance);

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

    private async Task<long?> ResolveInitialBaselineAsync(
        Vehicle vehicle,
        DateOnly workDate,
        long? requestedBaseline,
        CancellationToken cancellationToken)
    {
        if (requestedBaseline.HasValue)
        {
            return requestedBaseline.Value;
        }

        var startUtc = StartOfWorkDateUtc(workDate);
        var historicalReading = await dbContext.VehicleOdometerReadings
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicle.Id && x.RecordedAtUtc < startUtc)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => (long?)x.Reading)
            .FirstOrDefaultAsync(cancellationToken);
        if (historicalReading.HasValue)
        {
            return historicalReading.Value;
        }

        return !vehicle.LastOdometerAtUtc.HasValue || vehicle.LastOdometerAtUtc.Value < startUtc
            ? vehicle.CurrentOdometer
            : null;
    }

    private static void ApplyEffectiveDistance(Vehicle vehicle, VehicleDailyDistance distance)
    {
        var previousApplied = distance.AppliedDistanceKm;
        var selected = VehicleDailyDistanceRules.SelectAppliedDistance(distance.GpsDistanceKm, distance.ManualDistanceKm);
        distance.AppliedDistanceKm = selected.DistanceKm;
        distance.AppliedSource = selected.Source;
        vehicle.TrackedDistanceKm += VehicleDailyDistanceRules.CalculateTotalAdjustment(previousApplied, selected.DistanceKm);
    }

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
            distance?.GpsImportedAtUtc,
            distance?.ManualEnteredAtUtc,
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
            foreach (var key in values.SelectMany(PlateKeys).Distinct(StringComparer.Ordinal))
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

    private static IEnumerable<string> PlateKeys(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        var normalized = NormalizePlateCharacters(value);
        if (normalized.Length == 0)
        {
            yield break;
        }

        var variants = new HashSet<string>(StringComparer.Ordinal) { normalized };
        variants.Add(TransliterateArabicPlate(normalized));
        foreach (var variant in variants)
        {
            yield return variant;
            var digits = new string(variant.Where(char.IsDigit).ToArray());
            var letters = new string(variant.Where(char.IsLetter).ToArray());
            if (digits.Length > 0 && letters.Length > 0)
            {
                yield return digits + letters;
                yield return letters + digits;
            }
        }
    }

    private static string CanonicalPlateKey(string value)
    {
        var normalized = NormalizePlateCharacters(value);
        var transliterated = TransliterateArabicPlate(normalized);
        var digits = new string(transliterated.Where(char.IsDigit).ToArray());
        var letters = new string(transliterated.Where(char.IsLetter).ToArray());
        return digits.Length > 0 && letters.Length > 0 ? digits + letters : normalized;
    }

    private static string NormalizePlateCharacters(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(character))
            {
                continue;
            }

            builder.Append(character switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                '\u06F0' => '0', '\u06F1' => '1', '\u06F2' => '2', '\u06F3' => '3', '\u06F4' => '4',
                '\u06F5' => '5', '\u06F6' => '6', '\u06F7' => '7', '\u06F8' => '8', '\u06F9' => '9',
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                _ => char.ToUpperInvariant(character)
            });
        }

        return builder.ToString();
    }

    private static string TransliterateArabicPlate(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                'ا' => 'A', 'ب' => 'B', 'ح' => 'J', 'د' => 'D', 'ر' => 'R', 'س' => 'S',
                'ص' => 'X', 'ط' => 'T', 'ع' => 'E', 'ق' => 'G', 'ك' => 'K', 'ل' => 'L',
                'م' => 'Z', 'ن' => 'N', 'ه' => 'H', 'و' => 'U', 'ي' => 'V',
                _ => character
            });
        }

        return builder.ToString();
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
