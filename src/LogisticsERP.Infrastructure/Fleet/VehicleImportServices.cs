using System.Security.Cryptography;
using System.Text;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fleet;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsERP.Infrastructure.Fleet;

internal sealed class VehicleImportValidationService(VehicleImportProcessor processor)
    : IVehicleImportValidationService
{
    public Task<Result<VehicleImportResponse>> ValidateAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default) =>
        processor.ProcessAsync(content, fileName, commit: false, cancellationToken);
}

internal sealed class VehicleImportService(VehicleImportProcessor processor) : IVehicleImportService
{
    public Task<Result<VehicleImportResponse>> ImportAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default) =>
        processor.ProcessAsync(content, fileName, commit: true, cancellationToken);
}

internal sealed partial class VehicleImportProcessor(
    ApplicationDbContext dbContext,
    FleetServiceSupport support,
    ILogger<VehicleImportProcessor> logger)
{
    private static readonly Guid SystemActorId = Guid.Parse("019c18d5-62e1-7000-d000-000000000003");

    private static readonly Dictionary<string, VehicleRegistrationType> RegistrationTypes =
        new Dictionary<string, VehicleRegistrationType>(StringComparer.Ordinal)
        {
            [VehicleSpreadsheetParser.NormalizeKey("خصوصي")] = VehicleRegistrationType.Private,
            [VehicleSpreadsheetParser.NormalizeKey("خاص")] = VehicleRegistrationType.Private,
            [VehicleSpreadsheetParser.NormalizeKey("نقل خاص")] = VehicleRegistrationType.PrivateTransport,
            [VehicleSpreadsheetParser.NormalizeKey("حافلة صغيرة")] = VehicleRegistrationType.SmallBus,
            [VehicleSpreadsheetParser.NormalizeKey("أجرة")] = VehicleRegistrationType.Taxi,
            [VehicleSpreadsheetParser.NormalizeKey("تاكسي")] = VehicleRegistrationType.Taxi,
            [VehicleSpreadsheetParser.NormalizeKey("نقل عام")] = VehicleRegistrationType.PublicTransport,
            [VehicleSpreadsheetParser.NormalizeKey("حافلة عامة")] = VehicleRegistrationType.PublicBus,
            [VehicleSpreadsheetParser.NormalizeKey("دراجة آلية")] = VehicleRegistrationType.Motorcycle,
            [VehicleSpreadsheetParser.NormalizeKey("دراجة نارية")] = VehicleRegistrationType.Motorcycle,
            [VehicleSpreadsheetParser.NormalizeKey("دباب")] = VehicleRegistrationType.Motorcycle,
            [VehicleSpreadsheetParser.NormalizeKey("أشغال عامة")] = VehicleRegistrationType.PublicWorks
        };

    private static readonly Dictionary<char, char> PlateLetterMap = new()
    {
        ['ا'] = 'A', ['ب'] = 'B', ['ح'] = 'J', ['د'] = 'D', ['ر'] = 'R',
        ['س'] = 'S', ['ص'] = 'X', ['ط'] = 'T', ['ع'] = 'E', ['ق'] = 'G',
        ['ك'] = 'K', ['ل'] = 'L', ['م'] = 'Z', ['ن'] = 'N', ['ه'] = 'H',
        ['و'] = 'U', ['ي'] = 'V'
    };

    public async Task<Result<VehicleImportResponse>> ProcessAsync(
        Stream content,
        string fileName,
        bool commit,
        CancellationToken cancellationToken)
    {
        if (content is null || !content.CanRead)
        {
            return Result.Failure<VehicleImportResponse>(VehicleImportErrors.InvalidWorkbook);
        }

        try
        {
            var workbook = VehicleSpreadsheetParser.Parse(content);
            return await BuildAndOptionallyCommitAsync(workbook, Path.GetFileName(fileName), commit, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or IOException or ArgumentException)
        {
            LogInvalidWorkbook(logger, fileName, exception);
            return Result.Failure<VehicleImportResponse>(VehicleImportErrors.InvalidWorkbook);
        }
        catch (DbUpdateException exception)
        {
            LogImportFailure(logger, fileName, exception);
            dbContext.ChangeTracker.Clear();
            return Result.Failure<VehicleImportResponse>(VehicleImportErrors.ImportFailed);
        }
    }

    private async Task<Result<VehicleImportResponse>> BuildAndOptionallyCommitAsync(
        ParsedVehicleImportWorkbook workbook,
        string fileName,
        bool commit,
        CancellationToken cancellationToken)
    {
        var issues = workbook.Issues.ToList();
        var previews = new List<VehicleImportRowPreview>();
        var plannedVehicles = new List<PlannedVehicle>();
        var newManufacturers = new List<VehicleManufacturer>();
        var newModels = new List<VehicleModel>();

        var manufacturers = await dbContext.VehicleManufacturers.AsNoTracking().ToArrayAsync(cancellationToken);
        var manufacturerByName = manufacturers
            .GroupBy(item => VehicleSpreadsheetParser.NormalizeKey(item.NameAr), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var models = await dbContext.VehicleModels.AsNoTracking().ToArrayAsync(cancellationToken);
        var modelByName = models
            .GroupBy(item => (item.VehicleManufacturerId, VehicleSpreadsheetParser.NormalizeKey(item.NameAr)))
            .ToDictionary(group => group.Key, group => group.First());

        var cities = await (
            from operating in dbContext.OperatingCities.AsNoTracking()
            join global in dbContext.GlobalCities.AsNoTracking() on operating.GlobalCityId equals global.Id
            where operating.Status == CatalogStatus.Active && global.Status == CatalogStatus.Active
            select new CityLookup(operating.Id, global.NameAr, global.NameEn))
            .ToArrayAsync(cancellationToken);

        var sponsors = await dbContext.Sponsors.AsNoTracking()
            .Where(item => item.Status == CatalogStatus.Active)
            .ToArrayAsync(cancellationToken);
        var sponsorByNumber = BuildSponsorNumberIndex(sponsors);

        var suppliers = await dbContext.VehicleSuppliers.AsNoTracking()
            .Where(item => item.Status == VehicleCatalogStatus.Active)
            .ToArrayAsync(cancellationToken);
        var supplierByNumber = BuildSupplierNumberIndex(suppliers);

        var existingVehicles = await dbContext.Vehicles.AsNoTracking()
            .Select(item => new ExistingVehicleIdentity(
                item.NormalizedPlateNumberAr,
                item.NormalizedPlateNumberEn,
                item.NormalizedSerialNumber,
                item.NormalizedChassisNumber))
            .ToArrayAsync(cancellationToken);
        var usedPlatesAr = existingVehicles.Select(item => item.PlateAr).Where(item => item is not null).ToHashSet(StringComparer.Ordinal);
        var usedPlatesEn = existingVehicles.Select(item => item.PlateEn).Where(item => item is not null).ToHashSet(StringComparer.Ordinal);
        var usedSerials = existingVehicles.Select(item => item.Serial).Where(item => item is not null).ToHashSet(StringComparer.Ordinal);
        var usedChassis = existingVehicles.Select(item => item.Chassis).Where(item => item is not null).ToHashSet(StringComparer.Ordinal);

        foreach (var row in workbook.Rows)
        {
            var rowIssueCount = issues.Count(item => item.RowNumber == row.RowNumber && IsError(item));
            if (!TryParsePlate(row.PlateNumber, out var plate))
            {
                issues.Add(Error(row, "plateNumber", "صيغة رقم اللوحة العربية غير صالحة أو تحتوي على حرف غير مدعوم."));
            }
            if (!RegistrationTypes.TryGetValue(VehicleSpreadsheetParser.NormalizeKey(row.RegistrationType), out var registrationType))
            {
                issues.Add(Error(row, "registrationType", $"نوع تسجيل الاستمارة '{row.RegistrationType}' غير معروف."));
            }

            var city = cities.FirstOrDefault(item =>
                VehicleSpreadsheetParser.NormalizeKey(item.NameAr) == VehicleSpreadsheetParser.NormalizeKey(row.City)
                || VehicleSpreadsheetParser.NormalizeKey(item.NameEn) == VehicleSpreadsheetParser.NormalizeKey(row.City));
            if (city is null)
            {
                issues.Add(Error(row, "city", $"المدينة '{row.City}' غير مفعلة في النظام."));
            }

            if (row.OwnerNumber.Length == 0)
            {
                issues.Add(Error(row, "ownerNumber", "رقم هوية المالك يجب أن يحتوي على أرقام."));
            }
            LogisticsERP.Domain.Entities.Workforce.Sponsor? userSponsor = null;
            if (row.UserNumber.Length == 0 || !sponsorByNumber.TryGetValue(row.UserNumber, out userSponsor))
            {
                issues.Add(Error(row, "userNumber", $"لم يتم العثور على راعٍ/مستخدم فعال بالرقم '{row.UserNumber}'."));
            }

            VehicleSupplier? ownerSupplier = null;
            LogisticsERP.Domain.Entities.Workforce.Sponsor? ownerSponsor = null;
            if (row.OwnerNumber.Length > 0)
            {
                if (row.OwnerNumber == row.UserNumber && userSponsor is not null)
                {
                    ownerSponsor = userSponsor;
                }
                else if (!supplierByNumber.TryGetValue(row.OwnerNumber, out ownerSupplier)
                         && !sponsorByNumber.TryGetValue(row.OwnerNumber, out ownerSponsor))
                {
                    issues.Add(Error(row, "ownerNumber", $"لم يتم العثور على مورد أو راعٍ فعال برقم المالك '{row.OwnerNumber}'."));
                }
            }

            if (plate is not null)
            {
                var normalizedPlateAr = FleetServiceSupport.NormalizeIdentifier(plate.NumberAr);
                var normalizedPlateEn = FleetServiceSupport.NormalizeIdentifier(plate.NumberEn);
                if (!usedPlatesAr.Add(normalizedPlateAr) || !usedPlatesEn.Add(normalizedPlateEn))
                {
                    issues.Add(Error(row, "plateNumber", "رقم اللوحة موجود مسبقًا في النظام أو مكرر داخل الملف."));
                }
            }

            var normalizedSerial = FleetServiceSupport.NormalizeIdentifier(row.SerialNumber);
            if (!usedSerials.Add(normalizedSerial))
            {
                issues.Add(Error(row, "serialNumber", "الرقم التسلسلي موجود مسبقًا في النظام أو مكرر داخل الملف."));
            }
            var normalizedChassis = FleetServiceSupport.NormalizeIdentifier(row.ChassisNumber);
            if (!usedChassis.Add(normalizedChassis))
            {
                issues.Add(Error(row, "chassisNumber", "رقم الهيكل موجود مسبقًا في النظام أو مكرر داخل الملف."));
            }

            if (issues.Count(item => item.RowNumber == row.RowNumber && IsError(item)) > rowIssueCount)
            {
                continue;
            }

            var inferredType = InferVehicleType(registrationType);
            var manufacturerKey = VehicleSpreadsheetParser.NormalizeKey(row.Manufacturer);
            if (!manufacturerByName.TryGetValue(manufacturerKey, out var manufacturer))
            {
                manufacturer = new VehicleManufacturer
                {
                    Code = CatalogCode("IMP-M", manufacturerKey),
                    NameAr = row.Manufacturer,
                    NameEn = row.Manufacturer,
                    Status = VehicleCatalogStatus.Active,
                    DisplayOrder = 0
                };
                manufacturerByName[manufacturerKey] = manufacturer;
                newManufacturers.Add(manufacturer);
            }

            var modelKey = (manufacturer.Id, VehicleSpreadsheetParser.NormalizeKey(row.Model));
            if (!modelByName.TryGetValue(modelKey, out var model))
            {
                model = new VehicleModel
                {
                    VehicleManufacturerId = manufacturer.Id,
                    Code = CatalogCode("IMP-MD", $"{manufacturerKey}:{modelKey.Item2}"),
                    NameAr = row.Model,
                    NameEn = row.Model,
                    VehicleType = inferredType,
                    DefaultFuelType = VehicleFuelType.Petrol,
                    Status = VehicleCatalogStatus.Active
                };
                modelByName[modelKey] = model;
                newModels.Add(model);
            }
            else if (model.VehicleType != inferredType
                     && (model.VehicleType == VehicleType.Motorcycle || inferredType == VehicleType.Motorcycle))
            {
                issues.Add(Error(row, "model", "نوع الطراز في النظام لا يتوافق مع نوع تسجيل المركبة."));
                continue;
            }

            var sameSponsor = ownerSponsor is not null && userSponsor is not null && ownerSponsor.Id == userSponsor.Id;
            var ownershipType = sameSponsor ? VehicleOwnershipType.Owned : VehicleOwnershipType.Leased;
            if (sameSponsor)
            {
                issues.Add(new VehicleImportIssue(
                    row.RowNumber,
                    row.PlateNumber,
                    "Warning",
                    "purchasedFromSupplierId",
                    "المالك والمستخدم هما الراعي نفسه؛ صُنفت المركبة كمملوكة وبقي مورد الشراء فارغًا لعدم وجوده في الملف."));
            }

            var ownerType = ownerSupplier is not null ? "Supplier" : "Sponsor";
            var ownerName = ownerSupplier?.NameAr ?? ownerSponsor?.RegistryNameAr;
            previews.Add(new VehicleImportRowPreview(
                row.RowNumber,
                plate!.NumberAr,
                plate.NumberEn,
                registrationType.ToString(),
                manufacturer.NameAr,
                model.NameAr,
                row.ModelYear!.Value,
                city!.NameAr,
                row.OwnerNumber,
                ownerType,
                ownerName,
                row.UserNumber,
                userSponsor!.RegistryNameAr,
                ownershipType.ToString()));
            plannedVehicles.Add(new PlannedVehicle(
                row,
                plate,
                registrationType,
                manufacturer,
                model,
                city.Id,
                userSponsor.Id,
                ownerSupplier?.Id,
                ownerSponsor?.Id,
                ownershipType));
        }

        var canImport = !issues.Any(IsError);
        var imported = false;
        if (commit && canImport)
        {
            var actor = support.UserId ?? SystemActorId;

            dbContext.VehicleManufacturers.AddRange(newManufacturers);
            dbContext.VehicleModels.AddRange(newModels);
            foreach (var plan in plannedVehicles)
            {
                var vehicle = CreateVehicle(plan, fileName);
                dbContext.Vehicles.Add(vehicle);
                dbContext.VehicleOperationalStatusPeriods.Add(new VehicleOperationalStatusPeriod
                {
                    VehicleId = vehicle.Id,
                    Status = VehicleOperationalStatus.Available,
                    EffectiveFromUtc = support.UtcNow,
                    Reason = "Vehicle created by Excel import.",
                    SourceType = VehicleStatusSourceType.Vehicle,
                    SourceEntityId = vehicle.Id,
                    ChangedByUserId = actor
                });
                dbContext.VehicleOdometerReadings.Add(new VehicleOdometerReading
                {
                    VehicleId = vehicle.Id,
                    Reading = 0,
                    RecordedAtUtc = support.UtcNow,
                    SourceType = VehicleOdometerSourceType.Manual,
                    SourceEntityId = vehicle.Id,
                    Notes = "Initial odometer reading from vehicle Excel import."
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            imported = true;
        }

        var errorRows = issues.Where(IsError).Select(item => item.RowNumber).Distinct().Count();
        return Result.Success(new VehicleImportResponse(
            ValidateOnly: !commit,
            CanImport: canImport,
            Imported: imported,
            Worksheet: workbook.Worksheet,
            TotalRows: workbook.TotalRows,
            ValidRows: workbook.TotalRows - errorRows,
            CreatedVehicles: canImport ? plannedVehicles.Count : 0,
            CreatedManufacturers: canImport ? newManufacturers.Count : 0,
            CreatedModels: canImport ? newModels.Count : 0,
            Rows: previews.OrderBy(item => item.RowNumber).ToArray(),
            Issues: issues.OrderBy(item => item.RowNumber).ThenBy(item => item.Severity).ToArray()));
    }

    private static Vehicle CreateVehicle(PlannedVehicle plan, string fileName)
    {
        var vehicle = new Vehicle
        {
            SerialNumber = plan.Row.SerialNumber,
            NormalizedSerialNumber = FleetServiceSupport.NormalizeIdentifier(plan.Row.SerialNumber),
            PlateNumberAr = plan.Plate.NumberAr,
            NormalizedPlateNumberAr = FleetServiceSupport.NormalizeIdentifier(plan.Plate.NumberAr),
            PlateNumberEn = plan.Plate.NumberEn,
            NormalizedPlateNumberEn = FleetServiceSupport.NormalizeIdentifier(plan.Plate.NumberEn),
            PlateLettersAr = plan.Plate.LettersAr,
            PlateLettersEn = plan.Plate.LettersEn,
            PlateDigits = plan.Plate.Digits,
            ChassisNumber = plan.Row.ChassisNumber,
            NormalizedChassisNumber = FleetServiceSupport.NormalizeIdentifier(plan.Row.ChassisNumber),
            SponsorId = plan.UserSponsorId,
            OperatingCityId = plan.OperatingCityId,
            PurchasedFromSupplierId = null,
            RegisteredOwnerSupplierId = plan.OwnerSupplierId,
            RegisteredOwnerSponsorId = plan.OwnerSponsorId,
            RegistrationType = plan.RegistrationType,
            VehicleManufacturerId = plan.Manufacturer.Id,
            VehicleModelId = plan.Model.Id,
            ModelYear = plan.Row.ModelYear,
            VehicleType = plan.Model.VehicleType,
            FuelType = plan.Model.DefaultFuelType,
            TransmissionType = VehicleTransmissionType.Other,
            ColorAr = string.IsNullOrWhiteSpace(plan.Row.Color) ? null : plan.Row.Color,
            ColorEn = null,
            OwnershipType = plan.OwnershipType,
            OwnerName = null,
            CurrentOdometer = 0,
            TrackedDistanceKm = 0,
            CurrentOperationalStatus = VehicleOperationalStatus.Available,
            Notes = $"Imported from {fileName}, worksheet row {plan.Row.RowNumber}."
        };
        vehicle.AssetNumber = FleetServiceSupport.NewVehicleAssetNumber(vehicle.Id);
        vehicle.NormalizedAssetNumber = FleetServiceSupport.NormalizeIdentifier(vehicle.AssetNumber);
        return vehicle;
    }

    private static Dictionary<string, LogisticsERP.Domain.Entities.Workforce.Sponsor> BuildSponsorNumberIndex(
        IEnumerable<LogisticsERP.Domain.Entities.Workforce.Sponsor> sponsors)
    {
        var result = new Dictionary<string, LogisticsERP.Domain.Entities.Workforce.Sponsor>(StringComparer.Ordinal);
        foreach (var sponsor in sponsors)
        {
            AddNumber(result, sponsor.EmployerIdentityNumber, sponsor);
            AddNumber(result, sponsor.CommercialRegistrationNumber, sponsor);
            AddNumber(result, sponsor.UnifiedNationalNumber, sponsor);
        }
        return result;
    }

    private static Dictionary<string, VehicleSupplier> BuildSupplierNumberIndex(IEnumerable<VehicleSupplier> suppliers)
    {
        var result = new Dictionary<string, VehicleSupplier>(StringComparer.Ordinal);
        foreach (var supplier in suppliers)
        {
            AddNumber(result, supplier.CommercialRegistrationNumber, supplier);
            AddNumber(result, supplier.TaxNumber, supplier);
        }
        return result;
    }

    private static void AddNumber<T>(Dictionary<string, T> index, string? value, T entity) where T : class
    {
        var digits = value is null ? string.Empty : new(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length > 0)
        {
            index.TryAdd(digits, entity);
        }
    }

    internal static bool TryParsePlate(string value, out ParsedPlate? plate)
    {
        plate = null;
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var letters = new List<char>(3);
        var digits = new StringBuilder(4);
        foreach (var original in normalized)
        {
            var character = original switch
            {
                '\u0660' or '\u06F0' => '0', '\u0661' or '\u06F1' => '1',
                '\u0662' or '\u06F2' => '2', '\u0663' or '\u06F3' => '3',
                '\u0664' or '\u06F4' => '4', '\u0665' or '\u06F5' => '5',
                '\u0666' or '\u06F6' => '6', '\u0667' or '\u06F7' => '7',
                '\u0668' or '\u06F8' => '8', '\u0669' or '\u06F9' => '9',
                '\u0622' or '\u0623' or '\u0625' => '\u0627',
                '\u0649' => '\u064A',
                _ => original
            };
            if (char.IsAsciiDigit(character))
            {
                digits.Append(character);
            }
            else if (char.IsLetter(character) && character != '\u0640')
            {
                if (!PlateLetterMap.ContainsKey(character))
                {
                    return false;
                }
                letters.Add(character);
            }
            else if (!char.IsWhiteSpace(character) && character is not '-' and not '/')
            {
                return false;
            }
        }

        if (letters.Count is < 1 or > 3 || digits.Length is < 1 or > 4)
        {
            return false;
        }

        var lettersAr = string.Join(' ', letters);
        var lettersEn = string.Join(' ', letters.Select(letter => PlateLetterMap[letter]));
        plate = new ParsedPlate(
            $"{lettersAr} {digits}",
            $"{lettersEn} {digits}",
            lettersAr,
            lettersEn,
            digits.ToString());
        return true;
    }

    private static VehicleType InferVehicleType(VehicleRegistrationType registrationType) => registrationType switch
    {
        VehicleRegistrationType.Motorcycle => VehicleType.Motorcycle,
        VehicleRegistrationType.SmallBus or VehicleRegistrationType.PublicBus => VehicleType.Van,
        VehicleRegistrationType.PublicWorks => VehicleType.Other,
        _ => VehicleType.Car
    };

    private static string CatalogCode(string prefix, string key) =>
        $"{prefix}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..12]}";

    private static VehicleImportIssue Error(ParsedVehicleImportRow row, string field, string message) =>
        new(row.RowNumber, row.PlateNumber, "Error", field, message);

    private static bool IsError(VehicleImportIssue issue) =>
        string.Equals(issue.Severity, "Error", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(LogLevel.Warning, "Vehicle workbook {FileName} could not be parsed.")]
    private static partial void LogInvalidWorkbook(ILogger logger, string fileName, Exception exception);

    [LoggerMessage(LogLevel.Error, "Vehicle workbook {FileName} failed during database import.")]
    private static partial void LogImportFailure(ILogger logger, string fileName, Exception exception);

    internal sealed record ParsedPlate(
        string NumberAr,
        string NumberEn,
        string LettersAr,
        string LettersEn,
        string Digits);

    private sealed record CityLookup(Guid Id, string NameAr, string NameEn);
    private sealed record ExistingVehicleIdentity(string? PlateAr, string? PlateEn, string? Serial, string? Chassis);
    private sealed record PlannedVehicle(
        ParsedVehicleImportRow Row,
        ParsedPlate Plate,
        VehicleRegistrationType RegistrationType,
        VehicleManufacturer Manufacturer,
        VehicleModel Model,
        Guid OperatingCityId,
        Guid UserSponsorId,
        Guid? OwnerSupplierId,
        Guid? OwnerSponsorId,
        VehicleOwnershipType OwnershipType);
}
