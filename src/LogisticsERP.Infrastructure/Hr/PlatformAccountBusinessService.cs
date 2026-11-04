using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.Hr;
using LogisticsERP.Domain.Entities.Clients;
using LogisticsERP.Domain.Entities.Fleet;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Domain.Telecom;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogisticsERP.Infrastructure.Hr;

internal sealed class PlatformAccountBusinessService(ApplicationDbContext db, ICurrentUser user, IPermissionChecker permissions,
    TimeProvider clock, IConfiguration configuration, IPlatformCredentialProtector credentialProtector) : IPlatformAccountBusinessService
{
    private static readonly JsonSerializerOptions HistoryJson = new() { PropertyNameCaseInsensitive = true };
    public async Task<Result<PlatformCredentialRevealResponse>> RevealCredentialAsync(Guid accountId, PlatformCredentialRevealRequest request, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await Allowed(PermissionKeys.PlatformFields.Reveal, account.ClientPlatformId, ct)) return Result.Failure<PlatformCredentialRevealResponse>(HrErrors.Forbidden);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000) return Result.Failure<PlatformCredentialRevealResponse>(HrErrors.InvalidRequest);
        var credential = await db.PlatformAccountCredentialVersions.AsNoTracking().Where(c => c.PlatformRiderAccountId == accountId).OrderByDescending(c => c.KeyVersion).FirstOrDefaultAsync(ct);
        if (credential is null) return Result.Failure<PlatformCredentialRevealResponse>(HrErrors.NotFound);
        var secret = credentialProtector.Unprotect(new(credential.Ciphertext, credential.Nonce, credential.AuthenticationTag));
        db.AuditEntries.Add(new LogisticsERP.Domain.Entities.System.AuditEntry { ActorUserId = user.UserId, ActorType = "User", SessionId = user.SessionId,
            Action = "CredentialRevealed", Category = "PlatformAccounts", EntityType = nameof(PlatformAccountCredentialVersion), EntityId = credential.Id,
            OccurredAtUtc = clock.GetUtcNow(), CorrelationId = user.CorrelationId ?? string.Empty, Source = "PlatformBusinessService", Reason = request.Reason });
        await db.SaveChangesAsync(ct);
        return Result.Success(new PlatformCredentialRevealResponse(secret));
    }
    private Task<bool> Allowed(string key, Guid platformId, CancellationToken ct) => user.UserId is { } uid && user.AuthorizationVersion is { } version
        ? permissions.HasPermissionAsync(uid, version, key, new(AccessScopeType.ClientPlatform, platformId), ct) : Task.FromResult(false);
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
    public async Task<Result<PlatformBusinessContext>> ContextAsync(Guid accountId, DateOnly from, DateOnly through, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await Allowed(PermissionKeys.Operations.PlatformAccountsRead, account.ClientPlatformId, ct)) return Result.Failure<PlatformBusinessContext>(HrErrors.Forbidden);
        if (from == default || through < from || through == DateOnly.MaxValue || through > Today || through.DayNumber - from.DayNumber > 366) return Result.Failure<PlatformBusinessContext>(HrErrors.InvalidRequest);
        var details = await db.Set<PlatformAccountBusinessDetails>().AsNoTracking().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        var history = await db.Set<PlatformAccountBusinessRevision>().AsNoTracking().Where(r => r.PlatformRiderAccountId == accountId && r.EffectiveFrom <= from)
            .OrderByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).FirstOrDefaultAsync(ct);
        var registeredDetails = details;
        if (history is not null)
        {
            using var json = JsonDocument.Parse(history.AfterJson); var root = json.RootElement;
            if (root.TryGetProperty("details", out var nested) || root.TryGetProperty("Details", out nested)) root = nested;
            if (root.TryGetProperty("registeredVehicleType", out _) || root.TryGetProperty("RegisteredVehicleType", out _))
                registeredDetails = JsonSerializer.Deserialize<PlatformAccountBusinessDetails>(root.GetRawText(), HistoryJson);
        }
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
        var end = new DateTimeOffset(through.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
        var assignments = await db.RiderClientAssignments.AsNoTracking().Where(a => a.PlatformRiderAccountId == accountId && a.Status != RiderAssignmentStatus.Cancelled
            && a.EffectiveFrom <= through && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArrayAsync(ct);
        var riderIds = assignments.Select(a => a.RiderProfileId).ToArray();
        var candidateVehiclePeriods = await db.RiderVehicleAssignments.AsNoTracking().Where(v => v.Status != RiderVehicleAssignmentStatus.Cancelled && v.Status != RiderVehicleAssignmentStatus.Corrected
            && v.StartedAtUtc < end && (v.EndedAtUtc == null || v.EndedAtUtc > start)
            && (riderIds.Contains(v.RiderProfileId) || !v.IsRealRider)).ToArrayAsync(ct);
        var realIds = candidateVehiclePeriods.Where(v => !v.IsRealRider).Select(v => v.Id).ToArray();
        var actualPeople = await (from r in db.Set<RealRider>().AsNoTracking() join e in db.Employees.AsNoTracking() on r.IqamaNo equals e.IqamaNo
            join p in db.RiderProfiles.AsNoTracking() on e.Id equals p.EmployeeId where realIds.Contains(r.RiderVehicleAssignmentId)
            select new { r.RiderVehicleAssignmentId, RiderProfileId = p.Id }).ToArrayAsync(ct);
        var vehicleIds = candidateVehiclePeriods.Select(v => v.VehicleId).ToArray();
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id)).ToArrayAsync(ct);
        var result = new List<PlatformVehiclePeriod>();
        foreach (var assignment in assignments)
        {
            var aStart = new DateTimeOffset(assignment.EffectiveFrom.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime();
            var aEnd = assignment.EffectiveTo.HasValue ? new DateTimeOffset(assignment.EffectiveTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).ToUniversalTime() : end;
            var left = aStart > start ? aStart : start; var right = aEnd < end ? aEnd : end;
            var periods = candidateVehiclePeriods.Where(v => v.IsRealRider ? v.RiderProfileId == assignment.RiderProfileId
                : actualPeople.Count(p => p.RiderVehicleAssignmentId == v.Id && p.RiderProfileId == assignment.RiderProfileId) == 1).Where(v => v.StartedAtUtc < right && (v.EndedAtUtc == null || v.EndedAtUtc > left)).OrderBy(v => v.StartedAtUtc).ToArray();
            if (periods.Length == 0)
            {
                var ambiguous = candidateVehiclePeriods.Any(v => !v.IsRealRider && v.RiderProfileId == assignment.RiderProfileId && !actualPeople.Any(p => p.RiderVehicleAssignmentId == v.Id));
                result.Add(new(assignment.RiderProfileId, null, ambiguous ? "Unknown" : "Private", null, null, left, right, ambiguous ? "Unresolved actual vehicle user identity" : "No recorded fleet custody in this interval")); continue;
            }
            var cursor = left;
            foreach (var period in periods)
            {
                var pStart = period.StartedAtUtc > left ? period.StartedAtUtc : left;
                var pEnd = period.EndedAtUtc is { } finish && finish < right ? finish : right;
                if (pStart > cursor) result.Add(new(assignment.RiderProfileId, null, "Private", null, null, cursor, pStart, "No fleet custody"));
                var vehicle = vehicles.SingleOrDefault(v => v.Id == period.VehicleId);
                var overlap = periods.Any(v => v.Id != period.Id && v.StartedAtUtc < pEnd && (v.EndedAtUtc == null || v.EndedAtUtc > pStart));
                result.Add(new(assignment.RiderProfileId, vehicle?.Id, overlap || vehicle is null ? "Unknown" : vehicle.OwnershipType == VehicleOwnershipType.ThirdParty ? "Private" : "Company",
                    vehicle?.VehicleType.ToString(), vehicle?.PlateNumberAr ?? vehicle?.PlateNumberEn, pStart, pEnd, overlap ? "Overlapping fleet custody" : null));
                if (pEnd > cursor) cursor = pEnd;
            }
            if (cursor < right) result.Add(new(assignment.RiderProfileId, null, "Private", null, null, cursor, right, "No fleet custody"));
        }
        var ownerships = result.Select(r => r.Ownership).Distinct().ToArray();
        var ownership = ownerships.Length == 0 || ownerships.Contains("Unknown") ? "Unknown" : ownerships.Length == 1 ? ownerships[0] : "Mixed";
        var sim = new PlatformSimContext(null, "Unknown", null, null, null, "No verified account phone");
        if (PhoneSimRules.TryNormalizePhoneNumber(details?.PhoneNumber ?? string.Empty, out var normalized))
        {
            var sims = await db.PhoneSimCards.AsNoTracking().Where(s => s.NormalizedPhoneNumber == normalized).ToArrayAsync(ct);
            if (sims.Length == 1)
            {
                var card = sims[0]; var holders = await db.RiderPhoneSimAssignments.AsNoTracking().Where(a => a.PhoneSimCardId == card.Id && a.EffectiveFrom <= through && (a.EffectiveTo == null || a.EffectiveTo >= from)).ToArrayAsync(ct);
                var ownershipHistory = await db.Set<LogisticsERP.Domain.Entities.Telecom.PhoneSimOwnershipRevision>().AsNoTracking().Where(r => r.PhoneSimCardId == card.Id && r.EffectiveFrom <= through)
                    .OrderBy(r => r.EffectiveFrom).ThenBy(r => r.CreatedAtUtc).ThenBy(r => r.Id).ToArrayAsync(ct);
                var revisions = ownershipHistory.GroupBy(r => r.EffectiveFrom).Select(g => g.Last()).ToArray();
                var timeline = new List<PlatformSimOwnershipPeriod>();
                var starts = new[] { from }.Concat(revisions.Where(r => r.EffectiveFrom > from).Select(r => r.EffectiveFrom)).ToArray();
                for (var i = 0; i < starts.Length; i++)
                {
                    var at = starts[i]; var revision = revisions.LastOrDefault(r => r.EffectiveFrom <= at);
                    var value = revision?.Ownership ?? (card.OwnershipEffectiveFrom <= at ? card.Ownership : "Unknown");
                    timeline.Add(new(at, i == starts.Length - 1 ? through : starts[i + 1].AddDays(-1), value, revision?.EvidenceReference));
                }
                var owners = timeline.Select(t => t.Ownership).Distinct().ToArray();
                sim = new(card.Id, owners.Contains("Unknown") ? "Unknown" : owners.Length > 1 ? "Mixed" : owners[0], card.ResponsibleEmployeeId,
                    holders.Length == 1 ? holders[0].RiderProfileId : null, card.CarrierName, holders.Length > 1 ? "Multiple holders in requested interval" : null) { OwnershipPeriods = timeline };
            }
            else sim = new(null, sims.Length == 0 && configuration.GetValue<bool>("Features:VerifiedCompanySimRegistry") ? "Private" : "Unknown", null, null, null, sims.Length > 1 ? "Ambiguous phone mapping" : "No ownership evidence in registry");
        }
        return Result.Success(new PlatformBusinessContext(accountId, registeredDetails?.RegisteredVehicleType, registeredDetails?.RegisteredVehiclePlate, registeredDetails?.RegisteredVehicleSerial,
            result.OrderBy(r => r.FromUtc).ToArray(), ownership, sim, clock.GetUtcNow(), Convert.ToBase64String(details?.RowVersion ?? account.RowVersion)));
    }
    public async Task<Result> UpdateBusinessAsync(Guid accountId, PlatformBusinessUpdate request, CancellationToken ct = default)
    {
        var account = await db.PlatformRiderAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null || !await Allowed(PermissionKeys.Operations.PlatformAccountsUpdate, account.ClientPlatformId, ct)) return Result.Failure(HrErrors.Forbidden);
        if (request.EffectiveFrom == default || request.EffectiveFrom > Today || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 2000
            || request.PhoneNumber?.Length > 32 || request.RegistryIdentifier?.Length > 150 || request.SettlementModel?.Length > 150
            || request.RegisteredVehicleType?.Length > 32 || request.RegisteredVehiclePlate?.Length > 32 || request.RegisteredVehicleSerial?.Length > 100) return Result.Failure(HrErrors.InvalidRequest);
        await using var scope = await PlatformAccountWriteScope.AcquireAsync(db, [accountId], ct);
        var details = await db.Set<PlatformAccountBusinessDetails>().SingleOrDefaultAsync(d => d.PlatformRiderAccountId == accountId, ct);
        if (Convert.ToBase64String(details?.RowVersion ?? account.RowVersion) != request.RowVersion) return Result.Failure(HrErrors.ConcurrencyConflict);
        var item = details ?? new PlatformAccountBusinessDetails { PlatformRiderAccountId = accountId };
        static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var required = new List<string>();
        if (item.PhoneNumber != Clean(request.PhoneNumber)) required.Add(PermissionKeys.PlatformFields.Contact);
        if (item.RegistryIdentifier != Clean(request.RegistryIdentifier)) required.Add(PermissionKeys.PlatformFields.Registry);
        if (item.SettlementModel != Clean(request.SettlementModel)) required.Add(PermissionKeys.PlatformFields.Settlement);
        if (item.RegisteredVehicleType != Clean(request.RegisteredVehicleType) || item.RegisteredVehiclePlate != Clean(request.RegisteredVehiclePlate) || item.RegisteredVehicleSerial != Clean(request.RegisteredVehicleSerial)) required.Add(PermissionKeys.PlatformFields.Vehicle);
        foreach (var key in required) if (!await Allowed(key, account.ClientPlatformId, ct)) return Result.Failure(HrErrors.Forbidden);
        var before = JsonSerializer.Serialize(item); item.PhoneNumber = Clean(request.PhoneNumber); item.RegistryIdentifier = Clean(request.RegistryIdentifier); item.SettlementModel = Clean(request.SettlementModel);
        item.RegisteredVehicleType = Clean(request.RegisteredVehicleType); item.RegisteredVehiclePlate = Clean(request.RegisteredVehiclePlate); item.RegisteredVehicleSerial = Clean(request.RegisteredVehicleSerial);
        if (details is null) db.Add(item);
        db.Add(new PlatformAccountBusinessRevision { PlatformRiderAccountId = accountId, EffectiveFrom = request.EffectiveFrom, Reason = request.Reason, BeforeJson = before, AfterJson = JsonSerializer.Serialize(item) });
        try { await db.SaveChangesAsync(ct); return Result.Success(); } catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure(HrErrors.ConcurrencyConflict); }
    }
    public async Task<Result> UpdateSimOwnershipAsync(Guid simId, PlatformSimOwnershipUpdate request, CancellationToken ct = default)
    {
        if (user.UserId is not { } uid || user.AuthorizationVersion is not { } version || !await permissions.HasPermissionAsync(uid, version, PermissionKeys.Operations.PhoneSimsUpdate, null, ct)) return Result.Failure(HrErrors.Forbidden);
        if (request.Ownership is not ("Company" or "Private" or "Unknown") || request.EffectiveFrom == default || request.EffectiveFrom > Today || string.IsNullOrWhiteSpace(request.EvidenceReference) || request.EvidenceReference.Length > 1000) return Result.Failure(HrErrors.InvalidRequest);
        var card = await db.PhoneSimCards.SingleOrDefaultAsync(s => s.Id == simId, ct);
        if (card is null) return Result.Failure(HrErrors.NotFound);
        if (Convert.ToBase64String(card.RowVersion) != request.RowVersion) return Result.Failure(HrErrors.ConcurrencyConflict);
        card.Ownership = request.Ownership; card.OwnershipEvidence = request.EvidenceReference; card.OwnershipEffectiveFrom = request.EffectiveFrom;
        db.Add(new LogisticsERP.Domain.Entities.Telecom.PhoneSimOwnershipRevision { PhoneSimCardId = simId, Ownership = request.Ownership,
            EffectiveFrom = request.EffectiveFrom, EvidenceReference = request.EvidenceReference });
        try { await db.SaveChangesAsync(ct); return Result.Success(); } catch (DbUpdateException) { db.ChangeTracker.Clear(); return Result.Failure(HrErrors.ConcurrencyConflict); }
    }
}
