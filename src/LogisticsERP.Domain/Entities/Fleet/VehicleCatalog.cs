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

public sealed class FleetLocation : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public FleetLocationType LocationType { get; set; }
    public Guid? HousingId { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public VehicleCatalogStatus Status { get; set; } = VehicleCatalogStatus.Active;
}

public sealed class Vehicle : AuditableEntity
{
    public string AssetNumber { get; set; } = string.Empty;
    public string NormalizedAssetNumber { get; set; } = string.Empty;
    public string? PlateNumberAr { get; set; }
    public string? NormalizedPlateNumberAr { get; set; }
    public string? PlateNumberEn { get; set; }
    public string? NormalizedPlateNumberEn { get; set; }
    public string? PlateLettersAr { get; set; }
    public string? PlateLettersEn { get; set; }
    public string? PlateDigits { get; set; }
    public string? Vin { get; set; }
    public string? ChassisNumber { get; set; }
    public string? EngineNumber { get; set; }
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
    public Guid? CurrentLocationId { get; set; }
    public long CurrentOdometer { get; set; }
    public DateTimeOffset? LastOdometerAtUtc { get; set; }
    public VehicleOperationalStatus CurrentOperationalStatus { get; set; } = VehicleOperationalStatus.Available;
    public Guid? CurrentAssignmentId { get; set; }
    public DateTimeOffset? DecommissionedAtUtc { get; set; }
    public string? DecommissionReason { get; set; }
    public string? Notes { get; set; }
}
