using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record ClientPlatformUpsertRequest(string Code, string NameAr, string NameEn, string Status, string? Notes, string? RowVersion);
public sealed record ClientPlatformResponse(Guid Id, string Code, string NameAr, string NameEn, string Status, string? Notes, string RowVersion);

public sealed record ClientContractUpsertRequest(
    Guid ClientPlatformId,
    string Code,
    string DisplayNameAr,
    string DisplayNameEn,
    string? ExternalBusinessAccountId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    string? StatusReason,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string? Notes,
    string? RowVersion);

public sealed record ClientContractResponse(Guid Id, Guid ClientPlatformId, string PlatformNameAr, string Code, string DisplayNameAr, string DisplayNameEn, string? ExternalBusinessAccountId, DateOnly? StartDate, DateOnly? EndDate, string Status, string? StatusReason, string? ContactName, string? ContactPhone, string? ContactEmail, string? Notes, string RowVersion);

public sealed record PlatformAccountUpsertRequest(Guid ClientContractId, Guid ClientPlatformId, Guid? RegisteredEmployeeId, Guid? SponsorId, Guid OperatingCityId, string RegistrationType, string BillingMode, string Code, string ExternalAccountId, string? UserName, string? LabelAr, string? LabelEn, string Status, string? StatusReason, DateOnly? AcquisitionDate, DateOnly? StartDate, DateOnly? EndDate, string? OwnershipNotes, string? OperationalNotes, string? RowVersion);
public sealed record PlatformAccountResponse(Guid Id, Guid ClientContractId, string ContractNameAr, Guid ClientPlatformId, string PlatformNameAr, Guid? RegisteredEmployeeId, string? RegisteredEmployeeNameAr, Guid? SponsorId, string? SponsorNameAr, Guid OperatingCityId, string OperatingCityAr, string RegistrationType, string BillingMode, string Code, string ExternalAccountId, string? UserName, string? LabelAr, string? LabelEn, string Status, string? StatusReason, DateOnly? AcquisitionDate, DateOnly? StartDate, DateOnly? EndDate, string? OwnershipNotes, string? OperationalNotes, string RowVersion);

public sealed record RotatePlatformCredentialRequest(string Secret, string Reason);
public sealed record PlatformCredentialVersionResponse(
    Guid Id,
    Guid PlatformRiderAccountId,
    int KeyVersion,
    DateTimeOffset RotatedAtUtc,
    Guid RotatedByUserId,
    string RotationReason,
    Guid? SupersededVersionId);

public sealed record PlatformRegistrationUpsertRequest(Guid RegisteredEmployeeId, Guid RiderProfileId, Guid ClientPlatformId, Guid ClientContractId, Guid? SponsorId, Guid OperatingCityId, string RegistrationType, string Status, string? StatusReason, DateTimeOffset? RequestedAtUtc, DateTimeOffset? ActivatedAtUtc, Guid? PlatformRiderAccountId, string? Notes, string? RowVersion);
public sealed record PlatformRegistrationResponse(Guid Id, Guid RegisteredEmployeeId, string RegisteredEmployeeNameAr, Guid RiderProfileId, Guid ClientPlatformId, string PlatformNameAr, Guid ClientContractId, string ContractNameAr, Guid? SponsorId, string? SponsorNameAr, Guid OperatingCityId, string OperatingCityAr, string RegistrationType, string Status, string? StatusReason, DateTimeOffset? RequestedAtUtc, DateTimeOffset? ActivatedAtUtc, Guid? PlatformRiderAccountId, string? Notes, string RowVersion);

public sealed record AssignPlatformAccountRequest(Guid ActualEmployeeId, Guid RiderProfileId, Guid ClientContractId, Guid PlatformRiderAccountId, DateOnly EffectiveFrom, string Status, string? StartReason, string? OperationalAgreementReference, string? OperationalAgreementNotes, bool WasBackdated, string? BackdatedReason);
public sealed record ClosePlatformAssignmentRequest(DateOnly EffectiveTo, string Status, string EndReason, string RowVersion);
public sealed record PlatformAssignmentResponse(Guid Id, Guid ActualEmployeeId, string ActualEmployeeNameAr, Guid RiderProfileId, Guid ClientContractId, string ContractNameAr, Guid PlatformRiderAccountId, string ExternalAccountId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Status, string? StartReason, string? EndReason, string? OperationalAgreementReference, string? OperationalAgreementNotes, bool WasBackdated, string? BackdatedReason, string RowVersion);

public interface IPlatformOperationsService
{
    Task<Result<IReadOnlyList<ClientPlatformResponse>>> GetPlatformsAsync(CancellationToken cancellationToken = default);
    Task<Result<ClientPlatformResponse>> UpsertPlatformAsync(Guid? id, ClientPlatformUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ClientContractResponse>>> GetContractsAsync(Guid? platformId, CancellationToken cancellationToken = default);
    Task<Result<ClientContractResponse>> UpsertContractAsync(Guid? id, ClientContractUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PlatformAccountResponse>>> GetAccountsAsync(Guid? platformId, CancellationToken cancellationToken = default);
    Task<Result<PlatformAccountResponse>> UpsertAccountAsync(Guid? id, PlatformAccountUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PlatformCredentialVersionResponse>>> GetCredentialVersionsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Result<PlatformCredentialVersionResponse>> RotateCredentialAsync(Guid accountId, RotatePlatformCredentialRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PlatformRegistrationResponse>>> GetRegistrationsAsync(Guid? riderProfileId, CancellationToken cancellationToken = default);
    Task<Result<PlatformRegistrationResponse>> UpsertRegistrationAsync(Guid? id, PlatformRegistrationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PlatformAssignmentResponse>>> GetAssignmentsAsync(Guid? riderProfileId, bool currentOnly, CancellationToken cancellationToken = default);
    Task<Result<PlatformAssignmentResponse>> AssignAccountAsync(AssignPlatformAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<PlatformAssignmentResponse>> CloseAssignmentAsync(Guid id, ClosePlatformAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(string resource, Guid id, ArchiveRequest request, CancellationToken cancellationToken = default);
}
