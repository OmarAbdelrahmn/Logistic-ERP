using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Infrastructure.Identity;

public abstract class IdentityAuditableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletionReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RolePermissionGrant : IdentityAuditableEntity
{
    public Guid RoleId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
}

public sealed class UserRoleAssignment : IdentityAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid GrantedByUserId { get; set; }
    public string GrantReason { get; set; } = string.Empty;
    public bool IsAllHousingScope { get; set; }
    public bool IsAllClientScope { get; set; }
    public bool IncludesFuturePlatformContracts { get; set; }
}

public sealed class UserDirectPermissionAssignment : IdentityAuditableEntity
{
    public Guid UserId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public PermissionEffect Effect { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public Guid GrantedByUserId { get; set; }
    public string GrantReason { get; set; } = string.Empty;
    public bool IsAllHousingScope { get; set; }
    public bool IsAllClientScope { get; set; }
    public bool IncludesFuturePlatformContracts { get; set; }
}

public sealed class AccessScope : IdentityAuditableEntity
{
    public Guid? UserRoleAssignmentId { get; set; }
    public Guid? DirectPermissionAssignmentId { get; set; }
    public AccessScopeType ScopeType { get; set; }
    public Guid TargetId { get; set; }
}
