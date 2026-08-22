using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Documents;

public sealed class DocumentRequirement : AuditableEntity
{
    public Guid DocumentTypeId { get; set; }
    public EmployeeRelationshipType? RelationshipType { get; set; }
    public bool AppliesToRiderProfile { get; set; }
    public bool IsRequired { get; set; }
    public string ReminderOffsetsDays { get; set; } = "90,60,30,7,0";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
