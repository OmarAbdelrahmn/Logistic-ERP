using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Housing;

public sealed class HousingWarehouse : AuditableEntity
{
    public Guid HousingId { get; set; }
    public string NameAr { get; set; } = "المستودع الافتراضي";
    public string NameEn { get; set; } = "Default warehouse";
    public bool IsDefault { get; set; } = true;
}

public sealed class HousingWarehouseItem : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class HousingWarehouseItemBalance : AuditableEntity
{
    public Guid ItemId { get; set; }
    public HousingWarehouseItemStatus Status { get; set; }
    public decimal Quantity { get; set; }
}
