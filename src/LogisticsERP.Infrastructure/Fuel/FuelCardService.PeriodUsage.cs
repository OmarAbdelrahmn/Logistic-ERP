using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Fuel;
using LogisticsERP.Domain.Fuel;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Fuel;

internal sealed partial class FuelCardService
{
    public async Task<Result<FuelCardPeriodUsagePageResponse>> GetPeriodUsageAsync(
        DateOnly startDate, DateOnly endDate, string? provider, string? search,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(PermissionKeys.Fuel.Read, cancellationToken))
            return Result.Failure<FuelCardPeriodUsagePageResponse>(FuelErrors.Forbidden);

        var monthCount = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month + 1;
        if (startDate == default || endDate == default || startDate > endDate || monthCount > 36)
            return Result.Failure<FuelCardPeriodUsagePageResponse>(FuelErrors.InvalidReportPeriod);

        var monthFrom = FuelCardRules.MonthStart(startDate);
        var monthTo = FuelCardRules.MonthStart(endDate);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        page = Math.Clamp(page, 1, int.MaxValue / pageSize);

        var usageQuery = dbContext.FuelCardMonthlyUsages.AsNoTracking()
            .Where(usage => usage.ReportMonth >= monthFrom && usage.ReportMonth <= monthTo);
        var cardQuery = dbContext.FuelCards.IgnoreQueryFilters().AsNoTracking()
            .Where(card => usageQuery.Any(usage => usage.FuelCardId == card.Id));

        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (!TryParseProvider(provider, out var parsedProvider))
                return Result.Failure<FuelCardPeriodUsagePageResponse>(FuelErrors.InvalidProvider);
            cardQuery = cardQuery.Where(card => card.Provider == parsedProvider);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = NormalizeSearchIdentifier(term);
            cardQuery = cardQuery.Where(card => card.CardNumber.Contains(term)
                || card.PlateNumberText != null && card.PlateNumberText.Contains(term)
                || normalized.Length > 0 && card.NormalizedCardNumber.Contains(normalized));
        }

        var totalCount = await cardQuery.CountAsync(cancellationToken);
        var totals = await usageQuery
            .Join(cardQuery, usage => usage.FuelCardId, card => card.Id, (usage, _) => usage)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalLiters = group.Sum(usage => usage.TotalLiters),
                TotalAmount = group.Sum(usage => usage.TotalAmount),
                UnassignedTotalLiters = group.Sum(usage => usage.RiderProfileId == null ? usage.TotalLiters : 0m),
                UnassignedTotalAmount = group.Sum(usage => usage.RiderProfileId == null ? usage.TotalAmount : 0m)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var cards = await cardQuery
            .OrderBy(card => card.Provider)
            .ThenBy(card => card.NormalizedCardNumber)
            .ThenBy(card => card.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        var cardIds = cards.Select(card => card.Id).ToArray();
        var usages = await usageQuery
            .Where(usage => cardIds.Contains(usage.FuelCardId))
            .Select(usage => new
            {
                usage.FuelCardId,
                usage.RiderProfileId,
                usage.EmployeeId,
                usage.ReportMonth,
                usage.TotalLiters,
                usage.TotalAmount
            })
            .ToArrayAsync(cancellationToken);
        var assignments = await dbContext.FuelCardRiderAssignments.AsNoTracking()
            .Where(assignment => cardIds.Contains(assignment.FuelCardId)
                && assignment.EffectiveFrom <= endDate
                && (assignment.EffectiveTo == null || assignment.EffectiveTo >= startDate))
            .Select(assignment => new
            {
                assignment.Id,
                assignment.FuelCardId,
                assignment.RiderProfileId,
                assignment.EmployeeId,
                assignment.EffectiveFrom,
                assignment.EffectiveTo
            })
            .ToArrayAsync(cancellationToken);
        var employeeIds = usages.Where(usage => usage.EmployeeId.HasValue)
            .Select(usage => usage.EmployeeId!.Value)
            .Concat(assignments.Select(assignment => assignment.EmployeeId))
            .Distinct().ToArray();
        var employeeNames = await dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id))
            .Select(employee => new { employee.Id, employee.FullNameAr, employee.FullNameEn })
            .ToDictionaryAsync(employee => employee.Id, cancellationToken);

        var items = cards.Select(card =>
        {
            var cardUsages = usages.Where(usage => usage.FuelCardId == card.Id).ToArray();
            var cardAssignments = assignments.Where(assignment => assignment.FuelCardId == card.Id).ToArray();
            var riderKeys = cardUsages
                .Where(usage => usage.RiderProfileId.HasValue && usage.EmployeeId.HasValue)
                .Select(usage => (RiderProfileId: usage.RiderProfileId!.Value, EmployeeId: usage.EmployeeId!.Value))
                .Concat(cardAssignments.Select(assignment => (assignment.RiderProfileId, assignment.EmployeeId)))
                .DistinctBy(key => key.RiderProfileId);
            var riders = riderKeys.Select(key =>
            {
                var riderUsages = cardUsages.Where(usage => usage.RiderProfileId == key.RiderProfileId).ToArray();
                var riderAssignments = cardAssignments.Where(assignment => assignment.RiderProfileId == key.RiderProfileId)
                    .OrderBy(assignment => assignment.EffectiveFrom)
                    .Select(assignment => new FuelCardPeriodAssignmentResponse(
                        assignment.Id, assignment.EffectiveFrom, assignment.EffectiveTo,
                        assignment.EffectiveFrom > startDate ? assignment.EffectiveFrom : startDate,
                        assignment.EffectiveTo is { } end && end < endDate ? end : endDate))
                    .ToArray();
                employeeNames.TryGetValue(key.EmployeeId, out var employee);
                return new FuelCardPeriodRiderResponse(
                    key.RiderProfileId, key.EmployeeId, employee?.FullNameAr, employee?.FullNameEn,
                    riderUsages.Sum(usage => usage.TotalLiters),
                    riderUsages.Sum(usage => usage.TotalAmount),
                    riderAssignments,
                    riderUsages.OrderBy(usage => usage.ReportMonth)
                        .Select(usage => new FuelCardPeriodMonthResponse(
                            usage.ReportMonth, usage.TotalLiters, usage.TotalAmount))
                        .ToArray());
            }).OrderBy(rider => rider.Assignments.Count > 0 ? rider.Assignments[0].From : DateOnly.MaxValue)
              .ThenBy(rider => rider.RiderProfileId).ToArray();
            var unassignedMonths = cardUsages.Where(usage => usage.RiderProfileId == null)
                .OrderBy(usage => usage.ReportMonth)
                .Select(usage => new FuelCardPeriodMonthResponse(
                    usage.ReportMonth, usage.TotalLiters, usage.TotalAmount))
                .ToArray();
            return new FuelCardPeriodUsageResponse(
                card.Id, card.Provider.ToString(), ProviderNameAr(card.Provider),
                card.CardNumber, card.PlateNumberText,
                cardUsages.Sum(usage => usage.TotalLiters),
                cardUsages.Sum(usage => usage.TotalAmount),
                unassignedMonths.Sum(usage => usage.TotalLiters),
                unassignedMonths.Sum(usage => usage.TotalAmount),
                unassignedMonths, riders);
        }).ToArray();

        return Result.Success(new FuelCardPeriodUsagePageResponse(
            items, startDate, endDate, monthFrom, monthTo, page, pageSize, totalCount,
            totals?.TotalLiters ?? 0m, totals?.TotalAmount ?? 0m,
            totals?.UnassignedTotalLiters ?? 0m, totals?.UnassignedTotalAmount ?? 0m));
    }
}
