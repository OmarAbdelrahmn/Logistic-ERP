using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed class HungerDeferredReminderQuery(ApplicationDbContext db, TimeProvider clock, IConfiguration configuration) : IHungerDeferredReminderQuery
{
    public async Task<Result<IReadOnlyList<HungerDeferredReminder>>> FindDueAsync(Guid platformId, DateOnly? dueThrough = null, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("Features:Hunger") || !await db.ClientPlatforms.AnyAsync(p => p.Id == platformId && p.Code == "HUNGER", ct))
            return Result.Failure<IReadOnlyList<HungerDeferredReminder>>(HungerErrors.Forbidden);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        var due = dueThrough ?? today;
        if (due == default || due > today) return Result.Failure<IReadOnlyList<HungerDeferredReminder>>(HungerErrors.Invalid("Use a due date no later than today."));
        return Result.Success(await ReadDueAsync(db, [platformId], today, due, ct));
    }

    internal static async Task<IReadOnlyList<HungerDeferredReminder>> ReadDueAsync(ApplicationDbContext db, Guid[] platforms, DateOnly today, DateOnly due, CancellationToken ct)
    {
        var rows = await (from d in db.Set<PlatformAccountBusinessDetails>().AsNoTracking()
            join a in db.PlatformRiderAccounts.AsNoTracking() on d.PlatformRiderAccountId equals a.Id
            where platforms.Contains(a.ClientPlatformId) && d.OperationalDisposition == "DeferredNextMonth" && d.DeferredUntil <= due
            orderby d.DeferredUntil, a.ExternalAccountId, a.Id
            select new { a.Id, a.ClientPlatformId, a.ExternalAccountId, d.DeferredUntil }).ToArrayAsync(ct);
        return rows.Select(r => new HungerDeferredReminder(r.Id, r.ClientPlatformId, r.ExternalAccountId, r.DeferredUntil!.Value,
            today.DayNumber - r.DeferredUntil.Value.DayNumber)).ToArray();
    }
}
