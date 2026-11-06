using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

// Trusted background-job hook. HTTP consumers use the permission-scoped accounts service.
public interface IHungerDeferredReminderQuery
{
    Task<Result<IReadOnlyList<HungerDeferredReminder>>> FindDueAsync(Guid platformId, DateOnly? dueThrough = null, CancellationToken ct = default);
}
