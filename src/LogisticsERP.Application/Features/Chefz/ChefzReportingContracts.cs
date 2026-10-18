namespace LogisticsERP.Application.Features.Chefz;

public sealed record ChefzSettingsRequest(int OverallAOrders, int OverallBOrders, int OverallCOrders,
    int DailyAOrders, int DailyBOrders, int DailyCOrders, decimal AAcceptanceRate, decimal BAcceptanceRate,
    decimal CAcceptanceRate, int ChangeAfterNoOrderDays, bool FollowUpOnZeroOrdersWithMissedOffers, string? RowVersion);
public sealed record ChefzSettingsResponse(ChefzSettingsRequest Settings, string RowVersion);
public sealed record ChefzStatisticsRequest(int SponsorshipCount, int Working, int NotWorking, int Blocked,
    int PermanentlyBlocked, int UpdateBlocked, int Uploaded, int PrivateLicense, int PendingUpload, int NoLicense,
    int ManagementSuspended, int Motorcycle, int WorkingOutsideSponsorship, int? WorkingWithoutPerformance,
    string? RowVersion = null);
public sealed record ChefzStatisticsResponse(Guid Id, Guid SponsorId, string SponsorNameAr,
    ChefzStatisticsRequest Values, string RowVersion);
public sealed record ChefzDailyRating(DateOnly Date, long Orders, long ValidOffers, long MissedOffers,
    decimal? AcceptanceRate, string Rating);
public sealed record ChefzHalfReport(DateOnly From, DateOnly To, int DaysWithData, long Orders,
    decimal? AcceptanceRate, string? Rating, string? Message);
public sealed record ChefzReportSummary(int HalvesWithData, int DaysWithData, long Orders,
    decimal? AverageOrdersPerHalf, decimal? AcceptanceRate, string? Rating);
public sealed record ChefzRiderReport(Guid? AccountId, Guid? RiderId, int RequestedHalves,
    IReadOnlyList<ChefzHalfReport> Halves, ChefzReportSummary SelectedSummary, ChefzReportSummary AllTimeSummary,
    IReadOnlyList<ChefzDailyRating> Daily, IReadOnlyList<string> Messages)
{
    public string? RiderName { get; init; }
    public string? RiderIqamaNo { get; init; }
    public IReadOnlyList<ChefzAccountPerformanceReport> Accounts { get; init; } = [];
}
public sealed record ChefzAccountPerformanceReport(Guid AccountId, string ExternalAccountId, Guid RiderProfileId,
    string RiderName, ChefzReportSummary Summary, IReadOnlyList<ChefzPerformanceResponse> Daily)
{
    public string? RiderIqamaNo { get; init; }
}
public sealed record ChefzRidersPerformanceReport(DateOnly From, DateOnly To, int RequestedHalves,
    int TotalRiders, ChefzRatingCounts RatingCounts, IReadOnlyList<ChefzRiderPerformanceSummary> Riders);
public sealed record ChefzRatingCounts(int A, int B, int C, int D, int Change, int Unrated);
public sealed record ChefzRiderPerformanceSummary(Guid RiderProfileId, string RiderName,
    ChefzReportSummary SelectedSummary, IReadOnlyList<ChefzHalfReport> Halves,
    IReadOnlyList<ChefzAccountPerformanceSummary> Accounts)
{
    public string? RiderIqamaNo { get; init; }
}
public sealed record ChefzAccountPerformanceSummary(Guid AccountId, string ExternalAccountId, ChefzReportSummary Summary);
public sealed record ChefzManagementReport(Guid SponsorId, string SponsorNameAr, string Application,
    DateOnly ReportDate, DateOnly PerformanceDate, DateOnly CycleFrom, long CycleOrders,
    int AccountsBelowThreeOrdersInCycle, int RidersWithOrdersYesterday,
    int RidersWithThreeOrFewerOrdersYesterday, int RidersWithMoreThanThreeOrdersYesterday);
public sealed record ChefzAccountReminder(Guid AccountId, Guid OwnerEmployeeId, string OwnerName,
    string OwnerStatus, string Condition, string Message);
