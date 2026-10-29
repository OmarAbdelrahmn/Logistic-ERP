using LogisticsERP.Application.Features.Ninja;
using LogisticsERP.Domain.Ninja;

namespace LogisticsERP.Infrastructure.Ninja;

internal sealed partial class NinjaEvaluationService
{
    private static NinjaEvaluationRow BuildRow(string key, Guid? account, Guid? rider, string? name,
        IReadOnlyList<NinjaEvaluationDay> days, DateOnly asOf, DateOnly month, DateOnly today,
        Guid? city, string? cityName, Guid? sponsor, string? establishment, IReadOnlyList<string> initialReasons, bool open)
    {
        var scheduled = days.Where(d => d.Responsibility != 0).OrderBy(d => d.Date).ThenBy(d => d.AccountId).ThenBy(d => d.AssignmentId).ToArray();
        var observed = days.Where(d => d.Date <= asOf && d.Orders.HasValue).ToArray();
        // A missing day may be a break. It must not discard later verified performance.
        var evaluated = scheduled.Where(d => d.Date <= asOf && d.Coverage == "Complete").ToArray();
        var cutoff = evaluated.Select(d => (DateOnly?)d.Date).Max();
        var latest = observed.Select(d => (DateOnly?)d.Date).Max();
        var reasons = new List<string>(initialReasons) { "SuggestionOnly", "OtherContractConditionsUnavailable", "PendingExternalInvoice" };
        if (scheduled.Any(d => d.Responsibility is null)) reasons.Add("TargetWeightUnresolved");
        if (scheduled.Any(d => d.PricingType is null)) reasons.Add("PricingTypeUnresolved");
        var missing = scheduled.Any(d => d.Date <= asOf && d.Coverage != "Complete");
        if (missing) reasons.Add("MissingCoverage");
        if (scheduled.Any(d => d.HoursStatus == "PartialShiftUnknown")) reasons.Add("PartialShiftEligibilityUnknown");
        if (days.Any(d => d.Responsibility == 0 && d.Orders > 0)) reasons.Add("PerformanceWithoutResponsibility");
        var periodTarget = Target(scheduled);
        var evaluatedTotals = Totals(evaluated);
        var attendance = Attendance(scheduled, cutoff);
        var paceAttendance = Attendance(evaluated, cutoff);
        var money = Money(evaluated, d => d.Orders ?? 0, paceAttendance.Status, "AttendanceToCutoffPace", month);
        if (money is not null && paceAttendance.Status == "Fail") reasons.Add("SuggestedReductionFromAttendancePace");
        if (reasons.Contains("PerformanceWithoutResponsibility", StringComparer.Ordinal)) money = null;
        NinjaEvaluationForecast? projection = null;
        var elapsed = evaluated.Sum(d => d.Responsibility ?? 0);
        if (cutoff.HasValue && elapsed > 0 && periodTarget.HasValue && scheduled.All(d => d.Responsibility.HasValue))
        {
            var remainingDays = scheduled.Where(d => d.Date > cutoff).ToArray();
            var remaining = remainingDays.Sum(d => d.Responsibility!.Value);
            var rate = evaluatedTotals.Orders / elapsed;
            var projectedOrders = evaluatedTotals.Orders + rate * remaining;
            var remainingOrders = Math.Max(periodTarget.Value - evaluatedTotals.Orders, 0);
            var fullObserved = evaluated.Where(d => d.Responsibility == 1).ToArray();
            var qualified = fullObserved.Count(d => d.HoursStatus == "Qualified");
            var fullFuture = remainingDays.Where(d => d.Responsibility == 1).Sum(d => d.Responsibility!.Value);
            var futureUnknown = remainingDays.Where(d => d.Responsibility != 1).Sum(d => d.Responsibility!.Value);
            var priorUnknown = scheduled.Where(d => d.Date <= cutoff && (d.Coverage != "Complete" || d.HoursStatus is "Unknown" or "PartialShiftUnknown"))
                .Sum(d => d.Responsibility ?? 0);
            var qualifyingRate = fullObserved.Length == 0 ? (decimal?)null : (decimal)qualified / fullObserved.Length;
            var projectedQualified = qualified + (qualifyingRate ?? 0) * fullFuture;
            var unknown = priorUnknown + futureUnknown + (qualifyingRate is null ? fullFuture : 0);
            var projectedAttendance = NinjaSuggestionCalculator.Attendance(scheduled.Sum(d => d.Responsibility!.Value), projectedQualified, unknown);
            var projectedMoney = Money(scheduled, d => d.Date <= cutoff ? d.Orders ?? 0 : rate * d.Responsibility!.Value,
                projectedAttendance, "ProjectedAttendance", month);
            projection = new(projectedOrders, projectedOrders - periodTarget.Value, rate, remaining, remainingOrders,
                remaining > 0 ? remainingOrders / remaining : null, projectedQualified, projectedAttendance,
                qualified + priorUnknown + remaining >= scheduled.Sum(d => d.Responsibility!.Value) * .85m,
                projectedMoney, !missing, remaining == 0 ? "PeriodEnded" : open ? "ContinueCurrentAssignment" : "ThroughKnownAssignmentEnd");
            if (remaining == 0 && remainingOrders > 0) reasons.Add("NoTimeRemaining");
        }
        return new(key, account, rider, name, days.Select(d => d.AccountId).Distinct().Order().ToArray(),
            days.Select(d => d.CaptainId).Distinct().Order(StringComparer.Ordinal).ToArray(),
            days.Where(d => d.AssignmentId.HasValue).Select(d => d.AssignmentId!.Value).Distinct().Order().ToArray(),
            city, cityName, sponsor, establishment, latest, cutoff, asOf,
            cutoff.HasValue ? Math.Max(today.DayNumber - cutoff.Value.DayNumber, 0) : null,
            Totals(observed), evaluatedTotals, periodTarget, Target(scheduled.Where(d => d.Date <= today)), attendance,
            evaluatedTotals.Target is > 0 ? evaluatedTotals.Orders >= evaluatedTotals.Target ? "TargetAchieved" : "BelowTarget" : "Unknown",
            money, missing ? null : projection, missing ? projection : null, days.OrderBy(d => d.Date).ThenBy(d => d.AssignmentId).ToArray(), reasons);
    }

