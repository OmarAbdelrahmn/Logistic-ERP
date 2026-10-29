using System.Security.Cryptography;
using System.Text.Json;
using ClosedXML.Excel;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Hr;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed class NinjaPerformanceImportService(ApplicationDbContext db, ICurrentUser user,
    IPermissionChecker permissions, TimeProvider clock, IConfiguration configuration) : INinjaPerformanceImportService
{
    private async Task<Guid[]> Platforms(CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Features:NinjaEvaluation") || user.UserId is not { } id || user.AuthorizationVersion is not { } version) return [];
        var candidates = await db.ClientPlatforms.AsNoTracking().Where(p => p.Code == "NINJA").Select(p => p.Id).ToArrayAsync(ct);
        var ids = new List<Guid>();
        foreach (var p in candidates)
            if (await permissions.HasPermissionAsync(id, version, PermissionKeys.Ninja.PerformanceImport, new(AccessScopeType.ClientPlatform, p), ct)) ids.Add(p);
        return ids.ToArray();
    }
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
    private static string Fingerprint(byte[] content) => Convert.ToHexString(SHA256.HashData(content));
    public async Task<Result<NinjaEvaluationFile>> TemplateAsync(string mode, CancellationToken ct = default)
    {
        if ((await Platforms(ct)).Length == 0) return Result.Failure<NinjaEvaluationFile>(NinjaErrors.Forbidden);
        if (mode is not ("Daily" or "MultiDay")) return Result.Failure<NinjaEvaluationFile>(NinjaErrors.Invalid("اختر Daily أو MultiDay."));
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Performance");
        sheet.Cell(1, 1).Value = "Captain ID"; sheet.Cell(1, 2).Value = "Total Orders"; sheet.Cell(1, 3).Value = "Total Working Hours";
        sheet.Column(1).Style.NumberFormat.Format = "@";
        if (mode == "MultiDay") { sheet.Cell(1, 4).Value = "Date"; sheet.Column(4).Style.NumberFormat.Format = "yyyy-mm-dd"; }
        sheet.Row(1).Style.Font.Bold = true; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return Result.Success(new NinjaEvaluationFile(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Ninja-{mode}-template.xlsx"));
    }
    public async Task<Result<NinjaPerformancePreviewResponse>> PreviewAsync(string fileName, byte[] content, NinjaPerformanceImportOptions options, CancellationToken ct = default)
    {
        var platforms = await Platforms(ct);
        if (platforms.Length == 0) return Result.Failure<NinjaPerformancePreviewResponse>(NinjaErrors.Forbidden);
        var parsed = NinjaPerformanceParser.Parse(fileName, content, options, Today, ct);
        var hash = Fingerprint(content); var rows = new List<NinjaPerformancePreviewRow>();
        NinjaPerformanceImportEngine.Plan? plan = null;
        if (parsed.Rows.Count > 0)
        {
            plan = await NinjaPerformanceImportEngine.Prepare(db, platforms, parsed.Rows, fileName, hash, ct);
            rows.AddRange(plan.Rows);
        }
        rows.AddRange(parsed.Issues.Select(i => new NinjaPerformancePreviewRow(i.RowNumber, i.CaptainId ?? string.Empty, default, null, 0, 0, "Invalid", [i.Message])));
        var expires = clock.GetUtcNow().AddMinutes(30);
        var preview = new NinjaPerformancePreview { UserId = user.UserId!.Value, ContentHash = hash,
            OptionsJson = JsonSerializer.Serialize(options, NinjaEvaluationService.Json), InputRevision = plan?.InputRevision ?? string.Empty, ExpiresAtUtc = expires };
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(new NinjaPerformancePreviewResponse(preview.Id, expires, hash, preview.InputRevision,
            rows.Count > 0 && rows.All(r => r.Issues.Count == 0), rows));
    }
    public async Task<Result<NinjaPerformanceConfirmation>> ConfirmAsync(Guid previewId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var platforms = await Platforms(ct);
        if (platforms.Length == 0) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Forbidden);
        var preview = await db.Set<NinjaPerformancePreview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == previewId && p.UserId == user.UserId, ct);
        if (preview is null) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.NotFound);
        if (preview.ContentHash != Fingerprint(content)) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Invalid("الملف لا يطابق المعاينة."));
        var options = JsonSerializer.Deserialize<NinjaPerformanceImportOptions>(preview.OptionsJson, NinjaEvaluationService.Json)!;
        var parsed = NinjaPerformanceParser.Parse(fileName, content, options, Today, ct);
        if (parsed.Issues.Count > 0) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Invalid("الملف غير صالح؛ أنشئ معاينة جديدة."));
        var ids = await NinjaPerformanceImportEngine.AccountIds(db, platforms, parsed.Rows, ct);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids, ct);
                var existing = await db.Set<NinjaPerformanceArtifact>().AsNoTracking().SingleOrDefaultAsync(a => a.PreviewId == previewId, ct);
                // Check current record scope even for an idempotent replay.
                if (ids.Length != parsed.Rows.Select(r => r.CaptainId).Distinct().Count()) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Forbidden);
                if (existing is not null) return Result.Success(JsonSerializer.Deserialize<NinjaPerformanceConfirmation>(existing.ResultJson, NinjaEvaluationService.Json)!);
                if (preview.ExpiresAtUtc <= clock.GetUtcNow()) return Result.Failure<NinjaPerformanceConfirmation>(new("ninja.preview_expired", "انتهت المعاينة؛ أنشئ معاينة جديدة.", ErrorType.Conflict));
                var plan = await NinjaPerformanceImportEngine.Prepare(db, platforms, parsed.Rows, fileName, preview.ContentHash, ct);
                if (preview.InputRevision != plan.InputRevision) return Result.Failure<NinjaPerformanceConfirmation>(new("ninja.preview_stale", "تغيرت التكليفات أو الأوزان؛ أنشئ معاينة جديدة.", ErrorType.Conflict));
                if (!plan.Valid) return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Invalid("تعارض أو تخصيص غير مكتمل؛ لم تحفظ أي بيانات."));
                plan.Stage(db);
                var result = await Artifact(db, plan, previewId, fileName, content, ct);
                await db.SaveChangesAsync(ct);
                return Result.Success(result);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<NinjaPerformanceConfirmation>(NinjaErrors.Conflict); }
    }

    internal static async Task<NinjaPerformanceConfirmation> Artifact(ApplicationDbContext db, NinjaPerformanceImportEngine.Plan plan,
        Guid? preview, string fileName, byte[] content, CancellationToken ct)
    {
        var hash = Fingerprint(content);
        var original = await db.Set<NinjaPerformanceArtifact>().AsNoTracking().Where(a => a.ContentHash == hash && a.OriginalArtifactId == null)
            .Select(a => (Guid?)a.Id).FirstOrDefaultAsync(ct);
        var artifact = new NinjaPerformanceArtifact { PreviewId = preview, OriginalArtifactId = original, ContentHash = hash,
            FileName = Path.GetFileName(fileName), Format = Path.GetExtension(fileName).ToLowerInvariant(), Content = original is null ? content : [] };
        var result = new NinjaPerformanceConfirmation(artifact.Id, plan.Results.Sum(r => r.ImportedRows), plan.Results.Sum(r => r.SkippedRows), plan.Results);
        artifact.ResultJson = JsonSerializer.Serialize(result, NinjaEvaluationService.Json);
        db.Add(artifact);
        db.AddRange(plan.Results.Where(r => r.ImportId.HasValue).Select(r => new NinjaPerformanceImportArtifact { ArtifactId = artifact.Id, ImportId = r.ImportId!.Value }));
        return result;
    }
}
