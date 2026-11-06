using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public static class HungerAccountVerification
{
    public static async Task<Result<IReadOnlyList<HungerAccountItem>>> ReadAllAsync(IHungerAccountsService service, CancellationToken ct = default)
    {
        var rows = new List<HungerAccountItem>();
        int? expected = null;
        for (var page = 1; ; page++)
        {
            var result = await service.AccountsAsync(null, page, 100, ct);
            if (result.IsFailure) return Result.Failure<IReadOnlyList<HungerAccountItem>>(result.Error);
            var value = result.Value!;
            expected ??= value.TotalCount;
            if (expected != value.TotalCount || value.Items.Count == 0 && rows.Count < expected)
                return Result.Failure<IReadOnlyList<HungerAccountItem>>(HungerErrors.Conflict("Account inventory changed during verification; retry."));
            rows.AddRange(value.Items);
            if (rows.Count >= expected)
            {
                if (rows.Count != expected || rows.Select(r => r.AccountId).Distinct().Count() != expected)
                    return Result.Failure<IReadOnlyList<HungerAccountItem>>(HungerErrors.Conflict("Account verification found duplicate or missing rows."));
                return Result.Success<IReadOnlyList<HungerAccountItem>>(rows);
            }
        }
    }
}
