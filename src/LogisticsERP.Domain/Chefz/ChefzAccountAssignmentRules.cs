using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Jahez;
using LogisticsERP.Domain.Platform;

namespace LogisticsERP.Domain.Chefz;

public static class ChefzAccountAssignmentRules
{
    public const int MaximumActiveAccountsPerRider = 2;
    public const int MaximumCombinedActiveAccountsPerRider = 3;

    public static bool IsChefzPlatform(string? code) => code is "CHEFZ" or "THECHEFZ" or "CHEFSZ" or "SHIFTZ";

    public static int MaximumActiveAccounts(bool requestedIsChefz, IEnumerable<string?> activePlatformCodes) =>
        MaximumActiveAccounts(requestedIsChefz ? "CHEFZ" : null, activePlatformCodes);

    public static int MaximumActiveAccounts(string? requestedPlatformCode, IEnumerable<string?> activePlatformCodes)
    {
        var codes = activePlatformCodes.ToArray();
        if (codes.Any(code => !IsChefzPlatform(code) && code != "JAHEZ"))
            return PlatformAccountAssignmentPolicy.MaximumActiveAccountsPerRider;
        if (IsChefzPlatform(requestedPlatformCode))
            return codes.Contains("JAHEZ") ? MaximumCombinedActiveAccountsPerRider : MaximumActiveAccountsPerRider;
        if (requestedPlatformCode == "JAHEZ")
            return codes.Any(IsChefzPlatform) ? MaximumCombinedActiveAccountsPerRider : JahezRules.MaximumActiveAccountsPerRider;
        return PlatformAccountAssignmentPolicy.MaximumActiveAccountsPerRider;
    }

    public static PlatformAccountAssignmentDecision Evaluate(bool requestedIsChefz,
        IEnumerable<(string? PlatformCode, PlatformAccountPaymentModel PaymentModel)> activeAccounts,
        PlatformAccountPaymentModel requestedPaymentModel) =>
        Evaluate(requestedIsChefz ? "CHEFZ" : null, activeAccounts, requestedPaymentModel);

    public static PlatformAccountAssignmentDecision Evaluate(string? requestedPlatformCode,
        IEnumerable<(string? PlatformCode, PlatformAccountPaymentModel PaymentModel)> activeAccounts,
        PlatformAccountPaymentModel requestedPaymentModel)
    {
        var accounts = activeAccounts.ToArray();
        if (requestedPaymentModel == PlatformAccountPaymentModel.Salary && accounts.Any(a => a.PaymentModel == PlatformAccountPaymentModel.Salary))
            return PlatformAccountAssignmentDecision.SalaryAccountLimitReached;
        var chefz = accounts.Count(a => IsChefzPlatform(a.PlatformCode));
        var jahez = accounts.Count(a => a.PlatformCode == "JAHEZ");
        if ((IsChefzPlatform(requestedPlatformCode) || requestedPlatformCode == "JAHEZ") && chefz + jahez == accounts.Length)
        {
            if (IsChefzPlatform(requestedPlatformCode)) chefz++;
            else jahez++;
            return chefz <= MaximumActiveAccountsPerRider && jahez <= JahezRules.MaximumActiveAccountsPerRider
                ? PlatformAccountAssignmentDecision.Allowed : PlatformAccountAssignmentDecision.AccountLimitReached;
        }
        return PlatformAccountAssignmentPolicy.Evaluate(accounts.Select(a => a.PaymentModel), requestedPaymentModel);
    }
}
