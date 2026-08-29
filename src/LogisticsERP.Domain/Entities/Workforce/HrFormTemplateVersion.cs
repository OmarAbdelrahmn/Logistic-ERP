using LogisticsERP.Domain.Common;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class HrFormTemplateVersion : HistoryEntity
{
    public Guid HrFormTemplateId { get; set; }
    public int VersionNumber { get; set; }
    public int DefinitionSchemaVersion { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public string DefinitionSha256 { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
}
