using System.Globalization;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using LogisticsERP.Domain.Enums;
using LogisticsERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsERP.Api.Authorization;

internal sealed class PermissionAuthorizationHandler(IPermissionChecker permissionChecker, ApplicationDbContext dbContext)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!string.Equals(
                context.User.FindFirst(AuthenticationClaimNames.PasswordChangeRequired)?.Value,
                "false",
                StringComparison.Ordinal)
            || !Guid.TryParse(
                context.User.FindFirst(AuthenticationClaimNames.Subject)?.Value,
                out var userId)
            || !long.TryParse(
                context.User.FindFirst(AuthenticationClaimNames.AuthorizationVersion)?.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var authorizationVersion))
        {
            return;
        }

        PermissionScope? scope = null;
        if (requirement.PermissionKey.StartsWith("jahez.", StringComparison.Ordinal))
        {
            var platformId = await dbContext.ClientPlatforms.AsNoTracking().Where(x => x.Code == "JAHEZ")
                .Select(x => (Guid?)x.Id).SingleOrDefaultAsync();
            if (!platformId.HasValue) return;
            scope = new PermissionScope(AccessScopeType.ClientPlatform, platformId.Value);
        }

        if (await permissionChecker.HasPermissionAsync(
            userId,
            authorizationVersion,
            requirement.PermissionKey,
            scope))
        {
            context.Succeed(requirement);
        }
    }
}
