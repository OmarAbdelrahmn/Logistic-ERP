using System.Security.Cryptography;
using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Abstractions.Files;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Entities.Fuel;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Fleet;
using LogisticsERP.Domain.Fuel;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed class FuelCardService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider) : IFuelCardService
{
    private const long MaximumImportSize = 25 * 1024 * 1024;
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    public async Task<Result<FuelCardPageResponse>> GetCardsAsync(
        string? search,
        string? provider,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
        {
            return Result.Failure<FuelCardPageResponse>(FuelErrors.Forbidden);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 300);
        var query = dbContext.FuelCards.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (!TryParseProvider(provider, out var parsedProvider))
            {
                return Result.Failure<FuelCardPageResponse>(FuelErrors.InvalidProvider);
            }
            query = query.Where(x => x.Provider == parsedProvider);
        }

        if (riderProfileId.HasValue)
        {
            query = query.Where(card => dbContext.FuelCardRiderAssignments.Any(assignment =>
                assignment.FuelCardId == card.Id
                && assignment.RiderProfileId == riderProfileId.Value
                && assignment.EffectiveTo == null));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = NormalizeSearchIdentifier(term);
            query = normalized.Length == 0
                ? query.Where(x =>
                    x.CardNumber.Contains(term)
                    || x.PlateNumberText != null && x.PlateNumberText.Contains(term))
                : query.Where(x =>
                    x.CardNumber.Contains(term)
                    || x.NormalizedCardNumber.Contains(normalized)
                    || x.PlateNumberText != null && x.PlateNumberText.Contains(term)
                    || x.NormalizedPlateNumber != null && x.NormalizedPlateNumber.Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var cards = await query
            .OrderBy(x => x.Provider)
            .ThenBy(x => x.NormalizedCardNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        var currentRiders = await GetCurrentRidersAsync(cards.Select(x => x.Id).ToArray(), cancellationToken);
        var items = cards.Select(card => MapCard(card, currentRiders.GetValueOrDefault(card.Id))).ToArray();
        return Result.Success(new FuelCardPageResponse(items, page, pageSize, totalCount));
    }

    public async Task<Result<FuelCardResponse>> GetCardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.Forbidden);
        }

        var card = await dbContext.FuelCards.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (card is null)
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.NotFound);
        }

        var currentRiders = await GetCurrentRidersAsync([id], cancellationToken);
        return Result.Success(MapCard(card, currentRiders.GetValueOrDefault(id)));
    }

    public async Task<Result<FuelCardResponse>> CreateCardAsync(
        CreateFuelCardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Manage, cancellationToken))
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.Forbidden);
        }
        if (!TryParseProvider(request.Provider, out var provider))
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.InvalidProvider);
        }
        if (!ValidRequiredText(request.CardNumber, 100)
            || !ValidOptionalText(request.PlateNumberText, 100)
            || !ValidOptionalText(request.Notes, 4000))
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.InvalidRequest);
        }

        string normalizedCardNumber;
        var identifierType = FuelCardRules.DetectIdentifierType(request.CardNumber);
        try
        {
            normalizedCardNumber = FuelCardRules.NormalizeCardNumber(request.CardNumber, identifierType);
        }
        catch (ArgumentException)
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.InvalidCardNumber);
        }

        if (await dbContext.FuelCards.AnyAsync(x =>
                x.Provider == provider && x.NormalizedCardNumber == normalizedCardNumber,
                cancellationToken))
        {
            return Result.Failure<FuelCardResponse>(FuelErrors.DuplicateCard);
        }

        var plate = TrimOrNull(request.PlateNumberText);
        var card = new FuelCard
        {
            Provider = provider,
            IdentifierType = identifierType,
            CardNumber = request.CardNumber.Trim(),
            NormalizedCardNumber = normalizedCardNumber,
            PlateNumberText = plate,
            NormalizedPlateNumber = plate is null ? null : PlateNumberRules.CanonicalKey(plate),
            Notes = TrimOrNull(request.Notes)
        };
        dbContext.FuelCards.Add(card);

        var save = await SaveAsync(cancellationToken);
        return save.IsFailure
            ? Result.Failure<FuelCardResponse>(save.Error)
            : Result.Success(MapCard(card, null));
    }

    public async Task<Result<IReadOnlyList<FuelCardAssignmentResponse>>> GetAssignmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<FuelCardAssignmentResponse>>(FuelErrors.Forbidden);
        }
        if (!await dbContext.FuelCards.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<FuelCardAssignmentResponse>>(FuelErrors.NotFound);
        }

        var assignments = await BuildAssignmentQuery(id, newestFirst: true)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FuelCardAssignmentResponse>>(assignments.Select(MapAssignment).ToArray());
    }

    public async Task<Result<FuelCardAssignmentResponse>> AssignRiderAsync(
        Guid id,
        AssignFuelCardRiderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Manage, cancellationToken))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.Forbidden);
        }
        if (currentUser.UserId is not { } actorId)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 1000)
            || !ValidOptionalText(request.Notes, 4000)
            || request.EffectiveFrom > RiyadhToday())
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.InvalidRequest);
        }
        if (!await dbContext.FuelCards.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.NotFound);
        }
        if (await dbContext.FuelCardRiderAssignments.AnyAsync(x => x.FuelCardId == id && x.EffectiveTo == null, cancellationToken))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.ActiveAssignmentConflict);
        }

        var rider = await (
            from riderProfile in dbContext.RiderProfiles.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking() on riderProfile.EmployeeId equals employee.Id
            where riderProfile.Id == request.RiderProfileId
            select new RiderEligibility(riderProfile.EmployeeId, employee.IsEmployee, employee.Status))
            .SingleOrDefaultAsync(cancellationToken);
        if (rider is null)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.RiderNotFound);
        }
        if (rider.IsEmployee || rider.Status != EmployeeStatus.Active)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.RiderUnavailable);
        }

        var month = FuelCardRules.MonthStart(request.EffectiveFrom);
        var monthEnd = FuelCardRules.MonthEnd(month);
        var assignmentRidersInMonth = await dbContext.FuelCardRiderAssignments
            .Where(x =>
            x.FuelCardId == id
            && x.EffectiveFrom <= monthEnd
            && (x.EffectiveTo == null || x.EffectiveTo >= month))
            .Select(x => x.RiderProfileId)
            .ToArrayAsync(cancellationToken);
        var usageRidersInMonth = await dbContext.FuelCardMonthlyUsages
            .Where(x =>
            x.FuelCardId == id
            && x.ReportMonth == month)
            .Select(x => x.RiderProfileId)
            .ToArrayAsync(cancellationToken);
        if (!FuelCardRules.CanUseRiderForMonth(
                request.RiderProfileId,
                assignmentRidersInMonth.Concat(usageRidersInMonth)))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.MonthlyRiderConflict);
        }

        var latestEnd = await dbContext.FuelCardRiderAssignments
            .Where(x => x.FuelCardId == id && x.EffectiveTo != null)
            .MaxAsync(x => x.EffectiveTo, cancellationToken);
        if (latestEnd.HasValue && request.EffectiveFrom <= latestEnd.Value)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.InvalidDateRange);
        }

        var assignment = new FuelCardRiderAssignment
        {
            FuelCardId = id,
            RiderProfileId = request.RiderProfileId,
            EmployeeId = rider.EmployeeId,
            EffectiveFrom = request.EffectiveFrom,
            AssignedByUserId = actorId,
            AssignmentReason = request.Reason.Trim(),
            Notes = TrimOrNull(request.Notes)
        };
        dbContext.FuelCardRiderAssignments.Add(assignment);
        var save = await SaveAsync(cancellationToken);
        if (save.IsFailure)
        {
            return Result.Failure<FuelCardAssignmentResponse>(save.Error);
        }

        return await GetAssignmentAsync(id, assignment.Id, cancellationToken);
    }

    public async Task<Result<FuelCardAssignmentResponse>> StopRiderAsync(
        Guid id,
        StopFuelCardRiderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Manage, cancellationToken))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.Forbidden);
        }
        if (currentUser.UserId is null)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.CurrentUserUnavailable);
        }
        if (!ValidRequiredText(request.Reason, 1000) || request.EffectiveTo > RiyadhToday())
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.InvalidRequest);
        }
        if (!await dbContext.FuelCards.AnyAsync(x => x.Id == id, cancellationToken))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.NotFound);
        }

        var assignment = await dbContext.FuelCardRiderAssignments.SingleOrDefaultAsync(x =>
            x.FuelCardId == id && x.EffectiveTo == null,
            cancellationToken);
        if (assignment is null)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.AssignmentNotFound);
        }
        if (!MatchesRowVersion(assignment.RowVersion, request.RowVersion))
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.ConcurrencyConflict);
        }
        if (request.EffectiveTo < assignment.EffectiveFrom)
        {
            return Result.Failure<FuelCardAssignmentResponse>(FuelErrors.InvalidDateRange);
        }

        assignment.EffectiveTo = request.EffectiveTo;
        assignment.EndReason = request.Reason.Trim();
        assignment.ClosedAtUtc = timeProvider.GetUtcNow();
        assignment.ClosedByUserId = currentUser.UserId.Value;
        var save = await SaveAsync(cancellationToken);
        if (save.IsFailure)
        {
            return Result.Failure<FuelCardAssignmentResponse>(save.Error);
        }

        return await GetAssignmentAsync(id, assignment.Id, cancellationToken);
    }

    public async Task<Result<FuelMonthlyUsagePageResponse>> GetMonthlyUsageAsync(
        DateOnly month,
        string? search,
        string? provider,
        Guid? riderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
        {
            return Result.Failure<FuelMonthlyUsagePageResponse>(FuelErrors.Forbidden);
        }

        month = FuelCardRules.MonthStart(month);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 100 : pageSize, 1, 300);
        var cards = dbContext.FuelCards.IgnoreQueryFilters().AsNoTracking();
        var riders = dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var query = from usage in dbContext.FuelCardMonthlyUsages.AsNoTracking()
                    join card in cards on usage.FuelCardId equals card.Id
                    join rider in riders on usage.RiderProfileId equals rider.Id
                    join employee in employees on usage.EmployeeId equals employee.Id
                    where usage.ReportMonth == month
                    select new
                    {
                        Usage = usage,
                        Card = card,
                        RiderNameAr = employee.FullNameAr,
                        RiderNameEn = employee.FullNameEn
                    };

        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (!TryParseProvider(provider, out var parsedProvider))
            {
                return Result.Failure<FuelMonthlyUsagePageResponse>(FuelErrors.InvalidProvider);
            }
            query = query.Where(x => x.Card.Provider == parsedProvider);
        }
        if (riderProfileId.HasValue)
        {
            query = query.Where(x => x.Usage.RiderProfileId == riderProfileId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Card.CardNumber.Contains(term)
                || x.Card.PlateNumberText != null && x.Card.PlateNumberText.Contains(term)
                || x.RiderNameAr.Contains(term)
                || x.RiderNameEn != null && x.RiderNameEn.Contains(term));
        }

        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                TotalLiters = group.Sum(x => x.Usage.TotalLiters),
                TotalAmount = group.Sum(x => x.Usage.TotalAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Card.Provider)
            .ThenBy(x => x.Card.NormalizedCardNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MonthlyProjection(
                x.Usage,
                x.Card,
                x.RiderNameAr,
                x.RiderNameEn))
            .ToArrayAsync(cancellationToken);
        return Result.Success(new FuelMonthlyUsagePageResponse(
            rows.Select(MapMonthlyUsage).ToArray(),
            month,
            page,
            pageSize,
            totals?.TotalCount ?? 0,
            totals?.TotalLiters ?? 0m,
            totals?.TotalAmount ?? 0m));
    }

    public async Task<Result<FuelImportResponse>> ImportAsync(
        PrivateFileUpload file,
        DateOnly? expectedMonth,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Import, cancellationToken))
        {
            return Result.Failure<FuelImportResponse>(FuelErrors.Forbidden);
        }
        if (currentUser.UserId is not { } actorId)
        {
            return Result.Failure<FuelImportResponse>(FuelErrors.CurrentUserUnavailable);
        }

        var extension = Path.GetExtension(file.OriginalFileName).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > MaximumImportSize || extension is not ".xls" and not ".xlsx")
        {
            return Result.Failure<FuelImportResponse>(FuelErrors.InvalidFile);
        }

        await using var workbook = new MemoryStream((int)Math.Min(file.Length, int.MaxValue));
        await file.Content.CopyToAsync(workbook, cancellationToken);
        var checksum = Convert.ToHexString(SHA256.HashData(workbook.ToArray()));
        workbook.Position = 0;

        ParsedFuelReport report;
        try
        {
            report = FuelSpreadsheetParser.Parse(workbook);
        }
        catch (Exception exception) when (exception is InvalidDataException
            or NotSupportedException
            or FormatException
            or ArgumentException
            or ExcelDataReader.Exceptions.HeaderException)
        {
            return Result.Failure<FuelImportResponse>(FuelErrors.InvalidFile);
        }

        if (expectedMonth.HasValue && FuelCardRules.MonthStart(expectedMonth.Value) != report.ReportMonth)
        {
            return Result.Failure<FuelImportResponse>(FuelErrors.MonthMismatch);
        }

        var import = new FuelCardImport
        {
            Provider = report.Provider,
            ReportMonth = report.ReportMonth,
            ReportThroughAtUtc = report.ReportThroughAtUtc,
            OriginalFileName = Path.GetFileName(file.OriginalFileName),
            Sha256Checksum = checksum,
            SourceRows = report.SourceRows,
            CardRows = report.Cards.Count,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            CreatedByUserId = actorId
        };
        dbContext.FuelCardImports.Add(import);
        var errors = report.Errors
            .Select(error => new FuelImportRowError(error.RowNumber, error.CardNumber, error.Code, error.Message))
            .ToList();
        import.InvalidRows = report.Errors.Count;

        var existingCards = await dbContext.FuelCards
            .Where(x => x.Provider == report.Provider)
            .ToDictionaryAsync(x => x.NormalizedCardNumber, StringComparer.Ordinal, cancellationToken);
        foreach (var parsed in report.Cards)
        {
            if (!existingCards.TryGetValue(parsed.NormalizedCardNumber, out var card))
            {
                card = new FuelCard
                {
                    Provider = report.Provider,
                    IdentifierType = parsed.IdentifierType,
                    CardNumber = parsed.CardNumber,
                    NormalizedCardNumber = parsed.NormalizedCardNumber
                };
                dbContext.FuelCards.Add(card);
                existingCards.Add(parsed.NormalizedCardNumber, card);
                import.CreatedCards++;
            }

            ApplySourcePlate(card, parsed.PlateNumberText);
        }

        var cardIds = report.Cards
            .Select(parsed => existingCards[parsed.NormalizedCardNumber].Id)
            .Distinct()
            .ToArray();
        var monthEnd = FuelCardRules.MonthEnd(report.ReportMonth);
        var assignments = await dbContext.FuelCardRiderAssignments.AsNoTracking()
            .Where(x => cardIds.Contains(x.FuelCardId)
                && x.EffectiveFrom <= monthEnd
                && (x.EffectiveTo == null || x.EffectiveTo >= report.ReportMonth))
            .ToArrayAsync(cancellationToken);
        var assignmentsByCard = assignments.ToLookup(x => x.FuelCardId);
        var existingMonthly = await dbContext.FuelCardMonthlyUsages
            .Where(x => cardIds.Contains(x.FuelCardId) && x.ReportMonth == report.ReportMonth)
            .ToDictionaryAsync(x => x.FuelCardId, cancellationToken);

        foreach (var parsed in report.Cards)
        {
            var card = existingCards[parsed.NormalizedCardNumber];
            var monthlyRiders = assignmentsByCard[card.Id]
                .GroupBy(x => new { x.RiderProfileId, x.EmployeeId })
                .Select(group => group.Key)
                .ToArray();
            if (monthlyRiders.Length == 0)
            {
                import.UnassignedCards++;
                errors.Add(new FuelImportRowError(
                    parsed.FirstRowNumber,
                    parsed.CardNumber,
                    "card_not_assigned",
                    "البطاقة غير مسندة إلى رايدر في شهر التقرير؛ أُنشئت البطاقة دون سجل استهلاك شهري."));
                continue;
            }
            if (monthlyRiders.Length > 1)
            {
                import.InvalidRows++;
                errors.Add(new FuelImportRowError(
                    parsed.FirstRowNumber,
                    parsed.CardNumber,
                    "multiple_monthly_riders",
                    "البطاقة مرتبطة بأكثر من رايدر في شهر التقرير وتحتاج إلى تصحيح الإسناد."));
                continue;
            }

            var monthlyRider = monthlyRiders[0];
            var isNew = !existingMonthly.TryGetValue(card.Id, out var usage);
            if (!isNew && usage!.RiderProfileId != monthlyRider.RiderProfileId)
            {
                import.InvalidRows++;
                errors.Add(new FuelImportRowError(
                    parsed.FirstRowNumber,
                    parsed.CardNumber,
                    "monthly_rider_conflict",
                    "سجل الشهر مرتبط مسبقًا برايدر آخر ولن يتم تغييره."));
                continue;
            }

            usage ??= new FuelCardMonthlyUsage
            {
                FuelCardId = card.Id,
                ReportMonth = report.ReportMonth
            };
            usage.RiderProfileId = monthlyRider.RiderProfileId;
            usage.EmployeeId = monthlyRider.EmployeeId;
            usage.TotalLiters = parsed.TotalLiters;
            usage.TotalAmount = parsed.TotalAmount;
            usage.AmountBeforeTax = parsed.AmountBeforeTax;
            usage.VatAmount = parsed.VatAmount;
            usage.TransactionCount = parsed.TransactionCount;
            usage.FuelType = parsed.FuelType;
            usage.SourcePlateNumber = TrimOrNull(parsed.PlateNumberText);
            usage.NormalizedSourcePlateNumber = usage.SourcePlateNumber is null
                ? null
                : PlateNumberRules.CanonicalKey(usage.SourcePlateNumber);
            usage.FirstTransactionAtUtc = parsed.FirstTransactionAtUtc;
            usage.LastTransactionAtUtc = parsed.LastTransactionAtUtc;
            usage.ReportThroughAtUtc = report.ReportThroughAtUtc;
            usage.LastImportId = import.Id;

            if (isNew)
            {
                dbContext.FuelCardMonthlyUsages.Add(usage);
                existingMonthly.Add(card.Id, usage);
                import.CreatedMonthlyRecords++;
            }
            else
            {
                import.UpdatedMonthlyRecords++;
            }
        }

        import.RowErrorsJson = JsonSerializer.Serialize(errors);
        var save = await SaveAsync(cancellationToken);
        if (save.IsFailure)
        {
            return Result.Failure<FuelImportResponse>(save.Error);
        }

        return Result.Success(MapImport(import, errors));
    }

    public async Task<Result<IReadOnlyList<FuelImportHistoryResponse>>> GetImportsAsync(
        DateOnly? month,
        string? provider,
        CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<FuelImportHistoryResponse>>(FuelErrors.Forbidden);
        }

        var query = dbContext.FuelCardImports.AsNoTracking().AsQueryable();
        if (month.HasValue)
        {
            var reportMonth = FuelCardRules.MonthStart(month.Value);
            query = query.Where(x => x.ReportMonth == reportMonth);
        }
        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (!TryParseProvider(provider, out var parsedProvider))
            {
                return Result.Failure<IReadOnlyList<FuelImportHistoryResponse>>(FuelErrors.InvalidProvider);
            }
            query = query.Where(x => x.Provider == parsedProvider);
        }

        var imports = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FuelImportHistoryResponse>>(imports.Select(MapImportHistory).ToArray());
    }

    private IQueryable<AssignmentProjection> BuildAssignmentQuery(
        Guid cardId,
        Guid? assignmentId = null,
        bool newestFirst = false)
    {
        var cards = dbContext.FuelCards.IgnoreQueryFilters().AsNoTracking();
        var riders = dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var query = from assignment in dbContext.FuelCardRiderAssignments.AsNoTracking()
                    join card in cards on assignment.FuelCardId equals card.Id
                    join rider in riders on assignment.RiderProfileId equals rider.Id
                    join employee in employees on assignment.EmployeeId equals employee.Id
                    where assignment.FuelCardId == cardId
                    select new
                    {
                        Assignment = assignment,
                        card.CardNumber,
                        RiderNameAr = employee.FullNameAr,
                        RiderNameEn = employee.FullNameEn
                    };

        if (assignmentId.HasValue)
        {
            query = query.Where(x => x.Assignment.Id == assignmentId.Value);
        }
        if (newestFirst)
        {
            query = query.OrderByDescending(x => x.Assignment.EffectiveFrom);
        }

        return query.Select(x => new AssignmentProjection(
            x.Assignment,
            x.CardNumber,
            x.RiderNameAr,
            x.RiderNameEn));
    }

    internal async Task<Result<FuelCardAssignmentResponse>> GetAssignmentAsync(
        Guid cardId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var row = await BuildAssignmentQuery(cardId, assignmentId)
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? Result.Failure<FuelCardAssignmentResponse>(FuelErrors.AssignmentNotFound)
            : Result.Success(MapAssignment(row));
    }

    private async Task<Dictionary<Guid, CurrentRiderProjection>> GetCurrentRidersAsync(
        Guid[] cardIds,
        CancellationToken cancellationToken)
    {
        if (cardIds.Length == 0)
        {
            return [];
        }

        var riders = dbContext.RiderProfiles.IgnoreQueryFilters().AsNoTracking();
        var employees = dbContext.Employees.IgnoreQueryFilters().AsNoTracking();
        var rows = await (
            from assignment in dbContext.FuelCardRiderAssignments.AsNoTracking()
            join rider in riders on assignment.RiderProfileId equals rider.Id
            join employee in employees on assignment.EmployeeId equals employee.Id
            where cardIds.Contains(assignment.FuelCardId) && assignment.EffectiveTo == null
            select new CurrentRiderProjection(
                assignment.FuelCardId,
                assignment.Id,
                assignment.RiderProfileId,
                assignment.EmployeeId,
                employee.FullNameAr,
                employee.FullNameEn,
                assignment.EffectiveFrom,
                assignment.RowVersion))
            .ToArrayAsync(cancellationToken);
        return rows.ToDictionary(x => x.FuelCardId);
    }

    private async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || currentUser.AuthorizationVersion is not { } version)
        {
            return false;
        }

        return await permissionChecker.HasPermissionAsync(userId, version, permissionKey, null, cancellationToken);
    }

    private async Task<Result> SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(FuelErrors.ConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(FuelErrors.PersistenceConflict);
        }
    }

    private static FuelCardResponse MapCard(FuelCard card, CurrentRiderProjection? rider) => new(
        card.Id,
        card.Provider.ToString(),
        ProviderNameAr(card.Provider),
        card.IdentifierType.ToString(),
        card.CardNumber,
        card.NormalizedCardNumber,
        card.PlateNumberText,
        rider is null ? null : new FuelCardCurrentRiderResponse(
            rider.AssignmentId,
            rider.RiderProfileId,
            rider.EmployeeId,
            rider.RiderNameAr,
            rider.RiderNameEn,
            rider.EffectiveFrom,
            EncodeRowVersion(rider.RowVersion)),
        card.Notes,
        card.CreatedAtUtc,
        card.UpdatedAtUtc,
        EncodeRowVersion(card.RowVersion));

    private static FuelCardAssignmentResponse MapAssignment(AssignmentProjection row) => new(
        row.Assignment.Id,
        row.Assignment.FuelCardId,
        row.CardNumber,
        row.Assignment.RiderProfileId,
        row.Assignment.EmployeeId,
        row.RiderNameAr,
        row.RiderNameEn,
        row.Assignment.EffectiveFrom,
        row.Assignment.EffectiveTo,
        row.Assignment.AssignmentReason,
        row.Assignment.EndReason,
        row.Assignment.Notes,
        row.Assignment.AssignedByUserId,
        row.Assignment.ClosedByUserId,
        EncodeRowVersion(row.Assignment.RowVersion));

    private static FuelMonthlyUsageResponse MapMonthlyUsage(MonthlyProjection row) => new(
        row.Usage.Id,
        row.Card.Id,
        row.Card.Provider.ToString(),
        ProviderNameAr(row.Card.Provider),
        row.Card.CardNumber,
        row.Usage.SourcePlateNumber ?? row.Card.PlateNumberText,
        row.Usage.ReportMonth,
        row.Usage.RiderProfileId,
        row.Usage.EmployeeId,
        row.RiderNameAr,
        row.RiderNameEn,
        row.Usage.TotalLiters,
        row.Usage.TotalAmount,
        row.Usage.AmountBeforeTax,
        row.Usage.VatAmount,
        row.Usage.TransactionCount,
        row.Usage.FuelType,
        row.Usage.FirstTransactionAtUtc,
        row.Usage.LastTransactionAtUtc,
        row.Usage.ReportThroughAtUtc,
        row.Usage.LastImportId,
        row.Usage.UpdatedAtUtc,
        EncodeRowVersion(row.Usage.RowVersion));

    private static FuelImportResponse MapImport(FuelCardImport import, IReadOnlyList<FuelImportRowError> errors) => new(
        import.Id,
        import.Provider.ToString(),
        ProviderNameAr(import.Provider),
        import.ReportMonth,
        import.ReportThroughAtUtc,
        import.OriginalFileName,
        import.Sha256Checksum,
        import.SourceRows,
        import.CardRows,
        import.CreatedCards,
        import.CreatedMonthlyRecords,
        import.UpdatedMonthlyRecords,
        import.UnassignedCards,
        import.InvalidRows,
        errors,
        import.CreatedAtUtc);

    private static FuelImportHistoryResponse MapImportHistory(FuelCardImport import) => new(
        import.Id,
        import.Provider.ToString(),
        ProviderNameAr(import.Provider),
        import.ReportMonth,
        import.ReportThroughAtUtc,
        import.OriginalFileName,
        import.Sha256Checksum,
        import.SourceRows,
        import.CardRows,
        import.CreatedCards,
        import.CreatedMonthlyRecords,
        import.UpdatedMonthlyRecords,
        import.UnassignedCards,
        import.InvalidRows,
        import.CreatedAtUtc,
        import.CreatedByUserId);

    private static void ApplySourcePlate(FuelCard card, string? plateNumber)
    {
        var plate = TrimOrNull(plateNumber);
        if (plate is null)
        {
            return;
        }

        card.PlateNumberText = plate;
        card.NormalizedPlateNumber = PlateNumberRules.CanonicalKey(plate);
    }

    private static bool TryParseProvider(string value, out FuelCardProvider provider)
    {
        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .ToLowerInvariant();
        provider = normalized switch
        {
            "petroapp" or "petroup" or "بترواب" or "شركةبترواب" => FuelCardProvider.PetroApp,
            "sayaraapp" or "sayarahapp" or "سيارةاب" or "شركةسيارةاب" => FuelCardProvider.SayaraApp,
            _ => default
        };
        return provider != default;
    }

    private static string ProviderNameAr(FuelCardProvider provider) => provider switch
    {
        FuelCardProvider.PetroApp => "شركة بترو اب",
        FuelCardProvider.SayaraApp => "شركة سيارة اب",
        _ => provider.ToString()
    };

    private DateOnly RiyadhToday() => DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(RiyadhOffset).DateTime);

    private static bool ValidRequiredText(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximumLength;

    private static string NormalizeSearchIdentifier(string value)
    {
        try
        {
            return FuelCardRules.NormalizeCardNumber(value, FuelCardRules.DetectIdentifierType(value));
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    private static bool ValidOptionalText(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maximumLength;

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EncodeRowVersion(byte[] value) => Convert.ToBase64String(value);

    private static bool MatchesRowVersion(byte[] current, string supplied)
    {
        if (string.IsNullOrWhiteSpace(supplied)) return false;
        try { return CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(supplied)); }
        catch (FormatException) { return false; }
    }

    private sealed record RiderEligibility(Guid EmployeeId, bool IsEmployee, EmployeeStatus Status);
    private sealed record AssignmentProjection(
        FuelCardRiderAssignment Assignment,
        string CardNumber,
        string RiderNameAr,
        string? RiderNameEn);
    private sealed record CurrentRiderProjection(
        Guid FuelCardId,
        Guid AssignmentId,
        Guid RiderProfileId,
        Guid EmployeeId,
        string RiderNameAr,
        string? RiderNameEn,
        DateOnly EffectiveFrom,
        byte[] RowVersion);
    private sealed record MonthlyProjection(
        FuelCardMonthlyUsage Usage,
        FuelCard Card,
        string RiderNameAr,
        string? RiderNameEn);
}
