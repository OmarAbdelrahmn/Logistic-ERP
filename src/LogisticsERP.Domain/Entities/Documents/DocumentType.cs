using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Documents;

public sealed class DocumentType : AuditableEntity
{
    public static readonly Guid ResidencyPermitId = Guid.Parse("019c18d5-62e1-7000-8000-000000000030");
    public static readonly Guid DriverLicenseId = Guid.Parse("019c18d5-62e1-7000-8000-000000000031");
    public static readonly Guid RiderCardId = Guid.Parse("019c18d5-62e1-7000-8000-000000000032");
    public static readonly Guid HealthCardId = Guid.Parse("019c18d5-62e1-7000-8000-000000000033");
    public static readonly Guid PromissoryNoteId = Guid.Parse("019c18d5-62e1-7000-8000-000000000034");
    public static readonly Guid MedicalInsuranceId = Guid.Parse("019c18d5-62e1-7000-8000-000000000035");

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
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
