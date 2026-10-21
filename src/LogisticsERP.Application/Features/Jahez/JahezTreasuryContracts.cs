using LogisticsERP.Application.Common.Results;
using LogisticsERP.Domain.Entities.Jahez;

namespace LogisticsERP.Application.Features.Jahez;

public static class JahezTreasuryPermissions
{
    public const string Read = "jahez.treasury.read";
    public const string AccountsCreate = "jahez.treasury.accounts.create";
    public const string AccountsUpdate = "jahez.treasury.accounts.update";
    public const string TransfersCreate = "jahez.treasury.transfers.create";
    public const string EntriesCreate = "jahez.treasury.entries.create";
    public const string EvidenceCreate = "jahez.treasury.petroapp-evidence.create";
    public const string Adjust = "jahez.treasury.adjust";
    public const string ReportsRead = "jahez.financial-reports.read";
    public const string ReportsExport = "jahez.financial-reports.export";
    public static IReadOnlyList<string> All { get; } = [Read, AccountsCreate, AccountsUpdate, TransfersCreate, EntriesCreate, EvidenceCreate, Adjust, ReportsRead, ReportsExport];
}

public sealed record TreasuryAccountCreate(string Code, string NameAr, string NameEn, Guid ParentId,
    Guid SponsorId, TreasuryLocation Location, TreasuryPurpose Purpose, TreasuryDirections AllowedDirections, bool IsDefault = false);
public sealed record TreasuryAccountUpdate(string NameAr, string NameEn, bool IsActive, string RowVersion);
public sealed record TreasuryTransferCreate(Guid FromAccountId, Guid ToAccountId, decimal Amount, string Reference, string Reason, DateTimeOffset? OccurredAtUtc = null);
public sealed record TreasuryCashCreate(Guid SubAccountId, decimal Amount, TreasuryEntryKind Kind, string Reference, string Reason, DateTimeOffset? OccurredAtUtc = null);
public sealed record TreasuryReverseCreate(Guid PostingId, string Reference, string Reason);
public sealed record TreasuryVerificationCreate(Guid SubAccountId, decimal Balance, string Reference, string Reason);
public sealed record TreasuryAttributionCreate(TreasurySource Source, Guid SourceId, Guid SponsorId, string Evidence);
public sealed record TreasuryActivationCreate(string RowVersion, string Reason);
public sealed record TreasuryBackfillRequest(string Reason);
public sealed record TreasuryBalance(Guid SubAccountId, string Code, string NameAr, Guid? ParentId, Guid? SponsorId,
    TreasuryLocation Location, bool IsPosting, decimal Balance);
public sealed record TreasuryFilter(DateOnly FromDate, DateOnly ToDate, Guid? SubAccountId = null,
    Guid? SponsorId = null, TreasuryPurpose? Purpose = null, int Page = 1, int PageSize = 50);
public sealed record TreasuryJournalRow(TreasuryPosting Posting, string AccountCode, string AccountNameAr,
    string? SponsorNameAr, Guid? PlatformAccountId, Guid? OperationId, Guid? TransferredDebtId,
    JahezMoneyPurpose? BusinessPurpose, bool IsReconciled, Guid? DriverId, string? DriverNameAr, string? IqamaNo);
public sealed record TreasuryCashCount(Guid SubAccountId, decimal Amount);
public sealed record TreasuryHandoverConfirm(IReadOnlyList<TreasuryCashCount> CashCounts, string Reason, string RowVersion);
public sealed record TreasuryHandoverPreview(DateOnly BusinessDate, IReadOnlyList<TreasuryHandoverLine> Lines,
    decimal PhysicalCash, decimal BankActivity, decimal PetroAppActivity, int MovementCount);
public sealed record TreasuryHandoverDetail(JahezCashboxHandover Handover, IReadOnlyList<TreasuryHandoverLine> Lines);
public sealed record PetroEvidenceCreate(Guid SponsorId, PetroEvidenceKind Kind, DateOnly BusinessDate,
    decimal Amount, decimal AllocatedCollections, string Reference, string Reason, Guid? ReplacesId = null);
public sealed record PetroReconciliation(Guid SponsorId, string SponsorNameAr, decimal Collections,
    decimal? Funding, decimal? AllocatedCollections, decimal? UnallocatedCollections, decimal? FundingDifference,
    decimal? OpeningStatement, decimal? ClosingStatement, decimal? ImportedConsumption, string Status);
public sealed record TreasuryAccountPeriod(Guid SubAccountId, string Code, string NameAr, Guid? ParentId,
    Guid? SponsorId, bool IsPosting, decimal Opening, decimal Receipts, decimal Payments,
    decimal TransfersIn, decimal TransfersOut, decimal Closing);
