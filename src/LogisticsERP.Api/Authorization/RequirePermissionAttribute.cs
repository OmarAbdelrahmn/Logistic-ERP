using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionKey)
    {
        if (!PermissionKeys.All.Contains(permissionKey))
        {
            throw new ArgumentException($"Unknown permission key '{permissionKey}'.", nameof(permissionKey));
        }

        Policy = AuthenticationPolicies.PermissionPrefix + permissionKey;
    }
}
