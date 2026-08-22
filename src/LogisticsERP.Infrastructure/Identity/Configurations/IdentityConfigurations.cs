using LogisticsERP.Application.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity.SeedData;
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
        builder.HasIndex(entity => entity.IsDevelopmentOnly);
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

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            CreateRoleSeed(
                SystemRoles.SystemAdminId,
                SystemRoles.SystemAdmin,
                "مسؤول النظام",
                "System Administrator",
                "إدارة المستخدمين والأدوار والصلاحيات والأمن دون منح تلقائي لكل البيانات التشغيلية الحساسة.",
                "Manages users, roles, permissions, and security without automatic access to all sensitive operational data.",
                seededAt),
            CreateRoleSeed(
                SystemRoles.ManagerId,
                SystemRoles.Manager,
                "مدير",
                "Manager",
                "قراءة تشغيلية أساسية، وتضاف صلاحيات الإدارة والنطاقات حسب مسؤوليات الشخص.",
                "Minimal operational read access; management permissions and scopes are assigned per responsibility.",
                seededAt),
            CreateRoleSeed(
                SystemRoles.UserId,
                SystemRoles.User,
                "مستخدم",
                "User",
                "الوصول إلى الملف الشخصي والجلسات فقط حتى تمنح صلاحيات إضافية.",
                "Access to the user's own profile and sessions until additional permissions are granted.",
                seededAt));
    }

    private static ApplicationRole CreateRoleSeed(
        Guid id,
        string code,
        string nameAr,
        string nameEn,
        string descriptionAr,
        string descriptionEn,
        DateTimeOffset createdAtUtc) => new()
        {
            Id = id,
            Name = code,
            NormalizedName = code,
            Code = code,
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            Status = RoleStatus.Active,
            IsProtected = true,
            IsTemplate = true,
            CreatedAtUtc = createdAtUtc,
            IsDeleted = false,
            ConcurrencyStamp = $"protected-{code.ToLowerInvariant()}-v1"
        };
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

        var seededAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(AuthorizationSeedCatalog.RolePermissions.Select(grant => new RolePermissionGrant
        {
            Id = grant.Id,
            RoleId = grant.RoleId,
            PermissionKey = grant.PermissionKey,
            CreatedAtUtc = seededAt,
            IsDeleted = false
        }));
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
