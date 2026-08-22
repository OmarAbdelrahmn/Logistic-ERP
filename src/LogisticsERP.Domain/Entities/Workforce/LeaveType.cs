using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveType : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool RequiresBalance { get; set; }
    public bool RequiresHrDocuments { get; set; }
    public bool RequiresExitReentryVisa { get; set; }
    public int? MaximumCalendarDays { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
