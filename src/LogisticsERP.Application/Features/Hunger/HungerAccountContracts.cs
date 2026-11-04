using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerAccountSeed(int RowNumber, string AccountId, string OwnerIqama, string? ActualIqama,
    string Phone, string City, string Registry, string SettlementModel, string SourceStatus, string RegisteredVehicleType,
    string? SimOwnership, string? CompensationModel, string? ActualVehicleDescription, DateOnly? AssignmentFrom,
    decimal? OpeningWallet, int? OpeningOrders);
public sealed record HungerAccountImportRow(int RowNumber, string AccountId, Guid? MatchedAccountId, Guid? OwnerEmployeeId,
    Guid? ActualRiderProfileId, string Status, string? Error, string? AssignmentIssue);
public sealed record HungerAccountImportPreview(Guid PreviewId, DateTimeOffset ExpiresAtUtc, bool CanConfirm,
    IReadOnlyList<HungerAccountImportRow> Rows);
public sealed record HungerAccountImportResult(Guid ImportId, int ImportedAccounts, int MatchedActualUsers, int PendingAssignments,
    IReadOnlyList<HungerAccountImportRow> Rows);
public sealed record HungerAccountItem(Guid AccountId, string ExternalAccountId, Guid? OwnerEmployeeId, string? OwnerName,
    string? OwnerIqama, Guid? SourceActualRiderProfileId, string? SourceActualName, string? SourceActualIqama,
    Guid? CurrentActualRiderProfileId, string OperationalDisposition, string AssignmentStatus, string? PhoneNumber,
    string? RegistryIdentifier, string? SettlementModel, string? RegisteredVehicleType, string? SourceCompensationModel,
    string? SourceStatus, string RowVersion, DateOnly? DeferredUntil);
public sealed record HungerAccountPage(IReadOnlyList<HungerAccountItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerDispositionRequest(string Disposition, string Reason, DateOnly? DeferredUntil, string RowVersion);
public interface IHungerAccountsService
{
    Task<Result<HungerAccountImportPreview>> PreviewAccountsAsync(string fileName, byte[] content, CancellationToken ct = default);
    Task<Result<HungerAccountImportResult>> ConfirmAccountsAsync(Guid previewId, CancellationToken ct = default);
    Task<Result<HungerAccountPage>> AccountsAsync(string? disposition, int page, int pageSize, CancellationToken ct = default);
    Task<Result> DispositionAsync(Guid accountId, HungerDispositionRequest request, CancellationToken ct = default);
}
