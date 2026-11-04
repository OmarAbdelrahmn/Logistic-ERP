using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Infrastructure.Hr;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerOperationsService
{
    private static readonly string[] OperationHeaders = ["Captain ID", "CompletedOrders", "ActualWorkingHours", "RejectedOrders", "CancelledOrders", "NoShowCount"];
    private sealed record OperationEnvelope(HungerOperationInput[] Inputs, HungerImportRow[] Errors);
    private sealed record OperationChange(HungerOperationInput Input, Guid AccountId, IReadOnlyList<PlatformDayShare> Shares);
    private sealed record OperationPlan(string Revision, HungerImportRow[] Rows, OperationChange[] Changes);
    public async Task<Result<HungerFile>> OperationTemplateAsync(CancellationToken ct = default)
    {
        if ((await Platforms(PermissionKeys.Hunger.PerformanceImport, ct)).Length == 0) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Operations");
        for (var i = 0; i < OperationHeaders.Length; i++) sheet.Cell(1, i + 1).Value = OperationHeaders[i];
        sheet.Column(1).Style.NumberFormat.Format = "@"; sheet.Row(1).Style.Font.Bold = true; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return Result.Success(new HungerFile(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Hunger-Daily-Operations.xlsx"));
    }
    private static OperationEnvelope ParseOperations(byte[] content)
    {
        using var book = new XLWorkbook(new MemoryStream(content)); var sheet = book.Worksheet(1);
        var headers = sheet.Row(1).CellsUsed().ToLookup(c => new string(c.GetString().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()));
        var columns = new Dictionary<string, int>();
        foreach (var key in OperationHeaders)
        {
            var found = headers[new string(key.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray())].ToArray();
            if (found.Length != 1) throw new FormatException($"العمود {key} مفقود أو مكرر.");
            columns[key] = found[0].Address.ColumnNumber;
        }
        var rows = sheet.RowsUsed().Skip(1).ToArray();
        if (rows.Length is 0 or > HungerQualityParser.MaxRows) throw new FormatException("عدد الصفوف غير صالح.");
        var values = new List<HungerOperationInput>(); var errors = new List<HungerImportRow>();
        foreach (var row in rows)
        {
            var account = row.Cell(columns["Captain ID"]).GetString().Trim();
            try
            {
                decimal? Number(string key, decimal max, bool required)
                {
                    var cell = row.Cell(columns[key]); var text = cell.GetString();
                    if (string.IsNullOrWhiteSpace(text) && !required) return null;
                    if (cell.HasFormula || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0 || value > max
                        || key != "ActualWorkingHours" && decimal.Truncate(value) != value) throw new FormatException($"قيمة {key} غير صالحة.");
                    return value;
                }
                if (string.IsNullOrWhiteSpace(account) || account.Length > 150) throw new FormatException("معرف هنقر مطلوب.");
                values.Add(new(row.RowNumber(), account, (int)Number("CompletedOrders", int.MaxValue, true)!, Number("ActualWorkingHours", 24, true)!.Value,
                    (int?)Number("RejectedOrders", int.MaxValue, false), (int?)Number("CancelledOrders", int.MaxValue, false), (int?)Number("NoShowCount", int.MaxValue, false)));
            }
            catch (FormatException e) { errors.Add(new(row.RowNumber(), account, "Rejected", e.Message)); }
        }
        return new(values.ToArray(), errors.ToArray());
    }
    private async Task<OperationPlan> PrepareOperations(OperationEnvelope input, Guid[] platforms, DateOnly date, CancellationToken ct)
    {
        var codes = input.Inputs.Select(i => i.AccountId).ToArray();
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var ids = accounts.Select(a => a.Id).ToArray();
        var saved = await db.Set<HungerDailyOperation>().AsNoTracking().Where(p => ids.Contains(p.PlatformRiderAccountId) && p.ReportDate == date).OrderBy(p => p.Id).ToArrayAsync(ct);
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId) && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date)).OrderBy(a => a.Id).ToArrayAsync(ct);
        var usages = await db.PlatformAccountDayUsages.AsNoTracking().Where(u => ids.Contains(u.PlatformRiderAccountId) && u.BusinessDate == date).OrderBy(u => u.Id).ToArrayAsync(ct);
        var contributions = await db.Set<LogisticsERP.Domain.Entities.Clients.PlatformAccountDayContribution>().AsNoTracking().Where(c => ids.Contains(c.PlatformRiderAccountId) && c.BusinessDate == date).OrderBy(c => c.Id).ToArrayAsync(ct);
        var rows = input.Errors.ToList(); var changes = new List<OperationChange>(); var seen = new HashSet<Guid>();
        foreach (var source in input.Inputs)
        {
            var matches = accounts.Where(a => a.ExternalAccountId == source.AccountId).ToArray();
            if (matches.Length != 1) { rows.Add(new(source.RowNumber, source.AccountId, "Rejected", "المعرف غير معروف أو غير فريد.")); continue; }
            var account = matches[0];
            if (!seen.Add(account.Id)) { rows.Add(new(source.RowNumber, source.AccountId, "Rejected", "معرف مكرر في الملف.")); continue; }
            var old = saved.SingleOrDefault(s => s.PlatformRiderAccountId == account.Id);
            if (old is not null)
            {
                var same = old.CompletedOrders == source.CompletedOrders && old.ActualWorkingHours == source.ActualWorkingHours && old.RejectedOrders == source.RejectedOrders && old.CancelledOrders == source.CancelledOrders && old.NoShowCount == source.NoShowCount;
                rows.Add(new(source.RowNumber, source.AccountId, same ? "Skipped" : "Rejected", same ? null : "يوجد تقرير مختلف لهذا اليوم؛ لم يستبدل.")); continue;
            }
            var allocation = await new PlatformAccountDayAllocationService(db).AllocateAsync(account.Id, date,
                new Dictionary<PlatformDayMetric, decimal> { [PlatformDayMetric.OrdersCount] = source.CompletedOrders, [PlatformDayMetric.WorkingHours] = source.ActualWorkingHours }, ct);
            var shares = allocation.IsSuccess ? allocation.Value! : [];
            changes.Add(new(source, account.Id, shares));
            rows.Add(new(source.RowNumber, source.AccountId, "New", null, AllocationStatus: shares.Count > 0 ? "Resolved" : "UnresolvedAllocation"));
        }
        return new(Hash(Serialize(new { accounts, saved, assignments, usages, contributions })), rows.OrderBy(r => r.RowNumber).ToArray(), changes.ToArray());
    }
    public async Task<Result<HungerQualityPreview>> PreviewOperationsAsync(string fileName, byte[] content, DateOnly reportDate, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceImport, ct);
        if (platforms.Length == 0) return Result.Failure<HungerQualityPreview>(HungerErrors.Forbidden);
        if (reportDate == default || reportDate > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime)
            || content.Length is 0 or > HungerQualityParser.MaxBytes || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return Result.Failure<HungerQualityPreview>(HungerErrors.Invalid("حدد تاريخًا صحيحًا وملف xlsx لا يتجاوز 10 ميجابايت."));
        OperationEnvelope input;
        try { input = ParseOperations(content); }
        catch (Exception e) when (e is FormatException or ArgumentException or InvalidOperationException or IOException or System.Xml.XmlException)
        { return Result.Failure<HungerQualityPreview>(HungerErrors.Invalid("بنية ملف التشغيل غير صالحة؛ استخدم القالب.")); }
        var plan = await PrepareOperations(input, platforms, reportDate, ct);
        var preview = new HungerImportPreview { Kind = "Operations", UserId = user.UserId!.Value, ReportDate = reportDate, FileName = Path.GetFileName(fileName),
            ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)), InputRevision = plan.Revision, RowsJson = Serialize(input), ExpiresAtUtc = clock.GetUtcNow().AddMinutes(30) };
        db.Add(preview); await db.SaveChangesAsync(ct);
        return Result.Success(new HungerQualityPreview(preview.Id, reportDate, preview.ExpiresAtUtc, plan.Rows.All(r => r.Status != "Rejected"), plan.Rows));
    }
    public async Task<Result<HungerImportResult>> ConfirmOperationsAsync(Guid previewId, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceImport, ct);
        if (platforms.Length == 0) return Result.Failure<HungerImportResult>(HungerErrors.Forbidden);
        var preview = await db.Set<HungerImportPreview>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == previewId && p.UserId == user.UserId && p.Kind == "Operations", ct);
        if (preview is null) return Result.Failure<HungerImportResult>(HungerErrors.NotFound);
        var input = Read<OperationEnvelope>(preview.RowsJson); var codes = input.Inputs.Select(i => i.AccountId).ToArray();
        var ids = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId) && codes.Contains(a.ExternalAccountId)).Select(a => a.Id).ToArrayAsync(ct);
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear(); await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, ids, ct);
                var current = await db.Set<HungerImportPreview>().SingleAsync(p => p.Id == previewId, ct);
                if (ids.Length != codes.Distinct().Count()) return Result.Failure<HungerImportResult>(HungerErrors.Forbidden);
                if (current.ResultJson is not null) return Result.Success(Read<HungerImportResult>(current.ResultJson));
                var plan = await PrepareOperations(input, platforms, preview.ReportDate, ct);
                if (current.ExpiresAtUtc <= clock.GetUtcNow() || current.InputRevision != plan.Revision) return Result.Failure<HungerImportResult>(HungerErrors.Conflict("تغيرت المعاينة؛ أعد الرفع."));
                if (plan.Rows.Any(r => r.Status == "Rejected")) return Result.Failure<HungerImportResult>(HungerErrors.Invalid("أصلح الصفوف المرفوضة؛ لم يعتمد أي جزء من الدفعة."));
                foreach (var change in plan.Changes)
                    db.Add(new HungerDailyOperation { PlatformRiderAccountId = change.AccountId, ImportId = current.Id, ReportDate = current.ReportDate, SourceRowNumber = change.Input.RowNumber,
                        CompletedOrders = change.Input.CompletedOrders, ActualWorkingHours = change.Input.ActualWorkingHours,
                        RejectedOrders = change.Input.RejectedOrders, CancelledOrders = change.Input.CancelledOrders, NoShowCount = change.Input.NoShowCount,
                        AllocationsJson = Serialize(change.Shares), AllocationStatus = change.Shares.Count > 0 ? "Resolved" : "UnresolvedAllocation" });
                var result = new HungerImportResult(current.Id, plan.Rows.Length, plan.Changes.Length, plan.Rows.Count(r => r.Status == "Skipped"), 0, 0, plan.Changes.Count(c => c.Shares.Count == 0), plan.Rows);
                current.ResultJson = Serialize(result); await db.SaveChangesAsync(ct); return Result.Success(result);
            });
        }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure<HungerImportResult>(HungerErrors.Conflict("تعارض أثناء الاعتماد.")); }
    }
    public async Task<Result<HungerOperationReport>> OperationsAsync(DateOnly from, DateOnly through, Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<HungerOperationReport>(HungerErrors.Forbidden);
        if (from == default || through < from || through.DayNumber - from.DayNumber > 366 || page < 1 || pageSize is < 1 or > 100) return Result.Failure<HungerOperationReport>(HungerErrors.Invalid("حدد فترة لا تتجاوز سنة وصفحة صحيحة."));
        var startDate = from;
        var rows = await (from s in db.Set<HungerDailyOperation>().AsNoTracking() join a in db.PlatformRiderAccounts.AsNoTracking() on s.PlatformRiderAccountId equals a.Id
            where platforms.Contains(a.ClientPlatformId) && s.ReportDate >= startDate && s.ReportDate <= through && (accountId == null || a.Id == accountId)
            orderby s.ReportDate, a.ExternalAccountId select new { Source = s, a.ExternalAccountId, a.ClientPlatformId }).ToArrayAsync(ct);
        var rules = await db.Set<HungerAlertRule>().AsNoTracking().Where(r => platforms.Contains(r.ClientPlatformId) && r.EffectiveFrom <= through).ToArrayAsync(ct);
        var items = rows.Select(r =>
        {
            var s = r.Source; var alerts = new List<string>(); var reasons = new List<HungerOperationAlert>();
            bool Alert(string code, decimal actual, decimal defaultThreshold, string defaultSeverity, bool greater = false)
            {
                var rule = rules.Where(rule => rule.ClientPlatformId == r.ClientPlatformId && rule.Code == code && rule.EffectiveFrom <= s.ReportDate)
                    .OrderByDescending(rule => rule.EffectiveFrom).ThenByDescending(rule => rule.CreatedAtUtc).ThenByDescending(rule => rule.Id).FirstOrDefault();
                var threshold = rule?.Threshold ?? defaultThreshold;
                if (rule is { Enabled: false } || !(greater ? actual > threshold : actual < threshold)) return false;
                alerts.Add(code); reasons.Add(new(code, actual, threshold, rule?.Severity ?? defaultSeverity, rule?.Id)); return true;
            }
            if (!Alert("HoursCritical", s.ActualWorkingHours, 8, "Critical")) Alert("HoursFollowUp", s.ActualWorkingHours, 10, "Warning");
            if (s.NoShowCount.HasValue) Alert("NoShowPositive", s.NoShowCount.Value, 0, "Warning", true);
            return new HungerOperationItem(s.Id, s.PlatformRiderAccountId, r.ExternalAccountId, s.ReportDate, s.CompletedOrders, s.ActualWorkingHours,
                s.RejectedOrders, s.CancelledOrders, s.NoShowCount, s.ManualOrders, s.ManualHours, s.ManualOrders.HasValue ? s.CompletedOrders - s.ManualOrders : null,
                s.AllocationStatus, Read<PlatformDayShare[]>(s.AllocationsJson), alerts, Convert.ToBase64String(s.RowVersion)) { AlertDetails = reasons };
        }).Where(i => !riderId.HasValue || i.Allocations.Any(a => a.RiderProfileId == riderId)).ToArray();
        decimal Metric(HungerOperationItem item, PlatformDayMetric metric) => riderId.HasValue ? item.Allocations.Where(a => a.RiderProfileId == riderId).Sum(a => a.Values[metric])
            : metric == PlatformDayMetric.OrdersCount ? item.CompletedOrders : item.ActualWorkingHours;
        long? Total(Func<HungerOperationItem, int?> metric) => !riderId.HasValue && items.Length > 0 && items.All(i => metric(i).HasValue) ? items.Sum(i => (long)metric(i)!.Value) : null;
        return Result.Success(new HungerOperationReport(items.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), items.Length,
            (long)items.Sum(i => Metric(i, PlatformDayMetric.OrdersCount)), items.Sum(i => Metric(i, PlatformDayMetric.WorkingHours)), Total(i => i.RejectedOrders), Total(i => i.CancelledOrders), Total(i => i.NoShowCount), page, pageSize));
    }
    public async Task<Result> ManualPerformanceAsync(Guid sourceId, HungerManualPerformanceRequest request, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceImport, ct);
        var source = await (from s in db.Set<HungerDailyOperation>() join a in db.PlatformRiderAccounts on s.PlatformRiderAccountId equals a.Id where s.Id == sourceId && platforms.Contains(a.ClientPlatformId) select s).SingleOrDefaultAsync(ct);
        if (source is null) return Result.Failure(HungerErrors.Forbidden);
        if (request.Orders is < 0 || request.Hours is < 0 or > 24 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 2000) return Result.Failure(HungerErrors.Invalid("قيم المقارنة أو سبب التعديل غير صالح."));
        if (Convert.ToBase64String(source.RowVersion) != request.RowVersion) return Result.Failure(HungerErrors.Conflict("تغير التقرير."));
        source.ManualOrders = request.Orders; source.ManualHours = request.Hours; source.ManualReason = request.Reason;
        try { await db.SaveChangesAsync(ct); return Result.Success(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure(HungerErrors.Conflict("تغير التقرير.")); }
    }
}
