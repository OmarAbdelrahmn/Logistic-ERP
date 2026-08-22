using System.Text.Json;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.SupportAccess;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class PermissionChecker(
    IdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    IMemoryCache memoryCache,
    TimeProvider timeProvider) : IPermissionChecker
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(60);

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        long authorizationVersion,
        string permissionKey,
        PermissionScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty
            || authorizationVersion < 1
            || string.IsNullOrWhiteSpace(permissionKey)
            || !PermissionKeys.All.Contains(permissionKey))
        {
            return false;
        }

        var snapshot = await GetSnapshotAsync(
            userId,
            authorizationVersion,
            cancellationToken);
        if (!snapshot.Definitions.TryGetValue(permissionKey, out var definition))
        {
            return false;
        }

        Guid? targetClientPlatformId = null;
        if (definition.RequiresClientScope && scope?.Type == AccessScopeType.ClientContract)
        {
            targetClientPlatformId = await applicationDbContext.ClientContracts
                .AsNoTracking()
                .Where(contract => contract.Id == scope.TargetId)
                .Select(contract => (Guid?)contract.ClientPlatformId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        var directAssignments = snapshot.DirectAssignments
            .Where(assignment => string.Equals(
                assignment.PermissionKey,
                permissionKey,
                StringComparison.Ordinal));

        if (directAssignments.Any(assignment =>
            assignment.Effect == PermissionEffect.Deny
            && IsApplicable(definition, assignment, scope, targetClientPlatformId, denyWithoutScopeIsGlobal: true)))
        {
            return false;
        }

        if (directAssignments.Any(assignment =>
            assignment.Effect == PermissionEffect.Grant
            && IsApplicable(definition, assignment, scope, targetClientPlatformId, denyWithoutScopeIsGlobal: false)))
        {
            return true;
        }

        return snapshot.RoleAssignments.Any(assignment =>
            string.Equals(assignment.PermissionKey, permissionKey, StringComparison.Ordinal)
            && IsApplicable(definition, assignment, scope, targetClientPlatformId, denyWithoutScopeIsGlobal: false));
    }

    public void InvalidateUser(Guid userId, long authorizationVersion) =>
        memoryCache.Remove($"permission-snapshot:{userId:N}:{authorizationVersion}");

    private async Task<AuthorizationSnapshot> GetSnapshotAsync(
        Guid userId,
        long authorizationVersion,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"permission-snapshot:{userId:N}:{authorizationVersion}";
        if (memoryCache.TryGetValue(cacheKey, out AuthorizationSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await LoadSnapshotAsync(userId, cancellationToken);
        memoryCache.Set(cacheKey, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = snapshot.ValidUntilUtc
        });
        return snapshot;
    }

    private async Task<AuthorizationSnapshot> LoadSnapshotAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var definitions = await applicationDbContext.PermissionDefinitions
            .AsNoTracking()
            .Where(definition => !definition.IsDeprecated)
            .Select(definition => new PermissionDefinitionSnapshot(
                definition.Key,
                definition.RequiresHousingScope,
                definition.RequiresClientScope))
            .ToDictionaryAsync(definition => definition.Key, StringComparer.Ordinal, cancellationToken);

        var roleRows = await (
            from assignment in identityDbContext.UserRoleAssignments.AsNoTracking()
            join role in identityDbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            join permission in identityDbContext.RolePermissionGrants.AsNoTracking() on role.Id equals permission.RoleId
            where assignment.UserId == userId
                && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now)
                && role.Status == RoleStatus.Active
            select new AssignmentRow(
                assignment.Id,
                permission.PermissionKey,
                PermissionEffect.Grant,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                assignment.IncludesFuturePlatformContracts,
                assignment.StartsAtUtc,
                assignment.ExpiresAtUtc))
            .ToArrayAsync(cancellationToken);

        var directRows = await identityDbContext.UserDirectPermissionAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId
                && (assignment.ExpiresAtUtc == null || assignment.ExpiresAtUtc > now))
            .Select(assignment => new AssignmentRow(
                assignment.Id,
                assignment.PermissionKey,
                assignment.Effect,
                assignment.IsAllHousingScope,
                assignment.IsAllClientScope,
                assignment.IncludesFuturePlatformContracts,
                assignment.StartsAtUtc,
                assignment.ExpiresAtUtc))
            .ToArrayAsync(cancellationToken);

        var supportRows = await identityDbContext.SupportAccessGrants
            .AsNoTracking()
            .Where(grant => grant.PlatformOperatorUserId == userId
                && (grant.Status == SupportAccessStatus.Approved || grant.Status == SupportAccessStatus.Active)
                && grant.RequestedStartAtUtc <= now
                && grant.RequestedEndAtUtc > now)
            .Select(grant => new
            {
                grant.Id,
                grant.RequestedPermissionsJson,
                grant.RequestedScopesJson,
                grant.RequestedStartAtUtc,
                grant.RequestedEndAtUtc
            })
            .ToArrayAsync(cancellationToken);

        var supportAssignments = supportRows.SelectMany(row =>
        {
            var scopes = DeserializeSupportScopes(row.RequestedScopesJson);
            return DeserializeSupportPermissions(row.RequestedPermissionsJson)
                .Where(PermissionKeys.All.Contains)
                .Select(permission => new PermissionAssignmentSnapshot(
                    permission,
                    PermissionEffect.Grant,
                    false,
                    false,
                    false,
                    scopes));
        }).ToArray();

        var roleAssignmentIds = roleRows.Select(row => row.AssignmentId).Distinct().ToArray();
        var directAssignmentIds = directRows.Select(row => row.AssignmentId).Distinct().ToArray();
        var scopeRows = roleAssignmentIds.Length == 0 && directAssignmentIds.Length == 0
            ? []
            : await identityDbContext.AccessScopes
                .AsNoTracking()
                .Where(scope =>
                    scope.UserRoleAssignmentId.HasValue
                        && roleAssignmentIds.Contains(scope.UserRoleAssignmentId.Value)
                    || scope.DirectPermissionAssignmentId.HasValue
                        && directAssignmentIds.Contains(scope.DirectPermissionAssignmentId.Value))
                .Select(scope => new ScopeRow(
                    scope.UserRoleAssignmentId,
                    scope.DirectPermissionAssignmentId,
                    scope.ScopeType,
                    scope.TargetId))
                .ToArrayAsync(cancellationToken);

        var roleScopes = scopeRows
            .Where(scope => scope.RoleAssignmentId.HasValue)
            .ToLookup(scope => scope.RoleAssignmentId!.Value);
        var directScopes = scopeRows
            .Where(scope => scope.DirectAssignmentId.HasValue)
            .ToLookup(scope => scope.DirectAssignmentId!.Value);

        var authorizationBoundaries = roleRows
            .SelectMany(row => new DateTimeOffset?[] { row.StartsAtUtc, row.ExpiresAtUtc })
            .Concat(directRows.SelectMany(row => new DateTimeOffset?[] { row.StartsAtUtc, row.ExpiresAtUtc }))
            .Concat(supportRows.SelectMany(row => new DateTimeOffset?[] { row.RequestedStartAtUtc, row.RequestedEndAtUtc }))
            .Where(boundary => boundary > now)
            .Select(boundary => boundary!.Value);
        var validUntilUtc = authorizationBoundaries
            .Append(now.Add(CacheLifetime))
            .Min();

        return new AuthorizationSnapshot(
            definitions,
            roleRows
                .Where(row => row.StartsAtUtc <= now)
                .Select(row => ToSnapshot(row, roleScopes[row.AssignmentId]))
                .Concat(supportAssignments)
                .ToArray(),
            directRows
                .Where(row => row.StartsAtUtc <= now)
                .Select(row => ToSnapshot(row, directScopes[row.AssignmentId]))
                .ToArray(),
            validUntilUtc);
    }

    private static string[] DeserializeSupportPermissions(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static PermissionScope[] DeserializeSupportScopes(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<SupportAccessScopeRequest[]>(json) ?? [])
                .Where(scope => Enum.TryParse<AccessScopeType>(scope.Type, true, out _))
                .Select(scope => new PermissionScope(Enum.Parse<AccessScopeType>(scope.Type, true), scope.TargetId))
                .Distinct()
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static PermissionAssignmentSnapshot ToSnapshot(
        AssignmentRow row,
        IEnumerable<ScopeRow> scopes) => new(
            row.PermissionKey,
            row.Effect,
            row.IsAllHousingScope,
            row.IsAllClientScope,
            row.IncludesFuturePlatformContracts,
            scopes.Select(scope => new PermissionScope(scope.Type, scope.TargetId)).ToArray());

    private static bool IsApplicable(
        PermissionDefinitionSnapshot definition,
        PermissionAssignmentSnapshot assignment,
        PermissionScope? requestedScope,
        Guid? targetClientPlatformId,
        bool denyWithoutScopeIsGlobal)
    {
        if (!definition.RequiresHousingScope && !definition.RequiresClientScope)
        {
            return true;
        }

        if (denyWithoutScopeIsGlobal
            && !assignment.IsAllHousingScope
            && !assignment.IsAllClientScope
            && assignment.Scopes.Count == 0)
        {
            return true;
        }

        if (definition.RequiresHousingScope)
        {
            if (assignment.IsAllHousingScope)
            {
                return true;
            }

            if (requestedScope is not { Type: AccessScopeType.Housing })
            {
                return false;
            }

            return assignment.Scopes.Contains(requestedScope);
        }

        if (assignment.IsAllClientScope)
        {
            return true;
        }

        if (requestedScope is null
            || requestedScope.Type is not (AccessScopeType.ClientPlatform or AccessScopeType.ClientContract))
        {
            return false;
        }

        if (assignment.Scopes.Contains(requestedScope))
        {
            return true;
        }

        return requestedScope.Type == AccessScopeType.ClientContract
            && assignment.IncludesFuturePlatformContracts
            && targetClientPlatformId.HasValue
            && assignment.Scopes.Contains(new PermissionScope(
                AccessScopeType.ClientPlatform,
                targetClientPlatformId.Value));
    }

    private sealed record AuthorizationSnapshot(
        IReadOnlyDictionary<string, PermissionDefinitionSnapshot> Definitions,
        IReadOnlyList<PermissionAssignmentSnapshot> RoleAssignments,
        IReadOnlyList<PermissionAssignmentSnapshot> DirectAssignments,
        DateTimeOffset ValidUntilUtc);

    private sealed record PermissionDefinitionSnapshot(
        string Key,
        bool RequiresHousingScope,
        bool RequiresClientScope);

    private sealed record PermissionAssignmentSnapshot(
        string PermissionKey,
        PermissionEffect Effect,
        bool IsAllHousingScope,
        bool IsAllClientScope,
        bool IncludesFuturePlatformContracts,
        IReadOnlyList<PermissionScope> Scopes);

    private sealed record AssignmentRow(
        Guid AssignmentId,
        string PermissionKey,
        PermissionEffect Effect,
        bool IsAllHousingScope,
        bool IsAllClientScope,
        bool IncludesFuturePlatformContracts,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset? ExpiresAtUtc);

    private sealed record ScopeRow(
        Guid? RoleAssignmentId,
        Guid? DirectAssignmentId,
        AccessScopeType Type,
        Guid TargetId);
}
