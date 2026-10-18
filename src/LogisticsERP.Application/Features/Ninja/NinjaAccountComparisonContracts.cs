namespace LogisticsERP.Application.Features.Ninja;

public sealed record NinjaAccountSheetRow(int RowNumber, string CaptainId, string OwnerName, string City, string OwnerNationalId);
public sealed record NinjaComparisonSystemAccount(Guid AccountId, string CaptainId, string AccountCode, string Status,
    Guid? OwnerEmployeeId, string? OwnerNationalId, string? OwnerNameAr, string? OwnerNameEn,
    Guid OperatingCityId, string? CityCode, string? CityNameAr, string? CityNameEn);
public sealed record NinjaAccountDifference(string Field, string? SheetValue, string? SystemValue);
public sealed record NinjaComparedAccount(NinjaAccountSheetRow Sheet, NinjaComparisonSystemAccount System,
    IReadOnlyList<NinjaAccountDifference> Differences);
public sealed record NinjaSheetOnlyAccount(NinjaAccountSheetRow Sheet,
    IReadOnlyList<NinjaComparisonSystemAccount> PossibleSystemAccounts, int PossibleSystemAccountCount);
public sealed record NinjaAccountComparisonSummary(int SheetAccounts, int SystemAccounts, int MatchingAccounts,
    int DifferentAccounts, int SheetOnlyAccounts, int SystemOnlyAccounts);
public sealed record NinjaAccountComparisonResponse(string FileName, DateTimeOffset ComparedAtUtc, bool ComparisonCompleted,
    int IgnoredFooterRows, NinjaAccountComparisonSummary Summary,
    IReadOnlyList<NinjaComparedAccount> MatchingAccounts, IReadOnlyList<NinjaComparedAccount> DifferentAccounts,
    IReadOnlyList<NinjaSheetOnlyAccount> SheetOnlyAccounts, IReadOnlyList<NinjaComparisonSystemAccount> SystemOnlyAccounts,
    IReadOnlyList<NinjaImportIssue> Issues);
