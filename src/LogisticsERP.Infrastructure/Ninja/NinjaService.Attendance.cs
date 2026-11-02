using System.Globalization;
using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Entities.Ninja;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaService
{
    internal async Task<Dictionary<Guid, NinjaAccountAttendanceResponse>> AccountAttendance(Guid[] accountIds, CancellationToken ct)
    {
        if (accountIds.Length == 0) return [];
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
        var month = new DateOnly(today.Year, today.Month, 1);
        var through = today.AddDays(-1);
        // Account totals, not individual assignment shares, determine account attendance.
        var sources = db.Set<NinjaDailyPerformanceSource>().AsNoTracking().Where(s => accountIds.Contains(s.PlatformRiderAccountId));
        var facts = db.NinjaDailyPerformance.AsNoTracking().Where(s => accountIds.Contains(s.PlatformRiderAccountId));
        var sourceStarts = await sources.Where(s => s.TotalOrders > 0 && s.ReportDate <= today)
            .GroupBy(s => s.PlatformRiderAccountId).Select(g => new { Id = g.Key, Date = g.Min(s => s.ReportDate) }).ToDictionaryAsync(s => s.Id, s => s.Date, ct);
        var factStarts = await facts.Where(s => s.TotalOrders > 0 && s.ReportDate <= today)
            .GroupBy(s => s.PlatformRiderAccountId).Select(g => new { Id = g.Key, Date = g.Min(s => s.ReportDate) }).ToDictionaryAsync(s => s.Id, s => s.Date, ct);
        var contexts = await db.Set<NinjaEvaluationContextRevision>().AsNoTracking()
            .Where(c => accountIds.Contains(c.PlatformRiderAccountId)).GroupBy(c => c.PlatformRiderAccountId)
            .Select(g => g.OrderByDescending(c => c.Revision).First()).ToDictionaryAsync(c => c.PlatformRiderAccountId, ct);
        var dailySources = await sources.Where(s => s.ReportDate >= month && s.ReportDate <= through)
            .Select(s => new { Id = s.PlatformRiderAccountId, s.ReportDate, Hours = s.TotalWorkingHours }).ToArrayAsync(ct);
        var dailyFacts = await facts.Where(s => s.ReportDate >= month && s.ReportDate <= through)
            .GroupBy(s => new { s.PlatformRiderAccountId, s.ReportDate })
            .Select(g => new { Id = g.Key.PlatformRiderAccountId, g.Key.ReportDate, Hours = g.Sum(s => s.TotalWorkingHours) }).ToArrayAsync(ct);
        var hours = dailySources.ToDictionary(s => (s.Id, s.ReportDate), s => s.Hours);
        foreach (var fact in dailyFacts) hours.TryAdd((fact.Id, fact.ReportDate), fact.Hours);
        var result = new Dictionary<Guid, NinjaAccountAttendanceResponse>();
        foreach (var id in accountIds)
        {
            ct.ThrowIfCancellationRequested();
            var starts = new List<DateOnly>();
            if (sourceStarts.TryGetValue(id, out var sourceStart)) starts.Add(sourceStart);
            if (factStarts.TryGetValue(id, out var factStart)) starts.Add(factStart);
            DateOnly? first = contexts.GetValueOrDefault(id)?.ConfirmedFirstDeliveryDate
                ?? (starts.Count == 0 ? null : starts.Min());
            var from = first is { } delivery ? delivery > month ? delivery : month : (DateOnly?)null;
            var count = from.HasValue && from <= through ? through.DayNumber - from.Value.DayNumber + 1 : 0;
            var qualified = 0; var absent = 0; var shortHours = 0;
            var protectedAbsences = new List<DateOnly>();
            for (var i = 0; i < count; i++)
            {
                var date = from!.Value.AddDays(i);
                var worked = hours.GetValueOrDefault((id, date));
                if (worked >= 8) qualified++;
                else if (worked > 0) shortHours++;
                else
                {
                    absent++;
                    if (date.DayOfWeek is DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday)
                        protectedAbsences.Add(date);
                }
            }
            var required = (int)Math.Ceiling(count * .85m);
            var reasons = new List<string>();
            if (count == 0) reasons.Add(first is null ? "FirstDeliveryNotRecorded" : "NoCompletedDays");
            else
            {
                if (qualified < required) reasons.Add("AttendanceBelow85Percent");
                if (protectedAbsences.Count > 0) reasons.Add("LeaveOnProtectedDay");
            }
            result[id] = new(month.ToString("yyyy-MM", CultureInfo.InvariantCulture), first, from,
                count == 0 ? null : through, count, qualified, required, absent, shortHours,
                count == 0 ? null : (decimal)qualified / count, protectedAbsences, reasons,
                count == 0 ? "Pending" : reasons.Count == 0 ? "Valid" : "Invalid");
        }
        return result;
    }
}
