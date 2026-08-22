using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.System;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/audit-entries")]
[RequirePermission(PermissionKeys.Security.AuditRead)]
public sealed class AuditEntriesController(IAuditQueryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] string? correlationId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int pageSize = 100,
        [FromQuery] long? beforeSequence = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.QueryAsync(new AuditQuery(actorUserId, entityType, entityId, action,
            correlationId, fromUtc, toUtc, pageSize, beforeSequence), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> Get(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(eventId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}

