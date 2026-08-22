using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Tags;

public sealed class Tag : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Color { get; set; } = "slate";
    public bool AppliesToEmployees { get; set; }
    public bool AppliesToHousing { get; set; }
    public bool AppliesToClientContracts { get; set; }
    public bool AppliesToPlatformAccounts { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
