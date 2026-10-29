using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Chefz;

public sealed record ChefzAccountDetailsRequest(string AccountType, string? ReportIdNumber = null, string? RowVersion = null,
    string? VehicleType = null, string? DashboardStatus = null);
public sealed record ChefzPasswordRequest(string Password, string Reason);
public sealed record ChefzAccountStatusRequest(string Status, string RowVersion, string? Reason = null);
public sealed record ChefzAssignmentStartDateRequest(DateOnly EffectiveFrom, string RowVersion);
public sealed record ChefzAssignmentStartDateResponse(Guid AssignmentId, Guid AccountId, Guid RiderProfileId,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Status, string RowVersion);
public sealed record ChefzAccountResponse(Guid AccountId, string ExternalAccountId, string ReportIdNumber,
    string AccountType, decimal CommissionRate, decimal DailyFee, bool HasPassword, string RowVersion,
    Guid? SponsorId = null, Guid? OperatingCityId = null, string VehicleType = "Car", string DashboardStatus = "active",
    string Status = "Available", string? StatusReason = null, string AccountRowVersion = "") : LogisticsERP.Application.Features.Hr.IMonthlyAccountUsageResponse
{
    [global::System.Text.Json.Serialization.JsonIgnore]
    public Guid UsageAccountId => AccountId;
    public string MonthlyUsageStatus { get; set; } = "Unused";
    public string UsageMonth { get; set; } = string.Empty;

    public string? RiderIqamaNo { get; init; }
    public bool IsOperationallyBlocked { get; init; }
    public string? OperationalBlockReason { get; init; }
}
public sealed record ChefzSettlementRequest(Guid AssignmentId, int Year, int Month, int Half,
    decimal StartWalletAmount, decimal EndWalletAmount);
public sealed record ChefzSettlementResponse(Guid Id, Guid AccountId, Guid AssignmentId, Guid RiderProfileId,
    string AccountType, DateOnly PeriodFrom, DateOnly PeriodTo, DateOnly ChargedFrom, DateOnly ChargedTo,
    int AssignedDays, decimal StartWalletAmount, decimal EndWalletAmount, decimal GrossEarnings,
    decimal CommissionRate, decimal DailyFee, decimal CompanyAmount, decimal RiderAmount,
    string Source = "Wallet", Guid? ImportId = null, int SourceRows = 0,
    decimal DeliveryFees = 0, decimal VatAmount = 0, decimal Tips = 0,
    decimal Compensation = 0, decimal Bonuses = 0, decimal Deductions = 0)
{
    public string? SponsorNameAr { get; init; }
    public string? SponsorNameEn { get; init; }
    public string RiderName { get; init; } = "";
    public string? RiderIqamaNo { get; init; }
    public string? RiderIban { get; init; }
}
public sealed record ChefzPerformanceResponse(Guid Id, Guid AccountId, Guid AssignmentId, Guid RiderProfileId,
    DateOnly ReportDate, string ReportIdNumber, int TotalValidOffers, int AcceptedOffers,
    int DeclinedOffers, int MissedOffers, decimal AcceptanceRate)
{
    public string ExternalAccountId { get; init; } = "";
    public string AccountType { get; init; } = "Unconfigured";
    public string RiderName { get; init; } = "";
    public string? RiderIqamaNo { get; init; }
}
public sealed record ChefzPerformanceRider(Guid RiderProfileId, string RiderName, IReadOnlyList<ChefzPerformanceResponse> Accounts)
{
    public string? RiderIqamaNo { get; init; }
}
public sealed record ChefzImportIssue(int RowNumber, string? IdNumber, string Message);
public sealed record ChefzImportResponse(Guid? ImportId, DateOnly ReportDate, int ImportedRows, int SkippedRows,
    IReadOnlyList<ChefzImportIssue> Issues)
{
    public IReadOnlyList<ChefzPerformanceRider> Riders { get; init; } = [];
}
public sealed record ChefzPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
public sealed record ChefzAccountCounts(int Total, int Taken, int NotTaken,
    int Available, int Assigned, int Suspended, int Retired, int Archived);
public sealed record ChefzSponsorAccountCounts(Guid? SponsorId, string SponsorNameAr, string? SponsorNameEn,
    ChefzAccountCounts Counts);
