using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class Sponsor : AuditableEntity
{
    public Guid CompanyProfileId { get; set; }
    public string EmployerIdentityNumber { get; set; } = string.Empty;
    public string RegistryNameAr { get; set; } = string.Empty;
    public string? RegistryNameEn { get; set; }
    public string? CommercialRegistrationNumber { get; set; }
    public string? UnifiedNationalNumber { get; set; }
    public SponsorType SponsorType { get; set; } = SponsorType.Establishment;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
    public DateOnly? ActiveFrom { get; set; }
    public DateOnly? ActiveTo { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public Address Address { get; set; } = new();
    public string? Notes { get; set; }
}
