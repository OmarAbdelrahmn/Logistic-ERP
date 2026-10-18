using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    public async Task<Result<NinjaAccountComparisonResponse>> CompareAccountsAsync(string fileName, byte[] content, CancellationToken ct = default)
    {
        var ids = await AllowedPlatforms(PermissionKeys.Ninja.Read, ct);
        if (ids.Length == 0) return Result.Failure<NinjaAccountComparisonResponse>(NinjaErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255
            || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes)
            return Result.Failure<NinjaAccountComparisonResponse>(NinjaErrors.Invalid("ارفع ملف xlsx بحد أقصى 10 ميجابايت."));
        var parsed = NinjaAccountSheetParser.Parse(content, ct);
        NinjaAccountComparisonResponse Incomplete(IReadOnlyList<NinjaImportIssue> issues, int systemCount = 0) =>
            new(Path.GetFileName(fileName), clock.GetUtcNow(), false, parsed.IgnoredFooterRows,
                new(parsed.Rows.Count, systemCount, 0, 0, 0, 0), [], [], [], [], issues);
        // Invalid input must not create a misleading 'missing in sheet' report.
        if (parsed.Issues.Count > 0) return Result.Success(Incomplete(parsed.Issues));

        var system = await ComparisonAccounts(ids).Take(NinjaAccountSheetParser.MaxAccounts + 1).ToArrayAsync(ct);
        if (system.Length > NinjaAccountSheetParser.MaxAccounts)
            return Result.Failure<NinjaAccountComparisonResponse>(NinjaErrors.Invalid("المقارنة تدعم حتى 10000 حساب في النظام."));
        var grouped = system.ToLookup(a => NinjaSpreadsheetParser.NormalizeId(a.CaptainId), StringComparer.Ordinal);
        var ambiguous = grouped.Where(g => g.Count() > 1).Select(g =>
            new NinjaImportIssue(0, g.Key, "أكثر من حساب في النظام له نفس المعرف بعد توحيد الأرقام؛ صحح التعارض قبل المقارنة.")).ToArray();
        if (ambiguous.Length > 0) return Result.Success(Incomplete(ambiguous, system.Length));
        var sheetIds = parsed.Rows.Select(r => r.CaptainId).ToHashSet(StringComparer.Ordinal);
        var systemOnly = system.Where(a => !sheetIds.Contains(NinjaSpreadsheetParser.NormalizeId(a.CaptainId))).ToArray();
        var byOwner = systemOnly.Where(a => a.OwnerNationalId is not null)
            .GroupBy(a => NinjaSpreadsheetParser.NormalizeId(a.OwnerNationalId!), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var matching = new List<NinjaComparedAccount>(); var different = new List<NinjaComparedAccount>();
        var sheetOnly = new List<NinjaSheetOnlyAccount>();
        foreach (var row in parsed.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var account = grouped[row.CaptainId].SingleOrDefault();
            if (account is null)
            {
                // These are review candidates, not proof of an account-number change.
                var candidates = byOwner.GetValueOrDefault(row.OwnerNationalId) ?? [];
                sheetOnly.Add(new(row, candidates.Take(20).ToArray(), candidates.Length));
                continue;
            }
            var differences = new List<NinjaAccountDifference>();
            if (row.OwnerNationalId != NinjaSpreadsheetParser.NormalizeId(account.OwnerNationalId ?? ""))
                differences.Add(new("ownerNationalId", row.OwnerNationalId, account.OwnerNationalId));
            if (!Same(row.OwnerName, account.OwnerNameAr) && !Same(row.OwnerName, account.OwnerNameEn))
                differences.Add(new("ownerName", row.OwnerName, account.OwnerNameEn ?? account.OwnerNameAr));
            if (!Same(row.City, account.CityCode) && !Same(row.City, account.CityNameAr) && !Same(row.City, account.CityNameEn))
                differences.Add(new("city", row.City, account.CityNameEn ?? account.CityNameAr));
            var compared = new NinjaComparedAccount(row, account, differences);
            if (differences.Count == 0) matching.Add(compared); else different.Add(compared);
        }
        return Result.Success(new NinjaAccountComparisonResponse(Path.GetFileName(fileName), clock.GetUtcNow(), true,
            parsed.IgnoredFooterRows, new(parsed.Rows.Count, system.Length, matching.Count, different.Count, sheetOnly.Count, systemOnly.Length),
            matching, different, sheetOnly, systemOnly, []));
    }

    // Left joins retain accounts with unavailable owner/city references, which are then shown as differences.
    internal IQueryable<NinjaComparisonSystemAccount> ComparisonAccounts(Guid[] platformIds) =>
        from a in Accounts.AsNoTracking()
        join owner in db.Employees.AsNoTracking() on a.RegisteredEmployeeId equals owner.Id into owners
        from owner in owners.DefaultIfEmpty()
        join city in db.OperatingCities.AsNoTracking() on a.OperatingCityId equals city.Id into cities
        from city in cities.DefaultIfEmpty()
        join global in db.GlobalCities.AsNoTracking() on city.GlobalCityId equals global.Id into globals
        from global in globals.DefaultIfEmpty()
        where platformIds.Contains(a.ClientPlatformId)
        orderby a.ExternalAccountId, a.Id
        select new NinjaComparisonSystemAccount(a.Id, a.ExternalAccountId, a.Code, a.Status.ToString(),
            a.RegisteredEmployeeId, owner == null ? null : owner.IqamaNo,
            owner == null ? null : owner.FullNameAr, owner == null ? null : owner.FullNameEn,
            a.OperatingCityId, global == null ? null : global.Code, global == null ? null : global.NameAr,
            global == null ? null : global.NameEn);

    private static bool Same(string first, string? second) => second is not null &&
        string.Equals(NormalizeText(first), NormalizeText(second), StringComparison.OrdinalIgnoreCase);
    private static string NormalizeText(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
