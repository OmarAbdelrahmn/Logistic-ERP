using System.Globalization;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace LogisticsERP.Infrastructure.Hr;
internal sealed class MonthlyAccountUsageService(ApplicationDbContext db, TimeProvider clock) : IMonthlyAccountUsageService
{
    public async Task<MonthlyAccountUsage> GetAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(3)).DateTime);
        var first = new DateOnly(today.Year, today.Month, 1);
        var start = new DateTimeOffset(first.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
        var used = await db.RiderClientAssignments.AsNoTracking().Where(a => accountIds.Contains(a.PlatformRiderAccountId)
            && (a.Status == RiderAssignmentStatus.Active || a.Status == RiderAssignmentStatus.Ended)
            && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= first)
            && (!db.Set<JahezAccountSession>().Any(h => h.RiderClientAssignmentId == a.Id)
                || db.Set<JahezAccountSession>().Any(h => h.RiderClientAssignmentId == a.Id && h.StartedAtUtc <= now
                    && (h.EndedAtUtc == null || h.EndedAtUtc > start) && (h.EndedAtUtc == null || h.EndedAtUtc > h.StartedAtUtc))))
            .Select(a => a.PlatformRiderAccountId).Distinct().ToArrayAsync(cancellationToken);
        return new(first.ToString("yyyy-MM", CultureInfo.InvariantCulture), used.ToHashSet());
    }
}
