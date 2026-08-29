using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class HrFormTemplate : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CurrentDraftVersionId { get; set; }
    public Guid? CurrentPublishedVersionId { get; set; }
}
