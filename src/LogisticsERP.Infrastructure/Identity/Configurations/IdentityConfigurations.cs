using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsERP.Infrastructure.Identity.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", "identity");
        builder.Property(entity => entity.DisplayNameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DisplayNameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.PreferredLocale).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.PreferredTheme).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.PreferredDensity).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DeletionReason).HasMaxLength(500);
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        var defaultUserNameIndex = builder.Metadata.FindIndex(
            builder.Property(entity => entity.NormalizedUserName).Metadata);
        if (defaultUserNameIndex is not null)
        {
            builder.Metadata.RemoveIndex(defaultUserNameIndex);
        }

        builder.HasIndex(entity => entity.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedUserName")
            .HasFilter("[NormalizedUserName] IS NOT NULL");
        builder.HasIndex(entity => entity.NormalizedEmail);
        builder.HasIndex(entity => entity.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL");
        builder.HasIndex(entity => entity.Status);
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.Property(entity => entity.Code).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.DescriptionAr).HasMaxLength(500);
        builder.Property(entity => entity.DescriptionEn).HasMaxLength(500);
        builder.Property(entity => entity.DeletionReason).HasMaxLength(500);
        builder.Property(entity => entity.RowVersion).IsRowVersion();

        var defaultRoleNameIndex = builder.Metadata.FindIndex(
            builder.Property(entity => entity.NormalizedName).Metadata);
        if (defaultRoleNameIndex is not null)
        {
            builder.Metadata.RemoveIndex(defaultRoleNameIndex);
        }

        builder.HasOne<ApplicationRole>().WithMany().HasForeignKey(entity => entity.SourceTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.NormalizedName)
            .IsUnique()
            .HasDatabaseName("UX_Roles_NormalizedName")
            .HasFilter("[NormalizedName] IS NOT NULL");
    }
}

internal sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("UserClaims", "identity");
}

internal sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("UserLogins", "identity");
}

internal sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("UserTokens", "identity");
}

internal sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTable("RoleClaims", "identity");
}

internal sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("UserRoles", "identity");
}

internal sealed class RolePermissionGrantConfiguration : IEntityTypeConfiguration<RolePermissionGrant>
{
    public void Configure(EntityTypeBuilder<RolePermissionGrant> builder)
    {
        builder.ConfigureAuditableEntity("RolePermissions");
        builder.Property(entity => entity.PermissionKey).HasMaxLength(150).IsRequired();
        builder.HasOne<ApplicationRole>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.RoleId, entity.PermissionKey }).IsUnique();
        builder.HasIndex(entity => entity.PermissionKey);
    }
}

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ConfigureAuditableEntity("UserRoleAssignments");
        builder.Property(entity => entity.GrantReason).HasMaxLength(1000).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationRole>().WithMany().HasForeignKey(entity => entity.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.UserId, entity.RoleId, entity.StartsAtUtc });
        builder.HasIndex(entity => new { entity.UserId, entity.ExpiresAtUtc });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_UserRoleAssignments_TimeRange",
            "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [StartsAtUtc]"));
    }
}

internal sealed class UserDirectPermissionAssignmentConfiguration : IEntityTypeConfiguration<UserDirectPermissionAssignment>
{
    public void Configure(EntityTypeBuilder<UserDirectPermissionAssignment> builder)
    {
        builder.ConfigureAuditableEntity("UserDirectPermissionAssignments");
        builder.Property(entity => entity.PermissionKey).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.GrantReason).HasMaxLength(1000).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.UserId, entity.PermissionKey, entity.StartsAtUtc });
        builder.HasIndex(entity => new { entity.UserId, entity.ExpiresAtUtc });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_UserDirectPermissionAssignments_TimeRange",
            "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [StartsAtUtc]"));
    }
}

internal sealed class AccessScopeConfiguration : IEntityTypeConfiguration<AccessScope>
{
    public void Configure(EntityTypeBuilder<AccessScope> builder)
    {
        builder.ConfigureAuditableEntity("AccessScopes");
        builder.HasOne<UserRoleAssignment>().WithMany().HasForeignKey(entity => entity.UserRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserDirectPermissionAssignment>().WithMany().HasForeignKey(entity => entity.DirectPermissionAssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new
        {
            entity.UserRoleAssignmentId,
            entity.DirectPermissionAssignmentId,
            entity.ScopeType,
            entity.TargetId
        }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AccessScopes_ExactlyOneParent",
            "CASE WHEN [UserRoleAssignmentId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [DirectPermissionAssignmentId] IS NULL THEN 0 ELSE 1 END = 1"));
    }
}

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ConfigureAuditableEntity("UserSessions");
        builder.Property(entity => entity.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.DeviceLabel).HasMaxLength(200);
        builder.Property(entity => entity.UserAgentHash).HasMaxLength(128);
        builder.Property(entity => entity.LastIpAddress).HasMaxLength(64);
        builder.Property(entity => entity.RevocationReason).HasMaxLength(1000);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.RefreshTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.RevokedAtUtc, entity.AbsoluteExpiresAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_UserSessions_IdleExpiry", "[IdleExpiresAtUtc] > [CreatedAtUtc]");
            table.HasCheckConstraint("CK_UserSessions_AbsoluteExpiry", "[AbsoluteExpiresAtUtc] > [CreatedAtUtc]");
        });
    }
}

internal sealed class TemporaryCredentialConfiguration : IEntityTypeConfiguration<TemporaryCredential>
{
    public void Configure(EntityTypeBuilder<TemporaryCredential> builder)
    {
        builder.ConfigureAuditableEntity("TemporaryCredentials");
        builder.Property(entity => entity.CredentialHash).HasMaxLength(128).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.CredentialHash).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.Purpose, entity.ExpiresAtUtc });
    }
}

internal sealed class SupportAccessGrantConfiguration : IEntityTypeConfiguration<SupportAccessGrant>
{
    public void Configure(EntityTypeBuilder<SupportAccessGrant> builder)
    {
        builder.ConfigureAuditableEntity("SupportAccessGrants");
        builder.Property(entity => entity.RequestedPermissionsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RequestedScopesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.BreakGlassJustification).HasMaxLength(2000);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.PlatformOperatorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.Status, entity.RequestedStartAtUtc });
        builder.HasIndex(entity => new { entity.PlatformOperatorUserId, entity.RequestedEndAtUtc });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_SupportAccessGrants_TimeRange", "[RequestedEndAtUtc] > [RequestedStartAtUtc]");
            table.HasCheckConstraint("CK_SupportAccessGrants_MaxDuration", "DATEDIFF(HOUR, [RequestedStartAtUtc], [RequestedEndAtUtc]) <= 24");
            table.HasCheckConstraint("CK_SupportAccessGrants_BreakGlassReason", "[IsBreakGlass] = 0 OR [BreakGlassJustification] IS NOT NULL");
        });
    }
}
