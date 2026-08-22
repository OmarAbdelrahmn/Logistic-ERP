using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Platform;

public sealed class OperatingCity : AuditableEntity
{
    public static readonly Guid JeddahId = Guid.Parse("019c18d5-62e1-7000-8000-000000000003");
    public static readonly Guid RiyadhId = Guid.Parse("019c18d5-62e1-7000-8000-000000000005");

    public Guid GlobalCityId { get; set; }
    public DateOnly EnabledFrom { get; set; }
    public DateOnly? DisabledAt { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
