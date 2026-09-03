using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Fleet;

public sealed record ApproveVehiclePlatformAccountAssignmentRequest(
    Guid VehicleId,
    Guid PlatformRiderAccountId,
    DateTimeOffset? EffectiveFromUtc,
    string? Reason);

public sealed record CloseVehiclePlatformAccountAssignmentRequest(
    DateTimeOffset? EffectiveToUtc,
    string Reason,
    string RowVersion);

public sealed record SwitchVehiclePlatformAccountAssignmentRequest(
    Guid TargetVehicleId,
    string Mode,
    DateTimeOffset? EffectiveAtUtc,
    string Reason,
    string RowVersion);

public sealed record AcceptVehiclePlatformAccountSwitchRequest(
    DateTimeOffset? EffectiveAtUtc,
    string RowVersion);

public sealed record CreateSponsorVehicleLeaseAgreementRequest(
    Guid LessorSponsorId,
    Guid LesseeSponsorId,
    IReadOnlyList<Guid> VehicleIds,
    DateOnly? AgreementDate,
    string? AgreementReference,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Notes);

public sealed record CloseSponsorVehicleLeaseAgreementRequest(
    DateOnly? EffectiveTo,
    string Reason,
    string RowVersion);

public sealed record SponsorVehicleLeaseAgreementVehicleResponse(
    Guid Id,
    Guid VehicleId,
    string AssetNumber,
    string? RegistrationNumber,
    string? PlateNumberAr,
    string? PlateNumberEn);

public sealed record SponsorVehicleLeaseEligibleVehicleResponse(
    Guid VehicleId,
    string AssetNumber,
    string? RegistrationNumber,
    string? PlateNumberAr,
    string? PlateNumberEn,
    string VehicleType,
    string OperationalStatus,
    Guid? OperatingCityId);

public sealed record SponsorVehicleLeaseAgreementResponse(
    Guid Id,
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    Guid LessorSponsorId,
    string LessorSponsorNameAr,
    Guid LesseeSponsorId,
    string LesseeSponsorNameAr,
    DateOnly? AgreementDate,
    string? AgreementReference,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    string? EndReason,
    string? Notes,
    IReadOnlyList<SponsorVehicleLeaseAgreementVehicleResponse> Vehicles,
    string RowVersion);

public sealed record VehiclePlatformAccountSwitchResponse(
    Guid Id,
    Guid SourceAssignmentId,
    Guid SourceVehicleId,
    string SourceVehicleAssetNumber,
    string? SourceVehicleRegistrationNumber,
    string? SourceVehiclePlateNumberAr,
    string? SourceVehiclePlateNumberEn,
    Guid TargetVehicleId,
    string TargetVehicleAssetNumber,
    string? TargetVehicleRegistrationNumber,
    string? TargetVehiclePlateNumberAr,
    string? TargetVehiclePlateNumberEn,
    Guid PlatformRiderAccountId,
    string PlatformAccountCode,
    string Mode,
    string Status,
    string Reason,
    DateTimeOffset RequestedAtUtc,
    Guid RequestedByUserId,
    DateTimeOffset? EffectiveAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    Guid? AcceptedByUserId,
    Guid? NewAssignmentId,
    string RowVersion);

public sealed record VehiclePlatformAssignmentProblemResponse(
    string Code,
    string Severity,
    string Message,
    string? Expected,
    string? Actual,
    int? MaximumAccounts,
    int? ActiveAccountCount);

public sealed record VehiclePlatformAccountAssignmentResponse(
    Guid Id,
    Guid VehicleId,
    string VehicleAssetNumber,
    string? VehicleRegistrationNumber,
    string? VehiclePlateNumberAr,
    string? VehiclePlateNumberEn,
    string VehicleType,
    string VehicleOperationalStatus,
    Guid? VehicleSponsorId,
    string? VehicleSponsorNameAr,
    Guid? VehicleOperatingCityId,
    string? VehicleOperatingCityNameAr,
    Guid PlatformRiderAccountId,
    string PlatformAccountCode,
    string ExternalAccountId,
    string PlatformAccountStatus,
    Guid PlatformId,
    string PlatformCode,
    string PlatformNameAr,
    Guid AccountSponsorId,
    string AccountSponsorNameAr,
    Guid AccountOperatingCityId,
    string AccountOperatingCityNameAr,
    Guid? AccountOwnerEmployeeId,
    string? AccountOwnerNameAr,
    DateTimeOffset AssignedAtUtc,
    string? AssignmentReason,
    string ApprovalStatus,
    DateTimeOffset ApprovedAtUtc,
    Guid ApprovedByUserId,
    string Status,
    DateTimeOffset? EndedAtUtc,
    Guid? EndedByUserId,
    string? EndReason,
    bool UsesSponsorVehicleLeaseAgreement,
    Guid? SponsorVehicleLeaseAgreementId,
    bool HasProblems,
    IReadOnlyList<VehiclePlatformAssignmentProblemResponse> Problems,
    string RowVersion);

public interface IVehiclePlatformAccountAssignmentService
{
    Task<Result<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>> GetAssignmentsAsync(
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountAssignmentResponse>> GetAssignmentAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountAssignmentResponse>> ApproveAsync(
        ApproveVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountAssignmentResponse>> CloseAsync(
        Guid assignmentId,
        CloseVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VehiclePlatformAccountSwitchResponse>>> GetSwitchesAsync(
        bool pendingOnly,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountSwitchResponse>> GetSwitchAsync(
        Guid switchId,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountSwitchResponse>> SwitchAsync(
        Guid assignmentId,
        SwitchVehiclePlatformAccountAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<VehiclePlatformAccountSwitchResponse>> AcceptSwitchAsync(
        Guid switchId,
        AcceptVehiclePlatformAccountSwitchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SponsorVehicleLeaseAgreementResponse>>> GetLeaseAgreementsAsync(
        Guid? lessorSponsorId,
        Guid? lesseeSponsorId,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<Result<SponsorVehicleLeaseAgreementResponse>> GetLeaseAgreementAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SponsorVehicleLeaseEligibleVehicleResponse>>> GetLeaseEligibleVehiclesAsync(
        Guid lessorSponsorId,
        DateOnly? effectiveFrom,
        DateOnly? effectiveTo,
        CancellationToken cancellationToken = default);

    Task<Result<SponsorVehicleLeaseAgreementResponse>> CreateLeaseAgreementAsync(
        CreateSponsorVehicleLeaseAgreementRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SponsorVehicleLeaseAgreementResponse>> CloseLeaseAgreementAsync(
        Guid agreementId,
        CloseSponsorVehicleLeaseAgreementRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>> GetProblemsAsync(
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        CancellationToken cancellationToken = default);
}
