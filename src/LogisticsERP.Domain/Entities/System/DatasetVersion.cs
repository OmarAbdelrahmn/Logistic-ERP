using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.System;

public sealed class DatasetVersion : AuditableEntity
{
    public string ModuleKey { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset LastChangedAtUtc { get; set; }
}
