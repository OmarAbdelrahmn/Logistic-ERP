using LogisticsERP.Api.Authorization;
using LogisticsERP.Api.ErrorHandling;
using LogisticsERP.Application.Authorization;
using LogisticsERP.Application.Features.SupportAccess;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsERP.Api.Controllers;

[ApiController]
[Route("api/support-access")]
public sealed class SupportAccessController(ISupportAccessService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Security.SupportAccessManage)]
    public async Task<IActionResult> Get([FromQuery] Guid? operatorUserId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(operatorUserId, status, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> RequestAccess([FromBody] RequestSupportAccessRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RequestAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.Security.SupportAccessManage)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveSupportAccessRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ResolveAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokeSupportAccessRequest request, CancellationToken cancellationToken)
    {
        var result = await service.RevokeAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem(HttpContext);
    }
}
