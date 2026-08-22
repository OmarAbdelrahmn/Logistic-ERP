using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Documents;

public sealed class DocumentType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool AppliesToSponsoredInternal { get; set; }
    public bool AppliesToOutsideRider { get; set; }
    public bool AppliesToRiderProfile { get; set; }
    public bool RequiresNumber { get; set; }
    public bool RequiresIssueDate { get; set; }
    public bool RequiresExpiryDate { get; set; }
    public bool RequiresFile { get; set; }
    public string AllowedMimeTypes { get; set; } = "application/pdf,image/jpeg,image/png";
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
