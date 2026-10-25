using System.Text.Json;
using LogisticsERP.Domain.Entities.Jahez;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Jahez;

internal sealed partial class JahezWorkflowService
{
    // Resolve immutable command references before starting the transaction, then take the same
    // sorted account locks used by generic lifecycle, usage edits, and the other platform imports.
    private async Task<Guid[]> DayWriteAccounts(object payload, CancellationToken ct)
    {
        var accountIds = new HashSet<Guid>(); var handoverIds = new HashSet<Guid>(); var referenceIds = new HashSet<Guid>();
        var riderIds = new HashSet<Guid>();
        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array) { foreach (var item in element.EnumerateArray()) Visit(item); return; }
            if (element.ValueKind != JsonValueKind.Object) return;
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.TryGetGuid(out var id))
                {
                    if (property.Name is "AccountId" or "TargetAccountId") accountIds.Add(id);
                    else if (property.Name == "HandoverId") handoverIds.Add(id);
                    else if (property.Name is "id" or "Id") referenceIds.Add(id);
                    else if (property.Name is "riderId" or "RiderProfileId") riderIds.Add(id);
                }
                else Visit(property.Value);
            }
        }
        Visit(JsonSerializer.SerializeToElement(payload));
        foreach (var operationId in await db.JahezTransferredDebts.IgnoreQueryFilters().AsNoTracking()
            .Where(d => referenceIds.Contains(d.Id) || riderIds.Contains(d.RiderProfileId) && d.PaidAtUtc == null)
            .Select(d => d.OperationId).ToArrayAsync(ct)) referenceIds.Add(operationId);
        if (referenceIds.Count > 0)
        {
            var requestReferences = await db.Set<JahezApprovalRequest>().AsNoTracking().Where(r => referenceIds.Contains(r.Id))
                .Select(r => new { r.HandoverId, r.TargetAccountId, r.OperationId }).ToArrayAsync(ct);
            foreach (var request in requestReferences)
            {
                handoverIds.Add(request.HandoverId);
                if (request.TargetAccountId.HasValue) accountIds.Add(request.TargetAccountId.Value);
                if (request.OperationId.HasValue) referenceIds.Add(request.OperationId.Value);
            }
            foreach (var id in await db.Set<JahezOperation>().AsNoTracking().Where(o => referenceIds.Contains(o.Id)).Select(o => o.HandoverId).ToArrayAsync(ct)) handoverIds.Add(id);
            var importAccounts = await (from row in db.Set<JahezImportRow>() join file in db.Set<JahezImportFile>() on row.FileId equals file.Id
                join account in db.PlatformRiderAccounts on row.DriverId equals account.ExternalAccountId
                where referenceIds.Contains(file.BatchId)
                    && db.ClientPlatforms.Any(p => p.Id == account.ClientPlatformId && p.Code == "JAHEZ")
                select account.Id).Distinct().ToArrayAsync(ct);
            accountIds.UnionWith(importAccounts);
        }
        accountIds.UnionWith(await db.Set<JahezAccountSession>().AsNoTracking().Where(h => handoverIds.Contains(h.Id)).Select(h => h.PlatformRiderAccountId).ToArrayAsync(ct));
        return accountIds.Order().ToArray();
    }
}

