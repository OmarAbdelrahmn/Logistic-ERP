using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class InsurancePlanLevel : AuditableEntity
{
    public Guid InsuranceCompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int Rank { get; set; }
    public string? NetworkName { get; set; }
    public string? CoverageClass { get; set; }
    public decimal? AnnualCoverageLimit { get; set; }
    public decimal? DeductiblePercentage { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public InsurancePlanStatus Status { get; set; } = InsurancePlanStatus.Draft;
}
