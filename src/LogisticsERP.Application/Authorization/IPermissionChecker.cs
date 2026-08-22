using LogisticsERP.Domain.Enums;

namespace LogisticsERP.Application.Authorization;

public sealed record PermissionScope(AccessScopeType Type, Guid TargetId);

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        long authorizationVersion,
        string permissionKey,
        PermissionScope? scope = null,
        CancellationToken cancellationToken = default);

    void InvalidateUser(Guid userId, long authorizationVersion);
}