public sealed record TreasuryReport(string ReportType, TreasuryFilter Filter,
    IReadOnlyList<TreasuryAccountPeriod> Accounts, IReadOnlyList<TreasuryJournalRow> Movements,
    IReadOnlyList<PetroReconciliation> Fuel, TreasuryHandoverDetail? Handover, int TotalMovements);
public sealed record TreasuryExport(byte[] Content, string ContentType, string FileName);
public interface IJahezTreasuryReportRenderer
{
    TreasuryExport Render(TreasuryReport report, string format);
}
public sealed record TreasuryAttributionReview(TreasurySource Source, Guid SourceId, Guid PlatformAccountId,
    Guid? CurrentSponsorId, bool RequiresFinanceDecision, string EvidenceJson);
public sealed record TreasuryReadiness(bool IsActive, string RowVersion, int UnresolvedMovements,
    int UnresolvedFees, int UnresolvedExecutedOperations, int PendingLegacyHandovers,
    IReadOnlyList<Guid> UnverifiedAccounts);

public interface IJahezTreasuryService
{
    Task<Result<IReadOnlyList<TreasurySubAccount>>> TreasuryAccountsAsync(CancellationToken ct = default);
    Task<Result<TreasurySubAccount>> CreateTreasuryAccountAsync(string key, TreasuryAccountCreate request, CancellationToken ct = default);
    Task<Result<TreasurySubAccount>> UpdateTreasuryAccountAsync(string key, Guid id, TreasuryAccountUpdate request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TreasuryBalance>>> TreasuryBalancesAsync(CancellationToken ct = default);
    Task<Result<JahezPage<TreasuryJournalRow>>> TreasuryJournalAsync(TreasuryFilter filter, CancellationToken ct = default);
    Task<Result<Guid>> TransferTreasuryAsync(string key, TreasuryTransferCreate request, CancellationToken ct = default);
    Task<Result<TreasuryPosting>> RecordTreasuryCashAsync(string key, TreasuryCashCreate request, CancellationToken ct = default);
    Task<Result<Guid>> ReverseTreasuryAsync(string key, TreasuryReverseCreate request, CancellationToken ct = default);
    Task<Result<TreasuryBalanceVerification>> VerifyTreasuryBalanceAsync(string key, TreasuryVerificationCreate request, CancellationToken ct = default);
    Task<Result<TreasuryReadiness>> TreasuryReadinessAsync(CancellationToken ct = default);
    Task<Result<TreasuryState>> ActivateTreasuryAsync(string key, TreasuryActivationCreate request, CancellationToken ct = default);
    Task<Result<JahezPage<TreasuryAttributionReview>>> TreasuryAttributionReviewAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<TreasuryOwnership>> AttributeTreasuryAsync(string key, TreasuryAttributionCreate request, CancellationToken ct = default);
    Task<Result<int>> BackfillTreasuryAsync(string key, string reason, CancellationToken ct = default);
    Task<Result<TreasuryHandoverPreview>> PreviewTreasuryHandoverAsync(DateOnly businessDate, CancellationToken ct = default);
    Task<Result<JahezPage<JahezCashboxHandover>>> TreasuryHandoversAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<TreasuryHandoverDetail>> TreasuryHandoverAsync(Guid id, CancellationToken ct = default);
    Task<Result<TreasuryHandoverDetail>> SubmitTreasuryHandoverAsync(string key, JahezCashboxHandoverCreate request, CancellationToken ct = default);
    Task<Result<TreasuryHandoverDetail>> ConfirmTreasuryHandoverAsync(string key, Guid id, TreasuryHandoverConfirm request, CancellationToken ct = default);
    Task<Result<TreasuryHandoverDetail>> CancelTreasuryHandoverAsync(string key, Guid id, JahezCashboxHandoverCancel request, CancellationToken ct = default);
    Task<Result<PetroAppEvidence>> RecordPetroEvidenceAsync(string key, PetroEvidenceCreate request, CancellationToken ct = default);
    Task<Result<JahezPage<PetroAppEvidence>>> PetroEvidenceAsync(TreasuryFilter filter, CancellationToken ct = default);
    Task<Result<TreasuryReport>> TreasuryReportAsync(string type, TreasuryFilter filter, Guid? handoverId = null, CancellationToken ct = default);
    Task<Result<TreasuryExport>> ExportTreasuryReportAsync(string type, string format, TreasuryFilter filter, Guid? handoverId = null, CancellationToken ct = default);
}
