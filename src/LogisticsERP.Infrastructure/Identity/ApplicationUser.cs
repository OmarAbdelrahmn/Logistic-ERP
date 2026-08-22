using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LogisticsERP.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? EmployeeId { get; set; }
    public string DisplayNameAr { get; set; } = string.Empty;
    public string DisplayNameEn { get; set; } = string.Empty;
    public UserAccountStatus Status { get; set; } = UserAccountStatus.PendingTemporaryPassword;
    public string PreferredLocale { get; set; } = "ar";
    public string PreferredTheme { get; set; } = "light";
    public string PreferredDensity { get; set; } = "compact";
    public bool RequiresPasswordChange { get; set; } = true;
    public bool IsDevelopmentOnly { get; set; }
    public long AuthorizationVersion { get; set; } = 1;
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public DateTimeOffset? LastActivityAtUtc { get; set; }
    public DateTimeOffset? PasswordChangedAtUtc { get; set; }
    public DateTimeOffset? SessionsRevokedAtUtc { get; set; }
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
