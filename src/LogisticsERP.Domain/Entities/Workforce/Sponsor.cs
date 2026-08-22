using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class Sponsor : AuditableEntity
{
    public static readonly Guid AlBawabaCommercialEstablishmentId = Guid.Parse("019c18d5-62e1-7000-8000-000000000040");
    public static readonly Guid AlBawabaNextCompanyId = Guid.Parse("019c18d5-62e1-7000-8000-000000000041");
    public static readonly Guid ExpressGateId = Guid.Parse("019c18d5-62e1-7000-8000-000000000042");

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
