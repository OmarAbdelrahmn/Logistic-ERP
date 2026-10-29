using System.Security.Cryptography;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    private async Task<Result<NinjaImportResponse>> ImportPerformanceLegacy(DateOnly date, string name, byte[] content, CancellationToken ct)
    {
        var platforms = await AllowedPlatforms(PermissionKeys.Ninja.PerformanceImport, ct);
        if (platforms.Length == 0) return Result.Failure<NinjaImportResponse>(NinjaErrors.Forbidden);
        if (date == default || date > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime)
            || string.IsNullOrWhiteSpace(name) || name.Length > 255 || !name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || content.Length is 0 or > NinjaSpreadsheetParser.MaxBytes)
            return Result.Failure<NinjaImportResponse>(NinjaErrors.Invalid("حدد تاريخ التقرير وارفع ملف xlsx بحد أقصى 10 ميجابايت."));
        // Retain the legacy parser/response contract; only the allocation and commit core is shared.
        var parsed = NinjaSpreadsheetParser.Parse(content, false, ct);
        if (parsed.Issues.Count > 0) return Result.Success(new NinjaImportResponse(null, date, 0, 0, parsed.Issues));
        var rows = parsed.Rows.Select(r => new NinjaPerformanceInput(r.RowNumber, r.CaptainId, date, r.TotalOrders, r.WorkingHours)).ToArray();
        var ids = await NinjaPerformanceImportEngine.AccountIds(db, platforms, rows, ct);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids, ct);
                var plan = await NinjaPerformanceImportEngine.Prepare(db, platforms, rows, name, Convert.ToHexString(SHA256.HashData(content)), ct);
                if (!plan.Valid) return Result.Success(new NinjaImportResponse(null, date, 0, 0,
                    plan.Rows.Where(r => r.Issues.Count > 0).SelectMany(r => r.Issues.Select(i => new NinjaImportIssue(r.RowNumber, r.CaptainId, i))).ToArray()));
                // The same batch replay is read-only; don't duplicate the artifact or create extra history.
                if (plan.Imports.Count == 0) return Result.Success(plan.Results[0]);
                plan.Stage(db); await NinjaPerformanceImportService.Artifact(db, plan, null, name, content, ct);
                return await Persist(plan.Results[0], ct);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<NinjaImportResponse>(NinjaErrors.Conflict); }
    }
}
