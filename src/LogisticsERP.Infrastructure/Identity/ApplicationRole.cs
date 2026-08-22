using LogisticsERP.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LogisticsERP.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public RoleStatus Status { get; set; } = RoleStatus.Draft;
    public bool IsProtected { get; set; }
    public bool IsTemplate { get; set; }
    public Guid? SourceTemplateId { get; set; }
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
