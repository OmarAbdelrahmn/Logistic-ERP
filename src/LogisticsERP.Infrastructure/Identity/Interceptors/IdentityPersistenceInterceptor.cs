using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogisticsERP.Infrastructure.Identity.Interceptors;

internal sealed class IdentityPersistenceInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IdentityAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = entry.Entity.Id == Guid.Empty ? Guid.CreateVersion7() : entry.Entity.Id;
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedByUserId = currentUser.UserId;
                entry.Entity.DeletionReason ??= "Soft-deleted by the identity persistence policy.";
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
        }

        ApplyUserRules(context, now);
        ApplyRoleRules(context, now);
        ApplyProtectedRolePermissionRules(context);

        var unsupportedDeletes = context.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Deleted)
            .Select(entry => entry.Metadata.ClrType.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (unsupportedDeletes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Hard deletion is prohibited for identity records: {string.Join(", ", unsupportedDeletes)}.");
        }
    }

    private void ApplyUserRules(DbContext context, DateTimeOffset now)
    {
        foreach (var entry in context.ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = entry.Entity.Id == Guid.Empty ? Guid.CreateVersion7() : entry.Entity.Id;
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedByUserId = currentUser.UserId;
                entry.Entity.DeletionReason ??= "Soft-deleted by the identity persistence policy.";
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
        }
    }

    private void ApplyRoleRules(DbContext context, DateTimeOffset now)
    {
        foreach (var entry in context.ChangeTracker.Entries<ApplicationRole>())
        {
            var wasProtected = entry.State != EntityState.Added
                && entry.OriginalValues.GetValue<bool>(nameof(ApplicationRole.IsProtected));

            if (wasProtected && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    $"The protected role '{entry.OriginalValues.GetValue<string>(nameof(ApplicationRole.Code))}' cannot be modified or archived.");
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.Id = entry.Entity.Id == Guid.Empty ? Guid.CreateVersion7() : entry.Entity.Id;
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedByUserId = currentUser.UserId;
                entry.Entity.DeletionReason ??= "Soft-deleted by the identity persistence policy.";
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = currentUser.UserId;
            }
        }
    }

    private static void ApplyProtectedRolePermissionRules(DbContext context)
    {
        var protectedGrantChanges = context.ChangeTracker.Entries<RolePermissionGrant>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entry => SystemRoles.IsProtected(entry.Entity.RoleId))
            .Select(entry => entry.Entity.RoleId)
            .Distinct()
            .ToArray();

        if (protectedGrantChanges.Length > 0)
        {
            throw new InvalidOperationException(
                "The baseline permissions of protected system roles cannot be changed at runtime. Use direct user permissions and scopes instead.");
        }
    }
}
