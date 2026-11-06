using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hunger;

public sealed record HungerAccountSeed(int RowNumber, string AccountId, string OwnerIqama, string? ActualIqama,
    string Phone, string City, string Registry, string SettlementModel, string SourceStatus, string RegisteredVehicleType,
    string? SimOwnership, string? CompensationModel, string? ActualVehicleDescription, DateOnly? AssignmentFrom,
    decimal? OpeningWallet, int? OpeningOrders);
public sealed record HungerAccountImportRow(int RowNumber, string AccountId, Guid? MatchedAccountId, Guid? OwnerEmployeeId,
    Guid? ActualRiderProfileId, string Status, string? Error, string? AssignmentIssue)
{
    public IReadOnlyList<HungerAccountFieldChange> Changes { get; init; } = [];
    public Guid? PlatformId { get; init; }
}
public sealed record HungerAccountFieldChange(string Field, string? Existing, string? Incoming);
public sealed record HungerAccountRowResolution(int RowNumber, string Action, Guid? PlatformId = null);
public sealed record HungerAccountImportConfirmation(IReadOnlyList<HungerAccountRowResolution> Resolutions);
public sealed record HungerAccountImportPreview(Guid PreviewId, DateTimeOffset ExpiresAtUtc, bool CanConfirm,
    IReadOnlyList<HungerAccountImportRow> Rows)
{
}
public sealed record HungerAccountImportResult(Guid ImportId, int ImportedAccounts, int MatchedActualUsers, int PendingAssignments,
    IReadOnlyList<HungerAccountImportRow> Rows)
{
    public string? ResolutionHash { get; init; }
}
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
    public string? SourceSimOwnership { get; init; }
    public PlatformSimContext? Sim { get; init; }
    public HungerPerson? SimResponsibleEmployee { get; init; }
    public HungerPerson? SimHolder { get; init; }
    public IReadOnlyList<PlatformVehiclePeriod> ActualVehiclePeriods { get; init; } = [];
    public string ActualVehicleCustodyStatus { get; init; } = "Unknown";
    public DateTimeOffset? ContextVerifiedAtUtc { get; init; }
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
    Task<Result<HungerAccountImportResult>> ConfirmAccountsAsync(Guid previewId, HungerAccountImportConfirmation request, CancellationToken ct = default);
    Task<Result<HungerFile>> AccountTemplateAsync(CancellationToken ct = default);
    Task<Result<HungerFile>> ExportAccountsAsync(HungerAccountFilter filter, string format = "xlsx", CancellationToken ct = default);
    Task<Result<IReadOnlyList<HungerDeferredReminder>>> DeferredRemindersAsync(DateOnly? dueThrough = null, CancellationToken ct = default);
    Task<Result<HungerAccountPage>> AccountsAsync(string? disposition, int page, int pageSize, CancellationToken ct = default);
    Task<Result<HungerAccountPage>> AccountsFilteredAsync(HungerAccountFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<Result> DispositionAsync(Guid accountId, HungerDispositionRequest request, CancellationToken ct = default);
}

public sealed record HungerDeferredReminder(Guid AccountId, Guid PlatformId, string ExternalAccountId, DateOnly DeferredUntil, int OverdueDays);

public sealed record HungerAccountFilter
{
    public Guid? ActualRiderProfileId { get; init; }
    public string? ActualRiderName { get; init; }
    public string? ActualRiderIqama { get; init; }
    public string? ActualRiderPhone { get; init; }
    public string? SourceActualName { get; init; }
    public string? SourceActualIqama { get; init; }
    public Guid? SponsorId { get; init; }
    public string? SponsorNameAr { get; init; }
    public string? PaymentModel { get; init; }
    public string? CompensationModel { get; init; }
    public string? SourceStatus { get; init; }
    public string? MonthlyUsageStatus { get; init; }
    public string? LatestQualitySegment { get; init; }
    public int? QualityRankFrom { get; init; }
    public int? QualityRankThrough { get; init; }
    public decimal? QualityScoreFrom { get; init; }
    public decimal? QualityScoreThrough { get; init; }
    public decimal? QualityScoreEqual { get; init; }
    public DateOnly? DeferredFrom { get; init; }
    public DateOnly? DeferredThrough { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? Through { get; init; }
    public string? ExternalAccountId { get; init; }
    public string? AccountCode { get; init; }
    public string? OwnerName { get; init; }
    public string? OwnerNameEn { get; init; }
    public string? OwnerIqama { get; init; }
    public string? OwnerPhone { get; init; }
    public string? OwnerNationality { get; init; }
    public string? OwnerEmployeeStatus { get; init; }
    public string? OwnerRelationshipType { get; init; }
    public string? PhoneNumber { get; init; }
    public string? OperatingCityNameAr { get; init; }
    public string? RegistryIdentifier { get; init; }
    public string? SettlementModel { get; init; }
    public string? RegisteredVehicleType { get; init; }
    public string? RegisteredVehiclePlate { get; init; }
    public string? RegisteredVehicleSerial { get; init; }
    public string? ActualVehicleType { get; init; }
    public string? ActualVehiclePlate { get; init; }
    public string? ActualVehicleOwnership { get; init; }
    public string? ActualVehicleCustodyStatus { get; init; }
    public string? SimOwnership { get; init; }
    public string? SourceSimOwnership { get; init; }
    public string? SimResponsibleName { get; init; }
    public string? SimResponsibleIqama { get; init; }
    public string? SimResponsiblePhone { get; init; }
    public string? SimHolderName { get; init; }
    public string? SimHolderIqama { get; init; }
    public string? SimHolderPhone { get; init; }
    public string? OperationalDisposition { get; init; }
    public string? AssignmentStatus { get; init; }
    public Guid? OwnerEmployeeId { get; init; }
    public Guid? OperatingCityId { get; init; }
    public Guid? SimResponsibleEmployeeId { get; init; }
    public Guid? SimHolderRiderProfileId { get; init; }
}
