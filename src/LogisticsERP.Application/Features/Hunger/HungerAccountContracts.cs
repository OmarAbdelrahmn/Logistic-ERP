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
    string? SourceStatus, string RowVersion, DateOnly? DeferredUntil)
{
    public HungerPerson? Owner { get; init; }
    public HungerPerson? ActualRider { get; init; }
    public HungerPerson? SourceActualRider { get; init; }
    public HungerPerson? ReportedCurrentRider { get; init; }
    public string CurrentRiderSource { get; init; } = "None";
    public string ReportedAssignmentStatus { get; init; } = "Unknown";
    public Guid? CurrentAssignmentId { get; init; }
    public DateOnly? AssignmentFrom { get; init; }
    public string? AssignmentRowVersion { get; init; }
    public string? CompensationModel { get; init; }
    public string? PaymentModel { get; init; }
    public string? AccountCode { get; init; }
    public Guid OperatingCityId { get; init; }
    public string? OperatingCityNameAr { get; init; }
    public Guid? SponsorId { get; init; }
    public string? SponsorNameAr { get; init; }
    public string? SimOwnership { get; init; }
    public string? RegisteredVehiclePlate { get; init; }
    public string? RegisteredVehicleSerial { get; init; }
    public string? SourceVehicleDescription { get; init; }
    public string? ActualVehicleType { get; init; }
    public string? ActualVehiclePlate { get; init; }
    public string? ActualVehicleOwnership { get; init; }
    public DateOnly? SourceAssignmentFrom { get; init; }
    public string? DispositionReason { get; init; }
    public string MonthlyUsageStatus { get; init; } = "Unused";
    public string MonthlyUsageSource { get; init; } = "None";
    public string? UsageMonth { get; init; }
    public string? LatestQualitySegment { get; init; }
    public int? LatestQualityRank { get; init; }
    public decimal? LatestQualityScore { get; init; }
    public DateOnly? LatestQualityReportDate { get; init; }
    public bool HasPassword { get; init; }
    public string? LastAssignmentEvent { get; init; }
    public DateTimeOffset? LastAssignmentEventAtUtc { get; init; }
    public bool IsAssigned => CurrentAssignmentId.HasValue;
}
public sealed record HungerPerson(Guid EmployeeId, Guid? RiderProfileId, string NameAr, string? NameEn, string? IqamaNo, string? PhoneNumber)
{
    public string? Nationality { get; init; }
    public string? EmployeeStatus { get; init; }
    public string? RelationshipType { get; init; }
}
public sealed record HungerAccountPage(IReadOnlyList<HungerAccountItem> Items, int TotalCount, int Page, int PageSize);
public sealed record HungerDispositionRequest(string Disposition, string Reason, DateOnly? DeferredUntil, string RowVersion);
public interface IHungerAccountsService
{
    Task<Result<HungerAccountImportPreview>> PreviewAccountsAsync(string fileName, byte[] content, CancellationToken ct = default);
    Task<Result<HungerAccountImportResult>> ConfirmAccountsAsync(Guid previewId, CancellationToken ct = default);
    Task<Result<HungerAccountPage>> AccountsAsync(string? disposition, int page, int pageSize, CancellationToken ct = default);
    Task<Result> DispositionAsync(Guid accountId, HungerDispositionRequest request, CancellationToken ct = default);
}
