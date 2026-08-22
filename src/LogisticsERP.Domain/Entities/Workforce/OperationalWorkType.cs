using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class OperationalWorkType : AuditableEntity
{
    public static readonly Guid AdministrativeId = Guid.Parse("019c18d5-62e1-7000-8000-000000000010");
    public static readonly Guid CarId = Guid.Parse("019c18d5-62e1-7000-8000-000000000011");
    public static readonly Guid MotorcycleId = Guid.Parse("019c18d5-62e1-7000-8000-000000000012");

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
