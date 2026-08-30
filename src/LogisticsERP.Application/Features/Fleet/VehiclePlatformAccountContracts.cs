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

    Task<Result<IReadOnlyList<VehiclePlatformAccountAssignmentResponse>>> GetProblemsAsync(
        Guid? vehicleId,
        Guid? platformRiderAccountId,
        Guid? platformId,
        Guid? operatingCityId,
        Guid? sponsorId,
        CancellationToken cancellationToken = default);
}
