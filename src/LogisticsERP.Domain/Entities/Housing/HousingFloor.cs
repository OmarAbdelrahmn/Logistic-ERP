using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingFloor : AuditableEntity
{
    public Guid HousingId { get; set; }
    public string Name { get; set; } = string.Empty;
}
