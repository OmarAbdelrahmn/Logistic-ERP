using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingExternalOccupant : AuditableEntity
{
    public Guid RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
}
