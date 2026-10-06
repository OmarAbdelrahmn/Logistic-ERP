using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Fuel;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed partial class FuelCardService
{
    public async Task<Result<FuelUnassignedUsagePageResponse>> GetUnassignedUsageAsync(
        DateOnly from, DateOnly to, string? provider, string? search,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
            return Result.Failure<FuelUnassignedUsagePageResponse>(FuelErrors.Forbidden);

        var monthCount = (to.Year - from.Year) * 12 + to.Month - from.Month + 1;
        if (from == default || to == default || from > to || monthCount > 36)
            return Result.Failure<FuelUnassignedUsagePageResponse>(FuelErrors.InvalidReportPeriod);

        var monthFrom = FuelCardRules.MonthStart(from);
        var monthTo = FuelCardRules.MonthStart(to);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        page = Math.Clamp(page, 1, int.MaxValue / pageSize);

        var query = from usage in dbContext.FuelCardMonthlyUsages.AsNoTracking()
                    join card in dbContext.FuelCards.IgnoreQueryFilters().AsNoTracking()
                        on usage.FuelCardId equals card.Id
                    join import in dbContext.FuelCardImports.AsNoTracking()
                        on usage.LastImportId equals import.Id
                    where usage.RiderProfileId == null
                        && usage.ReportMonth >= monthFrom
                        && usage.ReportMonth <= monthTo
                    select new { Usage = usage, Card = card, Import = import };

        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (!TryParseProvider(provider, out var parsedProvider))
                return Result.Failure<FuelUnassignedUsagePageResponse>(FuelErrors.InvalidProvider);
            query = query.Where(x => x.Card.Provider == parsedProvider);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = NormalizeSearchIdentifier(term);
            query = query.Where(x => x.Card.CardNumber.Contains(term)
                || x.Card.PlateNumberText != null && x.Card.PlateNumberText.Contains(term)
                || x.Usage.SourcePlateNumber != null && x.Usage.SourcePlateNumber.Contains(term)
                || normalized.Length > 0 && x.Card.NormalizedCardNumber.Contains(normalized));
        }

        var totals = await query.GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Liters = group.Sum(x => x.Usage.TotalLiters),
                Amount = group.Sum(x => x.Usage.TotalAmount)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Usage.ReportMonth)
            .ThenBy(x => x.Card.Provider)
            .ThenBy(x => x.Card.NormalizedCardNumber)
            .ThenBy(x => x.Usage.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FuelUnassignedUsageResponse(
                x.Usage.Id,
                x.Card.Id,
                x.Card.Provider.ToString(),
                x.Card.CardNumber,
                x.Usage.SourcePlateNumber ?? x.Card.PlateNumberText,
                x.Card.Notes,
                x.Usage.ReportMonth,
                x.Usage.TotalLiters,
                x.Usage.TotalAmount,
                x.Usage.TransactionCount,
                x.Usage.FirstTransactionAtUtc,
                x.Usage.LastTransactionAtUtc,
                x.Import.Id,
                x.Import.OriginalFileName,
                x.Import.CreatedAtUtc,
                "card_not_assigned"))
            .ToArrayAsync(cancellationToken);

        return Result.Success(new FuelUnassignedUsagePageResponse(
            items, from, to, page, pageSize, totals?.Count ?? 0,
            totals?.Liters ?? 0m, totals?.Amount ?? 0m));
    }
}
