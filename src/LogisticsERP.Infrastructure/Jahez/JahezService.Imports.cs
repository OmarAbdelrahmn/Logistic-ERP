using System.Text;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Jahez;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezService
{
    public Task<Result<JahezImportPreview>> UploadAsync(string key, JahezImportCreateRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "upload", new { request.Kind, Files = request.Files.Select(f => new { f.FileName, Hash = Hash(f.Content) }), request.ReplacesBatchId, request.CorrectionReason },
            PermissionKeys.Jahez.ImportsCreate, async () =>
        {
            Require(Enum.IsDefined(request.Kind) && request.Files.Count is >= 1 and <= 10, JahezErrors.Invalid("حدد نوع الاستيراد وملفًا إلى عشرة ملفات."));
            Require(request.Files.All(f => f.Content.Length is > 0 and <= 10 * 1024 * 1024 && f.FileName.Length is > 0 and <= 260
                && Path.GetExtension(f.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) && request.Files.Sum(f => (long)f.Content.Length) <= 30 * 1024 * 1024,
                JahezErrors.Invalid("الملفات يجب أن تكون xlsx، بحد 10 ميجابايت للملف و30 ميجابايت للعملية."));
            var hashes = request.Files.Select(f => Hash(f.Content)).ToArray();
            Require(hashes.Distinct(StringComparer.Ordinal).Count() == hashes.Length, JahezErrors.Invalid("الملف نفسه مكرر في الطلب."));
            var combined = Hash(Encoding.UTF8.GetBytes(string.Join(':', hashes.Order(StringComparer.Ordinal))));
            var existing = await db.Set<JahezImportBatch>().SingleOrDefaultAsync(x => x.Kind == request.Kind && x.ContentHash == combined, ct);
            if (existing is not null)
            {
                Require(existing.ReplacesBatchId == request.ReplacesBatchId, JahezErrors.Conflict("الملفات موجودة ضمن عملية مختلفة."));
                return await ImportPreview(existing, null, ct);
            }
            if (request.ReplacesBatchId.HasValue)
            {
                Reason(request.CorrectionReason ?? string.Empty);
                var old = await db.Set<JahezImportBatch>().SingleOrDefaultAsync(x => x.Id == request.ReplacesBatchId, ct);
                Require(old is not null && old.CommittedAtUtc.HasValue && old.Kind == request.Kind, JahezErrors.Conflict("عملية الاستبدال الأصلية غير صالحة."));
            }
            var batch = new JahezImportBatch { Kind = request.Kind, ContentHash = combined, UploadedByUserId = Actor,
                ReplacesBatchId = request.ReplacesBatchId, CorrectionReason = request.CorrectionReason };
            List<JahezImportFile> files = [];
            List<JahezImportRow> rows = [];
            foreach (var f in request.Files)
            {
                ct.ThrowIfCancellationRequested();
                var file = new JahezImportFile { BatchId = batch.Id, FileName = Path.GetFileName(f.FileName), ContentHash = Hash(f.Content), Content = f.Content };
                try { rows.AddRange(JahezSpreadsheetParser.Parse(f.Content, file.Id, request.Kind)); }
                catch (Exception e) when (e is InvalidDataException or FormatException or IOException or ArgumentException)
                { throw new JahezBusinessException(JahezErrors.Invalid($"تعذر قراءة {file.FileName}: {e.Message}")); }
                files.Add(file);
            }
            Require(rows.Count <= 50000, JahezErrors.Invalid("أقصى عدد صفوف للعملية 50000."));
            db.Add(batch); db.AddRange(files); db.AddRange(rows);
            return await ResolveImport(batch, files.ToArray(), rows.ToArray(), null, ct);
        }, ct);

    public Task<Result<JahezImportPreview>> PreviewImportAsync(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.ImportsRead, async () =>
        {
            var batch = await db.Set<JahezImportBatch>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(batch is not null, JahezErrors.NotFound);
            return await ImportPreview(batch!, null, ct);
        }, ct);

    private async Task<JahezImportPreview> ImportPreview(JahezImportBatch batch, IReadOnlyList<JahezDispatchAllocation>? allocations, CancellationToken ct)
    {
        var files = await db.Set<JahezImportFile>().Where(x => x.BatchId == batch.Id).Select(x => new JahezImportFile
            { Id = x.Id, BatchId = x.BatchId, FileName = x.FileName }).ToArrayAsync(ct);
        var ids = files.Select(x => x.Id).ToArray();
        var rows = await db.Set<JahezImportRow>().AsNoTracking().Where(x => ids.Contains(x.FileId)).OrderBy(x => x.FileId).ThenBy(x => x.RowNumber).ToArrayAsync(ct);
        return await ResolveImport(batch, files, rows, allocations, ct);
    }

    private async Task<JahezImportPreview> ResolveImport(JahezImportBatch batch, JahezImportFile[] files, JahezImportRow[] rows,
        IReadOnlyList<JahezDispatchAllocation>? allocations, CancellationToken ct)
    {
        Require(allocations is null || allocations.Count <= 100000, JahezErrors.Invalid("عدد توزيعات الطلبات يتجاوز الحد المسموح."));
        var byRow = allocations?.GroupBy(x => x.RowId).ToDictionary(g => g.Key, g => g.ToArray()) ?? [];
        var knownRows = rows.Select(x => x.Id).ToHashSet();
        var drivers = rows.Select(x => x.DriverId).Distinct().ToArray();
        var accounts = await (from a in db.PlatformRiderAccounts.IgnoreQueryFilters() join p in db.ClientPlatforms on a.ClientPlatformId equals p.Id
            where p.Code == "JAHEZ" && drivers.Contains(a.ExternalAccountId) select a).AsNoTracking().ToArrayAsync(ct);
        var accountIds = accounts.Select(x => x.Id).ToArray();
        var handovers = await db.Set<JahezAccountHandover>().AsNoTracking().Where(x => accountIds.Contains(x.PlatformRiderAccountId)).ToArrayAsync(ct);
        var names = files.ToDictionary(x => x.Id, x => x.FileName);
        List<JahezImportIssue> issues = [];
        List<JahezImportRowPreview> previews = [];
        var duplicatedDaily = rows.GroupBy(x => new { x.DriverId, Date = JahezRules.RiyadhDate(x.OccurredAtUtc) }).Where(g => g.Count() > 1).SelectMany(g => g).Select(x => x.Id).ToHashSet();
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var account = accounts.SingleOrDefault(x => x.ExternalAccountId == row.DriverId);
            JahezAccountHandover? handover = null;
            void Issue(string code, string message) => issues.Add(new(row.Id, names[row.FileId], row.RowNumber, code, message));
            if (row.ParseError is not null) Issue("parse_error", row.ParseError);
            else if (account is null) Issue("unknown_account", "رقم الحساب غير معروف ضمن جاهز.");
            else if (row.OccurredAtUtc > Now) Issue("future_date", "تاريخ السجل في المستقبل.");
            else if (batch.Kind == JahezImportKind.Transactions)
            {
                var matches = handovers.Where(h => h.PlatformRiderAccountId == account.Id && JahezRules.Contains(h, row.OccurredAtUtc)).ToArray();
                if (matches.Length != 1) Issue("assignment_missing", "لم يُحدد مستخدم واحد للحساب وقت الحركة؛ استكمل سجل الاستلام.");
                else handover = matches[0];
            }
            else
            {
                var date = JahezRules.RiyadhDate(row.OccurredAtUtc);
                if (row.ToDate != date) Issue("multi_day_report", "استورد تقرير يوم واحد؛ لا يمكن توزيع إجمالي عدة أيام تلقائيًا.");
                if (duplicatedDaily.Contains(row.Id)) Issue("duplicate_daily_row", "أكثر من إجمالي طلبات للحساب واليوم نفسيهما في الملفات.");
                var dayStart = JahezRules.StartOfDay(date).ToUniversalTime();
                var dayEnd = dayStart.AddDays(1);
                var matches = handovers.Where(h => h.PlatformRiderAccountId == account.Id && h.StartedAtUtc < dayEnd && (h.EndedAtUtc == null || h.EndedAtUtc > dayStart)).ToArray();
                var manual = byRow.GetValueOrDefault(row.Id) ?? [];
                if (manual.Length > 0)
                {
                    if (manual.Any(a => a.Count < 0 || string.IsNullOrWhiteSpace(a.Reason) || a.Reason.Length > 2000 || !matches.Any(h => h.Id == a.HandoverId))
                        || manual.Select(x => x.HandoverId).Distinct().Count() != manual.Length || manual.Sum(x => (long)x.Count) != row.Dispatches)
                        Issue("invalid_allocation", "التوزيع يجب أن يغطي إجمالي الطلبات على مستخدمي هذا اليوم مع الأسباب.");
                }
                else if (matches.Length != 1) Issue("assignment_ambiguous", "يلزم توزيع يدوي للطلبات على مستخدمي الحساب خلال اليوم.");
                else handover = matches[0];
            }
            previews.Add(new(row.Id, names[row.FileId], row.RowNumber, row.DriverId, row.OccurredAtUtc, account?.Id, handover?.Id, handover?.RiderProfileId, row.NetAmount, row.Dispatches));
        }
        if (allocations?.Any(a => !knownRows.Contains(a.RowId)) == true)
            issues.Add(new(Guid.Empty, string.Empty, 0, "unknown_allocation_row", "التوزيع يشير إلى صف غير موجود في العملية."));
        if (!batch.CommittedAtUtc.HasValue)
        {
            var otherBatches = db.Set<JahezImportBatch>().Where(b => b.CommittedAtUtc != null && b.Id != batch.Id && b.Id != batch.ReplacesBatchId
                && !db.Set<JahezImportBatch>().Any(n => n.ReplacesBatchId == b.Id && n.CommittedAtUtc != null));
            var otherIds = await otherBatches.Select(x => x.Id).ToArrayAsync(ct);
            foreach (var group in previews.Where(x => x.AccountId.HasValue).GroupBy(x => x.AccountId!.Value))
            {
                var ids = handovers.Where(x => x.PlatformRiderAccountId == group.Key).Select(x => x.Id).ToArray();
                var from = group.Min(x => x.OccurredAtUtc); var to = group.Max(x => x.OccurredAtUtc);
                bool overlap;
                if (batch.Kind == JahezImportKind.Transactions)
                {
                    var ranges = await db.Set<JahezTransaction>().Where(x => otherIds.Contains(x.BatchId) && ids.Contains(x.HandoverId))
                        .GroupBy(x => x.BatchId).Select(g => new { From = g.Min(x => x.OccurredAtUtc), To = g.Max(x => x.OccurredAtUtc) }).ToArrayAsync(ct);
                    overlap = ranges.Any(x => x.From <= to && x.To >= from);
                }
                else
                {
                    var dates = group.Select(x => JahezRules.RiyadhDate(x.OccurredAtUtc)).Distinct().ToArray();
                    overlap = await db.Set<JahezDailyDispatch>().AnyAsync(x => otherIds.Contains(x.BatchId) && ids.Contains(x.HandoverId) && dates.Contains(x.Date), ct);
                }
                if (overlap)
                {
                    var first = group.First();
                    issues.Add(new(first.RowId, first.FileName, first.RowNumber, "overlapping_period", "الفترة تتداخل مع تقرير مرحل؛ استخدم استبدالًا موثقًا للعملية الأصلية."));
                }
            }
        }
        var accountMap = accounts.ToDictionary(x => x.ExternalAccountId, x => x.Id, StringComparer.Ordinal);
        var problemRows = issues.Select(x => x.RowId).ToHashSet();
        var problemDrivers = rows.Where(x => problemRows.Contains(x.Id)).Select(x => x.DriverId).ToHashSet(StringComparer.Ordinal);
        var summaries = rows.Where(x => x.ParseError is null).GroupBy(x => x.DriverId).Select(g =>
        {
            var net = g.Sum(x => x.NetAmount);
            return new JahezImportAccountSummary(accountMap.TryGetValue(g.Key, out var accountId) ? accountId : null, g.Key,
                g.Min(x => JahezRules.RiyadhDate(x.OccurredAtUtc)),
                g.Max(x => x.ToDate ?? JahezRules.RiyadhDate(x.OccurredAtUtc)), g.Count(), net,
                batch.Kind == JahezImportKind.Transactions ? -net : 0m,
                batch.Kind == JahezImportKind.DailyDispatches ? g.Sum(x => (long)x.Dispatches!.Value) : null,
                problemRows.Contains(Guid.Empty) || problemDrivers.Contains(g.Key));
        }).ToArray();
        return new(batch.Id, batch.Kind, batch.CommittedAtUtc.HasValue, previews, issues,
            files.Select(x => new JahezImportFileMetadata(x.Id, x.FileName)).ToArray(), summaries);
    }

    public Task<Result<JahezImportPreview>> CommitImportAsync(string key, Guid id, JahezImportCommitRequest request, CancellationToken ct = default) =>
        ExecuteAsync(key, "commit-import", new { id, request }, PermissionKeys.Jahez.ImportsUpdate, async () =>
        {
            var batch = await db.Set<JahezImportBatch>().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(batch is not null, JahezErrors.NotFound);
            if (batch!.CommittedAtUtc.HasValue) return await ImportPreview(batch, request.Allocations, ct);
            var preview = await ImportPreview(batch, request.Allocations, ct);
            Require(preview.Issues.Count == 0, JahezErrors.Conflict("حل جميع مشاكل المعاينة قبل الترحيل."));
            if (batch.ReplacesBatchId.HasValue)
            {
                Require(!await db.Set<JahezImportBatch>().AnyAsync(x => x.ReplacesBatchId == batch.ReplacesBatchId && x.CommittedAtUtc != null, ct), JahezErrors.Conflict("العملية الأصلية مستبدلة بالفعل."));
                var oldBatch = await db.Set<JahezImportBatch>().SingleAsync(x => x.Id == batch.ReplacesBatchId, ct);
                var oldPreview = await ImportPreview(oldBatch, null, ct);
                var originalScope = oldPreview.Rows.GroupBy(x => x.DriverId).Select(g => (g.Key, From: JahezRules.RiyadhDate(g.Min(x => x.OccurredAtUtc)), To: JahezRules.RiyadhDate(g.Max(x => x.OccurredAtUtc)))).OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
                var newScope = preview.Rows.GroupBy(x => x.DriverId).Select(g => (g.Key, From: JahezRules.RiyadhDate(g.Min(x => x.OccurredAtUtc)), To: JahezRules.RiyadhDate(g.Max(x => x.OccurredAtUtc)))).OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
                Require(originalScope.SequenceEqual(newScope), JahezErrors.Conflict("الاستبدال يجب أن يغطي الحسابات وحدود الأيام نفسها؛ لا يستبدل جزءًا من العملية الأصلية."));
                var oldIds = await db.Set<JahezTransaction>().Where(x => x.BatchId == oldBatch.Id).Select(x => x.Id).ToArrayAsync(ct);
                var entries = await db.Set<JahezLedgerEntry>().Where(x => oldIds.Contains(x.SourceId) && x.Kind == JahezLedgerKind.Charge).ToArrayAsync(ct);
                foreach (var entry in entries)
                {
                    var h = await Handover(entry.HandoverId, ct);
                    Entry(h, entry.Bucket, JahezLedgerKind.Adjustment, -entry.Amount, entry.Id, batch.CorrectionReason!, entry.OccurredAtUtc, entry.Id);
                }
            }
            var rowIds = preview.Rows.Select(x => x.RowId).ToArray();
            var sourceRows = await db.Set<JahezImportRow>().Where(x => rowIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var hIds = preview.Rows.Where(x => x.HandoverId.HasValue).Select(x => x.HandoverId!.Value)
                .Concat(request.Allocations?.Select(x => x.HandoverId) ?? []).Distinct().ToArray();
            var handovers = await db.Set<JahezAccountHandover>().Where(x => hIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            var allocationRows = request.Allocations?.GroupBy(x => x.RowId).ToDictionary(g => g.Key, g => g.ToArray()) ?? [];
            foreach (var row in preview.Rows)
            {
                var source = sourceRows[row.RowId];
                if (batch.Kind == JahezImportKind.Transactions)
                {
                    var transaction = new JahezTransaction { BatchId = batch.Id, ImportRowId = row.RowId, HandoverId = row.HandoverId!.Value,
                        OccurredAtUtc = source.OccurredAtUtc, DeliveryPrice = source.DeliveryPrice, CashAmount = source.CashAmount,
                        NetAmount = source.NetAmount, DriverAdjustment = source.DriverAdjustment };
                    db.Add(transaction);
                    Entry(handovers[transaction.HandoverId], JahezLedgerBucket.PlatformDebt, JahezLedgerKind.Charge, -source.NetAmount,
                        transaction.Id, "حركة جاهز مستوردة", source.OccurredAtUtc);
                }
                else
                {
                    var manual = allocationRows.GetValueOrDefault(row.RowId) ?? [];
                    if (manual.Length == 0) db.Add(new JahezDailyDispatch { BatchId = batch.Id, ImportRowId = row.RowId,
                        HandoverId = row.HandoverId!.Value, Date = JahezRules.RiyadhDate(row.OccurredAtUtc), Count = source.Dispatches!.Value });
                    else foreach (var allocation in manual) db.Add(new JahezDailyDispatch { BatchId = batch.Id, ImportRowId = row.RowId,
                        HandoverId = allocation.HandoverId, Date = JahezRules.RiyadhDate(row.OccurredAtUtc), Count = allocation.Count, AllocationReason = allocation.Reason });
                }
            }
            batch.CommittedAtUtc = Now;
            return preview with { Committed = true };
        }, ct);

    public Task<Result<JahezImportFileResponse>> GetImportFileAsync(Guid id, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.ImportsRead, async () =>
        {
            var f = await db.Set<JahezImportFile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            Require(f is not null, JahezErrors.NotFound);
            return new JahezImportFileResponse(f!.Id, f.FileName, f.Content);
        }, ct);

    public Task<Result<JahezPage<JahezDispatchReportRow>>> GetDispatchesAsync(DateOnly fromDate, DateOnly toDate, Guid? riderId, int page, int pageSize, CancellationToken ct = default) =>
        ReadAsync(PermissionKeys.Jahez.Read, async () =>
        {
            Page(page, pageSize);
            Require(fromDate <= toDate && toDate <= Today, JahezErrors.Invalid("فترة التقرير غير صالحة."));
            var rows = await (from d in db.Set<JahezDailyDispatch>().AsNoTracking()
                join h in db.Set<JahezAccountHandover>() on d.HandoverId equals h.Id
                join a in db.PlatformRiderAccounts.IgnoreQueryFilters() on h.PlatformRiderAccountId equals a.Id
                where d.Date >= fromDate && d.Date <= toDate && (!riderId.HasValue || h.RiderProfileId == riderId)
                    && !db.Set<JahezImportBatch>().Any(b => b.ReplacesBatchId == d.BatchId && b.CommittedAtUtc != null)
                orderby d.Date descending, d.Id
                select new JahezDispatchReportRow(a.Id, a.ExternalAccountId, h.RiderProfileId, h.Id, d.Date, d.Count))
                .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
            return new JahezPage<JahezDispatchReportRow>(rows, page, pageSize);
        }, ct);
}
