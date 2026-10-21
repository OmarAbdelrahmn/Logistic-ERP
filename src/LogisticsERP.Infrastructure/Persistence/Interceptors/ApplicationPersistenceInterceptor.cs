using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Domain.Common;
using LogisticsERP.Domain.Entities.Chefz;
using LogisticsERP.Domain.Entities.Jahez;
using LogisticsERP.Domain.Entities.System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogisticsERP.Infrastructure.Persistence.Interceptors;

internal sealed class ApplicationPersistenceInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IHttpContextAccessor? httpContextAccessor = null) : SaveChangesInterceptor
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
        "ConcurrencyStamp",
        "DefinitionJson"
    };
    private static readonly HashSet<string> ChefzPerformanceCorrectionProperties = new(StringComparer.Ordinal)
    {
        nameof(ChefzDailyPerformance.RiderClientAssignmentId),
        nameof(ChefzDailyPerformance.RiderProfileId),
        nameof(ChefzDailyPerformance.ImportId),
        nameof(ChefzDailyPerformance.ReportIdNumber),
        nameof(ChefzDailyPerformance.SourceRowNumber),
        nameof(ChefzDailyPerformance.TotalValidOffers),
        nameof(ChefzDailyPerformance.AcceptedOffers),
        nameof(ChefzDailyPerformance.DeclinedOffers),
        nameof(ChefzDailyPerformance.MissedOffers),
        nameof(ChefzDailyPerformance.AcceptanceRate)
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
        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            if (entry.Entity is TreasurySubAccount && (entry.State == EntityState.Deleted
                || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not
                    (nameof(TreasurySubAccount.NameAr) or nameof(TreasurySubAccount.NameEn) or nameof(TreasurySubAccount.IsActive) or nameof(TreasurySubAccount.RowVersion)))))
                throw new InvalidOperationException("Treasury account identity and routing are immutable.");
            if (entry.Entity is TreasuryState && (entry.State == EntityState.Deleted
                || entry.Property(nameof(TreasuryState.IsActive)).OriginalValue is true && entry.Property(nameof(TreasuryState.IsActive)).IsModified))
                throw new InvalidOperationException("Activated treasury cannot be disabled after cutover.");
            if (entry.Entity is JahezCashboxHandover && (entry.State == EntityState.Deleted
                || entry.Property(nameof(JahezCashboxHandover.Status)).OriginalValue is not JahezCashboxHandoverStatus.Pending
                || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not
                    (nameof(JahezCashboxHandover.Status) or nameof(JahezCashboxHandover.AccountantUserId)
                    or nameof(JahezCashboxHandover.ConfirmedAtUtc) or nameof(JahezCashboxHandover.ConfirmationReason)
                    or nameof(JahezCashboxHandover.CancelledAtUtc) or nameof(JahezCashboxHandover.CancellationReason)
                    or nameof(JahezCashboxHandover.AccountantNameAr) or nameof(JahezCashboxHandover.RowVersion)))))
                throw new InvalidOperationException("Cashbox handover snapshots are immutable and can only be confirmed or cancelled once.");
            if (entry.Entity is JahezOperation && entry.Property(nameof(JahezOperation.Status)).OriginalValue is JahezOperationStatus.Executed
                && !IsJahezPaymentMethodCorrection(entry) && !IsJahezCommissionStartCorrection(entry))
                throw new InvalidOperationException("Executed Jahez operations are immutable.");
            if (entry.Entity is JahezApprovalRequest
                && entry.Property(nameof(JahezApprovalRequest.Status)).OriginalValue is JahezApprovalStatus original
                && original != JahezApprovalStatus.Pending)
                throw new InvalidOperationException("Decided Jahez workflow requests are immutable.");
            if (entry.Entity is JahezTransferredDebt && (entry.State == EntityState.Deleted
                || entry.Property(nameof(JahezTransferredDebt.PaidAtUtc)).OriginalValue is not null
                || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not (nameof(JahezTransferredDebt.PaidAtUtc)
                    or nameof(JahezTransferredDebt.PaidByUserId) or nameof(JahezTransferredDebt.RowVersion)))))
                throw new InvalidOperationException("Transferred debt permits one full repayment only.");
        }
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
                if (IsJahezPaymentMethodCorrection(entry) || IsJahezCommissionStartCorrection(entry)) return;
                // Daily performance is corrected by reupload; preserve its account/day and creation identity.
                // Other history records remain append-only except the authorized Jahez corrections above.
                if (history is ChefzDailyPerformance && entry.Properties
                    .Where(property => property.IsModified)
                    .All(property => ChefzPerformanceCorrectionProperties.Contains(property.Metadata.Name)))
                    return;
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

    // Executed payouts permit cash/bank metadata corrections only. Amounts, ownership,
    // execution identity and creation identity remain protected by the history policy.
    private static bool IsJahezPaymentMethodCorrection(EntityEntry<Entity> entry)
    {
        if (entry.State != EntityState.Modified) return false;
        if (entry.Entity is JahezOperation operation)
            return operation.Status == JahezOperationStatus.Executed
                && operation.PaymentStatus == JahezOperationPaymentStatus.Paid && operation.Remaining < 0
                && !operation.TransferToRider
                && operation.Fund is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank
                && entry.Property(nameof(JahezOperation.Fund)).OriginalValue is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank
                && entry.Properties.Where(x => x.IsModified).All(x => x.Metadata.Name is
                    nameof(JahezOperation.Fund) or nameof(JahezOperation.PaymentReference));
        if (entry.Entity is JahezMoneyMovement movement)
            return movement.OperationId.HasValue && movement.Direction == JahezMoneyDirection.Disbursement
                && movement.Fund is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank
                && entry.Property(nameof(JahezMoneyMovement.Fund)).OriginalValue is JahezMoneyFund.Cashbox or JahezMoneyFund.Bank
                && entry.Properties.Where(x => x.IsModified).All(x => x.Metadata.Name is
                    nameof(JahezMoneyMovement.Fund) or nameof(JahezMoneyMovement.Reference));
        return false;
    }

    private static bool IsJahezCommissionStartCorrection(EntityEntry<Entity> entry)
    {
        if (entry.State != EntityState.Modified || entry.Context is not ApplicationDbContext context
            || context.JahezCommissionStartCorrectionOperationId is not Guid operationId) return false;
        if (entry.Entity is JahezOperation operation)
            return operation.Id == operationId && operation.Status == JahezOperationStatus.Executed
                && operation.Mode != JahezOperationMode.Withdraw && operation.Purpose == JahezMoneyPurpose.Settlement
                && !operation.TransferToRider && operation.CommissionStartDate.HasValue
                && operation.CommissionStartDate <= operation.ThroughDate
                && operation.Commission >= 0 && operation.Commission == operation.CalculatedCommission
                && operation.Remaining == operation.Net + operation.Commission
                && entry.Properties.Where(x => x.IsModified).All(x => x.Metadata.Name is
                    nameof(JahezOperation.CommissionStartDate) or nameof(JahezOperation.Commission)
                    or nameof(JahezOperation.CalculatedCommission) or nameof(JahezOperation.Remaining)
                    or nameof(JahezOperation.CalculationJson) or nameof(JahezOperation.PaymentStatus)
                    or nameof(JahezOperation.Fund) or nameof(JahezOperation.PaymentReference)
                    or nameof(JahezOperation.ApprovedRequestId));
        if (entry.Entity is JahezMoneyMovement movement)
            return movement.OperationId == operationId && movement.Purpose == JahezMoneyPurpose.Settlement
                && movement.Amount >= 0 && entry.Properties.Where(x => x.IsModified).All(x => x.Metadata.Name is
                    nameof(JahezMoneyMovement.Amount) or nameof(JahezMoneyMovement.Direction)
                    or nameof(JahezMoneyMovement.Fund) or nameof(JahezMoneyMovement.Reference));
        return false;
    }

    private IEnumerable<AuditEntry> CreateAuditEntries(
        IEnumerable<EntityEntry<Entity>> entries,
        DateTimeOffset now)
    {
        var httpContext = httpContextAccessor?.HttpContext;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        foreach (var entry in entries)
        {
            var action = GetAction(entry);
            var before = new Dictionary<string, object?>(StringComparer.Ordinal);
            var after = new Dictionary<string, object?>(StringComparer.Ordinal);

            // The immutable import file already retains its bytes; audit its identity and hash without duplicating the whole workbook.
            foreach (var property in entry.Properties.Where(property => !IsSensitiveAuditProperty(property.Metadata.Name)
                && !(entry.Entity is JahezImportFile && property.Metadata.Name == nameof(JahezImportFile.Content))))
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
                SessionId = currentUser.SessionId,
                Action = action,
                Category = "Persistence",
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entry.Entity.Id,
                OccurredAtUtc = now,
                CorrelationId = currentUser.CorrelationId ?? Guid.CreateVersion7().ToString(),
                TraceId = TrimTo(httpContext?.TraceIdentifier, 100),
                IpAddress = TrimTo(httpContext?.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = TrimTo(userAgent, 1000),
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

    private static string? TrimTo(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}
