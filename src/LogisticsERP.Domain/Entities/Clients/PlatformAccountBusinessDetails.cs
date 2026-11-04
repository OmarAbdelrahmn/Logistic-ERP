using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Clients;

/// <summary>Registration and operating classification, independent of account assignment status.</summary>
public sealed class PlatformAccountBusinessDetails : AuditableEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RegistryIdentifier { get; set; }
    public string? SettlementModel { get; set; }
    public string? RegisteredVehicleType { get; set; }
    public string? RegisteredVehiclePlate { get; set; }
    public string? RegisteredVehicleSerial { get; set; }
    public string OperationalDisposition { get; set; } = "Normal";
    public string? DispositionReason { get; set; }
    public DateOnly? DeferredUntil { get; set; }
    public string? SimOwnership { get; set; }
    public string? SourceStatus { get; set; }
    public Guid? SourceActualRiderProfileId { get; set; }
    public string? SourceActualIqama { get; set; }
    public string? SourceCompensationModel { get; set; }
    public string? SourceVehicleDescription { get; set; }
    public DateOnly? SourceAssignmentFrom { get; set; }
    public decimal? SourceOpeningWallet { get; set; }
    public int? SourceOpeningOrders { get; set; }
    public string? SourceReference { get; set; }
}

public sealed class PlatformAccountBusinessRevision : HistoryEntity
{
    public Guid PlatformRiderAccountId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
}
