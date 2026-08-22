using Microsoft.AspNetCore.Authorization;

namespace LogisticsERP.Api.Authorization;

internal sealed record PermissionRequirement(string PermissionKey) : IAuthorizationRequirement;
