using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Platform;

public sealed class PermissionDefinition : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public bool RequiresHousingScope { get; set; }
    public bool RequiresClientScope { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsHighTrust { get; set; }
    public string? GrantabilityRule { get; set; }
    public int Version { get; set; } = 1;
    public bool IsDeprecated { get; set; }
    public string? ReplacementKey { get; set; }
    public int DisplayOrder { get; set; }
}
