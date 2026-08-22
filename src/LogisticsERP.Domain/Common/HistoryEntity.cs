namespace LogisticsERP.Domain.Common;

public abstract class HistoryEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
