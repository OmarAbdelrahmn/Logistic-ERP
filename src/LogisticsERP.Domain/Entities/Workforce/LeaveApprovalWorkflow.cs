using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Domain.Entities.Workforce;

public sealed class LeaveApprovalWorkflow : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid? LeaveTypeId { get; set; }
    public EmployeeRelationshipType? RelationshipType { get; set; }
    public bool? AppliesToRider { get; set; }
    public Guid? ClientPlatformId { get; set; }
    public int Priority { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}

public sealed class LeaveApprovalWorkflowStep : AuditableEntity
{
    public Guid LeaveApprovalWorkflowId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string RequiredPermissionKey { get; set; } = string.Empty;
    public LeaveApprovalScopeSource ScopeSource { get; set; }
    public bool AllowsReturnForChanges { get; set; }
    public bool RequiresCommentOnApproval { get; set; }
    public int? TargetResponseHours { get; set; }
    public CatalogStatus Status { get; set; } = CatalogStatus.Active;
}
