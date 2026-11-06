using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionKey) : this([permissionKey]) { }

    public RequirePermissionAttribute(params string[] permissionKeys)
    {
        if (permissionKeys.Length == 0 || permissionKeys.Any(key => !PermissionKeys.All.Contains(key)))
        {
            throw new ArgumentException("Unknown permission key.", nameof(permissionKeys));
        }

        Policy = AuthenticationPolicies.PermissionPrefix + string.Join("|", permissionKeys);
    }
}
