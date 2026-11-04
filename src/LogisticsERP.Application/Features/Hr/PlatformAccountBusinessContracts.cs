using LogisticsERP.Application.Common.Results;

namespace LogisticsERP.Application.Features.Hr;

public sealed record PlatformBusinessUpdate(string? PhoneNumber, string? RegistryIdentifier, string? SettlementModel,
    string? RegisteredVehicleType, string? RegisteredVehiclePlate, string? RegisteredVehicleSerial,
    DateOnly EffectiveFrom, string Reason, string RowVersion);
public sealed record PlatformVehiclePeriod(Guid RiderProfileId, Guid? VehicleId, string Ownership, string? VehicleType,
    string? Plate, DateTimeOffset FromUtc, DateTimeOffset ThroughUtc, string? Reason);
public sealed record PlatformSimContext(Guid? SimId, string Ownership, Guid? ResponsibleEmployeeId, Guid? HolderRiderProfileId, string? Carrier, string? Reason)
{
    public IReadOnlyList<PlatformSimOwnershipPeriod> OwnershipPeriods { get; init; } = [];
}
public sealed record PlatformSimOwnershipPeriod(DateOnly From, DateOnly Through, string Ownership, string? EvidenceReference);
public sealed record PlatformBusinessContext(Guid AccountId, string? RegisteredVehicleType, string? RegisteredVehiclePlate,
    string? RegisteredVehicleSerial, IReadOnlyList<PlatformVehiclePeriod> ActualVehiclePeriods, string ActualVehicleOwnership,
    PlatformSimContext Sim, DateTimeOffset VerifiedAtUtc, string RowVersion);
public sealed record PlatformSimOwnershipUpdate(string Ownership, string EvidenceReference, DateOnly EffectiveFrom, string RowVersion);
public sealed record PlatformCredentialRevealRequest(string Reason);
public sealed record PlatformCredentialRevealResponse(string Secret);
public interface IPlatformAccountBusinessService
{
    Task<Result<PlatformBusinessContext>> ContextAsync(Guid accountId, DateOnly from, DateOnly through, CancellationToken ct = default);
    Task<Result> UpdateBusinessAsync(Guid accountId, PlatformBusinessUpdate request, CancellationToken ct = default);
    Task<Result> UpdateSimOwnershipAsync(Guid simId, PlatformSimOwnershipUpdate request, CancellationToken ct = default);
    Task<Result<PlatformCredentialRevealResponse>> RevealCredentialAsync(Guid accountId, PlatformCredentialRevealRequest request, CancellationToken ct = default);
}
