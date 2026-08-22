using System.Globalization;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Authorization;

internal sealed class PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
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

        if (await permissionChecker.HasPermissionAsync(
            userId,
            authorizationVersion,
            requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
    }
}