public sealed record ChefzAccountsResponse(IReadOnlyList<ChefzAccountResponse> Items, int Page, int PageSize,
    int TotalCount, ChefzAccountCounts Summary, IReadOnlyList<ChefzSponsorAccountCounts> Sponsors);

public interface IChefzService
{
    Task<Result<ChefzOrderSettlementImportResponse>> ProcessOrderSettlementsAsync(string fileName, byte[] content, bool preview, CancellationToken ct = default);
    Task<Result<ChefzOrderSettlementImportResponse>> GetOrderSettlementImportAsync(Guid importId, CancellationToken ct = default);
    Task<Result<ChefzSettingsResponse>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result<ChefzSettingsResponse>> SaveSettingsAsync(ChefzSettingsRequest request, CancellationToken ct = default);
    Task<Result<ChefzPage<ChefzStatisticsResponse>>> GetStatisticsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<ChefzStatisticsResponse>> GetStatisticsAsync(Guid sponsorId, CancellationToken ct = default);
    Task<Result<ChefzStatisticsResponse>> CreateStatisticsAsync(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct = default);
    Task<Result<ChefzStatisticsResponse>> UpdateStatisticsAsync(Guid sponsorId, ChefzStatisticsRequest request, CancellationToken ct = default);
    Task<Result> DeleteStatisticsAsync(Guid sponsorId, string rowVersion, CancellationToken ct = default);
    Task<Result<ChefzRiderReport>> GetRiderReportAsync(Guid? accountId, Guid? riderId, int year, int month, int half, int halves, CancellationToken ct = default);
    Task<Result<ChefzRidersPerformanceReport>> GetAllRidersReportAsync(int year, int month, int half, int halves, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ChefzManagementReport>>> GetManagementReportAsync(DateOnly reportDate, Guid? sponsorId, CancellationToken ct = default);
    Task<Result<ChefzPage<ChefzAccountReminder>>> GetRemindersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<ChefzAccountResponse>> GetAccountAsync(Guid accountId, CancellationToken ct = default);
    Task<Result<ChefzAccountsResponse>> GetAccountsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<ChefzAccountResponse>> SaveAccountAsync(Guid accountId, ChefzAccountDetailsRequest request, CancellationToken ct = default);
    Task<Result<ChefzAccountResponse>> SetAccountStatusAsync(Guid accountId, ChefzAccountStatusRequest request, CancellationToken ct = default);
    Task<Result<ChefzAssignmentStartDateResponse>> UpdateAssignmentStartDateAsync(Guid assignmentId, ChefzAssignmentStartDateRequest request, CancellationToken ct = default);
    Task<Result> SetPasswordAsync(Guid accountId, ChefzPasswordRequest request, CancellationToken ct = default);
    Task<Result<string>> GetPasswordAsync(Guid accountId, CancellationToken ct = default);
    Task<Result<ChefzSettlementResponse>> PreviewSettlementAsync(ChefzSettlementRequest request, CancellationToken ct = default);
    Task<Result<ChefzSettlementResponse>> SaveSettlementAsync(ChefzSettlementRequest request, CancellationToken ct = default);
    Task<Result<ChefzPage<ChefzSettlementResponse>>> GetSettlementsAsync(Guid? accountId, Guid? riderId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ChefzImportResponse>> ImportPerformanceAsync(DateOnly reportDate, string fileName, byte[] content, CancellationToken ct = default);
    Task<Result<ChefzPage<ChefzPerformanceResponse>>> GetPerformanceAsync(Guid? accountId, Guid? riderId, DateOnly fromDate, DateOnly toDate, int page, int pageSize, CancellationToken ct = default);
}

public static class ChefzErrors
{
    public static OperationError Invalid(string message) => new("Chefz.Invalid", message, ErrorType.Validation);
    public static OperationError Conflict(string message) => new("Chefz.Conflict", message, ErrorType.Conflict);
    public static readonly OperationError NotFound = new("Chefz.NotFound", "حساب شيفز أو السجل المطلوب غير موجود.", ErrorType.NotFound);
    public static readonly OperationError Forbidden = new("Chefz.Forbidden", "لا تملك صلاحية للعمل على حسابات شيفز.", ErrorType.Forbidden);
}
