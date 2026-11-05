namespace LogisticsERP.Domain.Hunger;

public static class HungerAccountUsageRules
{
    public static (string Status, string Source) Evaluate(DateOnly today, bool assignmentUsed, string? sourceStatus,
        string? sourceIdentity, DateOnly? sourceAssignmentFrom, DateOnly? sourceObserved)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        if (assignmentUsed) return ("Used", "Assignment");
        if (sourceStatus == "يعمل" && !string.IsNullOrWhiteSpace(sourceIdentity) && sourceAssignmentFrom.HasValue
            && sourceAssignmentFrom.Value <= today && sourceObserved >= monthStart && sourceObserved <= today)
            return ("Used", "ImportedSource");
        // A status snapshot does not prove an account had no earlier activity this month.
        return ("Unknown", "None");
    }
}
