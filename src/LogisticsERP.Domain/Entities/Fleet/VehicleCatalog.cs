using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Fleet;

public sealed class VehicleManufacturer : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public VehicleCatalogStatus Status { get; set; } = VehicleCatalogStatus.Active;
    public int DisplayOrder { get; set; }
}

public sealed class VehicleModel : AuditableEntity
{
    public Guid VehicleManufacturerId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public VehicleFuelType DefaultFuelType { get; set; }
    public VehicleCatalogStatus Status { get; set; } = VehicleCatalogStatus.Active;
}

public sealed class VehicleSupplier : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? CommercialRegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public Address Address { get; set; } = new();
    public VehicleCatalogStatus Status { get; set; } = VehicleCatalogStatus.Active;
    public string? Notes { get; set; }
}

public sealed class Vehicle : AuditableEntity
{
    public string AssetNumber { get; set; } = string.Empty;
    public string NormalizedAssetNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? NormalizedSerialNumber { get; set; }
    public string? PlateNumberAr { get; set; }
    public string? NormalizedPlateNumberAr { get; set; }
    public string? PlateNumberEn { get; set; }
    public string? NormalizedPlateNumberEn { get; set; }
    public string? PlateLettersAr { get; set; }
    public string? PlateLettersEn { get; set; }
    public string? PlateDigits { get; set; }
    public string? Vin { get; set; }
    public string? ChassisNumber { get; set; }
    public string? NormalizedChassisNumber { get; set; }
    public string? EngineNumber { get; set; }
    public Guid? SponsorId { get; set; }
    public Guid? OperatingCityId { get; set; }
    public Guid? PurchasedFromSupplierId { get; set; }
    public VehicleRegistrationType? RegistrationType { get; set; }
    public Guid VehicleManufacturerId { get; set; }
    public Guid VehicleModelId { get; set; }
    public int? ModelYear { get; set; }
    public VehicleType VehicleType { get; set; }
    public VehicleFuelType FuelType { get; set; }
    public VehicleTransmissionType TransmissionType { get; set; }
    public string? ColorAr { get; set; }
    public string? ColorEn { get; set; }
    public VehicleOwnershipType OwnershipType { get; set; }
    public string? OwnerName { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public string? LeaseReference { get; set; }
    public long CurrentOdometer { get; set; }
    public decimal TrackedDistanceKm { get; set; }
    public DateTimeOffset? LastOdometerAtUtc { get; set; }
    public VehicleOperationalStatus CurrentOperationalStatus { get; set; } = VehicleOperationalStatus.Available;
    public Guid? CurrentAssignmentId { get; set; }
    public DateTimeOffset? DecommissionedAtUtc { get; set; }
    public string? DecommissionReason { get; set; }
    public string? Notes { get; set; }
}

public sealed class VehicleIdentityCorrection : HistoryEntity
{
    public Guid VehicleId { get; set; }
    public string BeforeJson { get; set; } = string.Empty;
    public string AfterJson { get; set; } = string.Empty;
    public string? DocumentVersionReferencesJson { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
}

public sealed class VehicleRegistrationTransition : HistoryEntity
{
    public Guid VehicleId { get; set; }
    public VehicleRegistrationType FromType { get; set; }
    public VehicleRegistrationType ToType { get; set; }
    public string OldPlateNumberAr { get; set; } = string.Empty;
    public string OldPlateNumberEn { get; set; } = string.Empty;
    public string NewPlateNumberAr { get; set; } = string.Empty;
    public string NewPlateNumberEn { get; set; } = string.Empty;
    public string? OldPlateLettersAr { get; set; }
    public string? OldPlateLettersEn { get; set; }
    public string? OldPlateDigits { get; set; }
    public string? NewPlateLettersAr { get; set; }
    public string? NewPlateLettersEn { get; set; }
    public string? NewPlateDigits { get; set; }
    public DateTimeOffset EffectiveAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid IstimaraVersionId { get; set; }
    public Guid OperationCardVersionId { get; set; }
    public Guid ActorUserId { get; set; }
}
