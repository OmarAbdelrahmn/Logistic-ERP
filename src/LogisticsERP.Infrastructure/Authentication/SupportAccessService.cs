using System.Text.Json;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Common.Results;
using LogisticsERP.Application.Features.SupportAccess;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Identity;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Infrastructure.Authentication;

internal sealed class SupportAccessService(
    IdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker,
    TimeProvider timeProvider) : ISupportAccessService
{
    public async Task<Result<IReadOnlyList<SupportAccessResponse>>> GetAsync(
        Guid? operatorUserId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        SupportAccessStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SupportAccessStatus>(status, true, out var value))
            {
                return Result.Failure<IReadOnlyList<SupportAccessResponse>>(SupportAccessErrors.InvalidRequest);
            }
            parsedStatus = value;
        }

        var query = identityDbContext.SupportAccessGrants.AsNoTracking();
        if (operatorUserId.HasValue)
        {
            query = query.Where(item => item.PlatformOperatorUserId == operatorUserId.Value);
        }
        if (parsedStatus.HasValue)
        {
            query = query.Where(item => item.Status == parsedStatus.Value);
        }
        var rows = await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToArrayAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SupportAccessResponse>>(rows.Select(ToResponse).ToArray());
    }

    public async Task<Result<SupportAccessResponse>> RequestAsync(
        RequestSupportAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId
            || request.PermissionKeys.Count == 0
            || request.PermissionKeys.Any(key => !PermissionKeys.All.Contains(key))
            || request.PermissionKeys.Distinct(StringComparer.Ordinal).Count() != request.PermissionKeys.Count
            || string.IsNullOrWhiteSpace(request.Reason)
            || request.RequestedStartAtUtc >= request.RequestedEndAtUtc
            || request.RequestedEndAtUtc - request.RequestedStartAtUtc > TimeSpan.FromHours(24)
            || request.IsBreakGlass && string.IsNullOrWhiteSpace(request.BreakGlassJustification))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.InvalidRequest);
        }

        var operatorId = request.PlatformOperatorUserId ?? actorId;
        var canManage = await CanManageAsync(actorId, cancellationToken);
        if (operatorId != actorId && !canManage || request.IsBreakGlass && !canManage)
        {
            return Result.Failure<SupportAccessResponse>(new OperationError(
                "support_access.forbidden",
                "Only a high-trust support-access administrator can request access for another operator or use break-glass.",
                ErrorType.Forbidden));
        }
        if (!await identityDbContext.Users.AnyAsync(item => item.Id == operatorId, cancellationToken)
            || !await ValidateScopesAsync(request.Scopes, cancellationToken))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        var hasOverlap = await identityDbContext.SupportAccessGrants.AnyAsync(item =>
            item.PlatformOperatorUserId == operatorId
            && (item.Status == SupportAccessStatus.Pending
                || item.Status == SupportAccessStatus.Approved
                || item.Status == SupportAccessStatus.Active)
            && item.RequestedStartAtUtc < request.RequestedEndAtUtc
            && item.RequestedEndAtUtc > request.RequestedStartAtUtc,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.Conflict);
        }

        var grant = new SupportAccessGrant
        {
            PlatformOperatorUserId = operatorId,
            RequestedPermissionsJson = JsonSerializer.Serialize(request.PermissionKeys.Order(StringComparer.Ordinal)),
            RequestedScopesJson = JsonSerializer.Serialize(request.Scopes),
            Reason = request.Reason.Trim(),
            Status = request.IsBreakGlass ? SupportAccessStatus.Active : SupportAccessStatus.Pending,
            RequestedStartAtUtc = request.RequestedStartAtUtc,
            RequestedEndAtUtc = request.RequestedEndAtUtc,
            ApprovedByUserId = request.IsBreakGlass ? actorId : null,
            ApprovedAtUtc = request.IsBreakGlass ? now : null,
            IsBreakGlass = request.IsBreakGlass,
            BreakGlassJustification = TrimOrNull(request.BreakGlassJustification)
        };
        identityDbContext.SupportAccessGrants.Add(grant);
        await identityDbContext.SaveChangesAsync(cancellationToken);
        await InvalidateOperatorAsync(operatorId, cancellationToken);
        return Result.Success(ToResponse(grant));
    }

    public async Task<Result<SupportAccessResponse>> ResolveAsync(
        Guid id,
        ResolveSupportAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId
            || string.IsNullOrWhiteSpace(request.Reason)
            || !await CanManageAsync(actorId, cancellationToken))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.CurrentUserUnavailable);
        }
        var grant = await identityDbContext.SupportAccessGrants.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (grant is null)
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.NotFound);
        }
        if (!MatchesRowVersion(grant.RowVersion, request.RowVersion))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.ConcurrencyConflict);
        }
        if (grant.Status != SupportAccessStatus.Pending)
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.Conflict);
        }

        var now = timeProvider.GetUtcNow();
        grant.Status = request.Approve
            ? grant.RequestedStartAtUtc <= now ? SupportAccessStatus.Active : SupportAccessStatus.Approved
            : SupportAccessStatus.Rejected;
        grant.ApprovedByUserId = actorId;
        grant.ApprovedAtUtc = now;
        if (!request.Approve)
        {
            grant.DeletionReason = request.Reason.Trim();
        }
        await identityDbContext.SaveChangesAsync(cancellationToken);
        await InvalidateOperatorAsync(grant.PlatformOperatorUserId, cancellationToken);
        return Result.Success(ToResponse(grant));
    }

    public async Task<Result<SupportAccessResponse>> RevokeAsync(
        Guid id,
        RevokeSupportAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } actorId || string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.InvalidRequest);
        }
        var grant = await identityDbContext.SupportAccessGrants.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (grant is null)
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.NotFound);
        }
        if (grant.PlatformOperatorUserId != actorId && !await CanManageAsync(actorId, cancellationToken))
        {
            return Result.Failure<SupportAccessResponse>(new OperationError(
                "support_access.forbidden", "Only the operator or a support-access administrator can revoke this grant.", ErrorType.Forbidden));
        }
        if (!MatchesRowVersion(grant.RowVersion, request.RowVersion))
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.ConcurrencyConflict);
        }
        if (grant.Status is SupportAccessStatus.Rejected or SupportAccessStatus.Revoked or SupportAccessStatus.Expired)
        {
            return Result.Failure<SupportAccessResponse>(SupportAccessErrors.Conflict);
        }

        grant.Status = SupportAccessStatus.Revoked;
        grant.RevokedAtUtc = timeProvider.GetUtcNow();
        grant.RevokedByUserId = actorId;
        grant.DeletionReason = request.Reason.Trim();
        await identityDbContext.SaveChangesAsync(cancellationToken);
        await InvalidateOperatorAsync(grant.PlatformOperatorUserId, cancellationToken);
        return Result.Success(ToResponse(grant));
    }

    private async Task<bool> ValidateScopesAsync(
        IReadOnlyCollection<SupportAccessScopeRequest> scopes,
        CancellationToken cancellationToken)
    {
        if (scopes.Any(scope => scope.TargetId == Guid.Empty || !Enum.TryParse<AccessScopeType>(scope.Type, true, out _)))
        {
            return false;
        }
        foreach (var scope in scopes)
        {
            var type = Enum.Parse<AccessScopeType>(scope.Type, true);
            var exists = type switch
            {
                AccessScopeType.Housing => await applicationDbContext.Housing.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                AccessScopeType.ClientPlatform => await applicationDbContext.ClientPlatforms.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                AccessScopeType.ClientContract => await applicationDbContext.ClientContracts.AnyAsync(item => item.Id == scope.TargetId, cancellationToken),
                _ => false
            };
            if (!exists)
            {
                return false;
            }
        }
        return true;
    }

    private async Task<bool> CanManageAsync(Guid userId, CancellationToken cancellationToken) =>
        currentUser.AuthorizationVersion is { } version
        && await permissionChecker.HasPermissionAsync(
            userId,
            version,
            PermissionKeys.Security.SupportAccessManage,
            cancellationToken: cancellationToken);

    private async Task InvalidateOperatorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var version = await identityDbContext.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.AuthorizationVersion)
            .SingleOrDefaultAsync(cancellationToken);
        if (version > 0)
        {
            permissionChecker.InvalidateUser(userId, version);
        }
    }

    private static SupportAccessResponse ToResponse(SupportAccessGrant item) => new(
        item.Id,
        item.PlatformOperatorUserId,
        DeserializePermissions(item.RequestedPermissionsJson),
        DeserializeScopes(item.RequestedScopesJson),
        item.Reason,
        EffectiveStatus(item).ToString(),
        item.RequestedStartAtUtc,
        item.RequestedEndAtUtc,
        item.ApprovedByUserId,
        item.ApprovedAtUtc,
        item.IsBreakGlass,
        item.BreakGlassJustification,
        item.RevokedAtUtc,
        item.RevokedByUserId,
        Convert.ToBase64String(item.RowVersion));

    private static SupportAccessStatus EffectiveStatus(SupportAccessGrant item) =>
        item.Status is SupportAccessStatus.Approved or SupportAccessStatus.Active
            && item.RequestedEndAtUtc <= DateTimeOffset.UtcNow
            ? SupportAccessStatus.Expired
            : item.Status;

    private static string[] DeserializePermissions(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static SupportAccessScopeRequest[] DeserializeScopes(string json)
    {
        try { return JsonSerializer.Deserialize<SupportAccessScopeRequest[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static bool MatchesRowVersion(byte[] value, string? supplied) =>
        !string.IsNullOrWhiteSpace(supplied)
        && string.Equals(Convert.ToBase64String(value), supplied, StringComparison.Ordinal);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
