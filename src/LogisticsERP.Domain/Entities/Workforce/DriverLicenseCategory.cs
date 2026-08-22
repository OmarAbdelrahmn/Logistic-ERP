using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class DriverLicenseCategory : AuditableEntity
{
    public static readonly Guid LightTransportId = Guid.Parse("019c18d5-62e1-7000-8000-000000000020");
    public static readonly Guid MotorcycleId = Guid.Parse("019c18d5-62e1-7000-8000-000000000021");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
