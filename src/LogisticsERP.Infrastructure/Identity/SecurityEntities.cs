using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Infrastructure.Identity;

public sealed class UserSession : IdentityAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid RefreshTokenFamilyId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? DeviceLabel { get; set; }
    public string? UserAgentHash { get; set; }
    public string? LastIpAddress { get; set; }
    public DateTimeOffset LastUsedAtUtc { get; set; }
    public DateTimeOffset IdleExpiresAtUtc { get; set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public long AuthorizationVersion { get; set; }
}

public sealed class TemporaryCredential : IdentityAuditableEntity
{
    public Guid UserId { get; set; }
    public CredentialPurpose Purpose { get; set; }
    public string CredentialHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid IssuedByUserId { get; set; }
}

public sealed class SupportAccessGrant : IdentityAuditableEntity
{
    public Guid PlatformOperatorUserId { get; set; }
    public string RequestedPermissionsJson { get; set; } = "[]";
    public string RequestedScopesJson { get; set; } = "[]";
    public string Reason { get; set; } = string.Empty;
    public SupportAccessStatus Status { get; set; } = SupportAccessStatus.Pending;
    public DateTimeOffset RequestedStartAtUtc { get; set; }
    public DateTimeOffset RequestedEndAtUtc { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public bool IsBreakGlass { get; set; }
    public string? BreakGlassJustification { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
}
