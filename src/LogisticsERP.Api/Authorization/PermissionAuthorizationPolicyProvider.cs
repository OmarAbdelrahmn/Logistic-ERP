using System.Collections.Concurrent;
using LogisticsERP.Application.Abstractions.Authentication;
using LogisticsERP.Application.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LogisticsERP.Api.Authorization;

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> permissionPolicies =
        new(StringComparer.Ordinal);

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(AuthenticationPolicies.PermissionPrefix, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permissionKey = policyName[AuthenticationPolicies.PermissionPrefix.Length..];
        if (!PermissionKeys.All.Contains(permissionKey))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = permissionPolicies.GetOrAdd(permissionKey, static key =>
            new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim(AuthenticationClaimNames.PasswordChangeRequired, "false")
                .AddRequirements(new PermissionRequirement(key))
                .Build());

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