    private static decimal? Target(IEnumerable<NinjaEvaluationDay> days)
    {
        var rows = days.ToArray();
        if (rows.Length == 0 || rows.Any(d => d.Target is null || d.PricingType is null || d.Responsibility is null)) return null;
        var monthDays = DateTime.DaysInMonth(rows[0].Date.Year, rows[0].Date.Month);
        return rows.GroupBy(d => d.PricingType!).Sum(g => NinjaSuggestionCalculator.Rates(g.Key)!.Target * g.Sum(d => d.Responsibility!.Value) / monthDays);
    }
    private static NinjaEvaluationTotals Totals(IReadOnlyList<NinjaEvaluationDay> days)
    {
        var orders = days.Sum(d => (long)(d.Orders ?? 0)); var hours = days.Sum(d => d.Hours ?? 0);
        var target = Target(days);
        return new(orders, hours, days.Sum(d => d.Responsibility ?? 0), target, target is { } t ? orders - t : null,
            target is > 0 ? orders / target : null, hours > 0 ? orders / hours : null);
    }
    private static NinjaAttendanceEvaluation Attendance(NinjaEvaluationDay[] days, DateOnly? cutoff)
    {
        var scheduled = days.Sum(d => d.Responsibility ?? 0);
        var qualified = days.Where(d => cutoff.HasValue && d.Date <= cutoff && d.Coverage == "Complete" && d.HoursStatus == "Qualified").Sum(d => d.Responsibility ?? 0);
        var unknown = days.Where(d => !cutoff.HasValue || d.Date > cutoff || d.Coverage != "Complete" || d.HoursStatus is "Unknown" or "PartialShiftUnknown")
            .Sum(d => d.Responsibility ?? 0);
        var unresolved = days.Any(d => d.Responsibility is null);
        return new(scheduled, qualified, unknown,
            days.Where(d => cutoff.HasValue && d.Date <= cutoff && d.Coverage == "Complete" && d.HoursStatus == "ShortHours").Select(d => d.Date).Distinct().Count(),
            scheduled * .85m, days.All(d => d.Responsibility == 1) ? (int)Math.Ceiling(scheduled * .85m) : null,
            unresolved ? "Unknown" : NinjaSuggestionCalculator.Attendance(scheduled, qualified, unknown));
    }
    private static NinjaSuggestedMoney? Money(NinjaEvaluationDay[] days, Func<NinjaEvaluationDay, decimal> orders,
        string attendance, string basis, DateOnly month)
    {
        if (days.Length == 0 || days.Any(d => d.Responsibility is null || d.PricingType is null)) return null;
        var segments = new List<NinjaMoneySegment>();
        foreach (var group in days.GroupBy(d => d.PricingType!))
        {
            var weight = group.Sum(d => d.Responsibility!.Value);
            if (weight == 0) continue;
            var calculation = NinjaSuggestionCalculator.Orders(group.Key, DateTime.DaysInMonth(month.Year, month.Month), weight, group.Sum(orders));
            if (calculation is null) return null;
            segments.Add(new(group.Key, weight, calculation));
        }
        if (segments.Count == 0) return null;
        var amount = segments.Sum(s => s.Calculation.Amount);
        return new(NinjaSuggestionCalculator.Money(amount), NinjaSuggestionCalculator.Adjust(amount, attendance), attendance == "Fail", basis, segments);
    }
}
