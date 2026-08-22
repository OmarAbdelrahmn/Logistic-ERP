namespace LogisticsERP.Domain.Common;

public abstract class TemporalPeriodEntity : Entity
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
