using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Reporting.NotificationsRead)]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int pageSize = 50,
        [FromQuery] string? cursor = null, [FromQuery] string[]? permissions = null, CancellationToken cancellationToken = default)
    {
        var requested = Request.Query.ContainsKey("permissions") ? permissions ?? [] : null;
        var result = await service.GetMineAsync(unreadOnly, pageSize, cursor, requested, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("unread-count")]
    [RequirePermission(PermissionKeys.Reporting.NotificationsRead)]
    public async Task<IActionResult> UnreadCount([FromQuery] string[]? permissions = null, CancellationToken cancellationToken = default)
    {
        var requested = Request.Query.ContainsKey("permissions") ? permissions ?? [] : null;
        var result = await service.GetUnreadCountAsync(requested, cancellationToken);
        return result.IsSuccess ? Ok(new { Count = result.Value }) : result.ToProblem(HttpContext);
    }

    [HttpPost("query")]
    [RequirePermission(PermissionKeys.Reporting.NotificationsRead)]
    public async Task<IActionResult> Query([FromBody] NotificationQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.QueryAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Reporting.NotificationsManage)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/state")]
    [RequirePermission(PermissionKeys.Reporting.NotificationsRead)]
    public async Task<IActionResult> ChangeState(Guid id, [FromBody] NotificationStateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ChangeStateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
