using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class InsuranceCompany : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? ProviderRegistrationNumber { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public InsuranceCompanyStatus Status { get; set; } = InsuranceCompanyStatus.Draft;
    public string? Notes { get; set; }
}
