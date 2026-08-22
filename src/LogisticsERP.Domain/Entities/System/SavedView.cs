using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.System;

public sealed class SavedView : AuditableEntity
{
    public Guid UserId { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public string FiltersJson { get; set; } = "{}";
    public string SortingJson { get; set; } = "[]";
    public string ColumnsJson { get; set; } = "[]";
    public string ColumnOrderJson { get; set; } = "[]";
    public string Density { get; set; } = "compact";
}
