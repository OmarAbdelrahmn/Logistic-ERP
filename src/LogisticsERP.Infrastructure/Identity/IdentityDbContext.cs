using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Identity;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RolePermissionGrant> RolePermissionGrants => Set<RolePermissionGrant>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserDirectPermissionAssignment> UserDirectPermissionAssignments => Set<UserDirectPermissionAssignment>();
    public DbSet<AccessScope> AccessScopes => Set<AccessScope>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<TemporaryCredential> TemporaryCredentials => Set<TemporaryCredential>();
    public DbSet<SupportAccessGrant> SupportAccessGrants => Set<SupportAccessGrant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => type.Namespace?.StartsWith("LogisticsERP.Infrastructure.Identity.Configurations", StringComparison.Ordinal) == true);

        builder.Entity<ApplicationUser>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<ApplicationRole>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<RolePermissionGrant>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<UserRoleAssignment>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<UserDirectPermissionAssignment>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<AccessScope>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<UserSession>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<TemporaryCredential>().HasQueryFilter(entity => !entity.IsDeleted);
        builder.Entity<SupportAccessGrant>().HasQueryFilter(entity => !entity.IsDeleted);
    }
}
