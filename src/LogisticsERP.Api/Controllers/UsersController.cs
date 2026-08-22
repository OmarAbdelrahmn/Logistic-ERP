using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserManagementService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Security.UsersRead)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await service.GetUsersAsync(search, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{userId:guid}")]
    [RequirePermission(PermissionKeys.Security.UsersRead)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var result = await service.GetUserAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Security.UsersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateUserAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { userId = result.Value!.Id }, result.Value)
            : result.ToProblem(HttpContext);
    }

    [HttpPut("{userId:guid}")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("{userId:guid}/status")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> UpdateStatus(Guid userId, [FromBody] UpdateManagedUserStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateStatusAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{userId:guid}/password/reset")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> ResetPassword(Guid userId, [FromBody] ResetManagedUserPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ResetPasswordAsync(userId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("{userId:guid}/temporary-credentials")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> IssueTemporaryCredential(
        Guid userId,
        [FromBody] IssueTemporaryCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.IssueTemporaryCredentialAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{userId:guid}/temporary-credentials/{credentialId:guid}/revoke")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> RevokeTemporaryCredential(
        Guid userId,
        Guid credentialId,
        [FromBody] RevokeTemporaryCredentialRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.RevokeTemporaryCredentialAsync(userId, credentialId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPost("{userId:guid}/sessions/revoke")]
    [RequirePermission(PermissionKeys.Security.UsersUpdate)]
    public async Task<IActionResult> RevokeSessions(Guid userId, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var result = await service.RevokeSessionsAsync(userId, reason, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpPatch("{userId:guid}/archive")]
    [RequirePermission(PermissionKeys.Security.UsersArchive)]
    public async Task<IActionResult> Archive(Guid userId, [FromBody] ArchiveManagedUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveUserAsync(userId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("roles")]
    [RequirePermission(PermissionKeys.Security.RolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await service.GetRolesAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("roles")]
    [RequirePermission(PermissionKeys.Security.RolesManage)]
    public async Task<IActionResult> CreateRole([FromBody] ManagedRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertRoleAsync(null, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("roles/{roleId:guid}")]
    [RequirePermission(PermissionKeys.Security.RolesManage)]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] ManagedRoleUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertRoleAsync(roleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("roles/{roleId:guid}/permissions")]
    [RequirePermission(PermissionKeys.Security.RolesManage)]
    public async Task<IActionResult> ReplaceRolePermissions(Guid roleId, [FromBody] ReplaceRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReplaceRolePermissionsAsync(roleId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPatch("roles/{roleId:guid}/archive")]
    [RequirePermission(PermissionKeys.Security.RolesManage)]
    public async Task<IActionResult> ArchiveRole(Guid roleId, [FromBody] ArchiveManagedUserRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ArchiveRoleAsync(roleId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblem(HttpContext);
    }

    [HttpGet("permissions")]
    [RequirePermission(PermissionKeys.Security.PermissionsRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await service.GetPermissionsAsync(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{userId:guid}/authorization")]
    [RequirePermission(PermissionKeys.Security.PermissionsRead)]
    public async Task<IActionResult> GetAuthorization(Guid userId, CancellationToken cancellationToken)
    {
        var result = await service.GetAuthorizationAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{userId:guid}/roles")]
    [RequirePermission(PermissionKeys.Security.RolesManage)]
    public async Task<IActionResult> ReplaceRoles(Guid userId, [FromBody] ReplaceManagedUserRolesRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReplaceRolesAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPut("{userId:guid}/permissions")]
    [RequirePermission(PermissionKeys.Security.PermissionsManage)]
    public async Task<IActionResult> ReplacePermissions(Guid userId, [FromBody] ReplaceManagedUserPermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReplacePermissionsAsync(userId, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
