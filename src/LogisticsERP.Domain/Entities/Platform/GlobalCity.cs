using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Platform;

public sealed class GlobalCity : AuditableEntity
{
    public static readonly Guid JeddahId = Guid.Parse("019c18d5-62e1-7000-8000-000000000002");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string RegionAr { get; set; } = string.Empty;
    public string RegionEn { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SA";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int DisplayOrder { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
