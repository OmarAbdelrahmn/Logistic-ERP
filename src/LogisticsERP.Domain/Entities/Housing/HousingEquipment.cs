using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingEquipment : AuditableEntity
{
    public Guid? FloorId { get; set; }
    public Guid? RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
