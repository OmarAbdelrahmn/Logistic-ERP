using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record SimplePlatformUpsertRequest(
    string Code,
    string NameAr,
    string NameEn,
    IReadOnlyList<string> SupportedPaymentModels,
    string Status,
    string? Notes,
    string? ArchiveReason,
    string? RowVersion);

public sealed record SimplePlatformResponse(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    IReadOnlyList<string> SupportedPaymentModels,
    string Status,
    string? Notes,
    string RowVersion);

public sealed record SimplePlatformAccountUpsertRequest(
    Guid PlatformId,
    Guid OperatingCityId,
    Guid OwnerRiderProfileId,
    string Code,
    string ExternalAccountId,
    string? UserName,
    string PaymentModel,
    string Status,
    string? StatusReason,
    DateOnly? AcquisitionDate,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes,
    string? ArchiveReason,
    string? RowVersion);

public sealed record AssignSimplePlatformAccountRequest(
    Guid ActualRiderProfileId,
    DateOnly EffectiveFrom,
    string? Reason,
    bool WasBackdated,
    string? BackdatedReason);

public sealed record ReleaseSimplePlatformAccountRequest(
    DateOnly EffectiveTo,
    string Status,
    string Reason,
    string RowVersion);

public sealed record RotateSimplePlatformCredentialRequest(string Secret, string Reason);

public sealed record SimplePlatformAssignmentResponse(
    Guid Id,
    Guid AccountId,
    string PaymentModel,
    Guid ActualRiderProfileId,
    Guid ActualEmployeeId,
    string ActualRiderNameAr,
    string? ActualRiderNameEn,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string? StartReason,
    string? EndReason,
    bool WasBackdated,
    string? BackdatedReason,
    Guid AssignedByUserId,
    Guid? EndedByUserId,
    string RowVersion);

public sealed record SimplePlatformCredentialVersionResponse(
    Guid Id,
    int Version,
    DateTimeOffset RotatedAtUtc,
    Guid RotatedByUserId,
    string Reason);

public sealed record ActivePlatformAccountErrorDetail(
    Guid AssignmentId,
    Guid AccountId,
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    string PlatformNameEn,
    string ExternalAccountId,
    string PaymentModel);

public sealed record PlatformAssignmentLimitErrorDetail(
    Guid RiderProfileId,
    Guid RequestedAccountId,
    string RequestedPaymentModel,
    int MaximumActiveAccounts,
    int MaximumSalaryAccounts,
    IReadOnlyList<ActivePlatformAccountErrorDetail> ActiveAccounts,
    IReadOnlyList<string> AllowedPaymentModels);

public sealed record RiderPlatformHistoryItemResponse(
    Guid AssignmentId,
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    string PlatformNameEn,
    Guid AccountId,
    string AccountCode,
    string ExternalAccountId,
    string PaymentModel,
    Guid? OwnerRiderProfileId,
    string? OwnerRiderNameAr,
    string? OwnerRiderNameEn,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string? StartReason,
    string? EndReason,
    bool WasBackdated,
    string? BackdatedReason);

public sealed record RiderPlatformHistoryResponse(
    Guid RiderProfileId,
    Guid EmployeeId,
    string RiderNameAr,
    string? RiderNameEn,
    IReadOnlyList<RiderPlatformHistoryItemResponse> Assignments);

public sealed record SimplePlatformAccountResponse(
    Guid Id,
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    string PlatformNameEn,
    Guid OperatingCityId,
    string OperatingCityNameAr,
    string OperatingCityNameEn,
    Guid? OwnerRiderProfileId,
    Guid? OwnerEmployeeId,
    string? OwnerRiderNameAr,
    string? OwnerRiderNameEn,
    string Code,
    string ExternalAccountId,
    string? UserName,
    string PaymentModel,
    string Status,
    string? StatusReason,
    DateOnly? AcquisitionDate,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes,
    SimplePlatformAssignmentResponse? CurrentAssignment,
    string RowVersion);

public interface ISimplePlatformService
{
    Task<Result<IReadOnlyList<SimplePlatformResponse>>> GetPlatformsAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformResponse>> CreatePlatformAsync(
        SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformResponse>> UpdatePlatformAsync(
        Guid id,
        SimplePlatformUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SimplePlatformAccountResponse>>> GetAccountsAsync(
        Guid? accountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? ownerRiderProfileId,
        Guid? actualRiderProfileId,
        string? status,
        string? paymentModel,
        bool currentOnly,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformAccountResponse>> GetAccountAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformAccountResponse>> CreateAccountAsync(
        SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformAccountResponse>> UpdateAccountAsync(
        Guid id,
        SimplePlatformAccountUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformAssignmentResponse>> AssignAccountAsync(
        Guid accountId,
        AssignSimplePlatformAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformAssignmentResponse>> ReleaseAccountAsync(
        Guid accountId,
        ReleaseSimplePlatformAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SimplePlatformCredentialVersionResponse>> RotateCredentialAsync(
        Guid accountId,
        RotateSimplePlatformCredentialRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SimplePlatformAssignmentResponse>>> GetAccountAssignmentHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SimplePlatformCredentialVersionResponse>>> GetCredentialHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<Result<RiderPlatformHistoryResponse>> GetRiderPlatformHistoryAsync(
        Guid riderProfileId,
        CancellationToken cancellationToken = default);
}
