using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record PlatformAccountDayUsageInput(int? OrdersCount = null, decimal? WorkingHours = null,
    decimal? CollectedAmount = null, decimal? DeductionsAmount = null, int? TotalValidOffers = null,
    int? DeclinedOffers = null, int? MissedOffers = null);
public sealed record PlatformAccountDayUsageUpdate(Guid AssignmentId, PlatformAccountDayUsageInput? Figures,
    string? RowVersion = null);
public sealed record PlatformAccountDayUpdateRequest(IReadOnlyList<PlatformAccountDayUsageUpdate> Assignments, string Reason);
public sealed record PlatformAccountDayParticipant(Guid AssignmentId, Guid RiderProfileId, int? Sequence,
    PlatformAccountDayUsageInput Figures, string? RowVersion, IReadOnlyList<string> MissingFields);
public sealed record PlatformAccountDayResponse(Guid AccountId, DateOnly BusinessDate,
    IReadOnlyList<PlatformAccountDayParticipant> Assignments);
public enum PlatformDayMetric { OrdersCount, WorkingHours, CollectedAmount, DeductionsAmount, TotalValidOffers, DeclinedOffers, MissedOffers }
public sealed record PlatformDayShare(Guid AssignmentId, Guid RiderProfileId, int Sequence,
    IReadOnlyDictionary<PlatformDayMetric, decimal> Values);
public interface IPlatformAccountDayAllocationService
{
    Task<Result<IReadOnlyList<PlatformDayShare>>> AllocateAsync(Guid accountId, DateOnly businessDate,
        IReadOnlyDictionary<PlatformDayMetric, decimal> totals, CancellationToken ct = default);
}
public interface IPlatformAccountDayUsageService
{
    Task<Result<PlatformAccountDayResponse>> GetAsync(Guid accountId, DateOnly businessDate, CancellationToken ct = default);
    Task<Result<PlatformAccountDayResponse>> UpdateAsync(Guid accountId, DateOnly businessDate,
        PlatformAccountDayUpdateRequest request, CancellationToken ct = default);
}
