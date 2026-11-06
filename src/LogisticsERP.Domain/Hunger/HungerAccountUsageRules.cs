namespace LogisticsERP.Domain.Hunger;

public static class HungerAccountUsageRules
{
    public static (string Status, string Source) Evaluate(DateOnly today, bool hasObservedWorkingDays, string? sourceStatus,
        string? sourceIdentity, DateOnly? sourceAssignmentFrom, DateOnly? sourceObserved)
    {
        if (hasObservedWorkingDays) return ("Used", "ObservedWorkingHours");
        // Assignments and imported status snapshots do not prove an observed working day.
        return ("Unknown", "None");
    }
}
