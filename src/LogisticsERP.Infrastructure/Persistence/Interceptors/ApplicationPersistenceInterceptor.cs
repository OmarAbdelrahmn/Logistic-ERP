using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogisticsERP.Infrastructure.Persistence.Interceptors;

internal sealed class ApplicationPersistenceInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> TemporalClosureProperties = new(StringComparer.Ordinal)
    {
        nameof(TemporalPeriodEntity.EffectiveTo),
        "MoveOutReason",
        "DestinationReference",
        "EndReason"
    };
    private static readonly HashSet<string> ExcludedAuditProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditableEntity.RowVersion),
        "Ciphertext",
        "Nonce",
        "AuthenticationTag",
        "CredentialHash",
        "RefreshTokenHash",
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp"
    };

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
        var changedEntries = context.ChangeTracker.Entries<Entity>()
            .Where(entry => entry.Entity is not AuditEntry && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        foreach (var entry in changedEntries)
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.CreateVersion7();
            }

            ApplyEntityRules(entry, now);
        }

        foreach (var auditEntry in CreateAuditEntries(changedEntries, now))
        {
            context.Set<AuditEntry>().Add(auditEntry);
        }
    }

    private void ApplyEntityRules(EntityEntry<Entity> entry, DateTimeOffset now)
    {
        if (entry.Entity is AuditableEntity auditable)
        {
            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAtUtc = now;
                auditable.CreatedByUserId = currentUser.UserId;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                auditable.IsDeleted = true;
                auditable.DeletedAtUtc = now;
                auditable.DeletedByUserId = currentUser.UserId;
                auditable.DeletionReason ??= "Soft-deleted by the persistence policy.";
                auditable.UpdatedAtUtc = now;
                auditable.UpdatedByUserId = currentUser.UserId;
            }
            else
            {
                auditable.UpdatedAtUtc = now;
                auditable.UpdatedByUserId = currentUser.UserId;

                var isDeletedProperty = entry.Property(nameof(AuditableEntity.IsDeleted));
                if (isDeletedProperty.IsModified && auditable.IsDeleted)
                {
                    auditable.DeletedAtUtc ??= now;
                    auditable.DeletedByUserId ??= currentUser.UserId;
                    auditable.DeletionReason ??= "Soft-deleted by an application operation.";
                }
            }

            return;
        }

        if (entry.Entity is TemporalPeriodEntity temporal)
        {
            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("Temporal period records cannot be deleted.");
            }

            if (entry.State == EntityState.Added)
            {
                temporal.CreatedAtUtc = now;
                temporal.CreatedByUserId ??= currentUser.UserId;
                return;
            }

            var effectiveToProperty = entry.Property(nameof(TemporalPeriodEntity.EffectiveTo));
            var modifiedProperties = entry.Properties
                .Where(property => property.IsModified && property.Metadata.Name is not nameof(TemporalPeriodEntity.RowVersion))
                .Select(property => property.Metadata.Name)
                .ToArray();

            if (!effectiveToProperty.IsModified
                || modifiedProperties.Any(property => !TemporalClosureProperties.Contains(property))
                || effectiveToProperty.OriginalValue is not null
                || temporal.EffectiveTo is null
                || temporal.EffectiveTo < temporal.EffectiveFrom)
            {
                throw new InvalidOperationException(
                    "Temporal period records are immutable except for closing an open period once.");
            }

            temporal.ClosedAtUtc = now;
            temporal.ClosedByUserId = currentUser.UserId;
            return;
        }

        if (entry.Entity is HistoryEntity history)
        {
            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("History records are immutable and cannot be deleted.");
            }

            if (entry.State == EntityState.Modified)
            {
                throw new InvalidOperationException("History records are append-only and cannot be modified.");
            }

            history.CreatedAtUtc = now;
            history.CreatedByUserId ??= currentUser.UserId;
            return;
        }

        if (entry.State == EntityState.Deleted)
        {
            throw new InvalidOperationException($"Hard deletion is prohibited for {entry.Metadata.ClrType.Name}.");
        }
    }

    private IEnumerable<AuditEntry> CreateAuditEntries(
        IEnumerable<EntityEntry<Entity>> entries,
        DateTimeOffset now)
    {
        foreach (var entry in entries)
        {
            var action = GetAction(entry);
            var before = new Dictionary<string, object?>(StringComparer.Ordinal);
            var after = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var property in entry.Properties.Where(property => !IsSensitiveAuditProperty(property.Metadata.Name)))
            {
                if (entry.State != EntityState.Added && (entry.State == EntityState.Deleted || property.IsModified))
                {
                    before[property.Metadata.Name] = property.OriginalValue;
                }

                if (entry.State != EntityState.Deleted && (entry.State == EntityState.Added || property.IsModified))
                {
                    after[property.Metadata.Name] = property.CurrentValue;
                }
            }

            yield return new AuditEntry
            {
                Id = Guid.CreateVersion7(),
                EventId = Guid.CreateVersion7(),
                ActorUserId = currentUser.UserId,
                ActorType = currentUser.UserId.HasValue ? "User" : "System",
                Action = action,
                Category = "Persistence",
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entry.Entity.Id,
                OccurredAtUtc = now,
                CorrelationId = currentUser.CorrelationId ?? Guid.CreateVersion7().ToString(),
                Reason = (entry.Entity as AuditableEntity)?.DeletionReason,
                BeforeJson = before.Count == 0 ? null : JsonSerializer.Serialize(before),
                AfterJson = after.Count == 0 ? null : JsonSerializer.Serialize(after),
                Source = nameof(ApplicationDbContext),
                SchemaVersion = 1,
                CreatedAtUtc = now,
                CreatedByUserId = currentUser.UserId
            };
        }
    }

    private static string GetAction(EntityEntry<Entity> entry)
    {
        if (entry.State == EntityState.Added)
        {
            return "Created";
        }

        if (entry.State == EntityState.Deleted || entry.Entity is AuditableEntity { IsDeleted: true })
        {
            return "SoftDeleted";
        }

        if (entry.Entity is TemporalPeriodEntity { EffectiveTo: not null })
        {
            return "Closed";
        }

        return "Updated";
    }

    private static bool IsSensitiveAuditProperty(string propertyName) =>
        ExcludedAuditProperties.Contains(propertyName)
        || propertyName.EndsWith("Ciphertext", StringComparison.OrdinalIgnoreCase)
        || propertyName.EndsWith("LookupHash", StringComparison.OrdinalIgnoreCase)
        || propertyName.EndsWith("LastFour", StringComparison.OrdinalIgnoreCase);
}
