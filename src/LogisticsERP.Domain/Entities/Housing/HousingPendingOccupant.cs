using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingPendingOccupant : AuditableEntity
{
    public Guid RoomId { get; set; }
    public string IqamaNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SourceRow { get; set; }
}
