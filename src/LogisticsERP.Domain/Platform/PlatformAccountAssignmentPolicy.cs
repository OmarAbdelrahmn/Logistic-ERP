using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Platform;

public enum PlatformAccountAssignmentDecision
{
    Allowed = 0,
    AccountLimitReached = 1,
    SalaryAccountLimitReached = 2
}

public static class PlatformAccountAssignmentPolicy
{
    public const int MaximumActiveAccountsPerRider = 2;

    public static PlatformAccountAssignmentDecision Evaluate(
        IEnumerable<PlatformAccountPaymentModel> activePaymentModels,
        PlatformAccountPaymentModel requestedPaymentModel)
    {
        var activeModels = activePaymentModels.ToArray();
        if (requestedPaymentModel == PlatformAccountPaymentModel.Salary
            && activeModels.Contains(PlatformAccountPaymentModel.Salary))
        {
            return PlatformAccountAssignmentDecision.SalaryAccountLimitReached;
        }

        return activeModels.Length >= MaximumActiveAccountsPerRider
            ? PlatformAccountAssignmentDecision.AccountLimitReached
            : PlatformAccountAssignmentDecision.Allowed;
    }
}
