using System.Globalization;
using ClosedXML.Excel;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hunger;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Hunger;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Hunger;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Hunger;

internal sealed partial class HungerQualityService : IHungerReportingService
{
    private static HungerFile ExportBook(XLWorkbook book, string name)
    {
        foreach (var sheet in book.Worksheets) { sheet.RightToLeft = true; sheet.Row(1).Style.Font.Bold = true; sheet.SheetView.FreezeRows(1); sheet.Columns().AdjustToContents(1, 100); }
        using var stream = new MemoryStream(); book.SaveAs(stream);
        return new(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
    public Task<Result<HungerFile>> ExportQualityAsync(DateOnly month, Guid? accountId, CancellationToken ct = default)
        => ExportQualityAsync(month, accountId, new HungerReportExportFilter(), ct);
    public async Task<Result<HungerFile>> ExportQualityAsync(DateOnly month, Guid? accountId, HungerReportExportFilter filter, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.QualityExport, ct);
        if (platforms.Length == 0 || !await FieldAccess(platforms, [PermissionKeys.Hunger.Read], ct)) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        if (month == default || month.Year >= 9999) return Result.Failure<HungerFile>(HungerErrors.Invalid("حدد شهرًا صحيحًا."));
        var start = new DateOnly(month.Year, month.Month, 1); var end = start.AddMonths(1);
        var allowed = await ExportAccountIds(filter.Accounts, PermissionKeys.Hunger.QualityExport, ct);
        if (allowed.IsFailure) return Result.Failure<HungerFile>(allowed.Error);
        var selectedIds = allowed.Value ?? []; var filterAccounts = allowed.Value is not null;
        var source = await (from s in db.Set<HungerQualitySnapshot>().AsNoTracking() join a in db.PlatformRiderAccounts.AsNoTracking() on s.PlatformRiderAccountId equals a.Id
            where platforms.Contains(a.ClientPlatformId) && (!filterAccounts || selectedIds.Contains(a.Id)) && (accountId == null || a.Id == accountId) && s.ReportDate >= start && s.ReportDate < end
            orderby s.ReportDate, a.ExternalAccountId select new { Snapshot = s, a.ExternalAccountId }).ToArrayAsync(ct);
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Quality");
        string[] headers = ["معرف هنقر", "تاريخ التقرير", "بداية التغطية", "التصنيف", "الترتيب", "نسبة التحقق", "درجة الوقت", "درجة الجودة", "مكتملة تراكمية", "فرق المكتملة", "إجمالي الطلبات", "في الوقت", "طلبات التحقق", "تحقق ناجح", "فشل المندوب", "حالة التخصيص", "مرجع الاستيراد", "صف المصدر"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var n = 2;
        foreach (var row in source)
        {
            if (filter.RiderId.HasValue && !Read<LogisticsERP.Application.Features.Hr.PlatformDayShare[]>(row.Snapshot.AllocationsJson).Any(a => a.RiderProfileId == filter.RiderId)) continue;
            ct.ThrowIfCancellationRequested(); var s = row.Snapshot; var cumulative = Read<HungerQualityCounters>(s.CountersJson); var daily = Read<HungerQualityCounters>(s.DailyCountersJson);
            sheet.Cell(n, 1).Value = row.ExternalAccountId; sheet.Cell(n, 2).Value = s.ReportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sheet.Cell(n, 3).Value = s.CoverageFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); sheet.Cell(n, 4).Value = s.Segment;
            if (HungerQualityRules.Rank(s.Segment) is { } rank) sheet.Cell(n, 5).Value = rank;
            sheet.Cell(n, 6).Value = s.VerificationSuccessRate; sheet.Cell(n, 7).Value = s.OnTimeDeliveryScore; sheet.Cell(n, 8).Value = s.FinalDeliveryQualityScore;
            sheet.Cell(n, 9).Value = cumulative.CompletedOrders; sheet.Cell(n, 10).Value = daily.CompletedOrders; sheet.Cell(n, 11).Value = daily.GrossOrders;
            sheet.Cell(n, 12).Value = daily.CompletedOrdersInTime; sheet.Cell(n, 13).Value = daily.TotalVerificationRequests;
            sheet.Cell(n, 14).Value = daily.SuccessfulVerificationRequests; sheet.Cell(n, 15).Value = daily.FailedOrdersByRider;
            sheet.Cell(n, 16).Value = s.AllocationStatus; sheet.Cell(n, 17).Value = s.ImportId.ToString("D"); sheet.Cell(n, 18).Value = s.SourceRowNumber; n++;
        }
        return Result.Success(ExportBook(book, "Hunger-Quality.xlsx"));
    }
    public Task<Result<HungerFile>> ExportOperationsAsync(DateOnly from, DateOnly through, Guid? accountId, CancellationToken ct = default)
        => ExportOperationsAsync(from, through, accountId, new HungerReportExportFilter(), ct);
    public async Task<Result<HungerFile>> ExportOperationsAsync(DateOnly from, DateOnly through, Guid? accountId, HungerReportExportFilter filter, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.PerformanceExport, ct);
        if (platforms.Length == 0 || !await FieldAccess(platforms, [PermissionKeys.Hunger.Read], ct)) return Result.Failure<HungerFile>(HungerErrors.Forbidden);
        if (from == default || through < from || through.DayNumber - from.DayNumber > 366) return Result.Failure<HungerFile>(HungerErrors.Invalid("حدد فترة لا تتجاوز سنة."));
        var start = from;
        var allowed = await ExportAccountIds(filter.Accounts, PermissionKeys.Hunger.PerformanceExport, ct);
        if (allowed.IsFailure) return Result.Failure<HungerFile>(allowed.Error);
        var selectedIds = allowed.Value ?? []; var filterAccounts = allowed.Value is not null;
        var source = await (from s in db.Set<HungerDailyOperation>().AsNoTracking() join a in db.PlatformRiderAccounts.AsNoTracking() on s.PlatformRiderAccountId equals a.Id
            where platforms.Contains(a.ClientPlatformId) && (!filterAccounts || selectedIds.Contains(a.Id)) && (accountId == null || a.Id == accountId) && s.ReportDate >= start && s.ReportDate <= through
            orderby s.ReportDate, a.ExternalAccountId select new { Source = s, a.ExternalAccountId }).ToArrayAsync(ct);
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Operations");
        string[] headers = ["معرف هنقر", "التاريخ", "الطلبات المكتملة", "ساعات العمل", "الرفض", "الإلغاءات", "NoShow", "طلبات يدوية", "فرق الطلبات", "ساعات يدوية", "حالة التخصيص", "مرجع الاستيراد", "AcceptedOrders", "StackedOrders", "HoursUnit", "CaptainClassification"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var n = 2;
        foreach (var row in source)
        {
            if (filter.RiderId.HasValue && !Read<LogisticsERP.Application.Features.Hr.PlatformDayShare[]>(row.Source.AllocationsJson).Any(a => a.RiderProfileId == filter.RiderId)
                || !string.IsNullOrWhiteSpace(filter.CaptainClassification) && !string.Equals(row.Source.CaptainClassification, filter.CaptainClassification, StringComparison.OrdinalIgnoreCase)) continue;
            ct.ThrowIfCancellationRequested(); var s = row.Source;
            sheet.Cell(n, 1).Value = row.ExternalAccountId; sheet.Cell(n, 2).Value = s.ReportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sheet.Cell(n, 3).Value = s.CompletedOrders; if (s.ActualWorkingHours.HasValue) sheet.Cell(n, 4).Value = s.ActualWorkingHours.Value;
            if (s.RejectedOrders is { } rejected) sheet.Cell(n, 5).Value = rejected;
            if (s.CancelledOrders is { } cancelled) sheet.Cell(n, 6).Value = cancelled;
            if (s.NoShowCount is { } noShow) sheet.Cell(n, 7).Value = noShow;
            if (s.ManualOrders is { } manual) { sheet.Cell(n, 8).Value = manual; sheet.Cell(n, 9).Value = s.CompletedOrders - manual; }
            if (s.ManualHours is { } hours) sheet.Cell(n, 10).Value = hours;
            sheet.Cell(n, 11).Value = s.AllocationStatus; sheet.Cell(n, 12).Value = s.ImportId.ToString("D");
            if (s.AcceptedOrders.HasValue) sheet.Cell(n, 13).Value = s.AcceptedOrders.Value;
            if (s.StackedOrders.HasValue) sheet.Cell(n, 14).Value = s.StackedOrders.Value;
            sheet.Cell(n, 15).Value = s.HoursUnit; sheet.Cell(n, 16).Value = s.CaptainClassification; n++;
        }
        return Result.Success(ExportBook(book, "Hunger-Operations.xlsx"));
    }
    private async Task<Result<Guid[]?>> ExportAccountIds(HungerAccountFilter? filter, string permission, CancellationToken ct)
    {
        if (filter is null) return Result.Success<Guid[]?>(null);
        var result = await QueryAccounts(filter, 1, 100, true, permission, ct);
        return result.IsFailure ? Result.Failure<Guid[]?>(result.Error) : Result.Success<Guid[]?>(result.Value!.Items.Select(a => a.AccountId).ToArray());
    }
    public async Task<Result<HungerSummary>> SummaryAsync(DateOnly month, CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<HungerSummary>(HungerErrors.Forbidden);
        if (month == default || month.Year >= 9999) return Result.Failure<HungerSummary>(HungerErrors.Invalid("حدد الشهر."));
        var start = new DateOnly(month.Year, month.Month, 1); var end = start.AddMonths(1);
        var accounts = await db.PlatformRiderAccounts.AsNoTracking().Where(a => platforms.Contains(a.ClientPlatformId)).ToArrayAsync(ct);
        var ids = accounts.Select(a => a.Id).ToArray();
        var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().Where(d => ids.Contains(d.PlatformRiderAccountId)).ToArrayAsync(ct);
        var actuals = await db.RiderClientAssignments.AsNoTracking().Where(a => ids.Contains(a.PlatformRiderAccountId) && a.Status != RiderAssignmentStatus.Cancelled && a.Status != RiderAssignmentStatus.Planned
            && a.EffectiveFrom < end && (a.EffectiveTo == null || a.EffectiveTo >= start)).ToArrayAsync(ct);
        var active = actuals.Where(a => a.Status == RiderAssignmentStatus.Active && a.EffectiveTo == null && a.EffectiveFrom <= BusinessToday).Select(a => a.PlatformRiderAccountId).ToHashSet();
        string State(Guid id) => details.SingleOrDefault(d => d.PlatformRiderAccountId == id)?.OperationalDisposition ?? "Unknown";
        return Result.Success(new HungerSummary(accounts.Length, accounts.Count(a => State(a.Id) == "Normal" && active.Contains(a.Id)),
            accounts.Count(a => State(a.Id) == "Normal" && !active.Contains(a.Id) && a.Status == PlatformRiderAccountStatus.Available),
            accounts.GroupBy(a => State(a.Id)).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal), actuals.Select(a => a.RiderProfileId).Distinct().Count(),
            accounts.GroupBy(a => details.SingleOrDefault(d => d.PlatformRiderAccountId == a.Id)?.RegisteredVehicleType ?? "Unknown").ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal)));
    }
    public async Task<Result<IReadOnlyList<HungerAlertRuleItem>>> AlertRulesAsync(CancellationToken ct = default)
    {
        var platforms = await Platforms(PermissionKeys.Hunger.Read, ct);
        if (platforms.Length == 0) return Result.Failure<IReadOnlyList<HungerAlertRuleItem>>(HungerErrors.Forbidden);
        var source = await db.Set<HungerAlertRule>().AsNoTracking().Where(r => platforms.Contains(r.ClientPlatformId)).OrderBy(r => r.EffectiveFrom).ThenBy(r => r.CreatedAtUtc).ThenBy(r => r.Id).ToArrayAsync(ct);
        return Result.Success<IReadOnlyList<HungerAlertRuleItem>>(source.Select(r => new HungerAlertRuleItem(r.Id, r.ClientPlatformId, r.Code, r.Threshold, r.Severity, r.EffectiveFrom, r.Enabled, r.EvidenceReference) { Metric = r.Metric, Operator = r.Operator, Kind = r.Kind, Period = r.Period, OperatingCityId = r.OperatingCityId, EffectiveThrough = r.EffectiveThrough }).ToArray());
    }
    public async Task<Result<Guid>> AddAlertRuleAsync(HungerAlertRuleRequest request, CancellationToken ct = default)
    {
        if (!(await Platforms(PermissionKeys.Hunger.AlertsManage, ct)).Contains(request.PlatformId)) return Result.Failure<Guid>(HungerErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 50
            || request.Metric is not ("WorkingHours" or "CompletedOrders" or "WorkedDays" or "QualifiedDays" or "NoShowCount" or "RejectedOrders" or "CancelledOrders" or "AcceptedOrders" or "StackedOrders")
            || request.Operator is not ("LessThan" or "LessThanOrEqual" or "GreaterThan" or "GreaterThanOrEqual" or "Equal")
            || request.Kind is not ("Alert" or "Target") || request.Period is not ("Day" or "Month")
            || request.EffectiveThrough < request.EffectiveFrom || request.Threshold < 0 || request.Threshold > 1000000
            || request.Severity is not ("Critical" or "High" or "Warning" or "Info") || request.EffectiveFrom == default || string.IsNullOrWhiteSpace(request.EvidenceReference) || request.EvidenceReference.Length > 1000)
            return Result.Failure<Guid>(HungerErrors.Invalid("حدد قاعدة معروفة وحدًا ودرجة وسريانًا ودليلًا صالحًا."));
        if (request.OperatingCityId.HasValue && !await db.OperatingCities.AnyAsync(c => c.Id == request.OperatingCityId, ct))
            return Result.Failure<Guid>(HungerErrors.Invalid("Unknown city."));
        var rule = new HungerAlertRule { ClientPlatformId = request.PlatformId, Code = request.Code, Threshold = request.Threshold, Severity = request.Severity,
            Metric = request.Code == "NoShowPositive" ? "NoShowCount" : request.Metric, Operator = request.Code == "NoShowPositive" ? "GreaterThan" : request.Operator, Kind = request.Kind, Period = request.Period, OperatingCityId = request.OperatingCityId, EffectiveThrough = request.EffectiveThrough,
            EffectiveFrom = request.EffectiveFrom, Enabled = request.Enabled, EvidenceReference = request.EvidenceReference, CreatedAtUtc = clock.GetUtcNow() };
        db.Add(rule); await db.SaveChangesAsync(ct); return Result.Success(rule.Id);
    }
}
