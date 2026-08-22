using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Platform;

public sealed class ClientPlatform : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? LogoAssetKey { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public string? Notes { get; set; }
}
